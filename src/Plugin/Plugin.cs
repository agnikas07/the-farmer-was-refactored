using BepInEx;
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
        }
    }
}
