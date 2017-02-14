using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Orleans.Benchmarks.Common
{
    public static class TestResultStorage
    {

        private static string TEST_CONTAINER = "tests";
        private static string RESULTFILE_CONTAINER = "results";

        public static string MakeTestName(string benchmarkname)
        {
            return string.Format("{1}-{0:o}", DateTime.UtcNow, benchmarkname);
        }

        public static async Task<CloudBlobDirectory> GetOrCreateTestDirectory(string testname)
        {
            CloudStorageAccount account = Config.getBenchmarkStorage();
            CloudBlobClient blobclient = account.CreateCloudBlobClient();
            CloudBlobContainer container = blobclient.GetContainerReference(TEST_CONTAINER);
            await container.CreateIfNotExistsAsync();
            CloudBlobDirectory dir = container.GetDirectoryReference(testname);
            return dir;

        }

        public static async Task<CloudBlobDirectory> GetOrCreateResultFileDirectory(string testname)
        {
            CloudStorageAccount account = Config.getBenchmarkStorage();
            CloudBlobClient blobclient = account.CreateCloudBlobClient();
            CloudBlobContainer container = blobclient.GetContainerReference(RESULTFILE_CONTAINER);
            await container.CreateIfNotExistsAsync();
            CloudBlobDirectory dir = container.GetDirectoryReference(testname);
            return dir;
 
        }


    }
}
