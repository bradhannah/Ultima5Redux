namespace Ultima5Redux.PlayerCharacters.CombatItems
{
    public class ChestArmour : Armour
    {
        public override CharacterEquipped.EquippableSlot EquippableSlot { get; } =
            CharacterEquipped.EquippableSlot.Armour;
        // public ChestArmour(ChestArmourEnum chestArmourType, DataOvlReference.Equipment equipment,
        //     DataOvlReference dataOvlRef, int nQuantity)
        //     : base(equipment, dataOvlRef, nQuantity, (int)chestArmourType, CHEST_ARMOUR_SPRITE)
        // {
        //     ChestArmourType = chestArmourType;
        // }

        //public override int AttackStat { get; }

        //public override int DefendStat { get; }

        public override bool HideQuantity => false;

        //private const int CHEST_ARMOUR_SPRITE = 267;

        //public ChestArmourEnum ChestArmourType;

        public ChestArmour(CombatItemReference combatItemReference, int nQuantity) :
            base(combatItemReference, nQuantity)
        {
        }

        //public ChestArmour(ChestArmourEnum chestArmourType, int quantity, string longName, string shortName, int attackStat,
        //    int defendStat, DataOvlReference.EQUIPMENT specificEquipment) : 
        //    base(quantity, longName, shortName, CHEST_ARMOUR_SPRITE, attackStat, defendStat, specificEquipment)
        //{
        //    ChestArmourType = chestArmourType;
        //}
    }
}