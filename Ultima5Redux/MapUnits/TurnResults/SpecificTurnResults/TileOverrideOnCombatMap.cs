using System.Diagnostics.CodeAnalysis;
using Ultima5Redux.References.Maps;

namespace Ultima5Redux.MapUnits.TurnResults.SpecificTurnResults
{
    [SuppressMessage("ReSharper", "MemberCanBePrivate.Global")]
    [SuppressMessage("ReSharper", "UnusedAutoPropertyAccessor.Global")]
    public class TileOverrideOnCombatMap : TurnResult
    {
        public Point2D Position { get; }
        public TileReference ReplacementTile { get; }

        public TileOverrideOnCombatMap(Point2D position, TileReference replacementTile) : base(TurnResultType
            .OverrideCombatMapTile, TurnResulActionType.ActionAlreadyPerformed)
        {
            Position = position;
            ReplacementTile = replacementTile;
        }
    }
}