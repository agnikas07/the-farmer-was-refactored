using System;
using System.Text.RegularExpressions;
using TheFarmerWasRefactored.Runtime;

internal static class EditorStyleChecks
{
    private static readonly TypeScriptEditorStyle.ThemePalette Palette = new TypeScriptEditorStyle.ThemePalette(
        comment: "#COMMENT",
        @string: "#STRING",
        keyword: "#KEYWORD",
        function: "#FUNCTION",
        builtin: "#BUILTIN",
        number: "#NUMBER",
        searchMatch: "#SEARCH",
        searchMatchCurrent: "#CURRENT");

    internal static void Run()
    {
        CheckComments();
        CheckKeywordsAndPythonExclusion();
        CheckBuiltinsAndTypes();
        CheckFunctionsVsControlKeywords();
        CheckStringsAndNumbers();
        CheckSearchAndImeComposition();
        CheckNewLineIndentation();
        CheckAutoCloseBraces();
        Console.WriteLine("TypeScript editor coloring and indentation PASS");
    }

    private static void CheckComments()
    {
        var source = "// @refactored\n/* block */\n# python";
        var colored = Color(source);
        Check(HasColoredToken(colored, "#COMMENT", "// @refactored"), "Line comment missing comment color.");
        Check(HasColoredToken(colored, "#COMMENT", "/* block */"), "Block comment missing comment color.");
        Check(!HasColoredToken(colored, "#COMMENT", "# python"), "Python # comment should not be colored.");
        Check(StripTags(colored) == source, "Comment coloring altered source text.");
    }

    private static void CheckKeywordsAndPythonExclusion()
    {
        var tsKeywords = "const let function while if else return";
        var coloredTs = Color(tsKeywords);
        foreach (var word in tsKeywords.Split(' '))
            Check(HasColoredToken(coloredTs, "#KEYWORD", word), "Missing TypeScript keyword color for " + word);

        var pythonWords = "def elif pass and or not None True False";
        var coloredPython = Color(pythonWords);
        foreach (var word in pythonWords.Split(' '))
        {
            Check(!HasColoredToken(coloredPython, "#KEYWORD", word), "Python keyword leaked: " + word);
            Check(!HasColoredToken(coloredPython, "#BUILTIN", word), "Python literal leaked: " + word);
        }
    }

    private static void CheckBuiltinsAndTypes()
    {
        var builtins = "true false null undefined number string boolean Direction Error";
        var colored = Color(builtins);
        foreach (var word in builtins.Split(' '))
            Check(HasColoredToken(colored, "#BUILTIN", word), "Missing TypeScript builtin/type color for " + word);
    }

    private static void CheckFunctionsVsControlKeywords()
    {
        var calls = "doAFlip(); canHarvest(); harvest(); move(\"east\");";
        var coloredCalls = Color(calls);
        foreach (var fn in new[] { "doAFlip", "canHarvest", "harvest", "move" })
            Check(HasColoredToken(coloredCalls, "#FUNCTION", fn), "Missing function color for " + fn);

        var control = "if (canHarvest()) { while (true) { } }";
        var coloredControl = Color(control);
        Check(HasColoredToken(coloredControl, "#KEYWORD", "if"), "Control keyword 'if' lost keyword color.");
        Check(HasColoredToken(coloredControl, "#KEYWORD", "while"), "Control keyword 'while' lost keyword color.");
        Check(!HasColoredToken(coloredControl, "#FUNCTION", "if"), "'if' misclassified as function.");
        Check(!HasColoredToken(coloredControl, "#FUNCTION", "while"), "'while' misclassified as function.");
        Check(HasColoredToken(coloredControl, "#FUNCTION", "canHarvest"), "'canHarvest' missing function color.");
    }

    private static void CheckStringsAndNumbers()
    {
        var source = "\"east\" 'north' `patrol` 42 0.5";
        var colored = Color(source);
        foreach (var lit in new[] { "\"east\"", "'north'", "`patrol`" })
            Check(HasColoredToken(colored, "#STRING", lit), "Missing string color for " + lit);
        foreach (var num in new[] { "42", "0.5" })
            Check(HasColoredToken(colored, "#NUMBER", num), "Missing number color for " + num);
        Check(StripTags(colored) == source, "String/number coloring altered source text.");
    }

    private static void CheckSearchAndImeComposition()
    {
        var source = "move(\"east\"); move(\"west\");";
        var secondMove = source.LastIndexOf("move", StringComparison.Ordinal);
        var coloredSearch = TypeScriptEditorStyle.ColorCode(source, Palette, "move", secondMove);
        Check(coloredSearch.Contains("<mark=#SEARCH from_search><noparse>move</noparse></mark>"),
            "Missing non-current search highlight.");
        Check(coloredSearch.Contains("<mark=#CURRENT from_search><noparse>move</noparse></mark>"),
            "Missing current search highlight.");
        Check(StripTags(coloredSearch) == source, "Search tags altered source text.");

        var imeSource = "const direction = \"east\";";
        var searchIndexBeforeIme = imeSource.IndexOf("east", StringComparison.Ordinal) - 3;
        var coloredIme = TypeScriptEditorStyle.ColorCode(
            imeSource, Palette, "east", searchIndexBeforeIme, (6, 3));
        Check(coloredIme.Contains("<u><noparse>dir</noparse></u>"), "Missing IME composition underline.");
        Check(coloredIme.Contains("<mark=#CURRENT from_search><noparse>east</noparse></mark>"),
            "Search index did not adjust for IME composition range.");
        Check(StripTags(coloredIme) == imeSource, "IME tags altered source text.");
    }

    private static void CheckNewLineIndentation()
    {
        Check(TypeScriptEditorStyle.TryIndentNewLine("while (true) {\n", "\n", 15, out var updated, out var pos)
            && updated == "while (true) {\n\t" && pos == 16,
            "Failed top-level '{' tab indent.");

        Check(TypeScriptEditorStyle.TryIndentNewLine("\tif (canHarvest()) {\n", "\n", 21, out updated, out pos)
            && updated == "\tif (canHarvest()) {\n\t\t" && pos == 23,
            "Failed nested '{' tab indent.");

        Check(TypeScriptEditorStyle.TryIndentNewLine("    if (canHarvest()) {\n", "\n", 24, out updated, out pos)
            && updated == "    if (canHarvest()) {\n        " && pos == 32,
            "Failed nested '{' 4-space indent.");

        Check(TypeScriptEditorStyle.TryIndentNewLine("\tdoAFlip();\n", "\n", 12, out updated, out pos)
            && updated == "\tdoAFlip();\n\t" && pos == 13,
            "Failed preserving existing tab indent.");

        Check(!TypeScriptEditorStyle.TryIndentNewLine("const direction:\n", "\n", 17, out _, out _),
            "Python ':' at top level should not indent in TypeScript.");

        Check(TypeScriptEditorStyle.TryIndentNewLine("\tconst direction:\n", "\n", 18, out updated, out pos)
            && updated == "\tconst direction:\n\t" && pos == 19,
            "Python ':' added extra indent in TypeScript.");

        Check(!TypeScriptEditorStyle.TryIndentNewLine("while (true) {", "{", 14, out _, out _),
            "Non-newline insertion should not trigger indentation.");

        Check(TypeScriptEditorStyle.TryIndentNewLine("while (true) {\n}", "\n", 15, out updated, out pos)
            && updated == "while (true) {\n\t\n}" && pos == 16,
            "Failed top-level '{|}' Enter expansion.");

        Check(TypeScriptEditorStyle.TryIndentNewLine("\tif (canHarvest()) {\n}", "\n", 21, out updated, out pos)
            && updated == "\tif (canHarvest()) {\n\t\t\n\t}" && pos == 23,
            "Failed tab-indented '{|}' Enter expansion.");

        Check(TypeScriptEditorStyle.TryIndentNewLine("    if (canHarvest()) {\n}", "\n", 24, out updated, out pos)
            && updated == "    if (canHarvest()) {\n        \n    }" && pos == 32,
            "Failed space-indented '{|}' Enter expansion.");

        Check(TypeScriptEditorStyle.TryIndentNewLine("move(\n)", "\n", 6, out updated, out pos)
            && updated == "move(\n\t\n)" && pos == 7,
            "Failed top-level '(|)' Enter expansion.");

        Check(TypeScriptEditorStyle.TryIndentNewLine("const list = [\n]", "\n", 15, out updated, out pos)
            && updated == "const list = [\n\t\n]" && pos == 16,
            "Failed top-level '[|]' Enter expansion.");
    }

    private static void CheckAutoCloseBraces()
    {
        Check(TypeScriptEditorStyle.TryAutoCloseBrace("if (canHarvest()) {", "{", 19, out var updated, out var pos)
            && updated == "if (canHarvest()) {}" && pos == 19,
            "Failed auto-closing '{' at end of line.");

        Check(TypeScriptEditorStyle.TryAutoCloseBrace("if (canHarvest()) {\ndoAFlip();", "{", 19, out updated, out pos)
            && updated == "if (canHarvest()) {}\ndoAFlip();" && pos == 19,
            "Failed auto-closing '{' before newline.");

        Check(!TypeScriptEditorStyle.TryAutoCloseBrace("if (canHarvest()) {\n}", "{", 19, out _, out _),
            "Should not auto-close '{' when braces are already balanced.");

        Check(TypeScriptEditorStyle.TryAutoCloseBrace("while (true) {}}", "}", 15, out updated, out pos)
            && updated == "while (true) {}" && pos == 15,
            "Failed overtyping excess auto-closed '}'.");

        Check(!TypeScriptEditorStyle.TryAutoCloseBrace("if (a) { if (b) {}}", "}", 18, out _, out _),
            "Should not eat '}' when closing nested balanced braces.");

        Check(TypeScriptEditorStyle.TryAutoCloseBrace("doAFlip(", "(", 8, out updated, out pos)
            && updated == "doAFlip()" && pos == 8,
            "Failed auto-closing '(' at end of line.");

        Check(TypeScriptEditorStyle.TryAutoCloseBrace("if (canHarvest()", "(", 15, out updated, out pos)
            && updated == "if (canHarvest())" && pos == 15,
            "Failed auto-closing '(' inside outer ')'.");

        Check(TypeScriptEditorStyle.TryAutoCloseBrace("doAFlip())", ")", 9, out updated, out pos)
            && updated == "doAFlip()" && pos == 9,
            "Failed overtyping excess auto-closed ')'.");

        Check(!TypeScriptEditorStyle.TryAutoCloseBrace("if (canHarvest())", ")", 16, out _, out _),
            "Should not eat ')' when parentheses are balanced.");

        Check(TypeScriptEditorStyle.TryAutoCloseBrace("const list = [", "[", 14, out updated, out pos)
            && updated == "const list = []" && pos == 14,
            "Failed auto-closing '[' at end of line.");

        Check(TypeScriptEditorStyle.TryAutoCloseBrace("const list = []]", "]", 15, out updated, out pos)
            && updated == "const list = []" && pos == 15,
            "Failed overtyping excess auto-closed ']'.");

        Check(!TypeScriptEditorStyle.TryAutoCloseBrace("const a = [[1]]", "]", 14, out _, out _),
            "Should not eat ']' when square brackets are balanced.");

        Check(TypeScriptEditorStyle.TryAutoCloseBrace("move(\")", "\"", 6, out updated, out pos)
            && updated == "move(\"\")" && pos == 6,
            "Failed auto-closing '\"' inside parentheses.");

        Check(TypeScriptEditorStyle.TryAutoCloseBrace("move(\"east\"\")", "\"", 11, out updated, out pos)
            && updated == "move(\"east\")" && pos == 11,
            "Failed overtyping closing '\"'.");

        Check(TypeScriptEditorStyle.TryAutoCloseBrace("move(')", "'", 6, out updated, out pos)
            && updated == "move('')" && pos == 6,
            "Failed auto-closing '\\'' inside parentheses.");

        Check(TypeScriptEditorStyle.TryAutoCloseBrace("move('east'')", "'", 11, out updated, out pos)
            && updated == "move('east')" && pos == 11,
            "Failed overtyping closing '\\''.");

        Check(TypeScriptEditorStyle.TryAutoCloseBrace("const msg = `", "`", 13, out updated, out pos)
            && updated == "const msg = ``" && pos == 13,
            "Failed auto-closing '`'.");

        Check(TypeScriptEditorStyle.TryAutoCloseBrace("const msg = `hi``", "`", 16, out updated, out pos)
            && updated == "const msg = `hi`" && pos == 16,
            "Failed overtyping closing '`'.");

        Check(!TypeScriptEditorStyle.TryAutoCloseBrace("// don'", "'", 7, out _, out _),
            "Should not auto-close apostrophe after a word character.");
    }

    private static string Color(string code) => TypeScriptEditorStyle.ColorCode(code, Palette);

    private static bool HasColoredToken(string colored, string color, string token) =>
        colored.Contains("<color=" + color + "><noparse>" + token + "</noparse></color>");

    private static string StripTags(string markup) =>
        Regex.Replace(markup, "</?(?:noparse|color(?:=[^>]+)?|mark(?:=[^>]+)?|u)>", "");

    private static void Check(bool value, string message)
    {
        if (!value) throw new Exception(message);
    }
}
