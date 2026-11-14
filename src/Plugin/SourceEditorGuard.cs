using System;
using System.Reflection;
using HarmonyLib;

namespace TheFarmerWasRefactored
{
    // Installed only with the checksum-gated source-input diagnostic.
    internal static class SourceEditorGuard
    {
        private static readonly FieldInfo ParseLock = RequiredField("parseLock");
        private static readonly FieldInfo CachedProgram = RequiredField("cachedProgram");
        private static readonly FieldInfo ParseException = RequiredField("parseException");
        private static readonly FieldInfo Dirty = RequiredField("dirty");
        private static readonly MethodInfo CloseError = AccessTools.Method(typeof(CodeWindow), "CloseError")
            ?? throw new MissingMethodException("CodeWindow.CloseError");

        internal static bool IsTypeScript(string source) =>
            source.Replace("\r\n", "\n").StartsWith("// @refactored\n", StringComparison.Ordinal);

        internal static void Install(Harmony harmony)
        {
            // Resolve private fields before patching so incompatible builds fail at installation.
            _ = ParseLock; _ = CachedProgram; _ = ParseException; _ = Dirty; _ = CloseError;
            harmony.Patch(AccessTools.Method(typeof(CodeWindow), "Parse"),
                prefix: new HarmonyMethod(typeof(SourceEditorGuard), nameof(BeforeParse)));
            harmony.Patch(AccessTools.Method(typeof(CodeWindow), "Update"),
                prefix: new HarmonyMethod(typeof(SourceEditorGuard), nameof(BeforeUpdate)));
        }

        private static bool BeforeParse(CodeWindow __instance, ref Node __result)
        {
            if (!IsTypeScript(NativeActionProbe.ReadSource(__instance))) return true;
            lock (ParseLock.GetValue(__instance))
            {
                // Preserve native text/undo bookkeeping without constructing a Python syntax tree.
                __instance.UpdateTokens();
                CachedProgram.SetValue(__instance, null);
                ParseException.SetValue(__instance, null);
                __instance.parsedFunctions.Clear();
                // Removing the marker must force a fresh native parse, never reuse stale code.
                Dirty.SetValue(__instance, true);
                __result = null;
            }
            return false;
        }

        private static void BeforeUpdate(CodeWindow __instance)
        {
            if (IsTypeScript(NativeActionProbe.ReadSource(__instance)))
                // Unity-only UI cleanup; also closes a popup created before adding the marker.
                CloseError.Invoke(__instance, null);
        }

        private static FieldInfo RequiredField(string name) => AccessTools.Field(typeof(CodeWindow), name)
            ?? throw new MissingFieldException("CodeWindow", name);
    }
}
