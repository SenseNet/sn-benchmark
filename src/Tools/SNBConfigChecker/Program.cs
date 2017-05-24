using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace SNBConfigChecker
{
    class Program
    {
        static void Main(string[] args)
        {
            var serverPath1 = @"\\snbweb01\Web";
            var serverPath2 = @"\\snbweb02\Web";

            var snWebs = Directory.GetDirectories(serverPath1)
                .Concat(Directory.GetDirectories(serverPath2))
                .Select(SnWeb.Create)
                .ToArray();

            Console.WriteLine("NETWORKTARGETS");
            Console.WriteLine();
            TraceConfigs(snWebs, "/configuration/sensenet/packaging/add[@key='NetworkTargets']/@value");
            Console.WriteLine();
            Console.WriteLine("CONNECTIONSTRINGS");
            Console.WriteLine();
            Console.WriteLine("SnCrMsSql");
            Console.WriteLine();
            TraceConfigs(snWebs, "/configuration/connectionStrings/add[@name='SnCrMsSql']/@connectionString");
            Console.WriteLine();
            Console.WriteLine("SenseNet.MongoDbBlobDatabase");
            Console.WriteLine();
            TraceConfigs(snWebs, "/configuration/connectionStrings/add[@name='SenseNet.MongoDbBlobDatabase']/@connectionString");
            Console.WriteLine();
            Console.WriteLine("DATA HANDLING");
            Console.WriteLine();
            Console.WriteLine("BlobProvider");
            Console.WriteLine();
            TraceConfigs(snWebs, "/configuration/appSettings/add[@key='BlobProvider']/@value");
            Console.WriteLine();
            Console.WriteLine("Minimum blob size");
            Console.WriteLine();
            TraceConfigs(snWebs, "/configuration/appSettings/add[@key='MinimumSizeForBlobProviderKB']/@value");
            Console.WriteLine();
            Console.WriteLine("MongoDb blob size");
            Console.WriteLine();
            TraceConfigs(snWebs, "/configuration/appSettings/add[@key='MongoDbBlobDatabaseChunkSize']/@value");
            Console.WriteLine();
            Console.WriteLine("BinaryChunkSize");
            Console.WriteLine();
            TraceConfigs(snWebs, "/configuration/sensenet/dataHandling/add[@key='BinaryChunkSize']/@value");
            Console.WriteLine();
            Console.WriteLine("MESSAGE PROVIDERS");
            Console.WriteLine();
            Console.WriteLine("Repository");
            Console.WriteLine();
            TraceConfigs(snWebs, "/configuration/sensenet/messaging/add[@key='ClusterChannelProvider']/@value");
            Console.WriteLine();
            Console.WriteLine("Security");
            Console.WriteLine();
            TraceConfigs(snWebs, "/configuration/appSettings/add[@key='SecurityMessageProvider']/@value");
            Console.WriteLine();
            Console.WriteLine("MESSAGE QUEUES");
            Console.WriteLine();
            Console.WriteLine("Repository");
            Console.WriteLine();
            TraceConfigs(snWebs, "/configuration/sensenet/messaging/add[@key='MsmqChannelQueueName']/@value");
            Console.WriteLine();
            Console.WriteLine("Security");
            Console.WriteLine();
            TraceConfigs(snWebs, "/configuration/appSettings/add[@key='SecurityMsmqChannelQueueName']/@value");
            Console.WriteLine();
            Console.WriteLine("-------------------------------------------------------------------------");
            Console.WriteLine();
            Console.WriteLine("LAST INDEXING ACTIVITY ID");
            Console.WriteLine();
            TraceLastActivityIds(snWebs);
            Console.WriteLine();
            Console.WriteLine("-------------------------------------------------------------------------");
            Console.WriteLine();
            Console.WriteLine("LUCENE LOCK FILES");
            Console.WriteLine();
            TraceLuceneLockFiles(snWebs);
            Console.WriteLine();

            if (Debugger.IsAttached)
            {
                Console.Write("Press <enter> to exit ...");
                Console.ReadLine();
            }
        }

        private static void TraceConfigs(SnWeb[] snWebs, string xpath)
        {
            foreach (var snWeb in snWebs)
            {
                var attr1 = snWeb.WebConfig.SelectSingleNode(xpath);
                var attr2 = snWeb.SnAdminRuntimeConfig.SelectSingleNode(xpath);
                Console.WriteLine($"{snWeb.WebPath,-25} WEB: {attr1?.Value ?? ""}");
                Console.WriteLine($"{snWeb.WebPath,-25} SNA: {attr2?.Value ?? ""}");
            }
        }

        private static void TraceLastActivityIds(SnWeb[] snWebs)
        {
            Console.WriteLine($"{"WEB",-25}| {"DATABASE",12}{"INDEX",12}{"DIFF",12}");
            Console.WriteLine($"-------------------------|--------------------------------------");
            foreach (var snWeb in snWebs)
            {
                var db = GetLastActivityIdFromDatabase(snWeb);
                var index = GetLastActivityIdFromIndex(snWeb);
                Console.WriteLine(
                    $"{snWeb.WebPath,-25}| {db,12}{index,12}{db-index,12}");
            }
        }
        private static int GetLastActivityIdFromDatabase(SnWeb snWeb)
        {
            var xpath = "/configuration/connectionStrings/add[@name='SnCrMsSql']/@connectionString";
            var connectionString = snWeb.WebConfig.SelectSingleNode(xpath)?.Value;
            return SnDatabase.GetLastIndexingActivityId(connectionString);
        }
        private static int GetLastActivityIdFromIndex(SnWeb snWeb)
        {
            var segmentPath = FindSegmentsFile(snWeb.IndexPath);
            if (segmentPath == null)
                return -1;

            byte[] buffer;
            using (var stream = new FileStream(segmentPath, FileMode.Open, FileAccess.Read))
            {
                var length = Convert.ToInt32(stream.Length);
                buffer = new byte[length];
                stream.Read(buffer, 0, length);
                for (int i = 0; i < buffer.Length; i++)
                {
                    if (buffer[i] == (byte) 0x00)
                        buffer[i] = (byte) 0x20;
                }
            }

            string content;
            using (var reader = new StreamReader(new MemoryStream(buffer)))
                content = reader.ReadToEnd();

            var p = content.IndexOf("LastActivityId", StringComparison.Ordinal);
            if (p < 0)
                return -1;

            p += "LastActivityId".Length + 1;
            var p1 = content.IndexOf(" ", p, StringComparison.Ordinal);
            if (p1 < 0)
                return -1;

            var src = content.Substring(p, p1 - p);
            int result;
            if (!int.TryParse(src, out result))
                return -1;

            return result;
        }
        private static string FindSegmentsFile(string indexPath)
        {
            if (indexPath == null)
                return null;
            var segmentsFiles = Directory.GetFiles(indexPath, "segments_*");
            if (segmentsFiles.Length != 1)
                return null;
            return segmentsFiles[0];
        }

        private static void TraceLuceneLockFiles(SnWeb[] snWebs)
        {
            Console.WriteLine($"{"WEB",-25}| {"FILE",12}");
            Console.WriteLine($"-------------------------|--------------");
            foreach (var snWeb in snWebs)
            {
                if (snWeb.IndexPath != null)
                {
                    var path = Path.Combine(snWeb.IndexPath, "write.lock");
                    Console.WriteLine(
                        $"{snWeb.WebPath,-25}| {(File.Exists(path) ? "write.lock" : ""),12}");
                }
            }
        }
    }
}
