using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using JetBrains.Annotations;
using System.IO;


namespace RACErsLedger
{
    [BepInPlugin(UUID, "RACErs Ledger", "1.4.0.0")]
    [BepInProcess("Shipbreaker.exe")]
    public class Plugin : BaseUnityPlugin
    {
        private const string UUID = "dev.sariya.racersledger";
        private static ManualLogSource _logSource;
        public static StateManager StateManager { get; private set; }
        public static ConfigEntry<string> ConfigDataFolder { get; private set; }
        public static ConfigEntry<bool> ConfigEnableLamprey { get; private set; }
        public static ConfigEntry<int> ConfigLampreyListenPort { get; private set; }
        public static ConfigEntry<int> ConfigWebsocketListenPort { get; private set; }
        public static LampreyManager LampreyManager { get; private set; }

        [UsedImplicitly]
        public void Awake()
        {
            _logSource = Logger;
            // TODO(sariya) strip config data out into separate class
            ConfigDataFolder = Config.Bind(
                "RACErsLedger",
                "DataFolder",
                Path.Combine(Paths.GameRootPath, "RACErsLedger"),
                "Folder to save shift ledger data to. Will be created if it doesn't exist."
                );
            ConfigEnableLamprey = Config.Bind(
                "RACErsLedger",
                "UseLamprey",
                true,
                "Enable sidecar process for streaming events to interested clients (i.e. live data visualizers)."
            );
            ConfigLampreyListenPort = Config.Bind(
                "RACErsLedger",
                "LampreyListenPort",
                42069,
                // TODO(sariya) maybe we SHOULD do validation on this
                "Only change this if you know what you're doing. What port does the lamprey process accept connections on? Must be between 1 and 65535, inclusive, and also follow the rest of the rules of ports. We don't do validation on this."
            );
            ConfigWebsocketListenPort = Config.Bind(
                "RACErsLedger",
                "WebsocketListenPort",
                32325,
                // TODO(sariya) same as above
                "Only change this if you know what you're doing. What port does the RACErsLedger mod listen for websocket connections on? Must be between 1 and 65535, inclusive, and also follow the rest of the rules of ports. We don't do validation on this."
                );

            Log(LogLevel.Info, "RACErs Ledger loaded.");

            if (!Directory.Exists(ConfigDataFolder.Value))
            {
                Log(LogLevel.Info, $"DataFolder {ConfigDataFolder.Value} doesn't appear to exist, attempting to create...");
                Directory.CreateDirectory(ConfigDataFolder.Value);
                Log(LogLevel.Info, $"Succeeded creating {ConfigDataFolder.Value}!");
            }

            StateManager = new StateManager(ConfigDataFolder.Value);
            LampreyManager = new LampreyManager(websocketListenPort: ConfigWebsocketListenPort.Value, lampreyListenPort: ConfigLampreyListenPort.Value);
            if (ConfigEnableLamprey.Value)
            {
                LampreyManager.Start();
            }

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

        public void OnApplicationQuit()
        {
            if (ConfigEnableLamprey.Value)
            {
                LampreyManager.Stop();
            }
        }
        public static void Log(LogLevel level, string msg)
        {
            _logSource.Log(level, msg);
        }
    }
}


