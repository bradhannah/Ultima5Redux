using System.Runtime.Serialization;
using Newtonsoft.Json;
using Ultima5Redux.References.PlayerCharacters.Inventory;

namespace Ultima5Redux.PlayerCharacters.CombatItems
{
    [DataContract] public sealed class Ring : Armour
    {
        [IgnoreDataMember]
        public override CharacterEquipped.EquippableSlot EquippableSlot =>
            CharacterEquipped.EquippableSlot.Ring;

        [IgnoreDataMember] public override bool HideQuantity => false;

        [JsonConstructor] private Ring()
        {
        }

        public Ring(CombatItemReference combatItemReference, int nQuantity) : base(combatItemReference, nQuantity)
        {
        }
    }
}