using System;
using System.Collections.Generic;
using System.Linq;
using BattleTech;
using BattleTech.Data;
using BattleTech.Framework;

namespace OpportunityMissions.Extensions
{
    internal static class SimGameStateExtensions
    {
        public static Contract GetValidContractForContractDataAndSystem (this SimGameState simGameState, SimGameState.AddContractData addContractData, StarSystem starSystem)
        {
            Logger.Debug($"[SimGameStateExtensions_GetValidContractForContractDataAndSystem] (addContractData) ContractName: {addContractData.ContractName}, Target: {addContractData.Target}, Employer: {addContractData.Employer}, TargetSystem: {addContractData.TargetSystem}, TargetAlly: {addContractData.TargetAlly}");
            Logger.Debug($"[SimGameStateExtensions_GetValidContractForContractDataAndSystem] starSystem: {starSystem.Name}");
            Logger.Debug($"[SimGameStateExtensions_GetValidContractForContractDataAndSystem] starSystem.Def.SupportedBiomes: {String.Join(", ", starSystem.Def.SupportedBiomes.Select(sb => sb.ToString()))}");

            if (starSystem == null)
            {
                throw new Exception("starSystem is null");
            }

            GameInstance gameInstance = simGameState.BattleTechGame;
            DataManager dataManager = simGameState.BattleTechGame.DataManager;
            GameContext gameContext = new GameContext(simGameState.Context);
            gameContext.SetObject(GameContextObjectTagEnum.TargetStarSystem, starSystem);

            ContractOverride contractOverride = dataManager.ContractOverrides.Get(addContractData.ContractName).Copy();
            ContractTypeValue contractTypeValue = contractOverride.ContractTypeValue;

            FactionValue employer = string.IsNullOrEmpty(addContractData.Employer) ? FactionEnumeration.GetInvalidUnsetFactionValue() : FactionEnumeration.GetFactionByName(addContractData.Employer);
            FactionValue employersAlly = string.IsNullOrEmpty(addContractData.EmployerAlly) ? FactionEnumeration.GetInvalidUnsetFactionValue() : FactionEnumeration.GetFactionByName(addContractData.EmployerAlly);
            FactionValue target = string.IsNullOrEmpty(addContractData.Target) ? FactionEnumeration.GetInvalidUnsetFactionValue() : FactionEnumeration.GetFactionByName(addContractData.Target);
            FactionValue targetsAlly = string.IsNullOrEmpty(addContractData.TargetAlly) ? FactionEnumeration.GetInvalidUnsetFactionValue() : FactionEnumeration.GetFactionByName(addContractData.TargetAlly);
            FactionValue neutralToAll = string.IsNullOrEmpty(addContractData.NeutralToAll) ? FactionEnumeration.GetInvalidUnsetFactionValue() : FactionEnumeration.GetFactionByName(addContractData.NeutralToAll);
            FactionValue hostileToAll = string.IsNullOrEmpty(addContractData.HostileToAll) ? FactionEnumeration.GetInvalidUnsetFactionValue() : FactionEnumeration.GetFactionByName(addContractData.HostileToAll);

            if (employer.IsInvalidUnset || target.IsInvalidUnset)
            {
                throw new Exception("ContractData didn't have a valid employerValue and/or targetValue");
            }


            // Get all valid encounters (all contract types) for the requested system
            List<MapAndEncounters> allValidMapAndEncountersForCurrentSystem = MetadataDatabase.Instance.GetReleasedMapsAndEncountersBySinglePlayerProceduralContractTypeAndTags(starSystem.Def.MapRequiredTags, starSystem.Def.MapExcludedTags, starSystem.Def.SupportedBiomes, true);

            // Remove all items that dont have an encounter layer for the requested contract type
            allValidMapAndEncountersForCurrentSystem.RemoveAll(MapAndEncounters => MapAndEncounters.Encounters.All(el => el.ContractTypeValue != contractTypeValue));



            // Debug
            foreach (MapAndEncounters mae in allValidMapAndEncountersForCurrentSystem)
            {
                Logger.Info($"allValidMapAndEncountersForCurrentSystem (Map): {mae.Map.FriendlyName}");
                Logger.Info($"allValidMapAndEncountersForCurrentSystem (Encounters): {String.Join(", ", Array.ConvertAll(mae.Encounters, item => item.FriendlyName))}");
                Logger.Info($"-");
            }

            // @ToDo: Remove Map "mapGeneral_taigaRiver_iTnd" for ContractTypeValue "Rescue" (Broken in Vanilla, no buildings at target zones)
            //if (contractTypeValue.FriendlyName == "Rescue")
            //{
            //    allValidMapAndEncountersForCurrentSystem.RemoveAll(MapAndEncounters => MapAndEncounters.Map.MapID == "mapGeneral_taigaRiver_iTnd");
            //}



            // Get a random item
            Random random = new Random();
            MapAndEncounters mapAndEncounters = allValidMapAndEncountersForCurrentSystem.OrderBy(MapAndEncounters => random.NextDouble()).First();



            // Debug
            Logger.Info($"chosenMapAndEncounters (Map): {mapAndEncounters.Map.FriendlyName}");
            Logger.Info($"chosenMapAndEncounters: {String.Join(", ", Array.ConvertAll(mapAndEncounters.Encounters, item => item.FriendlyName))}");



            // Get the matching encounter layers
            List<EncounterLayer_MDD> encounterLayers = new List<EncounterLayer_MDD>();
            foreach (EncounterLayer_MDD encounterLayer in mapAndEncounters.Encounters)
            {
                if (encounterLayer.ContractTypeRow.ContractTypeID == (long)contractTypeValue.ID)
                {
                    encounterLayers.Add(encounterLayer);
                }
            }
            if (encounterLayers.Count <= 0)
            {
                throw new Exception("Map does not contain any encounters of type: " + contractTypeValue.Name);
            }



            // Debug
            foreach (EncounterLayer_MDD elMdd in encounterLayers)
            {
                Logger.Info($"chosenEncounter (Name): {elMdd.FriendlyName}");
                Logger.Info($"chosenEncounter (ContractTypeValue): {elMdd.ContractTypeValue}");
            }



            // Get GUID of a random valid encounter layer
            string encounterLayerGUID = encounterLayers[simGameState.NetworkRandom.Int(0, encounterLayers.Count)].EncounterLayerGUID;


            // Check if we need to travel
            if(addContractData.IsGlobal)
            {
                Logger.Debug($"[SimGameStateExtensions_GetValidContractForContractDataAndSystem] Custom handling of travel contracts isn't implemented. Falling back to default handling");

                // Fallback to relevant part of default vanilla method
                simGameState.AddContract(addContractData);
            
                //Contract travelContract = simGameState.CreateTravelContract(mapAndEncounters.Map.MapName, mapAndEncounters.Map.MapPath, encounterLayerGUID, contractTypeValue, contractOverride, gameContext, employer, employersAlly, target, targetsAlly, neutralToAll, hostileToAll, addContractData.IsGlobal);
                //simGameState.PrepContract(travelContract, employer, employersAlly, target, targetsAlly, neutralToAll, hostileToAll, mapAndEncounters.Map.BiomeSkinEntry.BiomeSkin, travelContract.Override.travelSeed, starSystem);
                //simGameState.GlobalContracts.Add(travelContract);

                //return travelContract;
            }

            Contract contract = new Contract(mapAndEncounters.Map.MapName, mapAndEncounters.Map.MapPath, encounterLayerGUID, contractTypeValue, gameInstance, contractOverride, gameContext, true, contractOverride.difficulty, 0, null);
            Logger.Debug($"[SimGameStateExtensions_GetValidContractForContractDataAndSystem] Contract: {contract.Override.ID}({contractTypeValue}), Difficulty: {contract.Difficulty}, CanNegotiate: {contract.CanNegotiate}, Biome: {mapAndEncounters.Map.BiomeSkinEntry.Name}, Map: {mapAndEncounters.Map.MapName}");

            simGameState.PrepContract(contract, employer, employersAlly, target, targetsAlly, neutralToAll, hostileToAll, mapAndEncounters.Map.BiomeSkinEntry.BiomeSkin, 0, starSystem);
            // Set negotiations
            if(!contract.CanNegotiate)
            {
                contract.SetNegotiatedValues(contract.Override.negotiatedSalary, contract.Override.negotiatedSalvage);
            }
            


            return contract;
        }
    }
}
