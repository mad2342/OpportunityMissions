using BattleTech;

namespace OpportunityMissions
{
    class Utilities
    {
        public static int GetMechPartCountForSalvage(MechDef m, Pilot p)
        {
            if (m.IsLocationDestroyed(ChassisLocations.CenterTorso))
            {
                return 1;
            }
            if (m.IsLocationDestroyed(ChassisLocations.LeftLeg) && m.IsLocationDestroyed(ChassisLocations.RightLeg))
            {
                return 2;
            }
            if (p.IsIncapacitated || m.IsLocationDestroyed(ChassisLocations.Head))
            {
                return 3;
            }
            return 0;
        }



        public static SalvageDef CreateMechPart(SimGameConstants sc, Contract c, MechDef m)
        {
            SalvageDef salvageDef = new SalvageDef();
            salvageDef.Type = SalvageDef.SalvageType.MECH_PART;
            salvageDef.ComponentType = ComponentType.MechPart;
            salvageDef.Count = 1;
            salvageDef.Weight = sc.Salvage.DefaultMechPartWeight;
            DescriptionDef description = m.Description;
            DescriptionDef description2 = new DescriptionDef(description.Id, string.Format("{0} {1}", description.Name, sc.Story.DefaultMechPartName), description.Details, description.Icon, description.Cost, description.Rarity, description.Purchasable, description.Manufacturer, description.Model, description.UIName);
            salvageDef.Description = description2;
            salvageDef.RewardID = c.GenerateRewardUID();
            return salvageDef;
        }



        public static SalvageDef CreateComponent(SimGameConstants sc, Contract c, MechComponentDef mc)
        {
            SalvageDef salvageDef = new SalvageDef();

            if (mc.ComponentType == ComponentType.Weapon)
            {
                WeaponDef weaponDef = mc as WeaponDef;
                salvageDef.MechComponentDef = weaponDef;

                salvageDef.Description = new DescriptionDef(weaponDef.Description);
                salvageDef.RewardID = c.GenerateRewardUID();
                salvageDef.Type = SalvageDef.SalvageType.COMPONENT;
                salvageDef.ComponentType = weaponDef.ComponentType;
                salvageDef.Damaged = false;
                salvageDef.Weight = sc.Salvage.DefaultWeaponWeight;
                salvageDef.Count = 1;

                return salvageDef;
            }
            else
            {
                salvageDef.MechComponentDef = mc;

                salvageDef.Description = new DescriptionDef(mc.Description);
                salvageDef.RewardID = c.GenerateRewardUID();
                salvageDef.Type = SalvageDef.SalvageType.COMPONENT;
                salvageDef.ComponentType = mc.ComponentType;
                salvageDef.Damaged = false;
                salvageDef.Weight = sc.Salvage.DefaultComponentWeight;
                salvageDef.Count = 1;

                return salvageDef;
            }
        }
    }
}
