using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.Serialization;
using Newtonsoft.Json;
using Ultima5Redux.External;
using Ultima5Redux.MapUnits;
using Ultima5Redux.MapUnits.CombatMapUnits;
using Ultima5Redux.MapUnits.NonPlayerCharacters;
using Ultima5Redux.MapUnits.TurnResults;
using Ultima5Redux.References;
using Ultima5Redux.References.Maps;

namespace Ultima5Redux.Maps
{
    [DataContract]
    public sealed class SmallMap : RegularMap
    {
        public const int X_TILES = 32;
        public const int Y_TILES = 32;

        [IgnoreDataMember] private SmallMaps _smallMaps;

        [IgnoreDataMember]
        public override SmallMapReferences.SingleMapReference CurrentSingleMapReference =>
            _currentSingleMapReference ??=
                GameReferences.Instance.SmallMapRef.GetSingleMapByLocation(MapLocation, MapFloor);


        public SmallMaps GetSmallMaps() => _smallMaps;

        [IgnoreDataMember] public override bool IsRepeatingMap => false;

        [IgnoreDataMember] public override int NumOfXTiles => CurrentSingleMapReference.XTiles;
        [IgnoreDataMember] public override int NumOfYTiles => CurrentSingleMapReference.YTiles;

        [IgnoreDataMember]
        [SuppressMessage("ReSharper", "UnusedMember.Global")]
        public bool IsBasement
        {
            get
            {
                if (CurrentSingleMapReference == null) return false;

                return MapFloor == -1;
            }
        }

        [IgnoreDataMember] public bool ShowOuterSmallMapTiles => true;

        [IgnoreDataMember] public override byte[][] TheMap { get; protected set; }

        private SmallMapReferences.SingleMapReference _currentSingleMapReference;

        public override Maps TheMapType => Maps.Small;

        [JsonConstructor]
        private SmallMap()
        {
        }


        /// <summary>
        ///     Creates a small map object using a pre-defined map reference
        /// </summary>
        /// <param name="singleSmallMapReference"></param>
        public SmallMap(SmallMapReferences.SingleMapReference singleSmallMapReference) : base(
            singleSmallMapReference.MapLocation, singleSmallMapReference.Floor) =>
            // load the map into memory
            TheMap = CurrentSingleMapReference.GetDefaultMap();

        [OnDeserialized]
        private void PostDeserialize(StreamingContext context)
        {
            TheMap = CurrentSingleMapReference.GetDefaultMap();
        }

        /// <summary>
        ///     Checks if a tile is in bounds of the actual map and not a border tile
        /// </summary>
        /// <param name="position">position within the virtual map</param>
        /// <returns></returns>
        internal static bool IsInBounds(Point2D position)
        {
            // determine if the x or y coordinates are in bounds, if they are out of bounds and the map does not repeat
            // then we are going to draw a default texture on the outside areas.
            bool xInBounds = position.X is >= 0 and < X_TILES;
            bool yInBounds = position.Y is >= 0 and < Y_TILES;

            // fill outside of the bounds with a default tile
            return xInBounds && yInBounds;
        }

        internal override WalkableType GetWalkableTypeByMapUnit(MapUnit mapUnit)
        {
            return mapUnit switch
            {
                Enemy enemy => enemy.EnemyReference.IsWaterEnemy
                    ? WalkableType.CombatWater
                    : WalkableType.StandardWalking,
                CombatPlayer => WalkableType.StandardWalking,
                _ => WalkableType.StandardWalking
            };
        }

        internal override void ProcessTileEffectsForMapUnit(TurnResults turnResults, MapUnit mapUnit)
        {
            // TBD
        }


        internal void ClearSmallMapFlags()
        {
            IsWantedManByThePoPo = false;
            DeclinedExtortion = false;

            // re-add all dead rats back
            foreach (NonPlayerCharacter mapUnit in CurrentMapUnits.NonPlayerCharacters
                         .Where(m => m.KeyTileReference.Index == (int)TileReference.SpriteIndex.Rat_KeyIndex)!)
            {
                mapUnit.NpcState.IsDead = false;
            }

            // Gargoyles may have overriden AI when the avatar gets too close them 
            foreach (NonPlayerCharacter mapUnit in CurrentMapUnits.NonPlayerCharacters.Where(m =>
                         m.KeyTileReference.Index == (int)TileReference.SpriteIndex.StoneGargoyle_KeyIndex)!)
            {
                mapUnit.NpcState.UnsetOverridenAi();
            }

            // as we exit the Small Map we must make sure we reset the extorted flag 
            // most maps only have one jerky guard, but Blackthorn's is crawling with them
            // and when you give them your password, they tend to leave you alone after unless
            // you engage in conversation with them
            bool? _ = CurrentMapUnits.NonPlayerCharacters.All(
                n => n.NpcState.HasExtortedAvatar = false);
        }

        /// <summary>
        ///     Gets the best possible stair or ladder location
        ///     to go to the destinedPosition
        ///     Ladder/Stair -> destinedPosition
        /// </summary>
        /// <param name="ladderOrStairDirection">go up or down a ladder/stair</param>
        /// <param name="destinedPosition">the position to go to</param>
        /// <returns></returns>
        internal List<Point2D> GetBestStairsAndLadderLocation(VirtualMap.LadderOrStairDirection ladderOrStairDirection,
            Point2D destinedPosition)
        {
            // get all ladder and stairs locations based (only up or down ladders/stairs)
            List<Point2D> allLaddersAndStairList = GetListOfAllLaddersAndStairs(ladderOrStairDirection);

            // get an ordered dictionary of the shortest straight line paths
            SortedDictionary<double, Point2D> sortedPoints = GetShortestPaths(allLaddersAndStairList, destinedPosition);

            // ordered list of the best choice paths (only valid paths) 
            List<Point2D> bestChoiceList = new(sortedPoints.Count);

            // to make it more familiar, we will transfer to an ordered list
            // ReSharper disable once ForeachCanBeConvertedToQueryUsingAnotherGetEnumerator
            foreach (Point2D xy in sortedPoints.Values)
            {
                bool bPathBuilt = GetTotalMovesToLocation(destinedPosition, xy, WalkableType.StandardWalking) > 0;
                // we first make sure that the path even exists before we add it to the list
                if (bPathBuilt) bestChoiceList.Add(xy);
            }

            return bestChoiceList;
        }

        /// <summary>
        ///     Gets the best possible stair or ladder locations from the current position to the given ladder/stair direction
        ///     currentPosition -> best ladder/stair
        /// </summary>
        /// <param name="ladderOrStairDirection">which direction will we try to get to</param>
        /// <param name="destinedPosition">the position you are trying to get to</param>
        /// <param name="currentPosition">the current position of the character</param>
        /// <returns></returns>
        [SuppressMessage("ReSharper", "ForeachCanBeConvertedToQueryUsingAnotherGetEnumerator")]
        internal List<Point2D> getBestStairsAndLadderLocationBasedOnCurrentPosition(
            VirtualMap.LadderOrStairDirection ladderOrStairDirection, Point2D destinedPosition, Point2D currentPosition)
        {
            // get all ladder and stairs locations based (only up or down ladders/stairs)
            List<Point2D> allLaddersAndStairList = GetListOfAllLaddersAndStairs(ladderOrStairDirection);

            // get an ordered dictionary of the shortest straight line paths
            SortedDictionary<double, Point2D> sortedPoints = GetShortestPaths(allLaddersAndStairList, destinedPosition);

            // ordered list of the best choice paths (only valid paths) 
            List<Point2D> bestChoiceList = new(sortedPoints.Count);

            // to make it more familiar, we will transfer to an ordered list
            foreach (Point2D xy in sortedPoints.Values)
            {
                bool bPathBuilt = GetTotalMovesToLocation(currentPosition, xy, WalkableType.StandardWalking) > 0;
                // we first make sure that the path even exists before we add it to the list
                if (bPathBuilt) bestChoiceList.Add(xy);
            }

            return bestChoiceList;
        }

        /// <summary>
        ///     Gets the appropriate out of bounds sprite based on the map
        /// </summary>
        /// <returns></returns>
        [SuppressMessage("ReSharper", "UnusedParameter.Global")]
        internal int GetOutOfBoundsSprite(in Point2D position)
        {
            byte currentSingleMapReferenceId = CurrentSingleMapReference.Id;
            return currentSingleMapReferenceId switch
            {
                // sin vraal - desert
                (int)SmallMapReferences.SingleMapReference.Location.SinVraals_Hut => (int)TileReference.SpriteIndex
                    .Desert1,
                // sutek or grendal
                (int)SmallMapReferences.SingleMapReference.Location.Suteks_Hut
                    or (int)SmallMapReferences.SingleMapReference.Location.Grendels_Hut => (int)TileReference.SpriteIndex.Swamp,
                // stonegate
                (int)SmallMapReferences.SingleMapReference.Location.Stonegate => 11,
                _ => (int)TileReference.SpriteIndex.Grass
            };
        }

        /// <summary>
        ///     Given the orientation of the stairs, it returns the correct sprite to display
        /// </summary>
        /// <param name="xy">position of stairs</param>
        /// <returns>stair sprite</returns>
        internal TileReference GetStairsSprite(in Point2D xy)
        {
            bool _ = IsStairGoingUp(xy, out TileReference stairTileReference);
            return stairTileReference;
        }

        /// <summary>
        ///     Returns the total number of moves to the number of moves for the character to reach a point
        /// </summary>
        /// <param name="currentXy"></param>
        /// <param name="targetXy">where the character would move</param>
        /// <param name="walkableType"></param>
        /// <returns>the number of moves to the targetXy</returns>
        /// <remarks>This is expensive, and would be wonderful if we had a better way to get this info</remarks>
        internal int GetTotalMovesToLocation(Point2D currentXy, Point2D targetXy, WalkableType walkableType)
        {
            Stack<Node> nodeStack = GetAStarMap(walkableType).FindPath(currentXy, targetXy);
            return nodeStack?.Count ?? 0;
        }

        /// <summary>
        ///     Some logic has to be processed afterwards and requires special conditions
        /// </summary>
        /// <exception cref="Ultima5ReduxException"></exception>
        internal void HandleSpecialCasesForSmallMapLoad()
        {
            // ReSharper disable once InvertIf
            if (CurrentSingleMapReference.MapLocation ==
                SmallMapReferences.SingleMapReference.Location.Iolos_Hut
                && GameStateReference.State.TheTimeOfDay.IsFirstDay())
            {
                // this is a bit redundant, but left in just in case
                // it it is the first day then we don't include Smith or the Rats. Let's start the day off
                // on a positive note!
                NonPlayerCharacter smith = CurrentMapUnits.NonPlayerCharacters.FirstOrDefault(m =>
                                               (TileReference.SpriteIndex)m.NpcRef.NPCKeySprite is TileReference.SpriteIndex.HorseLeft
                                               or TileReference.SpriteIndex.HorseRight) ??
                                           throw new Ultima5ReduxException("Smith was not in Iolo's hut");

                CurrentMapUnits.ClearMapUnit(smith);
                CurrentMapUnits.AllMapUnits.RemoveAll(m => m is NonPlayerCharacter);
            }
        }


        // small map
        /// <summary>
        ///     Resets the current map to a default state - typically no monsters and NPCs in there default positions
        /// </summary>
        [SuppressMessage("ReSharper", "ForeachCanBePartlyConvertedToQueryUsingAnotherGetEnumerator")]
        internal void InitializeFromLegacy(SmallMaps smallMaps, SmallMapReferences.SingleMapReference.Location location,
            ImportedGameState importedGameState,
            bool bInitialLegacyLoad, SearchItems searchItems)
        {
            // save this so we can check other floors for stairs and ladders
            _smallMaps = smallMaps;

            if (location is SmallMapReferences.SingleMapReference.Location.Combat_resting_shrine
                or SmallMapReferences.SingleMapReference.Location.Britannia_Underworld)
                throw new Ultima5ReduxException("Tried to load " + location + " into a small map");

            // populate each of the map characters individually
            for (int i = 0; i < MAX_MAP_CHARACTERS; i++)
            {
                var mapUnitMovement = new MapUnitMovement(i);

                switch (i)
                {
                    // if it is the first index, then it's the Avatar - but if it's the initial load
                    // then it will just load from disk, otherwise we need to create a stub
                    case 0 when !bInitialLegacyLoad:
                        mapUnitMovement.ClearMovements();
                        // load the existing AvatarMapUnit with boarded MapUnits
                        var avatar = Avatar.CreateAvatar(location, new MapUnitMovement(i), new MapUnitPosition(),
                            GameReferences.Instance.SpriteTileReferences.GetTileReference(284),
                            true);
                        CurrentMapUnits.Add(avatar);
                        GetAvatarMapUnit().MapLocation = location;
                        continue;
                    // The zero position is always Avatar, this grabs them from the legacy save file 
                    case 0:
                    {
                        MapUnitState theAvatarMapState =
                            importedGameState.GetMapUnitStatesByMap(Maps.Small).GetCharacterState(0);
                        MapUnit theAvatar = Avatar.CreateAvatar(location, mapUnitMovement,
                            new MapUnitPosition(theAvatarMapState.X, theAvatarMapState.Y, theAvatarMapState.Floor),
                            theAvatarMapState.Tile1Ref, UseExtendedSprites);
                        CurrentMapUnits.Add(theAvatar);
                        continue;
                    }
                }

                // we have extended the max characters from 32 to 64 - BUT - we have to make sure we don't
                // try to index into the legacy array if we are index 32+
                bool bIsInExtendedNpcArea = i >= MAX_LEGACY_MAP_CHARACTERS;

                if (bIsInExtendedNpcArea)
                {
                    var emptyUnit = new EmptyMapUnit();
                    CurrentMapUnits.Add(emptyUnit);
                    continue;
                }

                // get the specific NPC reference 
                NonPlayerCharacterState npcState =
                    GameStateReference.State.TheNonPlayerCharacterStates.GetStateByLocationAndIndex(location, i);

                // we keep the object because we may be required to save this to disk - but since we are
                // leaving the map there is no need to save their movements
                mapUnitMovement.ClearMovements();

                // set a default SmallMapCharacterState based on the given NPC
                bool bInitialMapAndSmall = bInitialLegacyLoad && importedGameState.InitialMap == Maps.Small;

                // if it's an initial load we use the imported state, otherwise we assume it's fresh and new
                SmallMapCharacterState smallMapCharacterState = bInitialMapAndSmall
                    ? importedGameState.SmallMapCharacterStates.GetCharacterState(i)
                    : new SmallMapCharacterState(npcState.NpcRef, i);

                MapUnit mapUnit = CreateNewMapUnit(mapUnitMovement, false, location, npcState,
                    smallMapCharacterState.TheMapUnitPosition,
                    GameReferences.Instance.SpriteTileReferences.GetTileReference(npcState.NpcRef.NPCKeySprite),
                    smallMapCharacterState);

                // I want to be able to do this - but if I do then no new map units are created...
                // I used the original game logic of limiting to 32 entities, which is probably the correct
                // thing to do
                //if (mapUnit is EmptyMapUnit) continue;

                CurrentMapUnits.Add(mapUnit);
            }

            int nFloors = GameReferences.Instance.SmallMapRef.GetNumberOfFloors(location);
            bool bHasBasement = GameReferences.Instance.SmallMapRef.HasBasement(location);
            int nTopFloor = bHasBasement ? nFloors - 2 : nFloors - 1;

            for (int nFloor = bHasBasement ? -1 : 0; nFloor <= nTopFloor; nFloor++)
            {
                Dictionary<Point2D, List<SearchItem>> searchItemsInMap =
                    searchItems.GetUnDiscoveredSearchItemsByLocation(
                        location, nFloor);
                foreach (KeyValuePair<Point2D, List<SearchItem>> kvp in searchItemsInMap)
                {
                    MapUnitPosition mapUnitPosition = new(kvp.Key.X, kvp.Key.Y, nFloor);
                    // at this point we are cycling through the positions
                    foreach (SearchItem searchItem in kvp.Value)
                    {
                        // TEMPORARY FIX: you are only supposed to discover one discoverable loot at a time
                        List<SearchItem> searchItemInList = new() { searchItem };
                        var discoverableLoot = new DiscoverableLoot(location, mapUnitPosition, searchItemInList);
                        CurrentMapUnits.AddMapUnit(discoverableLoot);
                    }
                }
            }
        }

        internal bool IsAvatarSitting() => TileReferences.IsChair(GetTileReferenceOnCurrentTile().Index);

        /// <summary>
        ///     This is a lightweight LoadSmallMap. It is used specifically when re-populating the NPCStates after
        ///     a deserialize.
        /// </summary>
        /// <param name="location"></param>
        /// <exception cref="Ultima5ReduxException"></exception>
        internal void ReloadNpcData(SmallMapReferences.SingleMapReference.Location location)
        {
            for (int i = 1; i < CurrentMapUnits.AllMapUnits.Count; i++)
            {
                MapUnit mapUnit = CurrentMapUnits.AllMapUnits[i];
                // ReSharper disable once ConvertIfStatementToSwitchStatement
                if (mapUnit is EmptyMapUnit or DiscoverableLoot) continue;

                if (mapUnit is DeadBody or BloodSpatter or Chest or Horse or MagicCarpet or ItemStack &&
                    mapUnit.NpcRef == null) continue;
                if (mapUnit.NpcRef == null)
                    throw new Ultima5ReduxException($"Expected NPCRef for MapUnit {mapUnit.GetType()}");
                if (mapUnit.NpcRef.DialogIndex == -1) continue;

                // get the specific NPC reference 
                NonPlayerCharacterState npcState =
                    GameStateReference.State.TheNonPlayerCharacterStates.GetStateByLocationAndIndex(location,
                        mapUnit.NpcRef.DialogIndex);

                mapUnit.NpcState = npcState;
            }
        }

        internal void SetSmallMaps(SmallMaps smallMaps)
        {
            Debug.Assert(smallMaps != null);
            _smallMaps = smallMaps;
        }

        /// <summary>
        ///     Gets the shortest path between a list of
        /// </summary>
        /// <param name="positionList">list of positions</param>
        /// <param name="destinedPosition">the destination position</param>
        /// <returns>an ordered directory list of paths based on the shortest path (straight line path)</returns>
        private static SortedDictionary<double, Point2D> GetShortestPaths(List<Point2D> positionList,
            in Point2D destinedPosition)
        {
            SortedDictionary<double, Point2D> sortedPoints = new();

            // get the distances and add to the sorted dictionary
            foreach (Point2D xy in positionList)
            {
                double dDistance = destinedPosition.DistanceBetween(xy);
                // make them negative so they sort backwards

                // if the distance is the same then we just add a bit to make sure there is no conflict
                while (sortedPoints.ContainsKey(dDistance)) dDistance += 0.0000001;

                sortedPoints.Add(dDistance, xy);
            }

            return sortedPoints;
        }

        /// <summary>
        ///     Gets a list of points for all stairs and ladders
        /// </summary>
        /// <param name="ladderOrStairDirection">direction of all stairs and ladders</param>
        /// <returns></returns>
        private List<Point2D> GetListOfAllLaddersAndStairs(VirtualMap.LadderOrStairDirection ladderOrStairDirection)
        {
            List<Point2D> laddersAndStairs = new();

            // go through every single tile on the map looking for ladders and stairs
            for (int x = 0; x < X_TILES; x++)
            {
                for (int y = 0; y < Y_TILES; y++)
                {
                    TileReference tileReference = GetTileReference(x, y);
                    if (ladderOrStairDirection == VirtualMap.LadderOrStairDirection.Down)
                    {
                        // if this is a ladder or staircase and it's in the right direction, then add it to the list
                        if (TileReferences.IsLadderDown(tileReference.Index) ||
                            IsStairGoingDown(new Point2D(x, y), out _))
                            laddersAndStairs.Add(new Point2D(x, y));
                    }
                    else // otherwise we know you are going up
                    {
                        if (TileReferences.IsLadderUp(tileReference.Index) ||
                            (TileReferences.IsStaircase(tileReference.Index) &&
                             IsStairGoingUp(new Point2D(x, y), out _)))
                            laddersAndStairs.Add(new Point2D(x, y));
                    }
                } // end y for
            }

            // end x for
            return laddersAndStairs;
        }

        /// <summary>
        ///     Gets the NPC you want to talk to in the given direction
        ///     If you are in front of a table then you can talk over top of it too
        /// </summary>
        /// <param name="direction"></param>
        /// <returns>the NPC or null if non are found</returns>
        public NonPlayerCharacter GetNpcToTalkTo(MapUnitMovement.MovementCommandDirection direction)
        {
            Point2D adjustedPosition = MapUnitMovement.GetAdjustedPos(CurrentPosition.XY, direction);

            var npc = GetSpecificMapUnitByLocation<NonPlayerCharacter>(adjustedPosition,
                CurrentSingleMapReference.Floor);

            if (npc != null) return npc;

            if (!GetTileReference(adjustedPosition).IsTalkOverable)
                return null;

            Point2D adjustedPosition2Away = MapUnitMovement.GetAdjustedPos(CurrentPosition.XY, direction, 2);

            return GetSpecificMapUnitByLocation<NonPlayerCharacter>(adjustedPosition2Away,
                CurrentSingleMapReference.Floor);
        }

        public bool IsNpcInBed(NonPlayerCharacter npc) =>
            GetTileReference(npc.MapUnitPosition.XY).Is(TileReference.SpriteIndex.LeftBed);

        /// <summary>
        ///     Are the stairs at the given position going down?
        ///     Be sure to check if they are stairs first
        /// </summary>
        /// <param name="xy"></param>
        /// <param name="stairTileReference"></param>
        /// <returns></returns>
        // ReSharper disable once MemberCanBePrivate.Global
        public bool IsStairGoingDown(in Point2D xy, out TileReference stairTileReference)
        {
            stairTileReference = null;
            if (!TileReferences.IsStaircase(GetTileReference(xy).Index))
                return false;
            bool bStairGoUp = _smallMaps.DoStairsGoUp(MapLocation, MapFloor, xy,
                out stairTileReference);
            return !bStairGoUp;
        }

        /// <summary>
        ///     Are the stairs at the given position going up?
        ///     Be sure to check if they are stairs first
        /// </summary>
        /// <param name="xy"></param>
        /// <param name="stairTileReference"></param>
        /// <returns></returns>
        [SuppressMessage("ReSharper", "MemberCanBePrivate.Global")]
        public bool IsStairGoingUp(in Point2D xy, out TileReference stairTileReference)
        {
            stairTileReference = null;
            if (!TileReferences.IsStaircase(GetTileReference(xy).Index))
                return false;

            bool bStairGoUp = _smallMaps.DoStairsGoUp(MapLocation, MapFloor, xy,
                out stairTileReference);
            return bStairGoUp;
        }

        /// <summary>
        ///     Are the stairs at the player characters current position going up?
        /// </summary>
        /// <returns></returns>
        // ReSharper disable once MemberCanBePrivate.Global
        [SuppressMessage("ReSharper", "OutParameterValueIsAlwaysDiscarded.Global")]
        public bool IsStairGoingUp(out TileReference stairTileReference) =>
            IsStairGoingUp(CurrentPosition.XY, out stairTileReference);

        /// <summary>
        ///     Are the stairs at the player characters current position going down?
        /// </summary>
        /// <returns></returns>
        [SuppressMessage("ReSharper", "UnusedMember.Global")]
        public bool IsStairsGoingDown(out TileReference stairTileReference) =>
            IsStairGoingDown(CurrentPosition.XY, out stairTileReference);

        /// <summary>
        ///     Calculates an appropriate A* weight based on the current tile as well as the surrounding tiles
        /// </summary>
        /// <param name="xy"></param>
        /// <returns></returns>
        protected override float GetAStarWeight(in Point2D xy)
        {
            bool isPreferredIndex(int nSprite) =>
                nSprite == GameReferences.Instance.SpriteTileReferences.GetTileReferenceByName("BrickFloor").Index ||
                GameReferences.Instance.SpriteTileReferences.IsPath(nSprite);

            const int fDefaultDeduction = 2;

            float fCost = 10;

            // we reduce the weight for the A* for each adjacent brick floor or path tile
            if (xy.X - 1 >= 0) fCost -= isPreferredIndex(TheMap[xy.X - 1][xy.Y]) ? fDefaultDeduction : 0;
            if (xy.X + 1 < CurrentSingleMapReference.XTiles)
                fCost -= isPreferredIndex(TheMap[xy.X + 1][xy.Y]) ? fDefaultDeduction : 0;
            if (xy.Y - 1 >= 0) fCost -= isPreferredIndex(TheMap[xy.X][xy.Y - 1]) ? fDefaultDeduction : 0;
            if (xy.Y + 1 < CurrentSingleMapReference.YTiles)
                fCost -= isPreferredIndex(TheMap[xy.X][xy.Y + 1]) ? fDefaultDeduction : 0;

            return fCost;
        }
    }
}