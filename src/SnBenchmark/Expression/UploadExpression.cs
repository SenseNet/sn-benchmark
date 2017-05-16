using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SenseNet.Client;

namespace SnBenchmark.Expression
{
    internal class UploadExpression : BenchmarkActionExpression
    {
        internal string Source { get; }
        internal string Target { get; }
        internal string Location { get; }

        public UploadExpression(string source, string target, string location)
        {
            Source = source;
            Target = target;
            Location = location;
        }

        internal override BenchmarkActionExpression Clone()
        {
            return new UploadExpression(Source, Target, Location);
        }

        internal override async Task ExecuteAsync(IExecutionContext context, string actionId)
        {
            var location = context.ReplaceTemplates(Location);
            var source = context.ReplaceTemplates(Source);
            var target = context.ReplaceTemplates(Target);

            var filePath = Path.Combine(location, source);
            var fileName = Path.GetFileName(filePath);

            Content response;
            using (var stream = File.OpenRead(filePath))
                response = await Content.UploadAsync(target, fileName, stream);

            context.SetVariable("@Response", response);
        }

        internal override void Test(IExecutionContext context, string actionId, string profileResponsesDirectory)
        {
            var location = context.ReplaceTemplates(Location);
            var source = context.ReplaceTemplates(Source);
            var target = context.ReplaceTemplates(Target);

            var filePath = Path.Combine(location, source);
            var fileName = Path.GetFileName(filePath);

            Content response;
            using (var stream = File.OpenRead(filePath))
                response = Content.UploadAsync(target, fileName, stream).Result;

            var responseFileName = Path.Combine(profileResponsesDirectory, $"Response_{actionId}");
            using (var writer = new StreamWriter(responseFileName))
                writer.Write(response);
        }
    }
}
