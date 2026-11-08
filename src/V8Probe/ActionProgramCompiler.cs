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
            var bundle = JObject.Parse(File.ReadAllText(bundlePath));
            string javascript;
            using (var compiler = new V8ScriptEngine())
            {
                compiler.Execute((string)bundle["compiler"]);
                compiler.Execute((string)bundle["adapter"]);
                compiler.Execute("var libraries = new Map(" + bundle["libraries"].ToString(Formatting.None) + ");");
                compiler.Execute("var apiText = " + JsonConvert.SerializeObject((string)bundle["api"]) + ";");
                var json = (string)compiler.Evaluate("JSON.stringify(compile(" +
                    JsonConvert.SerializeObject(Source) + ", libraries, apiText))");
                var result = JObject.Parse(json);
                if ((bool?)result["ok"] != true || result["javascript"]?.Type != JTokenType.String)
                    throw new InvalidOperationException("Fixed TypeScript program failed compilation: " + json);
                javascript = (string)result["javascript"];
            }
            // Prove exactly two stub calls and budget enforcement before arming native actions.
            JintOutputChecks.Run(javascript);
            return javascript;
        }
    }
}
