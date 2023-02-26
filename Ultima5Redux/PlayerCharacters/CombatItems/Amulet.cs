using System.Runtime.Serialization;
using Newtonsoft.Json;
using Ultima5Redux.References.PlayerCharacters.Inventory;

namespace Ultima5Redux.PlayerCharacters.CombatItems
{
    [DataContract]
    public sealed class Amulet : Armour
    {
        [IgnoreDataMember]
        public override CharacterEquipped.EquippableSlot EquippableSlot =>
            CharacterEquipped.EquippableSlot.Amulet;

        [IgnoreDataMember] public override bool HideQuantity => false;

        [JsonConstructor]
        private Amulet()
        {
        }

        public Amulet(CombatItemReference combatItemReference, int nQuantity) : base(combatItemReference, nQuantity)
        {
        }
    }
}