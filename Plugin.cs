﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BepInEx;
using BepInEx.Logging;
using HarmonyLib;


namespace racers_ledger
{
    [BepInPlugin(UUID, "RACErs Ledger", "0.0.0.1")]
    [BepInProcess("Shipbreaker.exe")]
    public class Plugin : BaseUnityPlugin
    {
        private const string UUID = "dev.sariya.racersledger";
        private static ManualLogSource logSource;

        public void Awake()
        {
            logSource = Logger;

            Log(LogLevel.Info, "RACErs Ledger loaded.");

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


