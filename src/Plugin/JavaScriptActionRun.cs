using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Jint;
using TheFarmerWasRefactored.Runtime;

namespace TheFarmerWasRefactored
{
    // Diagnostic only: one worker and one native execution.
    internal sealed class JavaScriptActionRun : IDisposable
    {
        internal readonly Execution Execution;
        private readonly ActionMailbox mailbox = new ActionMailbox();
        private readonly CancellationTokenSource stop = new CancellationTokenSource();
        private readonly Action<string> report;
        private readonly Task worker;
        private readonly int? expectedCalls;
        private readonly SourceRunTiming timing;
        private int disposed, yields;
        internal bool YieldRequested;
        internal bool Waiting;
        internal bool NeedsWorker => Waiting && !mailbox.HasPending && !worker.IsCompleted
            && Volatile.Read(ref disposed) == 0;

        internal JavaScriptActionRun(Execution execution, Action<string> report, string javascript = "doAFlip(); doAFlip();",
            Func<CancellationToken, string> prepare = null, int? expectedCalls = 2, SourceRunTiming timing = null)
        {
            Execution = execution;
            this.expectedCalls = expectedCalls;
            this.timing = timing;
            this.report = report;
            stop.CancelAfter(TimeSpan.FromSeconds(30)); // Diagnostic watchdog, not action timing.
            worker = Task.Run(() =>
            {
                timing?.Mark("worker_started");
                stop.Token.ThrowIfCancellationRequested();
                if (prepare != null)
                {
                    javascript = prepare(stop.Token);
                    report("JavaScript action probe: source compiled");
                }
                stop.Token.ThrowIfCancellationRequested();
                using (stop.Token.Register(mailbox.Dispose))
                using (var engine = new Engine(options => options.CancellationToken(stop.Token)
                    .MaxStatements(100).LimitMemory(8_000_000).LimitRecursion(64)))
                {
                    var requests = 0;
                    engine.SetValue("doAFlip", new Action(() =>
                    {
                        if (++requests == 1) timing?.Mark("first_request");
                        mailbox.DoAFlip();
                    }));
                    engine.SetValue("canHarvest", new Func<bool>(() =>
                    {
                        if (++requests == 1) timing?.Mark("first_request");
                        return mailbox.CanHarvest();
                    }));
                    engine.SetValue("harvest", new Func<bool>(() =>
                    {
                        if (++requests == 1) timing?.Mark("first_request");
                        return mailbox.Harvest();
                    }));
                    timing?.Mark("player_ready");
                    engine.Execute(javascript);
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
            var flips = 0;
            var queries = 0;
            var harvests = 0;
            while (Volatile.Read(ref disposed) == 0)
            {
                if (!mailbox.TryClaim(out var ticket))
                {
                    if (worker.IsCompleted)
                    {
                        Waiting = false;
                        if (worker.IsFaulted)
                            report($"JavaScript action probe FAILED: {worker.Exception.GetBaseException().Message}");
                        else if (worker.IsCanceled || (expectedCalls.HasValue && completed != expectedCalls.Value))
                            report("JavaScript action probe: cancelled/incomplete");
                        else if (!expectedCalls.HasValue) report($"JavaScript action probe: completed {flips} doAFlip calls; {queries} canHarvest calls; {harvests} harvest calls");
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
                if (completed == 0) timing?.Mark("first_claim");
                var before = state.OpCount;
                var query = ticket.Action == ActionMailbox.Command.CanHarvest;
                var harvesting = ticket.Action == ActionMailbox.Command.Harvest;
                var returnsBoolean = query || harvesting;
                var nativeName = ticket.Action switch
                {
                    ActionMailbox.Command.DoAFlip => "do_a_flip",
                    ActionMailbox.Command.CanHarvest => "can_harvest",
                    ActionMailbox.Command.Harvest => "harvest",
                    _ => throw new InvalidOperationException("Unsupported farm command.")
                };
                state.currentDependencies.Add((nativeName, node.boxedParams.wordStart, node.boxedParams.wordEnd));
                var ops = BuiltinFunctions.Functions[nativeName].binding(
                    new List<IPyObject>(), Execution.sim, Execution, state.DroneId);
                if (completed == 0) timing?.Mark("first_binding_returned");
                if (node.CheckIncrementOpCount(state, Execution, ops)) yield return 0;
                var charged = state.OpCount - before;
                if (state.currentSideEffect != SideEffect.None || (returnsBoolean ? !(state.ReturnValue is PyBool) : !(state.ReturnValue is PyNone)) || charged <= 0)
                {
                    mailbox.Fail(ticket, new InvalidOperationException("Unexpected native completion."));
                    yield break;
                }
                var result = returnsBoolean && ((PyBool)state.ReturnValue).num != 0;
                if (!mailbox.Complete(ticket, result)) yield break;
                completed++;
                if (completed == 1) timing?.Mark("first_action_completed");
                if (query)
                {
                    queries++;
                    report($"JavaScript action probe: canHarvest={result.ToString().ToLowerInvariant()} chargedOps={charged}");
                }
                else if (harvesting)
                {
                    harvests++;
                    report($"JavaScript action probe: harvest={result.ToString().ToLowerInvariant()} chargedOps={charged}");
                }
                else report($"JavaScript action probe: doAFlip={++flips} chargedOps={charged}");
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
                timing?.Mark("worker_released");
                report("JavaScript action probe: worker released");
                stop.Dispose();
            }, TaskScheduler.Default);
        }
    }
}
