using System;
using System.Collections.Generic;
using System.Linq;
using SnBenchmark.Expression;

namespace SnBenchmark
{
    public enum PathSetOperation { First, Current, Next, Index };
    public enum PathSetTransform { Parent, ODataEntity }

    internal class PathOperation
    {
        internal PathSet PathSet { get; private set; }
        internal PathSetOperation Operation { get; private set; }
        internal int AbsoluteIndex { get; private set; }
        internal PathSetTransform[] TransformationSteps { get; private set; }

        public static PathOperation Parse(string src, string profileName, List<PathSet> pathSets)
        {
            // Split source to segments. Minimum 2 segments are expected.
            var segments = src.Split('.');
            if (segments.Length < 2)
                throw new ApplicationException("Invalid Pathset definition.");

            var name = segments[0];

            // Get existing PathSet
            var pathSet = pathSets.FirstOrDefault(p => string.Equals(p.Name, name, StringComparison.CurrentCultureIgnoreCase));
            if (pathSet == null)
                throw new ApplicationException($"Pathset not found: {name}");

            // Parse operation. Non-begative integer or one of the PathSetOperation
            var operationSrc = segments[1];
            int absoluteIndex;
            if (int.TryParse(operationSrc, out absoluteIndex))
            {
                if (absoluteIndex < 0)
                    throw new ApplicationException($"Invalid index in Pathset '{name}': '{absoluteIndex}'. Index must be greater than or equal zero.");
                operationSrc = "index";
            }

            PathSetOperation operation;
            if (!Enum.TryParse(operationSrc, true, out operation))
                throw new ApplicationException($"Invalid operation in Pathset '{name}': '{operation}'");

            // Parse transformations. (.Parent)* (.Parent | .ODataEntity)
            var transformations = segments.Length > 2
                ? segments.Skip(2).Select(s => s.ToLowerInvariant()).ToArray()
                : new string[0];
            if (transformations.Any())
            {
                var last = transformations.Last();
                if (last != "odataentity" && last != "parent")
                    throw new ApplicationException(
                        $"Invalid transformation in Pathset '{name}': last item is '{last}'. It can be 'Parent' or 'ODataEntity'");
                if (transformations.Length > 1)
                {
                    var notParent =
                        transformations.Take(transformations.Length - 1).FirstOrDefault(t => t != "parent");
                    if (notParent != null)
                        throw new ApplicationException(
                            $"Invalid transformation in PathSet '{name}': '{notParent}'. It can be only 'Parent'");
                }
            }

            // Return with a new product.
            return new PathOperation
            {
                PathSet = pathSet,
                Operation = operation,
                AbsoluteIndex = absoluteIndex,
                TransformationSteps =
                    transformations.Select(t => (PathSetTransform) Enum.Parse(typeof(PathSetTransform), t, true))
                        .ToArray()
            };
        }

        public string Execute(IExecutionContext context)
        {
            var profile = context.CurrentProfile;
            var name = PathSet.Name;
            var count = PathSet.Paths.Length;
            int index;
            switch (Operation)
            {
                case PathSetOperation.First:
                    index = profile.InitialIndex;
                    break;
                case PathSetOperation.Current:
                    index = profile.GetIndex(name);
                    break;
                case PathSetOperation.Next:
                    index = profile.GetIndex(name) + 1;
                    break;
                case PathSetOperation.Index:
                    index = AbsoluteIndex;
                    break;
                default:
                    throw new ArgumentOutOfRangeException($"Unknown PathSetOperation: {Operation}");
            }

            index = index%count;
            profile.SetIndex(name, index);

            var path = PathSet.Paths[index];

            foreach (var transform in TransformationSteps)
            {
                switch (transform)
                {
                    case PathSetTransform.Parent:
                        path = GetParentPath(path);
                        break;
                    case PathSetTransform.ODataEntity:
                        path = MakeEntityPath(path);
                        break;
                    default:
                        throw new ArgumentOutOfRangeException($"Unknown PathSetTransform: {transform}");
                }
            }

            return path;
        }

        private string GetParentPath(string path)
        {
            if (string.IsNullOrEmpty(path))
                return string.Empty;

            var index = path.LastIndexOf("/", StringComparison.Ordinal);
            return index <= 0 ? string.Empty : path.Substring(0, index);
        }
        private string MakeEntityPath(string path)
        {
            var lastSlash = path.LastIndexOf('/');
            if (lastSlash > 0)
            {
                var path1 = path.Substring(0, lastSlash);
                var path2 = path.Substring(lastSlash + 1);
                return $"{path1}('{path2}')";
            }
            return $"/('{path.TrimStart('/')}')";
        }
    }
}
