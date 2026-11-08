using System;
using System.Threading;
using Jint;
using Jint.Runtime;

namespace TheFarmerWasRefactored.V8Probe
{
    internal static class JintOutputChecks
    {
        internal static void Run(string javascript)
        {
            var owner = Thread.CurrentThread.ManagedThreadId;
            var calls = 0;
            using (var engine = Create())
            {
                engine.SetValue("doAFlip", new Action(() =>
                {
                    if (Thread.CurrentThread.ManagedThreadId != owner)
                        throw new InvalidOperationException("Callback changed threads.");
                    calls++;
                }));
                engine.Execute(javascript);
                if (calls != 2) throw new InvalidOperationException("Expected two Jint stub calls.");
            }
            // The same emitted program must respect a deliberately insufficient budget.
            using (var limited = Create(1))
            {
                var limitedCalls = 0;
                limited.SetValue("doAFlip", new Action(() => limitedCalls++));
                try
                {
                    limited.Execute(javascript);
                    throw new InvalidOperationException("Statement budget did not stop emitted code.");
                }
                catch (StatementsCountOverflowException)
                {
                    if (limitedCalls >= 2)
                        throw new InvalidOperationException("Budget exhausted only after both calls.");
                }
            }
        }

        private static Engine Create(int statements = 1000) => new Engine(options => options
            .MaxStatements(statements).TimeoutInterval(TimeSpan.FromSeconds(2))
            .LimitMemory(8_000_000).LimitRecursion(64));
    }
}
