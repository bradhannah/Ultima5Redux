using Ultima5Redux.MapUnits.CombatMapUnits;

namespace Ultima5Redux.MapUnits.TurnResults.SpecificTurnResults
{
    public sealed class EnemyFocusedTurnResult : TurnResult, IEnemyFocus
    {
        public EnemyFocusedTurnResult(TurnResultType theTurnResultType, Enemy theEnemy) : base(theTurnResultType)
        {
            TheEnemy = theEnemy;
        }

        public Enemy TheEnemy { get; }
    }
}