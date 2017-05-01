namespace SnBenchmark.Expression
{
    internal interface IExecutionContext
    {
        Profile CurrentProfile { get; }

        void SetVariable(string name, object value);

        object GetVariable(string name);

        string ReplaceTemplates(string input);
    }
}
