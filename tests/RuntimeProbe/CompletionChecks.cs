using System;
using System.Linq;
using TheFarmerWasRefactored.Runtime;

internal static class CompletionChecks
{
    internal static void Run()
    {
        var words = TypeScriptCompletions.ForPosition("do", 0);
        foreach (var name in new[] { "doAFlip", "canHarvest", "harvest", "move", "Direction", "const", "true" })
            Check(words.Contains(name), "Missing " + name);
        foreach (var name in new[] { "do_a_flip", "can_harvest", "__name__", "and", "True", "import", "random" })
            Check(!words.Contains(name), "Unexpected native/unsupported suggestion " + name);
        Check(words.Distinct().Count() == words.Count, "Duplicate suggestions.");
        words.Clear();
        Check(TypeScriptCompletions.ForPosition("", 0).Count > 0, "Shared list was mutated.");
        foreach (var source in new[] { "value.", "value. ", "value?.", "value.\r\n" })
            Check(TypeScriptCompletions.ForPosition(source, source.Length).Count == 0, "Native members leaked.");
        Check(TypeScriptCompletions.ForPosition("const n = 1;\r\ndo", 14).Contains("doAFlip"),
            "Ordinary context lost vocabulary.");
        Console.WriteLine("TypeScript completion vocabulary and member suppression PASS");
    }
    private static void Check(bool value, string message)
    {
        if (!value) throw new Exception(message);
    }
}
