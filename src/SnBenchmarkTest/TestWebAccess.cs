using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using SenseNet.Client;
using SnBenchmark;

namespace SnBenchmarkTest
{
    internal class TestWebAccess : IWebAccess
    {
        public int AllRequests { get; }
        public int ActiveRequests { get; }
        public int RequestsPerSec { get; set; }
        public void Initialize(IEnumerable<string> speedItems)
        {
            throw new NotImplementedException();
        }

        public Dictionary<string, double> GetPeriodDataAndReset()
        {
            throw new NotImplementedException();
        }

        public Task<string> RequestAsync(string actionId, ServerContext server, string speedItem, string httpMethod, string url,
            string requestBody)
        {
            throw new NotImplementedException();
        }
    }
}
