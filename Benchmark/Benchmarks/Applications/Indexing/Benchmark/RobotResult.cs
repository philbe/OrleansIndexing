using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Orleans.Benchmarks.Indexing
{
    class RobotResult
    {
        public List<float> latencies;
        public double throughput;
        public int reattempts;
        public int timeouts;
        public string errors;
    }
}
