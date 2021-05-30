using System;
using System.Text;
using Ultima5Redux.PlayerCharacters.Inventory;

namespace Ultima5Redux.PlayerCharacters
{
    public class Spell : InventoryItem
    {
        public enum SpellWords
        {
            // taking a bit of a risk and just let the subsequent values be assigned since they should be in order
            In_Lor = 0x24A, Grav_Por, An_Zu, An_Nox, Mani, An_Ylem, An_Sanct, An_Xen_Corp, Rel_Hur, In_Wis, Kal_Xen,
            In_Xen_Mani, Vas_Lor, Vas_Flam, In_Flam_Grav, In_Nox_Grav, In_Zu_Grav, In_Por, An_Grav, In_Sanct,
            In_Sanct_Grav, Uus_Por, Des_Por, Wis_Quas, In_Bet_Xen, An_Ex_Por, In_Ex_Por, Vas_Mani, In_Zu, Rel_Tym,
            In_Vas_Por_Ylem, Quas_An_Wis, In_An, Wis_An_Ylem, An_Xen_Ex, Rel_Xen_Bet, Sanct_Lor, Xen_Corp, In_Quas_Xen,
            In_Quas_Wis, In_Nox_Hur, In_Quas_Corp, In_Mani_Corp, Kal_Xen_Corp, In_Vas_Grav_Corp, In_Flam_Hur,
            Vas_Rel_Por, An_Tym, Nox
        }

        public enum SpellWordsCircles
        {
            In_Lor = 1, Grav_Por = 1, An_Zu = 1, An_Nox = 1, Mani = 1, An_Ylem = 1, An_Sanct = 2, An_Xen_Corp = 2,
            Rel_Hur = 2, In_Wis = 2, Kal_Xen = 2, In_Xen_Mani = 2, Vas_Lor = 3, Vas_Flam = 3, In_Flam_Grav = 3,
            In_Nox_Grav = 3, In_Zu_Grav = 3, In_Por = 3, An_Grav = 4, In_Sanct = 4, In_Sanct_Grav = 4, Uus_Por = 4,
            Des_Por = 4, Wis_Quas = 4, In_Bet_Xen = 5, An_Ex_Por = 5, In_Ex_Por = 5, Vas_Mani = 5, In_Zu = 5,
            Rel_Tym = 5, In_Vas_Por_Ylem = 6, Quas_An_Wis = 6, In_An = 6, Wis_An_Ylem = 6, An_Xen_Ex = 6,
            Rel_Xen_Bet = 6, Sanct_Lor = 7, Xen_Corp = 7, In_Quas_Xen = 7, In_Quas_Wis = 7, In_Nox_Hur = 7,
            In_Quas_Corp = 7, In_Mani_Corp = 8, Kal_Xen_Corp = 8, In_Vas_Grav_Corp = 8, In_Flam_Hur = 8,
            Vas_Rel_Por = 8, An_Tym = 8,
            Nox = 0
        }

        public enum UnpublishedSpells { 
            An_Ylem, // negate matter 
            In_Xen_Mani, //create create life
            Rel_Xen_Bet, // change creature small
            In_Quas_Corp // create illusion death 
        } 

        private const int SPRITE_NUM = 260;

        public Spell(SpellWords spellWord, int quantity, string longName, string shortName) : base(quantity, longName,
            shortName, SPRITE_NUM)
        {
            SpellIncantation = spellWord;
        }

        public override bool HideQuantity { get; } = false;
        public override bool IsSellable => false;

        public override string InventoryReferenceString => SpellIncantation.ToString();
        
        public SpellWords SpellIncantation { get; }

        public int MinCircle()
        {
            return (int) Enum.Parse(typeof(SpellWordsCircles), SpellIncantation.ToString());
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