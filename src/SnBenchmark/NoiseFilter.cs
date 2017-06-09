using System;
using System.Linq;

namespace SnBenchmark
{
    public class NoiseFilter
    {
        private readonly double _qSize;
        private readonly double[] _buffer;
        private int _index;
        private bool _firstCycle = true;

        public double FilteredValue { get; private set; }
        public double MinValue => _buffer.Min();
        public double MaxValue => _buffer.Max();

        public NoiseFilter(int size)
        {
            _qSize = Convert.ToDouble(size);
            _buffer = new double[size];
        }

        public double NextValue(double value)
        {
            var last = _buffer[_index];
            _buffer[_index++] = value;

            if (_firstCycle)
            {
                FilteredValue = _buffer.Take(_index).Average();
                if (_index >= _buffer.Length)
                    _firstCycle = false;
            }
            else
            {
                FilteredValue += (value - last)/_qSize;
            }

            _index %= _buffer.Length;

            return FilteredValue;
        }
    }
}
