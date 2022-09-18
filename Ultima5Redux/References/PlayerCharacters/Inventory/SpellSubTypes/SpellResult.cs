using System.Text;

namespace Ultima5Redux.References.PlayerCharacters.Inventory.SpellSubTypes
{
    public class SpellResult
    {
        public enum SpellResultStatus { Success, Failure }

        public readonly StringBuilder OutputStringBuilder = new();

        public string OutputString => OutputStringBuilder.ToString();

        public MagicReference.SpellWords SpellSpellWords { get; }
        public SpellResultStatus Status { get; set; }

        public SpellResult(MagicReference.SpellWords spellWords) => SpellSpellWords = spellWords;
    }
}