using Ultima5Redux.Data;

namespace Ultima5Redux.PlayerCharacters.CombatItems
{
    public class Amulet : Armour
    {

        public Amulet(CombatItemReference combatItemReference, int nQuantity) :
            base(combatItemReference, nQuantity)
        {
            
        }        

        public override bool HideQuantity => false;

        public override CharacterEquipped.EquippableSlot EquippableSlot { get; } =
            CharacterEquipped.EquippableSlot.Amulet;
    }
}