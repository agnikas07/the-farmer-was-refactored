using System;
using System.Threading;
using TheFarmerWasRefactored.Runtime;

try
{
    RuntimeProbe.Run(Console.WriteLine);
    SteppingProbe.Run(Console.WriteLine);
    MailboxChecks.Run();
    var owner = Thread.CurrentThread.ManagedThreadId;
    using (var probe = new DispatchProbe(Console.WriteLine, value =>
    {
        if (Thread.CurrentThread.ManagedThreadId != owner)
            throw new InvalidOperationException("Host action ran on the wrong thread.");
        return value + 1;
    }))
    {
        while (!probe.IsCompleted) { probe.Tick(); Thread.Sleep(1); }
    }
    return 0;
}
catch (Exception error)
{
    Console.Error.WriteLine(error);
    return 1;
}
