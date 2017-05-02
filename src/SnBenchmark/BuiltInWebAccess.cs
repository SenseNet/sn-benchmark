using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using SenseNet.Client;

namespace SnBenchmark
{
    internal class BuiltInWebAccess : IWebAccess
    {
        private volatile int _allRequests;
        public int AllRequests => _allRequests;

        private volatile int _requestsPerSec;
        public int RequestsPerSec
        {
            get { return _requestsPerSec; }
            set { _requestsPerSec = value; }
        }

        private int _activeRequests;
        public int ActiveRequests => _activeRequests;

        private double AverageResponseTimeInSec { get; set; }

        private readonly object _responseTimesSync = new object();
        private readonly Queue<TimeSpan> _responseTimes = new Queue<TimeSpan>();

        private Dictionary<string, List<TimeSpan>> _periodResponseTimes = new Dictionary<string, List<TimeSpan>>();

        /// <summary>
        /// Resets the logged response times (grouped by speed category) for a period.
        /// </summary>
        /// <param name="speedItems">A list of speed categories (e.g. normal, slow).</param>
        public void Initialize(IEnumerable<string> speedItems)
        {
            _periodResponseTimes = new Dictionary<string, List<TimeSpan>>();
            foreach (var key in speedItems)
                _periodResponseTimes.Add(key, new List<TimeSpan>());
        }

        /// <summary>
        /// Sends an asynchronous request to the server and returns the whole response result as a string.
        /// Errors are logged and time measuring values and counters are updated.
        /// </summary>
        public async Task<string> RequestAsync(string actionId, ServerContext server, string speedItem, string httpMethod, string url, string requestBody)
        {
            if (Program.Pausing)
                return null;

            url += (url.Contains("?") ? "&" : "?") + "benchamrkId=" + actionId;
            var startTime = DateTime.UtcNow;
            Interlocked.Increment(ref _activeRequests);
            _allRequests++;
            _requestsPerSec++;

            var method = new HttpMethod(httpMethod);

            string responseString;
            try
            {
                responseString = await RESTCaller.GetResponseStringAsync(new Uri(url), server, method, requestBody);
            }
            finally
            {
                Interlocked.Decrement(ref _activeRequests);
                var duration = DateTime.UtcNow - startTime;

                lock (_responseTimesSync)
                {
                    _responseTimes.Enqueue(duration);
                    if (_responseTimes.Count > 100)
                        _responseTimes.Dequeue();
                    AverageResponseTimeInSec = _responseTimes.Average(x => x.TotalMilliseconds) / 1000;
                }

                lock (_periodResponseTimes)
                    _periodResponseTimes[speedItem].Add(duration);
            }

            return responseString;
        }

        /// <summary>
        /// At the end of a period this method resets time values and returns the average
        /// values collected in that period for all speed categories.
        /// </summary>
        public Dictionary<string, double> GetPeriodDataAndReset()
        {
            Dictionary<string, List<TimeSpan>> responseTimes;
            lock (_periodResponseTimes)
            {
                responseTimes = _periodResponseTimes;

                // reset speed category values
                Initialize(responseTimes.Keys);
            }

            var result = new Dictionary<string, double>();
            foreach (var item in responseTimes)
            {
                var avg = 0.0;
                if (item.Value.Any())
                    avg = item.Value.Average(x => x.TotalMilliseconds) / 1000.0;
                result.Add(item.Key, avg);
            }

            return result;
        }

    }
}
