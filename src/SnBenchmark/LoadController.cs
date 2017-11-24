using System;
using System.Collections.Generic;
using System.Linq;

namespace SnBenchmark
{
    public class LoadController : LoadControllerBase
    {
        private int _sustainCounterMax = 320; // noise filter length (100 + 200) + safety
        private readonly NoiseFilter _rpsFilter = new NoiseFilter(200);
        public readonly List<PerformanceRecord> AveragePerformanceHistory = new List<PerformanceRecord>();
        public double MaxPerformance { get; private set; }
        public double ExpectedPerformance { get; private set; }

        public int ProgressValue { get; private set; }

        public PerformanceRecord Result { get; private set; }
        public double Trace => _rpsFilter.FilteredValue;

        public LoadController(int growingTime) : base(growingTime) { }

        public override void Progress(int requestsPerSec, int countOfRunningProfiles, string runningProfileComposition, string averageResponseTime)
        {
            base.Progress(requestsPerSec, countOfRunningProfiles, runningProfileComposition, averageResponseTime);
            _rpsFilter.NextValue(MaxPerformanceDetector.FilteredRequestsPerSec);
        }

        public override LoadControlCommand Next()
        {
            double currentPerformance;
            switch (ControllerState)
            {
                case State.Initial:
                    Counter = 0;
                    ProgressValue = Counter;
                    ControllerState = State.Growing;
                    return LoadControlCommand.Stay;
                case State.Growing:
                    ProgressValue = GrowingCounterMax - Counter;
                    if (Counter < GrowingCounterMax)
                        return LoadControlCommand.Stay;

                    Counter = 0;
                    ProgressValue = GrowingCounterMax - Counter;
                    currentPerformance = _rpsFilter.FilteredValue;
                    AveragePerformanceHistory.Add(new PerformanceRecord
                    {
                        AverageRequestsPerSec = currentPerformance,
                        Profiles = CountOfRunningProfiles,
                        ProfileComposition = RunningProfileComposition,
                        AverageResponseTime = AverageResponseTime
                    });
                    if (PerformanceTopValues.Count == 0)
                        return LoadControlCommand.Increase;

                    ControllerState = State.MaxDetected;
                    return LoadControlCommand.Stay;
                case State.MaxDetected:
                    ProgressValue = _sustainCounterMax - Counter;
                    if (Counter < _sustainCounterMax)
                        return LoadControlCommand.Stay;

                    Counter = 0;
                    ProgressValue = 0;
                    currentPerformance = _rpsFilter.FilteredValue;
                    AveragePerformanceHistory.Add(new PerformanceRecord
                    {
                        AverageRequestsPerSec = currentPerformance,
                        Profiles = CountOfRunningProfiles,
                        ProfileComposition = RunningProfileComposition,
                        AverageResponseTime = AverageResponseTime
                    });
                    MaxPerformance = AveragePerformanceHistory.Max(x => x.AverageRequestsPerSec);
                    ExpectedPerformance = CalculateSweetPoint(currentPerformance);

                    ControllerState = State.Decreasing;
                    return LoadControlCommand.Decrease;
                case State.Decreasing:
                    var modulo = Counter % _sustainCounterMax;
                    ProgressValue = _sustainCounterMax - modulo;
                    currentPerformance = _rpsFilter.FilteredValue;
                    if (modulo > 0)
                        return LoadControlCommand.Stay;

                    var last = AveragePerformanceHistory[AveragePerformanceHistory.Count - 1];
                    AveragePerformanceHistory.Add(new PerformanceRecord
                    {
                        AverageRequestsPerSec = currentPerformance,
                        Profiles = CountOfRunningProfiles,
                        ProfileComposition = RunningProfileComposition,
                        AverageResponseTime = AverageResponseTime
                    });
                    if (currentPerformance > MaxPerformance)
                    {
                        MaxPerformance = currentPerformance;
                        ExpectedPerformance = CalculateSweetPoint(currentPerformance);
                    }

                    // result available anytime in the decreasing phase
                    Result = last;

                    if (currentPerformance > ExpectedPerformance)
                        return LoadControlCommand.Decrease;

                    return LoadControlCommand.Exit;
                default:
                    throw new ArgumentOutOfRangeException("Unused state: " + ControllerState);
            }
        }

        private double CalculateSweetPoint(double avg)
        {
            return avg*0.95;
        }
    }
}
