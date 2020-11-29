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
        public readonly DataOvlReference.Equipment SpecificEquipment;

        protected CombatItem(DataOvlReference.Equipment specificEquipment, DataOvlReference dataOvlRef,
            int nQuantity, int nOffset, int nSpriteNum)
            : base(nQuantity, GetEquipmentString(dataOvlRef, (int) specificEquipment),
                GetEquipmentString(dataOvlRef, (int) specificEquipment), nSpriteNum)
        {
            AttackStat = GetAttack(dataOvlRef, (int) specificEquipment);
            DefendStat = GetDefense(dataOvlRef, (int) specificEquipment);
            RequiredStrength = GetRequiredStrength(dataOvlRef, (int) specificEquipment);
            SpecificEquipment = specificEquipment;
            InitializePrices(dataOvlRef);
        }

        public virtual bool CanSell => BasePrice > 0;

        public int RequiredStrength { get; }
        public int AttackStat { get; }
        public int DefendStat { get; }

        public static int GetAttack(DataOvlReference dataOvlRef, int nIndex)
        {
            List<byte> attackValueList =
                dataOvlRef.GetDataChunk(DataOvlReference.DataChunkName.ATTACK_VALUES).GetAsByteList();
            if (nIndex >= attackValueList.Count) return 0;
            return attackValueList[nIndex];
        }

        public static int GetDefense(DataOvlReference dataOvlRef, int nIndex)
        {
            List<byte> defenseValueList =
                dataOvlRef.GetDataChunk(DataOvlReference.DataChunkName.DEFENSE_VALUES).GetAsByteList();
            if (nIndex >= defenseValueList.Count) return 0;
            return defenseValueList[nIndex];
        }

        public static int GetRequiredStrength(DataOvlReference dataOvlRef, int nIndex)
        {
            List<byte> requiredStrengthValueList =
                dataOvlRef.GetDataChunk(DataOvlReference.DataChunkName.REQ_STRENGTH_EQUIP).GetAsByteList();
            if (nIndex >= requiredStrengthValueList.Count) return 0;
            return requiredStrengthValueList[nIndex];
        }

        public static string GetEquipmentString(DataOvlReference dataOvlRef, int nString)
        {
            List<string> equipmentNames = dataOvlRef.GetDataChunk(DataOvlReference.DataChunkName.EQUIP_INDEXES)
                .GetAsStringListFromIndexes();
            return nString == 0xFF ? " " : equipmentNames[nString];
        }

        private void InitializePrices(DataOvlReference dataOvlRef)
        {
            BasePrice = dataOvlRef.GetDataChunk(DataOvlReference.DataChunkName.EQUIPMENT_BASE_PRICE)
                .GetChunkAsUint16List()[
                    (int) SpecificEquipment];
        }

        public override int GetAdjustedBuyPrice(PlayerCharacterRecords records,
            SmallMapReferences.SingleMapReference.Location location)
        {
            if (!IsSellable) return 0;

            // we add 3% of the value per dex point below 33, and subtract 3% for each point above 33
            const int nBaseDex = 33;
            int nAdjustedPrice =
                (int) (BasePrice + BasePrice * 0.03f * (nBaseDex - records.AvatarRecord.Stats.Dexterity));
            return nAdjustedPrice <= 0 ? 1 : nAdjustedPrice;
        }

        public override int GetAdjustedSellPrice(PlayerCharacterRecords records,
            SmallMapReferences.SingleMapReference.Location location)
        {
            if (!IsSellable) return 0;

            // we subtract 3% of the value for every dexterity point below 33, and add 3% for each point above it
            const int nBaseDex = 33;
            int nAdjustedPrice =
                (int) (BasePrice - BasePrice * 0.03f * (nBaseDex - records.AvatarRecord.Stats.Dexterity));
            return nAdjustedPrice <= 0 ? 1 : nAdjustedPrice;
        }
    }
}