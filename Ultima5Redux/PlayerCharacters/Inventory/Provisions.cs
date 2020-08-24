using System.Collections.Generic;
using Ultima5Redux.Data;

namespace Ultima5Redux.PlayerCharacters.Inventory
{
    public class Provisions : InventoryItems<Provision.ProvisionTypeEnum, Provision>
    {
        public Provisions(DataOvlReference dataOvlRef, GameState state) : base(dataOvlRef, null)
        {
            Items.Add(Provision.ProvisionTypeEnum.Torches, 
                new Provision(Provision.ProvisionTypeEnum.Torches, 
                    DataOvlRef.StringReferences.GetString(DataOvlReference.GetThingsStrings.S_TORCH).Trim(), 
                    DataOvlRef.StringReferences.GetString(DataOvlReference.GetThingsStrings.S_TORCH).Trim(), 
                    DataOvlRef.StringReferences.GetString(DataOvlReference.ThingsIFindStrings.SOME_TORCHES_BANG_N).Trim(), 
        (int)Provision.ProvisionSpritesTypeEnum.Torches, dataOvlRef, state));
            Items.Add(Provision.ProvisionTypeEnum.Gems, 
                new Provision(Provision.ProvisionTypeEnum.Gems, 
                    DataOvlRef.StringReferences.GetString(DataOvlReference.GetThingsStrings.S_GEM).Trim(), 
                    DataOvlRef.StringReferences.GetString(DataOvlReference.GetThingsStrings.S_GEM).Trim(), 
                    DataOvlRef.StringReferences.GetString(DataOvlReference.ThingsIFindStrings.A_GEM_BANG_N).Trim(), 
                    (int)Provision.ProvisionSpritesTypeEnum.Gems, dataOvlRef, state));
            Items.Add(Provision.ProvisionTypeEnum.Keys, 
                new Provision(Provision.ProvisionTypeEnum.Keys, 
                    DataOvlRef.StringReferences.GetString(DataOvlReference.GetThingsStrings.S_KEY).Trim(), 
                    DataOvlRef.StringReferences.GetString(DataOvlReference.GetThingsStrings.S_KEY).Trim(), 
                    DataOvlRef.StringReferences.GetString(DataOvlReference.ThingsIFindStrings.A_RING_OF_KEYS_BANG_N).Trim(), 
                    (int)Provision.ProvisionSpritesTypeEnum.Keys, dataOvlRef, state));
            Items.Add(Provision.ProvisionTypeEnum.SkullKeys, 
                new Provision(Provision.ProvisionTypeEnum.SkullKeys, 
                    DataOvlRef.StringReferences.GetString(DataOvlReference.SpecialItemNamesStrings.SKULL_KEYS).Trim(), 
                    DataOvlRef.StringReferences.GetString(DataOvlReference.SpecialItemNamesStrings.SKULL_KEYS).Trim(), 
                    DataOvlRef.StringReferences.GetString(DataOvlReference.GetThingsStrings.S_ODD_KEY).Trim(), 
                    (int)Provision.ProvisionSpritesTypeEnum.Torches, dataOvlRef, state));
        }

        public sealed override Dictionary<Provision.ProvisionTypeEnum, Provision> Items { get; } = 
            new Dictionary<Provision.ProvisionTypeEnum, Provision>();
    }
}