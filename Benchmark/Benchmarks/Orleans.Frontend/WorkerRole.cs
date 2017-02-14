using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Threading;
using Microsoft.WindowsAzure;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.ServiceRuntime;
using System.ServiceModel;
using System.ServiceModel.Description;
using System.Threading.Tasks;
using System.IO;
using Orleans;
using Orleans.Runtime.Host;
using Orleans.Benchmarks.Common;
using Orleans.Runtime;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.Extensibility;

namespace Orleans.Benchmarks.Orleans.Frontend
{
    public class WorkerRole : RoleEntryPoint
    {

        
        public override void Run()
        {

            diag("Portal: Starting Server");

            StartServices();

            diag("Portal: Running");

            var conductor = Config.GetConductor();

            var benchmarklist = new BenchmarkList();


            if (!string.IsNullOrEmpty(conductor))
            {
                // run the configured number of load generators on this front end

                var num_lgs = 1;

                var tasks = new List<Task>();

                for (int i = 0; i < num_lgs; i++)
                {
                    var lgid = Config.GetDataCenter() + "-" + RoleEnvironment.CurrentRoleInstance.Id + "-" + i;

                    var lg = new Common.LoadGenerator(lgid, benchmarklist.ByName);

                    tasks.Add(lg.RunAsync(new CancellationTokenSource().Token));
                }

                Task.WhenAll(tasks).Wait();
            }
            else 
            {
                // don't exit, otherwise the role goes down
               while (true)
               {
                   Thread.Sleep(10000);

                   //CheckHealth();

               }
            }
        }

        public static string GetAlphaHash(int seed, int length)
        {
            System.Text.StringBuilder b = new System.Text.StringBuilder();
            var random = new Random(seed);
            for (int i = 0; i < length; i++)
                b.Append((char)((int)'a' + random.Next(26)));
            return b.ToString();
        }

             /*
       public void CheckHealth()
        {
            if (currentserver == null)
                return;

            try
            {
                var problems = currentserver.IsDown();

                if (problems != null)
                {
                    try
                    {
                        var server = currentserver;
                        currentserver = null;
                        diag("##### Frontend: Problems Detected on Server " + server.GetIdentity() + ": " + problems);
                        diag("Frontend: Restarting " + server.GetIdentity());
                        server.Stop("Frontend: Restarting Server", true);
                        diag("Portal: Restarting " + server.GetIdentity() + " in 10 seconds");
                        Thread.Sleep(10000);
                        this.StartNewServer();
                    }
                    catch (Exception e)
                    {
                        diag("Portal: Failed to restart (Exception: " + e.Message + "). Requesting role recycle.");
                        RoleEnvironment.RequestRecycle();
                    }
                }
            }
            catch (Exception e)
            {
                diag("Portal: exception in health check: " + e);
            }

        }
             * */

        public override void OnStop()
        {
            diag("Portal: Stopping Frontend Server");

            if (currentserver != null)
            {
                var server = currentserver;
                currentserver = null;
                server.Stop();
            }

            diag("Portal: Stopped");

            base.OnStop();
        }

        /// <summary>
        /// low-level trace stuff
        /// </summary>
        /// <param name="s"></param>
        public void tracer(string s)
        {
            telemetryClient.TrackTrace(s, Microsoft.ApplicationInsights.DataContracts.SeverityLevel.Information);
            if (!runningincloud)
                Trace.WriteLine(s);
        }

        /// <summary>
        /// diagnostic information
        /// </summary>
        /// <param name="s"></param>
        public void diag(string s)
        {
            Trace.WriteLine(s);
            telemetryClient.TrackTrace(s, Microsoft.ApplicationInsights.DataContracts.SeverityLevel.Information);
            // may want to add this to some more visible log
        }


        public override bool OnStart()
        {
            TelemetryConfiguration.Active.InstrumentationKey = RoleEnvironment.GetConfigurationSettingValue("APPINSIGHTS_INSTRUMENTATIONKEY");
            telemetryClient = new TelemetryClient();
            TelemetryConfiguration.Active.DisableTelemetry = true;

            diag("Portal: WorkerRole.OnStart Called");

            ServicePointManager.DefaultConnectionLimit = 200;
            ServicePointManager.UseNagleAlgorithm = false;

            // get info
            deployment = RoleEnvironment.DeploymentId;
            instance = RoleEnvironment.CurrentRoleInstance.Id;

            // check if we are running in cloud or in simulator
            runningincloud = Common.Config.InCloud();

            return base.OnStart();
        }

        internal TelemetryClient telemetryClient;
        internal string deployment;
        internal string instance;
        internal bool runningincloud;
 

        internal FrontEndServer currentserver;

        public void StartServices()
        {
            try
            {
                // start orleans client
                diag("Portal: Starting Orleans Client");
                if (!AzureClient.IsInitialized)
                {
                    FileInfo clientConfigFile = AzureConfigUtils.ClientConfigFileLocation;
                    if (!clientConfigFile.Exists)
                    {
                        throw new FileNotFoundException(string.Format("Cannot find Orleans client config file for initialization at {0}", clientConfigFile.FullName), clientConfigFile.FullName);
                    }
                    AzureClient.Initialize(clientConfigFile);
                }
                diag("Portal: Orleans Client Started.");

                // create benchmark list
                var benchmarklist = new BenchmarkList();

                // start service endpoint
                var endpointdescriptor = RoleEnvironment.CurrentRoleInstance.InstanceEndpoints["frontend"];
                var securehttp = (endpointdescriptor.Protocol == "https");
                var port = endpointdescriptor.IPEndpoint.Port.ToString();
                var endpoint = endpointdescriptor.Protocol + "://+:" + port + "/";
                diag("Portal: Launching " + (runningincloud ? "deployed " : "local") + " service at " + endpoint);
                currentserver = new FrontEndServer(
                              RoleEnvironment.DeploymentId,
                              runningincloud,
                              securehttp,
                              this.tracer,
                              this.diag
                              );
                currentserver.Start(endpoint, (a,b,c,d)=>benchmarklist.ParseRequest(a,b,c,d));
                diag("Portal: Service started at endpoint: " + endpoint);

                diag("Portal: Startup complete");
            }
            catch (Exception e)
            {
                diag("Portal: failed to start: " + e.ToString());
            }
        }
    }
}

