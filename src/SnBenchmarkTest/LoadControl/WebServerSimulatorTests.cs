using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Diagnostics;
using SnBenchmark;
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
            var epc = new BenchmarkEndPointCalculator();
            for (var profiles = 1; profiles < 60; profiles++)
            {
                for (int iteration = 0; iteration < 50; iteration++)
                {
                    var reqPerSec = server.GetRequestPerSec(profiles + 1);
                    var detected = epc.Detect(reqPerSec) ? 1 : 0;
                    var filteredValue = epc.FilteredRequestsPerSec;
                    Debug.WriteLine("{0}\t{1}\t{2}\t{3}\t{4}", profiles, reqPerSec, filteredValue, epc.CurrentValue * 200, detected * 100);
                }
            }
        }
        [TestMethod]
        public void TestMethod2()
        {
            var server = new WebServerSimulator(50, 100);
            var epc = new BenchmarkEndPointCalculator();
            var controller = new LoadController();
            var profiles = 10;
            var growingProfiles = 4;
            while (true)
            {
                var detected = false;
                for (int iteration = 0; iteration < 30; iteration++)
                {
                    var reqPerSec = server.GetRequestPerSec(profiles);
                    controller.Progress(reqPerSec);
                    detected = epc.Detect(reqPerSec);
                    var filteredValue = epc.FilteredRequestsPerSec;
                    Debug.WriteLine("{0}\t{1}\t{2}\t{3}\t{4}", profiles, reqPerSec, filteredValue, epc.CurrentValue * 200, detected ? 0 : 100);
                }
                var loadControl = controller.Next();
                switch (loadControl)
                {
                    case LoadControl.Stay:
                        break;
                    case LoadControl.Increase:
                        break;
                    case LoadControl.Decrease:
                        break;
                    case LoadControl.Exit:
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
        }
    }
}
