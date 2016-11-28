using SenseNet.Client;
using SenseNet.Diagnostics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SnBenchmark
{
    class BuiltInBenchmarkActions
    {
        static volatile int _allRequests;
        static volatile int _requestsPerSec;
        static int _activeRequests;
        static bool _pausing;
        static Queue<TimeSpan> _responseTimes = new Queue<TimeSpan>();
        static double _averageResponseTimeInSec;

        public static async Task RequestAsync(string url, ServerContext server, string actionId)
        {
            if (_pausing)
                return;
            url += (url.Contains("?") ? "&" : "?") + ("benchamrkId=" + actionId);
            var startTime = DateTime.UtcNow;
            Interlocked.Increment(ref _activeRequests);
            _allRequests++;
            _requestsPerSec++;

            await RESTCaller.GetResponseStringAsync(new Uri(url), server, HttpMethod.Get);

            Interlocked.Decrement(ref _activeRequests);
            var duration = DateTime.UtcNow - startTime;
            SnTrace.Write("Duration: {0}", duration);

            _responseTimes.Enqueue(duration);
            if (_responseTimes.Count > 100)
                _responseTimes.Dequeue();
            _averageResponseTimeInSec = _responseTimes.Average(x => x.TotalMilliseconds) / 1000;

            lock (_periodResponseTimes)
                _periodResponseTimes.Add(duration);
        }

        public static void Monitor()
        {
            Console.WriteLine("{0}\t{1}\t{2}\t{3}\t{4:0.000}\t{5:0.000}\t{6:0.000}\t{7}",
                _allRequests,
                Program._runningProfiles.Count,
                _activeRequests,
                _requestsPerSec,
                _averageResponseTimeInSec,
                Program._periodData.Item1,
                Program._periodData.Item2,
                _pausing ? "pause" : "");
            _requestsPerSec = 0;

            if (Console.KeyAvailable)
            {
                var x = Console.ReadKey(true);
                if (x.KeyChar == ' ')
                    _pausing = !_pausing;
            }
        }


        static List<TimeSpan> _periodResponseTimes = new List<TimeSpan>();
        public static Tuple<double, double> GetPeriodDataAndReset()
        {
            TimeSpan[] responseTimes;
            lock (_periodResponseTimes)
            {
                responseTimes = _periodResponseTimes.ToArray();
                _periodResponseTimes.Clear();
            }

            var borderThickness = responseTimes.Length / 20;
            var length = responseTimes.Length - 2 * borderThickness;
            var avg1 = responseTimes.Average(x => x.TotalMilliseconds);
            var avg2 = responseTimes.OrderBy(x => x).Skip(borderThickness).Take(length).Average(x => x.Ticks);
            return new Tuple<double, double>(
                avg1 / 1000,
                TimeSpan.FromTicks(Convert.ToInt64(avg2)).TotalMilliseconds / 1000);
        }
    }
}
