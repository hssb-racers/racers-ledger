using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using System.IO;



namespace RACErsLedger
{
    [BepInPlugin(UUID, "RACErs Ledger", "0.0.0.1")]
    [BepInProcess("Shipbreaker.exe")]
    public class Plugin : BaseUnityPlugin
    {
        private const string UUID = "dev.sariya.racersledger";
        private static ManualLogSource logSource;
        public static StateManager StateManager { get; private set; }
        public static ConfigEntry<string> ConfigDataFolder { get; private set; }

        public void Awake()
        {
            logSource = Logger;
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
            logSource.Log(level, msg);
        }
    }
}


