using System;
using Jint;
using TheFarmerWasRefactored.V8Probe;

internal static class SourceInputChecks
{
    internal static void Run(string bundlePath)
    {
        var cases = new (string Source, int Calls)[]
        {
            ("// @refactored\ndoAFlip();", 1),
            ("// @refactored\nconst count: number = 3; for(let i=0;i<count;i++) doAFlip();", 3),
            ("// @refactored\nconst count: number = 0;", 0),
            ("// @refactored\r\ndoAFlip();", 1)
        };
        foreach (var item in cases)
        {
            var emitted = ActionProgramCompiler.Compile(bundlePath, item.Source);
            var calls = 0;
            using var player = new Engine(options => options.MaxStatements(100).LimitMemory(8_000_000));
            player.SetValue("doAFlip", new Action(() => calls++));
            player.Execute(emitted);
            if (calls != item.Calls) throw new InvalidOperationException("Source call count mismatch.");
        }
        try
        {
            ActionProgramCompiler.Compile(bundlePath, "// @refactored\ndoAFlip();\nconst count: number = 'two';");
            throw new InvalidOperationException("Invalid source was accepted.");
        }
        catch (InvalidOperationException error) when (error.Message.Contains("2322") && error.Message.Contains("\"line\":3")) { }
        // Compilation must not execute player code; runtime exceptions are not compile errors.
        ActionProgramCompiler.Compile(bundlePath, "// @refactored\nthrow new Error('runtime only');");
        try
        {
            ActionProgramCompiler.Compile(bundlePath, new string(' ', 32_769));
            throw new InvalidOperationException("Oversized source was accepted.");
        }
        catch (ArgumentException) { }
        // Fresh compiler after rejection still accepts valid source.
        ActionProgramCompiler.Compile(bundlePath, "// @refactored\ndoAFlip();");
        Console.WriteLine("Source input: actual counts, CRLF, semantic rejection, no preflight execution, size bound, recovery PASS");
    }
}
