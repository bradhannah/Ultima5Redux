using System.Diagnostics.CodeAnalysis;
using Ultima5Redux.PlayerCharacters;

namespace Ultima5Redux.MapUnits.TurnResults.SpecificTurnResults
{
    public sealed class SinglePlayerCharacterAffected : TurnResult, ISinglePlayerCharacterAffected
    {
        [SuppressMessage("ReSharper", "MemberCanBePrivate.Global")] public PlayerCharacterRecord PlayerRecord { get; }

        public SinglePlayerCharacterAffected(TurnResultType theTurnResultType,
            PlayerCharacterRecord record, CharacterStats stats) : base(theTurnResultType)
        {
            CombatMapUnitStats = stats;
            PlayerRecord = record;
        }

        public override string GetDebugString() => $@"PlayerRecord: {PlayerRecord?.Name}";

        public CharacterStats CombatMapUnitStats { get; }
    }
}