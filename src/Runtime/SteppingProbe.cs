using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using Jint;
using Jint.Runtime;
using Jint.Runtime.Debugger;

namespace TheFarmerWasRefactored.Runtime
{
    // Diagnostic only. The engine and all its objects stay on one background thread.
    public static class SteppingProbe
    {
        private sealed class Session : IDisposable
        {
            private readonly CancellationTokenSource stop;
            private readonly AutoResetEvent permit = new AutoResetEvent(false);
            private readonly AutoResetEvent paused = new AutoResetEvent(false);
            private readonly ManualResetEvent active = new ManualResetEvent(false);
            private readonly Task worker;
            private volatile bool running;
            public int Line;
            public readonly ConcurrentQueue<int> Values = new ConcurrentQueue<int>();

            public Session(string source, CancellationToken token)
            {
                stop = CancellationTokenSource.CreateLinkedTokenSource(token);
                worker = Task.Factory.StartNew(() =>
                {
                    try
                    {
                        using (var engine = new Engine(options =>
                        {
                            options.Debugger.Enabled = true;
                            options.Debugger.InitialStepMode = StepMode.Into;
                            options.CancellationToken(stop.Token).LimitMemory(8_000_000).LimitRecursion(64);
                        }))
                        {
                            engine.SetValue("record", new Action<int>(value =>
                            {
                                Values.Enqueue(value);
                                active.Set();
                            }));
                            engine.Debugger.Step += (_, info) =>
                            {
                                stop.Token.ThrowIfCancellationRequested();
                                if (!running)
                                {
                                    Line = info.Location.Start.Line;
                                    paused.Set();
                                    Wait(permit);
                                }
                                return StepMode.Into;
                            };
                            engine.Execute(source);
                        }
                    }
                    catch (OperationCanceledException) when (stop.IsCancellationRequested) { }
                    catch (ExecutionCanceledException) when (stop.IsCancellationRequested) { }
                }, CancellationToken.None, TaskCreationOptions.LongRunning, TaskScheduler.Default);
            }

            private void Wait(WaitHandle signal)
            {
                if (WaitHandle.WaitAny(new[] { signal, stop.Token.WaitHandle }, 2000) == WaitHandle.WaitTimeout)
                    throw new TimeoutException("Stepping probe handshake timed out.");
                stop.Token.ThrowIfCancellationRequested();
            }

            public void WaitPaused() => Wait(paused);
            public void WaitActive() => Wait(active);
            public void Step() => permit.Set();
            public void Resume() { running = true; permit.Set(); }
            public void Pause() { running = false; }
            public void Stop() => stop.Cancel();
            public void Finish()
            {
                if (!worker.Wait(2000)) throw new TimeoutException("Stepping worker did not finish.");
                worker.GetAwaiter().GetResult();
            }

            public void Dispose()
            {
                Stop();
                try { Finish(); }
                finally
                {
                    // Never dispose a wait handle while a worker could still use it.
                    if (worker.IsCompleted)
                    {
                        permit.Dispose(); paused.Dispose(); active.Dispose(); stop.Dispose();
                    }
                }
            }
        }

        private static void Check(bool condition, string message)
        {
            if (!condition) throw new InvalidOperationException(message);
        }

        public static void Run(Action<string> report, CancellationToken token = default)
        {
            using (var deadline = CancellationTokenSource.CreateLinkedTokenSource(token))
            {
                deadline.CancelAfter(TimeSpan.FromSeconds(10));
                using (var session = new Session("let n = 40;\nn++;\nrecord(n);\nn++;\nrecord(n);", deadline.Token))
                {
                    session.WaitPaused();
                    Check(session.Line == 1 && session.Values.IsEmpty, "Script ran before the first step.");
                    for (var line = 2; line <= 4; line++)
                    {
                        session.Step(); session.WaitPaused();
                        Check(session.Line == line, "Step skipped a source location.");
                    }
                    Check(session.Values.TryDequeue(out var first) && first == 41 && session.Values.IsEmpty,
                        "Stepping did not preserve local state or executed too far.");
                    report("Stepping probe: source stepping and preserved state PASS");
                    session.Resume(); session.Finish();
                    Check(session.Values.TryDequeue(out var last) && last == 42 && session.Values.IsEmpty,
                        "Resume restarted or lost script state.");
                    report("Stepping probe: resume to completion PASS");
                }
                foreach (var pauseBeforeStop in new[] { true, false })
                {
                    using (var session = new Session("record(1); while (true) {}", deadline.Token))
                    {
                        session.WaitPaused(); session.Resume(); session.WaitActive();
                        if (pauseBeforeStop) { session.Pause(); session.WaitPaused(); }
                        session.Stop(); session.Finish();
                        report(pauseBeforeStop ? "Stepping probe: pause running script and stop PASS"
                            : "Stepping probe: stop running script PASS");
                    }
                }
            }
            report("Stepping probe: ALL PASS");
        }
    }
}
