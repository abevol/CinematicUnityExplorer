using System.Text;

namespace UnityExplorer.CSConsole
{
    public sealed class ScriptEvaluatorResult
    {
        private readonly StringBuilder output;

        public StringWriter Writer { get; }

        public ScriptEvaluatorResult()
        {
            output = new StringBuilder();
            Writer = new StringWriter(output);
        }

        public void Clear() => output.Clear();

        public override string ToString() => output.ToString();
    }
}
