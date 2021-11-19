namespace Ultima5Redux.PlayerCharacters.CombatItems
{
    public class ChestArmour : Armour
    {
        public override CharacterEquipped.EquippableSlot EquippableSlot { get; } =
            CharacterEquipped.EquippableSlot.Armour;

        public override bool HideQuantity => false;

        public ChestArmour(CombatItemReference combatItemReference, int nQuantity) :
            base(combatItemReference, nQuantity)
        {
        }
    }
}