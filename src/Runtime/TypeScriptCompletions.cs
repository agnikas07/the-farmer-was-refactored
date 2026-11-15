using System.Collections.Generic;

namespace TheFarmerWasRefactored.Runtime
{
    // A beginner vocabulary, not semantic completion or a TypeScript language service.
    public static class TypeScriptCompletions
    {
        private static readonly string[] Words =
        {
            "doAFlip", "canHarvest", "harvest", "move", "Direction",
            "const", "let", "function", "return", "if", "else", "while", "do",
            "for", "of", "in", "break", "continue", "true", "false", "null",
            "undefined", "number", "string", "boolean", "void", "type",
            "interface", "enum", "new", "throw", "try", "catch", "finally",
            "switch", "case", "default", "typeof", "instanceof", "as", "Error"
        };

        public static List<string> ForPosition(string source, int wordStart)
        {
            // Native member lists are Python-specific. Do not invent TS members.
            var before = System.Math.Min(wordStart, source.Length) - 1;
            while (before >= 0 && char.IsWhiteSpace(source[before])) before--;
            if (before >= 0 && source[before] == '.') return new List<string>();
            return new List<string>(Words); // Native sorting must not mutate shared data.
        }
    }
}
