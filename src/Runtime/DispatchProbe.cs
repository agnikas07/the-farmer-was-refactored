using System;
using System.Threading;
using System.Threading.Tasks;
using Jint;

namespace TheFarmerWasRefactored.Runtime
{
    // Main-thread controller. Tick never waits for the worker.
    public sealed class DispatchProbe : IDisposable
    {
        private readonly CancellationTokenSource stop = new CancellationTokenSource();
        private readonly MainThreadMailbox mailbox;
        private readonly Action<string> report;
        private Task<int> worker;
        private int phase;
        private int calls;
        private bool disposed;
        private bool cancelledQueued;
        public bool IsCompleted { get; private set; }

        public DispatchProbe(Action<string> report, Func<int, int> hostAction)
        {
            this.report = report;
            mailbox = new MainThreadMailbox(value =>
            {
                calls++;
                if (value == -1) throw new InvalidOperationException("Expected diagnostic failure.");
                return hostAction(value);
            });
            stop.CancelAfter(TimeSpan.FromSeconds(10));
            worker = Task.Run(() =>
            {
                using (var engine = new Engine(options => options.CancellationToken(stop.Token)
                    .MaxStatements(1000).LimitMemory(8_000_000).LimitRecursion(64)))
                {
                    engine.SetValue("request", new Func<int, int>(value => mailbox.Invoke(value, stop.Token)));
                    return (int)engine.Evaluate("request(20) + request(21)").AsNumber();
                }
            });
        }

        public void Tick()
        {
            if (disposed) throw new ObjectDisposedException(nameof(DispatchProbe));
            if (IsCompleted) return;
            // For the final test, cancel while queued, before giving the owner a chance to claim it.
            if (phase == 2 && mailbox.HasPending) { cancelledQueued = true; stop.Cancel(); }
            mailbox.Pump();
            if (!worker.IsCompleted) return;
            var result = worker.GetAwaiter().GetResult();
            if (phase == 0)
            {
                Check(result == 43 && calls == 2, "JavaScript request/result round trip failed.");
                report("Dispatch probe: JavaScript main-thread round trip PASS");
                phase = 1;
                worker = Task.Run(() =>
                {
                    try { mailbox.Invoke(-1, stop.Token); }
                    catch (InvalidOperationException error) when (error.Message == "Expected diagnostic failure.")
                    { return 1; }
                    throw new InvalidOperationException("Host failure was not propagated.");
                });
            }
            else if (phase == 1)
            {
                Check(result == 1 && calls == 3, "Host error test failed.");
                report("Dispatch probe: host error propagation PASS");
                phase = 2;
                worker = Task.Run(() =>
                {
                    try { mailbox.Invoke(999, stop.Token); }
                    catch (OperationCanceledException) { return 1; }
                    throw new InvalidOperationException("Cancelled request unexpectedly completed.");
                });
            }
            else
            {
                Check(cancelledQueued && result == 1 && calls == 3, "Queued cancellation was not verified.");
                report("Dispatch probe: queued cancellation without side effects PASS");
                report("Dispatch probe: ALL PASS");
                IsCompleted = true;
            }
        }

        private static void Check(bool condition, string message)
        {
            if (!condition) throw new InvalidOperationException(message);
        }

        public void Dispose()
        {
            if (disposed) return;
            disposed = true;
            stop.Cancel(); // Release any waiting worker; never join it on the main thread.
            _ = worker.ContinueWith(completed =>
            {
                _ = completed.Exception; // Observe failures if the scene closed mid-probe.
                stop.Dispose();
            }, TaskScheduler.Default);
        }
    }
}
