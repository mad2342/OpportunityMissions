using System;
using System.Collections;
using System.Collections.Generic;
using BattleTech;
using BattleTech.Framework;
using BattleTech.UI;
using BattleTech.UI.TMProWrapper;
using Harmony;
using HBS;
using OpportunityMissions.Extensions;
using UnityEngine;

namespace OpportunityMissions.Patches
{
    [HarmonyPatch(typeof(StarSystem), "ResetContracts")]
    public static class StarSystem_ResetContracts_Patch
    {
        public static void Postfix(StarSystem __instance)
        {
            try
            {
                Logger.Debug($"[StarSystem_ResetContracts_POSTFIX] Remove company tag for any active opportunity mission");

                __instance.Sim.CompanyTags.Remove("starsystem_opportunitymission_active");
            }
            catch (Exception e)
            {
                Logger.Error(e);
            }
        }
    }



    // Allow negotiations of more than 100% in total for procedural contracts with disabled negotiations
    [HarmonyPatch(typeof(Contract), "SetNegotiatedValues")]
    public static class Contract_SetNegotiatedValues_Patch
    {
        public static bool Prefix(Contract __instance, float cbill, float salvage)
        {
            try
            {
                if (__instance.IsOpportunityMission() && (cbill + salvage > 1.01f))
                {
                    Logger.Debug($"[Contract_SetNegotiatedValues_PREFIX] Allow negotiations > 1.01f for {__instance.Override.ID}");
                    Logger.Debug($"[Contract_SetNegotiatedValues_PREFIX] cbill: {cbill}");
                    Logger.Debug($"[Contract_SetNegotiatedValues_PREFIX] salvage: {salvage}");

                    new Traverse(__instance).Property("PercentageContractValue").SetValue(cbill);
                    new Traverse(__instance).Property("PercentageContractSalvage").SetValue(salvage);
                    new Traverse(__instance).Property("PercentageContractReputation").SetValue(0f);

                    return false;

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



    // List procedural contracts with disabled negotiations on top of contracts list
    [HarmonyPatch(typeof(SGContractsWidget), "GetContractComparePriority")]
    public static class SGContractsWidget_GetContractComparePriority_Patch
    {
        public static void Postfix(SGContractsWidget __instance, ref int __result, Contract contract)
        {
            try
            {
                if (contract.IsOpportunityMission())
                {
                    Logger.Info($"[SGContractsWidget_GetContractComparePriority_POSTFIX] Set {contract.Override.ID} to have highest sort order");

                    __result = -1;
                }
            }
            catch (Exception e)
            {
                Logger.Error(e);
            }
        }
    }



    // Indicate opportunity missions at briefing area
    [HarmonyPatch(typeof(SGContractsWidget), "PopulateContract")]
    public static class SGContractsWidget_PopulateContract_Patch
    {
        public static void Postfix(SGContractsWidget __instance, Contract contract, LocalizableText ___ContractLocationField, GameObject ___ContractLocation, GameObject ___ContractLocationArrow, GameObject ___TravelContractBGFill, GameObject ___TravelIcon, GameObject ___PriorityMissionStoryObject)
        {
            try
            {
                // Only for "normal" contracts with no negotiations
                if (contract.IsOpportunityMission())
                {
                    Logger.Debug($"[SGContractsWidget_PopulateContract_POSTFIX] {contract.Override.ID} is an opportunity mission");

                    StarSystem targetSystem = contract.GameContext.GetObject(GameContextObjectTagEnum.TargetStarSystem) as StarSystem;
                    ___ContractLocationField.SetText("Opportunity Mission at {0}", new object[] { targetSystem.Name });
                    ___ContractLocation.SetActive(true);
                    ___ContractLocationArrow.SetActive(false);
                    //___TravelContractBGFill.SetActive(false);
                    ___TravelIcon.SetActive(false);

                    __instance.ForceRefreshImmediate();
                }
            }
            catch (Exception e)
            {
                Logger.Error(e);
            }
        }
    }



    // Change display type of procedural contracts with disabled negotiations
    [HarmonyPatch(typeof(SGContractsWidget), "AddContract")]
    public static class SGContractsWidget_AddContract_Patch
    {
        static bool isOpportunityMission = false;

        public static void Prefix(SGContractsWidget __instance, Contract contract)
        {
            try
            {
                if (contract.IsOpportunityMission())
                {
                    isOpportunityMission = true;
                    Logger.Debug($"[SGContractsWidget_AddContract_PREFIX] {contract.Override.ID} is an opportunity mission");

                    // Temporarily set contractDisplayStyle
                    contract.Override.contractDisplayStyle = ContractDisplayStyle.BaseCampaignStory;
                }
            }
            catch (Exception e)
            {
                Logger.Error(e);
            }
        }

        public static void Postfix(SGContractsWidget __instance, Contract contract)
        {
            // Reset contractDisplayStyle
            if (isOpportunityMission)
            {
                contract.Override.contractDisplayStyle = ContractDisplayStyle.BaseCampaignNormal;
            }
        }
    }

    [HarmonyPatch(typeof(SGContractsWidget), "ClearContracts")]
    public static class SGContractsWidget_ClearContracts_Patch
    {
        static List<int> opportunityMissionIndexList = new List<int>();

        public static void Prefix(SGContractsWidget __instance, List<SGContractsListItem> ___listedContracts)
        {
            try
            {
                for (int i = 0; i < ___listedContracts.Count; i++)
                {
                    SGContractsListItem sgContractsListItem = ___listedContracts[i];

                    if (sgContractsListItem.Contract.IsOpportunityMission())
                    {
                        opportunityMissionIndexList.Add(i);
                        Logger.Debug($"[SGContractsWidget_AddContract_PREFIX] Contract at index {i} is an opportunity mission");

                        // Temporarily set contractDisplayStyle
                        sgContractsListItem.Contract.Override.contractDisplayStyle = ContractDisplayStyle.BaseCampaignStory;
                    }
                }
            }
            catch (Exception e)
            {
                Logger.Error(e);
            }
        }

        public static void Postfix(SGContractsWidget __instance, List<SGContractsListItem> ___listedContracts)
        {
            for (int i = 0; i < ___listedContracts.Count; i++)
            {
                SGContractsListItem sgContractsListItem = ___listedContracts[i];

                // Reset contractDisplayStyle
                if (opportunityMissionIndexList.Contains(i))
                {
                    sgContractsListItem.Contract.Override.contractDisplayStyle = ContractDisplayStyle.BaseCampaignNormal;
                }
            }
            opportunityMissionIndexList.Clear();
        }
    }



    // Create a result string for ActionType "System_AddContract"
    // @ToDo: Check if this interferes with the vanilla usage of this action (see campaign event and milestones)
    [HarmonyPatch(typeof(SimGameState), "BuildSimGameActionString")]
    public static class SimGameState_BuildSimGameActionString_Patch
    {
        public static void Postfix(SimGameState __instance, ref List<ResultDescriptionEntry> __result, SimGameResultAction[] actions, GameContext context, string prefix)
        {
            try
            {
                List<ResultDescriptionEntry> list = new List<ResultDescriptionEntry>();

                foreach (SimGameResultAction simGameResultAction in actions)
                {
                    if (simGameResultAction.Type == SimGameResultAction.ActionType.System_AddContract)
                    {
                        GameContext gameContext = new GameContext(context);
                        gameContext.SetObject(GameContextObjectTagEnum.ResultValue, simGameResultAction.value);
                        Color emphasisColor = LazySingletonBehavior<UIManager>.Instance.UIColorRefs.gold;

                        string result = $"Added <color=#{ColorUtility.ToHtmlStringRGBA(emphasisColor)}>{simGameResultAction.valueConstant}</color> to available Contracts";

                        __result.Add(new ResultDescriptionEntry(string.Format("{0} {1}{2}", prefix, result, Environment.NewLine), gameContext, ""));
                    }
                }
            }
            catch (Exception e)
            {
                Logger.Error(e);
            }
        }
    }



    [HarmonyPatch(typeof(SimGameState), "ApplyEventAction")]
    public static class SimGameState_ApplyEventAction_Patch
    {
        public static bool Prefix(ref bool __result, SimGameResultAction action, object additionalObject)
        {
            try
            {
                SimGameState simulation = UnityGameInstance.BattleTechGame.Simulation;

                if (simulation == null || action.Type != SimGameResultAction.ActionType.System_AddContract)
                {
                    return true;
                }

                // In vanilla the SimGameResultAction.ActionType.System_AddContract is only used for travel-contracts (breadcrumbs)
                // I use it to add custom contracts by custom events which need to have negotiations disabled as they are "self-employed opportunity contracts"
                SimGameState.AddContractData addContractData = simulation.ParseContractActionData(action.value, action.additionalValues);
                ContractOverride contractOverride = simulation.DataManager.ContractOverrides.Get(addContractData.ContractName).Copy();
                ContractTypeValue contractTypeValue = contractOverride.ContractTypeValue;

                // Call original method if contract is a breadcrumb (used by campaign a lot)
                if (contractTypeValue.IsTravelOnly)
                {
                    return true;
                }
                // Custom handling
                else
                {
                    // Validate target system and enrich contractData
                    StarSystem targetSystem;
                    Logger.Debug($"[SimGameState_ApplyEventAction_PREFIX] addContractData.TargetSystem: {addContractData.TargetSystem}");
                    if (!string.IsNullOrEmpty(addContractData.TargetSystem))
                    {
                        string validatedSystemString = simulation.GetValidatedSystemString(addContractData.TargetSystem);
                        addContractData.IsGlobal = validatedSystemString != simulation.CurSystem.ID;

                        if (simulation.GetSystemById(validatedSystemString) != null)
                        {
                            targetSystem = simulation.GetSystemById(validatedSystemString);
                        }
                        else
                        {
                            throw new Exception("Couldn't find StarSystem for: " + validatedSystemString);
                        }  
                    }
                    else
                    {
                        targetSystem = simulation.CurSystem;
                    }
                    Logger.Debug($"[SimGameState_ApplyEventAction_PREFIX] targetSystem.ID: {targetSystem.ID}");



                    // Internal helper method
                    IEnumerator InjectAdditionalContractRoutine()
                    {
                        while (!targetSystem.InitialContractsFetched)
                        {
                            yield return new WaitForSeconds(0.2f);
                        }

                        InjectAdditionalContract();

                        yield break;
                    }

                    // Internal helper method
                    void InjectAdditionalContract(bool refresh = true)
                    {
                        // Get the contract
                        Contract contract = simulation.GetValidContractForContractDataAndSystem(addContractData, targetSystem);
                        targetSystem.SystemContracts.Add(contract);

                        if (refresh)
                        {
                            SGContractsWidget ___contractsWidget = (SGContractsWidget)AccessTools.Field(typeof(SGRoomController_CmdCenter), "contractsWidget").GetValue(simulation.RoomManager.CmdCenterRoom);
                            List<Contract> allContracts = simulation.GetAllCurrentlySelectableContracts(true);
                            ContractDisplayStyle? ___contractDisplayAutoSelect = Traverse.Create(___contractsWidget).Field("contractDisplayAutoSelect").GetValue<ContractDisplayStyle?>();
                            ___contractsWidget.ListContracts(allContracts, ___contractDisplayAutoSelect);
                        }
                    }



                    if (simulation.TimeMoving)
                    {
                        simulation.PauseTimer();
                        simulation.StopPlayMode();
                    }



                    // Need to check this as the initial contract fetching routine clears all existing contracts
                    Logger.Debug($"[SimGameState_ApplyEventAction_PREFIX] targetSystem.InitialContractsFetched: {targetSystem.InitialContractsFetched}");
                    if (!targetSystem.InitialContractsFetched)
                    {
                        SceneSingletonBehavior<UnityGameInstance>.Instance.StartCoroutine(InjectAdditionalContractRoutine());
                        return false;
                    }
                    else 
                    {
                        InjectAdditionalContract(false);

                        // Skip original method
                        return false;
                    }
                }
            }
            catch (Exception e)
            {
                Logger.Error(e);
                return true;
            }
        }
    }
}
