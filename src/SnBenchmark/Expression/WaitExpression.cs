using System.Threading.Tasks;

namespace SnBenchmark.Expression
{
    internal class WaitExpression : BenchmarkActionExpression
    {
        private readonly int _milliseconds;

        public WaitExpression(int milliseconds)
        {
            _milliseconds = milliseconds;
        }
        internal override BenchmarkActionExpression Clone()
        {
            return new WaitExpression(_milliseconds);
        }
        internal override async Task ExecuteAsync(IExecutionContext context, string actionId)
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
