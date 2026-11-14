using System;
using System.Threading;
using System.Threading.Tasks;
using Jint;
using TheFarmerWasRefactored.Runtime;

internal static class FarmBudgetChecks
{
    internal static void Run()
    {
        var budget = new FarmExecutionBudget();
        using (var engine = new Engine(options => options.Constraint(budget)))
        {
            ExpectLimit(() => engine.Execute("while (true) {}"));
            engine.Execute("let recovered = 1;");
        }
        // Many native completions, including false, renew only on the JS worker.
        var callbacks = 0;
        var owner = Thread.CurrentThread.ManagedThreadId;
        using (var mailbox = new ActionMailbox(() =>
        {
            Check(Thread.CurrentThread.ManagedThreadId != owner, "Budget reset on simulation owner.");
            Interlocked.Increment(ref callbacks);
            budget.Reset();
        }))
        {
            var worker = Task.Run(() =>
            {
                using var engine = new Engine(options => options.Constraint(budget));
                engine.SetValue("canHarvest", new Func<bool>(mailbox.CanHarvest));
                // Calls renew the budget, but later computation in the same run stays bounded.
                ExpectLimit(() => engine.Execute("for (let i=0; i<12000; i++) canHarvest(); while (true) {}"));
            });
            for (var i = 0; i < 12000; i++)
            {
                Wait(() => mailbox.HasPending);
                Check(mailbox.TryClaim(out var ticket), "Missing farm call.");
                Check(mailbox.Complete(ticket, false), "Completion rejected.");
            }
            Join(worker);
            Check(callbacks == 12000, "Completed false calls did not renew budget.");
        }
        foreach (var fail in new[] { false, true })
        using (var mailbox = new ActionMailbox(() => throw new Exception("Failed call renewed budget.")))
        {
            var worker = Task.Run(() =>
            {
                try { mailbox.CanHarvest(); }
                catch (OperationCanceledException) when (!fail) { return; }
                catch (InvalidOperationException) when (fail) { return; }
                throw new Exception("Expected failed call.");
            });
            Wait(() => mailbox.HasPending);
            if (fail)
            {
                Check(mailbox.TryClaim(out var ticket), "Missing failure ticket.");
                mailbox.Fail(ticket, new InvalidOperationException("native failure"));
            }
            else mailbox.Dispose();
            Join(worker);
        }
        // Stop remains effective even when frequent completed calls renew the budget.
        using (var stop = new CancellationTokenSource())
        using (var mailbox = new ActionMailbox(budget.Reset))
        using (stop.Token.Register(mailbox.Dispose))
        {
            var worker = Task.Run(() =>
            {
                using var engine = new Engine(options => options.Constraint(budget).CancellationToken(stop.Token));
                engine.SetValue("canHarvest", new Func<bool>(mailbox.CanHarvest));
                try { engine.Execute("while (true) canHarvest();"); }
                catch (Exception) when (stop.IsCancellationRequested) { return; }
                throw new Exception("Endless farm loop exited without Stop.");
            });
            Wait(() => mailbox.HasPending);
            Check(mailbox.TryClaim(out var ticket), "Missing stop ticket.");
            mailbox.Complete(ticket, false);
            Wait(() => mailbox.HasPending);
            stop.Cancel();
            Join(worker);
        }
        Console.WriteLine("Farm budget: continuous calls, false results, runaway limit, failure and Stop PASS");
    }
    private static void ExpectLimit(Action action)
    {
        try { action(); }
        catch (FarmComputationLimitException) { return; }
        throw new Exception("Runaway computation was not limited.");
    }
    private static void Wait(Func<bool> ready)
    {
        if (!SpinWait.SpinUntil(ready, TimeSpan.FromSeconds(5))) throw new TimeoutException("Farm call timed out.");
    }
    private static void Join(Task worker)
    {
        if (!worker.Wait(TimeSpan.FromSeconds(10))) throw new TimeoutException("Farm worker timed out.");
        worker.GetAwaiter().GetResult();
    }
    private static void Check(bool condition, string message)
    {
        if (!condition) throw new Exception(message);
    }
}
