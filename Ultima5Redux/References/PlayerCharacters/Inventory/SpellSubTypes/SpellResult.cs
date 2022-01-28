using System.Text;

namespace Ultima5Redux.References.PlayerCharacters.Inventory.SpellSubTypes
{
    public class SpellResult
    {
        public SpellResult(MagicReference.SpellWords spellWords)
        {
            SpellSpellWords = spellWords;
        }

        public MagicReference.SpellWords SpellSpellWords { get; }

        public enum SpellResultStatus { Success, Failure }

        public string OutputString => OutputStringBuilder.ToString();

        public readonly StringBuilder OutputStringBuilder = new();
        public SpellResultStatus Status { get; set; }
    }
}