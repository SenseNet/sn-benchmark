using System.IO;
using System.Threading.Tasks;
using SenseNet.Client;

namespace SnBenchmark.Expression
{
    internal class UploadExpression : BenchmarkActionExpression
    {
        internal string Source { get; }
        internal string Target { get; }
        internal string Location { get; }
        internal string Speed { get; }

        public UploadExpression(string source, string target, string location, string speed)
        {
            Source = source;
            Target = target;
            Location = location;
            Speed = speed;
        }

        internal override BenchmarkActionExpression Clone()
        {
            return new UploadExpression(Source, Target, Location, Speed);
        }

        internal override async Task ExecuteAsync(IExecutionContext context, string actionId)
        {
            var response = await UploadAsync(context, actionId);
            context.SetVariable("@Response", response);
        }

        internal override void Test(IExecutionContext context, string actionId, string profileResponsesDirectory)
        {
            var response = UploadAsync(context, actionId).Result;
            context.SetVariable("@Response", response);

            var responseFileName = context.GetResponseFilePath(profileResponsesDirectory, actionId);
            using (var writer = new StreamWriter(responseFileName))
                writer.Write(response);
        }

        private async Task<Content> UploadAsync(IExecutionContext context, string actionId)
        {
            var server = ClientContext.Current.RandomServer;

            var location = context.ReplaceTemplates(Location);
            var source = context.ReplaceTemplates(Source);
            var target = context.ReplaceTemplates(Target);

            var filePath = Path.Combine(location, source);
            var fileName = Path.GetFileName(filePath);

            Content response;
            using (var stream = File.OpenRead(filePath))
                response = await Web.UploadAsync(actionId, server, Speed, target, fileName, stream);

            return response;
        }
    }
}
