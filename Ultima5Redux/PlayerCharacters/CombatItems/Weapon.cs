using Ultima5Redux.Data;

// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable UnusedAutoPropertyAccessor.Global

namespace Ultima5Redux.PlayerCharacters.CombatItems
{
    public class Weapon : CombatItem
    {
        public Weapon(WeaponReference weaponReference, int nQuantity) : base(weaponReference, nQuantity)
        {
            
        }

        public override bool HideQuantity => false;

        public override CharacterEquipped.EquippableSlot EquippableSlot =>
            TheCombatItemReference.IsShield
                ? CharacterEquipped.EquippableSlot.RightHand
                : CharacterEquipped.EquippableSlot.LeftHand;
    }
}