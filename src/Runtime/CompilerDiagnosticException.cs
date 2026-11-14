using System;

namespace TheFarmerWasRefactored.Runtime
{
    public sealed class CompilerDiagnosticException : InvalidOperationException
    {
        public string DisplayMessage { get; }
        public int Start { get; }
        public int Length { get; }
        public CompilerDiagnosticException(string details, string displayMessage, int start, int length)
            : base(details) { DisplayMessage = displayMessage; Start = start; Length = length; }

        // TypeScript and the game's input both index UTF-16 code units; native end is inclusive.
        public (int Start, int End) Span(string source)
        {
            var last = Math.Max(0, source.Length - 1);
            var start = Math.Max(0, Math.Min(Start, last));
            var end = (int)Math.Min(last, (long)start + Math.Max(1, Length) - 1);
            return (start, end);
        }
    }
}
