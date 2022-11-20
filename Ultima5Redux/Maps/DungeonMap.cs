using System;
using System.Collections.Generic;
using Ultima5Redux.MapUnits;
using Ultima5Redux.MapUnits.TurnResults;
using Ultima5Redux.References;
using Ultima5Redux.References.Maps;

namespace Ultima5Redux.Maps
{
    public class DungeonMap : Map
    {
        public override int NumOfXTiles => SingleDungeonMapFloorReference.N_DUNGEON_COLS_PER_ROW;
        public override int NumOfYTiles => SingleDungeonMapFloorReference.N_DUNGEON_ROWS_PER_MAP;
        public override bool ShowOuterSmallMapTiles => false;

        public override byte[][] TheMap
        {
            get => new byte[DungeonMapReference.N_DUNGEONS][];
            protected set => throw new NotImplementedException();
        }

        protected override Dictionary<Point2D, TileOverrideReference> XYOverrides => new();

        internal override void ProcessTileEffectsForMapUnit(TurnResults turnResults, MapUnit mapUnit)
        {
            //throw new System.NotImplementedException();
        }

        protected override float GetAStarWeight(in Point2D xy) => 1.0f; //throw new System.NotImplementedException();

        protected override WalkableType GetWalkableTypeByMapUnit(MapUnit mapUnit) => WalkableType.StandardWalking;

        public override bool IsRepeatingMap => true;
    }
}