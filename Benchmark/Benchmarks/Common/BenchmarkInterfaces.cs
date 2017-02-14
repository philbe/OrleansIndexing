using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Orleans.Benchmarks.Common
{
   
    public interface IBenchmark : IRequestDispatcher
    {
        // the name of this benchmark
        string Name { get; }

        // built-in named scenarios
        IEnumerable<IScenario> Scenarios { get; }

        // constructor for making a scenario based on json parameters
        IScenario CreateScenarioFromJson(JObject json, int seed);

    }

    public interface IScenario
    {
        string Name { get;  }

        int NumRobots { get; }

        Task<string> ConductorScript(IConductorContext context, CancellationToken token);
        //void ConductorScript(object param);

        Task<string> RobotScript(IRobotContext context, int workernumber, string parameters);

        string JsonParameters { get; }
    }


    public interface IConductorContext
    {
        int NumRobots { get; }

        Task<string> RunRobot(int robotnumber, string parameters = "");

        // trace an event to the conductor console
        void Trace(string info);

        //void setConductorScriptResult(string result);

        string[] DataCenters { get; }
    }

 

    public interface IRobotContext
    {
       //send an http request to the service. The task finishes after the response has been processed.
        //each robot can optionally return a string encoded result back to the conductor.
        Task<string> ServiceRequest(ISimpleRequest request);

        //send an socket request to the service. The task finishes after the socket close has been processed.
        Task<string> ServiceConnection(ISocketRequest request);

        // the number of this robot
        int RobotNumber { get; }

        // trace an event
        void Trace(string info);
    }
 
 


}
