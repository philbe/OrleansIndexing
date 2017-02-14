using Orleans.Benchmarks.Common;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Orleans.Benchmarks
{
    public class Console
    {
        BenchmarkList benchmarks = new BenchmarkList();

        private void PrintUsage(Action<string> wl)
        {
            wl("Usage:");
            wl("'list'                    to list all benchmarks");
            wl("'list <bmark>'            to list all scenarios for <bmark>");
            wl("'run <bmark>.<scenario>'  to run a scenario");
            wl("'quit'  to exit");
            wl("");
        }

        Action<string> wl;
        Func<string> rl;


        public Console(Action<string> wl, Func<string> rl)
        {
            this.wl = wl;
            this.rl = rl;
        }

        public void Welcome()
        {
            wl("Welcome to the Benchmark Console.");
            PrintUsage(wl);
            wl("");
        }

        //
        // {
        //    benchmark : "",
        //    scenarios : [
        //           { ... },
        //           { ... }
        //    ]
        // }
        public class JsonTests
        {
            public string benchmark;
            public List<string> datacenters;
            public bool randomize;
            public JObject prototype;
            public List<JObject> scenarios;
            public int skip;
        }

        public class TestSpec
        {
            public string testname;
            public IBenchmark benchmark;
            public List<string> datacenters;
            public IReadOnlyList<ScenarioInfo> scenarios;
            public int skip;
        }

        public class ScenarioInfo
        {
            public IScenario scenario;
            public JObject spec;
        }
    
        public TestSpec SelectScenario()
        {
            while (true)
            {
                var command = rl();

                if (command == "quit" || command == "exit")
                    return null;

                else if (command == "list") {
                   foreach (var bm in benchmarks.Benchmarks)
                      wl(bm.Name);
                   wl("");
                }

                else if (command.Trim().StartsWith("{"))
                {

                    // it's a json description
                    try
                    {
                        var tests = JsonConvert.DeserializeObject<JsonTests>(command);
                        var bmname = tests.benchmark;
                        var bm = benchmarks.ByName(bmname);
                        var testname = string.Format("{1}-{0:o}", DateTime.UtcNow, bmname);
                        var scenarios = new List<ScenarioInfo>();
                        foreach (var o in tests.scenarios)
                        {
                            if (tests.prototype != null)
                                foreach (var kvp in tests.prototype)
                                    if (o.Property(kvp.Key) == null)
                                       o[kvp.Key] = kvp.Value;
                            scenarios.Add(new ScenarioInfo()
                            {
                                scenario = bm.CreateScenarioFromJson(o, testname.GetHashCode()),
                                spec = o
                            });
                            //if (tests.randomize)
                            //    scenarios.Sort((kvp1, kvp2) => kvp1.Key.CompareTo(kvp2.Key));
                        }
                        return new TestSpec()
                        {
                            testname = testname,
                            benchmark = bm,
                            datacenters = tests.datacenters,
                            scenarios = scenarios,
                            skip = tests.skip
                        };
                    }
                    catch (JsonReaderException jre)
                    {
                        wl("Json Error:");
                        wl(jre.Message);
                    }
                    catch (Exception e)
                    {
                        wl("Exception in Conductor:");
                        wl(e.ToString());
                    }
                }

                else
                {
                    var pos = command.LastIndexOf(" ");
                    if (pos == -1)
                        PrintUsage(wl);
                    else
                    {
                        var dotpos = command.LastIndexOf(".");
                        var bmname = (dotpos == -1 ? command.Substring(pos + 1) : command.Substring(pos + 1, dotpos - pos - 1));
                        var bm = benchmarks.ByName(bmname);
                        if (bm == null)
                            PrintUsage(wl);
                        else
                        {
                            if (command.StartsWith("list"))
                            {
                                foreach (var s in bm.Scenarios)
                                    wl(s.Name);
                                wl("");
                            }
                            else if (command.StartsWith("run"))
                            {
                                var scenarioname = command.Substring(dotpos + 1);

                                if (scenarioname == null || scenarioname.Length == 0)
                                {
                                    PrintUsage(wl);
                                }
                                else
                                {
                                    //support for basic globbing pattern matching (e.g. A* will select all scenarios beginning with A)
                                    //supports * and ? operators
                                    string pattern = string.Format("^{0}$", Regex.Escape(scenarioname).Replace(@"\*", ".*").Replace(@"\?", "."));
                                    Regex scenarioRegex = new Regex(pattern);
                                    var scenarios = bm.Scenarios.Where((s) => scenarioRegex.IsMatch(s.Name)).ToList();

                                    if (scenarios == null || !scenarios.Any())
                                    {
                                        PrintUsage(wl);
                                    }
                                    else
                                    {
                                        return new TestSpec()
                                        {
                                            testname = string.Format("{1}-{0:o}", DateTime.UtcNow, bmname),
                                            benchmark = bm,
                                            datacenters = null,
                                            scenarios = scenarios.Select(s => new ScenarioInfo() { scenario = s }).ToList(),
                                            skip = 0
                                        }; 
                                    }
                                }
                            }
                            else
                                PrintUsage(wl);
                        }
                    }
                }
            }
        }
    }
}
