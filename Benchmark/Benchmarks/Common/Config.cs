using Microsoft.Azure;
using Microsoft.WindowsAzure.Storage;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Orleans.Benchmarks.Common
{
    public static class Config
    {
        public static string GetConductor()
        {
            var dc = CloudConfigurationManager.GetSetting("Conductor");

            if (string.IsNullOrEmpty(dc))
                throw new Exception("invalid configuration: missing DataCenter");

            return dc;
        }


        public static string GetDataCenter()
        {
            try
            {
                var dc = CloudConfigurationManager.GetSetting("DataCenter");

                if (string.IsNullOrEmpty(dc))
                    throw new Exception("invalid configuration: missing DataCenter");

                return dc;
            }
            catch(Exception)
            {
                // we are in the single-process LocalDebuggingDeployment.
                return "localhost:847";
            }
        }

        public static string GetMultiCluster()
        {
            try
            {
                var mc = CloudConfigurationManager.GetSetting("MultiCluster");

                if (string.IsNullOrEmpty(mc))
                    throw new Exception("invalid configuration: missing MultiCluster");

                return mc;
            }
            catch (Exception)
            {
                // we are in the single-process LocalDebuggingDeployment.
                return "local";
            }
        }

        public static bool InCloud()
        {
            string dc = null;

            try
            {
                dc = CloudConfigurationManager.GetSetting("DataCenter");
            }
            catch (Exception)
            {
                // we are in the single-process LocalDebuggingDeployment.
                return false;
            }

               if (string.IsNullOrEmpty(dc))
                    throw new Exception("invalid configuration: missing DataCenter");

             return ! (dc.ToLowerInvariant().Contains("localhost") || dc.Contains("127.0.0.1"));
        }

        /// <summary>
        /// Returns Storage account associated with benchmark data
        /// such as statistics, results, validation
        /// </summary>
        /// <returns></returns>
        public static CloudStorageAccount getBenchmarkStorage()
        {
            string connectionKey = null;

            connectionKey = CloudConfigurationManager.GetSetting("BenchmarkStorage");

            if (string.IsNullOrEmpty(connectionKey))
            {
               connectionKey = "UseDevelopmentStorage=true";
            }

            return CloudStorageAccount.Parse(connectionKey);
        }

        /// <summary>
        /// Returns Storage account for datacenter-local storage
        /// </summary>
        /// <returns></returns>
        public static CloudStorageAccount getLocalStorage()
        {
            string connectionKey = null;

            connectionKey = CloudConfigurationManager.GetSetting("DataConnectionString");

            if (string.IsNullOrEmpty(connectionKey))
            {
                connectionKey = "UseDevelopmentStorage=true";
            }

            return CloudStorageAccount.Parse(connectionKey);
        }


    }
}
