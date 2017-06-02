using System;
using System.Collections.Generic;
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
        public void WebServerSimulator_SustainPerformanceLoadController()
        {
            var loadController = new SustainPerformanceLoadController(600);
            TestLoadController(new WebServerSimulator(50, 100), loadController, 10, 4);
        }
        [TestMethod]
        public void WebServerSimulator_SawToothLoadController_Dec()
        {
            var loadController = new SawToothLoadController(true);
            TestLoadController(new WebServerSimulator(50, 100), loadController, 10, 4);
        }
        [TestMethod]
        public void WebServerSimulator_SawToothLoadController()
        {
            var loadController = new SawToothLoadController(false);
            TestLoadController(new WebServerSimulator(50, 100), loadController, 10, 4);
        }

        private void TestLoadController(WebServerSimulator server, LoadController loadController, int profiles, int growth)
        {
            var exit = false;

            Debug.WriteLine("Trigger\tr/s\tAVGr/s\tMPD\tProfiles");
            while (!exit)
            {
                var reqPerSec = server.GetRequestPerSec(profiles);
                loadController.Progress(reqPerSec, profiles);
                var filteredValue = loadController.FilteredRequestsPerSec;
                var diffValue = loadController.DiffValue;
                var detected = loadController.TopValueDetected ? 1 : 0;
                //Debug.WriteLine("{0}\t{1}\t{2}\t{3}\t{4}", profiles, reqPerSec, filteredValue, diffValue * 200, detected * 100);
                Debug.WriteLine("{0}\t{1}\t{2}\t{3}\t{4}", detected * 100, reqPerSec, filteredValue, diffValue * 200, profiles);

                var loadControl = loadController.Next();
                switch (loadControl)
                {
                    case LoadControl.Stay:
                        break;
                    case LoadControl.Exit:
                        exit = true;
                        break;
                    case LoadControl.Increase:
                        profiles += growth;
                        break;
                    case LoadControl.Decrease:
                        profiles -= growth;
                        break;
                    default:
                        throw new ArgumentOutOfRangeException("Unknown load control: " + loadControl);
                }
            }
        }

        [TestMethod]
        public void WebServerSimulator_ProfileFinderLoadController()
        {
            var result = new List<PerformanceRecord>();
            for (int i = 0; i < 20; i++)
            {
                var loadController = new ProfileFinderLoadController();
                TestLoadController(new WebServerSimulator(50, 60), loadController, 10, 1, false);
                result.Add(loadController.Result);
            }
        }
        [TestMethod]
        public void WebServerSimulator_ProfileFinderLoadController_Trace()
        {
            var loadController = new ProfileFinderLoadController();
            TestLoadController(new WebServerSimulator(50, 60), loadController, 10, 1, true);
            var result = loadController.Result;
        }
        private void TestLoadController(WebServerSimulator server, ProfileFinderLoadController loadController, int profiles, int growth, bool trace)
        {
            var exit = false;

            if (trace)
                Debug.WriteLine("Trigger\tr/s\tAVGr/s\tMPD\tProfiles\tTrace");

            while (!exit)
            {
                var reqPerSec = server.GetRequestPerSec(profiles);
                loadController.Progress(reqPerSec, profiles);
                var filteredValue = loadController.FilteredRequestsPerSec;
                var diffValue = loadController.DiffValue;
                var detected = loadController.TopValueDetected ? 1 : 0;

                if(trace)
                    Debug.WriteLine("{0}\t{1}\t{2}\t{3}\t{4}\t{5}", detected * 100, reqPerSec, filteredValue, diffValue * 200, profiles, loadController.Trace);

                var loadControl = loadController.Next();
                switch (loadControl)
                {
                    case LoadControl.Stay:
                        break;
                    case LoadControl.Exit:
                        exit = true;
                        break;
                    case LoadControl.Increase:
                        profiles += growth;
                        break;
                    case LoadControl.Decrease:
                        profiles -= growth;
                        break;
                    default:
                        throw new ArgumentOutOfRangeException("Unknown load control: " + loadControl);
                }
            }
        }

    }
}
