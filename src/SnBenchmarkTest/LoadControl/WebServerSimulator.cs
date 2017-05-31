using SnBenchmark;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SenseNet.Client;
using System.IO;
using System.Threading;

namespace SnBenchmarkTest.LoadControl
{
    internal class WebServerSimulator
    {
        private double _steepness = 5.0;
        private int _maxProfiles = 40;
        private int _maxRequestsPerSec = 100;

        private Random _rng = new Random();

        public WebServerSimulator(int maxProfiles, int maxRequestsPerSec)
        {
            _maxProfiles = maxProfiles;
            _maxRequestsPerSec = maxRequestsPerSec;
        }

        public int GetRequestPerSec(int profiles)
        {
            // =(1-1/EXP(A2/$F$2*$F$1))*$F$3
            var y = (1.0 - 1.0 / Math.Exp(_steepness * profiles / _maxProfiles)) * _maxRequestsPerSec;
            // 0.5 <= rnd < 1
            return Convert.ToInt32((1.0 - (_rng.NextDouble() / 3.0)) * y);
        }
    }
}
