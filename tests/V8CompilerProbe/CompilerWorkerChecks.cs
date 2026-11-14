using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Jint;
using TheFarmerWasRefactored.V8Probe;
using TheFarmerWasRefactored.Runtime;

internal static class CompilerWorkerChecks
{
    internal static void Run(string bundlePath)
    {
        var logs = new ConcurrentQueue<string>();
        var threads = new ConcurrentBag<int>();
        using var worker = new CompilerWorker(bundlePath, logs.Enqueue);
        Action<string> mark = _ => threads.Add(Thread.CurrentThread.ManagedThreadId);
        foreach (var count in new[] { 1, 3, 0 })
        {
            var js = worker.Compile($"const count: number = {count}; for(let i=0;i<count;i++) doAFlip();", mark, default);
            var calls = 0;
            using var engine = new Engine(options => options.MaxStatements(100));
            engine.SetValue("doAFlip", new Action(() => calls++));
            engine.Execute(js);
            Check(calls == count, "Wrong output or stale source.");
        }
        Reject(() => worker.Compile("const count: number = 'two';", mark, default), "2322");
        Reject(() => worker.Compile("count;", mark, default), "2304"); // Previous declarations must not leak.
        worker.Compile("doAFlip();", mark, default);
        foreach (var newline in new[] { "\n", "\r\n" })
        {
            var source = "// @refactored" + newline + "// 🌱" + newline + "const count: number = 'two';";
            try { worker.Compile(source, mark, default); throw new Exception("Expected structured diagnostic."); }
            catch (CompilerDiagnosticException error)
            {
                var span = error.Span(source);
                Check(error.DisplayMessage.StartsWith("TypeScript TS2322:"), "Missing readable diagnostic.");
                Check(span.Start == source.IndexOf("count", StringComparison.Ordinal)
                    && source.Substring(span.Start, span.End - span.Start + 1) == "count",
                    "Diagnostic span lost UTF-16/CRLF position or inclusive end.");
            }
        }
        var edge = new CompilerDiagnosticException("", "", int.MaxValue, int.MaxValue).Span("abc");
        Check(edge == (2, 2), "Oversized diagnostic span was not clamped.");
        using (var cancel = new CancellationTokenSource())
        using (var entered = new ManualResetEventSlim())
        using (var release = new ManualResetEventSlim())
        {
            var pending = Task.Run(() => worker.Compile("doAFlip();", stage =>
            {
                mark(stage);
                if (stage != "compiler_request_started") return;
                entered.Set();
                if (!release.Wait(TimeSpan.FromSeconds(10))) throw new TimeoutException("Test barrier timed out.");
            }, cancel.Token));
            try
            {
                Check(entered.Wait(TimeSpan.FromSeconds(10)), "Request never reached compiler worker.");
                cancel.Cancel();
                Check(SpinWait.SpinUntil(() => pending.IsCompleted, 2000), "Cancellation waited for the compiler.");
                try { pending.GetAwaiter().GetResult(); throw new Exception("Cancelled request returned output."); }
                catch (OperationCanceledException) { }
            }
            finally { release.Set(); }
            worker.Compile("doAFlip();", mark, default); // Serialized after abandoned result; recovery.
        }
        Check(threads.Distinct().Count() == 1 && threads.First() != Thread.CurrentThread.ManagedThreadId,
            "Compiler work changed threads or ran on caller.");
        worker.Dispose();
        Check(worker.Completion.Wait(TimeSpan.FromSeconds(10)), "Compiler failed to exit.");
        Check(logs.Count(line => line.Contains(": ready")) == 1, "Compiler was reloaded.");
        Check(logs.Count(line => line.EndsWith(": released")) == 1, "Owner failed to release engine.");
        try { worker.Compile("doAFlip();", mark, default); throw new Exception("Disposed worker accepted input."); }
        catch (ObjectDisposedException) { }
        using (var closing = new CompilerWorker(bundlePath, _ => { }))
        using (var entered = new ManualResetEventSlim())
        using (var release = new ManualResetEventSlim())
        {
            var pending = Task.Run(() => closing.Compile("doAFlip();", stage =>
            {
                if (stage != "compiler_request_started") return;
                entered.Set();
                if (!release.Wait(TimeSpan.FromSeconds(10))) throw new TimeoutException();
            }, default));
            try
            {
                Check(entered.Wait(TimeSpan.FromSeconds(10)), "Shutdown test never started.");
                closing.Dispose();
                Check(SpinWait.SpinUntil(() => pending.IsCompleted, 2000), "Shutdown did not release caller.");
                try { pending.GetAwaiter().GetResult(); throw new Exception("Shutdown returned output."); }
                catch (OperationCanceledException) { }
            }
            finally { release.Set(); }
            Check(closing.Completion.Wait(TimeSpan.FromSeconds(10)), "Shutdown failed to release owner.");
        }
        using var broken = new CompilerWorker(bundlePath + ".missing", _ => { });
        Check(broken.Completion.Wait(TimeSpan.FromSeconds(10)), "Failed initialization hung.");
        Reject(() => broken.Compile("doAFlip();", mark, default), "initialize");
        Console.WriteLine("Compiler worker: reuse, source isolation, rejection/recovery, cancellation, ownership, disposal, initialization failure PASS");
    }

    private static void Reject(Action action, string message)
    {
        try { action(); }
        catch (InvalidOperationException error) when (error.Message.Contains(message)) { return; }
        throw new Exception("Expected rejection: " + message);
    }

    private static void Check(bool value, string message)
    { if (!value) throw new Exception(message); }
}
