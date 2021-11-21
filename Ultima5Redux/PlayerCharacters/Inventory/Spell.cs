using System.Runtime.Serialization;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Ultima5Redux.PlayerCharacters.Inventory
{
    [DataContract] public class Spell : InventoryItem
    {
        [JsonConverter(typeof(StringEnumConverter))] public enum UnpublishedSpells
        {
            An_Ylem, // negate matter 
            In_Xen_Mani, //create create life
            Rel_Xen_Bet, // change creature small
            In_Quas_Corp // create illusion death 
        }

        private const int SPRITE_NUM = 260;

        [IgnoreDataMember] public override bool HideQuantity => false;

        [IgnoreDataMember] public override string InventoryReferenceString => SpellIncantation.ToString();
        [IgnoreDataMember] public override bool IsSellable => false;

        [IgnoreDataMember] public int MinCircle => SpellMagicReference.Circle;

        [DataMember] public MagicReference.SpellWords SpellIncantation { get; }

        [DataMember] public MagicReference SpellMagicReference { get; }

        [JsonConstructor] public Spell()
        {
        }

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