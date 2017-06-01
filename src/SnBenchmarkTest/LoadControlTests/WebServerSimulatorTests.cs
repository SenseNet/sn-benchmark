using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Diagnostics;
using SnBenchmark;

namespace SnBenchmarkTest.LoadControlTests
{
    [TestClass]
    public class WebServerSimulatorTests
    {
        [TestMethod]
        public void TestMethod1()
        {
            var server = new WebServerSimulator(40, 100);
            var mpd = new MaxPerformanceDetector();
            for (var profiles = 1; profiles < 60; profiles++)
            {
                for (int iteration = 0; iteration < 50; iteration++)
                {
                    var reqPerSec = server.GetRequestPerSec(profiles + 1);
                    var detected = mpd.Detect(reqPerSec) ? 1 : 0;
                    var filteredValue = mpd.FilteredRequestsPerSec;
                    Debug.WriteLine("{0}\t{1}\t{2}\t{3}\t{4}", profiles, reqPerSec, filteredValue, mpd.CurrentValue * 200, detected * 100);
                }
            }
        }
        [TestMethod]
        public void TestMethod2()
        {
            var server = new WebServerSimulator(50, 100);
            var loadController = new LoadController();

            var profiles = 10;
            var growingProfiles = 4;
            var exit = false;
            while (!exit)
            {
                var reqPerSec = server.GetRequestPerSec(profiles);
                loadController.Progress(reqPerSec, profiles);
                var filteredValue = loadController.FilteredRequestsPerSec;
                var diffValue = loadController.DiffValue;
                Debug.WriteLine("{0}\t{1}\t{2}\t{3}", profiles, reqPerSec, filteredValue, diffValue * 200);

                var loadControl = loadController.Next();
                switch (loadControl)
                {
                    case LoadControl.Stay:
                        break;
                    case LoadControl.Exit:
                        exit = true;
                        break;
                    case LoadControl.Increase:
                        profiles += growingProfiles;
                        break;
                    case LoadControl.Decrease:
                        profiles -= growingProfiles;
                        break;
                    default:
                        throw new ArgumentOutOfRangeException("Unknown load control: " + loadControl);
                }
            }
        }
    }
}
