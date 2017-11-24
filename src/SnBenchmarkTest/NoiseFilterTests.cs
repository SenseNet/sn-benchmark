using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SnBenchmark;

namespace SnBenchmarkTest
{
    [TestClass]
    public class NoiseFilterTests
    {
        [TestMethod]
        public void NoiseFilter_AverageOfIncremental()
        {
            var length = 20;
            var size = 10;
            var expected = string.Join(";", Enumerable.Range(1, length)
                    .Select(x => Enumerable.Range(1, x).Skip(x <= size ? 0 : x - size).Average().ToString("0.00"))
                    .ToArray());

            var filter = new NoiseFilter(size);
            var values = new double[length];
            for (int i = 0; i < length; i++)
            {
                filter.NextValue(i + 1);
                values[i] = filter.FilteredValue;
            }
            var actual = string.Join(";", values.Select(x => x.ToString("0.00")).ToArray());

            Assert.AreEqual(expected, actual);
        }
        [TestMethod]
        public void NoiseFilter_AverageOfConstant()
        {
            var length = 20;
            var size = 10;
            var value = 42.01;
            var valueString = value.ToString("0.00");
            var expected = string.Join(";", Enumerable.Range(1, length).Select(x => valueString).ToArray());

            var filter = new NoiseFilter(size);

            var values = new double[length];
            for (int i = 0; i < length; i++)
            {
                filter.NextValue(value);
                values[i] = filter.FilteredValue;
            }
            var actual = string.Join(";", values.Select(x => x.ToString("0.00")).ToArray());

            Assert.AreEqual(expected, actual);
        }
    }
}
