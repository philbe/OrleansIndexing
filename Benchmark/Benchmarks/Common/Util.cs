using Microsoft.WindowsAzure.ServiceRuntime;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Auth;
using Microsoft.WindowsAzure.Storage.Table;
using Microsoft.WindowsAzure;
using System.Net;


namespace Orleans.Benchmarks.Common
{
    public static class Util
    {

  
  
      
        public static string PrintStats(Dictionary<string, LatencyDistribution> stats)
        {
            var b = new StringBuilder();
            if (stats != null)
                foreach (var kvp in stats)
                {
                    b.AppendLine(kvp.Key);
                    b.Append("      ");
                    b.AppendLine(string.Join(" ", kvp.Value.GetStats()));
                }
            return b.ToString();
        }



    


        public static void Assert(bool condition, string message = null)
        {
            if (condition)
                return;
            if (!string.IsNullOrEmpty(message))
                throw new AssertionException(message);
            else
                throw new AssertionException();
        }

        public static void Fail(string message)
        {
            throw new AssertionException(message);
        }

        [Serializable()]
        public class AssertionException : Exception
        {
            public AssertionException() { }
            public AssertionException(string message) : base(message) { }
            protected AssertionException(System.Runtime.Serialization.SerializationInfo info,
                     System.Runtime.Serialization.StreamingContext context)
                : base(info, context) { }
        }


   

    }



}
