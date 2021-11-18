using System.Text;
using Ultima5Redux.PlayerCharacters.Inventory;

namespace Ultima5Redux.PlayerCharacters
{
    public class Spell : InventoryItem
    {
        public enum UnpublishedSpells
        {
            An_Ylem, // negate matter 
            In_Xen_Mani, //create create life
            Rel_Xen_Bet, // change creature small
            In_Quas_Corp // create illusion death 
        }

        private const int SPRITE_NUM = 260;

        public override bool HideQuantity { get; } = false;

        public override string InventoryReferenceString => SpellIncantation.ToString();
        public override bool IsSellable => false;

        public int MinCircle => SpellMagicReference.Circle;

        public MagicReference.SpellWords SpellIncantation { get; }

        public MagicReference SpellMagicReference { get; }

        public Spell(MagicReference.SpellWords spellWord, int quantity, MagicReference magicReference) : base(quantity,
            SPRITE_NUM)
        {
            SpellIncantation = spellWord;
            SpellMagicReference = magicReference;
        }

        public string GetLiteralTranslation()
        {
            string[] spellStrs = SpellIncantation.ToString().Split('_'); // .Replace('_', ' ');
            StringBuilder sb = new StringBuilder();
            foreach (string str in spellStrs)
            {
                sb.Append(Spells.GetLiteralTranslation(str) + " ");
            }

            return sb.ToString().TrimEnd();
        }
    }
}