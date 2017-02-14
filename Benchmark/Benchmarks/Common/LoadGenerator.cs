using Orleans.Benchmarks.Common;
using Microsoft.ApplicationInsights;
using Microsoft.WindowsAzure.ServiceRuntime;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using Newtonsoft.Json.Linq;
using Orleans;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.WebSockets;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Orleans.Benchmarks.Common
{
    public class LoadGenerator
    {

        public LoadGenerator(string lgid, Func<string,IBenchmark> benchmarkfactory)
        {
            this.lgid = lgid;
            this.benchmarkfactory = benchmarkfactory;
        }
        private string lgid;
        Func<string, IBenchmark> benchmarkfactory;

        private ClientWebSocket ws = null;
        public CancellationTokenSource ws_cancel;
        public BatchWorker sendworker;
        public List<string> responsequeue = new List<string>();
        public Object responsequeuelock = new Object();

        private byte[] receiveBuffer = new byte[2048];


        public async Task SendQueuedResponses()
        {
            List<string> work = null;
            lock (responsequeuelock)
            {
                if (responsequeue != null)
                {
                    work = responsequeue;
                    responsequeue = new List<string>();
                }

            }
            if (work != null)
                foreach (var response in work)
                    if (!ws_cancel.Token.IsCancellationRequested)
                    {
                        var outputBuffer = new ArraySegment<byte>(Encoding.UTF8.GetBytes(response));
                        try
                        {
                            var sendtask = ws.SendAsync(outputBuffer, WebSocketMessageType.Text, true, ws_cancel.Token);

                            await Task.WhenAny(sendtask, Task.Delay(30000));

                            if (!sendtask.IsCompleted)
                            {
                                // close ws if it takes more than 30 sec to send something
                                LgTrace("canceling ws because sendasync took more than 30 sec");
                                ws_cancel.Cancel();
                            }
                        }
                        catch (Exception e)
                        {
                            LgTrace(string.Format("canceling websocket due to send exception {0}", e));
                            ws_cancel.Cancel();
                        }
                    }

        }

        private ScenarioTracker currentscenario;

        private class ScenarioTracker
        {
            public BatchingLogger logger = null;
            public int robotcount = 0;
            public Task<string>[] robottasks;
            public string testname;
            public string benchmarkname;
            public string scenarioname;
            public string lgname;
            public string scenarioparameters;
            public IBenchmark benchmark;
            public IScenario scenario;

            public async Task shutdown()
            {
                if (shutdowntask == null)
                    shutdowntask = ScenarioCompleted();
                await shutdowntask;
            }
            private Task shutdowntask;
            private async Task ScenarioCompleted()
            {
                int limit_sec = 30;

                while (robotcount > 0)
                {
                    ScenarioTrace(this, string.Format("Waiting for {0} robots to finish", robotcount));
                    await Task.Delay(1000);

                    if (limit_sec-- == 0)
                    {
                        ScenarioTrace(this, string.Format("Unclean shutdown, waiting for {0} robots to finish", robotcount));
                        logger.Done();
                    }
                }

                int successful = 0;
                int faulted = 0;
                int unused = 0;
                Exception sample = null;

                foreach (var tsk in robottasks)
                {
                    if (tsk == null)
                        unused++;
                    else if (tsk.IsFaulted)
                    {
                        faulted++;
                        if (sample == null)
                            sample = tsk.Exception;
                    }
                    else if (tsk.IsCompleted)
                    {
                        successful++;
                    }
                }

                ScenarioTrace(this, string.Format("Finished robots {0} successfully, {1} with errors, {2} unused", successful, faulted, unused));
                if (sample != null)
                    ScenarioTrace(this, string.Format("Exception sample: {0}", sample));

                logger.Done();
            }

        }



        private static void RobotTrace(ScenarioTracker t, int i, string msg)
        {
            var formattedtime = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss.fff");
            t.logger.Trace(string.Format("{3} {2} R{0:D6} {1}", i, msg, t.lgname, formattedtime));
            System.Diagnostics.Trace.TraceInformation(string.Format("{5} {2}.{3} {4} R{0:D6} {1}", i, msg, t.testname, t.scenarioname, t.lgname, formattedtime));
            TelemetryClient telemetryClient = new TelemetryClient();
            telemetryClient.TrackTrace(string.Format("{5} {2}.{3} {4} R{0:D6} {1}", i, msg, t.testname, t.scenarioname, t.lgname, formattedtime));
        }

        private static void ScenarioTrace(ScenarioTracker t, string msg)
        {
            var fmsg = string.Format("{1} {0}", msg, t.lgname);
            t.logger.Trace(fmsg);
            System.Diagnostics.Trace.TraceInformation(fmsg);
            TelemetryClient telemetryClient = new TelemetryClient();
            telemetryClient.TrackTrace(fmsg);

        }

     

        private void LgTrace(string msg)
        {
            msg = "LoadGenerator: " + msg;
            if (currentscenario != null)
            {
                ScenarioTrace(currentscenario, msg);
            }
            else
            {
                System.Diagnostics.Trace.TraceInformation(msg);
                TelemetryClient telemetryClient = new TelemetryClient();
                telemetryClient.TrackTrace(msg);
            }
        }


      
         


        private async Task<ScenarioTracker> StartScenarioAsync(JObject testdata)
        {
            var newscenario = new ScenarioTracker();

            try
            {

                newscenario.testname = (string)testdata["testname"];
                newscenario.lgname = (string)testdata["lgname"];
                newscenario.benchmarkname = (string)testdata["benchmarkname"];
                newscenario.scenarioname = (string)testdata["scenarioname"];
                newscenario.scenarioparameters = (string)testdata["scenarioparameters"];

                var testdir = await TestResultStorage.GetOrCreateTestDirectory(newscenario.testname);
                var scenariodir = testdir.GetDirectoryReference(newscenario.scenarioname);
                var blob = scenariodir.GetBlockBlobReference(SecUtility.Escape(newscenario.lgname));


                // CloudStorageAccount account = Config.getBenchmarkStorage();
                // CloudBlobClient blobclient = account.CreateCloudBlobClient();
                //  CloudBlobContainer container = blobclient.GetContainerReference("tests");
                //  await container.CreateIfNotExistsAsync();
                // CloudBlobDirectory testdir = container.GetDirectoryReference("tests");
                //  testdir = container.GetDirectoryReference("y");
                //  var blob = testdir.GetBlockBlobReference("z");

                newscenario.logger = new BatchingLogger(blob, () => newscenario.robotcount > 0, newscenario.shutdown);

                // find benchmark
                newscenario.benchmark = benchmarkfactory(newscenario.benchmarkname);

                // parse json parameters
                var json = JObject.Parse(newscenario.scenarioparameters);

                // create scenario
                LgTrace(json.ToString());
                newscenario.scenario = newscenario.benchmark.CreateScenarioFromJson(json, newscenario.testname.GetHashCode());
               
                newscenario.robottasks = new Task<string>[newscenario.scenario.NumRobots];
         
                return newscenario;
            }
            catch (Exception e)
            {
                var errmsg = string.Format("Could not find/construct scenario {0}, exception {1}", newscenario.scenarioname ?? "?", e);
                LgTrace(errmsg);

                return null;
            }
        }

        private async Task<string> StartRobotAsync(ScenarioTracker scenariotracker, JObject testdata, WebSocket ws, CancellationToken cancellationToken)
        {
            Interlocked.Increment(ref scenariotracker.robotcount);

            try
            {
                // extract contents of message
                int robotnr = int.Parse((string)testdata["robotnr"]);
                string args = (string)testdata["args"];

                String retval = "";
                bool success = true;
                string statsBase64 = "";

                Action<string> robottracer = (string msg) =>
                    RobotTrace(scenariotracker, robotnr, msg);

                IClient client = null;

                
                client = new DirectClient(robotnr, robottracer);
                

                if (success)
                {
                    //robottracer("Start " + args);

                    //Should catch exceptions from the robot script and notify the conductor of the failure. Which may decide to retry if required.
                    //no need to disconnect from the conductor since this is the FE error.

                    try
                    {
                        // run robot on threadpool
                        retval = await Task.Run(() =>
                        {
                            //robottracer("Run robot "+robotnr);
                            return scenariotracker.scenario.RobotScript(client, robotnr, args);
                        });
                    }
                    catch (Exception ex)
                    {
                        while (ex is AggregateException)
                            ex = ex.InnerException;

                        success = false;
                        retval = ex.Message + "\n" + ex.StackTrace;
                    }

                    //robottracer("End " + (success ? "" : "(FAIL) ") + retval);

                    if (client.Stats != null)
                    {
                        BinaryFormatter bf = new BinaryFormatter();
                        using (MemoryStream ms = new MemoryStream())
                        {
                            bf.Serialize(ms, client.Stats);
                            ms.Flush();
                            statsBase64 = System.Convert.ToBase64String(ms.ToArray());
                            byte[] converted = System.Convert.FromBase64String(statsBase64);
                        }
                    }
                }

                //  LoadGenerator -> Conductor : DONE robotnr stats retval

                var rmsg = ResponseMsg(robotnr, success, statsBase64, retval, scenariotracker.lgname);

                return await SendRobotResponse(scenariotracker, robotnr, rmsg);
            }
            finally
            {
                Interlocked.Decrement(ref scenariotracker.robotcount);
            }
        }

        private static string ResponseMsg(int robotnr, bool success, string statsBase64, string retval, string lgname)
        {
            var reply = JObject.FromObject(new
            {
                type = success ? "DONE" : "EXCEPTION",
                robotnr = robotnr.ToString(),
                lgname = lgname,
                stats = statsBase64,
                retval = retval
            });
            return reply.ToString();
        }

        


        async Task<string> SendRobotResponse(ScenarioTracker scenariotracker, int robotnr, string rmsg = null)
        {
            Action<string> robottracer = (string msg) =>
                   RobotTrace(scenariotracker, robotnr, msg);

            if (rmsg != null) // first time we send this
            {
                var delay = (int)(robotnr * (rmsg.Length / 500d));

                await Task.Delay(delay);

                //robottracer("Delayed " + delay);
            }
            else // resuming the old task
            {
                var prevtask = scenariotracker.robottasks[robotnr];
                if (prevtask != null)
                {
                    robottracer("Resume Started");
                    rmsg = await prevtask;
                    robottracer("Resume Done");
                }
                else
                {
                    rmsg = "Cannot resume: no such task";
                    robottracer(rmsg);
                }
            }

            try
            {
                lock (responsequeuelock)
                {
                    responsequeue.Add(rmsg);
                }

                await sendworker.NotifyAndWait();

                //robottracer("Sent");

                 //for debugging, simulate a failure
                //if (!hit && robotnr == 40)
               // {
                // hit = true;
                 //   ws.Abort();
               // }

            }
            catch (Exception e)
            {
                robottracer(string.Format("could not send result to conductor because of exception {0}", e));
            }

            return rmsg;
        }

     //   private static bool hit;

        public async Task RunAsync(CancellationToken cancellationToken)
        {
         
            while (!cancellationToken.IsCancellationRequested)
            {

                var uri = new Uri("ws://" + Config.GetConductor() + "/api/robots");

                ws = new ClientWebSocket();

                ws_cancel = new CancellationTokenSource();
                sendworker = new BatchWorkerFromDelegate(() => SendQueuedResponses());
                responsequeue = new List<string>();
                responsequeuelock = new Object();

                {

                    try
                    {
                        LgTrace(string.Format("Connecting to {0}...", uri));

                        try
                        {
                            await ws.ConnectAsync(uri, cancellationToken);
                        }
                        catch (Exception)
                        {
                            LgTrace("Could not connect to " + uri);
                        }

                        if (ws.State == WebSocketState.Open)
                        {
                            LgTrace("Connected.");

                            //  LoadGenerator ->  Conductor   :  READY 
                            JObject message = JObject.FromObject(new
                            {
                                type = "READY",
                                loadgenerator = lgid,
                                datacenter = Config.GetDataCenter()
                            });

                            lock (responsequeuelock)
                            {
                                responsequeue.Add(message.ToString());
                            }
                            await sendworker.NotifyAndWait();

                            LgTrace(string.Format("Sent {0}", message));
                        }

                        // receive loop
                        while (ws.State == WebSocketState.Open || ws.State == WebSocketState.CloseSent)
                        {
                            WebSocketReceiveResult receiveResult = null;

                            int bufsize = receiveBuffer.Length;
                            try
                            {
                                receiveResult = await ws.ReceiveAsync(new ArraySegment<byte>(receiveBuffer), cancellationToken);
                            }
                            catch (Exception ex)
                            {
                                LgTrace("Connection to conductor possibly timedout. Trying again. Error: " + ex.Message);
                                continue;
                            }

                            if (receiveResult.MessageType == WebSocketMessageType.Close)
                            {
                                LgTrace(string.Format("Received Close (StatusDescription={0})", ws.CloseStatusDescription));

                                await ws.CloseAsync(WebSocketCloseStatus.NormalClosure, "close ack", ws_cancel.Token);

                                LgTrace("Websocket closed.");
                            }
                            else if (receiveResult.MessageType != WebSocketMessageType.Text)
                            {
                                LgTrace("Received wrong message type.");

                                await ws.CloseAsync(WebSocketCloseStatus.InvalidMessageType, "Cannot accept binary frame", ws_cancel.Token);

                                LgTrace("Websocket closed.");
                            }
                            else
                            {
                                int count = receiveResult.Count;

                                while (receiveResult.EndOfMessage == false)
                                {
                                    if (count >= bufsize)
                                    {
                                        // enlarge buffer
                                        bufsize = bufsize * 2;
                                        var newbuf = new byte[bufsize * 2];
                                        receiveBuffer.CopyTo(newbuf, 0);
                                        receiveBuffer = newbuf;
                                    }

                                    receiveResult = await ws.ReceiveAsync(new ArraySegment<byte>(receiveBuffer, count, bufsize - count), ws_cancel.Token);

                                    if (receiveResult.MessageType != WebSocketMessageType.Text)
                                        await ws.CloseAsync(WebSocketCloseStatus.InvalidMessageType, "expected text frame", ws_cancel.Token);

                                    count += receiveResult.Count;
                                }

                                var content = Encoding.UTF8.GetString(receiveBuffer, 0, count);

                                //LgTrace("Received " + content);

                                JObject testdata = JObject.Parse(content);
                                var msgtype = (string)testdata["type"];

                                //  Conductor -> LoadGenerator : SCENARIO 
                                if (msgtype == "SCENARIO")
                                {
                                    if (currentscenario != null)
                                        await currentscenario.shutdown();

                                    currentscenario = await StartScenarioAsync(testdata);

                                    ScenarioTrace(currentscenario, "Started Scenario on " + DateTime.UtcNow);
                                }

                                //  Conductor -> LoadGenerator : START 
                                else if (msgtype == "START")
                                {
                                    int robotnr = int.Parse((string)testdata["robotnr"]);
                                    currentscenario.robottasks[robotnr] = StartRobotAsync(currentscenario, testdata, ws, ws_cancel.Token);

                                }

                                else if (msgtype == "RESUME")
                                {
                                    var testname = (string)testdata["testname"];
                                    var scenarioname = (string)testdata["scenarioname"];
                                    var robotnrsstring = (string)testdata["robotnrs"];
                                    var robotnrs = robotnrsstring.Split(',').Select(s => int.Parse(s));

                                    if (currentscenario != null &&
                                        testname == currentscenario.testname &&
                                        scenarioname == currentscenario.scenarioname)

                                        foreach (var x in robotnrs)
                                            currentscenario.robottasks[x] = SendRobotResponse(currentscenario, x);

                                    else
                                        foreach (var x in robotnrs)
                                        {
                                            var rmsg = ResponseMsg(x, false, "", "robot has disappeared", currentscenario.lgname);
                                            lock (responsequeuelock)
                                            {
                                                responsequeue.Add(rmsg);
                                            }
                                            await sendworker.NotifyAndWait();
                                        }
                                }


                                else
                                {
                                    LgTrace("unknown message from conductor");
                                }

                            }
                        }
                    }
                    catch (Exception e) //todo: use this handler only for websocket level exception. "server exceptions should be handled within the loop and the connection should be reused.
                    {
                        Trace.TraceInformation(string.Format("Exception caught: {0}", e));

                        // send exception to conductor if WS is open
                        LgTrace(string.Format("Exception in load generator: {0}", e));

                    }
                    finally
                    {

                        ws_cancel.Cancel();

                        // close websocket if it is still open
                        try
                        {
                            if (ws.State == WebSocketState.Open)
                                Task.WaitAny(
                                    ws.CloseOutputAsync(WebSocketCloseStatus.NormalClosure, "", cancellationToken),
                                    Task.Delay(10000));
                        }
                        catch (Exception e)
                        {
                            LgTrace(string.Format("Exception caught while closing websocket: {0}", e));
                        }

                        if (ws.State != WebSocketState.Closed && ws.State != WebSocketState.Aborted)
                        {
                            LgTrace(string.Format("Waiting for websocket to close"));

                            System.Threading.Thread.Sleep(10000);

                            // abort websocket if it is still open
                            if (ws.State != WebSocketState.Closed && ws.State != WebSocketState.Aborted)
                            {
                                LgTrace(string.Format("Aborting websocket"));
                                try
                                {
                                    ws.Abort();
                                }
                                catch (Exception e)
                                {
                                    LgTrace(string.Format("Exception caught while aborting websocket: {0}", e));
                                }
                            }
                        }
                    }

                    LgTrace(string.Format("Retrying Websocket in 10 sec"));

                    await Task.Delay(10000); // retry in 10 sec
                
                }

          }

        }
    }
}
