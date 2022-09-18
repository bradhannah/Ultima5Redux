using System.Runtime.Serialization;
using Newtonsoft.Json;
using Ultima5Redux.References.PlayerCharacters.Inventory;

namespace Ultima5Redux.PlayerCharacters.CombatItems
{
    [DataContract] public sealed class ChestArmour : Armour
    {
        [IgnoreDataMember]
        public override CharacterEquipped.EquippableSlot EquippableSlot =>
            CharacterEquipped.EquippableSlot.Armour;

        [IgnoreDataMember] public override bool HideQuantity => false;

        [JsonConstructor] private ChestArmour()
        {
        }

        public ChestArmour(CombatItemReference combatItemReference, int nQuantity) : base(combatItemReference,
            nQuantity)
        {
        }
    }
}