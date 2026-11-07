using System;
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

        private void Awake()
        {
            Logger.LogInfo("The Farmer was Refactored loaded.");
            Logger.LogInfo($"Plugin {Version}; game {Application.version}; Unity {Application.unityVersion}; platform {Application.platform}.");
            if (Config.Bind("Diagnostics", "RunRuntimeProbe", false,
                "Run bounded JavaScript feasibility checks at startup. Development only.").Value)
            {
                try { RuntimeProbe.Run(message => Logger.LogInfo(message)); }
                catch (Exception error) { Logger.LogError($"Runtime probe FAILED: {error}"); }
            }
        }
    }
}
