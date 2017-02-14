using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using Orleans.Benchmarks.Common;

namespace Orleans.Benchmarks.Common
{
    /// <summary>
    /// A wrapper for Http Client functionality
    /// </summary>
    public class DirectClient : IClient 
    {
        public DirectClient(int robotnumber, Action<string> tracer)
        {
            this.robotnumber = robotnumber;
            this.tracer = tracer;
        }

        public Dictionary<string, LatencyDistribution> Stats { get { return null; } } 

      
        int robotnumber;
        Action<string> tracer;

        public int RobotNumber { get { return robotnumber;  } }

        public async Task<string> ServiceRequest(ISimpleRequest request)
        {
              var response = await request.ProcessRequestOnServer();

              return await request.ProcessResponseOnClient(response);
        }

        public Task<string> ServiceConnection(ISocketRequest request)
        {
            throw new NotImplementedException();
        }


        public IBenchmark Benchmark
        {
            get { return Benchmark; }
        }


        public void Trace(string info)
        {
            tracer(info);
        }
    }
}
