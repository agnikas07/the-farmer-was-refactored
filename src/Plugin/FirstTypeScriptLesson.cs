using System;
using System.IO;
using HarmonyLib;

namespace TheFarmerWasRefactored
{
    // Installed only after the source-mode game checksum gate succeeds.
    internal static class FirstTypeScriptLesson
    {
        private static string lesson;

        internal static void Install(Harmony harmony, bool allWindows)
        {
            using (var stream = typeof(FirstTypeScriptLesson).Assembly.GetManifestResourceStream(
                "TheFarmerWasRefactored.FirstProgram.md"))
            {
                if (stream == null) throw new InvalidOperationException("Missing TypeScript lesson resource.");
                using (var reader = new StreamReader(stream))
                    lesson = reader.ReadToEnd()
                        .Replace("{{sourcePrefix}}", allWindows ? "" : "// @refactored\n")
                        .Replace("{{modeNote}}", allWindows
                            ? "Your code windows use TypeScript. No language marker is needed."
                            : "Keep `// @refactored` on the first line of each TypeScript program. It selects TypeScript for this window.");
            }
            harmony.Patch(AccessTools.Method(typeof(Localizer), "LoadDoc", new[] { typeof(string) }),
                postfix: new HarmonyMethod(typeof(FirstTypeScriptLesson), nameof(AfterLoadDoc)));
        }

        private static void AfterLoadDoc(string __0, ref string __result)
        {
            if (__0 != "docs/first_program.md" || lesson == null) return;
            // English lesson is an explicit first slice, independent of game language.
            __result = lesson;

        }

        internal static void Reset() => lesson = null;
    }
}
