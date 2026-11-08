using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Jint;
using TheFarmerWasRefactored.Runtime;

namespace TheFarmerWasRefactored
{
    // Diagnostic only: one worker, one native execution, two commands.
    internal sealed class JavaScriptActionRun : IDisposable
    {
        internal readonly Execution Execution;
        private readonly ActionMailbox mailbox = new ActionMailbox();
        private readonly CancellationTokenSource stop = new CancellationTokenSource();
        private readonly Action<string> report;
        private readonly Task worker;
        private int disposed, yields;
        internal bool YieldRequested;
        internal bool Waiting;
        internal bool NeedsWorker => Waiting && !mailbox.HasPending && !worker.IsCompleted
            && Volatile.Read(ref disposed) == 0;

        internal JavaScriptActionRun(Execution execution, Action<string> report)
        {
            Execution = execution;
            this.report = report;
            stop.CancelAfter(TimeSpan.FromSeconds(30)); // Diagnostic watchdog, not action timing.
            worker = Task.Run(() =>
            {
                using (stop.Token.Register(mailbox.Dispose))
                using (var engine = new Engine(options => options.CancellationToken(stop.Token)
                    .MaxStatements(100).LimitMemory(8_000_000).LimitRecursion(64)))
                {
                    engine.SetValue("doAFlip", new Action(mailbox.DoAFlip));
                    engine.Execute("doAFlip(); doAFlip();");
                }
            });
        }

        internal void RecordYield()
        {
            if (++yields == 1) report("JavaScript action probe: yielded native scheduler without charging operations");
        }

        internal IEnumerable<double> Commands(Node node, ProgramState state)
        {
            var completed = 0;
            while (Volatile.Read(ref disposed) == 0)
            {
                if (!mailbox.TryClaim(out var ticket))
                {
                    if (worker.IsCompleted)
                    {
                        Waiting = false;
                        if (worker.IsFaulted)
                            report($"JavaScript action probe FAILED: {worker.Exception.GetBaseException().Message}");
                        else if (worker.IsCanceled || completed != 2)
                            report("JavaScript action probe: cancelled/incomplete");
                        else report("JavaScript action probe: two doAFlip calls completed ALL PASS");
                        yield break;
                    }
                    Waiting = YieldRequested = true;
                    // Causes PerformExecutionStep to return after this yield, without a cost.
                    state.hitStoppingPoint = true;
                    yield return 0;
                    Waiting = false;
                    continue;
                }
                var before = state.OpCount;
                state.currentDependencies.Add(("do_a_flip", node.boxedParams.wordStart, node.boxedParams.wordEnd));
                var ops = BuiltinFunctions.Functions["do_a_flip"].binding(
                    new List<IPyObject>(), Execution.sim, Execution, state.DroneId);
                if (node.CheckIncrementOpCount(state, Execution, ops)) yield return 0;
                var charged = state.OpCount - before;
                if (state.currentSideEffect != SideEffect.None || !(state.ReturnValue is PyNone) || charged <= 0)
                {
                    mailbox.Fail(ticket, new InvalidOperationException("Unexpected native completion."));
                    yield break;
                }
                if (!mailbox.Complete(ticket)) yield break;
                completed++;
                report($"JavaScript action probe: doAFlip={completed} chargedOps={charged}");
            }
        }

        public void Dispose()
        {
            if (Interlocked.Exchange(ref disposed, 1) != 0) return;
            mailbox.Dispose();
            stop.Cancel();
            // Never join a worker while holding a native lock.
            _ = worker.ContinueWith(task =>
            {
                _ = task.Exception;
                report("JavaScript action probe: worker released");
                stop.Dispose();
            }, TaskScheduler.Default);
        }
    }
}
