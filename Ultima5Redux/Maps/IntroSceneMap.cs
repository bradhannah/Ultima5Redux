using System;
using Ultima5Redux.MapUnits;
using Ultima5Redux.MapUnits.TurnResults;
using Ultima5Redux.References;
using Ultima5Redux.References.Maps;

namespace Ultima5Redux.Maps
{
    public class IntroSceneMap : RegularMap
    {
        protected override void SetMaxVisibleArea(in Point2D startPos, int nVisibleTiles) {
            //base.SetMaxVisibleArea(in startPos, nVisibleTiles);
            _ = "";
        }

        public IntroSceneMap(SingleCutOrIntroSceneMapReference theSingleCutOrIntroSceneMapReference) : base(
            SmallMapReferences.SingleMapReference.Location.Combat_resting_shrine, 0) {
            TheSingleCutOrIntroSceneMapReference = theSingleCutOrIntroSceneMapReference;

            TheMap = theSingleCutOrIntroSceneMapReference.GetMap();

            CurrentMapUnits ??= new MapUnitCollection();
            CurrentMapUnits.Clear();

            MapUnit theAvatar = Avatar.CreateAvatar(
                SmallMapReferences.SingleMapReference.Location.Combat_resting_shrine,
                new MapUnitMovement(0),
                new MapUnitPosition(0, 0, 0),
                GameReferences.Instance.SpriteTileReferences.GetTileReference(284), UseExtendedSprites);
            CurrentMapUnits.Add(theAvatar);
        }

        public SingleCutOrIntroSceneMapReference TheSingleCutOrIntroSceneMapReference { get; }

        public override Maps TheMapType => Maps.CutScene;

        public override SmallMapReferences.SingleMapReference CurrentSingleMapReference =>
            GameReferences.Instance.SmallMapRef.GetSingleMapByLocation(
                SmallMapReferences.SingleMapReference.Location.Combat_resting_shrine, 0);

        //public override MapUnitPosition CurrentPosition { get; set; }
        public override int NumOfXTiles => TheSingleCutOrIntroSceneMapReference.N_MAP_COLS_PER_ROW;
        public override int NumOfYTiles => TheSingleCutOrIntroSceneMapReference.N_MAP_ROWS_PER_MAP;

        public sealed override byte[][] TheMap { get; protected set; }
        //protected override Dictionary<Point2D, TileOverrideReference> XyOverrides => new();

        protected override VirtualMap.AggressiveMapUnitInfo GetNonCombatMapAggressiveMapUnitInfo(
            Point2D attackFromPosition, Point2D attackToPosition,
            SingleCombatMapReference.Territory territory, MapUnit aggressorMapUnit) =>
            throw new NotImplementedException();

        internal override WalkableType GetWalkableTypeByMapUnit(MapUnit mapUnit) => throw new NotImplementedException();

        internal override void ProcessTileEffectsForMapUnit(TurnResults turnResults, MapUnit mapUnit) {
        }

        protected override float GetAStarWeight(in Point2D xy) => throw new NotImplementedException();

        public override bool IsRepeatingMap => false;
    }
}