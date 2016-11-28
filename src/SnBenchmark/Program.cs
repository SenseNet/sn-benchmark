﻿using SenseNet.Client;
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
        private static Configuration _configuration;
        private static List<string> _speedItems;
        private static Dictionary<string, double> _limits;

        private static void Main(string[] args)
        {
            ServicePointManager.DefaultConnectionLimit = 300;

            _configuration = new Configuration();

            try
            {
                var result = ArgumentParser.Parse(args, _configuration);
                if (result.IsHelp)
                    Console.WriteLine(result.GetHelpText());
                else
                    Run();
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

            var initial = InitializeProfiles(_configuration.InitialProfiles);
            var growing = InitializeProfiles(_configuration.GrowingProfiles);

            Run(initial, growing).Wait();
        }
        private static List<Profile> InitializeProfiles(Dictionary<string, int> config)
        {
            var profilesDirectory = Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Profiles"));
            if(!Directory.Exists(profilesDirectory))
                profilesDirectory = Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "../../Profiles"));
            if (!Directory.Exists(profilesDirectory))
                throw new ApplicationException("Profiles directory not found.");

            var profiles = new List<Profile>();
            var speedItems = new List<string> { RequestExpression.NormalSpeed };

            // Iterate through all configured profile types and add the requested
            // number of initial profile objects of that profile type to the list.
            foreach (var item in config)
            {
                var name = item.Key;
                var count = item.Value;

                var src = LoadProfile(name, profilesDirectory);
                var profile = Profile.Parse(name, src, speedItems);

                // add a configured number of initial profiles of this type to the list
                for (var i = 0; i < count; i++)
                    profiles.Add(profile.Clone());
            }

            _speedItems = speedItems;

            CreateLimitsAndInitialPeriodData(speedItems);
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
        private static void CreateLimitsAndInitialPeriodData(IEnumerable<string> speedItems)
        {
            _limits = new Dictionary<string, double>();
            _periodData = new Dictionary<string, double>();
            var config = _configuration.Limits;
            foreach (var key in speedItems)
            {
                double value;
                if (!config.TryGetValue(key, out value))
                    value = Configuration.DefaultLimitValue;
                _limits.Add(key, value);
                _periodData.Add(key, 0.0);
            }
        }

        private static readonly List<Profile> RunningProfiles = new List<Profile>();
        private static System.Timers.Timer _timer;
        public static int StoppedProfiles { get; set; }

        private static readonly object ErrorSync = new object();
        private static bool _isInWarmup;
        private static int _errorCount;
        private static int _errorCountInWarmup;
        public static void AddError(Exception e, Profile profile, int actionIndex, BenchmarkActionExpression action)
        {
            lock (ErrorSync)
            {
                WriteErrorToLog(e, profile, actionIndex, action);
                if (_isInWarmup)
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
            _timer = new System.Timers.Timer(1000.0);
            _timer.Elapsed += Timer_Elapsed;
            _timer.Disposed += Timer_Disposed;
            _timer.Enabled = true;

            _isInWarmup = true;

            EnsureOutputFile(_configuration);

            WriteColumnHeaders(_speedItems);

            // begin with starting the configured number of initial profiles as a warmup
            AddAndStartProfiles(initialProfiles);
            Monitor("---- WARMUP  ");

            await Task.Delay(_configuration.WarmupTime * 1000);

            var boundaryConditionHaveBeenFulfilled = false;
            _isInWarmup = false;

            // start more profiles periodically, while the terminating conditions are not fulfilled
            while (!boundaryConditionHaveBeenFulfilled)
            {
                AddAndStartProfiles(growingProfiles);
                Monitor("---- GROWING ");
                await Task.Delay(_configuration.GrowingTime * 1000);
                boundaryConditionHaveBeenFulfilled = CheckBoundaryConditions(_limits);
            }

            var benchmarkResult = "BENCHMARK RESULT: " + (_lastBenchmarkResult ?? _benchmarkResult) + " Total errors: " + (_errorCountInWarmup + _errorCount);
            if (!_configuration.Verbose)
                Console.WriteLine();
            Console.WriteLine(benchmarkResult);

            WriteToOutputFile(benchmarkResult);

            Monitor("---- STOPPING");

            // wait for profiles that are still running to stop
            await ShutdownProfiles();

            _timer.Stop();
            Console.WriteLine();
        }

        private static async Task ShutdownProfiles()
        {
            foreach (var runningProfile in RunningProfiles)
                runningProfile.Stop();

            var lastCounts = new Queue<int>();
            while (true)
            {
                var running = RunningProfiles.Count - StoppedProfiles;
                if (running == 0)
                    break;

                lastCounts.Enqueue(running);
                if (lastCounts.Count > 5)
                {
                    lastCounts.Dequeue();
                    if (lastCounts.First() == lastCounts.Last())
                        break;
                }

                await Task.Delay(1000);
            }
        }

        private static void Timer_Disposed(object sender, EventArgs e)
        {
            _timer.Elapsed -= Timer_Elapsed;
            _timer.Disposed -= Timer_Disposed;
        }
        private static void Timer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            Monitor();
        }

        private static readonly Dictionary<string, int> ActualProfileCounts = new Dictionary<string, int>();
        private static void AddAndStartProfiles(List<Profile> profiles)
        {
            foreach (var profile in profiles)
            {
                int actualCount;
                if (!ActualProfileCounts.TryGetValue(profile.Name, out actualCount))
                    ActualProfileCounts[profile.Name] = 0;

                var limit = _configuration.ProfileLimits[profile.Name];
                if (limit > 0 && actualCount >= limit)
                    continue;

                ActualProfileCounts[profile.Name]++;

                // clone the profile to avoid modifying the original version
                var newProfile = profile.Clone();
                RunningProfiles.Add(newProfile);

                // start executing the profile but do not wait for it to complete
#pragma warning disable CS4014 
                newProfile.ExecuteAsync();
#pragma warning restore CS4014
            }
        }

        private static Dictionary<string, double> _periodData;
        private static string _lastBenchmarkResult;
        private static string _benchmarkResult;
        private static bool CheckBoundaryConditions(Dictionary<string, double> limits)
        {
            _periodData = Web.GetPeriodDataAndReset();
            _lastBenchmarkResult = _benchmarkResult;
            _benchmarkResult = FormatBenchmarkResult(RunningProfiles, _periodData);
            var finished = _periodData.Any(x => x.Value >= limits[x.Key]) || _errorCount >= _configuration.MaxErrors;
            return finished;
        }

        //=========================================================== Write result output

        private static string FormatBenchmarkResult(List<Profile> profiles, Dictionary<string, double> avgResponseTimesInSec)
        {
            return $"{profiles.Count} profiles ({string.Join(", ", RunningProfiles.GroupBy(x => x.Name).Select(g => "" + g.Key + ":" + g.Count()))}), " + 
                $"all requests:{Web.AllRequests}, errors:{_errorCount}, " +
                $"average respose times: {string.Join(", ", avgResponseTimesInSec.Select(y => $"{y.Key}:{y.Value:0.00}"))}.";
        }
        private static void WriteColumnHeaders(IEnumerable<string> speedItems)
        {
            var speeds = speedItems.ToArray();
            if (_configuration.Verbose)
            {
                Console.WriteLine("Pcount\tActive\tReq/sec\t" + string.Join("\t", speeds) + "\t" + string.Join("\t", speeds.Select(i => "L" + i.ToLower())));
            }
            else
            {
                Console.WriteLine("Pcount\tActive\t" + string.Join("\t", speeds));
                Console.WriteLine("\t\t" + string.Join("\t", _limits.Values.Select(d => d.ToString("0.00")).ToArray()));
            }

            var outputHead = "Pcount;Active;Req/sec;" + string.Join(";", speeds) + ";" + string.Join(";", speeds.Select(i => "L" + i.ToLower()));

            WriteToOutputFile(outputHead);
        }

        public static bool Pausing;
        private static void Monitor(string consoleMessage = null)
        {
            var logLine = $"{RunningProfiles.Count - StoppedProfiles}\t{Web.ActiveRequests}\t{Web.RequestsPerSec}\t" + 
                $"{string.Join("\t", _periodData.Values.Select(d => d.ToString("0.00")).ToArray())}\t" + 
                $"{string.Join("\t", _limits.Values.Select(d => d.ToString("0.00")).ToArray())}\t{(Pausing ? "pause" : "")}";                                                           // 5

            WriteToOutputFile(logLine);

            if (_configuration.Verbose)
            {
                if (consoleMessage != null)
                    Console.WriteLine(consoleMessage);
                Console.WriteLine(logLine);
            }
            else
            {
                if (consoleMessage != null)
                {
                    var msg = $"{RunningProfiles.Count - StoppedProfiles}\t{Web.ActiveRequests}\t" + 
                        $"{string.Join("\t", _periodData.Values.Select(d => d.ToString("0.00")).ToArray())}";

                    Console.WriteLine();
                    Console.Write(msg);
                    Console.Write(" ");
                    Console.Write(consoleMessage);
                }
                else
                {
                    Console.Write("-");
                }
            }

            Web.RequestsPerSec = 0;

            // ReSharper disable once InvertIf
            if (Console.KeyAvailable)
            {
                var x = Console.ReadKey(true);
                if (x.KeyChar == ' ')
                    Pausing = !Pausing;
            }
        }

        //=========================================================== Output file

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
                    wr.WriteLine("Limits:;{0}", string.Join(Environment.NewLine + ";", configuration.Limits.Select(x => x.Key + ";" + x.Value)));
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

        //=========================================================== Error log

        private static readonly object ErrorFileSync = new object();
        private static string _errorFile;
        private static int _loggedErrorCount;
        private static readonly string ErrorFormat = "ERROR#{0}{1} in {2}. action of {3} #{4}: {5}" + Environment.NewLine
            + "{6}" + Environment.NewLine + "{7}" + Environment.NewLine;
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

                var clienEx = e as ClientException;
                var serverTrace = clienEx?.ErrorData?.InnerError?.Trace;

                using (var writer = new StreamWriter(_errorFile, true))
                    writer.WriteLine(ErrorFormat, ++_loggedErrorCount, _isInWarmup ? "(WARMUP)" : "", actionIndex, profile.Name,
                        profile.Id, action, e.Message, serverTrace ?? string.Empty);
            }
        }
    }
}