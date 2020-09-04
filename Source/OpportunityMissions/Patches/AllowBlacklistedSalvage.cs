using System;
using System.Collections.Generic;
using BattleTech;
using BattleTech.Data;
using BattleTech.UI;
using Harmony;
using OpportunityMissions.Extensions;

namespace OpportunityMissions.Patches
{
    // Check persistence of tags on units
    [HarmonyPatch(typeof(CombatHUDTargetingComputer), "RefreshActorInfo")]
    public static class CombatHUDTargetingComputer_RefreshActorInfo_Patch
    {
        public static void Postfix(CombatHUDTargetingComputer __instance)
        {
            try
            {
                if (__instance.ActivelyShownCombatant != null && __instance.ActivelyShownCombatant is Mech mech)
                {
                    Logger.Debug($"[CombatHUDTargetingComputer_RefreshActorInfo_POSTFIX] ({mech.DisplayName}) mech.MechDef.MechTags: {String.Join(", ", mech.MechDef.MechTags.ToArray())}");
                }
            }
            catch (Exception e)
            {
                Logger.Error(e);
            }
        }
    }



    [HarmonyPatch(typeof(AbstractActor), "CreateSpawnEffectByTag")]
    public static class AbstractActor_CreateSpawnEffectByTag_Patch
    {
        public static bool Prefix(AbstractActor __instance, string effectTag)
        {
            try
            {
                if (__instance is Mech mech)
                {
                    if (effectTag == "tagMech-WHITELIST_PARTS")
                    {
                        mech.MechDef.MechTags.Add("WHITELIST_PARTS");
                        Logger.Debug($"[AbstractActor_CreateSpawnEffectByTag_PREFIX] Added WHITELIST_PARTS tag to MechDef of {mech.DisplayName}");

                        return false;
                    }
                    else if (effectTag == "tagMech-WHITELIST_COMPONENTS")
                    {
                        mech.MechDef.MechTags.Add("WHITELIST_COMPONENTS");
                        Logger.Debug($"[AbstractActor_CreateSpawnEffectByTag_PREFIX] Added WHITELIST_COMPONENTS tag to MechDef of {mech.DisplayName}");

                        return false;
                    }
                }

                return true;
            }
            catch (Exception e)
            {
                Logger.Error(e);
                return true;
            }
        }
    }



    [HarmonyPatch(typeof(Contract), "GenerateSalvage")]
    public static class Contract_GenerateSalvage_Patch
    {
        public static void Postfix(Contract __instance, List<UnitResult> enemyMechs, ref List<SalvageDef> ___finalPotentialSalvage, DataManager ___dataManager)
        {
            try
            {
                // Only for opportunity contracts
                if (__instance.IsOpportunityMission())
                {
                    Logger.Debug($"[Contract_GenerateSalvage_POSTFIX] Checking BLACKLISTED overrides for contract {__instance.Override.ID}");

                    SimGameState simGameState = __instance.BattleTechGame.Simulation;
                    SimGameConstants simGameConstants = simGameState.Constants;

                    foreach (UnitResult unitResult in enemyMechs)
                    {
                        MechDef mech = unitResult.mech;
                        Pilot pilot = unitResult.pilot;



                        // Component handling needs to be before evaluating MechParts as there the MechDef is probably normalized
                        if (mech.MechTags.Contains("WHITELIST_COMPONENTS"))
                        {
                            Logger.Debug($"[Contract_GenerateSalvage_POSTFIX] MechDef of {mech.Name} has WHITELIST_COMPONENTS tag. Manually adding blacklisted components to salvage.");

                            foreach (MechComponentRef mechComponentRef in mech.Inventory)
                            {
                                // Skip if destroyed or in a destroyed location
                                if (mech.IsLocationDestroyed(mechComponentRef.MountedLocation) || mechComponentRef.DamageLevel == ComponentDamageLevel.Destroyed)
                                {
                                    continue;
                                }

                                if (mechComponentRef.Def.ComponentTags.Contains("BLACKLISTED"))
                                {
                                    Logger.Debug($"[Contract_GenerateSalvage_PREFIX] mechComponentRef.Def {mechComponentRef.Def.Description.Id} has BLACKLISTED tag. Adding to salvage.");

                                    SalvageDef salvageDef = Utilities.CreateComponent(simGameConstants, __instance, mechComponentRef.Def);
                                    ___finalPotentialSalvage.Add(salvageDef);
                                }
                            }
                        }



                        // MechParts
                        if (mech.MechTags.Contains("BLACKLISTED") || mech.Chassis.ChassisTags.Contains("BLACKLISTED"))
                        {
                            Logger.Debug($"[Contract_GenerateSalvage_POSTFIX] MechDef or ChassisDef of {mech.Name} has BLACKLISTED tag. Checking for override flags.");

                            if (mech.MechTags.Contains("WHITELIST_PARTS"))
                            {
                                Logger.Debug($"[Contract_GenerateSalvage_POSTFIX] MechDef of {mech.Name} has WHITELIST_PARTS tag. Manually adding MechParts to salvage.");

                                // Needs to work on original MechDef so has to be called before a possible normalization
                                int numberOfParts = Utilities.GetMechPartCountForSalvage(mech, pilot);

                                // Normalize MechDef
                                string stockMechDefId = mech.ChassisID.Replace("chassisdef", "mechdef");
                                if (mech.Description.Id != stockMechDefId)
                                {
                                    Logger.Debug($"[Contract_GenerateSalvage_POSTFIX] Normalizing {mech.Description.Id} to {stockMechDefId}");
                                    mech = ___dataManager.MechDefs.Get(stockMechDefId);
                                }

                                // Add MechParts to Salvage
                                for (int i = 0; i < numberOfParts; i++)
                                {
                                    SalvageDef salvageDef = Utilities.CreateMechPart(simGameConstants, __instance, mech);
                                    ___finalPotentialSalvage.Add(salvageDef);
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Logger.Error(e);
            }
        }
    }
}
