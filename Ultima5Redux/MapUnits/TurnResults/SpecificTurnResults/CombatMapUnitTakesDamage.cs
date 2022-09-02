using Ultima5Redux.PlayerCharacters;

namespace Ultima5Redux.MapUnits.TurnResults.SpecificTurnResults
{
    public sealed class CombatMapUnitTakesDamage : TurnResult, ISinglePlayerCharacterAffected, IDamageAmount
    {
        public CombatMapUnitTakesDamage(TurnResultType theTurnResultType, CharacterStats combatMapUnitStats,
            int damageAmount) : base(theTurnResultType)
        {
            CombatMapUnitStats = combatMapUnitStats;
            DamageAmount = damageAmount;
        }

        public int DamageAmount { get; }
        public CharacterStats CombatMapUnitStats { get; }
    }
}