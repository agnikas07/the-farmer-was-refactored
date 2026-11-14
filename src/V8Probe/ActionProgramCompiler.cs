using System;
using System.IO;
using Microsoft.ClearScript.V8;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace TheFarmerWasRefactored.V8Probe
{
    internal static class ActionProgramCompiler
    {
        internal const string Source = "const count: number = 2; for (let i = 0; i < count; i++) doAFlip();";

        internal static string Compile(string bundlePath)
        {
            var javascript = Compile(bundlePath, Source);
            JintOutputChecks.Run(javascript);
            return javascript;
        }

        internal static string Compile(string bundlePath, string source, Action<string> mark = null)
        {
            if (source == null || source.Length > 32_768)
                throw new ArgumentException("TypeScript source must be at most 32768 characters.");
            var bundle = JObject.Parse(File.ReadAllText(bundlePath));
            mark?.Invoke("bundle_loaded");
            string javascript;
            using (var compiler = new V8ScriptEngine())
            {
                compiler.Execute((string)bundle["compiler"]);
                compiler.Execute((string)bundle["adapter"]);
                compiler.Execute("var libraries = new Map(" + bundle["libraries"].ToString(Formatting.None) + ");");
                compiler.Execute("var apiText = " + JsonConvert.SerializeObject((string)bundle["api"]) + ";");
                mark?.Invoke("compiler_loaded");
                var json = (string)compiler.Evaluate("JSON.stringify(compile(" +
                    JsonConvert.SerializeObject(source) + ", libraries, apiText))");
                mark?.Invoke("emit_finished");
                var result = JObject.Parse(json);
                if ((bool?)result["ok"] != true || result["javascript"]?.Type != JTokenType.String)
                    throw new InvalidOperationException("TypeScript compilation failed: " + json);
                javascript = (string)result["javascript"];
            }
            mark?.Invoke("compiler_disposed");
            // Never execute user source as a compilation preflight.
            return javascript;
        }
    }
}
