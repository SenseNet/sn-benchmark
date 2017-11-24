using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using SnBenchmark;

namespace SnBenchmarkTest.LoadControlTests
{
    [TestClass]
    public class WebServerSimulatorTests
    {
        [TestMethod]
        public void WebServerSimulator_LoadController()
        {
            var result = new List<PerformanceRecord>();
            for (int i = 0; i < 1000; i++)
            {
                var loadController = new LoadController(30);
                TestLoadController(new WebServerSimulator(50, 60), loadController, 10, 1, false);
                result.Add(loadController.Result);
            }
            var min = result.Min(x => x.Profiles);
            var max = result.Max(x => x.Profiles);
            //using (var writer = new StreamWriter(@"D:\test.txt"))
            //    foreach (var item in result)
            //        writer.WriteLine($"{item.Profiles}\t{item.AverageRequestsPerSec.ToString(CultureInfo.InvariantCulture).Replace(".", ",")}");
        }
        [TestMethod]
        public void WebServerSimulator_LoadController_Trace()
        {
            var loadController = new LoadController(30);
            TestLoadController(new WebServerSimulator(50, 60), loadController, 10, 1, true);
            var result = loadController.Result;
        }
        private void TestLoadController(WebServerSimulator server, LoadController loadController, int profiles, int growth, bool trace)
        {
            var exit = false;

            if (trace)
                Debug.WriteLine("Trigger\tr/s\tAVGr/s\tMPD\tProfiles\tTrace");

            while (!exit)
            {
                var reqPerSec = server.GetRequestPerSec(profiles);
                loadController.Progress(reqPerSec, profiles, $"Profile1: {profiles}", "NORMAL: 0.42");
                var filteredValue = loadController.FilteredRequestsPerSec;
                var diffValue = loadController.DiffValue;
                var detected = loadController.TopValueDetected ? 1 : 0;

                if (trace)
                    Debug.WriteLine("{0}\t{1}\t{2}\t{3}\t{4}\t{5}", detected * 100, reqPerSec, filteredValue, diffValue * 200, profiles, loadController.Trace);

                var loadControl = loadController.Next();
                switch (loadControl)
                {
                    case LoadControlCommand.Stay:
                        break;
                    case LoadControlCommand.Exit:
                        exit = true;
                        break;
                    case LoadControlCommand.Increase:
                        profiles += growth;
                        break;
                    case LoadControlCommand.Decrease:
                        profiles -= growth;
                        break;
                    default:
                        throw new ArgumentOutOfRangeException("Unknown load control: " + loadControl);
                }
            }
        }
    }
}
