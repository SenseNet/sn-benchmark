using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace SnBenchmark
{
    [DebuggerDisplay("{Profiles}, {AverageRequestsPerSec}")]
    public class PerformanceRecord
    {
        public int Profiles { get; set; }
        public string ProfileComposition { get; set; }
        public double AverageRequestsPerSec { get; set; }
        public string AverageResponseTime { get; set; }
    }

    public enum LoadControl { Stay, Increase, Decrease, Exit }
    public abstract class LoadController
    {
        protected enum State { Initial, Growing, MaxDetected, Decreasing, Increasing };
        protected State ControllerState = State.Initial;

        public int Counter { get; protected set; }
        protected readonly int GrowingCounterMax = 30;
        protected int CountOfRunningProfiles;
        protected string RunningProfileComposition;
        protected string AverageResponseTime;

        protected readonly MaxPerformanceDetector MaxPerformanceDetector= new MaxPerformanceDetector();
        protected List<PerformanceRecord> PerformanceTopValues = new List<PerformanceRecord>();
        public bool TopValueDetected { get; private set; }

        public double FilteredRequestsPerSec => MaxPerformanceDetector.FilteredRequestsPerSec;
        public double DiffValue => MaxPerformanceDetector.CurrentValue;

        public virtual void Progress(int requestsPerSec, int countOfRunningProfiles, string runningProfileComposition, string averageResponseTime)
        {
            Counter++;
            CountOfRunningProfiles = countOfRunningProfiles;
            RunningProfileComposition = runningProfileComposition;
            AverageResponseTime = averageResponseTime;
            // ReSharper disable once RedundantBoolCompare
            if ((TopValueDetected = MaxPerformanceDetector.Detect(requestsPerSec)) == true)
            {
                PerformanceTopValues.Add(new PerformanceRecord
                {
                    AverageRequestsPerSec = MaxPerformanceDetector.FilteredRequestsPerSec,
                    Profiles = countOfRunningProfiles,
                    ProfileComposition = runningProfileComposition,
                    AverageResponseTime = averageResponseTime
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
            switch (ControllerState)
            {
                case State.Initial:
                    Counter = 0;
                    ControllerState = State.Growing;
                    return LoadControl.Stay;
                case State.Growing:
                    if (Counter < GrowingCounterMax)
                        return LoadControl.Stay;
                    Counter = 0;
                    if (PerformanceTopValues.Count == 0)
                        return LoadControl.Increase;
                    ControllerState = State.MaxDetected;
                    return LoadControl.Stay;
                case State.MaxDetected:
                    if (Counter < _sustainCounterMax)
                        return LoadControl.Stay;
                    return LoadControl.Exit;
                default:
                    throw new ArgumentOutOfRangeException("Unknown state: " + ControllerState);
            }
        }
    }

    public class SawToothLoadController : LoadController
    {
        private readonly bool _sawToothStartsWithDecrement;

        private int _incStepMax = 5;
        private int _decStepMax;
        private int _sustainCounterMax = 200;
        private int _sawToothCount;
        private int _sawToothMax = 3;

        public SawToothLoadController(bool sawToothStartsWithDecrement)
        {
            _sawToothStartsWithDecrement = sawToothStartsWithDecrement;
        }

        public override LoadControl Next()
        {
            switch (ControllerState)
            {
                case State.Initial:
                    Counter = 0;
                    ControllerState = State.Growing;
                    return LoadControl.Stay;
                case State.Growing:
                    if (Counter < GrowingCounterMax)
                        return LoadControl.Stay;
                    Counter = 0;
                    if (PerformanceTopValues.Count == 0)
                        return LoadControl.Increase;
                    ControllerState = State.MaxDetected;
                    return LoadControl.Stay;
                case State.MaxDetected:
                    if (_sawToothStartsWithDecrement)
                    {
                        if (Counter < 200)
                            return LoadControl.Stay;
                        _decStepMax = 1;
                        Counter = 0;
                        ControllerState = State.Decreasing;
                        return LoadControl.Decrease;
                    }
                    if (Counter < 100)
                        return LoadControl.Stay;
                    Counter = 0;
                    _decStepMax = _incStepMax - 1;
                    ControllerState = State.Increasing;
                    return LoadControl.Stay;
                case State.Decreasing:
                    if (Counter >= _decStepMax)
                    {
                        Counter = 0;
                        _decStepMax = _incStepMax - 1;
                        ControllerState = State.Increasing;
                        return LoadControl.Stay;
                    }
                    return LoadControl.Decrease;
                case State.Increasing:
                    var modulo = Counter % _sustainCounterMax;
                    var incStepCount = Counter / _sustainCounterMax;

                    if (incStepCount < _incStepMax)
                        return modulo == 0 ? LoadControl.Increase : LoadControl.Stay;

                    //next sawtooth
                    Counter = 0;
                    if (++_sawToothCount >= _sawToothMax)
                        return LoadControl.Exit;
                    ControllerState = State.Decreasing;
                    return LoadControl.Decrease;
                default:
                    throw new ArgumentOutOfRangeException("Unknown state: " + ControllerState);
            }
        }
    }

    public class ProfileFinderLoadController : LoadController
    {
        private int _sustainCounterMax = 400;
        private double _performanceDeltaTrigger = 5.0;
        private int _incStepMax = 2;
        private readonly NoiseFilter _rpsFilter = new NoiseFilter(200);
        public readonly List<PerformanceRecord> AveragePerformanceHistory = new List<PerformanceRecord>();
        public double MaxPerformance { get; private set; }
        public int ProgressValue { get; private set; }

        public PerformanceRecord Result { get; private set; }
        public double Trace =>  _rpsFilter.FilteredValue;

        public override void Progress(int requestsPerSec, int countOfRunningProfiles, string runningProfileComposition, string averageResponseTime)
        {
            base.Progress(requestsPerSec, countOfRunningProfiles, runningProfileComposition, averageResponseTime);
            _rpsFilter.NextValue(MaxPerformanceDetector.FilteredRequestsPerSec);
        }

        public override LoadControl Next()
        {
            switch (ControllerState)
            {
                case State.Initial:
                    Counter = 0;
                    ProgressValue = Counter;
                    ControllerState = State.Growing;
                    return LoadControl.Stay;
                case State.Growing:
                    ProgressValue = GrowingCounterMax - Counter;
                    if (Counter < GrowingCounterMax)
                        return LoadControl.Stay;
                    Counter = 0;
                    ProgressValue = GrowingCounterMax - Counter;
                    if (PerformanceTopValues.Count == 0)
                        return LoadControl.Increase;
                    ControllerState = State.MaxDetected;
                    return LoadControl.Stay;
                case State.MaxDetected:
                    ProgressValue = GrowingCounterMax * 2 - Counter;
                    if (Counter < GrowingCounterMax * 2)
                        return LoadControl.Stay;
                    Counter = 0;
                    ProgressValue = 0;
                    ControllerState = State.Increasing;
                    return LoadControl.Increase;
                case State.Increasing:
                    var currentAvg = _rpsFilter.FilteredValue;
                    var modulo = Counter % (GrowingCounterMax * 4);
                    var incStepCount = Counter / (GrowingCounterMax * 4);
                    ProgressValue = GrowingCounterMax * 4 - modulo;
                    if (incStepCount < _incStepMax)
                    {
                        if( modulo > 0 )
                            return LoadControl.Stay;
                        AveragePerformanceHistory.Add(new PerformanceRecord
                        {
                            AverageRequestsPerSec = currentAvg,
                            Profiles = CountOfRunningProfiles,
                            ProfileComposition = RunningProfileComposition,
                            AverageResponseTime = AverageResponseTime
                        });
                        return LoadControl.Increase;
                    }
                    AveragePerformanceHistory.Add(new PerformanceRecord
                    {
                        AverageRequestsPerSec = currentAvg,
                        Profiles = CountOfRunningProfiles,
                        ProfileComposition = RunningProfileComposition,
                        AverageResponseTime = AverageResponseTime
                    });
                    MaxPerformance = AveragePerformanceHistory.Max(x => x.AverageRequestsPerSec);
                    Counter = -1;
                    ProgressValue = 0;
                    ControllerState = State.Decreasing;
                    return LoadControl.Stay;
                case State.Decreasing:
                    modulo = Counter % _sustainCounterMax;
                    ProgressValue = _sustainCounterMax - modulo;
                    //incStepCount = _counter / _sustainCounterMax;
                    currentAvg = _rpsFilter.FilteredValue;
                    if (modulo > 0)
                        return LoadControl.Stay;
                    var last = AveragePerformanceHistory[AveragePerformanceHistory.Count - 1];
                    AveragePerformanceHistory.Add(new PerformanceRecord
                    {
                        AverageRequestsPerSec = currentAvg,
                        Profiles = CountOfRunningProfiles,
                        ProfileComposition = RunningProfileComposition,
                        AverageResponseTime = AverageResponseTime
                    });
                    if (MaxPerformance - currentAvg < _performanceDeltaTrigger)
                        return LoadControl.Decrease;
                    Result = last;
                    return LoadControl.Exit;
                default:
                    throw new ArgumentOutOfRangeException("Unknown state: " + ControllerState);
            }
        }
    }

    public class ProfileFinderLoadController2 : LoadController
    {
        private int _sustainCounterMax = 320; // noise filter length (100 + 200) + safety
        private readonly NoiseFilter _rpsFilter = new NoiseFilter(200);
        public readonly List<PerformanceRecord> AveragePerformanceHistory = new List<PerformanceRecord>();
        public double MaxPerformance { get; private set; }
        public double ExpectedPerformance { get; private set; }

        public int ProgressValue { get; private set; }

        public PerformanceRecord Result { get; private set; }
        public double Trace => _rpsFilter.FilteredValue;

        public override void Progress(int requestsPerSec, int countOfRunningProfiles, string runningProfileComposition, string averageResponseTime)
        {
            base.Progress(requestsPerSec, countOfRunningProfiles, runningProfileComposition, averageResponseTime);
            _rpsFilter.NextValue(MaxPerformanceDetector.FilteredRequestsPerSec);
        }

        public override LoadControl Next()
        {
            double currentAvg;
            switch (ControllerState)
            {
                case State.Initial:
                    Counter = 0;
                    ProgressValue = Counter;
                    ControllerState = State.Growing;
                    return LoadControl.Stay;
                case State.Growing:
                    ProgressValue = GrowingCounterMax - Counter;
                    if (Counter < GrowingCounterMax)
                        return LoadControl.Stay;

                    Counter = 0;
                    ProgressValue = GrowingCounterMax - Counter;
                    currentAvg = _rpsFilter.FilteredValue;
                    AveragePerformanceHistory.Add(new PerformanceRecord
                    {
                        AverageRequestsPerSec = currentAvg,
                        Profiles = CountOfRunningProfiles,
                        ProfileComposition = RunningProfileComposition,
                        AverageResponseTime = AverageResponseTime
                    });
                    if (PerformanceTopValues.Count == 0)
                        return LoadControl.Increase;

                    ControllerState = State.MaxDetected;
                    return LoadControl.Stay;
                case State.MaxDetected:
                    ProgressValue = _sustainCounterMax * 2 - Counter;
                    if (Counter < _sustainCounterMax * 2)
                        return LoadControl.Stay;

                    Counter = 0;
                    ProgressValue = 0;
                    currentAvg = _rpsFilter.FilteredValue;
                    AveragePerformanceHistory.Add(new PerformanceRecord
                    {
                        AverageRequestsPerSec = currentAvg,
                        Profiles = CountOfRunningProfiles,
                        ProfileComposition = RunningProfileComposition,
                        AverageResponseTime = AverageResponseTime
                    });
                    MaxPerformance = AveragePerformanceHistory.Max(x => x.AverageRequestsPerSec);
                    ExpectedPerformance = CalculateSweetPoint(_rpsFilter.MinValue, currentAvg, _rpsFilter.MaxValue);

                    ControllerState = State.Decreasing;
                    return LoadControl.Decrease;
                case State.Decreasing:
                    var modulo = Counter % _sustainCounterMax;
                    ProgressValue = _sustainCounterMax - modulo;
                    currentAvg = _rpsFilter.FilteredValue;
                    if (modulo > 0)
                        return LoadControl.Stay;

                    var last = AveragePerformanceHistory[AveragePerformanceHistory.Count - 1];
                    AveragePerformanceHistory.Add(new PerformanceRecord
                    {
                        AverageRequestsPerSec = currentAvg,
                        Profiles = CountOfRunningProfiles,
                        ProfileComposition = RunningProfileComposition,
                        AverageResponseTime = AverageResponseTime
                    });
                    if (currentAvg > MaxPerformance)
                    {
                        MaxPerformance = currentAvg;
                        ExpectedPerformance = CalculateSweetPoint(_rpsFilter.MinValue, currentAvg, _rpsFilter.MaxValue);
                    }

                    // result available anytime in the decreasing phase
                    Result = last;

                    if (currentAvg > ExpectedPerformance)
                        return LoadControl.Decrease;

                    return LoadControl.Exit;
                default:
                    throw new ArgumentOutOfRangeException("Unused state: " + ControllerState);
            }
        }

        private double CalculateSweetPoint(double min, double avg, double max)
        {
            //return avg * 0.99 - (max - min);
            return avg*0.95;
        }
    }
}
