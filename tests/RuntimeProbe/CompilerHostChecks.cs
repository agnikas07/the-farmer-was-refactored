using System;
using System.Diagnostics;
using System.IO;
using System.Text.Json;
using Jint;

internal static class CompilerHostChecks
{
    public static void Run(string path)
    {
        using var bundle = JsonDocument.Parse(File.ReadAllText(path));
        var data = bundle.RootElement;
        var watch = Stopwatch.StartNew();
        using var compiler = new Engine(options => options
            .TimeoutInterval(TimeSpan.FromSeconds(30)).MaxStatements(50_000_000)
            .LimitMemory(512_000_000).LimitRecursion(512));
        compiler.Execute(data.GetProperty("compiler").GetString(), "typescript.js");
        compiler.Execute(data.GetProperty("adapter").GetString(), "compile.js");
        compiler.SetValue("libraryJson", data.GetProperty("libraries").GetRawText());
        compiler.SetValue("apiText", data.GetProperty("api").GetString());
        compiler.Execute("var libraries = new Map(JSON.parse(libraryJson));");
        Console.WriteLine($"Compiler host: loaded TypeScript in {watch.ElapsedMilliseconds} ms");
        var references = (int)compiler.Evaluate(
            "ts.createSourceFile('/lib.es2020.d.ts', libraries.get('/lib.es2020.d.ts'), ts.ScriptTarget.ES2020, true).libReferenceDirectives.length").AsNumber();
        var expected = data.GetProperty("expectedReferences").GetInt32();
        Console.WriteLine($"Compiler host: library references Node={expected}, Jint={references}");
        if (references != expected)
            throw new InvalidOperationException("Compiler host incompatible: standard-library reference discovery differs.");
        foreach (var valid in new[] { true, false })
        {
            watch.Restart();
            compiler.SetValue("sourceText", valid
                ? "const count: number = 2; for (let i = 0; i < count; i++) doAFlip();"
                : "doAFlip(); const count: number = 'two';");
            var json = compiler.Evaluate("JSON.stringify(compile(sourceText, libraries, apiText))").AsString();
            using var result = JsonDocument.Parse(json);
            var root = result.RootElement;
            if (root.GetProperty("ok").GetBoolean() != valid)
                throw new InvalidOperationException(json);
            if (valid)
            {
                var flips = 0;
                using var runtime = new Engine(options => options.MaxStatements(1000));
                runtime.SetValue("doAFlip", new Action(() => flips++));
                runtime.Execute(root.GetProperty("javascript").GetString());
                if (flips != 2) throw new InvalidOperationException("Generated action count mismatch.");
                if (!root.TryGetProperty("sourceMap", out _)) throw new InvalidOperationException("Missing map.");
            }
            else if (root.TryGetProperty("javascript", out _) || root.GetProperty("diagnostics").GetArrayLength() == 0)
                throw new InvalidOperationException("Invalid source emitted or lost diagnostics.");
            Console.WriteLine($"Compiler host: {(valid ? "valid input" : "semantic rejection")} PASS in {watch.ElapsedMilliseconds} ms");
        }
        Console.WriteLine("Compiler host: managed Jint compilation ALL PASS (desktop only)");
    }
}
