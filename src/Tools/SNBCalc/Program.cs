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
            var path = args.FirstOrDefault(a => !a.Equals("-SUMMARY", StringComparison.InvariantCultureIgnoreCase));
            var summary = args.Any(a => a.Equals("-SUMMARY", StringComparison.InvariantCultureIgnoreCase));
            if (path == null)
            {
                Console.WriteLine("One file or directory path expected. Optional switch: -SUMMARY");
                return;
            }

            if (Directory.Exists(path))
            {
                var summaryPath = Path.Combine(path, "summary.txt");
                var paths = Directory.GetFiles(path, "*.csv")
                    .Where(p => !Path.GetFileName(p).EndsWith(".calc.csv", StringComparison.OrdinalIgnoreCase));
                using (var summaryWriter = new StreamWriter(summaryPath, false))
                    foreach (var file in paths)
                        ProcessFile(file, summary, summaryWriter);
                return;
            }

            if (File.Exists(path))
            {
                ProcessFile(path, summary);
                return;
            }

            Console.WriteLine("File or directory does not exist: " + path);
        }

        static void ProcessFile(string path, bool summaryOnly, StreamWriter summaryWriter = null)
        {
            var fileName = Path.GetFileName(path);
            string nameSuffix;
            var name = GetName(fileName, out nameSuffix);

            Console.WriteLine("Processing " + fileName);

            var detector = new BenchmarkEndPointCalculator();

            StreamWriter writer = null;
            if (!summaryOnly)
            {
                var outPath = Path.Combine(Path.GetDirectoryName(path), Path.GetFileNameWithoutExtension(fileName) + ".calc.csv");
                writer = new StreamWriter(outPath, false);
            }

            try
            {
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

                    if (!summaryOnly)
                    {
                        writer.WriteLine("\"sep=;\"");
                        headLine += ";;Pcount;Req/sec;InputAvg;DiffAvg;Trigger";
                        writer.WriteLine(headLine);
                    }

                    var triggered = false;
                    while ((line = reader.ReadLine()) != null)
                    {
                        if (line.StartsWith("BENCHMARK RESULT:"))
                            break;

                        bool trigger;
                        var detected = detector.Detect(line, out trigger);
                        if (!summaryOnly)
                            writer.WriteLine(line + ";" + detected);

                        if (trigger && !triggered)
                        {
                            triggered = true;
                            var summaryRecord = GetSummaryRecord(name, nameSuffix, detected);
                            Console.WriteLine("    " + summaryRecord);
                            summaryWriter?.WriteLine(summaryRecord);
                        }
                    }
                }
            }
            finally
            {
                writer?.Dispose();
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

        private static string GetSummaryRecord(string name, string suffix, string data)
        {
            var d = data.Split(';');
            return $"{name}\t{suffix}\t{d[0]}\t{d[2]}";
        }

    }
}
