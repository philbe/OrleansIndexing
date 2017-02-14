using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Orleans.Benchmarks.Common
{
    public static class Statistics
    {
        public static void computestats(double[,] latenciesbysecond, int warmup, int robots, int rounds, out int count, out double mean, out double q1, out double q3, out double min, out double max, out double stddev, out double variance, out double stderr, out double median, out double mad)
        {
            // compute mean, median, min, max
            double sum = 0;
            count = 0;
            var vals = new List<double>();
            for (int j = warmup; j < rounds; j++)
                for (int i = 0; i < robots; i++)
                {
                    var x = latenciesbysecond[j, i];
                    if (x >= 0)
                    {
                        sum += x;
                        count++;
                        vals.Add(x);
                    }
                }
            if (count == 0)
            {
                mean = double.NaN;
                median = double.NaN;
                min = double.NaN;
                max = double.NaN;
                stddev = double.NaN;
                stderr = double.NaN;
                variance = double.NaN;
                mad = double.NaN;
                q1 = double.NaN;
                q3 = double.NaN;
                return;
            }
            mean = sum / count;
            vals.Sort();
            median = (vals[(vals.Count - 1) / 2] + vals[vals.Count / 2]) / 2;
            min = vals[0];
            max = vals[vals.Count - 1];
            // https://en.wikipedia.org/wiki/Quartile
            if (vals.Count % 2 == 0)
            {
                var mid = vals.Count / 2;
                q1 = (vals[(mid - 1) / 2] + vals[mid / 2]) / 2;
                q3 = (vals[mid + ((mid - 1) / 2)] + vals[mid + (mid / 2)]) / 2;
            }
            else if (vals.Count % 4 == 1)
            {
                var n = vals.Count / 4;
                q1 = .25 * vals[n] + .75 * vals[n + 1];
                q3 = .75 * vals[3 * n + 1] + .25 * vals[3 * n + 2];
            }
            else // (vals.Count % 4 == 3)
            {
                var n = vals.Count / 4;
                q1 = .75 * vals[n + 1] + .25 * vals[n + 2];
                q3 = .25 * vals[3 * n + 2] + .75 * vals[3 * n + 3];
            }

            //compute stddev and mad (excluding warmup)
            double var = 0.0;
            var absdev = new List<double>();
            for (int j = warmup; j < rounds; j++)
                for (int i = 0; i < robots; i++)
                {
                    var x = latenciesbysecond[j, i];
                    if (x >= 0)
                    {
                        var delta = mean - x;
                        var += delta * delta;
                        absdev.Add(Math.Abs(delta));
                    }
                }
            variance = var / (count - 1);
            stddev = Math.Sqrt(variance);
            stderr = stddev / Math.Sqrt(count);
            absdev.Sort();
            mad = (absdev[(absdev.Count - 1) / 2] + absdev[absdev.Count / 2]) / 2;
        }

        public static List<double> removeoutliers(double[,] latenciesbysecond, int warmup, int robots, int rounds, double threshold, double mean, double mad)
        {
            // http://www.itl.nist.gov/div898/handbook/eda/section3/eda35h.htm

            var outliers = new List<double>();
            for (int j = warmup; j < rounds; j++)
                for (int i = 0; i < robots; i++)
                {
                    var x = latenciesbysecond[j, i];
                    if (x >= 0 && Math.Abs(0.6745 * (x - mean) / mad) > threshold)
                    {
                        outliers.Add(x);
                        latenciesbysecond[j, i] = -1;
                    };
                }
            return outliers;
        }

    }
}
