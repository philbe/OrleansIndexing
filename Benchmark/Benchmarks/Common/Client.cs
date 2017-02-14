using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Orleans.Benchmarks.Common
{
    public interface IClient : IRobotContext
    {
       Dictionary<string, LatencyDistribution> Stats { get; }
    }
}
