using System;
using System.Threading;
using System.Threading.Tasks;
using Jint;
using TheFarmerWasRefactored.Runtime;

internal static class ActionMailboxChecks
{
    public static void Run()
    {
        using (var mailbox = new ActionMailbox())
        using (var stop = new CancellationTokenSource(TimeSpan.FromSeconds(5)))
        {
            var progress = 0;
            var worker = Task.Run(() =>
            {
                using var engine = new Engine(options => options.CancellationToken(stop.Token).MaxStatements(100));
                engine.SetValue("doAFlip", new Action(mailbox.DoAFlip));
                engine.SetValue("reportProgress", new Action<int>(value => Volatile.Write(ref progress, value)));
                engine.Execute("doAFlip(); reportProgress(1); doAFlip(); reportProgress(2);");
                Check(engine.Evaluate("typeof do_a_flip").AsString() == "undefined", "Snake-case alias leaked.");
            });
            Wait(() => mailbox.HasPending);
            Check(mailbox.TryClaim(out var first), "First command missing.");
            Check(first.Action == ActionMailbox.Command.DoAFlip, "Wrong command.");
            Check(!worker.IsCompleted && Volatile.Read(ref progress) == 0, "Claim released script early.");
            Check(!mailbox.TryClaim(out _), "Claimed command delivered twice.");
            var overlap = Task.Run(() => Expect<InvalidOperationException>(mailbox.DoAFlip));
            Join(overlap); // Capacity includes the applied-but-not-completed interval.
            Check(mailbox.Complete(first), "First completion rejected.");
            Wait(() => mailbox.HasPending);
            Check(Volatile.Read(ref progress) == 1, "Script did not resume after completion.");
            Check(mailbox.TryClaim(out var second), "Second command missing.");
            Check(!mailbox.Complete(first), "Stale completion accepted.");
            Check(!worker.IsCompleted && Volatile.Read(ref progress) == 1, "Second command completed early.");
            Check(mailbox.Complete(second), "Second completion rejected.");
            Join(worker);
            Check(progress == 2, "Script failed to finish.");
        }

        using (var mailbox = new ActionMailbox())
        {
            Expect<InvalidOperationException>(mailbox.DoAFlip);
            Join(Task.Run(() => Expect<InvalidOperationException>(() => mailbox.TryClaim(out _))));
            var worker = Task.Run(() => Expect<OperationCanceledException>(mailbox.DoAFlip));
            Wait(() => mailbox.HasPending);
            mailbox.Dispose();
            Check(!mailbox.TryClaim(out _), "Queued cancellation allowed application.");
            Join(worker);
            Join(Task.Run(() => Expect<OperationCanceledException>(mailbox.DoAFlip)));
        }

        using (var mailbox = new ActionMailbox())
        {
            var worker = Task.Run(() => Expect<OperationCanceledException>(mailbox.DoAFlip));
            Wait(() => mailbox.HasPending);
            Check(mailbox.TryClaim(out var ticket), "Cancellation test did not claim.");
            Join(Task.Run(() => Expect<InvalidOperationException>(() => mailbox.Complete(ticket))));
            mailbox.Dispose();
            Check(!mailbox.Complete(ticket), "Cancelled completion accepted.");
            Join(worker);
        }

        using (var mailbox = new ActionMailbox())
        {
            var expected = new InvalidOperationException("native action failed");
            var worker = Task.Run(() =>
            {
                try { mailbox.DoAFlip(); }
                catch (InvalidOperationException error) when (ReferenceEquals(error, expected)) { return; }
                throw new Exception("Host error did not reach worker.");
            });
            Wait(() => mailbox.HasPending);
            Check(mailbox.TryClaim(out var ticket), "Failure test did not claim.");
            using var replacement = new ActionMailbox();
            Check(!replacement.Complete(ticket), "Old run ticket accepted by replacement.");
            Check(mailbox.Fail(ticket, expected), "Failure rejected.");
            Join(worker);
        }
        foreach (var command in new[] { ActionMailbox.Command.CanHarvest, ActionMailbox.Command.Harvest })
        foreach (var ready in new[] { false, true })
        using (var mailbox = new ActionMailbox())
        {
            var worker = Task.Run(() =>
            {
                using var engine = new Engine(options => options.MaxStatements(100));
                engine.SetValue("canHarvest", new Func<bool>(mailbox.CanHarvest));
                engine.SetValue("harvest", new Func<bool>(mailbox.Harvest));
                engine.SetValue("doAFlip", new Action(mailbox.DoAFlip));
                var name = command == ActionMailbox.Command.Harvest ? "harvest" : "canHarvest";
                engine.Execute($"const ready = {name}(); if (ready) doAFlip();");
                Check(engine.Evaluate("ready").AsBoolean() == ready, "Boolean result changed.");
                Check(engine.Evaluate("typeof can_harvest").AsString() == "undefined", "Native alias leaked.");
            });
            Wait(() => mailbox.HasPending);
            Check(mailbox.TryClaim(out var query), "Missing query.");
            Check(query.Action == command, "Wrong boolean command.");
            Check(!worker.IsCompleted, "Query returned before native completion.");
            Check(mailbox.Complete(query, ready), "Query completion rejected.");
            if (ready)
            {
                Wait(() => mailbox.HasPending);
                Check(mailbox.TryClaim(out var flip), "Missing conditional flip.");
                Check(flip.Action == ActionMailbox.Command.DoAFlip, "Wrong conditional command.");
                Check(!mailbox.Complete(query, false), "Stale query result accepted.");
                Check(mailbox.Complete(flip), "Conditional flip rejected.");
            }
            Join(worker);
        }
        using (var mailbox = new ActionMailbox())
        {
            var worker = Task.Run(() => Expect<OperationCanceledException>(() => mailbox.CanHarvest()));
            Wait(() => mailbox.HasPending);
            mailbox.Dispose();
            Check(!mailbox.TryClaim(out _), "Cancelled query was claimed.");
            Join(worker);
        }
        foreach (var claimBeforeStop in new[] { false, true })
        using (var mailbox = new ActionMailbox())
        {
            var worker = Task.Run(() => Expect<OperationCanceledException>(() => mailbox.Harvest()));
            Wait(() => mailbox.HasPending);
            ActionMailbox.Ticket ticket = null;
            if (claimBeforeStop) Check(mailbox.TryClaim(out ticket), "Harvest not claimed.");
            mailbox.Dispose();
            Check(!mailbox.TryClaim(out _), "Stopped harvest claimed.");
            if (ticket != null) Check(!mailbox.Complete(ticket, true), "Stopped harvest completed.");
            Join(worker);
        }
        using (var mailbox = new ActionMailbox())
        {
            var worker = Task.Run(() =>
            {
                using var engine = new Engine(options => options.MaxStatements(100));
                engine.SetValue("harvest", new Func<bool>(mailbox.Harvest));
                engine.SetValue("canHarvest", new Func<bool>(mailbox.CanHarvest));
                engine.Execute("if (!harvest() || canHarvest()) throw new Error('Unexpected field state');");
            });
            Wait(() => mailbox.HasPending);
            Check(mailbox.TryClaim(out var harvest), "Harvest missing.");
            Check(harvest.Action == ActionMailbox.Command.Harvest, "Wrong first action.");
            Check(!mailbox.HasPending && !worker.IsCompleted, "Query ran before harvest completed.");
            Check(mailbox.Complete(harvest, true), "Harvest completion rejected.");
            Wait(() => mailbox.HasPending);
            Check(mailbox.TryClaim(out var query), "Post-harvest query missing.");
            Check(query.Action == ActionMailbox.Command.CanHarvest, "Wrong follow-up action.");
            Check(mailbox.Complete(query, false), "Post-harvest query rejected.");
            Join(worker);
        }
        Console.WriteLine("Action mailbox: harvest/query branching, sequencing and cancellation PASS");
        Console.WriteLine("Action mailbox: camelCase JS sequencing, completion gate, cancellation, identity and failure PASS");
    }

    private static void Wait(Func<bool> ready)
    {
        if (!SpinWait.SpinUntil(ready, TimeSpan.FromSeconds(5))) throw new TimeoutException("Action request timed out.");
    }

    private static void Join(Task worker)
    {
        if (!worker.Wait(TimeSpan.FromSeconds(5))) throw new TimeoutException("Action worker timed out.");
        worker.GetAwaiter().GetResult();
    }

    private static void Expect<T>(Action action) where T : Exception
    {
        try { action(); }
        catch (T) { return; }
        throw new Exception("Expected " + typeof(T).Name);
    }

    private static void Check(bool condition, string message)
    {
        if (!condition) throw new Exception(message);
    }
}
