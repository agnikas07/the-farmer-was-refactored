using System;
using System.Threading;
using System.Threading.Tasks;
using Jint;
using TheFarmerWasRefactored.Runtime;

internal static class MovementChecks
{
    internal static void Run()
    {
        foreach (var direction in new[] { "north", "east", "south", "west" })
        foreach (var result in new[] { false, true })
        using (var mailbox = new ActionMailbox())
        {
            var worker = Task.Run(() =>
            {
                using var engine = new Engine(options => options.MaxStatements(100));
                MovementApi.Bind(engine, mailbox, null);
                return engine.Evaluate($"move('{direction}')").AsBoolean();
            });
            Wait(() => mailbox.HasPending);
            Check(mailbox.TryClaim(out var ticket), "Missing move.");
            Check(ticket.Action == ActionMailbox.Command.Move && ticket.Direction == direction, "Direction changed.");
            Check(!worker.IsCompleted, "Movement returned before completion.");
            Check(mailbox.Complete(ticket, result), "Completion rejected.");
            Join(worker);
            Check(worker.Result == result, "Movement result changed.");
            Check(!mailbox.Complete(ticket, !result), "Stale completion accepted.");
        }
        foreach (var value in new[] { "'up'", "'North'", "null", "undefined", "1", "{toString(){return 'north';}}" })
        using (var mailbox = new ActionMailbox())
        {
            var worker = Task.Run(() =>
            {
                using var engine = new Engine(options => options.MaxStatements(100));
                MovementApi.Bind(engine, mailbox, null);
                try { engine.Execute($"move({value});"); }
                catch (Exception) { return; }
                throw new Exception("Invalid direction accepted.");
            });
            Join(worker);
            Check(!mailbox.HasPending, "Invalid direction reached simulation.");
        }
        foreach (var claim in new[] { false, true })
        using (var mailbox = new ActionMailbox())
        {
            var worker = Task.Run(() =>
            {
                try { mailbox.Move("north"); }
                catch (OperationCanceledException) { return; }
                throw new Exception("Cancelled move returned.");
            });
            Wait(() => mailbox.HasPending);
            ActionMailbox.Ticket ticket = null;
            if (claim) Check(mailbox.TryClaim(out ticket), "Missing cancellation ticket.");
            mailbox.Dispose();
            Check(!mailbox.TryClaim(out _), "Stopped move claimed.");
            if (ticket != null) Check(!mailbox.Complete(ticket), "Stopped move completed.");
            Join(worker);
        }
        Console.WriteLine("Movement: directions, boolean results, validation and cancellation PASS");
    }

    private static void Wait(Func<bool> ready)
    {
        if (!SpinWait.SpinUntil(ready, TimeSpan.FromSeconds(5))) throw new TimeoutException("Movement request timed out.");
    }
    private static void Join(Task worker)
    {
        if (!worker.Wait(TimeSpan.FromSeconds(5))) throw new TimeoutException("Movement worker timed out.");
        worker.GetAwaiter().GetResult();
    }
    private static void Check(bool condition, string message)
    {
        if (!condition) throw new Exception(message);
    }
}
