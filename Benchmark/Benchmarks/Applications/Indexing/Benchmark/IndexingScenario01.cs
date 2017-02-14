using Orleans.Benchmarks.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using Newtonsoft.Json;
using System.Diagnostics;
using Orleans;
using Newtonsoft.Json.Linq;

namespace Orleans.Benchmarks.Indexing.Scenario01
{
    class IndexingScenario01 : IScenario
    {
        public class Params
        {
            public int numRobots { get; set; }

            public int runTimeSecs { get; set; }

            public int indexCount { get; set; }
            public int grainCount { get; set; }
            public int distinctKeyCount { get; set; }

            public int randomseed { get; set; }
            public int randomseedoffset { get; set; }

            public string scenarioType { get; set; }

            // needed to not overwhelm conductor with results (leads to stack overflow)
            // also needed to not influence request performance by sending back results
            public int sendbackdelay { get; set; }

            // the grain to use could be one of the parameters (see SizeScenario for reference)
        }

        public Params parameters;

        public IndexingScenario01(int numRobots, int runTimeSecs, int indexCount)
        {
            throw new Exception("this should never be used... parameters are not set");
        }

        public IndexingScenario01(string json, int seed)
        {
            parameters = JsonConvert.DeserializeObject<Params>(json);
            fixParameters(seed);

            // each robot initializes at least 1000 grains... but more if it has to
            initsPerRobot = Math.Max(1000, (int)Math.Ceiling((float)parameters.grainCount / parameters.numRobots));
            initRobotCount = (int)Math.Ceiling((float)parameters.grainCount / initsPerRobot);
            // possibly randomize some parameters using the provided seed
        }

        private void fixParameters(int seed)
        {
            // randomize if no seed explicitly specified
            if (parameters.randomseed == 0)
            {
                parameters.randomseed = new Random(seed + parameters.randomseedoffset).Next();
            }

            // be conservative if there is no specified sendbackdelay
            if (parameters.sendbackdelay == 0)
            {
                parameters.sendbackdelay = 8000;
            }

            if (parameters.runTimeSecs == 0)
            {
                parameters.runTimeSecs = 60;
            }

            if (parameters.distinctKeyCount == 0)
            {
                parameters.distinctKeyCount = 1;
            }

            if (parameters.grainCount == 0)
            {
                parameters.grainCount = 1;
            }
        }

        public string JsonParameters
        {
            get
            {
                return JsonConvert.SerializeObject(parameters);
            }
        }


        public string Name { get { return String.Format("IndexingScenario01_robots={0}_runtime={1}_grainCount={2}_distinctKeyCount={3}_indexCount={4}", parameters.numRobots, parameters.runTimeSecs, parameters.grainCount, parameters.distinctKeyCount, parameters.indexCount); } }
        public int NumRobots { get { return parameters.numRobots; } }

        private int initsPerRobot;
        private int initRobotCount;

        public async Task<string> ConductorScript(IConductorContext context, CancellationToken token)
        {
            context.Trace("start conductor script");
            JObject results = null;

            // init first
            // have one robot activate at least 1000 grains
            var initRobotTasks = new Task<string>[initRobotCount];
            for (int i = 0; i < initRobotTasks.Length; i++)
            {
                initRobotTasks[i] = context.RunRobot(i, "init");
            }

            await Task.WhenAll(initRobotTasks);
            for (int i = 0; i < initRobotTasks.Length; i++)
            {
                if (initRobotTasks[i].Result.StartsWith("EXCEPTION"))
                {
                    throw new Exception("Exception occured in robot: " + initRobotTasks[i].Result.Substring("EXCEPTION".Length));
                }
            }
            context.Trace("init done");

            // run
            var robotTasks = new Task<string>[parameters.numRobots];

            // start each robot
            for (int i = 0; i < robotTasks.Length; i++)
            {
                robotTasks[i] = context.RunRobot(i, "run");
            }
            context.Trace("robots started... waiting");

            // wait for all robots
            await Task.WhenAll(robotTasks);
            context.Trace("all robots done");

            // check results
            var globalLatencies = new List<float>();
            double latencySum = 0;
            double throughput = 0;
            int totalOps = 0;
            int totalTimeouts = 0;

            for (int i = 0; i < robotTasks.Length; i++)
            {
                var json = robotTasks[i].Result;
                if (json.StartsWith("EXCEPTION"))
                {
                    throw new Exception("Exception occured in robot: " + json.Substring("EXCEPTION".Length));

                }
                RobotResult response;
                try
                {
                    response = JsonConvert.DeserializeObject<RobotResult>(json);
                }
                catch (Exception)
                {
                    throw new Exception("Invalid response by robot " + i + ": " + robotTasks[i].Result);
                }
                for (int j = 0; j < parameters.runTimeSecs; j++)
                {
                    if (j >= response.latencies.Count)
                    {
                        continue;
                    }
                    var x = response.latencies[j];
                    totalOps++;
                    globalLatencies.Add(x);
                    latencySum += x;
                }
                throughput += response.throughput;
                totalTimeouts += response.timeouts;

            }

            results = JsonConvert.DeserializeObject<JObject>(this.JsonParameters);

            results["total_ops"] = totalOps;
            results["latency_avg"] = string.Format("{0:F1}", latencySum / totalOps);
            results["throughput"] = string.Format("{0:D}", (long)throughput);
            results["timeouts"] = totalTimeouts;

            reportLatency(results, context, globalLatencies);

            return results.ToString();
            //context.setConductorScriptResult(results.ToString());
        }

        private void reportLatency(JObject results, IConductorContext context, List<float> latencies)
        {
            if (latencies.Count() > 0)
            {
                latencies.Sort();
                var avg = latencies.Average();
                var min = latencies[0];
                var max = latencies[latencies.Count - 1];
                var p50 = latencies[getNearestRankForPercentile(latencies.Count(), 50)];
                var p90 = latencies[getNearestRankForPercentile(latencies.Count(), 90)];
                var p95 = latencies[getNearestRankForPercentile(latencies.Count(), 95)];

                string dist = string.Format("{0}min:{1:F1}, 50th:{2:F1}, 90th:{3:F1}, 95th:{4:F1}, max:{5:F1}{6}", "{", min, p50, p90, p95, max, "}");

                results["latency_dist"] = dist;
            }
        }

        private int getNearestRankForPercentile(int size, float percentile)
        {
            if (percentile > 100)
            {
                percentile = 100;
            }
            else if (percentile < 0)
            {
                percentile = 0;
            }
            int rank = (int)(percentile * size / 100);
            if (rank > size)
            {
                rank = size;
            }
            return rank;
        }


        public Task<string> RobotScript(IRobotContext context, int robotnumber, string parameters)
        {
            // run different tasks depending on the parameters
            if (parameters.StartsWith("init"))
            {
                return RobotInit(context, robotnumber);
            }
            else if(parameters.StartsWith("run"))
            {
                return RobotMeasure(context, robotnumber);
            }
            // crude way to pass exception from robot (on frontend) to conductor
            return Task.FromResult<string>("EXCEPTION invalid robot parameter");
        }

        public async Task<string> RobotInit(IRobotContext context, int robotnumber)
        {
            Random random = new Random(robotnumber ^ parameters.randomseed);

            List<Task> initTasks = new List<Task>();

            for (int i = (robotnumber * initsPerRobot); i < Math.Min(((robotnumber + 1) * initsPerRobot), parameters.grainCount); i++)
            {
                switch (parameters.indexCount)
                {
                case 0: // no index - use IPlayerGrain
                    {
                        IPlayerGrain p1 = GrainClient.GrainFactory.GetGrain<IPlayerGrain>(i, "Orleans.Benchmarks.Indexing.Scenario01.PlayerGrain");
                        initTasks.Add( p1.SetLocation(getRandomLocation(random)) );
                        //await p1.LogSilo("mode2");
                        break;
                    }
                case 1: // 1 index - use IIndexedPlayer1Grain
                    {
                        IIndexedPlayer1Grain p1 = GrainClient.GrainFactory.GetGrain<IIndexedPlayer1Grain>(i, "Orleans.Benchmarks.Indexing.Scenario01.IndexedPlayer1Grain");
                        initTasks.Add( p1.SetLocation(getRandomLocation(random)) );
                        //await p1.LogSilo("mode1");
                        break;
                    }
                case 2: // 2 indexes - use IIndexedPlayer2Grain
                    {
                        IIndexedPlayer2Grain p1 = GrainClient.GrainFactory.GetGrain<IIndexedPlayer2Grain>(i, "Orleans.Benchmarks.Indexing.Scenario01.IndexedPlayer2Grain");
                        initTasks.Add( p1.SetTwoLocations(getRandomLocation(random), getRandomLocation(random)) );
                        //await p1.LogSilo("mode1");
                        break;
                    }
                case 3: // 3 indexes - use IIndexedPlayer3Grain
                    {
                        IIndexedPlayer3Grain p1 = GrainClient.GrainFactory.GetGrain<IIndexedPlayer3Grain>(i, "Orleans.Benchmarks.Indexing.Scenario01.IndexedPlayer3Grain");
                        initTasks.Add( p1.SetThreeLocations(getRandomLocation(random), getRandomLocation(random), getRandomLocation(random)) );
                        //await p1.LogSilo("mode1");
                        break;
                    }
                case 4: // 4 indexes - use IIndexedPlayer4Grain
                    {
                        IIndexedPlayer4Grain p1 = GrainClient.GrainFactory.GetGrain<IIndexedPlayer4Grain>(i, "Orleans.Benchmarks.Indexing.Scenario01.IndexedPlayer4Grain");
                        initTasks.Add( p1.SetFourLocations(getRandomLocation(random), getRandomLocation(random), getRandomLocation(random), getRandomLocation(random)) );
                        //await p1.LogSilo("mode1");
                        break;
                    }
                default:
                    {
                        // crude way to pass exception from robot (on frontend) to conductor
                        return "EXCEPTION more than 4 indexes are not implemented in this scenario";
                    }
                }
            }

            await Task.WhenAll(initTasks);
            await Task.Delay(1000);
            return "done";
        }

        public async Task<string> RobotMeasure(IRobotContext context, int robotnumber)
        {
            var result = new RobotResult()
            {
                latencies = new List<float>()
            };

            var random = new Random(robotnumber ^ parameters.randomseed);

            // delay each robot by a random delay so we don't have unrealistic request rate spike
            await Task.Delay(random.Next(1000));

            // Stats
            var numRequests = 0;
            var numTimeouts = 0;

            var robotTimer = new Stopwatch();
            var requestTimer = new Stopwatch();

            robotTimer.Start();
            while (numRequests < parameters.runTimeSecs && robotTimer.ElapsedMilliseconds / 1000 < parameters.runTimeSecs)
            {

                // target a random grain
                int nextGrain = 0;

                try
                {
                    nextGrain = random.Next(parameters.grainCount);

                    switch (parameters.indexCount)
                    {
                    case 0:
                        {
                            IPlayerGrain p1 = GrainClient.GrainFactory.GetGrain<IPlayerGrain>(nextGrain, "Orleans.Benchmarks.Indexing.Scenario01.PlayerGrain");
                            requestTimer.Start();
                            await p1.SetLocation(getRandomLocation(random));
                            requestTimer.Stop();
                            break;
                        }
                    case 1:
                        {
                            IIndexedPlayer1Grain p1 = GrainClient.GrainFactory.GetGrain<IIndexedPlayer1Grain>(nextGrain, "Orleans.Benchmarks.Indexing.Scenario01.IndexedPlayer1Grain");
                            requestTimer.Start();
                            await p1.SetLocation(getRandomLocation(random));
                            requestTimer.Stop();
                            break;
                        }
                    case 2:
                        {
                            IIndexedPlayer2Grain p1 = GrainClient.GrainFactory.GetGrain<IIndexedPlayer2Grain>(nextGrain, "Orleans.Benchmarks.Indexing.Scenario01.IndexedPlayer2Grain");
                            requestTimer.Start();
                            await p1.SetTwoLocations(getRandomLocation(random), getRandomLocation(random));
                            requestTimer.Stop();
                            break;
                        }
                    case 3:
                        {
                            IIndexedPlayer3Grain p1 = GrainClient.GrainFactory.GetGrain<IIndexedPlayer3Grain>(nextGrain, "Orleans.Benchmarks.Indexing.Scenario01.IndexedPlayer3Grain");
                            requestTimer.Start();
                            await p1.SetThreeLocations(getRandomLocation(random), getRandomLocation(random), getRandomLocation(random));
                            requestTimer.Stop();
                            break;
                        }
                    case 4:
                        {
                            IIndexedPlayer4Grain p1 = GrainClient.GrainFactory.GetGrain<IIndexedPlayer4Grain>(nextGrain, "Orleans.Benchmarks.Indexing.Scenario01.IndexedPlayer4Grain");
                            requestTimer.Start();
                            await p1.SetFourLocations(getRandomLocation(random), getRandomLocation(random), getRandomLocation(random), getRandomLocation(random));
                            requestTimer.Stop();
                            break;
                        }
                    default:
                        {
                            // crude way to pass exception from robot (on frontend) to conductor
                            return "EXCEPTION more than 4 indexes are not implemented in this scenario";
                        }
                    }


                result.latencies.Add((float)requestTimer.Elapsed.TotalMilliseconds);
                } 
                catch (Exception e)
                {
                    if (e is TimeoutException)
                    {
                        numTimeouts++;
                    }
                    else
                    {
                        context.Trace("Exception in request: " + e);
                        return ("EXCEPTION " + e);
                    }
                }
                requestTimer.Reset();
                numRequests++;

                if (numRequests < parameters.runTimeSecs)
                {
                    // rate control: do not issue more than 1 request per second per robot
                    var aheadofrate = (int)(numRequests * 1000 - robotTimer.ElapsedMilliseconds);
                    if (aheadofrate > 0)
                    {
                        // randomize to avoid convoying
                        aheadofrate = aheadofrate - random.Next(Math.Min(aheadofrate - 1, 100));
                        await Task.Delay(aheadofrate);
                    }
                }


            }
            robotTimer.Stop();

            // delay sending results back to avoid interference with other robots on same load generator
            //await Task.Delay(parameters.sendbackdelay + (parameters.readers + parameters.writers) / 3);
            await Task.Delay(parameters.sendbackdelay + random.Next(parameters.sendbackdelay));

            // record actual runtime in [ms]
            var elapsed = Math.Max(robotTimer.ElapsedMilliseconds, 1000 * parameters.runTimeSecs);

            // record throughput in [req/s]
            result.throughput = (result.latencies.Count * 1000d) / elapsed;
            result.timeouts = numTimeouts;

            return JsonConvert.SerializeObject(result);
        }

        // deprecated
        public string RobotServiceEndpoint(int workernumber)
        {
            throw new NotImplementedException();
        }

        private string getRandomLocation(Random rand)
        {
            return "location_" + rand.Next(parameters.distinctKeyCount);
        }
    }
}
