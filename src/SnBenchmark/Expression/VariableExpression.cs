using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Threading.Tasks;

namespace SnBenchmark.Expression
{
    internal class VariableExpression : BenchmarkActionExpression
    {
        internal const char VariableStart = '@';

        private readonly string _name;
        private readonly string _objectName;
        private readonly string[] _propertyPath;

        public VariableExpression(string name, string objectName, string[] propertyPath)
        {
            this._name = name;
            this._objectName = objectName;
            this._propertyPath = propertyPath;
        }

        internal override BenchmarkActionExpression Clone()
        {
            return new VariableExpression(_name, _objectName, _propertyPath);
        }

        internal override Task ExecuteAsync(IExecutionContext context, string actionId)
        {
            var @object = context.GetVariable(_objectName);
            var value = ResolveProperty(@object as string, _propertyPath);
            context.SetVariable(_name, value);

            // Return an empty task as this particular method 
            // does not need to execute anything asynchronously.
            return Task.FromResult<object>(null);
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
            return _name + " = " + _objectName + string.Join(".", _propertyPath);
        }
    }
}
