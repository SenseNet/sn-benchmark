namespace SNBCalc
{
    internal class BenchmarkLine
    {
        private static char Separator = ';';
        public string Source { get; private set; }
        public int ProfileCount { get; private set; }
        public int ActiveConnections { get; private set; }
        public int RequestsPerSec { get; private set; }

        public static BenchmarkLine Parse(string src)
        {
            var a = src.Split(Separator);
            if (a.Length < 3)
                return null;

            int pcount;
            if (!int.TryParse(a[0], out pcount))
                return null;

            int active;
            if (!int.TryParse(a[1], out active))
                return null;

            int reqPerSec;
            if (!int.TryParse(a[2], out reqPerSec))
                return null;

            return new BenchmarkLine
            {
                Source = src,
                ProfileCount = pcount,
                ActiveConnections = active,
                RequestsPerSec = reqPerSec
            };
        }
    }
}
