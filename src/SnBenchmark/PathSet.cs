using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SnBenchmark.Expression;

namespace SnBenchmark
{
    internal class PathSet
    {
        public string ProfileName { get; set; }
        public string Name { get; set; }
        public string[] Paths { get; set; }

        //--------------------------------------------------------

        internal static List<PathSet> PathSets = new List<PathSet>();

        public static PathSet Create(string profileName, string name, string definition)
        {
            //UNDONE: implement PathSet.Create()
            throw new NotImplementedException();
        }
        internal static PathSet Create(string profileName, string name, string[] paths)
        {
            var pathSet = PathSets.FirstOrDefault(p => p.ProfileName == profileName && p.Name == name);
            if (pathSet != null)
                return pathSet;
            pathSet = new PathSet {ProfileName = profileName, Name = name, Paths = paths};
            PathSets.Add(pathSet);
            return pathSet;
        }

        public static string ResolveUrl(string input, IExecutionContext context)
        {
            // /OData.svc/##BigFiles.next.odataentity##?metadata=no

            int p0;
            if ((p0 = input.IndexOf("##", StringComparison.Ordinal)) < 0)
                return input;

            int p1;
            if ((p1 = input.IndexOf("##", p0 + 2, StringComparison.Ordinal)) < 0)
                return input;

            var src = input.Substring(p0 + 2, p1 - p0 - 2);
            var pathSetExpr = PathSetExpression.Parse(input.Substring(p0 + 2, p1 - p0 - 2), context.CurrentProfile.Name, PathSets);

            var path = pathSetExpr.Execute(context);

            return input.Replace($"##{src}##", path);
        }

    }
}
