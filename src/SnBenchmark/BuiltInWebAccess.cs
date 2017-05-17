using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
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

        // ReSharper disable once UnusedAutoPropertyAccessor.Local
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
                var data = requestBody?.Length > 100 ? requestBody.Substring(0, 100) + "..." : requestBody;
                LogRequest(data == null ? $"{httpMethod} {url}" : $"{httpMethod} {url} | {data}");
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

        public async Task<Content> UploadAsync(string actionId, ServerContext server, string speedItem, string targetContainerPath, string fileName,
            Stream stream)
        {
            if (Program.Pausing)
                return null;

            var startTime = DateTime.UtcNow;
            Interlocked.Increment(ref _activeRequests);
            _allRequests++;
            _requestsPerSec++;

            Content responseContent;
            try
            {
                LogRequest($"UPLOAD: fileName:{fileName} target:{targetContainerPath}, server:{server.Url}, actionId:{actionId}");
                responseContent = await Content.UploadAsync(targetContainerPath, fileName, stream);
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

            return responseContent;
        }

        public async Task<IEnumerable<string>> QueryPathSetAsync(string query)
        {
            var hostUrl = ClientContext.Current.RandomServer.Url;
            var url = $"{hostUrl}/OData.svc/Root?metadata=no&$select=Path&query={query}";
            var responseString = await RESTCaller.GetResponseStringAsync(new Uri(url));
            return ParsePaths(responseString);
        }

        internal IEnumerable<string> ParsePaths(string src)
        {
            var result = new List<string>();
            var propToken = "\"Path\":";
            var p = 0;
            while (true)
            {
                p = src.IndexOf(propToken, p, StringComparison.Ordinal);
                if (p < 0)
                    break;
                var p0 = src.IndexOf("\"", p+ propToken.Length, StringComparison.Ordinal);
                var p1 = src.IndexOf("\"", p0 + 1, StringComparison.Ordinal);
                var path = src.Substring(p0 + 1, p1 - p0 - 1);
                result.Add(path);
                p = p0 + 1;
            }

            return result;
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

        public string[] GetRequestLog()
        {
            var part1 = _requestLog.Skip(_requestLogIndex).Where(x => x != null);
            var part2 = _requestLog.Take(_requestLogIndex).Where(x => x != null);
            return part1.Union(part2).ToArray();
        }


        private readonly object _requestLogSync = new object();
        private readonly string[] _requestLog = new string[10000];
        private int _requestLogIndex;

        private void LogRequest(string url)
        {
            lock (_requestLogSync)
            {
                _requestLog[_requestLogIndex] = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss.ffff}\t{url}";
                _requestLogIndex = (_requestLogIndex + 1)%_requestLog.Length;
            }
        }
    }
}

