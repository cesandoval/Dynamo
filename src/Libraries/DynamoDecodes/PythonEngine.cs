using System.Collections.Generic;
using System.Linq;
using Dynamo;

namespace DynamoPython
{
    public static class PythonEngineDecodes
    {
        public delegate FScheme.Value EvaluationDelegate(
            bool dirty, string script, IEnumerable<KeyValuePair<string, dynamic>> bindings,
            IEnumerable<KeyValuePair<string, FScheme.Value>> inputs);

        public delegate void DrawDelegate(FScheme.Value val, string id);

        public static EvaluationDelegate Evaluator;

        public static DrawDelegate Drawing;

        private static readonly DecodesPythonEngine Engine = new DecodesPythonEngine();

        static PythonEngineDecodes()
        {
            Evaluator =
                delegate(bool dirty, string script,
                         IEnumerable<KeyValuePair<string, dynamic>> bindings,
                         IEnumerable<KeyValuePair<string, FScheme.Value>> inputs)
                {
                    if (dirty)
                    {
                        Engine.ProcessCode(script);
                        dirty = false;
                    }

                    return Engine.Evaluate(DecodesPythonBindings.Bindings.Concat(bindings), inputs);
                };

            Drawing = delegate { };
        }
    }
}
