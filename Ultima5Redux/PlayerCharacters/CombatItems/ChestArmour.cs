using System.Collections.Generic;
using Ultima5Redux.Data;

namespace Ultima5Redux.PlayerCharacters.CombatItems
{
    public class ChestArmour : Armour
    {
        public enum ChestArmourEnum { ClothArmour = 0x223, LeatherArmour = 0x224, Ringmail = 0x225, ScaleMail = 0x226 , 
            ChainMail = 0x227, PlateMail = 0x228 , MysticArmour = 0x229 }

        public ChestArmourEnum ChestArmourType;

        private const int CHEST_ARMOUR_SPRITE = 267;

        //public override int AttackStat { get; }

        //public override int DefendStat { get; }

        public override bool HideQuantity => false;

        public ChestArmour(ChestArmourEnum chestArmourType, DataOvlReference.Equipment equipment, DataOvlReference dataOvlRef, List<byte> gameStateByteArray)
            : base(equipment, dataOvlRef, gameStateByteArray, (int)chestArmourType, CHEST_ARMOUR_SPRITE)
        {
            ChestArmourType = chestArmourType;
        }

        //public ChestArmour(ChestArmourEnum chestArmourType, int quantity, string longName, string shortName, int attackStat,
        //    int defendStat, DataOvlReference.EQUIPMENT specificEquipment) : 
        //    base(quantity, longName, shortName, CHEST_ARMOUR_SPRITE, attackStat, defendStat, specificEquipment)
        //{
        //    ChestArmourType = chestArmourType;
        //}
    }
}