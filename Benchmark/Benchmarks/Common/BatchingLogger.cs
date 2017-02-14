 
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Web;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure;
using Microsoft.WindowsAzure.ServiceRuntime;
using System.Threading.Tasks;
using System.Diagnostics;
using Microsoft.WindowsAzure.Storage.Blob;
using System.Text;
#pragma warning disable 0649
#pragma warning disable 0420

namespace Orleans.Benchmarks.Common
{
    public class BatchingLogger
    {
        private volatile Thread writebackthread;

        private List<string> Msgs = new List<string>();

        public BatchingLogger(CloudBlockBlob blob, Func<bool> isbusy, Func<Task> shutdown)
        {
            this.blob = blob;
            log = new StringBuilder();
            this.isbusy = isbusy;
            this.shutdown = shutdown;
            writebackthread = new Thread(BgWork);
            writebackthread.Name = "Batching Logger";
            writebackthread.Start();
        }

        private StringBuilder log;
        CloudBlockBlob blob;
        Func<bool> isbusy;
        Func<Task> shutdown;

        public void Trace(string msg)
        {
            lock (this)
                Msgs.Add(msg);
        }

        public void Done()
        {
            done = true;
        }

        private int backoff_msec = 500;
        private int shutdown_msec = 120000;

        volatile bool done = false;

        public void BgWork()
        {
            try
            {
                int quietfor = 0;

             start:
                do
                {
                    List<string> work = null;

                    lock (this)
                    {
                        if (Msgs.Count > 0)
                        {
                            work = Msgs;
                            Msgs = new List<string>();
                        }
                    }

                    if (work == null)
                    {
                        Thread.Sleep(backoff_msec);

                        if (!isbusy())
                            quietfor += backoff_msec;
                    }
                    else
                    {
                        quietfor = 0;

                        foreach (var s in work)
                            log.AppendLine(s);

                        if (work.Count < 100)
                            Thread.Sleep(backoff_msec); // increase batching
                    }


                } while (!done && quietfor < shutdown_msec);

                if (shutdown != null)
                {
                    shutdown();
                    shutdown = null; // don't call it again
                    quietfor = 0;
                    goto start; // try once more
                }

                // upload text
                blob.UploadText(log.ToString());
            }
            catch (Exception e)
            {
                System.Diagnostics.Trace.WriteLine("[TraceInterval.cs] Could not write to trace blob because of exception: e=" + e);
            }
        }


    }
}