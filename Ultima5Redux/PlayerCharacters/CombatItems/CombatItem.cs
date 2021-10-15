using System.Collections.Generic;
using Ultima5Redux.Data;
using Ultima5Redux.Maps;
using Ultima5Redux.PlayerCharacters.Inventory;

// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable UnusedAutoPropertyAccessor.Global

namespace Ultima5Redux.PlayerCharacters.CombatItems
{
    public abstract class CombatItem : InventoryItem
    {
        public enum MissileType { None = -1, Arrow = 0, CannonBall, Axe, Red, Blue, Green, Violet, Rock }

        public const int BareHandsIndex = -2;
        public readonly DataOvlReference.Equipment SpecificEquipment;

        protected CombatItem(DataOvlReference.Equipment specificEquipment, DataOvlReference dataOvlRef,
            int nQuantity, int nOffset, int nSpriteNum)
            : base(nQuantity, GetEquipmentString(dataOvlRef, (int)specificEquipment),
                GetEquipmentString(dataOvlRef, (int)specificEquipment), nSpriteNum)
        {
            AttackStat = GetAttack(dataOvlRef, (int)specificEquipment);
            DefendStat = GetDefense(dataOvlRef, (int)specificEquipment);
            int nRange = GetRange(dataOvlRef, (int)specificEquipment);
            Missile = GetMissileType(specificEquipment, nRange);
            Range = nRange == 0 ? 1 : nRange;
            EquipmentName = GetEquipmentString(dataOvlRef, (int)specificEquipment);
            RequiredStrength = GetRequiredStrength(dataOvlRef, (int)specificEquipment);
            SpecificEquipment = specificEquipment;
            InitializePrices(dataOvlRef);
        }

        public virtual bool CanSell => BasePrice > 0;
        public int AttackStat { get; }
        public int DefendStat { get; }
        public int Range { get; }

        public int RequiredStrength { get; }
        public MissileType Missile { get; }
        public string EquipmentName { get; }
        public abstract PlayerCharacterRecord.CharacterEquipped.EquippableSlot EquippableSlot { get; }
        
        public override string InventoryReferenceString => SpecificEquipment.ToString();

        private static int GetAttack(DataOvlReference dataOvlRef, int nIndex)
        {
            if (nIndex == BareHandsIndex) return 3;

            List<byte> attackValueList =
                dataOvlRef.GetDataChunk(DataOvlReference.DataChunkName.ATTACK_VALUES).GetAsByteList();
            return nIndex >= attackValueList.Count ? 0 : attackValueList[nIndex];
        }

        private static int GetRange(DataOvlReference dataOvlRef, int nIndex)
        {
            if (nIndex == BareHandsIndex) return 1;

            List<byte> rangeValueList =
                dataOvlRef.GetDataChunk(DataOvlReference.DataChunkName.ATTACK_RANGE_VALUES).GetAsByteList();
            if (nIndex >= rangeValueList.Count) return 1;
            return rangeValueList[nIndex];
        }

        private static int GetDefense(DataOvlReference dataOvlRef, int nIndex)
        {
            if (nIndex == BareHandsIndex) return 0;

            List<byte> defenseValueList =
                dataOvlRef.GetDataChunk(DataOvlReference.DataChunkName.DEFENSE_VALUES).GetAsByteList();
            if (nIndex >= defenseValueList.Count) return 0;
            return defenseValueList[nIndex];
        }

        private static MissileType GetMissileType(DataOvlReference.Equipment equipment, int nRange)
        {
            switch (equipment)
            {
                case DataOvlReference.Equipment.Sling:
                    return MissileType.Rock;
                case DataOvlReference.Equipment.FlamingOil:
                    return MissileType.Red;
                case DataOvlReference.Equipment.ThrowingAxe:
                case DataOvlReference.Equipment.MagicAxe:
                    return MissileType.Axe;
            }

            return nRange == 0 ? MissileType.None : MissileType.Arrow;
        }

        private static int GetRequiredStrength(DataOvlReference dataOvlRef, int nIndex)
        {
            if (nIndex == BareHandsIndex) return 0;

            List<byte> requiredStrengthValueList =
                dataOvlRef.GetDataChunk(DataOvlReference.DataChunkName.REQ_STRENGTH_EQUIP).GetAsByteList();
            if (nIndex >= requiredStrengthValueList.Count) return 0;
            return requiredStrengthValueList[nIndex];
        }

        private static string GetEquipmentString(DataOvlReference dataOvlRef, int nString)
        {
            if (nString == BareHandsIndex) return "Bare Hands";
            List<string> equipmentNames = dataOvlRef.GetDataChunk(DataOvlReference.DataChunkName.EQUIP_INDEXES)
                .GetAsStringListFromIndexes();
            return nString == 0xFF ? " " : equipmentNames[nString];
        }

        private void InitializePrices(DataOvlReference dataOvlRef)
        {
            if (SpecificEquipment == DataOvlReference.Equipment.BareHands)
            {
                BasePrice = 0;
                return;
            }

            BasePrice = dataOvlRef.GetDataChunk(DataOvlReference.DataChunkName.EQUIPMENT_BASE_PRICE)
                .GetChunkAsUint16List()[
                    (int)SpecificEquipment];
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