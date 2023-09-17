using System;
using System.Collections.Generic;
using Ultima5Redux.MapUnits;
using Ultima5Redux.MapUnits.TurnResults;
using Ultima5Redux.References;
using Ultima5Redux.References.Maps;

namespace Ultima5Redux.Maps
{
    public class CutOrIntroSceneMap : Map
    {
        public CutOrIntroSceneMap(SingleCutOrIntroSceneMapReference theSingleCutOrIntroSceneMapReference) =>
            TheSingleCutOrIntroSceneMapReference = theSingleCutOrIntroSceneMapReference;

        public SingleCutOrIntroSceneMapReference TheSingleCutOrIntroSceneMapReference { get; }

        public override Maps TheMapType => Maps.CutScene;

        public override SmallMapReferences.SingleMapReference CurrentSingleMapReference =>
            GameReferences.Instance.SmallMapRef.GetSingleMapByLocation(
                SmallMapReferences.SingleMapReference.Location.Combat_resting_shrine, 0);

        public override MapUnitPosition CurrentPosition { get; set; }
        public override int NumOfXTiles => TheSingleCutOrIntroSceneMapReference.N_MAP_COLS_PER_ROW;
        public override int NumOfYTiles => TheSingleCutOrIntroSceneMapReference.N_MAP_ROWS_PER_MAP;
        public override byte[][] TheMap { get; protected set; }
        protected override Dictionary<Point2D, TileOverrideReference> XyOverrides => new();
        internal override WalkableType GetWalkableTypeByMapUnit(MapUnit mapUnit) => throw new NotImplementedException();

        internal override void ProcessTileEffectsForMapUnit(TurnResults turnResults, MapUnit mapUnit)
        {
        }

        protected override float GetAStarWeight(in Point2D xy) => throw new NotImplementedException();

        public override bool IsRepeatingMap => false;
    }
}