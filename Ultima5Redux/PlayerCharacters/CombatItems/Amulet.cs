namespace Ultima5Redux.PlayerCharacters.CombatItems
{
    public class Amulet : Armour
    {
        public override CharacterEquipped.EquippableSlot EquippableSlot { get; } =
            CharacterEquipped.EquippableSlot.Amulet;

        public override bool HideQuantity => false;

        public Amulet(CombatItemReference combatItemReference, int nQuantity) : base(combatItemReference, nQuantity)
        {
        }
    }
}