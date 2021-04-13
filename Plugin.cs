using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using System.IO;
using JetBrains.Annotations;


namespace RACErsLedger
{
    [BepInPlugin(UUID, "RACErs Ledger", "1.1.0.0")]
    [BepInProcess("Shipbreaker.exe")]
    public class Plugin : BaseUnityPlugin
    {
        private const string UUID = "dev.sariya.racersledger";
        private static ManualLogSource _logSource;
        public static StateManager StateManager { get; private set; }
        public static ConfigEntry<string> ConfigDataFolder { get; private set; }

        [UsedImplicitly]
        public void Awake()
        {
            _logSource = Logger;
            ConfigDataFolder = Config.Bind(
                "RACErsLedger",
                "DataFolder",
                Path.Combine(Paths.GameRootPath, "RACErsLedger"),
                "Folder to save shift ledger data to. Will be created if it doesn't exist."
                );

            Log(LogLevel.Info, "RACErs Ledger loaded.");

            if (!Directory.Exists(ConfigDataFolder.Value))
            {
                Log(LogLevel.Info, $"DataFolder {ConfigDataFolder.Value} doesn't appear to exist, attempting to create...");
                Directory.CreateDirectory(ConfigDataFolder.Value);
                Log(LogLevel.Info, $"Succeeded creating {ConfigDataFolder.Value}!");
            }

            StateManager = new StateManager(ConfigDataFolder.Value);

            var harmony = new Harmony(UUID);
            harmony.PatchAll();
            Log(LogLevel.Info, "RACErs Ledger patched!");

            foreach (var patchedMethod in harmony.GetPatchedMethods())
            {
                Log(
                    LogLevel.Info,
                    $"Patched: {patchedMethod.DeclaringType?.FullName}:{patchedMethod}");
            }
        }
        public static void Log(LogLevel level, string msg)
        {
            _logSource.Log(level, msg);
        }
    }
}


