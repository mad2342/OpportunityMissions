using BattleTech;
using BattleTech.Framework;

namespace OpportunityMissions.Extensions
{
    internal static class ContractExtensions
    {
        public static bool IsOpportunityMission (this Contract contract)
        {
            //Logger.Info($"[ContractExtensions_IsOpportunityMission] contract: {contract.Name}");
            //Logger.Info($"[ContractExtensions_IsOpportunityMission] contract.Override.contractDisplayStyle: {contract.Override.contractDisplayStyle}");
            //Logger.Info($"[ContractExtensions_IsOpportunityMission] contract.CanNegotiate: {contract.CanNegotiate}");

            return contract.Override.contractDisplayStyle == ContractDisplayStyle.BaseCampaignNormal && !contract.CanNegotiate;
        }
    }
}
