using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SnBenchmark
{
    public class MaxPerformanceDetector
    {
        private readonly NoiseFilter _inputFilter = new NoiseFilter(100);
        private readonly NoiseFilter _diffFilter = new NoiseFilter(50);
        private int trigger = 10;
        private int _continuousNegative;

        public double CurrentValue { get; private set; }
        public double FilteredRequestsPerSec { get; private set; }

        public bool Detect(int requestsPerSec)
        {
            var lastFilteredInput = _inputFilter.FilteredValue;
            var filteredInput = _inputFilter.NextValue(requestsPerSec);
            FilteredRequestsPerSec = filteredInput;

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
