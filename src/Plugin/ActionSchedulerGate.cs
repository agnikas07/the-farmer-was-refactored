using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using System.Threading;
using HarmonyLib;

namespace TheFarmerWasRefactored
{
    internal static class ActionSchedulerGate
    {
        internal static JavaScriptActionRun Run;

        internal static IEnumerable<CodeInstruction> YieldAtLoopBoundary(
            IEnumerable<CodeInstruction> instructions, ILGenerator generator)
        {
            var code = instructions.ToList();
            var getter = AccessTools.PropertyGetter(typeof(Execution), "GlobalOpCount");
            var matches = Enumerable.Range(0, code.Count - 2).Where(i =>
                code[i].opcode == OpCodes.Ldarg_0 && code[i + 1].Calls(getter)
                && code[i + 2].opcode == OpCodes.Stloc_3).ToList();
            if (matches.Count != 1) throw new NotSupportedException("Native execution loop shape changed.");
            var index = matches[0]; // Inspected loop head lies outside both native locks/try blocks.
            if (code[index].blocks.Count != 0) throw new NotSupportedException("Unexpected exception boundary.");
            var resume = generator.DefineLabel();
            var enter = new CodeInstruction(OpCodes.Ldarg_0);
            // Loader Harmony references framework Label; use its list through IList
            // to avoid the netstandard reference-assembly type-forwarding mismatch.
            var labelsField = AccessTools.Field(typeof(CodeInstruction), "labels");
            var originalLabels = (IList)labelsField.GetValue(code[index]);
            var enterLabels = (IList)labelsField.GetValue(enter);
            foreach (var label in originalLabels) enterLabels.Add(label);
            originalLabels.Clear();
            originalLabels.Add(resume);
            code.InsertRange(index, new[]
            {
                enter,
                new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(ActionSchedulerGate), nameof(ShouldYield))),
                new CodeInstruction(OpCodes.Brfalse, resume),
                new CodeInstruction(OpCodes.Ret)
            });
            return code;
        }

        private static bool ShouldYield(Execution execution)
        {
            var run = Volatile.Read(ref Run);
            if (run == null || run.Execution != execution || !run.YieldRequested) return false;
            run.YieldRequested = false;
            run.RecordYield();
            return true;
        }

        internal static bool BeforeSimulationStep(Simulation __instance, object __1)
        {
            var run = Volatile.Read(ref Run);
            if (run == null || run.Execution.sim != __instance) return true;
            lock (__1)
            {
                if (__instance.Execution != run.Execution || !run.NeedsWorker) return true;
                // Like the native idle wait, release the simulation lock while parked.
                // Unity pulses this lock each frame; timeout also covers a missing pulse.
                Monitor.Wait(__1, 10);
                return false;
            }
        }

        internal static void BeforeStop(Execution __instance)
        {
            var run = Volatile.Read(ref Run);
            if (run != null && run.Execution == __instance)
            {
                Interlocked.CompareExchange(ref Run, null, run);
                run.Dispose();
            }
        }
    }
}
