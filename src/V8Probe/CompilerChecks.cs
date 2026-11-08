using System;
using System.Diagnostics;
using System.IO;
using Microsoft.ClearScript.V8;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace TheFarmerWasRefactored.V8Probe
{
    // Shared with the desktop probe. Inputs are trusted development fixtures only.
    internal static class CompilerChecks
    {
        internal static void Run(string bundlePath, Action<string> log, Func<bool> stopped)
        {
            var bundle = JObject.Parse(File.ReadAllText(bundlePath));
            var watch = Stopwatch.StartNew();
            using (var compiler = new V8ScriptEngine())
            {
                compiler.Execute((string)bundle["compiler"]);
                compiler.Execute((string)bundle["adapter"]);
                compiler.Execute("var libraries = new Map(" + bundle["libraries"].ToString(Formatting.None) + ");");
                compiler.Execute("var apiText = " + JsonConvert.SerializeObject((string)bundle["api"]) + ";");
                Check(Convert.ToInt32(compiler.Evaluate(
                    "ts.createSourceFile('/lib.es2020.d.ts', libraries.get('/lib.es2020.d.ts'), ts.ScriptTarget.ES2020, true).libReferenceDirectives.length"))
                    == (int)bundle["expectedReferences"], "Library references differ.");
                log($"compiler load and library references PASS in {watch.ElapsedMilliseconds} ms");
                var sources = new[]
                {
                    "const count: number = 2; for (let i = 0; i < count; i++) doAFlip();",
                    "doAFlip();\nconst count: number = 'two';",
                    "do_a_flip();",
                    "export const count = 2;",
                    "doAFlip(); doAFlip();"
                };
                var names = new[] { "typed loop", "semantic rejection", "snake_case rejection", "module rejection", "recovery after errors" };
                for (var i = 0; i < sources.Length; i++)
                {
                    if (stopped()) return;
                    watch.Restart();
                    var json = (string)compiler.Evaluate("JSON.stringify(compile(" +
                        JsonConvert.SerializeObject(sources[i]) + ", libraries, apiText))");
                    var result = JObject.Parse(json);
                    var valid = i == 0 || i == 4;
                    Check((bool)result["ok"] == valid, names[i] + ": " + json);
                    if (valid)
                    {
                        var map = JObject.Parse((string)result["sourceMap"]);
                        Check((string)map["sourcesContent"][0] == sources[i], "Source map lost original source.");
                        // Stub only: emitted code must not access compiler internals or farm state.
                        using (var player = new V8ScriptEngine())
                        {
                            player.Execute("var flipCount = 0; function doAFlip() { flipCount++; }");
                            player.Execute((string)result["javascript"]);
                            Check(Convert.ToInt32(player.Evaluate("flipCount")) == 2, "Expected two stub calls.");
                        }
                    }
                    else
                    {
                        Check(result["javascript"] == null && result["sourceMap"] == null, "Rejected input emitted output.");
                        var diagnostics = (JArray)result["diagnostics"];
                        Check(diagnostics.Count > 0, "Missing diagnostics.");
                        if (i == 1)
                        {
                            var found = false;
                            foreach (var error in diagnostics)
                                if ((int)error["code"] == 2322 && (int?)error["line"] == 2) found = true;
                            Check(found, "Expected TS2322 on original source line 2.");
                        }
                    }
                    log($"{names[i]} PASS in {watch.ElapsedMilliseconds} ms");
                }
            }
            if (!stopped()) log("ALL PASS");
        }

        private static void Check(bool condition, string message)
        {
            if (!condition) throw new InvalidOperationException(message);
        }
    }
}
