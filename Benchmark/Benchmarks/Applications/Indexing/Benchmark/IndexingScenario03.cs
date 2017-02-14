using Orleans.Benchmarks.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Diagnostics;
using Orleans;

namespace Orleans.Benchmarks.Indexing.Scenario03
{
    class IndexingScenario03 : IScenario
    {
        public class Params
        {
            public int numRobots { get; set; }

            public int runTimeSecs { get; set; }

            public string indexType { get; set; } // possible values "NotIndexedNotPersisted", "NotIndexedPersisted", "IndexedNotPersisted", "IndexedPersisted"
            public int grainCount { get; set; }
            public int distinctKeyCount { get; set; }

            public int randomseed { get; set; }
            public int randomseedoffset { get; set; }

            public string scenarioType { get; set; }

            // needed to not overwhelm conductor with results (may lead to stack overflow)
            // also needed to not influence request performance by sending back results
            public int sendbackdelay { get; set; }

            // the grain to use could be one of the parameters (see SizeScenario for reference)
        }

        public Params parameters;

        public IndexingScenario03()
        {
            throw new Exception("this should never be used... parameters are not set");
        }

        public IndexingScenario03(string json, int seed)
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

            if (parameters.distinctKeyCount == 0)
            {
                parameters.distinctKeyCount = 1;
            }

            if (parameters.grainCount == 0)
            {
                parameters.grainCount = 1;
            }

            if (String.IsNullOrWhiteSpace(parameters.indexType))
            {
                parameters.indexType = "NotIndexedNotPersisted";
            }
        }

        public string JsonParameters
        {
            get
            {
                return JsonConvert.SerializeObject(parameters);
            }
        }


        public string Name { get { return String.Format("IndexingScenario03_robots={0}_runtime={1}_grainCount={2}_distinctKeyCount={3}_indexType={4}", parameters.numRobots, parameters.runTimeSecs, parameters.grainCount, parameters.distinctKeyCount, parameters.indexType); } }
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
            for (int i = 0; i < initRobotTasks.Length; i++ )
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
            int numReattempts = 0;
            int totalTimeouts = 0;

            for (int i = 0; i < robotTasks.Length; i++)
            {
                var json = robotTasks[i].Result;
                if (json.StartsWith("EXCEPTION"))
                {
                    throw new Exception("Exception occured in robot: " +json.Substring("EXCEPTION".Length));
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
                numReattempts += response.reattempts;
                totalTimeouts += response.timeouts;
            }

            results = JsonConvert.DeserializeObject<JObject>(this.JsonParameters);

            results["total_ops"] = totalOps;
            results["latency_avg"] = string.Format("{0:F1}", latencySum / totalOps);
            results["throughput"] = string.Format("{0:D}", (long)throughput);
            results["reattempts"] = numReattempts;
            results["timeouts"] = totalTimeouts;

            reportLatency(results, context, globalLatencies);

            return results.ToString();
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
            else if (parameters.StartsWith("run"))
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
                if (parameters.indexType.Equals("NotIndexedNotPersisted", StringComparison.OrdinalIgnoreCase))
                {
                    IPlayerGrain p1 = GrainClient.GrainFactory.GetGrain<IPlayerGrain>(i, "Orleans.Benchmarks.Indexing.Scenario03.PlayerNotPersistedGrain");
                    initTasks.Add( p1.SetLocation(getRandomLocation(random)) );
                }
                else if (parameters.indexType.Equals("NotIndexedPersisted", StringComparison.OrdinalIgnoreCase))
                {
                    IPlayerGrain p1 = GrainClient.GrainFactory.GetGrain<IPlayerGrain>(i, "Orleans.Benchmarks.Indexing.Scenario03.PlayerPersistedGrain");
                    initTasks.Add( p1.SetLocation(getRandomLocation(random)) );
                }
                else if (parameters.indexType.Equals("IndexedNotPersisted", StringComparison.OrdinalIgnoreCase))
                {
                    IIndexedPlayerGrain p1 = GrainClient.GrainFactory.GetGrain<IIndexedPlayerGrain>(i, "Orleans.Benchmarks.Indexing.Scenario03.IndexedPlayerNotPersistedGrain");
                    initTasks.Add( p1.SetLocation(getRandomLocation(random)) );
                }
                else if (parameters.indexType.Equals("IndexedPersisted", StringComparison.OrdinalIgnoreCase))
                {
                    IIndexedPlayerGrain p1 = GrainClient.GrainFactory.GetGrain<IIndexedPlayerGrain>(i, "Orleans.Benchmarks.Indexing.Scenario03.IndexedPlayerPersistedGrain");
                    initTasks.Add( p1.SetLocation(getRandomLocation(random)) );
                }
                else
                {
                    // crude way to pass exception from robot (on frontend) to conductor
                    return "EXCEPTION wrong index type specified. possible values are default, persilo, and perkey";
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
            var numReattempts = 0;
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

                    if (parameters.indexType.Equals("NotIndexedNotPersisted", StringComparison.OrdinalIgnoreCase))
                    {
                        IPlayerGrain p1 = GrainClient.GrainFactory.GetGrain<IPlayerGrain>(nextGrain, "Orleans.Benchmarks.Indexing.Scenario03.PlayerNotPersistedGrain");
                        requestTimer.Start();
                        while (!(await p1.SetLocation(getRandomLocation(random)))) {
                            numReattempts++; // this should never happen... it's just here to keep the code paths equal for all cases
                        }
                        requestTimer.Stop();
                    }
                    else if (parameters.indexType.Equals("NotIndexedPersisted", StringComparison.OrdinalIgnoreCase))
                    {
                        IPlayerGrain p1 = GrainClient.GrainFactory.GetGrain<IPlayerGrain>(nextGrain, "Orleans.Benchmarks.Indexing.Scenario03.PlayerPersistedGrain");
                        requestTimer.Start();
                        while (!(await p1.SetLocation(getRandomLocation(random))))
                        {
                            numReattempts++; // means that AzureTable choked on etag because of concurrent update -> retry
                        }
                        requestTimer.Stop();
                    }
                    else if (parameters.indexType.Equals("IndexedNotPersisted", StringComparison.OrdinalIgnoreCase))
                    {
                        IIndexedPlayerGrain p1 = GrainClient.GrainFactory.GetGrain<IIndexedPlayerGrain>(nextGrain, "Orleans.Benchmarks.Indexing.Scenario03.IndexedPlayerNotPersistedGrain");
                        requestTimer.Start();
                        while (!(await p1.SetLocation(getRandomLocation(random))))
                        {
                            numReattempts++; // this should never happen... it's just here to keep the code paths equal for all cases
                        }
                        requestTimer.Stop();
                    }
                    else if (parameters.indexType.Equals("IndexedPersisted", StringComparison.OrdinalIgnoreCase))
                    {
                        IIndexedPlayerGrain p1 = GrainClient.GrainFactory.GetGrain<IIndexedPlayerGrain>(nextGrain, "Orleans.Benchmarks.Indexing.Scenario03.IndexedPlayerPersistedGrain");
                        requestTimer.Start();
                        while (!(await p1.SetLocation(getRandomLocation(random))))
                        {
                            numReattempts++; // means that AzureTable choked on etag because of concurrent update -> retry
                        }
                        requestTimer.Stop();
                    }
                    else
                    {
                        // crude way to pass exception from robot (on frontend) to conductor
                        return "EXCEPTION wrong index type specified. possible values are none, default, persilo, and perkey";
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
            result.reattempts = numReattempts;
            result.timeouts = numTimeouts;

            return JsonConvert.SerializeObject(result);
        }

        private string getRandomLocation(Random rand)
        {
            return "location_" + rand.Next(parameters.distinctKeyCount);
        }

    }


}
