using System.Runtime.Serialization;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Ultima5Redux.References;
using Ultima5Redux.References.PlayerCharacters.Inventory;

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

        [IgnoreDataMember] public override string FindDescription => SpellMagicReference.Spell;

        [IgnoreDataMember] public override bool HideQuantity => false;

        [IgnoreDataMember] public override string InventoryReferenceString => SpellIncantation.ToString();
        [IgnoreDataMember] public override bool IsSellable => false;

        [IgnoreDataMember] public int MinCircle => SpellMagicReference.Circle;

        [DataMember] public MagicReference.SpellWords SpellIncantation { get; private set; }

        [IgnoreDataMember] public MagicReference SpellMagicReference =>
            GameReferences.MagicRefs.GetMagicReference(SpellIncantation);

        [JsonConstructor] public Spell()
        {
        }

        public Spell(MagicReference.SpellWords spellWord, int quantity) : base(quantity,
            SPRITE_NUM)
        {
            SpellIncantation = spellWord;
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