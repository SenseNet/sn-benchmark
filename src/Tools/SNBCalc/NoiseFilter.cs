using System;

namespace SNBCalc
{
    internal class NoiseFilter<T> where T : IConvertible
    {
        private readonly double _qSize;
        private readonly T[] _buffer;
        private int _index;
        public double FilteredValue { get; private set; }

        public NoiseFilter(int size)
        {
            _qSize = Convert.ToDouble(size);
            _buffer = new T[size];
        }

        public double NextValue(T value)
        {
            var last = _buffer[_index];
            _buffer[_index] = value;
            _index = (_index + 1) % _buffer.Length;

            FilteredValue += Minus(value, last) / _qSize;

            return FilteredValue;
        }

        private double Minus(T a, T b)
        {
            if (a is Int32)
                return Convert.ToDouble(Convert.ToInt32(a) - Convert.ToInt32(b));
            if (a is double)
                return Convert.ToDouble(a) - Convert.ToDouble(b);
            throw new NotImplementedException();
        }
    }
}
