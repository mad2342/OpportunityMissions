using System;
using System.Collections.Generic;
using BattleTech;
using BattleTech.Data;
using Harmony;
using UnityEngine;

namespace OpportunityMissions.Patches
{
    [HarmonyPatch(typeof(SimGameEventTracker), "CheckRoll")]
    public static class SimGameEventTracker_CheckRoll_Patch
    {
        public static void Prefix(SimGameEventTracker __instance, ref float randomRoll, float ___eventChance, float ___chanceIncrement)
        {
            try
            {
                Logger.Info($"[SimGameEventTracker_CheckRoll_PREFIX] (ORIGINAL) randomRoll: {randomRoll}");
                Logger.Info($"[SimGameEventTracker_CheckRoll_PREFIX] ___eventChance: {___eventChance}");
                Logger.Info($"[SimGameEventTracker_CheckRoll_PREFIX] ___chanceIncrement: {___chanceIncrement}");

                // Tweak the roll to slightly raise the frequency of events
                float modifiedRoll = randomRoll - 10;
                randomRoll = Mathf.Clamp(modifiedRoll, 0f, 100f);

                Logger.Info($"[SimGameEventTracker_CheckRoll_PREFIX] (TWEAKED) randomRoll: {randomRoll}");
            }
            catch (Exception e)
            {
                Logger.Error(e);
            }
        }
    }



    [HarmonyPatch(typeof(SimGameEventTracker), "GetRandomEvent")]
    public static class SimGameEventTracker_GetRandomEvent_Patch
    {
        public static void Postfix(SimGameEventTracker __instance, SimGameEventTracker.PotentialEvent __result, ref List<string> ___discardList, SimGameState ___sim)
        {
            try
            {
                Logger.Info($"[SimGameEventTracker_GetRandomEvent_POSTFIX] __result.Def.Description.Id: {__result.Def.Description.Id}");
                Logger.Info($"[SimGameEventTracker_GetRandomEvent_POSTFIX] ___discardList: {String.Join(", ", ___discardList.ToArray())}");
                Logger.Info($"[SimGameEventTracker_GetRandomEvent_POSTFIX] ___sim.CompanyTags: {String.Join(", ", ___sim.CompanyTags.ToArray())}");

                // Remove custom events from discardList to allow them to reoccur earlier (after their exclusion tags expire)
                // REQUIRES custom exclusion tag in SimGameEventDef
                if (__result.Def.Requirements.ExclusionTags.Contains("starsystem_opportunitymission_active"))
                {
                    ___discardList.Remove(__result.Def.Description.Id);
                }

                Logger.Info($"[SimGameEventTracker_GetRandomEvent_POSTFIX] ___discardList: {String.Join(", ", ___discardList.ToArray())}");
            }
            catch (Exception e)
            {
                Logger.Error(e);
            }
        }
    }



    // Info
    [HarmonyPatch(typeof(SimGameEventTracker), "IsEventValid")]
    public static class SimGameEventTracker_IsEventValid_Patch
    {
        public static void Postfix(SimGameEventTracker __instance, bool __result, EventScope scope, EventDef_MDD evt)
        {
            try
            {
                //Logger.Info($"[SimGameEventTracker_IsEventValid_POSTFIX] evt.EventDefID: {evt.EventDefID}");
                //Logger.Info($"[SimGameEventTracker_IsEventValid_POSTFIX] scope: {scope}");
                //Logger.Info($"[SimGameEventTracker_IsEventValid_POSTFIX] __result: {__result}");

                if (__result)
                {
                    Logger.Info($"[SimGameEventTracker_IsEventValid_POSTFIX] Currently valid event id: {evt.EventDefID}");
                }
            }
            catch (Exception e)
            {
                Logger.Error(e);
            }
        }
    }
}
