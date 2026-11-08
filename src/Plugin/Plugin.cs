using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using BepInEx;
using TheFarmerWasRefactored.Runtime;
using UnityEngine;

namespace TheFarmerWasRefactored
{
    [BepInPlugin(Id, Name, Version)]
    public sealed class Plugin : BaseUnityPlugin
    {
        public const string Id = "io.github.agnikas07.thefarmerwasrefactored";
        public const string Name = "The Farmer was Refactored";
        public const string Version = "0.1.0";

        private CancellationTokenSource steppingStop;
        private DispatchProbe dispatchProbe;
        private SimulationProbe simulationProbe;

        private void Awake()
        {
            Logger.LogInfo("The Farmer was Refactored loaded.");
            if (Config.Bind("Diagnostics", "RunSimulationProbe", false,
                "Observe native simulation ownership and time without mutations.").Value)
            {
                try { simulationProbe = new SimulationProbe(message => Logger.LogInfo(message)); }
                catch (Exception error) { Logger.LogError($"Simulation probe disabled: {error}"); }
            }
            Logger.LogInfo($"Plugin {Version}; game {Application.version}; Unity {Application.unityVersion}; platform {Application.platform}.");
            if (Config.Bind("Diagnostics", "RunRuntimeProbe", false,
                "Run bounded JavaScript feasibility checks at startup. Development only.").Value)
            {
                try { RuntimeProbe.Run(message => Logger.LogInfo(message)); }
                catch (Exception error) { Logger.LogError($"Runtime probe FAILED: {error}"); }
            }
            if (Config.Bind("Diagnostics", "RunSteppingProbe", false,
                "Run background pause/resume diagnostics. Development only.").Value)
                StartCoroutine(RunSteppingProbe());
            if (Config.Bind("Diagnostics", "RunDispatchProbe", false,
                "Test worker-to-main-thread request dispatch. Development only.").Value)
            {
                dispatchProbe = new DispatchProbe(message => Logger.LogInfo(message), value =>
                {
                    _ = Time.frameCount; // Exercise a read-only Unity API on the owner thread.
                    return value + 1;
                });
            }
        }

        private IEnumerator RunSteppingProbe()
        {
            var messages = new ConcurrentQueue<string>();
            var stop = steppingStop = new CancellationTokenSource();
            var probe = Task.Run(() => SteppingProbe.Run(messages.Enqueue, stop.Token));
            // Disposal happens only after the worker no longer needs the token source.
            _ = probe.ContinueWith(_ => stop.Dispose(), TaskScheduler.Default);
            while (!probe.IsCompleted)
            {
                while (messages.TryDequeue(out var message)) Logger.LogInfo(message);
                yield return null;
            }
            while (messages.TryDequeue(out var remaining)) Logger.LogInfo(remaining);
            if (probe.IsFaulted) Logger.LogError($"Stepping probe FAILED: {probe.Exception}");
            steppingStop = null;
        }

        private void Update()
        {
            simulationProbe?.Poll();
            if (dispatchProbe == null) return;
            try
            {
                dispatchProbe.Tick();
                if (!dispatchProbe.IsCompleted) return;
            }
            catch (Exception error) { Logger.LogError($"Dispatch probe FAILED: {error}"); }
            dispatchProbe.Dispose();
            dispatchProbe = null;
        }

        private void OnDestroy()
        {
            simulationProbe?.Dispose();
            dispatchProbe?.Dispose();
            try { steppingStop?.Cancel(); }
            catch (ObjectDisposedException) { } // The completion continuation won the race.
        }
    }
}
