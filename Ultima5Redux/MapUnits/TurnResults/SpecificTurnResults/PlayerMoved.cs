using Ultima5Redux.References.Maps;

namespace Ultima5Redux.MapUnits.TurnResults.SpecificTurnResults
{
    public sealed class PlayerMoved : TurnResult, IMovedPosition, IMovedToTileReference
    {
        public PlayerMoved(TurnResultType theTurnResultType, Point2D movedFromPosition, Point2D moveToPosition,
            TileReference movedToTileReference) : base(theTurnResultType)
        {
            MovedFromPosition = movedFromPosition;
            MoveToPosition = moveToPosition;
            MovedToTileReference = movedToTileReference;
        }

        public override string GetDebugString()
        {
            return $@"MovedFromPosition: {MovedFromPosition.GetFriendlyString()}
MoveToPosition: {MoveToPosition.GetFriendlyString()}
MovedToTileReference: {MovedToTileReference.Name} (<sprite={MovedToTileReference.Index}>)";
        }

        public Point2D MovedFromPosition { get; }
        public Point2D MoveToPosition { get; }
        public TileReference MovedToTileReference { get; }
    }
}