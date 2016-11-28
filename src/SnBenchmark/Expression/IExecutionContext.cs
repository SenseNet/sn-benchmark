namespace SnBenchmark.Expression
{
    internal interface IExecutionContext
    {
        void SetVariable(string name, object value);

        object GetVariable(string name);

        string ReplaceTemplates(string input);
    }
}
