using System;
using System.Text;
using System.Text.RegularExpressions;

namespace TheFarmerWasRefactored.Runtime
{
    public static class TypeScriptEditorStyle
    {
        public readonly struct ThemePalette
        {
            public string Comment { get; }
            public string String { get; }
            public string Keyword { get; }
            public string Function { get; }
            public string Builtin { get; }
            public string Number { get; }
            public string SearchMatch { get; }
            public string SearchMatchCurrent { get; }

            public ThemePalette(
                string comment,
                string @string,
                string keyword,
                string function,
                string builtin,
                string number,
                string searchMatch,
                string searchMatchCurrent)
            {
                Comment = comment;
                String = @string;
                Keyword = keyword;
                Function = function;
                Builtin = builtin;
                Number = number;
                SearchMatch = searchMatch;
                SearchMatchCurrent = searchMatchCurrent;
            }
        }

        private const string TokenPattern =
            @"(?<comment>//[^\r\n]*|/\*[\s\S]*?(?:\*/|$))"
            + @"|(?<string>""(?:\\[\s\S]|[^""\\\r\n])*""|'(?:\\[\s\S]|[^'\\\r\n])*'|`(?:\\[\s\S]|[^`\\])*`)"
            + @"|(?<keyword>\b(?:const|let|var|function|return|if|else|while|do|for|of|in|break|continue|switch|case|default|try|catch|finally|throw|new|typeof|instanceof|void|type|interface|enum|as)\b)"
            + @"|(?<builtin>\b(?:true|false|null|undefined|number|string|boolean|Direction|Error)\b|(?<=[a-zA-Z_$]\.)[a-zA-Z_$][\w$]*)"
            + @"|(?<function>\b[a-zA-Z_$][\w$]*(?=\())"
            + @"|(?<number>\b(?:\d*\.)?\d+\b)";

        private static readonly Regex BaseRegex = new Regex(TokenPattern, RegexOptions.Compiled);

        /// <summary>
        /// Produces TextMeshPro rich-text markup matching native CodeUtilities.SyntaxColor2 tag structure
        /// so overlay character indices stay aligned with CodeInputField.
        /// </summary>
        public static string ColorCode(
            string code,
            ThemePalette palette,
            string searchWord = "",
            int searchIndex = -1,
            (int index, int length) imeCompositionRange = default)
        {
            code ??= string.Empty;
            if (imeCompositionRange.length > 0 && searchIndex >= imeCompositionRange.index)
                searchIndex += imeCompositionRange.length;

            var hasSearch = !string.IsNullOrEmpty(searchWord);
            var escapedSearch = hasSearch ? Regex.Escape(searchWord) : null;
            var tokenRegex = hasSearch
                ? new Regex("(?<search>(?i:" + escapedSearch + ")(?-i))|" + TokenPattern)
                : BaseRegex;
            var searchRegex = hasSearch ? new Regex(escapedSearch, RegexOptions.IgnoreCase) : null;

            var builder = new StringBuilder("<noparse>");
            var cursor = 0;
            foreach (Match match in tokenRegex.Matches(code))
            {
                builder.Append(UnderlineComposition(code.Substring(cursor, match.Index - cursor), cursor));
                var formatted = FormatText(match.Value, match.Index);
                string color = null;
                if (match.Groups["comment"].Success) color = palette.Comment;
                else if (match.Groups["string"].Success) color = palette.String;
                else if (match.Groups["keyword"].Success) color = palette.Keyword;
                else if (match.Groups["function"].Success) color = palette.Function;
                else if (match.Groups["builtin"].Success) color = palette.Builtin;
                else if (match.Groups["number"].Success) color = palette.Number;

                builder.Append(color == null
                    ? formatted
                    : "</noparse><color=" + color + "><noparse>" + formatted + "</noparse></color><noparse>");
                cursor = match.Index + match.Length;
            }
            builder.Append(UnderlineComposition(code.Substring(cursor), cursor));
            builder.Append("</noparse>");
            return builder.ToString();

            string FormatText(string value, int start)
            {
                if (!hasSearch) return UnderlineComposition(value, start);
                var searchBuilder = new StringBuilder();
                var offset = 0;
                foreach (Match searchMatch in searchRegex.Matches(value))
                {
                    searchBuilder.Append(UnderlineComposition(value.Substring(offset, searchMatch.Index - offset), start + offset));
                    var markColor = start + searchMatch.Index == searchIndex
                        ? palette.SearchMatchCurrent
                        : palette.SearchMatch;
                    searchBuilder.Append("</noparse><mark=" + markColor + " from_search><noparse>");
                    searchBuilder.Append(UnderlineComposition(searchMatch.Value, start + searchMatch.Index));
                    searchBuilder.Append("</noparse></mark><noparse>");
                    offset = searchMatch.Index + searchMatch.Length;
                }
                searchBuilder.Append(UnderlineComposition(value.Substring(offset), start + offset));
                return searchBuilder.ToString();
            }

            string UnderlineComposition(string value, int start)
            {
                if (imeCompositionRange.length <= 0) return value;
                var underlineStart = Math.Max(start, imeCompositionRange.index) - start;
                var underlineEnd = Math.Min(start + value.Length, imeCompositionRange.index + imeCompositionRange.length) - start;
                if (underlineStart >= underlineEnd) return value;
                return value.Substring(0, underlineStart)
                    + "</noparse><u><noparse>"
                    + value.Substring(underlineStart, underlineEnd - underlineStart)
                    + "</noparse></u><noparse>"
                    + value.Substring(underlineEnd);
            }
        }

        /// <summary>
        /// Computes TypeScript newline indentation preserving leading whitespace, adding an indent level after '{', '(', or '[',
        /// and expanding '{|}', '(|)', or '[|]' across an indented middle line with the closing delimiter aligned below.
        /// </summary>
        public static bool TryIndentNewLine(
            string source,
            string inserted,
            int stringPosition,
            out string updatedSource,
            out int updatedPosition)
        {
            updatedSource = source;
            updatedPosition = stringPosition;
            if (string.IsNullOrEmpty(source)
                || string.IsNullOrEmpty(inserted)
                || !inserted.EndsWith("\n", StringComparison.Ordinal))
                return false;

            var newlineIndex = stringPosition - 1;
            if (newlineIndex <= 0 || newlineIndex >= source.Length || source[newlineIndex] != '\n')
                return false;

            var lineStart = source.LastIndexOf('\n', newlineIndex - 1) + 1;
            var indentEnd = lineStart;
            while (indentEnd < newlineIndex && (source[indentEnd] == ' ' || source[indentEnd] == '\t'))
                indentEnd++;

            var baseIndent = source.Substring(lineStart, indentEnd - lineStart);
            var lastNonSpace = newlineIndex - 1;
            while (lastNonSpace >= indentEnd
                && (source[lastNonSpace] == ' ' || source[lastNonSpace] == '\t' || source[lastNonSpace] == '\r'))
                lastNonSpace--;

            if (lastNonSpace >= indentEnd
                && (source[lastNonSpace] == '{' || source[lastNonSpace] == '(' || source[lastNonSpace] == '['))
            {
                var expectedClose = source[lastNonSpace] == '{' ? '}'
                    : source[lastNonSpace] == '(' ? ')' : ']';
                var innerIndent = baseIndent + (baseIndent.StartsWith(" ", StringComparison.Ordinal) ? "    " : "\t");
                var afterSpaces = newlineIndex + 1;
                while (afterSpaces < source.Length && (source[afterSpaces] == ' ' || source[afterSpaces] == '\t'))
                    afterSpaces++;

                if (afterSpaces < source.Length && source[afterSpaces] == expectedClose)
                {
                    updatedSource = source.Substring(0, newlineIndex + 1)
                        + innerIndent
                        + "\n"
                        + baseIndent
                        + source.Substring(afterSpaces);
                    updatedPosition = newlineIndex + 1 + innerIndent.Length;
                    return true;
                }

                updatedSource = source.Insert(newlineIndex + 1, innerIndent);
                updatedPosition = stringPosition + innerIndent.Length;
                return true;
            }

            if (baseIndent.Length == 0) return false;
            updatedSource = source.Insert(newlineIndex + 1, baseIndent);
            updatedPosition = stringPosition + baseIndent.Length;
            return true;
        }

        /// <summary>
        /// Auto-inserts a matching closing delimiter or quote, or steps over an excess auto-closed delimiter/quote.
        /// </summary>
        public static bool TryAutoCloseBrace(
            string source,
            string inserted,
            int stringPosition,
            out string updatedSource,
            out int updatedPosition)
        {
            updatedSource = source;
            updatedPosition = stringPosition;
            if (string.IsNullOrEmpty(source)
                || string.IsNullOrEmpty(inserted)
                || stringPosition <= 0
                || stringPosition > source.Length)
                return false;

            if (inserted == "\"" || inserted == "'" || inserted == "`")
            {
                var quote = inserted[0];
                if (source[stringPosition - 1] != quote || IsEscaped(source, stringPosition - 1)) return false;
                if (CountUnescaped(source, quote) % 2 == 0) return false;
                if (stringPosition < source.Length && source[stringPosition] == quote)
                {
                    updatedSource = source.Remove(stringPosition, 1);
                    updatedPosition = stringPosition;
                    return true;
                }
                if (stringPosition >= 2)
                {
                    var prev = source[stringPosition - 2];
                    if (char.IsLetterOrDigit(prev) || prev == '_' || prev == '$' || prev == quote)
                        return false;
                }
                if (stringPosition < source.Length && !IsAutoCloseFollower(source[stringPosition])) return false;
                updatedSource = source.Insert(stringPosition, quote.ToString());
                updatedPosition = stringPosition;
                return true;
            }

            if (inserted == "{" || inserted == "(" || inserted == "[")
            {
                var open = inserted[0];
                var close = open == '{' ? '}' : open == '(' ? ')' : ']';
                if (source[stringPosition - 1] != open) return false;
                if (CountChar(source, open) <= CountChar(source, close)) return false;
                if (stringPosition < source.Length && !IsAutoCloseFollower(source[stringPosition])) return false;
                updatedSource = source.Insert(stringPosition, close.ToString());
                updatedPosition = stringPosition;
                return true;
            }

            if (inserted == "}" || inserted == ")" || inserted == "]")
            {
                var close = inserted[0];
                var open = close == '}' ? '{' : close == ')' ? '(' : '[';
                if (stringPosition < source.Length
                    && source[stringPosition - 1] == close
                    && source[stringPosition] == close
                    && CountChar(source, close) > CountChar(source, open))
                {
                    updatedSource = source.Remove(stringPosition, 1);
                    updatedPosition = stringPosition;
                    return true;
                }
            }

            return false;
        }

        private static bool IsAutoCloseFollower(char ch) =>
            ch == ' ' || ch == '\t' || ch == '\r' || ch == '\n'
            || ch == ')' || ch == ']' || ch == '}' || ch == ';' || ch == ',' || ch == ':';

        private static bool IsEscaped(string text, int index)
        {
            var backslashes = 0;
            for (var i = index - 1; i >= 0 && text[i] == '\\'; i--)
                backslashes++;
            return backslashes % 2 == 1;
        }

        private static int CountUnescaped(string text, char target)
        {
            var count = 0;
            for (var i = 0; i < text.Length; i++)
                if (text[i] == target && !IsEscaped(text, i)) count++;
            return count;
        }

        private static int CountChar(string text, char target)
        {
            var count = 0;
            for (var i = 0; i < text.Length; i++)
                if (text[i] == target) count++;
            return count;
        }
    }
}
