using System.Collections.Generic;
using Ultima5Redux.Data;
using Ultima5Redux.PlayerCharacters.Inventory;

namespace Ultima5Redux.PlayerCharacters.CombatItems
{
    public class CombatItemReference
    {
        public enum MissileType { None = -1, Arrow = 0, CannonBall, Axe, Red, Blue, Green, Violet, Rock }

        private readonly DataOvlReference _dataOvlReference;
        
        public virtual bool CanSell => BasePrice > 0;
        
        public int AttackStat { get; }
        public int DefendStat { get; }
        public int Range { get; }

        public int RequiredStrength { get; }
        public MissileType Missile { get; }
        public string EquipmentName { get; }
        public int BasePrice { get; set; } = 0;
        public int Sprite { get; }

        public readonly DataOvlReference.Equipment SpecificEquipment;

        public const int BareHandsIndex = -2;

        public bool IsAmmo => SpecificEquipment == DataOvlReference.Equipment.Quarrels ||
                              SpecificEquipment == DataOvlReference.Equipment.Arrows;

        public bool IsShield =>
            SpecificEquipment == DataOvlReference.Equipment.SmallShield ||
            SpecificEquipment == DataOvlReference.Equipment.LargeShield ||
            SpecificEquipment == DataOvlReference.Equipment.SpikedShield ||
            SpecificEquipment == DataOvlReference.Equipment.MagicShield ||
            SpecificEquipment == DataOvlReference.Equipment.JewelShield;

        public bool IsTwoHanded => SpecificEquipment == DataOvlReference.Equipment.TwoHAxe ||
                                   SpecificEquipment == DataOvlReference.Equipment.TwoHSword ||
                                   SpecificEquipment == DataOvlReference.Equipment.TwoHHammer ||
                                   SpecificEquipment == DataOvlReference.Equipment.Bow ||
                                   SpecificEquipment == DataOvlReference.Equipment.MagicBow ||
                                   SpecificEquipment == DataOvlReference.Equipment.Crossbow ||
                                   SpecificEquipment == DataOvlReference.Equipment.Halberd ||
                                   SpecificEquipment == DataOvlReference.Equipment.FlamingOil;

        public InventoryReference InventoryReference { get; }

        public CombatItemReference(DataOvlReference dataOvlReference, InventoryReference inventoryReference)
        {
            _dataOvlReference = dataOvlReference;

            DataOvlReference.Equipment specificEquipment = inventoryReference.GetAsEquipment();
            
            AttackStat = GetAttack(_dataOvlReference, (int)specificEquipment);
            DefendStat = GetDefense(_dataOvlReference, (int)specificEquipment);
            int nRange = GetRange(_dataOvlReference, (int)specificEquipment);
            Missile = GetMissileType(specificEquipment, nRange);
            Range = nRange == 0 ? 1 : nRange;
            EquipmentName = GetEquipmentString(dataOvlReference, (int)specificEquipment);
            RequiredStrength = GetRequiredStrength(dataOvlReference, (int)specificEquipment);
            SpecificEquipment = specificEquipment;
            Sprite = 1;
            InitializePrices();
        }
        private static string GetEquipmentString(DataOvlReference dataOvlRef, int nString)
        {
            if (nString == BareHandsIndex) return "Bare Hands";
            List<string> equipmentNames = dataOvlRef.GetDataChunk(DataOvlReference.DataChunkName.EQUIP_INDEXES)
                .GetAsStringListFromIndexes();
            return nString == 0xFF ? " " : equipmentNames[nString];
        }

        private void InitializePrices()
        {
            if (SpecificEquipment == DataOvlReference.Equipment.BareHands)
            {
                BasePrice = 0;
                return;
            }

            BasePrice = _dataOvlReference.GetDataChunk(DataOvlReference.DataChunkName.EQUIPMENT_BASE_PRICE)
                .GetChunkAsUint16List()[
                    (int)SpecificEquipment];
        }
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
        
    }
}