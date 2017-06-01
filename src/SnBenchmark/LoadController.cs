using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace SnBenchmark
{
    public enum LoadControl {Stay, Increase, Decrease, Exit}
    public class LoadController
    {
        private enum State { Initial, Growing, MaxDetected, Decreasing, Increasing };
        private State _state = State.Initial;
        private int _counter;
        private readonly int _growingCounterMax = 30;

        readonly MaxPerformanceDetector _endpointCalc = new MaxPerformanceDetector(); //UNDONE: rename _endpointCalc
        private bool _firstMaxPerformanceDetected;
        private int _profilesWhenFirstMaxPerformanceDetected;
        public double FilteredRequestsPerSec => _endpointCalc.FilteredRequestsPerSec;
        public double DiffValue => _endpointCalc.CurrentValue;

        public void Progress(int requestsPerSec, int countOfRunningProfiles)
        {
            _counter++;
            if (_endpointCalc.Detect(requestsPerSec))
            {
                _firstMaxPerformanceDetected = true;
                _profilesWhenFirstMaxPerformanceDetected = countOfRunningProfiles;
            }
        }
        public LoadControl Next()
        {
            switch (_state)
            {
                case State.Initial:
                    _counter = 0;
                    _state=State.Growing;
                    return LoadControl.Stay;
                case State.Growing:
                    if (_counter < _growingCounterMax)
                        return LoadControl.Stay;
                    _counter = 0;
                    if (!_firstMaxPerformanceDetected)
                        return LoadControl.Increase;
                    _state = State.MaxDetected;
                    return LoadControl.Stay;
                case State.MaxDetected:
                    if (_counter < 200)
                        return LoadControl.Stay;
                    return LoadControl.Exit;
                case State.Decreasing:
                    break;
                case State.Increasing:
                    break;
                default:
                    throw new ArgumentOutOfRangeException("Unknown state: " + _state);
            }
            // ???

            return LoadControl.Stay;
        }
    }
}
