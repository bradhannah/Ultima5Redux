using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.Serialization;
using Newtonsoft.Json;
using Ultima5Redux.References;
using Ultima5Redux.References.Maps;

namespace Ultima5Redux.Maps
{
    [DataContract] public class SmallMaps
    {
        [DataMember(Name = "MapLocationDictionary")]
        private readonly Dictionary<SmallMapReferences.SingleMapReference.Location, Dictionary<int, SmallMap>>
            _mapLocationDictionary = new();

        [JsonConstructor] public SmallMaps()
        {
            // if the _mapLocationDictionary already has elements, then we assume it was deserialized and skip this step
            if (_mapLocationDictionary.Count > 0) return;

            foreach (SmallMapReferences.SingleMapReference mapRef in GameReferences.SmallMapRef.MapReferenceList)
            {
                // TEMPORARY while we don't currently read in Dungeons
                if (mapRef.MapType == Map.Maps.Dungeon)
                {
                    if (!_mapLocationDictionary.ContainsKey(mapRef.MapLocation))
                        _mapLocationDictionary.Add(mapRef.MapLocation, new Dictionary<int, SmallMap>());
                    _mapLocationDictionary[mapRef.MapLocation].Add(mapRef.Floor, null);
                    continue;
                }

                // now I can go through each and every reference
                SmallMap smallMap = new(mapRef);

                // we make a map that allows us to map the _location and Floor number to the small map with 
                // details such as the grid
                if (!_mapLocationDictionary.ContainsKey(mapRef.MapLocation))
                    _mapLocationDictionary.Add(mapRef.MapLocation, new Dictionary<int, SmallMap>());
                _mapLocationDictionary[mapRef.MapLocation].Add(mapRef.Floor, smallMap);
            }
        }

        // TileReference GetCorrectStairTile(Point2D.Direction direction)
        // {
        //     bool bGoingUp = IsStairGoingUp(xy, out TileReference stairTileReference);
        //     return stairTileReference;

        // Point2D.Direction direction;
        // if (!bGoingUp)
        // {
        //     direction = GetStairsDirection(xy);
        // }
        // else
        // {
        //     direction = GetStairsDirection(xy);
        // }
        //
        // int nSpriteNum = -1;
        // switch (direction)
        // {
        //     case Point2D.Direction.Up:
        //         nSpriteNum = bGoingUp
        //             ? GameReferences.SpriteTileReferences.GetTileReferenceByName("StairsNorth").Index
        //             : GameReferences.SpriteTileReferences.GetTileReferenceByName("StairsNorth").Index;
        //         break;
        //     case Point2D.Direction.Down:
        //         nSpriteNum = bGoingUp
        //             ? GameReferences.SpriteTileReferences.GetTileReferenceByName("StairsSouth").Index
        //             : GameReferences.SpriteTileReferences.GetTileReferenceByName("StairsSouth").Index;
        //         break;
        //     case Point2D.Direction.Left:
        //         nSpriteNum = bGoingUp
        //             ? GameReferences.SpriteTileReferences.GetTileReferenceByName("StairsWest").Index
        //             : GameReferences.SpriteTileReferences.GetTileReferenceByName("StairsWest").Index;
        //         break;
        //     case Point2D.Direction.Right:
        //         nSpriteNum = bGoingUp
        //             ? GameReferences.SpriteTileReferences.GetTileReferenceByName("StairsEast").Index
        //             : GameReferences.SpriteTileReferences.GetTileReferenceByName("StairsEast").Index;
        //         break;
        //     case Point2D.Direction.None:
        //         break;
        //     default:
        //         throw new ArgumentOutOfRangeException(nameof(xy), @"Point2D is out of range for stairs check");
        // }
        //
        // return nSpriteNum;
        // }

        public bool DoStairsGoDown(SmallMapReferences.SingleMapReference.Location location, int nFloor,
            Point2D tilePos, out TileReference stairTileReference) =>
            !DoStairsGoUp(location, nFloor, tilePos, out stairTileReference);

        public bool DoStairsGoUp(SmallMapReferences.SingleMapReference.Location location, int nFloor, Point2D tilePos,
            out TileReference stairTileReference)
        {
            SmallMap currentFloorSmallMap = _mapLocationDictionary[location][nFloor];
            bool bHasLowerFloor = _mapLocationDictionary[location].ContainsKey(nFloor - 1);
            bool bHasHigherFloor = _mapLocationDictionary[location].ContainsKey(nFloor + 1);

            // is it a stair case?
            Debug.Assert(
                GameReferences.SpriteTileReferences.IsStaircase(currentFloorSmallMap.TheMap[tilePos.X][tilePos.Y]));
            // is it the bottom or top floor? if so, then we know
            if (!bHasLowerFloor)
            {
                // no lower floor? then it's going up
                // we use the existing tile reference - it is fine
                stairTileReference =
                    GameReferences.SpriteTileReferences.GetTileReference(
                        currentFloorSmallMap.TheMap[tilePos.X][tilePos.Y]);
                return true;
            }

            // if no higher floor OR s there a stair case on the lower floor?
            if (!bHasHigherFloor)
            {
                // no higher floor? Definitely going down, so let's get the lower stair sprite
                stairTileReference =
                    GameReferences.SpriteTileReferences.GetTileReference(
                        _mapLocationDictionary[location][nFloor - 1].TheMap[tilePos.X][tilePos.Y]);
                return false;
            }

            // if the floor below has a staircase, then we know it's a down
            if (GameReferences.SpriteTileReferences.IsStaircase(
                    _mapLocationDictionary[location][nFloor - 1].TheMap[tilePos.X][tilePos.Y]))
            {
                stairTileReference =
                    GameReferences.SpriteTileReferences.GetTileReference(
                        _mapLocationDictionary[location][nFloor - 1].TheMap[tilePos.X][tilePos.Y]);

                return false;
            }
            // stairTileReference =
            //     GameReferences.SpriteTileReferences.GetTileReference(
            //         currentFloorSmallMap.TheMap[tilePos.X][tilePos.Y]);

            // // is there a stair case on the lower floor?
            // if (GameReferences.SpriteTileReferences.IsStaircase(
            //         _mapLocationDictionary[location][nFloor - 1].TheMap[tilePos.X][tilePos.Y]))
            // {
            //     return false;
            // }
            // is there a stair case on the upper floor?
            // if (GameReferences.SpriteTileReferences.IsStaircase(

            stairTileReference =
                GameReferences.SpriteTileReferences.GetTileReference(
                    currentFloorSmallMap.TheMap[tilePos.X][tilePos.Y]);
            return true;
            // if not - then WTF?
            // throw new Ultima5ReduxException("There is staircase with apparently no matching stair case");
        }

        public SmallMap GetSmallMap(SmallMapReferences.SingleMapReference.Location location, int nFloor) =>
            _mapLocationDictionary[location][nFloor];
    }
}