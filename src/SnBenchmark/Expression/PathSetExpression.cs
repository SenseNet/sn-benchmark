using System;
using System.Threading.Tasks;

namespace SnBenchmark.Expression
{
    internal class PathSetExpression : BenchmarkActionExpression
    {
        public string Name { get; }
        public string Definition { get; }

        public PathSetExpression(string name, string definition)
        {
            Name = name;
            Definition = definition;
        }

        internal override BenchmarkActionExpression Clone()
        {
            throw new NotSupportedException();
        }

        internal override void Test(IExecutionContext context, string actionId, string profileResponsesDirectory)
        {
            // do nothing
        }

        internal override Task ExecuteAsync(IExecutionContext context, string actionId)
        {
            throw new NotSupportedException();
        }
    }
}
