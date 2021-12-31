using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.Serialization;
using Newtonsoft.Json;
using Ultima5Redux.External;
using Ultima5Redux.MapUnits;
using Ultima5Redux.MapUnits.CombatMapUnits;
using Ultima5Redux.MapUnits.NonPlayerCharacters;
using Ultima5Redux.MapUnits.SeaFaringVessels;
using Ultima5Redux.PlayerCharacters;
using Ultima5Redux.PlayerCharacters.Inventory;
using Ultima5Redux.References;
using Ultima5Redux.References.Maps;
using Ultima5Redux.References.MapUnits.NonPlayerCharacters;

// ReSharper disable UnusedMember.Global

// ReSharper disable IdentifierTypo

namespace Ultima5Redux.Maps
{
    [DataContract] public class VirtualMap
    {
        internal enum LadderOrStairDirection { Up, Down }

        /// <summary>
        ///     Both underworld and overworld maps
        /// </summary>
        [DataMember(Name = "LargeMaps")]
        private readonly Dictionary<Map.Maps, LargeMap> _largeMaps = new Dictionary<Map.Maps, LargeMap>(2);

        /// <summary>
        ///     All the small maps
        /// </summary>
        [DataMember(Name = "SmallMaps")] private readonly SmallMaps _smallMaps;

        /// <summary>
        ///     Which map was the avatar on before this one?
        /// </summary>
        [DataMember]
        private RegularMap PreCombatMap { get; set; }

        /// <summary>
        ///     The position of the Avatar from the last place he came from (ie. on a small map, from a big map)
        /// </summary>
        [DataMember]
        private MapUnitPosition PreMapUnitPosition { get; set; } = new MapUnitPosition();

        [DataMember] private MapOverrides PreTheMapOverrides { get; set; }

        [DataMember] private MapOverrides TheMapOverrides { get; set; }

        /// <summary>
        ///     Detailed reference of current small map
        /// </summary>
        [DataMember]
        public SmallMapReferences.SingleMapReference CurrentSingleMapReference
        {
            get
            {
                if (_currentSingleMapReference == null)
                    return null;
                if (_currentSingleMapReference.MapLocation ==
                    SmallMapReferences.SingleMapReference.Location.Combat_resting_shrine)
                    return SmallMapReferences.SingleMapReference.GetCombatMapSingleInstance();
                return _currentSingleMapReference;
            }
            private set => _currentSingleMapReference = value;
        }

        /// <summary>
        ///     The current small map (null if on large map)
        /// </summary>
        // ReSharper disable once MemberCanBePrivate.Global
        [DataMember]
        public SmallMap CurrentSmallMap { get; private set; }

        /// <summary>
        ///     If we are on a large map - then are we on overworld or underworld
        /// </summary>
        [DataMember]
        public Map.Maps LargeMapOverUnder { get; private set; } = (Map.Maps)(-1);

        [DataMember] public MapUnits.MapUnits TheMapUnits { get; private set; }

        [IgnoreDataMember] private SmallMapReferences.SingleMapReference _currentSingleMapReference;

        /// <summary>
        ///     The abstracted Map object for the current map
        ///     Returns large or small depending on what is active
        /// </summary>
        [IgnoreDataMember]
        public Map CurrentMap
        {
            get
            {
                if (CurrentSingleMapReference == null)
                    throw new Ultima5ReduxException("No single map is set in virtual map");

                switch (CurrentSingleMapReference.MapLocation)
                {
                    case SmallMapReferences.SingleMapReference.Location.Combat_resting_shrine:
                        return CurrentCombatMap;
                    case SmallMapReferences.SingleMapReference.Location.Britannia_Underworld:
                        return CurrentSingleMapReference.Floor == 0 ? OverworldMap : UnderworldMap;
                    default:
                        return CurrentSmallMap;
                }
            }
        }

        [IgnoreDataMember] public bool IsAvatarInFrigate => TheMapUnits.AvatarMapUnit.CurrentBoardedMapUnit is Frigate;
        [IgnoreDataMember] public bool IsAvatarInSkiff => TheMapUnits.AvatarMapUnit.CurrentBoardedMapUnit is Skiff;

        [IgnoreDataMember]
        public bool IsAvatarRidingCarpet =>
            TheMapUnits.AvatarMapUnit.CurrentBoardedMapUnit is MagicCarpet;

        [IgnoreDataMember] public bool IsAvatarRidingHorse => TheMapUnits.AvatarMapUnit.CurrentBoardedMapUnit is Horse;
        [IgnoreDataMember] public bool IsAvatarRidingSomething => TheMapUnits.AvatarMapUnit.IsAvatarOnBoardedThing;

        [IgnoreDataMember]
        public bool IsBasement
        {
            get
            {
                if (CurrentSingleMapReference == null)
                    throw new Ultima5ReduxException("No single map is set in virtual map");

                return !IsLargeMap && CurrentSingleMapReference.Floor == -1;
            }
        }

        [IgnoreDataMember] public bool IsCombatMap => CurrentMap is CombatMap;

        /// <summary>
        ///     Are we currently on a large map?
        /// </summary>
        [IgnoreDataMember]
        public bool IsLargeMap => LargeMapOverUnder != Map.Maps.Small; //{ get; private set; }

        //set => TheMapUnits.CurrentAvatarPosition = value;
        /// <summary>
        ///     Number of total columns for current map
        /// </summary>
        [IgnoreDataMember]
        public int NumberOfColumnTiles => CurrentMap.NumOfXTiles;

        /// <summary>
        ///     Number of total rows for current map
        /// </summary>
        [IgnoreDataMember]
        public int NumberOfRowTiles => CurrentMap.NumOfYTiles;

        /// <summary>
        ///     The persistant overworld map
        /// </summary>
        [IgnoreDataMember]
        public LargeMap OverworldMap => _largeMaps[Map.Maps.Overworld];

        /// <summary>
        ///     The persistant underworld map
        /// </summary>
        [IgnoreDataMember]
        public LargeMap UnderworldMap => _largeMaps[Map.Maps.Underworld];

        [IgnoreDataMember] public CombatMap CurrentCombatMap { get; private set; }

        /// <summary>
        ///     Current large map (null if on small map)
        /// </summary>
        // ReSharper disable once MemberCanBePrivate.Global
        [IgnoreDataMember]
        public LargeMap CurrentLargeMap { get; private set; }

        [IgnoreDataMember]
        public MapUnitPosition CurrentPosition
        {
            get
            {
                if (CurrentMap is CombatMap combatMap)
                {
                    return combatMap.CurrentCombatMapUnit?.MapUnitPosition ??
                           throw new Ultima5ReduxException("Combat map was unexpectedly null");
                }

                return TheMapUnits.CurrentAvatarPosition;
            }
            set => TheMapUnits.CurrentAvatarPosition = value;
        }

        /// <summary>
        ///     Construct the VirtualMap (requires initialization still)
        /// </summary>
        /// <param name="smallMaps"></param>
        /// <param name="overworldMap"></param>
        /// <param name="underworldMap"></param>
        /// <param name="initialMap"></param>
        /// <param name="currentSmallMapReference"></param>
        /// <param name="bUseExtendedSprites"></param>
        /// <param name="importedGameState"></param>
        internal VirtualMap(SmallMaps smallMaps, LargeMap overworldMap, LargeMap underworldMap, Map.Maps initialMap,
            SmallMapReferences.SingleMapReference currentSmallMapReference, bool bUseExtendedSprites,
            ImportedGameState importedGameState)
        {
            _smallMaps = smallMaps;
            _largeMaps.Add(Map.Maps.Overworld, overworldMap);
            _largeMaps.Add(Map.Maps.Underworld, underworldMap);

            SmallMapReferences.SingleMapReference.Location mapLocation = currentSmallMapReference?.MapLocation ??
                                                                         SmallMapReferences.SingleMapReference.Location
                                                                             .Britannia_Underworld;

            // load the characters for the very first time from disk
            // subsequent loads may not have all the data stored on disk and will need to recalculate
            TheMapUnits = new MapUnits.MapUnits(initialMap, bUseExtendedSprites, importedGameState, mapLocation);

            switch (initialMap)
            {
                case Map.Maps.Small:
                    LoadSmallMap(currentSmallMapReference, null, true);
                    break;
                case Map.Maps.Overworld:
                case Map.Maps.Underworld:
                    LoadLargeMap(initialMap);
                    break;
                case Map.Maps.Combat:
                    throw new Ultima5ReduxException("Can't load a Combat Map on the initialization of a virtual map");
                default:
                    throw new ArgumentOutOfRangeException(nameof(initialMap), initialMap, null);
            }
        }

        [JsonConstructor] private VirtualMap()
        {
        }

        /// <summary>
        ///     Gets the best possible stair or ladder location
        ///     to go to the destinedPosition
        ///     Ladder/Stair -> destinedPosition
        /// </summary>
        /// <param name="ladderOrStairDirection">go up or down a ladder/stair</param>
        /// <param name="destinedPosition">the position to go to</param>
        /// <returns></returns>
        internal List<Point2D> GetBestStairsAndLadderLocation(LadderOrStairDirection ladderOrStairDirection,
            Point2D destinedPosition)
        {
            // get all ladder and stairs locations based (only up or down ladders/stairs)
            List<Point2D> allLaddersAndStairList = GetListOfAllLaddersAndStairs(ladderOrStairDirection);

            // get an ordered dictionary of the shortest straight line paths
            SortedDictionary<double, Point2D> sortedPoints = GetShortestPaths(allLaddersAndStairList, destinedPosition);

            // ordered list of the best choice paths (only valid paths) 
            List<Point2D> bestChoiceList = new List<Point2D>(sortedPoints.Count);

            // to make it more familiar, we will transfer to an ordered list
            foreach (Point2D xy in sortedPoints.Values)
            {
                bool bPathBuilt = GetTotalMovesToLocation(destinedPosition, xy, Map.WalkableType.StandardWalking) > 0;
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
        internal List<Point2D> getBestStairsAndLadderLocationBasedOnCurrentPosition(
            LadderOrStairDirection ladderOrStairDirection, Point2D destinedPosition, Point2D currentPosition)
        {
            // get all ladder and stairs locations based (only up or down ladders/stairs)
            List<Point2D> allLaddersAndStairList = GetListOfAllLaddersAndStairs(ladderOrStairDirection);

            // get an ordered dictionary of the shortest straight line paths
            SortedDictionary<double, Point2D> sortedPoints = GetShortestPaths(allLaddersAndStairList, destinedPosition);

            // ordered list of the best choice paths (only valid paths) 
            List<Point2D> bestChoiceList = new List<Point2D>(sortedPoints.Count);

            // to make it more familiar, we will transfer to an ordered list
            foreach (Point2D xy in sortedPoints.Values)
            {
                bool bPathBuilt = GetTotalMovesToLocation(currentPosition, xy, Map.WalkableType.StandardWalking) > 0;
                // we first make sure that the path even exists before we add it to the list
                if (bPathBuilt) bestChoiceList.Add(xy);
            }

            return bestChoiceList;
        }

        /// <summary>
        ///     Returns the total number of moves to the number of moves for the character to reach a point
        /// </summary>
        /// <param name="currentXy"></param>
        /// <param name="targetXy">where the character would move</param>
        /// <param name="walkableType"></param>
        /// <returns>the number of moves to the targetXy</returns>
        /// <remarks>This is expensive, and would be wonderful if we had a better way to get this info</remarks>
        internal int GetTotalMovesToLocation(Point2D currentXy, Point2D targetXy, Map.WalkableType walkableType)
        {
            Stack<Node> nodeStack = CurrentMap.GetAStarByWalkableType(walkableType).FindPath(currentXy, targetXy);

            return nodeStack?.Count ?? 0;
        }

        /// <summary>
        ///     Gets a suitable random position when wandering
        /// </summary>
        /// <param name="characterPosition">position of character</param>
        /// <param name="scheduledPosition">scheduled position of the character</param>
        /// <param name="nMaxDistance">max number of tiles the wander can be from the scheduled position</param>
        /// <param name="direction">OUT - the direction that the character should travel</param>
        /// <returns></returns>
        internal Point2D GetWanderCharacterPosition(Point2D characterPosition, Point2D scheduledPosition,
            int nMaxDistance, out MapUnitMovement.MovementCommandDirection direction)
        {
            Random ran = new Random();
            List<MapUnitMovement.MovementCommandDirection> possibleDirections =
                GetPossibleDirectionsList(characterPosition, scheduledPosition, nMaxDistance, true);

            // if no directions are returned then we tell them not to move
            if (possibleDirections.Count == 0)
            {
                direction = MapUnitMovement.MovementCommandDirection.None;

                return characterPosition.Copy();
            }

            direction = possibleDirections[ran.Next() % possibleDirections.Count];

            Point2D adjustedPosition = MapUnitMovement.GetAdjustedPos(characterPosition, direction);

            return adjustedPosition;
        }

        internal bool IsTileFreeToTravel(Point2D xy, bool bNoStaircases = false) =>
            IsTileFreeToTravel(xy, bNoStaircases, TheMapUnits.AvatarMapUnit.CurrentAvatarState);

        internal bool IsTileFreeToTravel(Point2D xy, bool bNoStaircases, Avatar.AvatarState forcedAvatarState) =>
            IsTileFreeToTravel(CurrentPosition.XY, xy, bNoStaircases, forcedAvatarState);

        /// <summary>
        ///     Is the particular tile eligible to be moved onto
        /// </summary>
        /// <param name="currentPosition"></param>
        /// <param name="newPosition"></param>
        /// <param name="bNoStaircases"></param>
        /// <param name="forcedAvatarState"></param>
        /// <returns>true if you can move onto the tile</returns>
        internal bool IsTileFreeToTravel(Point2D currentPosition, Point2D newPosition, bool bNoStaircases,
            Avatar.AvatarState forcedAvatarState)
        {
            if (newPosition.X < 0 || newPosition.Y < 0) return false;

            bool bIsAvatarTile = currentPosition == newPosition;

            // get the regular tile reference AND get the map unit (NPC, frigate etc)
            // we need to evaluate both
            TileReference tileReference = GetTileReference(newPosition);
            MapUnit mapUnit = GetTopVisibleMapUnit(newPosition, true);

            // if we want to eliminate staircases as an option then we need to make sure it isn't a staircase
            // true indicates that it is walkable
            bool bStaircaseWalkable =
                !(bNoStaircases && GameReferences.SpriteTileReferences.IsStaircase(tileReference.Index));

            // if it's nighttime then the portcullises go down and you cannot pass
            bool bPortcullisDown = GameReferences.SpriteTileReferences.GetTileNumberByName("BrickWallArchway") ==
                tileReference.Index && !GameStateReference.State.TheTimeOfDay.IsDayLight;

            // we check both the tile reference below as well as the map unit that occupies the tile
            bool bIsWalkable;
            // if the MapUnit is null then we do a basic evaluation 
            if (mapUnit is null)
            {
                bIsWalkable = (tileReference.IsPassable(forcedAvatarState) && bStaircaseWalkable) && !bPortcullisDown;
            }
            else // otherwise we need to evaluate if the vehicle can moved to the tile
            {
                bIsWalkable = mapUnit.KeyTileReference.IsPassable(forcedAvatarState);
            }

            // there is not an NPC on the tile, it is walkable and the Avatar is not currently occupying it
            // return !bIsNpcTile && bIsWalkable && !bIsAvatarTile;
            return bIsWalkable && !bIsAvatarTile;
        }

        /// <summary>
        ///     Advances each of the NPCs by one movement each
        /// </summary>
        internal void MoveMapUnitsToNextMove()
        {
            // go through each of the NPCs on the map
            foreach (MapUnit mapUnit in TheMapUnits.CurrentMapUnits.AllActiveMapUnits)
            {
                mapUnit.CompleteNextMove(this, GameStateReference.State.TheTimeOfDay,
                    CurrentMap.GetAStarByMapUnit(mapUnit));
            }
        }

        internal int SearchAndExposeItems(Point2D xy)
        {
            // check for moonstones
            // moonstone check
            if (IsLargeMap && GameStateReference.State.TheMoongates.IsMoonstoneBuried(xy, LargeMapOverUnder) &&
                GameStateReference.State.TheTimeOfDay.IsDayLight)
            {
                MoonPhaseReferences.MoonPhases moonPhase =
                    GameStateReference.State.TheMoongates.GetMoonPhaseByPosition(xy, LargeMapOverUnder);
                InventoryItem invItem = GameStateReference.State.PlayerInventory.TheMoonstones.Items[moonPhase];
                TheMapOverrides.EnqueueSearchItem(xy, invItem);

                GameStateReference.State.TheMoongates.SetMoonstoneBuried((int)moonPhase, false);

                return 1;
            }

            return 0;
        }

        /// <summary>
        ///     Create a list of the free spaces surrounding around the Avatar suitable for something to be generated onto
        ///     Uses all 8 directions
        /// </summary>
        /// <returns></returns>
        private List<Point2D> GetFreeSpacesSurroundingAvatar()
        {
            List<Point2D> freeSpacesAroundAvatar = new List<Point2D>();

            for (int x = -1; x <= 1; x++)
            {
                for (int y = -1; y <= 1; y++)
                {
                    Point2D pointToCheck = new Point2D(Math.Max(CurrentPosition.X + x, 0),
                        Math.Max(CurrentPosition.Y + y, 0));
                    if (!IsMapUnitOccupiedTile(pointToCheck) && GetTileReference(pointToCheck).IsWalking_Passable)
                        freeSpacesAroundAvatar.Add(pointToCheck);
                }
            }

            return freeSpacesAroundAvatar;
        }

        /// <summary>
        ///     Gets a list of points for all stairs and ladders
        /// </summary>
        /// <param name="ladderOrStairDirection">direction of all stairs and ladders</param>
        /// <returns></returns>
        private List<Point2D> GetListOfAllLaddersAndStairs(LadderOrStairDirection ladderOrStairDirection)
        {
            List<Point2D> laddersAndStairs = new List<Point2D>();

            // go through every single tile on the map looking for ladders and stairs
            for (int x = 0; x < SmallMap.X_TILES; x++)
            {
                for (int y = 0; y < SmallMap.Y_TILES; y++)
                {
                    TileReference tileReference = GetTileReference(x, y);
                    if (ladderOrStairDirection == LadderOrStairDirection.Down)
                    {
                        // if this is a ladder or staircase and it's in the right direction, then add it to the list
                        if (GameReferences.SpriteTileReferences.IsLadderDown(tileReference.Index) ||
                            IsStairGoingDown(new Point2D(x, y)))
                            laddersAndStairs.Add(new Point2D(x, y));
                    }
                    else // otherwise we know you are going up
                    {
                        if (GameReferences.SpriteTileReferences.IsLadderUp(tileReference.Index) ||
                            GameReferences.SpriteTileReferences.IsStaircase(tileReference.Index) &&
                            IsStairGoingUp(new Point2D(x, y)))
                            laddersAndStairs.Add(new Point2D(x, y));
                    }
                } // end y for
            } // end x for

            return laddersAndStairs;
        }

        private Point2D GetPositionIfUserCanMove(MapUnitMovement.MovementCommandDirection direction,
            Point2D characterPosition, bool bNoStaircases, Point2D scheduledPosition, int nMaxDistance)
        {
            Point2D adjustedPosition = MapUnitMovement.GetAdjustedPos(characterPosition, direction);

            // always include none
            if (direction == MapUnitMovement.MovementCommandDirection.None) return adjustedPosition;

            if (adjustedPosition.X < 0 || adjustedPosition.X >= CurrentMap.TheMap.Length || adjustedPosition.Y < 0 ||
                adjustedPosition.Y >= CurrentMap.TheMap[0].Length) return null;

            // is the tile free to travel to? even if it is, is it within N tiles of the scheduled tile?
            if (IsTileFreeToTravel(adjustedPosition, bNoStaircases) &&
                scheduledPosition.IsWithinN(adjustedPosition, nMaxDistance)) return adjustedPosition;

            return null;
        }

        /// <summary>
        ///     Gets possible directions that are accessible from a particular point
        /// </summary>
        /// <param name="characterPosition">the current position of the character</param>
        /// <param name="scheduledPosition">the place they are supposed to be</param>
        /// <param name="nMaxDistance">max distance they can travel from that position</param>
        /// <param name="bNoStaircases"></param>
        /// <returns></returns>
        private List<MapUnitMovement.MovementCommandDirection> GetPossibleDirectionsList(Point2D characterPosition,
            Point2D scheduledPosition, int nMaxDistance, bool bNoStaircases)
        {
            List<MapUnitMovement.MovementCommandDirection> directionList =
                new List<MapUnitMovement.MovementCommandDirection>();

            // gets an adjusted position OR returns null if the position is not valid

            foreach (MapUnitMovement.MovementCommandDirection direction in Enum.GetValues(
                         typeof(MapUnitMovement.MovementCommandDirection)))
            {
                // we may be asked to avoid including .None in the list
                if (direction == MapUnitMovement.MovementCommandDirection.None) continue;

                Point2D adjustedPos = GetPositionIfUserCanMove(direction, characterPosition, bNoStaircases,
                    scheduledPosition, nMaxDistance);
                // if adjustedPos == null then the particular direction was not allowed for one reason or another
                if (adjustedPos != null) directionList.Add(direction);
            }

            return directionList;
        }

        /// <summary>
        ///     Gets the shortest path between a list of
        /// </summary>
        /// <param name="positionList">list of positions</param>
        /// <param name="destinedPosition">the destination position</param>
        /// <returns>an ordered directory list of paths based on the shortest path (straight line path)</returns>
        private SortedDictionary<double, Point2D> GetShortestPaths(List<Point2D> positionList, Point2D destinedPosition)
        {
            SortedDictionary<double, Point2D> sortedPoints = new SortedDictionary<double, Point2D>();

            // get the distances and add to the sorted dictionary
            foreach (Point2D xy in positionList)
            {
                double dDistance = destinedPosition.DistanceBetween(xy);
                // make them negative so they sort backwards

                // if the distance is the same then we just add a bit to make sure there is no conflict
                while (sortedPoints.ContainsKey(dDistance))
                {
                    dDistance += 0.0000001;
                }

                sortedPoints.Add(dDistance, xy);
            }

            return sortedPoints;
        }

        private bool IsTileFreeToTravelLocal(Point2D.Direction direction, Avatar.AvatarState avatarState) =>
            IsTileFreeToTravel(CurrentPosition.XY.GetAdjustedPosition(direction), true, avatarState);

        /// <summary>
        ///     Loads a combat map as the current map
        ///     Saves the previous map state, for post combat
        /// </summary>
        /// <param name="singleCombatMapReference"></param>
        /// <param name="entryDirection"></param>
        /// <param name="records"></param>
        /// <param name="primaryEnemyReference"></param>
        /// <param name="nPrimaryEnemies"></param>
        /// <param name="secondaryEnemyReference"></param>
        /// <param name="nSecondaryEnemies"></param>
        /// <param name="npcRef"></param>
        private void LoadCombatMap(SingleCombatMapReference singleCombatMapReference,
            SingleCombatMapReference.EntryDirection entryDirection, PlayerCharacterRecords records,
            EnemyReference primaryEnemyReference, int nPrimaryEnemies, EnemyReference secondaryEnemyReference,
            int nSecondaryEnemies, NonPlayerCharacterReference npcRef)
        {
            if (npcRef != null)
                Debug.Assert(nPrimaryEnemies == 1 && nSecondaryEnemies == 0,
                    "when assigning an NPC, you must have single enemy");

            // if the PreCombatMap is not set OR the existing map is not already a combat map then
            // we set the PreCombatMap so we know which map to return to
            if (PreCombatMap == null || !IsCombatMap)
            {
                Debug.Assert(CurrentMap is RegularMap,
                    "You can't load a combat map when you are already in a combat map");
                PreCombatMap = (RegularMap)CurrentMap;
                PreMapUnitPosition.Floor = CurrentPosition.Floor;
                PreMapUnitPosition.X = CurrentPosition.X;
                PreMapUnitPosition.Y = CurrentPosition.Y;
            }

            CurrentSingleMapReference = SmallMapReferences.SingleMapReference.GetCombatMapSingleInstance();

            CurrentCombatMap = new CombatMap(singleCombatMapReference);

            // we only want to push the exposed items and override map if we are on a small or large map 
            // not if we are going combat to combat map (think Debug)
            if (TheMapOverrides.NumOfRows > CurrentCombatMap.NumOfXTiles)
            {
                PreTheMapOverrides = TheMapOverrides;
            }

            TheMapOverrides = new MapOverrides(CurrentCombatMap);

            TheMapUnits.SetCurrentMapType(CurrentSingleMapReference, Map.Maps.Combat);
            LargeMapOverUnder = Map.Maps.Combat;

            CurrentCombatMap.CreateParty(entryDirection, records);

            CurrentCombatMap.CreateEnemies(singleCombatMapReference, primaryEnemyReference, nPrimaryEnemies,
                secondaryEnemyReference, nSecondaryEnemies, npcRef);

            CurrentCombatMap.InitializeInitiativeQueue();
        }

        public static Point2D GetLocationOfDock(SmallMapReferences.SingleMapReference.Location location)
        {
            List<byte> xDockCoords = GameReferences.DataOvlRef.GetDataChunk(DataOvlReference.DataChunkName.X_DOCKS)
                .GetAsByteList();
            List<byte> yDockCoords = GameReferences.DataOvlRef.GetDataChunk(DataOvlReference.DataChunkName.Y_DOCKS)
                .GetAsByteList();
            Dictionary<SmallMapReferences.SingleMapReference.Location, Point2D> docks =
                new Dictionary<SmallMapReferences.SingleMapReference.Location, Point2D>
                {
                    {
                        SmallMapReferences.SingleMapReference.Location.Jhelom,
                        new Point2D(xDockCoords[0], yDockCoords[0])
                    },
                    {
                        SmallMapReferences.SingleMapReference.Location.Minoc,
                        new Point2D(xDockCoords[1], yDockCoords[1])
                    },
                    {
                        SmallMapReferences.SingleMapReference.Location.East_Britanny,
                        new Point2D(xDockCoords[2], yDockCoords[2])
                    },
                    {
                        SmallMapReferences.SingleMapReference.Location.Buccaneers_Den,
                        new Point2D(xDockCoords[3], yDockCoords[3])
                    }
                };

            if (!docks.ContainsKey(location))
                throw new Ultima5ReduxException("Asked for a dock  in " + location + " but there isn't one there");

            return docks[location];
        }

        public bool ContainsSearchableThings(Point2D xy)
        {
            // moonstone check
            return IsLargeMap && GameStateReference.State.TheMoongates.IsMoonstoneBuried(xy, LargeMapOverUnder);
        }

        /// <summary>
        ///     Creates a horse MapUnit in the surrounding tiles of the Avatar - if one exists
        /// </summary>
        /// <returns>the new horse or null if there was no where to put it</returns>
        public Horse CreateHorseAroundAvatar()
        {
            List<Point2D> freeSpacesAroundAvatar = GetFreeSpacesSurroundingAvatar();
            if (freeSpacesAroundAvatar.Count <= 0) return null;

            Random ran = new Random();
            Point2D chosenLocation = freeSpacesAroundAvatar[ran.Next() % freeSpacesAroundAvatar.Count];
            Horse horse = TheMapUnits.CreateHorse(
                new MapUnitPosition(chosenLocation.X, chosenLocation.Y, CurrentPosition.Floor), LargeMapOverUnder,
                out int nIndex);

            if (nIndex == -1 || horse == null) return null;

            return horse;
        }

        public InventoryItem DequeuExposedSearchItems(Point2D xy) => TheMapOverrides.DequeueSearchItem(xy);

        public int GetCalculatedSpriteIndexByTile(TileReference tileReference, Point2D tilePosInMap, bool bIsAvatarTile,
            bool bIsMapUnitOccupiedTile, out bool bDrawCharacterOnTile)
        {
            int nSprite = tileReference.Index;
            bool bIsMirror = GameReferences.SpriteTileReferences.IsUnbrokenMirror(nSprite);
            bDrawCharacterOnTile = false;

            if (bIsMirror)
            {
                // if the avatar is south of the mirror then show his image
                Point2D expectedAvatarPos = new Point2D(tilePosInMap.X, tilePosInMap.Y + 1);
                if (expectedAvatarPos == CurrentPosition.XY)
                {
                    return GameReferences.SpriteTileReferences.GetTileNumberByName("MirrorAvatar");
                }
            }

            //bool bIsDoorTile = GameReferences.SpriteTileReferences.IsDoor(nSprite); // is it a door?
            // is the sprite a Chair? if so, we need to figure out if someone is sitting on it
            bool bIsChair = GameReferences.SpriteTileReferences.IsChair(nSprite);
            // bh: i should clean this up so that it doesn't need to call all this - since it's being called in GetCorrectSprite
            bool bIsLadder = GameReferences.SpriteTileReferences.IsLadder(nSprite);
            // is it the human sleeping side of the bed?
            bool bIsHeadOfBed = GameReferences.SpriteTileReferences.IsHeadOfBed(nSprite);
            // we need to check these before they get "corrected"
            // is it the stocks
            bool bIsStocks = GameReferences.SpriteTileReferences.IsStocks(nSprite);
            bool bIsManacles = GameReferences.SpriteTileReferences.IsManacles(nSprite); // is it shackles/manacles

            // this is unfortunate since I would prefer the GetCorrectSprite took care of all of this
            bool bIsFoodNearby = GameReferences.SpriteTileReferences.IsChair(nSprite) && IsFoodNearby(tilePosInMap);

            bool bIsStaircase = GameReferences.SpriteTileReferences.IsStaircase(nSprite); // is it a staircase

            int nNewSpriteIndex; //= nSprite;

            if (bIsStaircase)
            {
                nNewSpriteIndex = GetStairsSprite(tilePosInMap);
            }
            else
            {
                nNewSpriteIndex = GameReferences.SpriteTileReferences.GetCorrectSprite(nSprite, bIsMapUnitOccupiedTile,
                    bIsAvatarTile, bIsFoodNearby, GameStateReference.State.TheTimeOfDay.IsDayLight);
            }

            if (nNewSpriteIndex == -2)
            {
                nNewSpriteIndex = GuessTile(tilePosInMap);
            }

            bDrawCharacterOnTile = (!bIsChair && !bIsLadder && !bIsHeadOfBed && !bIsStocks && !bIsManacles) &&
                                   bIsMapUnitOccupiedTile;

            return nNewSpriteIndex;
        }

        public SingleCombatMapReference GetCombatMapReferenceByPosition(Point2D xy,
            SingleCombatMapReference.Territory territory)
        {
            TileReference tileReference = GetTileReference(xy);
            return GameReferences.CombatMapRefs.GetSingleCombatMapReference(territory,
                (int)tileReference.CombatMapIndex);
        }

        /// <summary>
        ///     Gets the Avatar's current position in 3D spaces
        /// </summary>
        /// <returns></returns>
        public Point3D GetCurrent3DPosition()
        {
            if (LargeMapOverUnder == Map.Maps.Small)
                return new Point3D(CurrentPosition.X, CurrentPosition.Y, CurrentSmallMap.MapFloor);

            return new Point3D(CurrentPosition.X, CurrentPosition.Y,
                LargeMapOverUnder == Map.Maps.Overworld ? 0 : 0xFF);
        }

        public IEnumerable<InventoryItem> GetExposedSearchItems(Point2D xy) =>
            TheMapOverrides.GetExposedSearchItems(xy);

        /// <summary>
        ///     Gets a map unit on the current tile (that ISN'T the Avatar)
        /// </summary>
        /// <returns>MapUnit or null if none exist</returns>
        public MapUnit GetMapUnitOnCurrentTile()
        {
            return GetTopVisibleMapUnit(CurrentPosition.XY, true);
        }

        /// <summary>
        ///     If an NPC is on a tile, then it will get them
        ///     assumes it's on the same floor
        /// </summary>
        /// <param name="xy"></param>
        /// <returns>the NPC or null if one does not exist</returns>
        public List<MapUnit> GetMapUnitOnTile(Point2D xy)
        {
            if (CurrentSingleMapReference == null)
                throw new Ultima5ReduxException("No single map is set in virtual map");

            List<MapUnit> mapUnits =
                TheMapUnits.GetMapUnitByLocation(LargeMapOverUnder, xy, CurrentSingleMapReference.Floor);

            return mapUnits;
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

            if (CurrentSingleMapReference == null)
                throw new Ultima5ReduxException("No single map is set in virtual map");

            NonPlayerCharacter npc = TheMapUnits.GetSpecificMapUnitByLocation<NonPlayerCharacter>(LargeMapOverUnder,
                adjustedPosition, CurrentSingleMapReference.Floor);

            if (npc != null) return npc;

            if (!GetTileReference(adjustedPosition).IsTalkOverable)
                return null;

            Point2D adjustedPosition2Away = MapUnitMovement.GetAdjustedPos(CurrentPosition.XY, direction, 2);

            if (CurrentSingleMapReference == null)
                throw new Ultima5ReduxException("No single map is set in virtual map");

            return TheMapUnits.GetSpecificMapUnitByLocation<NonPlayerCharacter>(LargeMapOverUnder,
                adjustedPosition2Away, CurrentSingleMapReference.Floor);
        }

        // ReSharper disable once MemberCanBePrivate.Global
        public SeaFaringVessel GetSeaFaringVesselAtDock(SmallMapReferences.SingleMapReference.Location location)
        {
            // 0 = Jhelom
            // 1 = Minoc
            // 2 = East Brittany
            // 3 = Buccaneer's Den

            SeaFaringVessel seaFaringVessel = TheMapUnits.GetSpecificMapUnitByLocation<SeaFaringVessel>(
                Map.Maps.Overworld, GetLocationOfDock(location), 0, true);
            return seaFaringVessel;
        }

        /// <summary>
        ///     When orienting the stairs, which direction should they be drawn
        /// </summary>
        /// <param name="xy"></param>
        /// <returns></returns>
        // ReSharper disable once MemberCanBePrivate.Global
        public Point2D.Direction GetStairsDirection(Point2D xy)
        {
            // we are making a BIG assumption at this time that a stair case ONLY ever has a single
            // entrance point, and solid walls on all other sides... hopefully this is true
            if (!GetTileReference(xy.X - 1, xy.Y).IsSolidSprite) return Point2D.Direction.Left;
            if (!GetTileReference(xy.X + 1, xy.Y).IsSolidSprite) return Point2D.Direction.Right;
            if (!GetTileReference(xy.X, xy.Y - 1).IsSolidSprite) return Point2D.Direction.Up;
            if (!GetTileReference(xy.X, xy.Y + 1).IsSolidSprite) return Point2D.Direction.Down;
            throw new Ultima5ReduxException("Can't get stair direction - something is amiss....");
        }

        /// <summary>
        ///     Given the orientation of the stairs, it returns the correct sprite to display
        /// </summary>
        /// <param name="xy">position of stairs</param>
        /// <returns>stair sprite</returns>
        public int GetStairsSprite(Point2D xy)
        {
            bool bGoingUp = IsStairGoingUp(xy);
            Point2D.Direction direction = GetStairsDirection(xy);
            int nSpriteNum = -1;
            switch (direction)
            {
                case Point2D.Direction.Up:
                    nSpriteNum = bGoingUp
                        ? GameReferences.SpriteTileReferences.GetTileReferenceByName("StairsNorth").Index
                        : GameReferences.SpriteTileReferences.GetTileReferenceByName("StairsSouth").Index;
                    break;
                case Point2D.Direction.Down:
                    nSpriteNum = bGoingUp
                        ? GameReferences.SpriteTileReferences.GetTileReferenceByName("StairsSouth").Index
                        : GameReferences.SpriteTileReferences.GetTileReferenceByName("StairsNorth").Index;
                    break;
                case Point2D.Direction.Left:
                    nSpriteNum = bGoingUp
                        ? GameReferences.SpriteTileReferences.GetTileReferenceByName("StairsWest").Index
                        : GameReferences.SpriteTileReferences.GetTileReferenceByName("StairsEast").Index;
                    break;
                case Point2D.Direction.Right:
                    nSpriteNum = bGoingUp
                        ? GameReferences.SpriteTileReferences.GetTileReferenceByName("StairsEast").Index
                        : GameReferences.SpriteTileReferences.GetTileReferenceByName("StairsWest").Index;
                    break;
                case Point2D.Direction.None:
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            return nSpriteNum;
        }

        /// <summary>
        ///     Gets a tile reference from the given coordinate
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="bIgnoreExposed"></param>
        /// <param name="bIgnoreMoongate"></param>
        /// <returns></returns>
        public TileReference GetTileReference(int x, int y, bool bIgnoreExposed = false, bool bIgnoreMoongate = false)
        {
            // we FIRST check if there is an exposed item to show - this takes precedence over an overriden tile
            if (!bIgnoreExposed)
                if (TheMapOverrides.HasExposedSearchItems(x, y))
                {
                    Queue<InventoryItem> searchItems = TheMapOverrides.GetExposedSearchItems(x, y);
                    if (searchItems.Count > 0)
                        // we get the top most exposed item and only show it
                        return GameReferences.SpriteTileReferences.GetTileReference(searchItems.Peek().SpriteNum);
                }

            // if it's a large map and there should be a moongate and it's nighttime then it's a moongate!
            // bajh: March 22, 2020 - we are going to try to always include the Moongate, and let the game decide what it wants to do with it
            if (!bIgnoreMoongate && IsLargeMap &&
                GameStateReference.State.TheMoongates.IsMoonstoneBuried(new Point3D(x, y,
                    LargeMapOverUnder == Map.Maps.Overworld ? 0 : 0xFF)))
            {
                return GameReferences.SpriteTileReferences.GetTileReferenceByName("Moongate") ??
                       throw new Ultima5ReduxException("Supposed to get a moongate override: " + new Point2D(x, y));
            }

            // we check to see if our override map has something on top of it
            if (TheMapOverrides.HasOverrideTile(x, y))
            {
                return TheMapOverrides.GetOverrideTileReference(x, y) ??
                       throw new Ultima5ReduxException("Expected tile override at " + new Point2D(x, y));
            }

            // the GetTileReference accounts for any forced overrides across the entire world
            return CurrentMap.GetTileReference(new Point2D(x, y));
        }

        /// <summary>
        ///     Gets a tile reference from the given coordinate
        /// </summary>
        /// <param name="xy"></param>
        /// <returns></returns>
        public TileReference GetTileReference(Point2D xy)
        {
            return GetTileReference(xy.X, xy.Y);
        }

        /// <summary>
        ///     Gets a tile reference from the tile the avatar currently resides on
        /// </summary>
        /// <returns></returns>
        public TileReference GetTileReferenceOnCurrentTile()
        {
            return GetTileReference(CurrentPosition.XY);
        }

        /// <summary>
        ///     Gets the top visible map unit - excluding the Avatar
        /// </summary>
        /// <param name="xy"></param>
        /// <param name="bExcludeAvatar"></param>
        /// <returns>MapUnit or null</returns>
        // ReSharper disable once MemberCanBePrivate.Global
        public MapUnit GetTopVisibleMapUnit(Point2D xy, bool bExcludeAvatar)
        {
            List<Type> visibilePriorityOrder = new List<Type>
            {
                typeof(Horse), typeof(MagicCarpet), typeof(Skiff), typeof(Frigate), typeof(NonPlayerCharacter),
                typeof(Enemy), typeof(CombatPlayer), typeof(Avatar)
            };
            List<MapUnit> mapUnits = GetMapUnitOnTile(xy);

            // this is inefficient, but the lists are so small it is unlikely to matter
            foreach (Type type in visibilePriorityOrder)
            {
                if (bExcludeAvatar && type == typeof(Avatar)) continue;
                foreach (MapUnit mapUnit in mapUnits)
                {
                    if (!mapUnit.IsActive) continue;
                    // if it's a combat unit but they dead or gone then we skip
                    if (mapUnit is CombatMapUnit combatMapUnit)
                    {
                        if (combatMapUnit.HasEscaped || combatMapUnit.Stats.CurrentHp <= 0) continue;
                    }

                    // if we find the first highest priority item, then we simply return it
                    if (mapUnit.GetType() == type) return mapUnit;
                }
            }

            return null;
        }

        /// <summary>
        ///     Attempts to guess the tile underneath a thing that is upright such as a fountain
        ///     <remarks>This is only for 3D worlds, the 2D top down single sprite per tile model would not require this</remarks>
        /// </summary>
        /// <param name="xy">position of the thing</param>
        /// <returns>tile (sprite) number</returns>
        public int GuessTile(Point2D xy)
        {
            Dictionary<int, int> tileCountDictionary = new Dictionary<int, int>();

            // we check our high level tile override
            // this method is much quicker because we only load the data once in the maps 
            if (!IsLargeMap && CurrentMap.IsXYOverride(xy))
                return CurrentMap.GetTileOverride(xy).SpriteNum;
            if (IsLargeMap && CurrentMap.IsXYOverride(xy)) return CurrentMap.GetTileOverride(xy).SpriteNum;

            // if has exposed search then we evaluate and see if it is actually a normal tile underneath
            if (TheMapOverrides.HasExposedSearchItems(xy))
            {
                // there are exposed items on this tile
                TileReference tileRef = GetTileReference(xy.X, xy.Y, true, true);
                if (tileRef.FlatTileSubstitutionIndex != -2)
                    return tileRef.Index;
            }

            for (int i = -1; i <= 1; i++)
            {
                for (int j = -1; j <= 1; j++)
                {
                    // if it is out of bounds then we skips them altogether
                    if (xy.X + i < 0 || xy.X + i >= NumberOfRowTiles || xy.Y + j < 0 || xy.Y + j >= NumberOfColumnTiles)
                        continue;
                    TileReference tileRef = GetTileReference(xy.X + i, xy.Y + j);
                    // only look at non-upright sprites AND if it's a guessable tile
                    if (tileRef.IsUpright || !tileRef.IsGuessableFloor) continue;

                    int nTile = tileRef.Index;
                    if (tileCountDictionary.ContainsKey(nTile))
                        tileCountDictionary[nTile] += 1;
                    else
                        tileCountDictionary.Add(nTile, 1);
                }
            }

            int nMostTile = -1;
            int nMostTileTotal = -1;
            // go through each of the tiles we saw and record the tile with the most instances
            foreach (int nTile in tileCountDictionary.Keys)
            {
                int nTotal = tileCountDictionary[nTile];
                if (nMostTile == -1 || nTotal > nMostTileTotal)
                {
                    nMostTile = nTile;
                    nMostTileTotal = nTotal;
                }
            }

            // just in case we didn't find a match - just use grass for now
            return nMostTile == -1 ? 5 : nMostTile;
        }

        public bool HasAnyExposedSearchItems(Point2D xy) => TheMapOverrides.HasExposedSearchItems(xy);

        public bool IsAvatarSitting()
        {
            return GameReferences.SpriteTileReferences.IsChair(GetTileReferenceOnCurrentTile().Index);
        }

        /// <summary>
        ///     Is there food on a table within 1 (4 way) tile
        ///     Used for determining if eating animation should be used
        /// </summary>
        /// <param name="characterPos"></param>
        /// <returns>true if food is within a tile</returns>
        public bool IsFoodNearby(Point2D characterPos)
        {
            bool isFoodTable(int nSprite)
            {
                return nSprite == GameReferences.SpriteTileReferences.GetTileReferenceByName("TableFoodTop").Index ||
                       nSprite == GameReferences.SpriteTileReferences.GetTileReferenceByName("TableFoodBottom").Index ||
                       nSprite == GameReferences.SpriteTileReferences.GetTileReferenceByName("TableFoodBoth").Index;
            }

            // yuck, but if the food is up one tile or down one tile, then food is nearby
            bool bIsFoodNearby = isFoodTable(GetTileReference(characterPos.X, characterPos.Y - 1).Index) ||
                                 isFoodTable(GetTileReference(characterPos.X, characterPos.Y + 1).Index);
            return bIsFoodNearby;
        }

        /// <summary>
        ///     Is the door at the specified coordinate horizontal?
        /// </summary>
        /// <param name="xy"></param>
        /// <returns></returns>
        public bool IsHorizDoor(Point2D xy)
        {
            if (xy.X - 1 < 0 || xy.X + 1 >= NumberOfColumnTiles) return false;
            if (xy.Y - 1 < 0 || xy.Y + 1 >= NumberOfRowTiles) return true;

            return (GetTileReference(xy.X - 1, xy.Y).IsSolidSpriteButNotDoorAndNotNPC) ||
                   (GetTileReference(xy.X + 1, xy.Y).IsSolidSpriteButNotDoorAndNotNPC);
        }

        public bool IsLandNearby() =>
            IsLandNearby(CurrentPosition.XY, false, TheMapUnits.AvatarMapUnit.CurrentAvatarState);

        public bool IsLandNearby(Point2D xy, bool bNoStairCases, Avatar.AvatarState avatarState) =>
            IsTileFreeToTravel(xy.GetAdjustedPosition(Point2D.Direction.Down), bNoStairCases, avatarState) ||
            IsTileFreeToTravel(xy.GetAdjustedPosition(Point2D.Direction.Up), bNoStairCases, avatarState) ||
            IsTileFreeToTravel(xy.GetAdjustedPosition(Point2D.Direction.Left), bNoStairCases, avatarState) ||
            IsTileFreeToTravel(xy.GetAdjustedPosition(Point2D.Direction.Right), bNoStairCases, avatarState);

        public bool IsLandNearby(Avatar.AvatarState avatarState) =>
            IsTileFreeToTravelLocal(Point2D.Direction.Down, avatarState) ||
            IsTileFreeToTravelLocal(Point2D.Direction.Up, avatarState) ||
            IsTileFreeToTravelLocal(Point2D.Direction.Left, avatarState) ||
            IsTileFreeToTravelLocal(Point2D.Direction.Right, avatarState);

        /// <summary>
        ///     Gets a map unit if it's on the current tile
        /// </summary>
        /// <returns>true if there is a map unit of on the tile</returns>
        public bool IsMapUnitOccupiedTile()
        {
            return IsMapUnitOccupiedTile(CurrentPosition.XY);
        }

        /// <summary>
        ///     Is there an NPC on the tile specified?
        /// </summary>
        /// <param name="xy"></param>
        /// <returns></returns>
        public bool IsMapUnitOccupiedTile(Point2D xy)
        {
            List<MapUnit> mapUnits = TheMapUnits.GetMapUnitCollection(LargeMapOverUnder).AllActiveMapUnits;
            for (int index = 0; index < mapUnits.Count; index++)
            {
                // sometimes characters are null because they don't exist - and that is OK
                Debug.Assert(mapUnits[index].IsActive);

                MapUnitPosition mapUnitPosition = mapUnits[index].MapUnitPosition;
                if (mapUnitPosition.X == xy.X && mapUnitPosition.Y == xy.Y &&
                    mapUnitPosition.Floor == CurrentPosition.Floor)
                    return true;
            }

            return false;
        }

        public bool IsNPCInBed(NonPlayerCharacter npc)
        {
            return GetTileReference(npc.MapUnitPosition.XY).Index ==
                   GameReferences.SpriteTileReferences.GetTileNumberByName("LeftBed");
        }

        /// <summary>
        ///     Determines if a specific Dock is occupied by a Sea Faring Vessel
        /// </summary>
        /// <returns></returns>
        public bool IsShipOccupyingDock(SmallMapReferences.SingleMapReference.Location location)
        {
            return GetSeaFaringVesselAtDock(location) != null;
        }

        /// <summary>
        ///     Are the stairs at the given position going down?
        ///     Be sure to check if they are stairs first
        /// </summary>
        /// <param name="xy"></param>
        /// <returns></returns>
        // ReSharper disable once MemberCanBePrivate.Global
        public bool IsStairGoingDown(Point2D xy)
        {
            if (!GameReferences.SpriteTileReferences.IsStaircase(GetTileReference(xy).Index)) return false;
            bool bStairGoUp = _smallMaps.DoStairsGoUp(CurrentSmallMap.MapLocation, CurrentSmallMap.MapFloor, xy);
            return bStairGoUp;
        }

        /// <summary>
        ///     Are the stairs at the given position going up?
        ///     Be sure to check if they are stairs first
        /// </summary>
        /// <param name="xy"></param>
        /// <returns></returns>
        public bool IsStairGoingUp(Point2D xy)
        {
            if (!GameReferences.SpriteTileReferences.IsStaircase(GetTileReference(xy).Index)) return false;

            if (IsCombatMap) return false;

            bool bStairGoUp = _smallMaps.DoStairsGoUp(CurrentSmallMap.MapLocation, CurrentSmallMap.MapFloor, xy);
            return bStairGoUp;
        }

        /// <summary>
        ///     Are the stairs at the player characters current position going up?
        /// </summary>
        /// <returns></returns>
        // ReSharper disable once MemberCanBePrivate.Global
        public bool IsStairGoingUp()
        {
            return IsStairGoingUp(CurrentPosition.XY);
        }

        /// <summary>
        ///     Are the stairs at the player characters current position going down?
        /// </summary>
        /// <returns></returns>
        public bool IsStairsGoingDown()
        {
            return IsStairGoingDown(CurrentPosition.XY);
        }

        public void LoadCombatMap(SingleCombatMapReference singleCombatMapReference,
            SingleCombatMapReference.EntryDirection entryDirection, PlayerCharacterRecords records,
            EnemyReference primaryEnemyReference = null, int nPrimaryEnemies = 0,
            EnemyReference secondaryEnemyReference = null, int nSecondaryEnemies = 0)
        {
            LoadCombatMap(singleCombatMapReference, entryDirection, records, primaryEnemyReference, nPrimaryEnemies,
                secondaryEnemyReference, nSecondaryEnemies, null);
        }

        public void LoadCombatMap(SingleCombatMapReference singleCombatMapReference,
            SingleCombatMapReference.EntryDirection entryDirection, PlayerCharacterRecords records,
            EnemyReference primaryEnemyReference, NonPlayerCharacterReference npcRef)
        {
            LoadCombatMap(singleCombatMapReference, entryDirection, records, primaryEnemyReference, 1, null, 0, npcRef);
        }

        /// <summary>
        ///     Loads a combat map, but aut
        /// </summary>
        /// <param name="singleCombatMapReference"></param>
        /// <param name="entryDirection"></param>
        /// <param name="records"></param>
        /// <param name="enemyReference"></param>
        public void LoadCombatMapWithCalculation(SingleCombatMapReference singleCombatMapReference,
            SingleCombatMapReference.EntryDirection entryDirection, PlayerCharacterRecords records,
            EnemyReference enemyReference)
        {
            int nPrimaryEnemies = 1;
            int nSecondaryEnemies = 1;

            if (enemyReference.IsNpc) nSecondaryEnemies = 0;

            EnemyReference primaryEnemyReference = enemyReference;
            EnemyReference secondaryEnemyReference = GameReferences.EnemyRefs.GetFriendReference(primaryEnemyReference);

            LoadCombatMap(singleCombatMapReference, entryDirection, records, primaryEnemyReference, nPrimaryEnemies,
                secondaryEnemyReference, nSecondaryEnemies);
        }

        /// <summary>
        ///     Loads a large map -either overworld or underworld
        /// </summary>
        /// <param name="map"></param>
        public void LoadLargeMap(Map.Maps map)
        {
            CurrentLargeMap = _largeMaps[map];
            switch (map)
            {
                case Map.Maps.Underworld:
                case Map.Maps.Overworld:
                    CurrentSingleMapReference = CurrentLargeMap.CurrentSingleMapReference;
                    break;
                case Map.Maps.Combat:
                case Map.Maps.Small:
                    throw new Ultima5ReduxException("You can't load a small large map!");
                default:
                    throw new ArgumentOutOfRangeException(nameof(map), map, null);
            }

            // this will probably fail, which means creating a new map was a good idea
            //Debug.Assert(TheMapOverrides.TheMap is LargeMap && TheMapOverrides.TheMap == CurrentLargeMap);
            // todo: maybe we store each map override indefinitely so we never lose anything 
            TheMapOverrides = new MapOverrides(CurrentLargeMap);

            LargeMapOverUnder = map;

            TheMapUnits.SetCurrentMapType(SmallMapReferences.SingleMapReference.GetLargeMapSingleInstance(map), map);
        }

        public void LoadSmallMap(SmallMapReferences.SingleMapReference singleMapReference, Point2D xy = null,
            bool bLoadFromDisk = false)
        {
            CurrentSingleMapReference = singleMapReference ??
                                        throw new Ultima5ReduxException(
                                            "Tried to load a small map, but null map reference was given");
            CurrentSmallMap = _smallMaps.GetSmallMap(singleMapReference.MapLocation, singleMapReference.Floor);

            TheMapOverrides = new MapOverrides(CurrentSmallMap);

            LargeMapOverUnder = (Map.Maps)(-1);

            TheMapUnits.SetCurrentMapType(singleMapReference, Map.Maps.Small, bLoadFromDisk);
            // change the floor that the Avatar is on, otherwise he will be on the last floor he started on
            TheMapUnits.AvatarMapUnit.MapUnitPosition.Floor = singleMapReference.Floor;

            if (xy != null) CurrentPosition.XY = xy;
        }

        public void MoveAvatar(Point2D newPosition)
        {
            TheMapUnits.CurrentAvatarPosition =
                new MapUnitPosition(newPosition.X, newPosition.Y, TheMapUnits.CurrentAvatarPosition.Floor);
        }

        public void ReturnToPreviousMapAfterCombat()
        {
            switch (PreCombatMap)
            {
                case LargeMap _:
                case SmallMap _:
                    // restore our old map overrides
                    TheMapOverrides = PreTheMapOverrides;

                    LargeMapOverUnder = PreCombatMap.CurrentSingleMapReference.MapType;
                    CurrentSingleMapReference = PreCombatMap.CurrentSingleMapReference;
                    TheMapUnits.SetCurrentMapType(PreCombatMap.CurrentSingleMapReference, LargeMapOverUnder, false);
                    TheMapUnits.AvatarMapUnit.MapUnitPosition = PreMapUnitPosition;
                    PreCombatMap = null;
                    break;
                default:
                    throw new Ultima5ReduxException(
                        "Attempting to return to previous map after combat with an unsupported map type: " +
                        PreCombatMap?.GetType());
            }
        }

        /// <summary>
        ///     Sets an override for the current tile which will be favoured over the static map tile
        /// </summary>
        /// <param name="tileReference">the reference (sprite)</param>
        /// <param name="xy"></param>
        public void SetOverridingTileReferece(TileReference tileReference, Point2D xy)
        {
            TheMapOverrides.SetOverrideTile(xy, tileReference);
        }

        public void SwapTiles(Point2D tile1Pos, Point2D tile2Pos)
        {
            TileReference tileRef1 = GetTileReference(tile1Pos);
            TileReference tileRef2 = GetTileReference(tile2Pos);

            SetOverridingTileReferece(tileRef1, tile2Pos);
            SetOverridingTileReferece(tileRef2, tile1Pos);
        }

        // private bool IsTileFreeToTravelLocal(Point2D.Direction direction, Point2D xy, Avatar.AvatarState avatarState) =>
        //     IsTileFreeToTravel(xy.GetAdjustedPosition(direction), true, avatarState);

        /// <summary>
        ///     Use the stairs and change floors, loading a new map
        /// </summary>
        /// <param name="xy">the position of the stairs, ladder or trapdoor</param>
        /// <param name="bForceDown">force a downward stairs</param>
        public void UseStairs(Point2D xy, bool bForceDown = false)
        {
            bool bStairGoUp = IsStairGoingUp() && !bForceDown;

            if (CurrentSingleMapReference == null)
                throw new Ultima5ReduxException("No single map is set in virtual map");

            LoadSmallMap(GameReferences.SmallMapRef.GetSingleMapByLocation(CurrentSingleMapReference.MapLocation,
                CurrentSmallMap.MapFloor + (bStairGoUp ? 1 : -1)), xy.Copy());
        }

        [OnDeserialized] private void PostDeserialize(StreamingContext context)
        {
            TheMapOverrides.TheMap = CurrentMap;
        }
    }
}