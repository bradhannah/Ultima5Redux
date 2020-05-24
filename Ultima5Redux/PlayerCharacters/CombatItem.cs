using System.Collections.Generic;
using Ultima5Redux.Data;
// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable UnusedAutoPropertyAccessor.Global

namespace Ultima5Redux.PlayerCharacters
{
    public abstract class CombatItem : InventoryItem
    {
        public readonly DataOvlReference.Equipment SpecificEquipment;

        public static int GetAttack(DataOvlReference dataOvlRef, int nIndex)
        {
            List<byte> attackValueList = dataOvlRef.GetDataChunk(DataOvlReference.DataChunkName.ATTACK_VALUES).GetAsByteList();
            if (nIndex >= attackValueList.Count)
            {
                return 0;
            }
            return attackValueList[nIndex];
        }
        public static int GetDefense(DataOvlReference dataOvlRef, int nIndex)
        {
            List<byte> defenseValueList = dataOvlRef.GetDataChunk(DataOvlReference.DataChunkName.DEFENSE_VALUES).GetAsByteList();
            if (nIndex >= defenseValueList.Count)
            {
                return 0;
            }
            return defenseValueList[nIndex];
        }

        public static int GetRequiredStrength(DataOvlReference dataOvlRef, int nIndex)
        {
            List<byte> requiredStrengthValueList = dataOvlRef.GetDataChunk(DataOvlReference.DataChunkName.REQ_STRENGTH_EQUIP).GetAsByteList();
            if (nIndex >= requiredStrengthValueList.Count)
            {
                return 0;
            }
            return requiredStrengthValueList[nIndex];
        }

        public static string GetEquipmentString(DataOvlReference dataOvlRef, int nString)
        {
            List<string> equipmentNames = dataOvlRef.GetDataChunk(DataOvlReference.DataChunkName.EQUIP_INDEXES).GetAsStringListFromIndexes();
            return nString == 0xFF ? " " : equipmentNames[nString];
        }

        public int RequiredStrength { get; }
        public int AttackStat { get; }
        public int DefendStat { get; }
        //public CombatItem(int quantity, string longName, string shortName, int nSpriteNum, int attackStat, int defendStat, DataOvlReference.EQUIPMENT specificEquipment)
        //    : base(quantity, longName, shortName, nSpriteNum)
        //{
        //    attackStat = AttackStat;
        //    defendStat = DefendStat;
        //    this.SpecificEquipment = specificEquipment;
        //}
        //    ChestArmours.Add(new ChestArmour(chestArmour, gameStateByteArray[(int)chestArmour],
        //equipmentNames[(int)equipment], equipmentNames[(int)equipment],
        //       CombatItem.GetAttack(dataOvlRef, (int) equipment),
        //       CombatItem.GetDefense(dataOvlRef, (int) equipment),equipment));
        
        // CombatItem.GetAttack(dataOvlRef, (int)specificEquipment), CombatItem.GetDefense(dataOvlRef, (int)specificEquipment)
        protected CombatItem(DataOvlReference.Equipment specificEquipment, DataOvlReference dataOvlRef, IReadOnlyList<byte> gameStateRef, int nOffset, int nSpriteNum) 
            : base (gameStateRef[nOffset], CombatItem.GetEquipmentString(dataOvlRef, (int)specificEquipment), 
                CombatItem.GetEquipmentString(dataOvlRef, (int)specificEquipment), nSpriteNum)
        {
            AttackStat = GetAttack(dataOvlRef, (int)specificEquipment);
            DefendStat = GetDefense(dataOvlRef, (int)specificEquipment);
            RequiredStrength = GetRequiredStrength(dataOvlRef, (int)specificEquipment);
            this.SpecificEquipment = specificEquipment;
            InitializePrices(dataOvlRef);
        }
        
        private void InitializePrices(DataOvlReference dataOvlRef)
        {
            BasePrice = dataOvlRef.GetDataChunk(DataOvlReference.DataChunkName.EQUIPMENT_BASE_PRICE).GetChunkAsUint16List()[
                (int) SpecificEquipment];
        }

    }
}