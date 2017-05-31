using System;

namespace SnBenchmarkTest.LoadControl
{
    internal class NoiseFilter
    {
        private readonly double _qSize;
        private readonly double[] _buffer;
        private int _index;
        public double FilteredValue { get; private set; }

        public NoiseFilter(int size)
        {
            _qSize = Convert.ToDouble(size);
            _buffer = new double[size];
        }

        public double NextValue(double value)
        {
            var last = _buffer[_index];
            _buffer[_index] = value;
            _index = (_index + 1) % _buffer.Length;

            FilteredValue += (value - last) / _qSize;

            return FilteredValue;
        }
    }
}
