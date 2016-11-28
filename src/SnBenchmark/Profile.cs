using SnBenchmark.Expression;
using SnBenchmark.Parser;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace SnBenchmark
{
    /// <summary>
    /// Represents a series of actions that a typical uses performs on a site. E.g. a Visitor profile
    /// may consist of a few browsing steps, a search request and visiting one of the results.
    /// </summary>
    [DebuggerDisplay("{Id}:{Name}")]
    internal class Profile : IExecutionContext
    {
        //======================================================== Properties

        private static volatile int _lastId;
        public int Id { get; }

        public string Name { get; }

        public List<BenchmarkActionExpression> Actions { get; }

        //======================================================== Constructors

        public Profile(string name, List<BenchmarkActionExpression> actions)
        {
            Id = ++_lastId;
            Name = name;
            Actions = actions;
        }

        //======================================================== Static API

        /// <summary>
        /// Creates and initializes a profile from a script definition and speed limits.
        /// </summary>
        /// <param name="name">Name of the profile.</param>
        /// <param name="src">Profile definition script.</param>
        /// <param name="speedItems">List of response limit names.</param>
        /// <returns>A fully initialized profile object.</returns>
        public static Profile Parse(string name, string src, List<string> speedItems)
        {
            var parser = new ProfileParser(src, speedItems);
            var benchmarkActions = parser.Parse();
            return new Profile(name, benchmarkActions);
        }

        //======================================================== Instance API

        internal Profile Clone()
        {
            return new Profile(this.Name, this.Actions.Select(action => action.Clone()).ToList());
        }

        private bool _running;
        internal async Task ExecuteAsync()
        {
            _running = true;

            await Task.Delay(RNG.Get(0, 2000));

            while (_running)
            {
                for (var i = 0; i < this.Actions.Count; i++)
                {
                    if (!_running)
                        break;
                    try
                    {
                        await this.Actions[i].ExecuteAsync(this, "P" + this.Id + "A" + i + "x");
                    }
                    catch (Exception e)
                    {
                        Program.AddError(e, this, i, this.Actions[i]);
                    }
                }
            }
            Program.StoppedProfiles++;
        }
        internal void Stop()
        {
            _running = false;
        }

        //======================================================== IExecutionContext members

        private static readonly Dictionary<string, object> GlobalScope = new Dictionary<string, object>();
        private readonly Dictionary<string, object> _localScope = new Dictionary<string, object>();

        void IExecutionContext.SetVariable(string name, object value)
        {
            GetScope(name)[name] = value;
        }

        object IExecutionContext.GetVariable(string name)
        {
            object value;
            return GetScope(name).TryGetValue(name, out value) ? value : null;
        }

        string IExecutionContext.ReplaceTemplates(string input)
        {
            // because interface implementation is explicit
            var ctx = (IExecutionContext)this;

            var result = input;
            while (true)
            {
                int p0;
                int p1;
                if ((p0 = result.IndexOf("<<", StringComparison.Ordinal)) < 0)
                    break;
                if ((p1 = result.IndexOf(">>", p0, StringComparison.Ordinal)) < 0)
                    throw new ApplicationException("Invalid template: " + input);

                var templateName = input.Substring(p0, p1 - p0 + 2);
                var variableName = input.Substring(p0 + 2, p1 - p0 - 2);

                var templateValue = ctx.GetVariable(variableName) ?? string.Empty;

                result = result.Replace(templateName, templateValue.ToString());
            }

            return result;
        }

        private Dictionary<string, object> GetScope(string name)
        {
            return name[1] == VariableExpression.VariableStart ? GlobalScope : _localScope;
        }
    }
}
