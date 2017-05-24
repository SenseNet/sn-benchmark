using System;
using System.Diagnostics;
using System.IO;

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
            if (args.Length != 1)
            {
                Console.WriteLine(args.Length == 0
                    ? "Missing file or directory path."
                    : "One file or directory path expected.");
                return;
            }

            var path = args[0];

            if (Directory.Exists(path))
            {
                var summaryPath = Path.Combine(path, "summary.txt");
                using (var summaryWriter = new StreamWriter(summaryPath, false))
                    foreach (var file in Directory.GetFiles(path, "SNB*.csv"))
                        ProcessFile(file, summaryWriter);
                return;
            }

            if (File.Exists(path))
            {
                ProcessFile(path);
                return;
            }

            Console.WriteLine("File or directory does not exist: " + path);
        }

        static void ProcessFile(string path, StreamWriter summaryWriter = null)
        {
            var fileName = Path.GetFileName(path);
            string nameSuffix;
            var name = GetName(fileName, out nameSuffix);

            Console.WriteLine("Processing " + fileName);

            var detector = new BenchmarkEndPointCalculator();
            var outPath = Path.Combine(Path.GetDirectoryName(path), "calc_" + fileName);

            using (var writer = new StreamWriter(outPath, false))
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

                writer.WriteLine("sep=;");
                headLine += ";;Pcount;Req/sec;InputAvg;DiffAvg;Trigger";
                writer.WriteLine(headLine);

                var triggered = false;
                while ((line = reader.ReadLine()) != null)
                {
                    if (line.StartsWith("BENCHMARK RESULT:"))
                        break;

                    bool trigger;
                    var detected = detector.Detect(line, out trigger);
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
