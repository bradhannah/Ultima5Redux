using System.Diagnostics.CodeAnalysis;
using System.Text;

namespace Ultima5Redux.References.PlayerCharacters.Inventory.SpellSubTypes
{
    [SuppressMessage("ReSharper", "UnusedAutoPropertyAccessor.Global")]
    public class SpellResult
    {
        public enum SpellResultStatus
        {
            Success,
            Failure
        }

        public readonly StringBuilder OutputStringBuilder = new();

        [SuppressMessage("ReSharper", "UnusedMember.Global")]
        public string OutputString => OutputStringBuilder.ToString();

        [SuppressMessage("ReSharper", "MemberCanBePrivate.Global")]
        public MagicReference.SpellWords SpellSpellWords { get; }

        public SpellResultStatus Status { get; set; }

        public SpellResult(MagicReference.SpellWords spellWords) => SpellSpellWords = spellWords;
    }
}