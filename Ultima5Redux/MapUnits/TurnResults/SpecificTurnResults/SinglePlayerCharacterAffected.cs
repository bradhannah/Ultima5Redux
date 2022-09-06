using Ultima5Redux.PlayerCharacters;

namespace Ultima5Redux.MapUnits.TurnResults.SpecificTurnResults
{
    public sealed class SinglePlayerCharacterAffected : TurnResult, ISinglePlayerCharacterAffected
    {
        public PlayerCharacterRecord PlayerRecord { get; set; }

        public SinglePlayerCharacterAffected(TurnResultType theTurnResultType,
            CharacterStats stats) : base(theTurnResultType)
        {
            CombatMapUnitStats = stats;
        }

        public override string GetDebugString()
        {
            return $@"PlayerRecord: {PlayerRecord?.Name}";
        }

        public CharacterStats CombatMapUnitStats { get; }
    }
}