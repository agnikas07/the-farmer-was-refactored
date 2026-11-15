using System;
using System.Reflection;
using System.Threading;
using HarmonyLib;

namespace TheFarmerWasRefactored
{
    // Installed only inside the existing source-mode/checksum gate.
    internal static class SourceRunLifecycle
    {
        private static readonly FieldInfo SimulationLock = AccessTools.Field(typeof(MainSim), "lockSimulation")
            ?? throw new MissingFieldException("MainSim", "lockSimulation");

        internal static void Install(Harmony harmony)
        {
            _ = SimulationLock;
            harmony.Patch(AccessTools.Method(typeof(Window), "Close"),
                prefix: new HarmonyMethod(typeof(SourceRunLifecycle), nameof(BeforeClose)));
            harmony.Patch(AccessTools.Method(typeof(MainSim), "SetupSim"),
                prefix: new HarmonyMethod(typeof(SourceRunLifecycle), nameof(BeforeWorldChange)));
            harmony.Patch(AccessTools.Method(typeof(MainSim), "OnDestroy"),
                prefix: new HarmonyMethod(typeof(SourceRunLifecycle), nameof(BeforeWorldChange)));
        }

        private static void BeforeClose(Window __instance)
        {
            var window = __instance.GetComponent<CodeWindow>();
            if (window == null) return; // Documentation and unrelated windows are unaffected.
            SourceEditorGuard.Forget(window);
            var run = Volatile.Read(ref ActionSchedulerGate.Run);
            if (run?.Execution.sim.simRunner is MainSim owner)
                StopOwnedRun(owner, window);
        }

        private static void BeforeWorldChange(MainSim __instance)
        {
            StopOwnedRun(__instance, null);
        }

        private static void StopOwnedRun(MainSim owner, CodeWindow closingWindow)
        {
            // Re-read ownership under the same lock used by native start/stop and stepping.
            // Dispose cancels the mailbox/worker without joining either thread.
            lock (SimulationLock.GetValue(owner))
            {
                var run = Volatile.Read(ref ActionSchedulerGate.Run);
                if (run == null || !ReferenceEquals(run.Execution.sim.simRunner, owner)
                    || (closingWindow != null && run.SourceWindow != closingWindow)) return;
                if (run.SourceWindow != null) SourceEditorGuard.Forget(run.SourceWindow);
                if (ReferenceEquals(run.Execution.sim.Execution, run.Execution))
                    run.Execution.sim.StopProgramExecution();
                // Also detach a stale run whose native execution was already removed.
                ActionSchedulerGate.BeforeStop(run.Execution);
            }
        }
    }
}
