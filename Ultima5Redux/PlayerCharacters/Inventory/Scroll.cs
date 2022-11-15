using System.Runtime.Serialization;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Ultima5Redux.References;
using Ultima5Redux.References.PlayerCharacters.Inventory;

namespace Ultima5Redux.PlayerCharacters.Inventory
{
    [DataContract] public class Scroll : InventoryItem
    {
        [JsonConverter(typeof(StringEnumConverter))]
        public enum ScrollSpells
        {
            Vas_Lor = 0x27A, Rel_Hur = 0x27B, In_Sanct = 0x27C, In_An = 0x27D, In_Quas_Wis = 0x27E,
            Kal_Xen_Corp = 0x27F, In_Mani_Corp = 0x280, An_Tym = 0x281
        }

        private const int SCROLL_SPRITE = 260;

        [DataMember] public MagicReference.SpellWords ScrollSpell { get; private set; }

        [IgnoreDataMember] public override int BasePrice => 0;
        [IgnoreDataMember] public override string FindDescription => ScrollMagicReference.Spell + " scroll";

        [IgnoreDataMember] public override bool HideQuantity => false;

        [IgnoreDataMember] public override string InventoryReferenceString => ScrollSpell.ToString();
        [IgnoreDataMember] public override bool IsSellable => false;

        public static int GetLegacySaveQuantityIndex(ScrollSpells scrollSpells) => (int)scrollSpells;
        
        [IgnoreDataMember]
        public MagicReference ScrollMagicReference
        {
            get => GameReferences.MagicRefs.GetMagicReference(ScrollSpell);
            set => ScrollSpell = value.SpellEnum;
        }

        [JsonConstructor] private Scroll()
        {
        }

        public Scroll(MagicReference.SpellWords spell, int quantity, MagicReference scrollMagicReference) : base(
            quantity, SCROLL_SPRITE, InventoryReferences.InventoryReferenceType.Item)
        {
            ScrollSpell = spell;
            ScrollMagicReference = scrollMagicReference;
        }
    }
}