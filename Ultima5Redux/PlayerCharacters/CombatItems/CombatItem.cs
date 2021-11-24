using System.Runtime.Serialization;
using Newtonsoft.Json;
using Ultima5Redux.PlayerCharacters.Inventory;
using Ultima5Redux.References;
using Ultima5Redux.References.Maps;
using Ultima5Redux.References.PlayerCharacters.Inventory;

// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable UnusedAutoPropertyAccessor.Global

namespace Ultima5Redux.PlayerCharacters.CombatItems
{
    [DataContract] public abstract class CombatItem : InventoryItem
    {
        [IgnoreDataMember] public override string FindDescription => InvRef.FriendlyItemName;
        
        [IgnoreDataMember] public override int BasePrice => TheCombatItemReference.BasePrice;

        [IgnoreDataMember] public override string InventoryReferenceString =>
            TheCombatItemReference.SpecificEquipment.ToString();

        [IgnoreDataMember] public override string LongName => TheCombatItemReference.EquipmentName;

        [IgnoreDataMember] public abstract CharacterEquipped.EquippableSlot EquippableSlot { get; }

        [DataMember] public DataOvlReference.Equipment SpecificEquipment { get; private set; }

        [IgnoreDataMember] public CombatItemReference TheCombatItemReference
        {
            get => GameReferences.CombatItemRefs.GetCombatItemReferenceFromEquipment(SpecificEquipment);
            private set => SpecificEquipment = value.SpecificEquipment;
        }

        [JsonConstructor] protected CombatItem()
        {
        }

        protected CombatItem(CombatItemReference theCombatItemReference, int nQuantity) : base(nQuantity,
            theCombatItemReference.Sprite)
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