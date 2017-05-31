using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Diagnostics;
using SNBCalc;

namespace SnBenchmarkTest.LoadControl
{
    [TestClass]
    public class WebServerSimulatorTests
    {
        [TestMethod]
        public void TestMethod1()
        {
            var server = new WebServerSimulator(40, 100);
            var filter = new NoiseFilter(100);
            var epc = new BenchmarkEndPointCalculator();
            for (int profiles = 1; profiles < 60; profiles++)
            {
                for (int iteration = 0; iteration < 50; iteration++)
                {
                    var reqPerSec = server.GetRequestPerSec(profiles + 1);
                    var filteredValue = filter.NextValue(reqPerSec);
                    var detected = epc.Detect(reqPerSec) ? 1 : 0;
                    Debug.WriteLine("{0}\t{1}\t{2}\t{3}\t{4}\t{5}", profiles, iteration, reqPerSec, filteredValue, epc.CurrentValue*200, detected*100);
                }
            }
        }
    }
}
