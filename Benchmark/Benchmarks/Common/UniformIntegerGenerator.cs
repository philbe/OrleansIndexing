using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Orleans.Benchmarks.Common
{
    public class UniformIntegerGenerator
    {
        Random generator;
        int startKey;
        int endKey;       

        public UniformIntegerGenerator(int seed, int startKey, int endKey)
        {
            this.generator = new Random(seed);
            this.startKey = startKey;
            this.endKey = endKey;
        }

        public int nextInt()
        {
            return generator.Next(startKey, endKey);
        }
    }
}
