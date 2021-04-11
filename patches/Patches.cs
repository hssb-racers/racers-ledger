using BBI.Unity.Game;
using BepInEx.Logging;
using HarmonyLib;
using System.Collections.Generic;
using System.Reflection;
using Unity.Entities;

namespace RACErsLedger.Patches
{
    [HarmonyPatch]
    class RACErsLedgerPatchAddHooks
    {
        [HarmonyTargetMethod]
        public static MethodBase TargetMethod()
        {
            return typeof(GameSession).GetMethod("Initialize", BindingFlags.Public | BindingFlags.Instance);
        }

        [HarmonyPrefix]
        public static bool Postfix()
        {
            Plugin.Log(LogLevel.Info, "hello session");
            Main.EventSystem.AddHandler<SalvageableChangedEvent>(new Carbon.Core.Events.EventHandler<SalvageableChangedEvent>(SalvageableChangedEventHandler));
            Main.EventSystem.AddHandler<GameStateChangedEvent>(new Carbon.Core.Events.EventHandler<GameStateChangedEvent>(GameStateChangedEventHandler));

            return true;
        }
        public static void SalvageableChangedEventHandler(SalvageableChangedEvent ev)
        {
            Plugin.Log(LogLevel.Info, $"received SalvageableChangedEvent {{ state:{ev.State}, entity:{ev.SalvagedEntity}, SalvageableInfo:{{ SalvageableComponent:{ev.SalvageableInfo.SalvageableComponent}, MassAtTimeOfProcessing:{ev.SalvageableInfo.MassAtTimeOfProcessing}, ObjectName(Localized):{Main.Instance.LocalizationService.Localize(ev.SalvageableInfo.ObjectName, null)}, ObjectCategories:{ev.SalvageableInfo.ObjectCategories}, Rewards:{ev.SalvageableInfo.Rewards}, QualityWhenSalvaged:{ev.SalvageableInfo.QualityWhenSalvaged}, SalvagedBy:{ev.SalvageableInfo.SalvagedBy}}}, Mass:{ev.Mass}, ObjectName(Localized):{Main.Instance.LocalizationService.Localize(ev.ObjectName, null)}, Categories:{ev.Categories}, IsSellOff:{ev.IsSellOff}, Scrapped:{ev.Scrapped}, SalvagedBy:{ev.SalvagedBy}, CommandBuffer:{ev.CommandBuffer} }} ");
        }
        public static void GameStateChangedEventHandler(GameStateChangedEvent ev) {
            Plugin.Log(LogLevel.Info, $"received GameStateChangedEvent {{ GameState:{ev.GameState}, PrevGameState:{ev.PrevGameState} }}");
        }
    }

    [HarmonyPatch]
    class RACErsLedgerPatchRemoveHooks
    {
        [HarmonyTargetMethod]
        public static MethodBase TargetMethod()
        {
            return typeof(GameSession).GetMethod("Shutdown", BindingFlags.Public | BindingFlags.Instance);
        }

        [HarmonyPrefix]
        public static bool Postfix()
        {
            Plugin.Log(LogLevel.Info, "goodbye session");
            Main.EventSystem.RemoveHandler<SalvageableChangedEvent>(new Carbon.Core.Events.EventHandler<SalvageableChangedEvent>(RACErsLedgerPatchAddHooks.SalvageableChangedEventHandler));
            Main.EventSystem.RemoveHandler<GameStateChangedEvent>(new Carbon.Core.Events.EventHandler<GameStateChangedEvent>(RACErsLedgerPatchAddHooks.GameStateChangedEventHandler));

            return true;
        }
    }
}
