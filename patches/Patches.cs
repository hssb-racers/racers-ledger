using BBI.Unity.Game;
using HarmonyLib;
using System.Collections.Generic;
using System.Reflection;
using Unity.Entities;

namespace RACErsLedger.Patches
{
    [HarmonyPatch]
    class RACErsLedgerPatch1
    {
        [HarmonyTargetMethod]
        public static MethodBase TargetMethod()
        {
            return typeof(SalvageableChangedEvent).GetMethod("ProcessObject", BindingFlags.Public | BindingFlags.Static);
        }

        [HarmonyPrefix]
        public static bool Prefix(
      Entity salvagedEntity,
      SalvageableSystem.ProcessedSalvageableInfo salvageableInfo,
      float mass,
      string name,
      List<CategoryAsset> categories,
      bool isSellOff,
      SalvagedBy salvagedBy,
      EntityCommandBuffer commandBuffer)
        {
            Plugin.Log(BepInEx.Logging.LogLevel.Info, $"called: SalvageableChangedEvent.ProcessObject({salvagedEntity.ToString()}, {salvageableInfo.ToString()}, {mass.ToString()}, " +
                $"{Main.Instance.LocalizationService.Localize(name, null)}, {categories.ToString()}, {isSellOff.ToString()}, {salvagedBy.ToString()}, {commandBuffer.ToString()})");
            return true;
        }
    }

    [HarmonyPatch]
    class RACErsLedgerPatch2
    {
        [HarmonyTargetMethod]
        public static MethodBase TargetMethod()
        {
            return typeof(SalvageableChangedEvent).GetMethod("DestroyObject", BindingFlags.Public | BindingFlags.Static);
        }

        [HarmonyPrefix]
        public static bool Prefix(
      Entity salvagedEntity,
      SalvageableSystem.ProcessedSalvageableInfo salvageableInfo,
      float mass,
      string name,
      List<CategoryAsset> categories,
      bool scrapped,
      SalvagedBy salvagedBy,
      EntityCommandBuffer commandBuffer)
        {
            Plugin.Log(BepInEx.Logging.LogLevel.Info, $"called: SalvageableChangedEvent.DestroyObject({salvagedEntity.ToString()}, {salvageableInfo.ToString()}, {mass.ToString()}, " +
                "{Main.Instance.LocalizationService.Localize(name, null)}, {categories.ToString()}, {scrapped.ToString()}, {salvagedBy.ToString()}, {commandBuffer.ToString()})");
            return true;
        }
    }
}
