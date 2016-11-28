using SenseNet.Client;
using System.Threading.Tasks;

namespace SnBenchmark.Expression
{
    internal class RequestExpression : BenchmarkActionExpression
    {
        public const string NormalSpeed = "NORMAL";

        private readonly string _httpMethod;
        private readonly string _url;
        private readonly string _requestData;
        private readonly string _speed;

        public RequestExpression(string url, string httpMethod, string requestData, string speed)
        {
            _httpMethod = httpMethod ?? "GET";
            _url = url;
            _requestData = requestData;
            _speed = speed;
        }

        internal override BenchmarkActionExpression Clone()
        {
            return new RequestExpression(_url, _httpMethod, _requestData, _speed);
        }
        internal override async Task ExecuteAsync(IExecutionContext context, string actionId)
        {
            var server = ClientContext.Current.RandomServer;
            var url = server.Url + context.ReplaceTemplates(_url);

            var requestData = _httpMethod == "GET" ? null : _requestData;
            if (requestData != null)
                requestData = context.ReplaceTemplates(requestData);

            var response = await Web.RequestAsync(actionId, server, _speed, _httpMethod, url, requestData);

            context.SetVariable("@Response", response);
        }
        public override string ToString()
        {
            return _httpMethod + ": " + _url;
        }
    }
}
