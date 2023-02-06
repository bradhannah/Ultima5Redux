using System.Diagnostics.CodeAnalysis;
using System.Runtime.Serialization;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Ultima5Redux.References;
using Ultima5Redux.References.PlayerCharacters.Inventory;
using Ultima5Redux.References.PlayerCharacters.Inventory.SpellSubTypes;

namespace Ultima5Redux.PlayerCharacters.Inventory
{
    [DataContract] public sealed class Spell : InventoryItem
    {
        [JsonConverter(typeof(StringEnumConverter))] [SuppressMessage("ReSharper", "InconsistentNaming")]
        public enum UnpublishedSpells
        {
            An_Ylem, // negate matter 
            In_Xen_Mani, //create create life
            Rel_Xen_Bet, // change creature small
            In_Quas_Corp // create illusion death 
        }

        private const int SPRITE_NUM = 260;

        [DataMember(Name = "IsSpellMemorized")]
        private bool _memorizedSpell;

        [DataMember] public MagicReference.SpellWords SpellIncantation { get; private set; }

        [IgnoreDataMember] public override string FindDescription => SpellMagicReference.Spell;

        [IgnoreDataMember] public override bool HideQuantity => false;

        [IgnoreDataMember] public override string InventoryReferenceString => SpellIncantation.ToString();
        [IgnoreDataMember] public override bool IsSellable => false;

        [IgnoreDataMember] public bool IsMemorized => Quantity > 0 || _memorizedSpell;

        [IgnoreDataMember] public int MinCircle => SpellMagicReference.Circle;

        [IgnoreDataMember]
        public MagicReference SpellMagicReference =>
            GameReferences.Instance.MagicRefs.GetMagicReference(SpellIncantation);

        [JsonConstructor] public Spell()
        {
        }

        public Spell(MagicReference.SpellWords spellWord, int quantity) : base(quantity, SPRITE_NUM,
            InventoryReferences.InventoryReferenceType.Spell)
        {
            SpellIncantation = spellWord;
            if (Quantity > 0) LearnSpell();
        }

        [OnDeserialized] private void PostDeserialized(StreamingContext context)
        {
            if (Quantity > 0) LearnSpell();
        }

        public SpellResult CastSpell(GameState state, SpellCastingDetails details)
        {
            if (Quantity <= 0)
                throw new Ultima5ReduxException($"Tried to cast {LongName} but had quantity: {Quantity}");

            Quantity--;
            return SpellMagicReference.CastSpell(state, details);
        }

        public string GetLiteralTranslation()
        {
            string[] spellStrings = SpellIncantation.ToString().Split('_');
            StringBuilder sb = new();
            foreach (string str in spellStrings)
            {
                sb.Append(Spells.GetLiteralTranslation(str) + " ");
            }

            return sb.ToString().TrimEnd();
        }

        public bool IsCastableByPlayer(PlayerCharacterRecord record) =>
            Quantity > 0 && record.Stats.CurrentMp >= MinCircle;

        public void LearnSpell()
        {
            _memorizedSpell = true;
        }
    }
}