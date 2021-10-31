using Ultima5Redux.Data;

// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable UnusedAutoPropertyAccessor.Global

namespace Ultima5Redux.PlayerCharacters.CombatItems
{
    public class Weapon : CombatItem
    {
       

        // public Weapon(WeaponTypeEnum weapon, WeaponTypeSpriteEnum sprite, DataOvlReference.Equipment equipment,
        //     DataOvlReference dataOvlRef, int nQuantity)
        //     : base(equipment, dataOvlRef, nQuantity, (int)weapon, (int)sprite)
        // {
        // }

        public Weapon(WeaponReference weaponReference, int nQuantity) :
            base(weaponReference, nQuantity)
        {
            
        }
        

        //public override bool CanSell => BasePrice > 0 || TheCombatItemReference.IsAmmo;

        public override bool HideQuantity => false;

        //public WeaponTypeEnum WeaponType { get; }

        public override PlayerCharacterRecord.CharacterEquipped.EquippableSlot EquippableSlot =>
            TheCombatItemReference.IsShield
                ? PlayerCharacterRecord.CharacterEquipped.EquippableSlot.RightHand
                : PlayerCharacterRecord.CharacterEquipped.EquippableSlot.LeftHand;
    }
}