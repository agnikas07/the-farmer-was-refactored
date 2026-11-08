using System;
using System.Collections.Concurrent;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using BepInEx;
using BepInEx.Bootstrap;
using Microsoft.ClearScript.V8;

namespace TheFarmerWasRefactored.V8Probe
{
    [BepInDependency(global::TheFarmerWasRefactored.Plugin.Id)]
    [BepInPlugin("io.github.agnikas07.thefarmerwasrefactored.v8probe", "Refactored V8 loader probe", "0.1.0")]
    public sealed class Plugin : BaseUnityPlugin
    {
        private readonly ConcurrentQueue<string> messages = new ConcurrentQueue<string>();
        private int stopped;
        private readonly ConcurrentQueue<string> compiledPrograms = new ConcurrentQueue<string>();
        private IDisposable actionProbe;

        private void Awake()
        {
            var nativeActions = Config.Bind("Diagnostics", "RunCompiledActionProbe", false,
                "Compile a fixed TypeScript loop and arm two real native flips from the marked program.").Value;
            var useJint = Config.Bind("Diagnostics", "RunJintPipelineProbe", false,
                "Execute V8-compiled TypeScript in bounded Jint engines with counting stubs.").Value;
            var compile = Config.Bind("Diagnostics", "RunCompilerProbe", false,
                "Compile trusted TypeScript fixtures on the V8 worker; no farm actions.").Value;
            if (!Config.Bind("Diagnostics", "Enabled", false,
                "Load Windows x64 V8 on a worker. Development-only; action mode requires a marked program.").Value) return;
            if (Environment.OSVersion.Platform != PlatformID.Win32NT || !Environment.Is64BitProcess)
            {
                Logger.LogError("V8 loader probe: requires a Windows x64 process (including Proton).");
                return;
            }
            var unityThread = Thread.CurrentThread.ManagedThreadId;
            var bundlePath = Path.Combine(Path.GetDirectoryName(Info.Location), "compiler-bundle.json");
            compile = compile || useJint;
            var prefix = nativeActions ? "TypeScript action probe" : useJint ? "V8 to Jint game probe" : compile ? "V8 game compiler probe" : "V8 loader probe";
            Logger.LogInfo(prefix + ": starting worker");
            _ = Task.Run(() =>
            {
                try
                {
                    Run(unityThread);
                    if (nativeActions && Volatile.Read(ref stopped) == 0)
                    {
                        var javascript = ActionProgramCompiler.Compile(bundlePath);
                        if (Volatile.Read(ref stopped) == 0) compiledPrograms.Enqueue(javascript);
                    }
                    else if (compile && Volatile.Read(ref stopped) == 0)
                        CompilerChecks.Run(bundlePath,
                            message => messages.Enqueue(prefix + ": " + message),
                            () => Volatile.Read(ref stopped) != 0,
                            useJint ? (Action<string>)(javascript => JintOutputChecks.Run(javascript)) : null);
                }
                catch (Exception error) { messages.Enqueue($"{prefix} FAILED: {error}"); }
            });
        }

        private void Run(int unityThread)
        {
            if (Volatile.Read(ref stopped) != 0) return;
            var workerThread = Thread.CurrentThread.ManagedThreadId;
            if (workerThread == unityThread) throw new InvalidOperationException("Worker ran on Unity thread.");
            using (var engine = new V8ScriptEngine())
            {
                if (Convert.ToInt32(engine.Evaluate("6 * 7")) != 42)
                    throw new InvalidOperationException("Evaluation mismatch.");
                if (Convert.ToInt32(engine.Evaluate(
                    "(function(){let count=0; scan:while(count<2){count++;continue;}return count;})()")) != 2)
                    throw new InvalidOperationException("Labeled-loop mismatch.");
                messages.Enqueue($"V8 loader probe: native evaluation and loop control PASS (worker={workerThread}, unity={unityThread})");
            }
            if (Volatile.Read(ref stopped) != 0) return;
            // Both engine instances are created, used, and disposed by this worker.
            using (var fresh = new V8ScriptEngine())
            {
                if (Convert.ToInt32(fresh.Evaluate("21 * 2")) != 42)
                    throw new InvalidOperationException("Fresh engine mismatch.");
            }
            messages.Enqueue("V8 loader probe: disposal and fresh engine PASS");
            messages.Enqueue("V8 loader probe: ALL PASS");
        }

        private void Update()
        {
            while (messages.TryDequeue(out var message)) Logger.LogInfo(message);
            if (Volatile.Read(ref stopped) == 0 && compiledPrograms.TryDequeue(out var javascript))
            {
                try
                {
                    var main = (global::TheFarmerWasRefactored.Plugin)Chainloader.PluginInfos[
                        global::TheFarmerWasRefactored.Plugin.Id].Instance;
                    actionProbe = main.ArmCompiledActionProbe(javascript);
                    Logger.LogInfo("TypeScript action probe: compiler and bounded Jint preflight PASS; native probe ready");
                }
                catch (Exception error) { Logger.LogError($"TypeScript action probe FAILED: {error}"); }
            }
        }

        private void OnDestroy()
        {
            // Fixed scripts terminate themselves. Never join or dispose an engine on Unity.
            Interlocked.Exchange(ref stopped, 1);
            actionProbe?.Dispose();
        }
    }
}
