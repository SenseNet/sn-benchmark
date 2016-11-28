using SenseNet.Client;
using SenseNet.Diagnostics;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SnBenchmark
{
    internal abstract class BenchmarkActionExpression
    {
        internal abstract BenchmarkActionExpression Clone();

        internal abstract Task ExecuteAsync(string actionId);
    }

    internal class RequestExpression : BenchmarkActionExpression
    {
        string _httpMethod;
        string _url;
        string _requestData;

        public RequestExpression(string url, string httpMethod, string requestData)
        {
            _httpMethod = httpMethod ?? "GET";
            _url = url;
            _requestData = requestData;
        }

        internal override BenchmarkActionExpression Clone()
        {
            return new RequestExpression(_url, _httpMethod, _requestData);
        }
        internal async override Task ExecuteAsync(string actionId)
        {
            //UNDONE: Use _httpMethod and _requestData
            var server = ClientContext.Current.RandomServer;
            var url = server.Url + _url;

            await BuiltInBenchmarkActions.RequestAsync(url, server, actionId);
        }
        public override string ToString()
        {
            return _httpMethod + ": " + _url;
        }
    }

    internal class WaitExpression : BenchmarkActionExpression
    {
        int _milliseconds;
        public WaitExpression(int milliseconds)
        {
            _milliseconds = milliseconds;
        }
        internal override BenchmarkActionExpression Clone()
        {
            return new WaitExpression(_milliseconds);
        }
        internal async override Task ExecuteAsync(string actionId)
        {
            var ms = _milliseconds;
            if (ms > 5)
                ms = RNG.Get(ms - ms / 4, ms + ms / 2);
            await Task.Delay(ms);
        }
        public override string ToString()
        {
            return "WAIT: " + _milliseconds;
        }
    }
}
