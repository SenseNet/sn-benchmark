namespace SNBCalc
{
    internal class BenchmarkEndPointCalculator
    {
        private readonly NoiseFilter<int> _inputFilter = new NoiseFilter<int>(100);
        private readonly NoiseFilter<double> _diffFilter = new NoiseFilter<double>(50);
        private int trigger = 10;
        private int _continuousNegative;

        public string Detect(string line, out bool reached)
        {
            reached = false;

            var dataLine = BenchmarkLine.Parse(line);
            if (dataLine == null)
                return null;

            var lastFilteredInput = _inputFilter.FilteredValue;
            var filteredInput = _inputFilter.NextValue(dataLine.RequestsPerSec);

            var diff = filteredInput - lastFilteredInput;

            var filteredDiff = _diffFilter.NextValue(diff);
            if (filteredDiff < 0.0)
                _continuousNegative++;
            else
                _continuousNegative = 0;
            reached = _continuousNegative >= trigger;

            return $"{dataLine.ProfileCount};{dataLine.RequestsPerSec};{filteredInput};{filteredDiff};{((reached) ? dataLine.ProfileCount : 0)}";
        }
    }
}
