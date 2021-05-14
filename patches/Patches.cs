using BBI.Unity.Game;
using BepInEx.Logging;
using HarmonyLib;
using JetBrains.Annotations;
using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Unity.Entities;

namespace RACErsLedger.Patches
{
    [HarmonyPatch]
    class RACErsLedgerPatchAddHooks
    {
        [HarmonyTargetMethod]
        [UsedImplicitly]
        public static MethodBase TargetMethod()
        {
            return typeof(GameSession).GetMethod("Initialize", BindingFlags.Public | BindingFlags.Instance);
        }

        [HarmonyPrefix]
        [UsedImplicitly]
        public static bool Postfix()
        {
            try
            {
                Plugin.Log(LogLevel.Info, "hello session");
                Main.EventSystem.AddHandler(new Carbon.Core.Events.EventHandler<SalvageableChangedEvent>(SalvageableChangedEventHandler));
                Main.EventSystem.AddHandler(new Carbon.Core.Events.EventHandler<GameStateChangedEvent>(GameStateChangedEventHandler));
            }
            catch (Exception e) { Plugin.Log(LogLevel.Error, e.ToString()); }
            return true;

        }
        public static void SalvageableChangedEventHandler(SalvageableChangedEvent ev)
        {
            try
            {
                var timer = World.DefaultGameObjectInjectionWorld.EntityManager.GetComponentData<GameSessionTimerData>(GameSession.CurrentSessionEntity);
                Plugin.Log(LogLevel.Debug, $"@{timer.CurrentTime} received SalvageableChangedEvent {{ state:{ev.State}, entity:{ev.SalvagedEntity}," +
                    $" SalvageableInfo:{{ SalvageableComponent:{ev.SalvageableInfo.SalvageableComponent}, MassAtTimeOfProcessing:{ev.SalvageableInfo.MassAtTimeOfProcessing}, ObjectName(Localized):{Utilities.Localize(ev.SalvageableInfo.ObjectName)}, " +
                    $"ObjectCategories:[{string.Join(",", from category in ev.SalvageableInfo.ObjectCategories select Utilities.Localize(category.CategoryName))}]," +
                    $" Rewards:[{string.Join(",", from reward in ev.SalvageableInfo.Rewards select $"{{c:{reward.CurrencyAssetID}, v:{reward.ValueAtTimeOfProcessing}, mbv:{reward.MassBasedValue}, oibv:{reward.ObjectIntegrityBasedValue}}}")}], QualityWhenSalvaged:{ev.SalvageableInfo.QualityWhenSalvaged}, " +
                    $"SalvagedBy:{ev.SalvageableInfo.SalvagedBy}}}, Mass:{ev.Mass}, ObjectName(Localized):{Utilities.Localize(ev.ObjectName)}, Categories:[{string.Join(",", from category in ev.Categories select Utilities.Localize(category.CategoryName))}]," +
                    $" IsSellOff:{ev.IsSellOff}, Scrapped:{ev.Scrapped}, SalvagedBy:{ev.SalvagedBy}, CommandBuffer:{ev.CommandBuffer} }}");

                string objectName = Utilities.Localize(ev.SalvageableInfo.ObjectName);
                float mass = ev.SalvageableInfo.MassAtTimeOfProcessing;
                string[] categories = (from category in ev.SalvageableInfo.ObjectCategories select Utilities.Localize(category.CategoryName)).ToArray();
                string salvagedBy = ev.SalvageableInfo.SalvagedBy.ToString();
                // assume that we only have one reward value and that it's always the dollars. maybe risky, not sure. debug logs will help me with this if it's ever a problem i guess!
                float value = 0;
                bool massBasedValue = true;
                // sometimes there's a fun bug where there's 0 rewards and i think that was causing the event processing to throw an error and retry.
                // let's just log that as 0.
                // though, if we don't get any rewards, perhaps we should just ignore it instead of logging it to the ledger?
                if (ev.SalvageableInfo.Rewards.Count > 0)
                {
                    value = ev.SalvageableInfo.Rewards[0].ValueAtTimeOfProcessing;
                    massBasedValue = ev.SalvageableInfo.Rewards[0].MassBasedValue;
                }
                bool destroyed = (ev.State == SalvageableChangedEvent.SalvageableState.Destroyed);
                float gameTime = timer.TimerCountsUp ? (float)timer.CurrentTime : timer.MaxTime - timer.CurrentTime;

                Plugin.StateManager.CurrentShift.AddSalvage(objectName, mass, categories, salvagedBy, value, massBasedValue, destroyed, gameTime);
            }
            catch (Exception e) { Plugin.Log(LogLevel.Error, e.ToString()); }
        }
        public static void GameStateChangedEventHandler(GameStateChangedEvent ev)
        {
            try
            {
                // surpress the seldom duplicate events towards the outside
                if( ev.PrevGameState != ev.GameState ){
                    Plugin.LampreyManager.SendEvent(new RACErsLedger.DataTypes.GameStateChangedEvent(ev.GameState, ev.PrevGameState));
                }
                Plugin.Log(LogLevel.Debug, $"received GameStateChangedEvent {{ GameState:{ev.GameState}, PrevGameState:{ev.PrevGameState} }}");
                if (ev.GameState == GameSession.GameState.Gameplay && (ev.PrevGameState == GameSession.GameState.Splash || ev.PrevGameState == GameSession.GameState.Loading))
                {
                    // looks like this is the start of a new shift!
                    Plugin.Log(LogLevel.Info, "looks like there's a new shift running, starting a new shift log");
                    var shift = Plugin.StateManager.StartShift();
                    if (GameSession.CurrentSessionType == GameSession.SessionType.WeeklyShip)
                    {
                        Plugin.Log(LogLevel.Debug, "looks like we're in RACE, let's grab some info about it");
                        var weeklyShipResult = LynxOnlineService.Instance.WeeklyShip.GetRecentWeeklyShip();
                        var shipPreview = LynxOnlineService.Instance.WeeklyShip.GetRecentWeeklyShipPreview();
                        var maxSalvageableValue = PlaystreamService.Instance.GetShipValue(shipPreview);
                        shift.SetRACEInfo(weeklyShipResult.Seed, weeklyShipResult.Version, weeklyShipResult.StartDateUTC, (int) maxSalvageableValue, (int) shipPreview.Mass);
                    }
                }
                if (
                    ev.GameState == GameSession.GameState.GameComplete /* Shift Summary screen */ ||
                    ev.GameState == GameSession.GameState.None /* Esc -> Quit in RACE (maybe mark as abandoned specifically somewhere?) */
                    )
                {
                    // looks like the end of the shift! 

                    string ExitCause;
                    switch (ev.GameState)
                    {
                        case GameSession.GameState.GameComplete:
                            ExitCause = "complete";
                            break;
                        case GameSession.GameState.None:
                            ExitCause = "abort";
                            break;
                        default:
                            ExitCause = "unknown (" + ev.GameState + ")";
                            break;
                    }

                    // TODO(sariya): keep an eye out for any other state transitions that mean "over" that you weren't expecting -- especially what's timeout? i think there's always a GameComplete though. 
                    Plugin.Log(LogLevel.Info, "looks like current shift is over, marking end time on shift asynchronously. Cause: " + ExitCause);

                    new Task(() => Plugin.StateManager.EndShift(ExitCause)).Start();
                }
            }
            catch (Exception e) { Plugin.Log(LogLevel.Error, e.ToString()); }
        }
    }

    [HarmonyPatch]
    class RACErsLedgerPatchRemoveHooks
    {
        [HarmonyTargetMethod]
        [UsedImplicitly]
        public static MethodBase TargetMethod()
        {
            return typeof(GameSession).GetMethod("Shutdown", BindingFlags.Public | BindingFlags.Instance);
        }

        [HarmonyPrefix]
        [UsedImplicitly]
        public static bool Postfix()
        {
            Plugin.Log(LogLevel.Info, "goodbye session");
            Main.EventSystem.RemoveHandler(new Carbon.Core.Events.EventHandler<SalvageableChangedEvent>(RACErsLedgerPatchAddHooks.SalvageableChangedEventHandler));
            Main.EventSystem.RemoveHandler(new Carbon.Core.Events.EventHandler<GameStateChangedEvent>(RACErsLedgerPatchAddHooks.GameStateChangedEventHandler));

            return true;
        }
    }

    [HarmonyPatch]
    class RACErsLedgerTimerTicks
    {
        [HarmonyTargetMethod]
        [UsedImplicitly]
        public static MethodBase TargetMethod()
        {
            return typeof(GameSessionTimerData).GetMethod("UpdateTimer", BindingFlags.Public | BindingFlags.Instance);
        }

        private static int previousTime;

        [HarmonyPrefix]
        [UsedImplicitly]
        public static void Postfix()
        {
            var timer = World.DefaultGameObjectInjectionWorld.EntityManager.GetComponentData<GameSessionTimerData>(GameSession.CurrentSessionEntity);

            // trigger the event only once a second (game runs it about 20 times a second)
            if( previousTime == (int) timer.CurrentTime ){
                return;
            }
            previousTime = (int) timer.CurrentTime;

            Plugin.LampreyManager.SendEvent(new RACErsLedger.DataTypes.TimeTickEvent(timer.CurrentTime, timer.MaxTime, timer.IsFinished, timer.TimerCountsUp));

            Plugin.Log(LogLevel.Debug, $"time tick of {timer.CurrentTime}, max time {timer.MaxTime}, finished: {timer.IsFinished}, TimerCountsUp: {timer.TimerCountsUp}");
        }
    }
}
