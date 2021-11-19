namespace Ultima5Redux.PlayerCharacters.CombatItems
{
    public class Ring : Armour
    {
        public override CharacterEquipped.EquippableSlot EquippableSlot { get; } =
            CharacterEquipped.EquippableSlot.Ring;

        public override bool HideQuantity => false;

        public Ring(CombatItemReference combatItemReference, int nQuantity) :
            base(combatItemReference, nQuantity)
        {
        }
    }
}