using System;
using System.IO;
using System.Text.Json;
using Jint;
using Jint.Runtime;

internal static class CompiledTypeScriptChecks
{
    public static void Run(string path)
    {
        using var document = JsonDocument.Parse(File.ReadAllText(path));
        var cases = document.RootElement;
        Check(cases.GetArrayLength() == 7, "Fixture count changed or incomplete.");
        foreach (var fixture in cases.EnumerateArray())
        {
            var name = fixture.GetProperty("name").GetString();
            var outcome = fixture.GetProperty("outcome").GetString();
            var ok = fixture.GetProperty("ok").GetBoolean();
            var hasCode = fixture.TryGetProperty("javascript", out var code);
            if (outcome == "rejected")
            {
                Check(!ok && !hasCode && !fixture.TryGetProperty("sourceMap", out _),
                    name + ": rejected input had executable output.");
                Check(fixture.GetProperty("diagnostics").GetArrayLength() > 0,
                    name + ": missing diagnostic.");
                Console.WriteLine($"TypeScript/Jint: {name} PASS");
                continue;
            }
            Check(ok && hasCode && code.ValueKind == JsonValueKind.String, name + ": missing code.");
            var flips = 0;
            using var engine = new Engine(options => options.MaxStatements(1000)
                .TimeoutInterval(TimeSpan.FromSeconds(2)).LimitMemory(8_000_000).LimitRecursion(64));
            engine.SetValue("doAFlip", new Action(() => flips++));
            var actual = "success";
            try { engine.Execute(code.GetString(), "main.js"); }
            catch (StatementsCountOverflowException) { actual = "budget"; }
            catch (JavaScriptException error)
            {
                actual = "throw";
                Check(error.Message.Contains("expected fixture failure"), name + ": unexpected JS error.");
            }
            Check(actual == outcome, $"{name}: expected {outcome}, got {actual}.");
            Check(flips == fixture.GetProperty("flips").GetInt32(), name + ": action count mismatch.");
            Check(engine.Evaluate("typeof do_a_flip").AsString() == "undefined", "Snake-case alias leaked.");
            Console.WriteLine($"TypeScript/Jint: {name} PASS");
        }
        Console.WriteLine("TypeScript/Jint: ALL PASS");
    }

    private static void Check(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
