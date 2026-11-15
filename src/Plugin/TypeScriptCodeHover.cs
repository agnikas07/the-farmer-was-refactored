using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using HarmonyLib;
using TheFarmerWasRefactored.Runtime;

namespace TheFarmerWasRefactored
{
    internal static class TypeScriptCodeHover
    {
        internal static void Install(Harmony harmony)
        {
            harmony.Patch(AccessTools.Method(typeof(CodeWindow), "GetTooltipInfo"),
                transpiler: new HarmonyMethod(typeof(TypeScriptCodeHover), nameof(ReplaceLookups)));
        }

        private static IEnumerable<CodeInstruction> ReplaceLookups(IEnumerable<CodeInstruction> instructions)
        {
            var code = instructions.ToList();
            var wordLookup = AccessTools.Method(typeof(TooltipUtils), "GetWordTooltip");
            var valueLookup = AccessTools.Method(typeof(MainSim), "EvaluateName");
            var wordCalls = code.Count(c => c.Calls(wordLookup));
            var valueCalls = code.Count(c => c.Calls(valueLookup));
            if (wordCalls != 1 || valueCalls != 1)
                throw new NotSupportedException("Code hover lookup shape changed.");
            for (var i = 0; i < code.Count; i++)
            {
                if (code[i].Calls(wordLookup))
                {
                    code[i].opcode = OpCodes.Call;
                    code[i].operand = AccessTools.Method(typeof(TypeScriptCodeHover), nameof(WordHelp));
                }
                else if (code[i].Calls(valueLookup))
                {
                    // Append the owning window to the existing instance/word/out-value arguments.
                    // Keep labels on the original instruction so branches still load that window.
                    code[i].opcode = OpCodes.Ldarg_0;
                    code[i].operand = null;
                    code.Insert(++i, new CodeInstruction(OpCodes.Call,
                        AccessTools.Method(typeof(TypeScriptCodeHover), nameof(EvaluateNativeName))));
                }
            }
            return code;
        }

        private static TooltipInfo WordHelp(string word, CodeWindow window)
        {
            if (!SourceEditorGuard.IsTypeScript(NativeActionProbe.ReadSource(window)))
                return TooltipUtils.GetWordTooltip(word, window);
            var text = TypeScriptCompletions.HelpForWord(word);
            return text == null ? null : new TooltipInfo(text);
        }

        private static bool EvaluateNativeName(MainSim simulation, string word, out IPyObject value, CodeWindow window)
        {
            if (!SourceEditorGuard.IsTypeScript(NativeActionProbe.ReadSource(window)))
                return simulation.EvaluateName(word, out value);
            // Python variable inspection must not overwrite TypeScript help.
            value = null;
            return false;
        }
    }
}
