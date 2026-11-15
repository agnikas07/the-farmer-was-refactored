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
        var sorted = new[] { "move", "const", "harvest", "Direction", "doAFlip", "canHarvest" };
        for (var i = 0; i < sorted.Length; i++)
        {
            var help = TypeScriptCompletions.HelpForOption(sorted, i);
            Check(sorted[i] == "const" ? help == null : help.Contains(sorted[i]),
                "Help does not follow the displayed candidate.");
        }
        Check(TypeScriptCompletions.HelpForOption(sorted, -1) == null
            && TypeScriptCompletions.HelpForOption(sorted, sorted.Length) == null
            && TypeScriptCompletions.HelpForOption(null, 0) == null,
            "Invalid completion index was not handled.");
        Check(TypeScriptCompletions.HelpForOption(new[] { "do_a_flip" }, 0) == null,
            "Native help leaked into TypeScript.");
        Console.WriteLine("TypeScript completion vocabulary and member suppression PASS");
    }
    private static void Check(bool value, string message)
    {
        if (!value) throw new Exception(message);
    }
}
