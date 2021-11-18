using System.Runtime.Serialization;
using Ultima5Redux.Data;
using Ultima5Redux.Maps;
using Ultima5Redux.PlayerCharacters.Inventory;

// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable UnusedAutoPropertyAccessor.Global

namespace Ultima5Redux.PlayerCharacters.CombatItems
{
    [DataContract] public abstract class CombatItem : InventoryItem
    {
        public override int BasePrice => TheCombatItemReference.BasePrice;

        public override string InventoryReferenceString => TheCombatItemReference.SpecificEquipment.ToString();

        public override string LongName => TheCombatItemReference.EquipmentName;

        public abstract CharacterEquipped.EquippableSlot EquippableSlot { get; }

        public DataOvlReference.Equipment SpecificEquipment => TheCombatItemReference.SpecificEquipment;
        public CombatItemReference TheCombatItemReference { get; }


        public CombatItem(CombatItemReference theCombatItemReference, int nQuantity)
            : base(nQuantity, theCombatItemReference.Sprite)
        {
            TheCombatItemReference = theCombatItemReference;
        }

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