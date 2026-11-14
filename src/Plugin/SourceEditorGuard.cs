using System;
using System.Reflection;
using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using TheFarmerWasRefactored.Runtime;
using HarmonyLib;

namespace TheFarmerWasRefactored
{
    // Installed only with the checksum-gated source-input diagnostic.
    internal static class SourceEditorGuard
    {
        private static ConditionalWeakTable<CodeWindow, ErrorState> errors = new ConditionalWeakTable<CodeWindow, ErrorState>();
        private static readonly ConcurrentQueue<Action> pending = new ConcurrentQueue<Action>();
        private static readonly FieldInfo ErrorString = RequiredField("errorString");
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

        internal static Action<CompilerDiagnosticException> BeginRun(CodeWindow window, string source)
        {
            var state = errors.GetValue(window, _ => new ErrorState());
            state.Source = source;
            state.Error = null;
            var version = ++state.Version;
            CloseError.Invoke(window, null);
            // Worker only queues data; Unity objects and state are inspected by Poll.
            return error => pending.Enqueue(() =>
            {
                if (window == null || !errors.TryGetValue(window, out var current) || current != state
                    || state.Version != version || NativeActionProbe.ReadSource(window) != source) return;
                state.Error = error;
            });
        }

        internal static void Poll()
        {
            while (pending.TryDequeue(out var apply)) apply();
        }

        internal static void Reset()
        {
            foreach (var entry in errors)
                if (entry.Key != null && entry.Value.Error != null
                    && (string)ErrorString.GetValue(entry.Key) == entry.Value.Error.DisplayMessage)
                    CloseError.Invoke(entry.Key, null);
            errors = new ConditionalWeakTable<CodeWindow, ErrorState>();
            while (pending.TryDequeue(out _)) { }
        }

        private static void BeforeUpdate(CodeWindow __instance)
        {
            var source = NativeActionProbe.ReadSource(__instance);
            errors.TryGetValue(__instance, out var state);
            if (state != null && state.Source != source)
            {
                if (state.Error != null && (string)ErrorString.GetValue(__instance) == state.Error.DisplayMessage)
                    CloseError.Invoke(__instance, null);
                state.Version++;
                state.Source = source;
                state.Error = null;
            }
            if (!IsTypeScript(source)) return;
            if (state?.Error == null) CloseError.Invoke(__instance, null);
            else
            {
                var span = state.Error.Span(source);
                __instance.SetErrorMessage(state.Error.DisplayMessage, span.Start, span.End);
            }
        }

        private sealed class ErrorState
        {
            internal string Source;
            internal int Version;
            internal CompilerDiagnosticException Error;
        }

        private static FieldInfo RequiredField(string name) => AccessTools.Field(typeof(CodeWindow), name)
            ?? throw new MissingFieldException("CodeWindow", name);
    }
}
