// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable UnusedAutoPropertyAccessor.Global

using System.Runtime.Serialization;
using Newtonsoft.Json;

namespace Ultima5Redux.PlayerCharacters.CombatItems
{
    [DataContract] public sealed class Weapon : CombatItem
    {
        [IgnoreDataMember] public override CharacterEquipped.EquippableSlot EquippableSlot =>
            TheCombatItemReference.IsShield
                ? CharacterEquipped.EquippableSlot.RightHand
                : CharacterEquipped.EquippableSlot.LeftHand;

        [IgnoreDataMember] public override bool HideQuantity => false;

        [JsonConstructor] private Weapon()
        {
        }

        public Weapon(WeaponReference weaponReference, int nQuantity) : base(weaponReference, nQuantity)
        {
        }
    }
}