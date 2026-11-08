using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Threading;
using HarmonyLib;

namespace TheFarmerWasRefactored
{
    // Opt-in JavaScript-to-native action diagnostic, not a player language.
    internal sealed class NativeActionProbe : IDisposable
    {
        private readonly string marker;
        private readonly string javascript;
        private readonly string label;
        private readonly Func<string, string> compile;
        private static NativeActionProbe active;
        private readonly Harmony harmony = new Harmony(Plugin.Id + ".native-action-probe");
        private readonly ConcurrentQueue<string> messages = new ConcurrentQueue<string>();
        private readonly Action<string> report;
        private readonly int unityThread = Thread.CurrentThread.ManagedThreadId;
        private int claimed, disposed;

        public NativeActionProbe(Action<string> report, string compiledJavaScript = null, Func<string, string> compile = null)
        {
            this.report = report;
            this.compile = compile;
            javascript = compiledJavaScript ?? "doAFlip(); doAFlip();";
            label = compile != null ? "TypeScript source probe" : compiledJavaScript == null ? "JavaScript action probe" : "TypeScript action probe";
            marker = compiledJavaScript == null ? "# refactored-javascript-probe\ndoAFlip()"
                : "# refactored-typescript-probe\ndoAFlip()";
            using (var input = File.OpenRead(typeof(Node).Assembly.Location))
            using (var sha = SHA256.Create())
            {
                var hash = BitConverter.ToString(sha.ComputeHash(input)).Replace("-", "").ToLowerInvariant();
                if (hash != "f0efda7c138de8028b1769c121a4b5c17b6bb538aeae5a67d1881e7c4ebd7d25")
                    throw new NotSupportedException("Core.dll differs from the inspected build.");
            }
            if (Interlocked.CompareExchange(ref active, this, null) != null)
                throw new InvalidOperationException("JavaScript action probe already installed.");
            try
            {
                if (compile != null)
                    harmony.Patch(AccessTools.Method(typeof(CodeWindow), "StartExecution", new[] { typeof(bool) }),
                        prefix: new HarmonyMethod(typeof(NativeActionProbe), nameof(BeforeSourceRun)));
                var target = AccessTools.Method(typeof(MainSim), "StartMainExecution",
                    new[] { typeof(CodeWindow), typeof(Node), typeof(bool) });
                if (target == null) throw new MissingMethodException("StartMainExecution");
                harmony.Patch(target, prefix: new HarmonyMethod(typeof(NativeActionProbe), nameof(BeforeStart)));
                harmony.Patch(AccessTools.Method(typeof(Execution), "Execute"),
                    transpiler: new HarmonyMethod(typeof(ActionSchedulerGate), nameof(ActionSchedulerGate.YieldAtLoopBoundary)));
                harmony.Patch(AccessTools.Method(typeof(Simulation), "RunNextStep"),
                    prefix: new HarmonyMethod(typeof(ActionSchedulerGate), nameof(ActionSchedulerGate.BeforeSimulationStep)));
                harmony.Patch(AccessTools.Method(typeof(Execution), "StopExecution"),
                    prefix: new HarmonyMethod(typeof(ActionSchedulerGate), nameof(ActionSchedulerGate.BeforeStop)));
            }
            catch { Dispose(); throw; }
            if (compile != null) report($"{label}: ready; Run a window beginning with // @refactored");
            else report($"{label}: armed; run the marked two-line program in the normal farm.");
        }

        private static bool BeforeSourceRun(CodeWindow __instance, bool stepByStep)
        {
            var probe = Volatile.Read(ref active);
            if (probe?.compile == null) return true;
            var source = ReadSource(__instance);
            if (!source.Replace("\r\n", "\n").StartsWith("// @refactored\n", StringComparison.Ordinal)) return true;
            // This prefix runs on Unity's Run-button path, before the native parser.
            if (stepByStep)
            {
                probe.messages.Enqueue($"{probe.label}: step mode is not supported; use ordinary Run.");
                return false;
            }
            probe.messages.Enqueue($"{probe.label}: captured {source.Length} characters for this run");
            MainSim.Inst.StopMainExecution();
            MainSim.Inst.StartMainExecution(__instance, new FlipNode(__instance, probe, source), false);
            return false;
        }

        private static string ReadSource(CodeWindow window)
        {
            var input = AccessTools.Property(typeof(CodeWindow), "CodeInput").GetValue(window);
            return (string)AccessTools.Property(input.GetType(), "text").GetValue(input);
        }

        private static void BeforeStart(CodeWindow cw, ref Node syntaxTree, bool stepByStep)
        {
            var probe = Volatile.Read(ref active);
            if (probe == null || probe.compile != null || cw == null || syntaxTree == null || stepByStep) return;
            try
            {
                // Reflect UI text so no UI library needs to be redistributed or referenced here.
                var input = AccessTools.Property(typeof(CodeWindow), "CodeInput").GetValue(cw);
                var source = (string)AccessTools.Property(input.GetType(), "text").GetValue(input);
                if (source.Replace("\r\n", "\n").Trim() != probe.marker) return;
                if (Interlocked.CompareExchange(ref probe.claimed, 1, 0) != 0) return;
                syntaxTree = new FlipNode(cw, probe);
                probe.messages.Enqueue($"{probe.label}: custom node selected; expecting two doAFlip calls.");
            }
            catch (Exception error)
            {
                probe.messages.Enqueue($"{probe.label} FAILED: {error.GetType().Name}: {error.Message}");
            }
        }

        private sealed class FlipNode : Node
        {
            private readonly NativeActionProbe probe;
            private readonly string source;
            public FlipNode(CodeWindow window, NativeActionProbe probe, string source = null)
                : base(window, source == null ? probe.marker.IndexOf("doAFlip", StringComparison.Ordinal) : 0,
                    (source ?? probe.marker).Length - 1)
            {
                this.probe = probe;
                this.source = source;
            }

            public override IEnumerable<double> Execute(ProgramState state, Execution execution, int depth)
            {
                var thread = Thread.CurrentThread.ManagedThreadId;
                if (Volatile.Read(ref probe.disposed) != 0) yield break;
                if (thread == probe.unityThread || execution.sim.leaderboardType != LeaderboardType.none
                    || execution.States.Count != 1 || state.DroneId != 0 || execution.sim.simRunner.GetType() != typeof(MainSim))
                {
                    probe.messages.Enqueue($"{probe.label} FAILED: requires one drone in a normal farm on the simulation thread.");
                    yield break;
                }
                state.CurrentExecutingNode = this;
                var run = new JavaScriptActionRun(execution,
                    message => probe.messages.Enqueue(message.Replace("JavaScript action probe", probe.label)),
                    probe.javascript, source == null ? (Func<string>)null : () => probe.compile(source),
                    source == null ? (int?)2 : null);
                if (Interlocked.CompareExchange(ref ActionSchedulerGate.Run, run, null) != null)
                {
                    run.Dispose();
                    throw new InvalidOperationException("Diagnostic run already active.");
                }
                foreach (var step in run.Commands(this, state)) yield return step;
            }

            public override Node DeepCopy(Dictionary<object, object> copies)
            {
                if (copies.TryGetValue(this, out var existing)) return (Node)existing;
                var copy = new FlipNode(boxedParams.codeWindow, probe, source);
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
            Interlocked.Exchange(ref ActionSchedulerGate.Run, null)?.Dispose();
            harmony.UnpatchSelf();
        }
    }
}
