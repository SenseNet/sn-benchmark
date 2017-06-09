using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
// ReSharper disable PossibleNullReferenceException
// ReSharper disable AssignNullToNotNullAttribute

namespace SNBCalc
{
    class Program
    {
        static void Main(string[] args)
        {
            Run(args);

            if (Debugger.IsAttached)
            {
                Console.Write("Press <enter> to exit...");
                Console.ReadLine();
            }
        }

        static void Run(string[] args)
        {
            var path = args.FirstOrDefault();
            if (path == null)
            {
                Console.WriteLine("One file or directory path expected.");
                return;
            }

            if (Directory.Exists(path))
            {
                var summaryPath = Path.Combine(path, "summary.txt");
                var paths = Directory.GetFiles(path, "*.csv");
                using (var summaryWriter = new StreamWriter(summaryPath, false))
                {
                    summaryWriter.WriteLine("Name\tTime\tMeasuring time (sec)\tProfiles\tComposition\tRPS\tAll requests\tErrors\tResponse times");
                    foreach (var file in paths)
                        ProcessFile(file, summaryWriter);
                }
                return;
            }

            Console.WriteLine("Directory does not exist: " + path);
        }

        static void ProcessFile(string path, StreamWriter summaryWriter)
        {
            var fileName = Path.GetFileName(path);
            string nameSuffix;
            var name = GetName(fileName, out nameSuffix);

            Console.WriteLine("Processing " + fileName);

            using (var reader = new StreamReader(path))
            {
                string headLine;
                string line;
                while ((headLine = reader.ReadLine()) != null)
                {
                    if (headLine.StartsWith("Pcount;"))
                        break;
                }

                if (headLine == null)
                {
                    Console.WriteLine("Headline not found.");
                    return;
                }

                var lineCount = 0;
                while ((line = reader.ReadLine()) != null)
                {
                    if (line.StartsWith("BENCHMARK RESULT:"))
                    {
                        var summaryRecord = GetSummaryRecord(name, nameSuffix, lineCount, line);
                        //Console.WriteLine("    " + summaryRecord);
                        summaryWriter.WriteLine(summaryRecord);
                        break;
                    }
                    lineCount++;
                }
            }
        }

        private static string GetName(string fileName, out string suffix)
        {
            // SNB10K_2017-05-07_23-23-08.csv           SNB10K,        2017-05-07, 23-23-08
            // SNB100K_RW5M_2017-05-17_06-12-46.csv     SNB100K. RW5M. 2017-05-17. 06-12-46
            var length = "2017-05-17_06-12-46".Length;
            var rawName = Path.GetFileNameWithoutExtension(fileName);
            var name = rawName?.Substring(0, rawName.Length - length - 1);
            suffix = rawName?.Substring(rawName.Length - length);
            return name;
        }

        private static string GetSummaryRecord(string name, string suffix, int lineCount, string line)
        {
            // "Profiles\tComposition\tRPS\tAll requests\tErrors\tResponse times"
            var data = line.Split(';');

            // Profiles
            var profileData = data[0].Replace("BENCHMARK RESULT:", "")
                .Replace("Profiles:", "")
                .Split(new[] {'(', ')'}, StringSplitOptions.RemoveEmptyEntries);
            var profiles = profileData[0].Trim();
            var composition = profileData[1].Trim();

            // rps
            var rps = data[1].Replace("RPS:", "").Trim();

            // All requests
            var allRequests = data[2].Replace("All requests:", "").Trim();

            // Errors
            var errors = data[3].Replace("Errors:", "").Trim();

            // Response times
            var responseTimes = string
                .Join("\t", data.Skip(4).ToArray())
                .Replace("Response times:", "")
                .Replace(":", "\t")
                .Trim();

            return $"{name}\t{suffix}\t{lineCount}\t{profiles}\t{composition}\t{rps}\t{allRequests}\t{errors}\t{responseTimes}";
        }

    }
}
