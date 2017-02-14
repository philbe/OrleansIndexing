using Orleans.Benchmarks.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Specialized;
using Newtonsoft.Json.Linq;
using Orleans.Benchmarks.Indexing.Scenario01;
using Orleans.Benchmarks.Indexing.Scenario02;
using Orleans.Benchmarks.Indexing.Scenario03;
using Orleans.Benchmarks.Indexing.Scenario04;
using Orleans.Benchmarks.Indexing.Scenario05;

namespace Orleans.Benchmarks.Indexing
{
    public class Benchmark : IBenchmark
    {
        public string Name { get { return "indexing"; } }

        // list of scenarios for this benchmark
        public IEnumerable<IScenario> Scenarios { get { return scenarios; } }

        private IScenario[] scenarios = new IScenario[]
        {
            // new IndexingScenario1(...),
            // new IndexingScenario2(...),
            //new SimpleScenario(10, 120),
        };

        // parsing of http requests - not necessary, we do not separate load generator from front end
        public IRequest ParseRequest(string verb, IEnumerable<string> urlpath, NameValueCollection arguments, string body = null)
        {
            throw new NotImplementedException();
        }

        public IScenario CreateScenarioFromJson(JObject json, int seed)
        {
            if (json["scenarioType"].ToString().Equals("scenario01", StringComparison.OrdinalIgnoreCase) || json["scenarioType"].ToString().Equals("indexingscenario01", StringComparison.OrdinalIgnoreCase))
            {
                return new IndexingScenario01(json.ToString(), seed);
            }
            if (json["scenarioType"].ToString().Equals("scenario02", StringComparison.OrdinalIgnoreCase) || json["scenarioType"].ToString().Equals("indexingscenario02", StringComparison.OrdinalIgnoreCase))
            {
                return new IndexingScenario02(json.ToString(), seed);
            }
            if (json["scenarioType"].ToString().Equals("scenario03", StringComparison.OrdinalIgnoreCase) || json["scenarioType"].ToString().Equals("indexingscenario03", StringComparison.OrdinalIgnoreCase))
            {
                return new IndexingScenario03(json.ToString(), seed);
            }
            if (json["scenarioType"].ToString().Equals("scenario04", StringComparison.OrdinalIgnoreCase) || json["scenarioType"].ToString().Equals("indexingscenario04", StringComparison.OrdinalIgnoreCase))
            {
                return new IndexingScenario04(json.ToString(), seed);
            }
            if (json["scenarioType"].ToString().Equals("scenario05", StringComparison.OrdinalIgnoreCase) || json["scenarioType"].ToString().Equals("indexingscenario05", StringComparison.OrdinalIgnoreCase))
            {
                return new IndexingScenario05(json.ToString(), seed);
            }
            throw new Exception("No valid scenarioType was specified. Possible values are scenario01, scenario02, scenario03, or scenario04");
        }
    }
}
