﻿using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.Serialization;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Ultima5Redux.References.PlayerCharacters.Inventory
{
    [SuppressMessage("ReSharper", "UnusedAutoPropertyAccessor.Global")] [DataContract]
    public class CombatItemReference
    {
        [JsonConverter(typeof(StringEnumConverter))] public enum MissileType
        {
            None = -1, Arrow = 0, CannonBall, Axe, Red, Blue, Green, Violet, Rock
        }

        [IgnoreDataMember] public const int BARE_HANDS_INDEX = -2;

        [IgnoreDataMember] private readonly DataOvlReference _dataOvlReference;

        [DataMember] public DataOvlReference.Equipment SpecificEquipment { get; private set; }

        [IgnoreDataMember] public virtual bool CanSell => BasePrice > 0;

        [DataMember] public int AttackStat { get; private set; }
        [DataMember] public int BasePrice { get; private set; }
        [DataMember] public int DefendStat { get; private set; }
        [DataMember] public string EquipmentName { get; private set; }

        [IgnoreDataMember] public bool IsAmmo => SpecificEquipment == DataOvlReference.Equipment.Quarrels ||
                                                 SpecificEquipment == DataOvlReference.Equipment.Arrows;

        [IgnoreDataMember] public bool IsShield =>
            SpecificEquipment == DataOvlReference.Equipment.SmallShield ||
            SpecificEquipment == DataOvlReference.Equipment.LargeShield ||
            SpecificEquipment == DataOvlReference.Equipment.SpikedShield ||
            SpecificEquipment == DataOvlReference.Equipment.MagicShield ||
            SpecificEquipment == DataOvlReference.Equipment.JewelShield;

        [IgnoreDataMember] 
        public bool IsTwoHanded => SpecificEquipment == DataOvlReference.Equipment.TwoHAxe ||
                                   SpecificEquipment == DataOvlReference.Equipment.TwoHSword ||
                                   SpecificEquipment == DataOvlReference.Equipment.TwoHHammer ||
                                   SpecificEquipment == DataOvlReference.Equipment.Bow ||
                                   SpecificEquipment == DataOvlReference.Equipment.MagicBow ||
                                   SpecificEquipment == DataOvlReference.Equipment.Crossbow ||
                                   SpecificEquipment == DataOvlReference.Equipment.Halberd ||
                                   SpecificEquipment == DataOvlReference.Equipment.FlamingOil;

        [DataMember] public MissileType Missile { get; private set; }
        [DataMember] public int Range { get; private set; }

        [DataMember] public int RequiredStrength { get; private set; }
        [DataMember] public int Sprite { get; private set; }

        [JsonConstructor] protected CombatItemReference()
        {
        }

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

        private static int GetAttack(DataOvlReference dataOvlRef, int nIndex)
        {
            if (nIndex == BARE_HANDS_INDEX) return 3;

            List<byte> attackValueList =
                dataOvlRef.GetDataChunk(DataOvlReference.DataChunkName.ATTACK_VALUES).GetAsByteList();
            return nIndex >= attackValueList.Count ? 0 : attackValueList[nIndex];
        }

        private static int GetDefense(DataOvlReference dataOvlRef, int nIndex)
        {
            if (nIndex == BARE_HANDS_INDEX) return 0;

            List<byte> defenseValueList =
                dataOvlRef.GetDataChunk(DataOvlReference.DataChunkName.DEFENSE_VALUES).GetAsByteList();
            if (nIndex >= defenseValueList.Count) return 0;
            return defenseValueList[nIndex];
        }

        private static string GetEquipmentString(DataOvlReference dataOvlRef, int nString)
        {
            if (nString == BARE_HANDS_INDEX) return "Bare Hands";
            List<string> equipmentNames = dataOvlRef.GetDataChunk(DataOvlReference.DataChunkName.EQUIP_INDEXES)
                .GetAsStringListFromIndexes();
            return nString == 0xFF ? " " : equipmentNames[nString];
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

        private static int GetRange(DataOvlReference dataOvlRef, int nIndex)
        {
            if (nIndex == BARE_HANDS_INDEX) return 1;

            List<byte> rangeValueList = dataOvlRef.GetDataChunk(DataOvlReference.DataChunkName.ATTACK_RANGE_VALUES)
                .GetAsByteList();
            if (nIndex >= rangeValueList.Count) return 1;
            return rangeValueList[nIndex];
        }

        private static int GetRequiredStrength(DataOvlReference dataOvlRef, int nIndex)
        {
            if (nIndex == BARE_HANDS_INDEX) return 0;

            List<byte> requiredStrengthValueList =
                dataOvlRef.GetDataChunk(DataOvlReference.DataChunkName.REQ_STRENGTH_EQUIP).GetAsByteList();
            if (nIndex >= requiredStrengthValueList.Count) return 0;
            return requiredStrengthValueList[nIndex];
        }

        private void InitializePrices()
        {
            if (SpecificEquipment == DataOvlReference.Equipment.BareHands)
            {
                BasePrice = 0;
                return;
            }

            BasePrice = _dataOvlReference.GetDataChunk(DataOvlReference.DataChunkName.EQUIPMENT_BASE_PRICE)
                .GetChunkAsUint16List()[(int)SpecificEquipment];
        }
    }
}