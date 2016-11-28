using System.Threading.Tasks;

namespace SnBenchmark.Expression
{
    internal abstract class BenchmarkActionExpression
    {
        internal abstract BenchmarkActionExpression Clone();

        internal abstract Task ExecuteAsync(IExecutionContext context, string actionId);
    }
}
