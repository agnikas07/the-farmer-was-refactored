using System;
using System.Threading;
using Jint;
using TheFarmerWasRefactored.Runtime;

internal static class RuntimeErrorChecks
{
    internal static void Run()
    {
        Exception shown = null;
        using (var engine = new Engine(options => options.Constraint(new FarmExecutionBudget())))
        {
            try
            {
                RuntimeErrorReporter.Execute(() => engine.Execute("while (true) {}"),
                    CancellationToken.None, error => shown = error);
                throw new Exception("Expected computation failure.");
            }
            catch (FarmComputationLimitException error)
            {
                Check(ReferenceEquals(error, shown), "Failure was not preserved and reported.");
            }
        }
        var diagnostic = RuntimeErrorReporter.ForEditor(shown);
        Check(diagnostic.DisplayMessage.Contains("Computation limit"), "Missing budget explanation.");
        Check(diagnostic.DisplayMessage.Contains("Exact source location"), "Missing location limitation.");
        Check(diagnostic.Span("") == (0, 0), "Empty-source span unsafe.");

        using (var engine = new Engine())
        {
            shown = null;
            try
            {
                RuntimeErrorReporter.Execute(() => engine.Execute("throw new Error('test failure');"),
                    CancellationToken.None, error => shown = error);
            }
            catch (Jint.Runtime.JavaScriptException) { }
            Check(shown != null && RuntimeErrorReporter.ForEditor(shown).DisplayMessage.Contains("test failure"),
                "Player exception not reported.");
            engine.Execute("let recovered = true;");
        }

        using (var stop = new CancellationTokenSource())
        {
            stop.Cancel();
            shown = null;
            try
            {
                RuntimeErrorReporter.Execute(() => throw new InvalidOperationException("stop race"),
                    stop.Token, error => shown = error);
            }
            catch (InvalidOperationException) { }
            Check(shown == null, "Stop showed an error.");
        }
        shown = null;
        RuntimeErrorReporter.Execute(() => { }, CancellationToken.None, error => shown = error);
        Check(shown == null, "Success showed an error.");
        var compiler = new CompilerDiagnosticException("details", "TypeScript TS2322", 4, 3);
        Check(ReferenceEquals(compiler, RuntimeErrorReporter.ForEditor(compiler)), "Compiler span changed.");
        var escaped = RuntimeErrorReporter.ForEditor(new Exception("<b>" + new string('x', 1000)));
        Check(!escaped.DisplayMessage.Contains("<") && escaped.DisplayMessage.Length < 750,
            "Unbounded or rich-text popup.");
        Console.WriteLine("Runtime errors: budget, thrown error, Stop, recovery and display PASS");
    }
    private static void Check(bool condition, string message)
    {
        if (!condition) throw new Exception(message);
    }
}
