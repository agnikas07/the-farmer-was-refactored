using System;
using System.Threading;
using Jint;
using Jint.Runtime;

namespace TheFarmerWasRefactored.Runtime
{
    // Feasibility diagnostics only: this is not the game's execution scheduler.
    public static class RuntimeProbe
    {
        private static Engine Create(CancellationToken token, int statements = 10000)
        {
            return new Engine(options => options
                .CancellationToken(token)
                .MaxStatements(statements)
                .TimeoutInterval(TimeSpan.FromSeconds(2))
                .LimitMemory(8_000_000)
                .LimitRecursion(64));
        }

        public static void Run(Action<string> report)
        {
            string message = null;
            using (var engine = Create(CancellationToken.None))
            {
                engine.SetValue("log", new Action<string>(text => message = text));
                var result = engine.Evaluate("log('hello from JavaScript'); [1, 2, 3].map(x => x * 2).reduce((a, b) => a + b, 0)");
                if (result.AsNumber() != 12 || message != "hello from JavaScript")
                    throw new InvalidOperationException("JavaScript result or host callback mismatch.");
                report("Runtime probe: evaluation and host logging PASS");
            }

            using (var engine = Create(CancellationToken.None, 100))
            {
                try
                {
                    engine.Execute("while (true) {}");
                    throw new InvalidOperationException("Infinite loop unexpectedly returned.");
                }
                catch (StatementsCountOverflowException)
                {
                    report("Runtime probe: statement limit PASS");
                }
            }

            using (var cancellation = new CancellationTokenSource())
            using (var engine = Create(cancellation.Token, int.MaxValue))
            {
                // Trigger cancellation after execution starts, from another thread.
                engine.SetValue("started", new Action(() => cancellation.CancelAfter(25)));
                try
                {
                    engine.Execute("started(); while (true) {}");
                    throw new InvalidOperationException("Cancelled loop unexpectedly returned.");
                }
                catch (ExecutionCanceledException)
                {
                    report("Runtime probe: running-loop cancellation PASS");
                }
            }

            using (var engine = Create(CancellationToken.None))
            {
                if (engine.Evaluate("6 * 7").AsNumber() != 42)
                    throw new InvalidOperationException("Fresh engine failed after cancellation.");
            }
            report("Runtime probe: fresh engine after cancellation PASS");
            report("Runtime probe: ALL PASS");
        }
    }
}
