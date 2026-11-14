using System;
using System.Diagnostics;
using System.Globalization;
using System.Threading;

namespace TheFarmerWasRefactored
{
    // Immutable origin shared by Unity, simulation, and worker threads.
    internal sealed class SourceRunTiming
    {
        private static int nextId;
        private readonly int id = Interlocked.Increment(ref nextId);
        private readonly long origin = Stopwatch.GetTimestamp();
        private readonly Action<string> report;

        internal SourceRunTiming(Action<string> report) { this.report = report; }

        internal void Mark(string stage)
        {
            var elapsed = (Stopwatch.GetTimestamp() - origin) * 1000.0 / Stopwatch.Frequency;
            report($"TypeScript source timing: run={id} stage={stage} elapsedMs={elapsed.ToString("F2", CultureInfo.InvariantCulture)}");
        }
    }
}
