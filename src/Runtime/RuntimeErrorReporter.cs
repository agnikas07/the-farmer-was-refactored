using System;
using System.Threading;

namespace TheFarmerWasRefactored.Runtime
{
    public static class RuntimeErrorReporter
    {
        // Report only execution failures. Compilation has its own precise diagnostics.
        // Preserve the exception so logs and native execution cleanup remain unchanged.
        public static void Execute(Action execute, CancellationToken stop, Action<Exception> report)
        {
            try { execute(); }
            catch (Exception error)
            {
                if (!stop.IsCancellationRequested && !(error is OperationCanceledException))
                    report?.Invoke(error);
                throw;
            }
        }

        public static CompilerDiagnosticException ForEditor(Exception error)
        {
            if (error is CompilerDiagnosticException compiler) return compiler;
            var message = error.Message ?? error.GetType().Name;
            // Bound popup size and prevent player-thrown text becoming rich-text markup.
            message = message.Replace("<", "‹").Replace(">", "›");
            if (message.Length > 600) message = message.Substring(0, 600) + "…";
            return new CompilerDiagnosticException(error.ToString(),
                "Runtime error: " + message + "\nRun stopped. Exact source location is not available yet.", 0, 1);
        }
    }
}
