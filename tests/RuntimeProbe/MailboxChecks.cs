using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using TheFarmerWasRefactored.Runtime;

internal static class MailboxChecks
{
    public static void Run()
    {
        var calls = 0;
        var mailbox = new MainThreadMailbox(value => { calls++; return value; });
        Expect<InvalidOperationException>(() => mailbox.Invoke(1, CancellationToken.None));
        Task.Run(() => Expect<InvalidOperationException>(mailbox.Pump)).GetAwaiter().GetResult();
        using (var stop = new CancellationTokenSource(TimeSpan.FromSeconds(2)))
        {
            var waiting = Task.Run(() => mailbox.Invoke(1, stop.Token));
            var deadline = Stopwatch.StartNew();
            while (!mailbox.HasPending && deadline.ElapsedMilliseconds < 1000) Thread.Sleep(1);
            if (!mailbox.HasPending) throw new TimeoutException("No queued mailbox request.");
            Task.Run(() => Expect<InvalidOperationException>(() => mailbox.Invoke(2, stop.Token)))
                .GetAwaiter().GetResult();
            stop.Cancel();
            Expect<OperationCanceledException>(() => waiting.GetAwaiter().GetResult());
            mailbox.Pump();
            if (calls != 0) throw new InvalidOperationException("Cancelled request ran.");
        }
        Console.WriteLine("Mailbox guards: owner, capacity, cancellation PASS");
    }

    private static void Expect<T>(Action action) where T : Exception
    {
        try { action(); }
        catch (T) { return; }
        throw new InvalidOperationException($"Expected {typeof(T).Name}.");
    }
}
