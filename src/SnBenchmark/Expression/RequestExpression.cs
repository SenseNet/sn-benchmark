using SenseNet.Client;
using System.Threading.Tasks;

namespace SnBenchmark.Expression
{
    internal class RequestExpression : BenchmarkActionExpression
    {
        public const string NormalSpeed = "NORMAL";

        internal string HttpMethod { get; }
        internal string Url { get; }
        internal string RequestData { get; }
        internal string Speed { get; }

        public RequestExpression(string url, string httpMethod, string requestData, string speed)
        {
            HttpMethod = httpMethod ?? "GET";
            Url = url;
            RequestData = requestData;
            Speed = speed;
        }

        internal override BenchmarkActionExpression Clone()
        {
            return new RequestExpression(Url, HttpMethod, RequestData, Speed);
        }
        internal override async Task ExecuteAsync(IExecutionContext context, string actionId)
        {
            var server = ClientContext.Current.RandomServer;
            var url = server.Url + context.ReplaceTemplates(PathSet.ResolveUrl(Url, context));

            var requestData = HttpMethod == "GET" ? null : RequestData;
            if (requestData != null)
                requestData = context.ReplaceTemplates(requestData);

            var response = await Web.RequestAsync(actionId, server, Speed, HttpMethod, url, requestData);

            context.SetVariable("@Response", response);
        }
        public override string ToString()
        {
            return HttpMethod + ": " + Url;
        }
    }
}
