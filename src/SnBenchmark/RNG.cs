using System;

namespace SnBenchmark
{
    // ReSharper disable once InconsistentNaming
    internal class RNG
    {
        private static readonly Random Rnd = new Random();

        public static int Get(int from, int to)
        {
            return Rnd.Next(from, to);
        }
    }
}
