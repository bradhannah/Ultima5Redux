namespace Ultima5Redux.PlayerCharacters.Inventory
{
    public class Scroll : InventoryItem
    {
        public enum ScrollSpells
        {
            Vas_Lor = 0, Rel_Hur, In_Sanct, In_An, In_Quas_Wis, Kal_Xen_Corp, In_Mani_Corp, An_Tym
        }

        private const int SCROLL_SPRITE = 260;

        public Scroll(MagicReference.SpellWords spell, int quantity, string longName, string shortName,  MagicReference scrollMagicReference) : base(quantity,
            longName, shortName, SCROLL_SPRITE)
        {
            ScrollSpell = spell;
            ScrollMagicReference = scrollMagicReference;
        }

        public override bool HideQuantity { get; } = false;
        public override bool IsSellable => false;
        public override int BasePrice => 0;

        public MagicReference.SpellWords ScrollSpell { get; }
        public MagicReference ScrollMagicReference { get; }

        public override string InventoryReferenceString => ScrollSpell.ToString();
    }
}