using System;
using System.Collections.Generic;
using Ultima5Redux.MapUnits;
using Ultima5Redux.MapUnits.NonPlayerCharacters;
using Ultima5Redux.MapUnits.TurnResults;
using Ultima5Redux.References;
using Ultima5Redux.References.Maps;

namespace Ultima5Redux.Maps
{
    public class CutSceneMap : RegularMap
    {
        public override SmallMapReferences.SingleMapReference CurrentSingleMapReference =>
            GameReferences.Instance.SmallMapRef.GetSingleMapByLocation(
                SmallMapReferences.SingleMapReference.Location.Combat_resting_shrine, 0);

        public override bool IsRepeatingMap => false;

        //public override MapUnitPosition CurrentPosition { get; set; }
        public override int NumOfXTiles => TheSingleCutOrIntroSceneMapReference.N_MAP_COLS_PER_ROW;
        public override int NumOfYTiles => TheSingleCutOrIntroSceneMapReference.N_MAP_ROWS_PER_MAP;

        public override byte[][] TheMap { get; protected set; }

        public override Maps TheMapType => Maps.CutScene;

        public SingleCutOrIntroSceneMapReference TheSingleCutOrIntroSceneMapReference { get; }

        public CutSceneMap(SingleCutOrIntroSceneMapReference theSingleCutOrIntroSceneMapReference) : base(
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

        private readonly Dictionary<string, CutSceneNonPlayerCharacter> _mapUnitsByIdentifier = new();

        public void ProcessScriptLine(CutOrIntroSceneScriptLine scriptLine) {
            switch (scriptLine.Command) {
                case CutOrIntroSceneScriptLine.CutOrIntroSceneScriptLineCommand.CreateMapunit:
                    var mapUnitPosition = new MapUnitPosition();
                    mapUnitPosition.XY = scriptLine.Position;

                    // MapUnit mapUnit = new CutSceneNonPlayerCharacter(scriptLine.Visible || true, scriptLine.TileReference);
                    var mapUnit = new CutSceneNonPlayerCharacter(scriptLine.Visible, scriptLine.TileReference);
                    
                    mapUnit.MapUnitPosition.XY = scriptLine.Position;
                    mapUnit.SetActive(scriptLine.Visible);
                    // we maintain a specific identifier for the mapunit going forward
                    _mapUnitsByIdentifier.Add(scriptLine.StrParam, mapUnit);
                    CurrentMapUnits.Add(mapUnit);
                    break;
                case CutOrIntroSceneScriptLine.CutOrIntroSceneScriptLineCommand.MoveMapunit:
                    if (!_mapUnitsByIdentifier.ContainsKey(scriptLine.StrParam)) {
                        throw new Ultima5ReduxException(
                            $"{scriptLine.Command} command issued for mapunit \"{scriptLine.StrParam}\", but does not appear to be created yet.");
                    }

                    CutSceneNonPlayerCharacter mapUnitToMove = _mapUnitsByIdentifier[scriptLine.StrParam];
                    mapUnitToMove.MapUnitPosition.XY = scriptLine.Position;
                    mapUnitToMove.SetActive(scriptLine.Visible);
                    break;
                case CutOrIntroSceneScriptLine.CutOrIntroSceneScriptLineCommand.Pause:
                    break;
                case CutOrIntroSceneScriptLine.CutOrIntroSceneScriptLineCommand.PromptVirtueMeditate:
                    // Something must be done externally for this...
                    break;
                case CutOrIntroSceneScriptLine.CutOrIntroSceneScriptLineCommand.EndSequence:
                    // Close the map and return to whence you came
                    break;
                case CutOrIntroSceneScriptLine.CutOrIntroSceneScriptLineCommand.Comment:
                    // we will always do nothing here, it's just for the readability of the script
                    break;
                case CutOrIntroSceneScriptLine.CutOrIntroSceneScriptLineCommand.Output:
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        internal override WalkableType GetWalkableTypeByMapUnit(MapUnit mapUnit) => throw new NotImplementedException();

        internal override void ProcessTileEffectsForMapUnit(TurnResults turnResults, MapUnit mapUnit) {
        }

        protected override float GetAStarWeight(in Point2D xy) => throw new NotImplementedException();
        //protected override Dictionary<Point2D, TileOverrideReference> XyOverrides => new();

        protected override VirtualMap.AggressiveMapUnitInfo GetNonCombatMapAggressiveMapUnitInfo(
            Point2D attackFromPosition, Point2D attackToPosition,
            SingleCombatMapReference.Territory territory, MapUnit aggressorMapUnit) =>
            throw new NotImplementedException();

        protected override void SetMaxVisibleArea(in Point2D startPos, int nVisibleTiles) {
            //base.SetMaxVisibleArea(in startPos, nVisibleTiles);
            _ = "";
        }
    }
}