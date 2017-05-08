using System.Threading.Tasks;

namespace SnBenchmark.Expression
{
    internal class WaitExpression : BenchmarkActionExpression
    {
        internal int Milliseconds { get; }

        public WaitExpression(int milliseconds)
        {
            Milliseconds = milliseconds;
        }
        internal override BenchmarkActionExpression Clone()
        {
            return new WaitExpression(Milliseconds);
        }

        internal override void Test(IExecutionContext context, string actionId, string profileResponsesDirectory)
        {
            // do nothing
        }

        internal override async Task ExecuteAsync(IExecutionContext context, string actionId)
        {
            var ms = Milliseconds;
            if (ms > 5)
                ms = RNG.Get(ms - ms / 4, ms + ms / 2);
            await Task.Delay(ms);
        }
        public override string ToString()
        {
            return "WAIT: " + Milliseconds;
        }
    }
}
