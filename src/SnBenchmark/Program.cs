using SenseNet.Client;
using SnBenchmark.Expression;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using SenseNet.Tools.CommandLineArguments;

namespace SnBenchmark
{
    internal class Program
    {
        private enum MainState { Initial, Warmup, Measuring, Cooldown }

        private static Configuration _configuration;
        private static List<string> _speedItems;
        private static Dictionary<string, double> _averageResponseTime;
        private static string _averageResponseTimeString;

        private static void Main(string[] args)
        {
            ServicePointManager.DefaultConnectionLimit = 1500;

            _configuration = new Configuration();

            try
            {
                var result = ArgumentParser.Parse(args, _configuration);
                if (result.IsHelp)
                {
                    Console.WriteLine(result.GetHelpText());
                }
                else
                {
                    Console.WriteLine(result.GetAppNameAndVersion());
                    Console.WriteLine();
                    Run();
                }
            }
            catch (ParsingException e)
            {
                Console.WriteLine(e.FormattedMessage);
                Console.WriteLine(e.Result.GetHelpText());
            }

            // ReSharper disable once InvertIf
            if (Debugger.IsAttached)
            {
                Console.WriteLine("Press <enter> to exit");
                Console.ReadLine();
            }
        }

        private static void Run()
        {
            ClientContext.Initialize(
                _configuration.SiteUrls
                .Select(u => new ServerContext
                {
                    Url = u,
                    Username = _configuration.UserName,
                    Password = _configuration.Password
                })
                .ToArray()
            );

            Console.WriteLine("Initializing profiles.");

            IDictionary<string, PathSetExpression[]> pathSetExpressionsByProfiles;
            IDictionary<string, PathSetExpression[]> psExprs2;
            var initial = InitializeProfiles(_configuration.InitialProfiles, out pathSetExpressionsByProfiles);
            var growing = InitializeProfiles(_configuration.GrowingProfiles, out psExprs2);

            Console.WriteLine("Ok.");
            Console.WriteLine("Initializing path sets.");

            foreach (var profileItem in pathSetExpressionsByProfiles)
            {
                foreach (var pathSetExpression in profileItem.Value)
                {
                    Console.Write($"  Getting paths: {profileItem.Key}.{ pathSetExpression.Name} ... ");
                    var pathSet = PathSet.Create(profileItem.Key, pathSetExpression.Name, pathSetExpression.Definition);
                    if (_configuration.TestOnly)
                        SavePathSet(pathSet);
                    Console.WriteLine($"Ok. Count: {pathSet.Paths.Length}");
                }
            }

            Console.WriteLine("Start.");

            EnsureOutputFile(_configuration);
            if (_configuration.TestOnly)
                TestProfiles(initial);
            else
                Run(initial, growing).Wait();

            WriteRequestLog();

            Console.WriteLine("Finished.                           ");
        }

        private static void SavePathSet(PathSet pathSet)
        {
            var profileDir = EnsureProfileResponsesDirectory(pathSet.ProfileName);
            var pathSetPath = Path.Combine(profileDir, $"{pathSet.Name}.pathset");
            using (var writer = new StreamWriter(pathSetPath))
                foreach (var path in pathSet.Paths)
                    writer.WriteLine(path);
        }

        private static string EnsureProfileResponsesDirectory(string profileName)
        {
            var dir = _configuration.ResponsesDirectoryPath;
            var profileDir = Path.Combine(dir, profileName);
            if (!Directory.Exists(profileDir))
                Directory.CreateDirectory(profileDir);
            return profileDir;
        }

        private static List<Profile> InitializeProfiles(Dictionary<string, int> config,
            out IDictionary<string, PathSetExpression[]> pathSetExpressions)
        {
            var profilesDirectory = Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Profiles"));
            if(!Directory.Exists(profilesDirectory))
                profilesDirectory = Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "../../Profiles"));
            if (!Directory.Exists(profilesDirectory))
                throw new ApplicationException("Profiles directory not found.");

            var profiles = new List<Profile>();
            var speedItems = new List<string> { RequestExpression.NormalSpeed };
            pathSetExpressions = new Dictionary<string, PathSetExpression[]>();

            // Iterate through all configured profile types and add the requested
            // number of initial profile objects of that profile type to the list.
            foreach (var item in config)
            {
                var name = item.Key;
                var count = item.Value;

                var src = LoadProfile(name, profilesDirectory);
                var profile = Profile.Parse(name, src, profilesDirectory, speedItems);

                IEnumerable<PathSetExpression> psExpr;
                Profile.GetPathSets(profile, out psExpr);
                pathSetExpressions.Add(profile.Name, psExpr.ToArray());

                // add a configured number of initial profiles of this type to the list
                for (var i = 0; i < count; i++)
                    profiles.Add(profile.Clone());
            }

            _speedItems = speedItems;
            _averageResponseTime = new Dictionary<string, double>();
            foreach (var key in speedItems)
                _averageResponseTime.Add(key, 0.0);
            AverageResponseTimeToString();

            Web.Initialize(speedItems);

            return profiles;
        }
        private static string LoadProfile(string name, string profilesDirectory)
        {
            var profileFiles = Directory.GetFiles(profilesDirectory, name + ".*");
            if (profileFiles.Length == 0)
                throw new ApplicationException("Profile not found: " + name);
            if (profileFiles.Length > 1)
                throw new ApplicationException("More than one profiles found: " + name);

            using (var reader = new StreamReader(profileFiles[0]))
                return reader.ReadToEnd();
        }

        private static readonly Dictionary<string, int> CurrentProfileComposition = new Dictionary<string, int>();
        private static object _currentProfilesLock = new object();
        private static string _currentProfileCompositionString;
        private static readonly List<Profile> RunningProfiles = new List<Profile>();
        private static List<Profile> _growingProfiles;

        private static System.Timers.Timer _timer;

        private static readonly object ErrorSync = new object();
        private static MainState _mainState;
        private static int _errorCount;
        private static int _errorCountInWarmup;
        public static void AddError(Exception e, Profile profile, int actionIndex, BenchmarkActionExpression action)
        {
            lock (ErrorSync)
            {
                WriteErrorToLog(e, profile, actionIndex, action);
                if (_mainState == MainState.Warmup)
                {
                    _errorCountInWarmup++;
                    Console.WriteLine(e.Message);
                }
                else
                {
                    _errorCount++;
                    Console.WriteLine("{0}/{1}: {2}", +_errorCount, _configuration.MaxErrors, e.Message);
                }
            }
        }

        private static async Task Run(List<Profile> initialProfiles, List<Profile> growingProfiles)
        {
            _growingProfiles = growingProfiles;

            _timer = new System.Timers.Timer(1000.0);
            _timer.Elapsed += Timer_Elapsed;
            _timer.Disposed += Timer_Disposed;
            _timer.Enabled = true;

            WriteColumnHeaders(_speedItems);

            // begin with starting the configured number of initial profiles as a warmup
            AddAndStartProfiles(initialProfiles);
            _mainState = MainState.Warmup;

            await Task.Delay(_configuration.WarmupTime * 1000);

            Console.WriteLine("================= MEASUREMENT  Press <x> to exit");

            Web.RequestsPerSec = 0;
            _mainState = MainState.Measuring;
            _loadController = new LoadController(_configuration.GrowingTime);

            // wait for the benchmark finished
            while (!_finished)
                await Task.Delay(1000);
            _mainState = MainState.Cooldown;

            var result = _loadController.Result;
            if (result != null)
            {
                var benchmarkResult = FormatBenchmarkResult(result);
                Console.WriteLine(benchmarkResult);
                WriteToOutputFile(benchmarkResult);
                WriteToOutputFile($"Max performance (RPS);{_loadController.MaxPerformance}");
                WriteToOutputFile($"Sweet point (RPS);{_loadController.ExpectedPerformance}");
            }

            // wait for profiles that are still running to stop
            await ShutdownProfiles();

            _timer.Stop();
        }

        private static void AddAndStartProfiles(List<Profile> profiles)
        {
            foreach (var profile in profiles)
            {
                int actualCount;
                if (!CurrentProfileComposition.TryGetValue(profile.Name, out actualCount))
                    CurrentProfileComposition[profile.Name] = 0;

                var limit = _configuration.ProfileLimits[profile.Name];
                if (limit > 0 && actualCount >= limit)
                    continue;

                CurrentProfileComposition[profile.Name]++;

                // clone the profile to avoid modifying the original version
                var newProfile = profile.Clone();
                RunningProfiles.Add(newProfile);

                // start executing the profile but do not wait for it to complete
#pragma warning disable CS4014
                newProfile.ExecuteAsync();
#pragma warning restore CS4014

            }
            CurrentProfileCompositionToString();
        }
        private static void StopProfiles(List<Profile> profiles)
        {
            foreach (var profile in profiles)
                RunningProfiles.FirstOrDefault(p=>p.Name == profile.Name && p.Running)?.Stop();
        }
        private static async Task ShutdownProfiles()
        {
            foreach (var runningProfile in RunningProfiles)
                runningProfile.Stop();

            var lastCounts = new Queue<int>();
            while (true)
            {
                var running = RunningProfiles.Count;
                if (running == 0)
                    break;

                lastCounts.Enqueue(running);
                if (lastCounts.Count > 30)
                {
                    lastCounts.Dequeue();
                    if (lastCounts.First() == lastCounts.Last())
                        break;
                }

                await Task.Delay(1000);
            }
        }
        public static void ProfileStopped(Profile profile)
        {
            RunningProfiles.Remove(profile);
            CurrentProfileComposition[profile.Name]--;
            CurrentProfileCompositionToString();
        }

        private static void CurrentProfileCompositionToString()
        {
            lock (_currentProfilesLock)
                _currentProfileCompositionString = string.Join("; ",
                    CurrentProfileComposition.Select(x => $"{x.Key}: {x.Value}").ToArray());
        }

        private static void TestProfiles(List<Profile> profiles)
        {
            foreach (var profile in profiles)
                profile.Test(EnsureProfileResponsesDirectory(profile.Name));
        }

        // =========================================================== Write result output

        private static string FormatBenchmarkResult(PerformanceRecord result)
        {
            return $"BENCHMARK RESULT: Profiles: {result.Profiles} ({result.ProfileComposition}); " +
                   $"RPS: {result.AverageRequestsPerSec:0.####}; " +
                   $"All requests: {Web.AllRequests}; " +
                   $"Errors: {_errorCount + _errorCountInWarmup}; " +
                   $"Response times: {result.AverageResponseTime}";
        }
        private static void WriteColumnHeaders(IEnumerable<string> speedItems)
        {
            var speeds = speedItems.ToArray();
            var outputHead = "Pcount;Active;RPS;RPSavg;RPSavg2;RPSdiff;Trigger;" + string.Join(";", speeds);
            WriteToOutputFile(outputHead);
        }

        // ===================================================================== Clock

        private static void Timer_Disposed(object sender, EventArgs e)
        {
            _timer.Elapsed -= Timer_Elapsed;
            _timer.Disposed -= Timer_Disposed;
        }

        private static bool _working;
        private static void Timer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            if (_working)
                return;

            _working = true;
            switch (_mainState)
            {
                case MainState.Initial:
                    break;
                case MainState.Warmup:
                    Warmup();
                    break;
                case MainState.Measuring:
                    Measuring();
                    break;
                case MainState.Cooldown:
                    Cooldown();
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
            _working = false;
        }

        private static bool _finished;
        private static LoadController _loadController;

        private static int _warmupCounter;
        private static void Warmup()
        {
            Console.Write($"Warmup: {_configuration.WarmupTime - _warmupCounter++}     \r");
        }
        private static void Cooldown()
        {
            Console.Write($"Waiting for {RunningProfiles.Count} profiles stopped.     \r");
        }

        private static double _maxPerformance;
        private static void Measuring()
        {
            var reqPerSec = Web.RequestsPerSec;
            Web.RequestsPerSec = 0;

            var profiles = RunningProfiles.Count;
            _loadController.Progress(reqPerSec, profiles, _currentProfileCompositionString, _averageResponseTimeString);

            var filteredValue = _loadController.FilteredRequestsPerSec;
            var diffValue = _loadController.DiffValue;
            var detected = _loadController.TopValueDetected ? 1 : 0;
            var logLine = $"{profiles}\t{Web.ActiveRequests}\t{reqPerSec}\t" +
                $"{filteredValue}\t" +
                $"{_loadController.Trace}\t" +
                $"{diffValue}\t" +
                $"{detected}\t" +
                $"{string.Join("\t", _averageResponseTime.Values.Select(d => d.ToString("0.00")).ToArray())}";
            WriteToOutputFile(logLine);

            var loadControl = _loadController.Next();
            switch (loadControl)
            {
                case LoadControlCommand.Stay:
                    Console.Write($"Working {_loadController.ProgressValue}       \r");
                    break;
                case LoadControlCommand.Exit:
                    Console.WriteLine("FINISHED.");
                    _finished = true;
                    break;
                case LoadControlCommand.Increase:
                    var speedTrace = string.Join("; ", _averageResponseTime.Values.Select(d => d.ToString("0.00")).ToArray());
                    Console.WriteLine($"INCREASE. {RunningProfiles.Count}; {_loadController.AveragePerformanceHistory.Last().AverageRequestsPerSec:0.000} RPS; {speedTrace}");
                    _averageResponseTime = Web.GetAverageResponseStringAndReset();
                    AverageResponseTimeToString();
                    AddAndStartProfiles(_growingProfiles);
                    break;
                case LoadControlCommand.Decrease:
                    if (Math.Abs(_loadController.MaxPerformance - _maxPerformance) > 0.000001d )
                    {
                        Console.WriteLine($"Performance max: {_loadController.MaxPerformance:0.000}; sweetpoint: {_loadController.ExpectedPerformance:0.000}");
                        _maxPerformance = _loadController.MaxPerformance;
                    }
                    speedTrace = string.Join("; ", _averageResponseTime.Values.Select(d => d.ToString("0.00")).ToArray());
                    Console.WriteLine($"DECREASE. {RunningProfiles.Count}; {_loadController.AveragePerformanceHistory.Last().AverageRequestsPerSec:0.000} RPS; {speedTrace}");
                    _averageResponseTime = Web.GetAverageResponseStringAndReset();
                    AverageResponseTimeToString();
                    StopProfiles(_growingProfiles);
                    break;
                default:
                    throw new ArgumentOutOfRangeException("Unknown load control: " + loadControl);
            }

            if(_errorCount >= _configuration.MaxErrors)
                _finished = true;

            if (Console.KeyAvailable)
            {
                var key = Console.ReadKey(true);
                if (key.KeyChar == 'x')
                {
                    Console.WriteLine("Interrupted by <x> key.");
                    WriteToOutputFile("Interrupted by <x> key.");
                    _finished = true;
                }
            }
        }

        private static void AverageResponseTimeToString()
        {
            _averageResponseTimeString = string.Join("; ", _averageResponseTime.Select(x => $"{x.Key}: {x.Value:0.###}").ToArray());
        }

        // ===================================================================== Output file

        private static readonly object OutputFileSync = new object();
        private static string _outputFile;

        private static void EnsureOutputFile(Configuration configuration)
        {
            _outputFile = configuration.OutputFile;

            var dirName = Path.GetDirectoryName(_outputFile);
            if (dirName != null)
                Directory.CreateDirectory(dirName);

            using (var fs = new FileStream(_outputFile, FileMode.Create))
            {
                using (var wr = new StreamWriter(fs))
                {
                    wr.WriteLine("\"sep=;\"");
                    wr.WriteLine("Benchmark start (UTC):;{0}", DateTime.UtcNow);
                    wr.WriteLine("Sites:;{0}", string.Join(Environment.NewLine + ";", configuration.SiteUrls));
                    wr.WriteLine("Warmup time:;{0}", configuration.WarmupTime);
                    wr.WriteLine("Initial profiles:;{0}", string.Join(Environment.NewLine + ";", configuration.InitialProfiles.Select(x => x.Key + ";" + x.Value)));
                    wr.WriteLine("Growing profiles:;{0}", string.Join(Environment.NewLine + ";", configuration.GrowingProfiles.Select(x => x.Key + ";" + x.Value)));
                    wr.WriteLine("Growing time:;{0}", configuration.GrowingTime);
                    wr.WriteLine("Max error count:;{0}", _configuration.MaxErrors);
                    wr.WriteLine();
                }
            }
        }
        private static void WriteToOutputFile(string line)
        {
            lock (OutputFileSync)
                using (var writer = new StreamWriter(_outputFile, true))
                    writer.WriteLine(line.Replace('\t', ';'));
        }

        // =========================================================== Error log

        private static readonly object ErrorFileSync = new object();
        private static string _errorFile;
        private static int _loggedErrorCount;
        private static readonly string ErrorFormat = "ERROR#{0}{1} in {2}. action of {3} #{4}: {5}" + Environment.NewLine
            + "{6}" + Environment.NewLine;
        private static void WriteErrorToLog(Exception e, Profile profile, int actionIndex, BenchmarkActionExpression action)
        {
            lock (ErrorFileSync)
            {
                if (_errorFile == null)
                {
                    _errorFile = _outputFile + ".errors.log";
                    using (var fs = new FileStream(_errorFile, FileMode.Create))
                    using (var wr = new StreamWriter(fs))
                        wr.WriteLine();
                }

                using (var writer = new StreamWriter(_errorFile, true))
                    writer.WriteLine(ErrorFormat, ++_loggedErrorCount, _mainState == MainState.Warmup ? "(WARMUP)" : "", actionIndex, profile.Name,
                        profile.Id, action, GetExceptionInfo(e));
            }
        }

        private static string GetExceptionInfo(Exception exception)
        {
            return SenseNet.Tools.Utility.CollectExceptionMessages(exception);
        }

        private static void WriteRequestLog()
        {
            var file = _outputFile + ".requests.log";
            var lines = Web.WebAccess.GetRequestLog();
            using (var writer = new StreamWriter(file, false))
            {
                for (int i = 0; i < lines.Length; i++)
                    writer.WriteLine($"{i:#####}\t{lines[i]}");
            }
        }
    }
}
