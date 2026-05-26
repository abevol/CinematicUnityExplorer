using Mono.CSharp;

namespace UnityExplorer.CSConsole
{
    public sealed class ConsoleScriptEvaluator : IDisposable
    {
        private ScriptEvaluator evaluator;
        private ScriptEvaluatorResult result;

        public ScriptEvaluator Evaluator => evaluator;

        public override string ToString() => result?.ToString();

        public string[] GetCompletions(string input, out string prefix)
        {
            prefix = string.Empty;
            return evaluator?.GetCompletions(input, out prefix);
        }

        public void ClearOutput() => result?.Clear();

        public CompiledMethod Compile(string text) => evaluator?.Compile(text);

        public void Initialize()
        {
            if (evaluator == null)
                Recreate();

            if (result == null)
            {
                result = new ScriptEvaluatorResult();
                evaluator.TextWriter = result.Writer;
            }
        }

        public void Recreate()
        {
            evaluator?.Dispose();

            result = new ScriptEvaluatorResult();
            evaluator = new ScriptEvaluator(result.Writer)
            {
                InteractiveBaseClass = typeof(ScriptInteraction)
            };
        }

        public void Dispose() => evaluator?.Dispose();
    }
}
