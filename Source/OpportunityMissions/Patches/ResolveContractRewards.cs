using System;
using System.Linq;
using BattleTech;
using BattleTech.Framework;
using BattleTech.UI;
using BattleTech.UI.TMProWrapper;
using Harmony;
using HBS.Collections;
using OpportunityMissions.Extensions;

namespace OpportunityMissions.Patches
{
    // If a successfully resolved bonus objective is marked by tags, display a note on objective list item in AAR
    [HarmonyPatch(typeof(AAR_ObjectiveListItem), "Init")]
    public static class AAR_ObjectiveListItem_Init_Patch
    {
        public static void Postfix(AAR_ObjectiveListItem __instance, MissionObjectiveResult missionObjectiveResult, Contract contract, LocalizableText ___GainsText)
        {
            try
            {
                // Only for successful opportunity missions
                if (contract.IsOpportunityMission() && contract.State == Contract.ContractState.Complete)
                {
                    Logger.Debug($"[AAR_ObjectiveListItem_Init_POSTFIX] Append text for {contract.Override.ID} if special tags are present.");

                    // Successful secondary objective
                    if (!missionObjectiveResult.isPrimary && missionObjectiveResult.status == ObjectiveStatus.Succeeded)
                    {
                        foreach (SimGameEventResult simGameEventResult in missionObjectiveResult.simGameEventResultList)
                        {
                            // If any result of the objective contains a special tag, which will trigger a reward popup, set the info text and leave the loop
                            if (simGameEventResult.AddedTags.ToArray().Any(tag => tag.Contains("triggerReward-")))
                            {
                                // NOTE: If any other result did already set some text it will get overwritten
                                ___GainsText.gameObject.SetActive(true);
                                ___GainsText.SetText("• Acquired Mission Bonus Reward", new object[] { });
                                break;
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



    // If a successfully resolved bonus objective is marked by tags, enrich the debriefing text of the employer with additional text
    [HarmonyPatch(typeof(AAR_ContractTermsWidget), "SetFactionResponseText")]
    public static class AAR_ContractTermsWidget_SetFactionResponseText_Patch
    {
        public static void Postfix(AAR_ContractTermsWidget __instance, Contract ___theContract, LocalizableText ___DescriptionText)
        {
            try
            {
                // Only for successful opportunity missions
                if (___theContract.IsOpportunityMission() && ___theContract.State == Contract.ContractState.Complete)
                {
                    Logger.Debug($"[AAR_ContractTermsWidget_SetFactionResponseText_POSTFIX] Append text for {___theContract.Override.ID} if special tags are present.");

                    foreach (MissionObjectiveResult missionObjectiveResult in ___theContract.MissionObjectiveResultList)
                    {
                        // Successful secondary objective
                        if (!missionObjectiveResult.isPrimary && missionObjectiveResult.status == ObjectiveStatus.Succeeded)
                        {
                            foreach (SimGameEventResult simGameEventResult in missionObjectiveResult.simGameEventResultList)
                            {
                                // If any result of the objective contains a special tag, which is a key of the internal dictionary, append the related text (value of the dict.key)
                                string lineBreak = "\r\n\r\n";
                                string appendText = "";
                                foreach (string tag in simGameEventResult.AddedTags)
                                {
                                    if (Fields.FactionResponseTextAppendices.TryGetValue(tag, out appendText))
                                    {
                                        ___DescriptionText.AppendTextAndRefresh($"{lineBreak}{appendText}", new object[] { });
                                    }
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



    // Check if a contract has set special tags in CompanyTags during its course to trigger stuff according to them
    [HarmonyPatch(typeof(SimGameState), "ResolveCompleteContract")]
    public static class SimGameState_ResolveCompleteContract_Patch
    {
        // Needs to be prefix as SimGameState.CompletedContract is nulled in original method
        public static void Prefix(SimGameState __instance, SimGameInterruptManager ___interruptQueue)
        {
            try
            {
                Contract completedContract = __instance.CompletedContract;
                bool SimGameStateCompanyTagsHasAnyTrigger = __instance.CompanyTags.Any(tag => tag.Contains("triggerReward-"));

                Logger.Debug($"[SimGameState_ResolveCompleteContract_PREFIX] __instance.CompanyTags: {String.Join(", ", __instance.CompanyTags.ToArray())}");
                Logger.Debug($"[SimGameState_ResolveCompleteContract_PREFIX] SimGameStateCompanyTagsHasAnyTrigger: {SimGameStateCompanyTagsHasAnyTrigger}");

                if (completedContract.IsOpportunityMission() && SimGameStateCompanyTagsHasAnyTrigger)
                {
                    foreach (string tag in __instance.CompanyTags.ToArray())
                    {
                        if (tag.Contains("triggerReward-"))
                        {
                            Logger.Debug($"[SimGameState_ResolveCompleteContract_PREFIX] tag: {tag}");
                            __instance.SetTimeMoving(false, true);
                            string itemCollectionId = tag.Replace("triggerReward-", "");
                            Logger.Debug($"[SimGameState_ResolveCompleteContract_PREFIX] itemCollectionId: {itemCollectionId}");
                            ___interruptQueue.QueueRewardsPopup(itemCollectionId);

                            __instance.CompanyTags.Remove(tag);
                        }
                    }
                }

                Logger.Debug($"[SimGameState_ResolveCompleteContract_PREFIX] __instance.CompanyTags: {String.Join(", ", __instance.CompanyTags.ToArray())}");
            }
            catch (Exception e)
            {
                Logger.Error(e);
            }
        }
    }
}
