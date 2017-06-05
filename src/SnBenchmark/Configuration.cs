using System.Collections.Generic;
using System.Globalization;
using System;
using System.IO;
using SenseNet.Tools.CommandLineArguments;
// ReSharper disable UnusedMember.Local
// ReSharper disable ArgumentsStyleStringLiteral
// ReSharper disable ArgumentsStyleLiteral

namespace SnBenchmark
{
    public class Configuration
    {
        private string _profileArg;
        private Dictionary<string, int> _initialProfiles;
        private Dictionary<string, int> _growingProfiles;
        private Dictionary<string, int> _profileLimits;

        [CommandLineArgument(name: "Profile", required: true, aliases: "P", helpText: "Comma separated name:count+growth|limit structures, where limit is optional (e.g.: 'Profile1:5+1,Profile2:1+1',Profile3:5+1|50).")]
        private string ProfileArg
        {
            get { return _profileArg; }
            set
            {
                _profileArg = value;
                ParseProfiles(value, out _initialProfiles, out _growingProfiles, out _profileLimits);
            }
        }
        private static void ParseProfiles(string profileArg, out Dictionary<string, int> initialProfiles, out Dictionary<string, int> growingProfiles, out Dictionary<string, int> profileLimits)
        {
            initialProfiles = new Dictionary<string, int>();
            growingProfiles = new Dictionary<string, int>();
            profileLimits = new Dictionary<string, int>();
            foreach (var item in profileArg.Split(','))
            {
                // Profile :: <profileName> SEMI <initialCount> PLUS <growingCount> [ PIPE <profileLimit> ] 
                // SEMI :: ':'
                // PLUS :: '+'
                // PIPE :: '|'

                var q = item.Split('|');
                if (q.Length > 2)
                    throw new ArgumentException("Invalid profile: " + item);
                var profileLimit = q.Length > 1 ? int.Parse(q[1]) : 0;
                var x = q[0].Split(':');
                if (x.Length > 2)
                    throw new ArgumentException("Invalid profile: " + item);
                var profileName = x[0];
                var y = x[1].Split('+');
                if (y.Length != 2)
                    throw new ArgumentException("Invalid profile: " + item);
                var initialCount = int.Parse(y[0]);
                var growingCount = int.Parse(y[1]);
                if (growingCount < 0)
                    throw new ArgumentException("Invalid profile: " + item);
                initialProfiles.Add(profileName, initialCount);
                growingProfiles.Add(profileName, growingCount);
                profileLimits.Add(profileName, profileLimit);
            }
        }

        /// <summary>
        /// Number of initial profile instances (profilename/value pairs).
        /// </summary>
        public Dictionary<string, int> InitialProfiles => _initialProfiles;
        /// <summary>
        /// Number of periodically created new profile instances (profilename/value pairs).
        /// </summary>
        public Dictionary<string, int> GrowingProfiles => _growingProfiles;
        /// <summary>
        /// Maximum number of profile instances (profilename/value pairs).
        /// </summary>
        public Dictionary<string, int> ProfileLimits => _profileLimits;

        private string _siteUrlArg;

        [CommandLineArgument(name: "Site", required: true, aliases: "S", helpText: "Comma separated url list (e.g.: 'http://mysite1,http://mysite1').")]
        private string SiteUrlArg
        {
            get { return _siteUrlArg; }
            set
            {
                _siteUrlArg = value;
                SiteUrls = ParseSiteUrls(value);
            }
        }
        private static string[] ParseSiteUrls(string siteUrlArg)
        {
            var sites = siteUrlArg.Split(',');
            for (var i = 0; i < sites.Length; i++)
                sites[i] = sites[i].Trim().TrimEnd('/');
            return sites;
        }
        public string[] SiteUrls { get; private set; }

        [CommandLineArgument(name: "UserName", required: true, aliases: "Usr", helpText: "Username and domain (e.g. 'Admin' or 'demo\\someone).")]
        public string UserName { get; set; }

        [CommandLineArgument(name: "Password", required: true, aliases: "Pwd", helpText: "Password")]
        public string Password { get; set; }

        private string _warmupTimeArg;
        [CommandLineArgument(name: "WarmupTime", required: false, aliases: "W,WARMUP", helpText: "Warmup time in seconds. Default: 60")]
        private string WarmupTimeArg
        {
            get { return _warmupTimeArg; }
            set
            {
                _warmupTimeArg = value;
                WarmupTime = int.Parse(value);
            }
        }

        /// <summary>
        /// Warmup time in seconds, while measuring is skipped.
        /// </summary>
        public int WarmupTime { get; private set; } = 60;

        private string _growingTimeArg;
        [CommandLineArgument(name: "GrowingTime", required: false, aliases: "G,GROW,GROWING", helpText: "Growing time in seconds. Default: 30")]
        private string GrowingTimeArg
        {
            get { return _growingTimeArg; }
            set
            {
                _growingTimeArg = value;
                GrowingTime = int.Parse(value);
            }
        }

        /// <summary>
        /// Length of a single period in seconds.
        /// </summary>
        public int GrowingTime { get; private set; } = 30;

        private string _maxErrorsArg;
        [CommandLineArgument(name: "MaxErrors", required: false, aliases: "E,ERRORS", helpText: "Maximum allowed error count. Default: 10")]
        private string MaxErrorsArg
        {
            get { return _maxErrorsArg; }
            set
            {
                _maxErrorsArg = value;
                MaxErrors = int.Parse(value);
            }
        }

        /// <summary>
        /// Maximum number of errors that can occur without stopping the benchmark.
        /// </summary>
        public int MaxErrors { get; private set; } = 10;

        //UNDONE: Remove Pausing feature from documentation
        //UNDONE: Remove Verbose argument from documentation
        //UNDONE: Remove Limits argument from documentation

        private string _outputFileArg;
        [CommandLineArgument(name: "Output", required: false, aliases: "O,Out", helpText: "Output file for further analysis.")]
        private string OutputFileArg
        {
            get { return _outputFileArg; }
            set
            {
                _outputFileArg = value;
                _outputFile = ParseOutputFile(value);
            }
        }
        private string _outputFile;
        public string OutputFile => _outputFile ?? (_outputFile = ParseOutputFile(GetDefaultOutputFile()));
        public string ResponsesDirectoryPath => GetResponsesDirectoryPath(OutputFile);

        [CommandLineArgument(name: "TestOnly", required: false, aliases: "T,Test", helpText: "Plays the profiles once and saves their responses.")]
        public bool TestOnly { get; set; }

        private static string ParseOutputFile(string fileName)
        {
            fileName = ReplaceWildcard(fileName);
            if (Path.IsPathRooted(fileName))
                return fileName;
            var path = Path.Combine("Output", fileName);
            path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, path);
            return Path.GetFullPath(path);
        }
        private static string GetDefaultOutputFile(string pattern = null)
        {
            if (pattern == null)
                return ReplaceWildcard("Benchmark-Output_*.csv");

            var temp = pattern.Replace("*", string.Empty);
            if (pattern.Length - temp.Length > 1)
                throw new ArgumentException("Output definition cannot contain the '*' character more than once.");

            return ReplaceWildcard(pattern);
        }
        private static string ReplaceWildcard(string fileName)
        {
            var temp = fileName.Replace("*", string.Empty);
            if (fileName.Length - temp.Length > 1)
                throw new ArgumentException("Output definition cannot contain the '*' character more than once.");
            var dateTimeString = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss", CultureInfo.InvariantCulture);
            return fileName.Replace("*", dateTimeString);
        }
        private static string GetResponsesDirectoryPath(string outputFile)
        {
            if (outputFile == null)
                throw new ArgumentNullException(nameof(outputFile));
            return Path.Combine(Path.GetDirectoryName(outputFile) ?? "", Path.GetFileNameWithoutExtension(outputFile));
        }
    }
}