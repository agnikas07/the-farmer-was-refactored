using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using TheFarmerWasRefactored.Runtime;

namespace TheFarmerWasRefactored
{
    internal static class TypeScriptEditorStylePatch
    {
        private static readonly PropertyInfo CodeInputProperty =
            AccessTools.Property(typeof(CodeWindow), "CodeInput")
            ?? throw new MissingMemberException(nameof(CodeWindow), "CodeInput");

        private static readonly PropertyInfo TextProperty =
            AccessTools.Property(CodeInputProperty.PropertyType, "text")
            ?? throw new MissingMemberException(CodeInputProperty.PropertyType.Name, "text");

        private static readonly PropertyInfo StringPositionProperty =
            AccessTools.Property(CodeInputProperty.PropertyType, "stringPosition")
            ?? throw new MissingMemberException(CodeInputProperty.PropertyType.Name, "stringPosition");

        private static readonly MethodInfo SyntaxColor2Method =
            AccessTools.Method(typeof(CodeUtilities), "SyntaxColor2")
            ?? throw new MissingMethodException(nameof(CodeUtilities), "SyntaxColor2");

        internal static void Install(Harmony harmony)
        {
            _ = CodeInputProperty;
            _ = TextProperty;
            _ = StringPositionProperty;
            _ = SyntaxColor2Method;

            var updateSearch = AccessTools.Method(typeof(CodeWindow), "UpdateSearch", new[] { typeof(string), typeof(int) })
                ?? throw new MissingMethodException(nameof(CodeWindow), "UpdateSearch");
            var onThemeChanged = AccessTools.Method(typeof(CodeWindow), "OnThemeChanged", new[] { typeof(ColorTheme) })
                ?? throw new MissingMethodException(nameof(CodeWindow), "OnThemeChanged");
            var insertNewLine = AccessTools.Method(typeof(CodeWindow), "InsertNewLineIndentation", new[] { typeof(string) })
                ?? throw new MissingMethodException(nameof(CodeWindow), "InsertNewLineIndentation");

            var coloringTranspiler = new HarmonyMethod(typeof(TypeScriptEditorStylePatch), nameof(ReplaceSyntaxColoring));
            harmony.Patch(updateSearch, transpiler: coloringTranspiler);
            harmony.Patch(onThemeChanged, transpiler: coloringTranspiler);
            harmony.Patch(insertNewLine,
                prefix: new HarmonyMethod(typeof(TypeScriptEditorStylePatch), nameof(BeforeInsertNewLineIndentation)));
        }

        private static IEnumerable<CodeInstruction> ReplaceSyntaxColoring(IEnumerable<CodeInstruction> instructions)
        {
            var code = instructions.ToList();
            if (code.Count(c => c.Calls(SyntaxColor2Method)) != 1)
                throw new NotSupportedException("Code syntax coloring shape changed.");
            for (var i = 0; i < code.Count; i++)
            {
                if (!code[i].Calls(SyntaxColor2Method)) continue;
                // Keep labels on the original call instruction while appending the owning CodeWindow.
                code[i].opcode = OpCodes.Ldarg_0;
                code[i].operand = null;
                code.Insert(i + 1, new CodeInstruction(OpCodes.Call,
                    AccessTools.Method(typeof(TypeScriptEditorStylePatch), nameof(SyntaxColor))));
                break;
            }
            return code;
        }

        private static string SyntaxColor(
            string code,
            ColorTheme colorTheme,
            string searchWord,
            int searchIndex,
            (int index, int length) imeCompositionRange,
            CodeWindow window)
        {
            if (window == null || !SourceEditorGuard.IsTypeScript(NativeActionProbe.ReadSource(window)))
                return CodeUtilities.SyntaxColor2(code, colorTheme, searchWord, searchIndex, imeCompositionRange);
            if (colorTheme == null) colorTheme = ThemeManager.Inst.Theme;
            var palette = new TypeScriptEditorStyle.ThemePalette(
                colorTheme.code.comment,
                colorTheme.code.@string,
                colorTheme.code.keyword,
                colorTheme.code.function,
                colorTheme.code.builtin,
                colorTheme.code.number,
                colorTheme.ui.search_match,
                colorTheme.ui.search_match_current);
            return TypeScriptEditorStyle.ColorCode(code, palette, searchWord, searchIndex, imeCompositionRange);
        }

        private static bool BeforeInsertNewLineIndentation(CodeWindow __instance, string __0)
        {
            var input = CodeInputProperty.GetValue(__instance);
            var source = (string)TextProperty.GetValue(input);
            if (!SourceEditorGuard.IsTypeScript(source)) return true;
            var pos = (int)StringPositionProperty.GetValue(input);
            if (TypeScriptEditorStyle.TryAutoCloseBrace(source, __0, pos, out var updatedSource, out var updatedPos)
                || TypeScriptEditorStyle.TryIndentNewLine(source, __0, pos, out updatedSource, out updatedPos))
            {
                TextProperty.SetValue(input, updatedSource);
                // Re-read position because UpdateTokens may normalize tabs to spaces during SetText.
                var currentPos = (int)StringPositionProperty.GetValue(input);
                StringPositionProperty.SetValue(input, currentPos + (updatedPos - pos));
            }
            return false;
        }
    }
}
