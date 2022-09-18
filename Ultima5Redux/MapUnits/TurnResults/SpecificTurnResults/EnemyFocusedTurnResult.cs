using Ultima5Redux.MapUnits.CombatMapUnits;

namespace Ultima5Redux.MapUnits.TurnResults.SpecificTurnResults
{
    public sealed class EnemyFocusedTurnResult : TurnResult, IEnemyFocus
    {
        public EnemyFocusedTurnResult(TurnResultType theTurnResultType, Enemy theEnemy) : base(theTurnResultType) =>
            TheEnemy = theEnemy;

        public override string GetDebugString() =>
            $@"Enemy: {TheEnemy.FriendlyName}
EnemyXY: {TheEnemy.MapUnitPosition.XY.GetFriendlyString()}";

        public Enemy TheEnemy { get; }
    }
}