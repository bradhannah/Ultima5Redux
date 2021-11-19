namespace Ultima5Redux.PlayerCharacters.Inventory
{
    public class Scroll : InventoryItem
    {
        public enum ScrollSpells
        {
            Vas_Lor = 0, Rel_Hur, In_Sanct, In_An, In_Quas_Wis, Kal_Xen_Corp, In_Mani_Corp, An_Tym
        }

        private const int SCROLL_SPRITE = 260;
        public override int BasePrice => 0;

        public override bool HideQuantity { get; } = false;

        public override string InventoryReferenceString => ScrollSpell.ToString();
        public override bool IsSellable => false;
        public MagicReference ScrollMagicReference { get; }

        public MagicReference.SpellWords ScrollSpell { get; }

        public Scroll(MagicReference.SpellWords spell, int quantity, MagicReference scrollMagicReference) : base(
            quantity, SCROLL_SPRITE)
        {
            ScrollSpell = spell;
            ScrollMagicReference = scrollMagicReference;
        }
    }
}