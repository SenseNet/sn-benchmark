using System.Collections.Generic;
using System.Diagnostics;

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

    public enum LoadControlCommand { Stay, Increase, Decrease, Exit }

    public abstract class LoadControllerBase
    {
        protected enum State { Initial, Growing, MaxDetected, Decreasing, Increasing };
        protected State ControllerState = State.Initial;

        public int Counter { get; protected set; }
        protected readonly int GrowingCounterMax;
        protected int CountOfRunningProfiles;
        protected string RunningProfileComposition;
        protected string AverageResponseTime;

        protected readonly MaxPerformanceDetector MaxPerformanceDetector = new MaxPerformanceDetector();
        protected List<PerformanceRecord> PerformanceTopValues = new List<PerformanceRecord>();
        public bool TopValueDetected { get; private set; }

        protected LoadControllerBase(int growingTime)
        {
            GrowingCounterMax = growingTime;
        }

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

        public abstract LoadControlCommand Next();
    }
}
