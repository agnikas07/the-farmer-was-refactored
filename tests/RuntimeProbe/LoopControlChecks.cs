using System;
using System.IO;
using System.Text.Json;
using Jint;

internal static class LoopControlChecks
{
    public static void Run(string path)
    {
        using var document = JsonDocument.Parse(File.ReadAllText(path));
        if (document.RootElement.GetArrayLength() != 6)
            throw new InvalidOperationException("Unexpected reproduction case count.");
        var failures = 0;
        foreach (var item in document.RootElement.EnumerateArray())
        {
            using var engine = new Engine(options => options.MaxStatements(1000));
            var result = engine.Evaluate("(function(){" + item.GetProperty("body").GetString() + "})()");
            var pass = result.IsNumber() && result.AsNumber() == 2;
            Console.WriteLine($"Loop control: {item.GetProperty("name").GetString()}: expected=2 actual={result} {(pass ? "PASS" : "FAIL")}");
            if (!pass) failures++;
        }
        if (failures != 0) throw new InvalidOperationException($"{failures} loop-control cases failed.");
    }
}
