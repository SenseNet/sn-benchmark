using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Xml;

namespace SNBConfigChecker
{
    [DebuggerDisplay("{WebPath}")]
    public class SnWeb
    {
        public string WebPath { get; set; }
        public XmlDocument WebConfig { get; set; }
        public XmlDocument SnAdminRuntimeConfig { get; set; }
        public string IndexPath { get; set; }

        public static SnWeb Create(string path)
        {
            return new SnWeb
            {
                WebPath = path,
                WebConfig = LoadConfig(Path.Combine(path, "web.config")),
                SnAdminRuntimeConfig = LoadConfig(Path.Combine(path, @"Tools\SnAdminRuntime.exe.config")),
                IndexPath = FindIndexDirectory(path)
            };
        }

        private static XmlDocument LoadConfig(string path)
        {
            var xml = new XmlDocument();
            xml.Load(path);
            return xml;
        }

        private static string FindIndexDirectory(string path)
        {
            var indexRoot = Path.Combine(path, @"App_Data\LuceneIndex");
            if (!Directory.Exists(indexRoot))
                return null;
            return Directory.GetDirectories(indexRoot)
                .OrderBy(p => p)
                .LastOrDefault();
        }
    }
}
