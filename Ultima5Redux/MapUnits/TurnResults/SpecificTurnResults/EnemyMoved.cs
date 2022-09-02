using Ultima5Redux.MapUnits.CombatMapUnits;

namespace Ultima5Redux.MapUnits.TurnResults.SpecificTurnResults
{
    public sealed class EnemyMoved : TurnResult, IEnemyFocus, IMovedPosition
    {
        public EnemyMoved(TurnResultType theTurnResultType, Enemy theEnemy, Point2D movedFromPosition,
            Point2D moveToPosition) : base(theTurnResultType)
        {
            TheEnemy = theEnemy;
            MovedFromPosition = movedFromPosition;
            MoveToPosition = moveToPosition;
        }

        public Enemy TheEnemy { get; }

        public Point2D MovedFromPosition { get; }
        public Point2D MoveToPosition { get; }
    }
}