using System;
using System.Diagnostics;
using System.IO;
using System.Text.Json;
using Microsoft.ClearScript.V8;

try
{
    if (args.Length != 2) throw new ArgumentException("Supply bundle and loop-case JSON paths.");
    TheFarmerWasRefactored.V8Probe.CompilerChecks.Run(args[0],
        message => Console.WriteLine("Shared game compiler checks: " + message), () => false);
    TheFarmerWasRefactored.V8Probe.CompilerChecks.Run(args[0],
        message => Console.WriteLine("V8 to Jint desktop checks: " + message), () => false,
        javascript => TheFarmerWasRefactored.V8Probe.JintOutputChecks.Run(javascript));
    _ = TheFarmerWasRefactored.V8Probe.ActionProgramCompiler.Compile(args[0]);
    Console.WriteLine("Native action compiler handoff: fixed TypeScript compilation and Jint preflight PASS");
    using var bundle = JsonDocument.Parse(File.ReadAllText(args[0]));
    using var loops = JsonDocument.Parse(File.ReadAllText(args[1]));
    var data = bundle.RootElement;
    var watch = Stopwatch.StartNew();
    using var compiler = new V8ScriptEngine();
    // Trusted local diagnostic texts only. No CLR objects/types exposed to script.
    compiler.Execute(data.GetProperty("compiler").GetString());
    compiler.Execute(data.GetProperty("adapter").GetString());
    compiler.Execute("var libraries = new Map(" + data.GetProperty("libraries").GetRawText() + ");");
    compiler.Execute("var apiText = " + JsonSerializer.Serialize(data.GetProperty("api").GetString()) + ";");
    Console.WriteLine($"V8 compiler: loaded in {watch.ElapsedMilliseconds} ms");
    Check(Convert.ToInt32(compiler.Evaluate(
        "ts.createSourceFile('/lib.es2020.d.ts', libraries.get('/lib.es2020.d.ts'), ts.ScriptTarget.ES2020, true).libReferenceDirectives.length"))
        == data.GetProperty("expectedReferences").GetInt32(), "Library reference mismatch.");
    Check(loops.RootElement.GetArrayLength() == 6, "Loop-case fixture incomplete.");
    foreach (var item in loops.RootElement.EnumerateArray())
        Check(Convert.ToInt32(compiler.Evaluate("(function(){" + item.GetProperty("body").GetString() + "})()")) == 2,
            "Loop-control mismatch: " + item.GetProperty("name").GetString());
    Console.WriteLine("V8 compiler: library references and six loop-control cases PASS");

    var cases = new (string Name, string Source, bool Valid)[]
    {
        ("typed loop", "const count: number = 2; for(let i=0;i<count;i++) doAFlip();", true),
        ("semantic rejection", "doAFlip();\nconst count: number = 'two';", false),
        ("snake_case rejection", "do_a_flip();", false),
        ("module rejection", "export const count = 2;", false),
        ("recovery after errors", "doAFlip(); doAFlip();", true),
    };
    foreach (var item in cases)
    {
        watch.Restart();
        var source = JsonSerializer.Serialize(item.Source);
        var json = (string)compiler.Evaluate("JSON.stringify(compile(" + source + ", libraries, apiText))");
        using var result = JsonDocument.Parse(json);
        var root = result.RootElement;
        Check(root.GetProperty("ok").GetBoolean() == item.Valid, item.Name + ": " + json);
        if (item.Valid)
        {
            var map = JsonDocument.Parse(root.GetProperty("sourceMap").GetString());
            using (map)
                Check(map.RootElement.GetProperty("sourcesContent")[0].GetString() == item.Source,
                    "Source map lost original text.");
            using var player = new V8ScriptEngine();
            player.Execute("var flipCount = 0; function doAFlip() { flipCount++; }");
            player.Execute(root.GetProperty("javascript").GetString());
            Check(Convert.ToInt32(player.Evaluate("flipCount")) == 2, "Generated action count mismatch.");
        }
        else
        {
            Check(!root.TryGetProperty("javascript", out _) && !root.TryGetProperty("sourceMap", out _),
                "Rejected input emitted output.");
            Check(root.GetProperty("diagnostics").GetArrayLength() > 0, "Missing diagnostics.");
            if (item.Name == "semantic rejection")
            {
                var found = false;
                foreach (var error in root.GetProperty("diagnostics").EnumerateArray())
                    if (error.GetProperty("code").GetInt32() == 2322
                        && error.GetProperty("line").GetInt32() == 2) found = true;
                Check(found, "Semantic diagnostic lost original line.");
            }
        }
        Console.WriteLine($"V8 compiler: {item.Name} PASS in {watch.ElapsedMilliseconds} ms");
    }
    Console.WriteLine("V8 compiler: ALL PASS (Linux x64 desktop only)");
    return 0;
}
catch (Exception error)
{
    Console.Error.WriteLine(error);
    return 1;
}

static void Check(bool condition, string message)
{
    if (!condition) throw new InvalidOperationException(message);
}
