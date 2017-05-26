namespace SNBCalc
{
    public class BenchmarkEndPointCalculator
    {
        private readonly NoiseFilter<int> _inputFilter = new NoiseFilter<int>(100);
        private readonly NoiseFilter<double> _diffFilter = new NoiseFilter<double>(50);
        private int trigger = 10;
        private int _continuousNegative;

        public double CurrentValue { get; private  set; }

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
        public bool Detect(int requestsPerSec)
        {
            var lastFilteredInput = _inputFilter.FilteredValue;
            var filteredInput = _inputFilter.NextValue(requestsPerSec);

            var diff = filteredInput - lastFilteredInput;

            var filteredDiff = _diffFilter.NextValue(diff);
            CurrentValue = filteredDiff;

            if (filteredDiff < 0.0)
                _continuousNegative++;
            else
                _continuousNegative = 0;

            var triggered = _continuousNegative >= trigger;
            if (triggered)
                _continuousNegative = 0;

            return triggered;
        }
    }
}
