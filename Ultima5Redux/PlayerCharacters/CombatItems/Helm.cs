using System.Runtime.Serialization;
using Newtonsoft.Json;
using Ultima5Redux.References.PlayerCharacters.Inventory;

namespace Ultima5Redux.PlayerCharacters.CombatItems
{
    [DataContract] public sealed class Helm : Armour
    {
        [IgnoreDataMember]
        public override CharacterEquipped.EquippableSlot EquippableSlot =>
            CharacterEquipped.EquippableSlot.Helm;

        [IgnoreDataMember] public override bool HideQuantity => false;

        [JsonConstructor] private Helm()
        {
        }

        public Helm(CombatItemReference combatItemReference, int nQuantity) : base(combatItemReference, nQuantity)
        {
        }
    }
}