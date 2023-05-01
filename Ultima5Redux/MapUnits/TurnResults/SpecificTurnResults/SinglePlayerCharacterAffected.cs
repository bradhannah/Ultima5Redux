using System.Diagnostics.CodeAnalysis;
using Ultima5Redux.PlayerCharacters;

namespace Ultima5Redux.MapUnits.TurnResults.SpecificTurnResults
{
    public sealed class SingleCombatMapPlayerCharacterAffected : TurnResult, ISingleCombatMapPlayerCharacterAffected
    {
        [SuppressMessage("ReSharper", "MemberCanBePrivate.Global")]
        public PlayerCharacterRecord PlayerRecord { get; }

        public SingleCombatMapPlayerCharacterAffected(TurnResultType theTurnResultType,
            PlayerCharacterRecord record, CharacterStats stats) : base(theTurnResultType,
            TurnResulActionType.ActionAlreadyPerformed)
        {
            CombatMapUnitStats = stats;
            PlayerRecord = record;
        }

        public override string GetDebugString() => $@"PlayerRecord: {PlayerRecord?.Name}";

        public CharacterStats CombatMapUnitStats { get; }
    }
}