using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace SnBenchmark
{
    [DebuggerDisplay("{Profiles}\t{AverageRequestsPerSec}")]
    public class PerformanceRecord
    {
        public double AverageRequestsPerSec { get; set; }
        public int Profiles { get; set; }
    }

    public enum LoadControl { Stay, Increase, Decrease, Exit }
    public abstract class LoadController
    {
        protected enum State { Initial, Growing, MaxDetected, Decreasing, Increasing };
        protected State _state = State.Initial;

        protected int _counter;
        protected readonly int _growingCounterMax = 30;

        protected readonly MaxPerformanceDetector _maxPerformanceDetector= new MaxPerformanceDetector();
        protected List<PerformanceRecord> _performanceTopValues = new List<PerformanceRecord>();
        public bool TopValueDetected { get; private set; }

        public double FilteredRequestsPerSec => _maxPerformanceDetector.FilteredRequestsPerSec;
        public double DiffValue => _maxPerformanceDetector.CurrentValue;

        public void Progress(int requestsPerSec, int countOfRunningProfiles)
        {
            _counter++;
            // ReSharper disable once RedundantBoolCompare
            if ((TopValueDetected = _maxPerformanceDetector.Detect(requestsPerSec)) == true)
            {
                _performanceTopValues.Add(new PerformanceRecord
                {
                    AverageRequestsPerSec = _maxPerformanceDetector.FilteredRequestsPerSec,
                    Profiles = countOfRunningProfiles
                });
            }
        }

        public abstract LoadControl Next();
    }

    public class SustainPerformanceLoadController : LoadController
    {
        private readonly int _sustainCounterMax;

        public SustainPerformanceLoadController(int sustainInSec)
        {
            _sustainCounterMax = sustainInSec;
        }

        public override LoadControl Next()
        {
            switch (_state)
            {
                case State.Initial:
                    _counter = 0;
                    _state = State.Growing;
                    return LoadControl.Stay;
                case State.Growing:
                    if (_counter < _growingCounterMax)
                        return LoadControl.Stay;
                    _counter = 0;
                    if (_performanceTopValues.Count == 0)
                        return LoadControl.Increase;
                    _state = State.MaxDetected;
                    return LoadControl.Stay;
                case State.MaxDetected:
                    if (_counter < _sustainCounterMax)
                        return LoadControl.Stay;
                    return LoadControl.Exit;
                default:
                    throw new ArgumentOutOfRangeException("Unknown state: " + _state);
            }
        }
    }

    public class SawToothLoadController : LoadController
    {
        protected bool _sawToothStartsWithDecrement;

        protected int _incStepMax = 5;
        protected int _decStepMax;
        protected int _sustainCounterMax = 200;
        protected int _sawToothCount;
        protected int _sawToothMax = 3;

        public SawToothLoadController(bool sawToothStartsWithDecrement)
        {
            _sawToothStartsWithDecrement = sawToothStartsWithDecrement;
        }

        public override LoadControl Next()
        {
            switch (_state)
            {
                case State.Initial:
                    _counter = 0;
                    _state = State.Growing;
                    return LoadControl.Stay;
                case State.Growing:
                    if (_counter < _growingCounterMax)
                        return LoadControl.Stay;
                    _counter = 0;
                    if (_performanceTopValues.Count == 0)
                        return LoadControl.Increase;
                    _state = State.MaxDetected;
                    return LoadControl.Stay;
                case State.MaxDetected:
                    if (_sawToothStartsWithDecrement)
                    {
                        if (_counter < 200)
                            return LoadControl.Stay;
                        _decStepMax = 1;
                        _counter = 0;
                        _state = State.Decreasing;
                        return LoadControl.Decrease;
                    }
                    if (_counter < 100)
                        return LoadControl.Stay;
                    _counter = 0;
                    _decStepMax = _incStepMax - 1;
                    _state = State.Increasing;
                    return LoadControl.Stay;
                case State.Decreasing:
                    if (_counter >= _decStepMax)
                    {
                        _counter = 0;
                        _decStepMax = _incStepMax - 1;
                        _state = State.Increasing;
                        return LoadControl.Stay;
                    }
                    return LoadControl.Decrease;
                case State.Increasing:
                    var modulo = _counter % _sustainCounterMax;
                    var incStepCount = _counter / _sustainCounterMax;

                    if (incStepCount < _incStepMax)
                        return modulo == 0 ? LoadControl.Increase : LoadControl.Stay;

                    //next sawtooth
                    _counter = 0;
                    if (++_sawToothCount >= _sawToothMax)
                        return LoadControl.Exit;
                    _state = State.Decreasing;
                    return LoadControl.Decrease;
                default:
                    throw new ArgumentOutOfRangeException("Unknown state: " + _state);
            }
            throw new InvalidOperationException();
        }
    }
}
