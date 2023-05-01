using Ultima5Redux.MapUnits.CombatMapUnits;

namespace Ultima5Redux.MapUnits.TurnResults.SpecificTurnResults
{
    public sealed class EnemyMoved : TurnResult, IEnemyFocus, IMovedPosition
    {
        public EnemyMoved(TurnResultType theTurnResultType, Enemy theEnemy, Point2D movedFromPosition,
            Point2D moveToPosition) : base(theTurnResultType, TurnResulActionType.ActionAlreadyPerformed)
        {
            TheEnemy = theEnemy;
            MovedFromPosition = movedFromPosition;
            MoveToPosition = moveToPosition;
        }

        public override string GetDebugString() =>
            $@"Enemy: {TheEnemy.FriendlyName}
EnemyFromPosition: {MovedFromPosition.GetFriendlyString()}
EnemyToPosition: {MoveToPosition.GetFriendlyString()}
";

        public Enemy TheEnemy { get; }

        public Point2D MovedFromPosition { get; }
        public Point2D MoveToPosition { get; }
    }
}