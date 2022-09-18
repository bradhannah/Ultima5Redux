using Ultima5Redux.MapUnits.CombatMapUnits;
using Ultima5Redux.References.Maps;

namespace Ultima5Redux.MapUnits.TurnResults.SpecificTurnResults
{
    public sealed class CombatPlayerMoved : TurnResult, ICombatPlayerFocus, IMovedPosition, IMovedToTileReference
    {
        public CombatPlayerMoved(TurnResultType theTurnResultType, CombatPlayer theCombatPlayer,
            Point2D movedFromPosition, Point2D moveToPosition, TileReference movedToTileReference) : base(
            theTurnResultType)
        {
            TheCombatPlayer = theCombatPlayer;
            MovedFromPosition = movedFromPosition;
            MoveToPosition = moveToPosition;
            MovedToTileReference = movedToTileReference;
        }

        public override string GetDebugString() =>
            $@"CombatPlayer: {TheCombatPlayer.FriendlyName}
MovedFrom: {MovedFromPosition.GetFriendlyString()}
MovedTo: {MoveToPosition.GetFriendlyString()}
MovedToTileReference: {MovedToTileReference.Description} (<sprite={MovedToTileReference.Index}>)
";

        public CombatPlayer TheCombatPlayer { get; }
        public Point2D MovedFromPosition { get; }
        public Point2D MoveToPosition { get; }
        public TileReference MovedToTileReference { get; }
    }
}