using System;
using System.Threading;
using TheFarmerWasRefactored.Runtime;

try
{
    if (args.Length == 2 && args[0] == "--compiled-typescript")
    {
        CompiledTypeScriptChecks.Run(args[1]);
        return 0;
    }
    if (args.Length != 0) throw new ArgumentException("Unknown test-runner arguments.");
    RuntimeProbe.Run(Console.WriteLine);
    SteppingProbe.Run(Console.WriteLine);
    MailboxChecks.Run();
    ActionMailboxChecks.Run();
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
