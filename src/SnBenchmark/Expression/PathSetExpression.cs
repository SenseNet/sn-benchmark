using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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

        internal override Task ExecuteAsync(IExecutionContext context, string actionId)
        {
            throw new NotSupportedException();
        }
    }
}
