using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Threading;
using HarmonyLib;

namespace TheFarmerWasRefactored
{
    // A fixed command source for testing native scheduling, not a player language.
    internal sealed class NativeActionProbe : IDisposable
    {
        private const string Marker = "# refactored-action-probe\ndo_a_flip()";
        private static NativeActionProbe active;
        private readonly Harmony harmony = new Harmony(Plugin.Id + ".native-action-probe");
        private readonly ConcurrentQueue<string> messages = new ConcurrentQueue<string>();
        private readonly Action<string> report;
        private readonly int unityThread = Thread.CurrentThread.ManagedThreadId;
        private int claimed, disposed;

        public NativeActionProbe(Action<string> report)
        {
            this.report = report;
            using (var input = File.OpenRead(typeof(Node).Assembly.Location))
            using (var sha = SHA256.Create())
            {
                var hash = BitConverter.ToString(sha.ComputeHash(input)).Replace("-", "").ToLowerInvariant();
                if (hash != "f0efda7c138de8028b1769c121a4b5c17b6bb538aeae5a67d1881e7c4ebd7d25")
                    throw new NotSupportedException("Core.dll differs from the inspected build.");
            }
            if (Interlocked.CompareExchange(ref active, this, null) != null)
                throw new InvalidOperationException("Native action probe already installed.");
            try
            {
                var target = AccessTools.Method(typeof(MainSim), "StartMainExecution",
                    new[] { typeof(CodeWindow), typeof(Node), typeof(bool) });
                if (target == null) throw new MissingMethodException("StartMainExecution");
                harmony.Patch(target, prefix: new HarmonyMethod(typeof(NativeActionProbe), nameof(BeforeStart)));
            }
            catch { Dispose(); throw; }
            report("Native action probe: armed; run the marked two-line program in the normal farm.");
        }

        private static void BeforeStart(CodeWindow cw, ref Node syntaxTree, bool stepByStep)
        {
            var probe = Volatile.Read(ref active);
            if (probe == null || cw == null || syntaxTree == null || stepByStep) return;
            try
            {
                // Reflect UI text so no UI library needs to be redistributed or referenced here.
                var input = AccessTools.Property(typeof(CodeWindow), "CodeInput").GetValue(cw);
                var source = (string)AccessTools.Property(input.GetType(), "text").GetValue(input);
                if (source.Replace("\r\n", "\n").Trim() != Marker) return;
                if (Interlocked.CompareExchange(ref probe.claimed, 1, 0) != 0) return;
                syntaxTree = new FlipNode(cw, probe);
                probe.messages.Enqueue("Native action probe: custom node selected; expecting two native flips.");
            }
            catch (Exception error)
            {
                probe.messages.Enqueue($"Native action probe FAILED: {error.GetType().Name}: {error.Message}");
            }
        }

        private sealed class FlipNode : Node
        {
            private readonly NativeActionProbe probe;
            public FlipNode(CodeWindow window, NativeActionProbe probe) : base(window, Marker.IndexOf("do_a_flip", StringComparison.Ordinal), Marker.Length - 1)
            {
                this.probe = probe;
            }

            public override IEnumerable<double> Execute(ProgramState state, Execution execution, int depth)
            {
                var thread = Thread.CurrentThread.ManagedThreadId;
                if (Volatile.Read(ref probe.disposed) != 0) yield break;
                if (thread == probe.unityThread || execution.sim.leaderboardType != LeaderboardType.none
                    || execution.States.Count != 1 || state.DroneId != 0 || execution.sim.simRunner.GetType() != typeof(MainSim))
                {
                    probe.messages.Enqueue("Native action probe FAILED: requires one drone in a normal farm on the simulation thread.");
                    yield break;
                }
                // The scheduler owns this iterator. No worker waits, direct Drone calls,
                // manual ApplySideEffect calls, or synthetic time/operation increments.
                state.CurrentExecutingNode = this;
                for (var index = 1; index <= 2; index++)
                {
                    if (Volatile.Read(ref probe.disposed) != 0) yield break;
                    var before = state.OpCount;
                    state.currentDependencies.Add(("do_a_flip", boxedParams.wordStart, boxedParams.wordEnd));
                    var ops = BuiltinFunctions.Functions["do_a_flip"].binding(
                        new List<IPyObject>(), execution.sim, execution, state.DroneId);
                    if (CheckIncrementOpCount(state, execution, ops)) yield return 0;
                    // Resumed by native scheduling only after its action cost gate.
                    var charged = state.OpCount - before;
                    if (state.currentSideEffect != SideEffect.None || !(state.ReturnValue is PyNone)
                        || charged <= 0 || Thread.CurrentThread.ManagedThreadId != thread)
                    {
                        probe.messages.Enqueue("Native action probe FAILED: unexpected native completion.");
                        yield break;
                    }
                    probe.messages.Enqueue($"Native action probe: flip={index} thread={thread} chargedOps={charged}");
                }
                probe.messages.Enqueue("Native action probe: two native actions completed ALL PASS");
            }

            public override Node DeepCopy(Dictionary<object, object> copies)
            {
                if (copies.TryGetValue(this, out var existing)) return (Node)existing;
                var copy = new FlipNode(boxedParams.codeWindow, probe);
                copies[this] = copy;
                return copy;
            }
        }

        public void Poll()
        {
            while (messages.TryDequeue(out var message)) report(message);
        }

        public void Dispose()
        {
            if (Interlocked.Exchange(ref disposed, 1) != 0) return;
            Interlocked.CompareExchange(ref active, null, this);
            harmony.UnpatchSelf();
        }
    }
}
