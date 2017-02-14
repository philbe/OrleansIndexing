using System;
using Orleans.Benchmarks.Common;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.Linq;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Auth;
using Microsoft.WindowsAzure.Storage.Table;
using Conductor.Webrole;
using Newtonsoft.Json;
using System.Diagnostics;
using Microsoft.ApplicationInsights;

namespace Conductor.Webrole
{
    public class Conductor : IConductorContext
    {

        public static Conductor Instance { get { return _instance ?? (_instance = new Conductor()); } }

        private static Conductor _instance;

        private Conductor() { }


        public Orleans.Benchmarks.Console console;

        public CancellationTokenSource canceller;

        private string testname;
        private string scenarioname;
        private int scenarioindex;

        private string conductorScriptResult;

        private StringBuilder consolelog = new StringBuilder();

        public Dictionary<string, LoadGeneratorInfo> LoadGenerators = new Dictionary<string, LoadGeneratorInfo>();

        public string[] datacenters;

        IBenchmark benchmark;
        IReadOnlyList<Orleans.Benchmarks.Console.ScenarioInfo> scenarios;
        public List<RobotInfo> robots;

        private string CONDUCTORLOGNAME = "conductorlog";

        public class LoadGeneratorInfo
        {
            public string instance;
            public WebSocket ws;
            public string datacenter;
            public string lgname;
            public Task lastsendtask;
        }

        public class RobotInfo
        {
            public LoadGeneratorInfo loadgenerator;
            public TaskCompletionSource<string> promise;
            public Dictionary<string, LatencyDistribution> stats;
        }


        public CommandHub Hub;

        public void Broadcast(string a, string b)
        {
            if (!string.IsNullOrEmpty(a))
                System.Diagnostics.Trace.TraceInformation("{0} {1}", a, b);
            else
                System.Diagnostics.Trace.TraceInformation(b);

            if (!string.IsNullOrEmpty(scenarioname))
            {
                var formattedtime = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss.fff");
                consolelog.Append(formattedtime);
                consolelog.Append(' ');
                if (!string.IsNullOrEmpty(a))
                {
                    consolelog.Append(a);
                    consolelog.Append(" ");
                }
                consolelog.AppendLine(b);
            }


            if (Hub != null)
                try
                {
                    Hub.Clients.All.addNewMessageToPage(a, b);
                }
                catch (Exception)
                {
                }
        }

        private Task SendAsync(LoadGeneratorInfo lg, string message)
        {
            Func<Task, Task> queuetask = async (Task prev) =>
                {
                    await prev;
                    //consolelog.AppendLine(string.Format("SendAsync {0} ws{1} Start", lg.lgname, lg.ws.GetHashCode()));
                    try
                     {
                         var buffer = new ArraySegment<byte>(Encoding.UTF8.GetBytes(message.ToString()));
                         await lg.ws.SendAsync(buffer, WebSocketMessageType.Text, true, CancellationToken.None);
                        //consolelog.AppendLine(string.Format("SendAsync {0} ws{1} Done", lg.lgname, lg.ws.GetHashCode()));
                    }
                     catch (Exception e)
                     {
                         consolelog.AppendLine(string.Format("SendAsync {0} ws{1} Failed {2}", lg.lgname, lg.ws.GetHashCode(), e));
                         System.Diagnostics.Trace.TraceInformation(string.Format("SendAsync {0} ws{1} Failed {2}", lg.lgname, lg.ws.GetHashCode(), e));
                         TelemetryClient telemetryClient = new TelemetryClient();
                         telemetryClient.TrackException(e);
                         throw e;
                     }
                };

            lg.lastsendtask = queuetask(lg.lastsendtask);

            return lg.lastsendtask;
        }

        //private void Run()
        //{
        //    Thread t = new Thread(RunItAll, 128 * 1024 * 1024);
        //    t.Name = "RunItAll";
        //    t.Start();
        //    t.Join();
        //}

        private void Run()
        {
            console = new Orleans.Benchmarks.Console(WriteLine, ReadLine);

            console.Welcome();

            canceller = new CancellationTokenSource();

            while (! canceller.IsCancellationRequested)
            {
                var testspec = console.SelectScenario();

                if (testspec == null)
                    break;

                polite_cancellation = false; // reset this if it was set in a prior test

                this.testname = testspec.testname;
                this.benchmark = testspec.benchmark;
                this.scenarios = testspec.scenarios;

                lock (LoadGenerators)
                {
                    if (LoadGenerators.Count == 0)
                    {
                        Broadcast("Failed", "cannot run scenario: no load generators");
                        continue;
                    }
                    foreach (var lg in LoadGenerators)
                    {
                        bool ws_is_ok = false;
                        try
                        {
                            ws_is_ok = lg.Value.ws.State == WebSocketState.Open;
                        }
                        catch { }
                        if (!ws_is_ok)
                        {
                            Broadcast("Failed", "cannot run scenario: load generator " + lg.Key + " is disconnected");
                            continue;
                        }
                    }
                }

                var testdirectory = TestResultStorage.GetOrCreateTestDirectory(testname).Result;
                var resultfiledirectory = TestResultStorage.GetOrCreateResultFileDirectory(testname).Result;

                CloudTableClient tableClient = Config.getBenchmarkStorage().CreateCloudTableClient();

                //var resultstable = new ResultsTable();

                Broadcast("Start Test", this.testname);
                var teststopwatch = new Stopwatch();
                teststopwatch.Start();

                int scenariocount = 0;
                var discardedScenarios = new HashSet<string>();
                Broadcast("Started Scenarios", "...");
                foreach (var sinfo in scenarios)
                {
                    Broadcast("Working on Scenario", benchmark.Name + "." + sinfo.scenario.Name);

                    var scenarioJson = JsonConvert.DeserializeObject<JObject>(sinfo.scenario.JsonParameters);
                    scenarioJson["numRobots"] = 9999999;
                    var scenarioWithoutNumRobots = scenarioJson.ToString();
                    if(discardedScenarios.Contains(scenarioWithoutNumRobots))
                    {
                        Broadcast("Skipped Scenario", benchmark.Name + "." + sinfo.scenario.Name);
                        continue;
                    }

                    if (polite_cancellation)
                        break;

                    this.scenarioindex = scenariocount++;

                    if (this.scenarioindex < testspec.skip)
                        continue;

                    this.scenarioname = string.Format("{0:D3}-{1}", scenarioindex, sinfo.scenario.Name);
                    var scenariodirectory = testdirectory.GetDirectoryReference(scenarioname);

                    try
                    {
                        // construct a safe blob container n

                        this.robots = new List<RobotInfo>();

                        var tasks = new List<Task>();

                        lock (LoadGenerators)
                        {
                            // sort load generators into data centers
                            var lgs = new Dictionary<string, List<LoadGeneratorInfo>>();
                            foreach (var lg in LoadGenerators.Values)
                            {
                                List<LoadGeneratorInfo> list;
                                if (!lgs.TryGetValue(lg.datacenter, out list))
                                    lgs[lg.datacenter] = list = new List<LoadGeneratorInfo>();
                                var pos = lg.datacenter.IndexOfAny(":./-".ToCharArray());
                                var dcname = (pos == -1) ? lg.datacenter : lg.datacenter.Substring(0, pos);
                                lg.lgname = string.Format("{0}-{1:D3}", dcname, list.Count());
                                list.Add(lg);

                                

                                // send start msg for scenario
                                JObject message = JObject.FromObject(new
                                {
                                    type = "SCENARIO",
                                    testname = testname,
                                    lgname = lg.lgname,
                                    benchmarkname = benchmark.Name,
                                    scenarioname = scenarioname,
                                    scenarioparameters = sinfo.scenario.JsonParameters
                                });
                                tasks.Add(SendAsync(lg, message.ToString()));
                            }
                            
                            //determine datacenters to be used
                            if (testspec.datacenters != null)
                            {
                                datacenters = testspec.datacenters.ToArray();
                                for (int i = 0; i <  datacenters.Length; i++)  
                                if (! lgs.ContainsKey(datacenters[i]))
                                   throw new Exception("specified datacenter not present");

                                var dcs = sinfo.spec["selectdcs"];
                                if (dcs != null && dcs.HasValues)
                                    datacenters = dcs.Children().Select(i => datacenters[int.Parse(i.ToString())]).ToArray();

                            }
                            else
                                datacenters = lgs.Keys.ToArray();

                            // assign robots to load generators using nested round-robin
                            for (int i = 0; i < sinfo.scenario.NumRobots; i++)
                            {
                                var list = lgs[datacenters[i % datacenters.Length]];
                                var lg = list[(i / datacenters.Length) % list.Count()];
                                robots.Add(new RobotInfo() { loadgenerator = lg });
                            }
                        }

                        Task.WaitAll(tasks.ToArray(), canceller.Token);

                        if (canceller.IsCancellationRequested)
                            throw new Exception("Canceled");

                        var scenariotask = RunScenario(sinfo.scenario, canceller.Token);

                   
                        scenariotask.Wait(canceller.Token);

                        if (canceller.IsCancellationRequested)
                            throw new Exception("Canceled");

                        var result = scenariotask.Result;

                        // collect stats from all robots
                        var overallstats = new Dictionary<string, LatencyDistribution>();
                        foreach (var robot in robots)
                            if (robot.stats != null)
                                foreach (var kkvp in robot.stats)
                                {
                                    if (!overallstats.ContainsKey(kkvp.Key))
                                        overallstats.Add(kkvp.Key, new LatencyDistribution());
                                    overallstats[kkvp.Key].MergeDistribution(kkvp.Value);
                                }

                        Broadcast(
                            string.Format("Result ({0}/{1})", scenarioindex+1, scenarios.Count),
                            result + " " + Orleans.Benchmarks.Common.Util.PrintStats(overallstats)
                        );

                        if (result.StartsWith("{"))
                        {
                            // save json
                            var json = JsonConvert.DeserializeObject<JObject>(result);

                            if (double.Parse(json["latency_avg"].ToString()) > 1200)
                            {
                                discardedScenarios.Add(scenarioWithoutNumRobots);
                            }
                            json.Add("benchmark", benchmark.Name);
                            json.Add("testname", testname);
                            json.Add("scenarioname", scenarioname);
                            json.Add("scenarioindex", scenarioindex);
                            var id = (testname + scenarioname).GetHashCode().ToString("X");
                            json.Add("id", id);
                            json.Add("datacenters", new JArray(datacenters));
                            AddFieldsIfNotExist(json, sinfo.spec);                                 
                            var filename = string.Format("result-{0}.json", id);
                            { // save in tests directory
                                var resultblob = scenariodirectory.GetBlockBlobReference(filename);
                                resultblob.Properties.ContentType = "application/json";
                                resultblob.UploadText(json.ToString());
                            }
                            { // save in resultfile directory
                                var resultblob = resultfiledirectory.GetBlockBlobReference(filename);
                                resultblob.Properties.ContentType = "application/json";
                                resultblob.UploadText(json.ToString());
                            }
                        }

                        LatencyDistribution stats = null;
                        if (overallstats.Any())
                        {
                            stats = overallstats.First().Value;
                        }

                        //resultstable.RecordResult(testname, scenarioname, result, stats);

                        if (overallstats.Count > 0)
                            System.Console.WriteLine("Stats", Orleans.Benchmarks.Common.Util.PrintStats(overallstats));

                    }
                    catch (Exception e)
                    {
                        Broadcast("Error", "Caught Exception during scenario " + scenarioname + ":" + e + " " + e.StackTrace);
                    }

                    var blob = scenariodirectory.GetBlockBlobReference(CONDUCTORLOGNAME);
                    scenarioname = "";
                    robots = null;
                    blob.UploadText(consolelog.ToString());
                    consolelog.Clear();
                }

                teststopwatch.Stop();
                Broadcast("End Test", string.Format("{0} elapsed:{1}", this.testname, teststopwatch.Elapsed));
            }

            IEnumerable<WebSocket> connected;
            lock (LoadGenerators)
                connected = LoadGenerators.Values.Select(lgi => lgi.ws).ToList();

            LoadGenerators.Clear();

            foreach (var ws in connected)
                try
                {
                    ws.CloseOutputAsync(WebSocketCloseStatus.NormalClosure, "Console Shutting Down", CancellationToken.None);
                }
                catch (Exception )
                {
                }

            Broadcast("Console Terminated", "(reload page to start new one)");

            console = null;

        }
        public int NumRobots
        {
            get;
            set;
        }

        private void AddFieldsIfNotExist(JObject target, JObject source)
        {
            if (source == null)
                return;
            var keys = target.Children().Select(t => ((JProperty)t).Name).ToList();
            foreach (var t in source)
                if (!keys.Contains(t.Key))
                    target.Add(t.Key, source[t.Key]);
        }


        public async Task<string> RunRobot(int robotnumber, string parameters = "")
        {
            var robot = robots[robotnumber];

            Util.Assert(robot.promise == null);
            robot.promise = new TaskCompletionSource<string>();

            //var message = "START " + testname + " " + robotnumber + " " + parameters;
            JObject message = JObject.FromObject(new
            {
                type = "START",
                robotnr = robotnumber,
                args = parameters
            });

            //await SendAsync(robot.loadgenerator, message.ToString());
            SendAsync(robot.loadgenerator, message.ToString()).Wait();

            //if (robotnumber % 250 == 0)
            //{
            //    Broadcast("Robot sent start", robotnumber.ToString());
            //}
            return await robot.promise.Task;
        }

        public void OnRobotMessage(int robotnumber, string message, Dictionary<string, LatencyDistribution> stats)
        {
            var robot = robots[robotnumber];
            var promise = robot.promise;
            robot.promise = null;
            robot.stats = stats;

            // for debugging, simulate a failure
           // if (!hit && robotnumber == 40)
           // {
           //     hit = true;
           //     robot.loadgenerator.ws.Abort(); // simulate failure
           // }

            promise.SetResult(message);
        }

      //  private static bool hit;

        public void OnDisconnect(string instance, string message)
        {
            //  lock (LoadGenerators)
            // {
            //             if (LoadGenerators.Remove(instance))
            //          {
            Broadcast("Disconnected", instance + ": " + message);
            //if (robots != null)
            //    foreach (RobotInfo r in robots)
            //        if (r.loadgenerator.instance == instance && r.promise != null)
            //            r.promise.TrySetResult("ERROR: Lost Connection");
            //  }
            // }
        }

        public void OnConnect(string instance, string datacenter, WebSocket ws)
        {
            LoadGeneratorInfo current;

            lock (LoadGenerators)
            {
                if (LoadGenerators.TryGetValue(instance, out current))
                {
                    Broadcast("Reconnected", instance);
                    try
                    {
                        System.Diagnostics.Trace.TraceInformation("{0} Abort because superseded by {1}", current.ws, ws);
                        current.ws.Abort();
                    }
                    catch
                    {
                    }
                    current.ws = ws;
                }
                else {
                    LoadGenerators[instance] = new LoadGeneratorInfo()
                    {
                        instance = instance,
                        datacenter = datacenter,
                        ws = ws,
                        lastsendtask = Task.FromResult(0)
                    };
                    ShowGenerators();
                }
            }

            if (robots != null)
            {
                var danglingrobots = new List<string>();
                for (int i = 0; i < robots.Count(); i++)
                    if (robots[i].loadgenerator.instance == instance
                        && robots[i].promise != null
                        && !robots[i].promise.Task.IsCompleted)
                        danglingrobots.Add(i.ToString());
                JObject message = JObject.FromObject(new
                {
                    type = "RESUME",
                    testname = testname,
                    scenarioname = scenarioname,
                    robotnrs = string.Join(",", danglingrobots)
                });
                //System.Diagnostics.Trace.TraceInformation("{0} Sending {1}", ws, message);

                SendAsync(LoadGenerators[instance], message.ToString()).Wait();
            }


         
        }

        public void ShowGenerators()
        {
            lock (LoadGenerators)
            {
                if (LoadGenerators.Count == 0)
                    Broadcast("Connected Generators", "None");
                else
                    Broadcast("Connected Generators (" + LoadGenerators.Count.ToString() + ")", string.Join(" ", LoadGenerators.Keys));
            }
        }

        private async Task<string> RunScenario(IScenario scenario, CancellationToken token)
        {
            try
            {
                Broadcast("Start Scenario", benchmark.Name + "." + scenario.Name);
                return await scenario.ConductorScript(this, token);
            }
            catch (Exception e)
            {
                TelemetryClient telemetryClient = new TelemetryClient();
                telemetryClient.TrackException(e);
                return "ERROR: exception " + e.ToString();
            }
            finally
            {
                Broadcast("End Scenario", benchmark.Name + "." + scenario.Name);
            }
        }

        private void threadMethod(object param)
        {
            //foobar
            Trace("Testing thread");
        }

        public void WriteLine(string what)
        {
            Broadcast("", what);
        }

        public string ReadLine()
        {
            string command = null;

            lock (commands)
            {
                if (commands.Count > 0)
                    command = commands.Dequeue();
            }

            if (command == null)
            {
                //Broadcast("#>>>>", "Please enter a command");

                lock (commands)
                {
                    while (commands.Count == 0)
                        System.Threading.Monitor.Wait(commands);

                    command = commands.Dequeue();
                }
            }

            Broadcast("Entered", command);

            return command;
        }

        private Queue<string> commands = new Queue<string>();
        private bool polite_cancellation;

        public void Typed(string command)
        {

            if (console == null)
            {
                Thread thread = new Thread(new ThreadStart(Run));
                thread.Start();

                return;
            }

            if (!string.IsNullOrEmpty(command))
            {
                if (command == "kill")
                {
                    if (canceller != null)
                        canceller.Cancel();
                }
                else if (command == "cancel")
                {
                    polite_cancellation = true;
                }
                else
                {
                    // queue the command
                    lock (commands)
                    {
                        commands.Enqueue(command);
                        if (commands.Count == 1)
                            System.Threading.Monitor.PulseAll(commands);
                    }
                }
            }
        }

        public void Trace(string info)
        {
            Broadcast("", info);
        }


        public string[] DataCenters
        {
            get { return datacenters; }
        }
    }
}