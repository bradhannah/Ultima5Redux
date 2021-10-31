using System.Collections.Generic;
using System.Runtime.Serialization;
using Ultima5Redux.Data;
using Ultima5Redux.Maps;
using Ultima5Redux.PlayerCharacters.Inventory;

// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable UnusedAutoPropertyAccessor.Global

namespace Ultima5Redux.PlayerCharacters.CombatItems
{
    [DataContract]
    public abstract class CombatItem : InventoryItem
    {
        public CombatItemReference TheCombatItemReference { get; }

        public DataOvlReference.Equipment SpecificEquipment => TheCombatItemReference.SpecificEquipment;
        
        // protected CombatItem(DataOvlReference.Equipment specificEquipment, DataOvlReference dataOvlRef,
        //     int nQuantity, int nOffset, int nSpriteNum)
        //     : base(nQuantity, GetEquipmentString(dataOvlRef, (int)specificEquipment),
        //         GetEquipmentString(dataOvlRef, (int)specificEquipment), nSpriteNum)
        // {
        //     DataOvlRef = dataOvlRef;
        //     InitializePrices(dataOvlRef);
        // }

        public CombatItem(CombatItemReference theCombatItemReference, int nQuantity) 
        : base(nQuantity, theCombatItemReference.EquipmentName, theCombatItemReference.EquipmentName, 
            theCombatItemReference.Sprite)
        {
            TheCombatItemReference = theCombatItemReference;
        }

        public abstract PlayerCharacterRecord.CharacterEquipped.EquippableSlot EquippableSlot { get; }
        
        public override string InventoryReferenceString => TheCombatItemReference.SpecificEquipment.ToString();

        public override int GetAdjustedBuyPrice(PlayerCharacterRecords records,
            SmallMapReferences.SingleMapReference.Location location)
        {
            if (!IsSellable) return 0;

            // we add 3% of the value per dex point below 33, and subtract 3% for each point above 33
            const int nBaseDex = 33;
            int nAdjustedPrice =
                (int)(BasePrice + BasePrice * 0.03f * (nBaseDex - records.AvatarRecord.Stats.Dexterity));
            return nAdjustedPrice <= 0 ? 1 : nAdjustedPrice;
        }

        public override int GetAdjustedSellPrice(PlayerCharacterRecords records,
            SmallMapReferences.SingleMapReference.Location location)
        {
            if (!IsSellable) return 0;

            // we subtract 3% of the value for every dexterity point below 33, and add 3% for each point above it
            const int nBaseDex = 33;
            int nAdjustedPrice =
                (int)(BasePrice - BasePrice * 0.03f * (nBaseDex - records.AvatarRecord.Stats.Dexterity));
            return nAdjustedPrice <= 0 ? 1 : nAdjustedPrice;
        }
    }
}