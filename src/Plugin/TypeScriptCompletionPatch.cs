using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using TheFarmerWasRefactored.Runtime;

namespace TheFarmerWasRefactored
{
    internal static class TypeScriptCompletionPatch
    {
        private static readonly FieldInfo Owner = AccessTools.Field(typeof(CodeCompleter), "func")
            ?? throw new System.MissingFieldException("CodeCompleter", "func");

        internal static void Install(Harmony harmony)
        {
            _ = Owner;
            harmony.Patch(AccessTools.Method(typeof(CodeCompleter), "Open"),
                prefix: new HarmonyMethod(typeof(TypeScriptCompletionPatch), nameof(BeforeOpen)));
            harmony.Patch(AccessTools.Method(typeof(CodeCompleter), "GetCompletionTooltip"),
                prefix: new HarmonyMethod(typeof(TypeScriptCompletionPatch), nameof(BeforeTooltip)));
        }

        private static bool BeforeOpen(CodeCompleter __instance, ref List<string> __1, int __3, CodeWindow __4)
        {
            if (__4 == null) return true;
            var source = NativeActionProbe.ReadSource(__4);
            if (!SourceEditorGuard.IsTypeScript(source)) return true;
            __1 = TypeScriptCompletions.ForPosition(source, __3);
            if (__1.Count != 0) return true;
            if (__instance.IsOpen) __instance.Close();
            return false;
        }

        private static bool BeforeTooltip(CodeCompleter __instance, ref TooltipInfo __result)
        {
            var window = (CodeWindow)Owner.GetValue(__instance);
            if (window == null || !SourceEditorGuard.IsTypeScript(NativeActionProbe.ReadSource(window)))
                return true;
            // Native tooltip lookup describes Python APIs, even for shared names like harvest.
            __result = default;
            return false;
        }
    }
}
