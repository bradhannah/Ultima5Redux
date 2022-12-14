using Ultima5Redux.References.Maps;

namespace Ultima5Redux.MapUnits.TurnResults.SpecificTurnResults
{
    public class TileOverrideOnCombatMap : TurnResult
    {
        public TileOverrideOnCombatMap(Point2D position, TileReference replacementTile) : base(TurnResultType
            .OverrideCombatMapTile)
        {
            Position = position;
            ReplacementTile = replacementTile;
        }

        public Point2D Position { get; }
        public TileReference ReplacementTile { get; }
    }
}