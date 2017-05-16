using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Threading.Tasks;

namespace SnBenchmark.Expression
{
    internal class VariableExpression : BenchmarkActionExpression
    {
        internal const char VariableStart = '@';

        internal string Name { get; }
        internal string ObjectName { get; }
        internal string[] PropertyPath { get; }

        public VariableExpression(string name, string objectName, string[] propertyPath)
        {
            this.Name = name;
            this.ObjectName = objectName;
            this.PropertyPath = propertyPath;
        }

        internal override BenchmarkActionExpression Clone()
        {
            return new VariableExpression(Name, ObjectName, PropertyPath);
        }

        internal override Task ExecuteAsync(IExecutionContext context, string actionId)
        {
            var @object = context.GetVariable(ObjectName);
            var value = ResolveProperty(@object as string, PropertyPath);
            context.SetVariable(Name, value);

            // Return an empty task as this particular method 
            // does not need to execute anything asynchronously.
            return Task.FromResult<object>(null);
        }

        internal override void Test(IExecutionContext context, string actionId, string profileResponsesDirectory)
        {
            var @object = context.GetVariable(ObjectName);
            var value = ResolveProperty(@object as string, PropertyPath);
            context.SetVariable(Name, value);
        }

        private static object ResolveProperty(string objectSrc, string[] propertyPath)
        {
            if (string.IsNullOrEmpty(objectSrc))
                return null;

            JObject @object;
            try
            {
                @object = JsonConvert.DeserializeObject(objectSrc) as JObject;
            }
            catch
            {
                return null;
            }

            if (@object == null || propertyPath == null || propertyPath.Length == 0)
                return null;

            object value = @object;
            for (var i = 0; i < propertyPath.Length; i++)
            {
                var propertyName = propertyPath[i];
                value = @object[propertyName];

                if (i >= propertyPath.Length - 1)
                    continue;

                @object = value as JObject;
                if (@object == null)
                    return null;
            }

            return value;
        }

        public override string ToString()
        {
            return Name + " = " + ObjectName + string.Join(".", PropertyPath);
        }
    }
}
