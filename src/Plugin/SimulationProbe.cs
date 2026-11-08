using System;
using System.Collections.Concurrent;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Threading;
using HarmonyLib;

namespace TheFarmerWasRefactored
{
    // Observe the native scheduler only; never call it or change simulation state.
    internal sealed class SimulationProbe : IDisposable
    {
        private sealed class Seen
        {
            public int Id, Thread;
            public long? Idle, Running;
            public bool IdleAdvanced, RunningAdvanced;
        }

        private const string ExpectedHash = "f0efda7c138de8028b1769c121a4b5c17b6bb538aeae5a67d1881e7c4ebd7d25";
        private static SimulationProbe active;
        private readonly Harmony harmony = new Harmony(Plugin.Id + ".simulation-probe");
        private readonly ConcurrentQueue<string> messages = new ConcurrentQueue<string>();
        private readonly ConditionalWeakTable<object, Seen> seen = new ConditionalWeakTable<object, Seen>();
        private readonly int unityThread = Thread.CurrentThread.ManagedThreadId;
        private readonly Action<string> report;
        private readonly Type mainSim;
        private readonly FieldInfo runner, nanoseconds;
        private readonly PropertyInfo time, opDuration, execution;
        private readonly MethodInfo target;
        private int nextId, remaining = 32, finished;
        private bool disposed;

        public SimulationProbe(Action<string> report)
        {
            this.report = report;
            var simulation = AccessTools.TypeByName("Simulation") ?? throw new MissingMemberException("Simulation");
            mainSim = simulation.Assembly.GetType("MainSim", true);
            using (var input = File.OpenRead(simulation.Assembly.Location))
            using (var sha = SHA256.Create())
            {
                var hash = BitConverter.ToString(sha.ComputeHash(input)).Replace("-", "").ToLowerInvariant();
                if (hash != ExpectedHash) throw new NotSupportedException("Core.dll differs from the inspected build.");
            }
            time = simulation.GetProperty("CurrentTime") ?? throw new MissingMemberException("CurrentTime");
            opDuration = simulation.GetProperty("OpDuration") ?? throw new MissingMemberException("OpDuration");
            execution = simulation.GetProperty("Execution") ?? throw new MissingMemberException("Execution");
            runner = simulation.GetField("simRunner") ?? throw new MissingMemberException("simRunner");
            nanoseconds = time.PropertyType.GetField("nanoseconds") ?? throw new MissingMemberException("nanoseconds");
            target = simulation.GetMethod("RunNextStep", new[] { time.PropertyType, typeof(object), typeof(bool) });
            if (target == null || target.ReturnType != typeof(void) || nanoseconds.FieldType != typeof(long)
                || opDuration.PropertyType != time.PropertyType)
                throw new MissingMemberException("Unexpected simulation method/member shapes.");
            if (Interlocked.CompareExchange(ref active, this, null) != null)
                throw new InvalidOperationException("Simulation probe already installed.");
            try { harmony.Patch(target, prefix: new HarmonyMethod(typeof(SimulationProbe), nameof(BeforeStep))); }
            catch { Interlocked.CompareExchange(ref active, null, this); harmony.UnpatchSelf(); throw; }
            report("Simulation probe: hook installed; load a farm, wait briefly, then run native code.");
        }

        private static void BeforeStep(object __instance, object __1)
        {
            var probe = Volatile.Read(ref active);
            if (probe == null || Volatile.Read(ref probe.finished) != 0) return;
            try { probe.Observe(__instance, __1); }
            catch (Exception error)
            {
                if (Interlocked.Exchange(ref probe.finished, 1) == 0)
                    probe.messages.Enqueue($"Simulation probe FAILED: {error.GetType().Name}: {error.Message}");
            }
        }

        private void Observe(object simulation, object simulationLock)
        {
            if (simulationLock == null) throw new InvalidOperationException("Missing native simulation lock.");
            // Match the native lock, only for the snapshot; release before RunNextStep can wait.
            lock (simulationLock)
            {
                var owner = runner.GetValue(simulation);
                if (owner == null || owner.GetType() != mainSim) return; // Exclude UISim/other runners.
                var thread = Thread.CurrentThread.ManagedThreadId;
                if (thread == unityThread) throw new InvalidOperationException("Scheduler observed on Unity thread.");
                var state = seen.GetValue(simulation, _ => new Seen { Id = Interlocked.Increment(ref nextId), Thread = thread });
                if (state.Thread != thread) throw new InvalidOperationException("Simulation changed execution threads.");
                var now = (long)nanoseconds.GetValue(time.GetValue(simulation));
                var op = (long)nanoseconds.GetValue(opDuration.GetValue(simulation));
                var running = execution.GetValue(simulation) != null;
                if (op <= 0) throw new InvalidOperationException("Nonpositive operation duration.");
                bool emit;
                if (running)
                {
                    emit = !state.Running.HasValue || (!state.RunningAdvanced && now > state.Running.Value);
                    if (state.Running.HasValue && now > state.Running.Value) state.RunningAdvanced = true;
                    if (!state.Running.HasValue) state.Running = now;
                }
                else
                {
                    emit = !state.Idle.HasValue || (!state.IdleAdvanced && now > state.Idle.Value);
                    if (state.Idle.HasValue && now > state.Idle.Value) state.IdleAdvanced = true;
                    if (!state.Idle.HasValue) state.Idle = now;
                }
                if (emit)
                {
                    if (Interlocked.Decrement(ref remaining) < 0)
                        throw new InvalidOperationException("Observation limit reached; restart diagnostic.");
                    messages.Enqueue($"Simulation probe: snapshot generation={state.Id} thread={thread} unityThread={unityThread} executing={running} timeNs={now} opNs={op}");
                }
                if (state.IdleAdvanced && state.RunningAdvanced && Interlocked.Exchange(ref finished, 1) == 0)
                    messages.Enqueue("Simulation probe: distinct owner thread and advancing idle/running time ALL PASS");
            }
        }

        public void Poll()
        {
            while (messages.TryDequeue(out var message)) report(message);
            if (Volatile.Read(ref finished) != 0) Dispose();
        }

        public void Dispose()
        {
            if (disposed) return;
            disposed = true;
            Interlocked.CompareExchange(ref active, null, this);
            harmony.UnpatchSelf(); // Only this diagnostic's patches; leave other mods alone.
        }
    }
}
