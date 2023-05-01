using System.Diagnostics.CodeAnalysis;
using Ultima5Redux.PlayerCharacters;
using Ultima5Redux.References.PlayerCharacters.Inventory;

namespace Ultima5Redux.MapUnits.TurnResults.SpecificTurnResults
{
    [SuppressMessage("ReSharper", "UnusedAutoPropertyAccessor.Global")]
    [SuppressMessage("ReSharper", "MemberCanBePrivate.Global")]
    public class ReadScroll : TurnResult
    {
        public PlayerCharacterRecord PlayerWhoIsTargeted { get; }
        public PlayerCharacterRecord ReadByWho { get; }
        public MagicReference.SpellWords SpellWords { get; }

        public ReadScroll(MagicReference.SpellWords spellWords, PlayerCharacterRecord readByWho,
            PlayerCharacterRecord playerWhoIsTargeted) : base(TurnResultType.ActionUseReadScroll,
            TurnResulActionType.ActionRequired)
        {
            SpellWords = spellWords;
            ReadByWho = readByWho;
            PlayerWhoIsTargeted = playerWhoIsTargeted;
        }
    }
}