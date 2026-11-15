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

        public static string HelpForOption(IReadOnlyList<string> options, int index)
        {
            if (options == null || index < 0 || index >= options.Count) return null;
            // Lookup by the actual sorted candidate, never by a fixed vocabulary index.
            return options[index] switch
            {
                "doAFlip" => "doAFlip(): void\n\nFlip once. The next command waits for the flip's native action cost.\n\nExample: doAFlip();",
                "canHarvest" => "canHarvest(): boolean\n\nReturns true when the entity under the drone is ready to harvest. This check does not collect it.\n\nExample: if (canHarvest()) { harvest(); }",
                "harvest" => "harvest(): boolean\n\nHarvest the entity under the drone. Returns false if there is nothing to harvest. An unready plant can also be harvested; check canHarvest() first.\n\nExample: if (canHarvest()) { harvest(); }",
                "move" => "move(direction: Direction): boolean\n\nMove one tile. Returns true on success or false when blocked. Directions are lowercase strings: \"north\", \"east\", \"south\", \"west\".\n\nExample: move(\"east\");",
                "Direction" => "type Direction = \"north\" | \"east\" | \"south\" | \"west\"\n\nUse a quoted lowercase direction with move(). Direction is a type, not a runtime object.\n\nExample: const direction: Direction = \"east\";",
                _ => null
            };
        }

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
