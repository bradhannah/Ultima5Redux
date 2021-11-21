using System.Runtime.Serialization;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Ultima5Redux.PlayerCharacters.Inventory
{
    [DataContract] public class Scroll : InventoryItem
    {

        [JsonConverter(typeof(StringEnumConverter))] public enum ScrollSpells
        {
            Vas_Lor = 0, Rel_Hur, In_Sanct, In_An, In_Quas_Wis, Kal_Xen_Corp, In_Mani_Corp, An_Tym
        }

        private const int SCROLL_SPRITE = 260;
        [IgnoreDataMember] public override int BasePrice => 0;

        [DataMember] public override bool HideQuantity { get; }

        [IgnoreDataMember] public override string InventoryReferenceString => ScrollSpell.ToString();
        [IgnoreDataMember] public override bool IsSellable => false;
        [DataMember] public MagicReference ScrollMagicReference { get; }
        [DataMember] public MagicReference.SpellWords ScrollSpell { get; }

        [JsonConstructor] private Scroll()
        {
        }
        
        public Scroll(MagicReference.SpellWords spell, int quantity, MagicReference scrollMagicReference) : base(
            quantity, SCROLL_SPRITE)
        {
            ScrollSpell = spell;
            ScrollMagicReference = scrollMagicReference;
        }
    }
}