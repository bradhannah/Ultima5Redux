namespace Ultima5Redux.PlayerCharacters.CombatItems
{
    public class Helm : Armour
    {
        public override CharacterEquipped.EquippableSlot EquippableSlot { get; } =
            CharacterEquipped.EquippableSlot.Helm;

        public override bool HideQuantity => false;

        public Helm(CombatItemReference combatItemReference, int nQuantity) : base(combatItemReference, nQuantity)
        {
        }
    }
}