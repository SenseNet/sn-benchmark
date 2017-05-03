using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using SenseNet.Tools.CommandLineArguments;
using SnBenchmark;

namespace SnBenchmarkTest
{
    [TestClass]
    public class ArgumentTests
    {
        [TestMethod]
        public void Args_Output()
        {
            var root = AppDomain.CurrentDomain.BaseDirectory;
            var rootParent = Path.GetDirectoryName(root);

            var testCases = new[] {
                null,
                "-OUT:C:\\BenchmarkLogs\\Monday\\Benchmark_*.csv",
                "-OUT:C:\\BenchmarkLogs\\Monday\\Benchmark.csv",
                "-OUT:Benchmark_*.csv",
                "-OUT:Benchmark.csv",
                "-OUT:..\\..\\Logs\\Benchmark_*.csv",
                "-OUT:..\\..\\Logs\\Benchmark.csv"
            };

            // yyyy-MM-dd_HH-mm-ss
            var expectations = new[] {
                root + "\\Output\\Benchmark-Output_????-??-??_??-??-??.csv",
                "C:\\BenchmarkLogs\\Monday\\Benchmark_????-??-??_??-??-??.csv",
                "C:\\BenchmarkLogs\\Monday\\Benchmark.csv",
                root + "\\Output\\Benchmark_????-??-??_??-??-??.csv",
                root + "\\Output\\Benchmark.csv",
                rootParent + "\\Logs\\Benchmark_????-??-??_??-??-??.csv",
                rootParent + "\\Logs\\Benchmark.csv"
            };

            for (var i = 0; i < testCases.Length; i++)
            {
                var testCase = testCases[i];
                var expectation = expectations[i];
                Args_Output_Test(testCase, expectation);
            }
        }
        private static void Args_Output_Test(string testCase, string expectation)
        {
            var args = testCase == null
                                ? new[] { "-PROFILE:Editor:1+1", "-SITE:http://localhost", "-USR:admin", "-PWD:admin" }
                                : new[] { "-PROFILE:Editor:1+1", "-SITE:http://localhost", "-USR:admin", "-PWD:admin", testCase };

            var configuration = new Configuration();
            ArgumentParser.Parse(args, configuration);

            var r = new Regex(@"\d", RegexOptions.None);
            var actual = r.Replace(configuration.OutputFile, "?");
            expectation = r.Replace(expectation, "?");

            Assert.AreEqual(expectation, actual);

        }

        [TestMethod]
        public void Args_Profile()
        {
            Args_Profile_TheTest(
                "-PROFILE:Reader:4+1",
                initial: new Dictionary<string, int> { { "Reader", 4 } },
                growing: new Dictionary<string, int>() { { "Reader", 1 } },
                limits: new Dictionary<string, int>() { { "Reader", 0 } });

            Args_Profile_TheTest(
                "-PROFILE:JohnSmith:4+1|11,PaulRock:42+0",
                initial: new Dictionary<string, int> { { "JohnSmith", 4 }, { "PaulRock", 42 } },
                growing: new Dictionary<string, int>() { { "JohnSmith", 1 }, { "PaulRock", 0 } },
                limits: new Dictionary<string, int>() { { "JohnSmith", 11 }, { "PaulRock", 0 } });

        }
        [TestMethod]
        public void Args_Profile_Invalid()
        {
            Assert.IsFalse(Args_Profile_IsValid("-PROFILE:Visitor:4"));
            Assert.IsFalse(Args_Profile_IsValid("-PROFILE:Visitor:4?1"));
            Assert.IsFalse(Args_Profile_IsValid("-PROFILE:Visitor:4:1"));
            Assert.IsFalse(Args_Profile_IsValid("-PROFILE:Visitor:4_1"));
            Assert.IsFalse(Args_Profile_IsValid("-PROFILE:Visitor:4.1"));
            Assert.IsFalse(Args_Profile_IsValid("-PROFILE:Visitor:4,1"));
            Assert.IsFalse(Args_Profile_IsValid("-PROFILE:Visitor:4-1"));
            Assert.IsFalse(Args_Profile_IsValid("-PROFILE:Visitor:4/1"));
            Assert.IsFalse(Args_Profile_IsValid("-PROFILE:Visitor:4*1"));
            Assert.IsFalse(Args_Profile_IsValid("-PROFILE:Visitor:4|1"));
            Assert.IsFalse(Args_Profile_IsValid("-PROFILE:Visitor:4+1?10"));
            Assert.IsFalse(Args_Profile_IsValid("-PROFILE:Visitor:4+1:10"));
            Assert.IsFalse(Args_Profile_IsValid("-PROFILE:Visitor:4+1_10"));
            Assert.IsFalse(Args_Profile_IsValid("-PROFILE:Visitor:4+1.10"));
            Assert.IsFalse(Args_Profile_IsValid("-PROFILE:Visitor:4+1,10"));
            Assert.IsFalse(Args_Profile_IsValid("-PROFILE:Visitor:4+1-10"));
            Assert.IsFalse(Args_Profile_IsValid("-PROFILE:Visitor:4+1/10"));
            Assert.IsFalse(Args_Profile_IsValid("-PROFILE:Visitor:4+1*10"));
            Assert.IsFalse(Args_Profile_IsValid("-PROFILE:Visitor:4+1+10"));
            Assert.IsTrue(Args_Profile_IsValid("-PROFILE:Visitor:4+1|10"));
            Assert.IsFalse(Args_Profile_IsValid("-PROFILE:Visitor:4|1|10"));
        }

        private static void Args_Profile_TheTest(string testCase, Dictionary<string, int> initial, Dictionary<string, int> growing, Dictionary<string, int> limits)
        {
            var args = new[] {testCase, "-SITE:http://localhost", "-USR:admin", "-PWD:admin"};

            var configuration = new Configuration();
            ArgumentParser.Parse(args, configuration);

            if (initial == null)
                throw new ArgumentNullException(nameof(initial));
            if (growing == null)
                throw new ArgumentNullException(nameof(growing));
            if (limits == null)
                throw new ArgumentNullException(nameof(limits));

            var expected = DictionaryToString("initial", initial);
            var actual = DictionaryToString("initial", configuration.InitialProfiles);
            Assert.AreEqual(expected, actual);

            expected = DictionaryToString("growing", growing);
            actual = DictionaryToString("growing", configuration.GrowingProfiles);
            Assert.AreEqual(expected, actual);

            expected = DictionaryToString("limits", limits);
            actual = DictionaryToString("limits", configuration.ProfileLimits);
            Assert.AreEqual(expected, actual);
        }
        private static string DictionaryToString(string prefix, Dictionary<string, int> dict)
        {
            return prefix + ":" + string.Join(",", dict.Select(i => i.Key + "=" + i.Value).ToArray());
        }

        private static bool Args_Profile_IsValid(string testCase)
        {
            try
            {
                Args_Profile_TheTest(testCase, null, null, null);
            }
            catch (ArgumentNullException)
            {
                return true;
            }
            catch (Exception)
            {
                return false;
            }
            return true;
        }

    }
}
