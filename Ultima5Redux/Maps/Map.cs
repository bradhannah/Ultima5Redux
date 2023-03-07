using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.Serialization;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Ultima5Redux.External;
using Ultima5Redux.MapUnits;
using Ultima5Redux.MapUnits.CombatMapUnits;
using Ultima5Redux.MapUnits.NonPlayerCharacters;
using Ultima5Redux.MapUnits.SeaFaringVessels;
using Ultima5Redux.MapUnits.TurnResults;
using Ultima5Redux.MapUnits.TurnResults.SpecificTurnResults;
using Ultima5Redux.PlayerCharacters;
using Ultima5Redux.References;
using Ultima5Redux.References.Maps;
using Ultima5Redux.References.MapUnits.NonPlayerCharacters;

namespace Ultima5Redux.Maps
{
    public partial class VirtualMap
    {
        internal enum LadderOrStairDirection { Up, Down }
    }

    [DataContract] public abstract class Map
    {
        [JsonConverter(typeof(StringEnumConverter))]
        public enum Maps { Small = -1, Overworld, Underworld, Combat, Dungeon }

        [JsonConverter(typeof(StringEnumConverter))]
        public enum WalkableType { StandardWalking, CombatLand, CombatWater, CombatFlyThroughWalls, CombatLandAndWater }

        public const int MAX_MAP_CHARACTERS = 64;
        protected const int MAX_LEGACY_MAP_CHARACTERS = 32;
        protected const int TOTAL_VISIBLE_TILES = 26;

        [DataMember] internal MapOverrides TheMapOverrides { get; set; }

        [DataMember(Name = "OpenDoors")] private readonly Dictionary<Point2D, int> _openDoors = new();

        [DataMember] public abstract Maps TheMapType { get; }

        [DataMember(Name = "UseExtendedSprites")]
        public readonly bool UseExtendedSprites = true;

        [DataMember] public MapUnitCollection CurrentMapUnits { get; protected set; } = new();
        [DataMember] public int MapFloor { get; protected set; }

        [DataMember] public SmallMapReferences.SingleMapReference.Location MapLocation { get; protected set; }

        [DataMember] public bool XRayMode { get; set; }

        [IgnoreDataMember] private readonly List<Type> _visiblePriorityOrder = new()
        {
            typeof(Horse), typeof(MagicCarpet), typeof(Skiff), typeof(Frigate), typeof(NonPlayerCharacter),
            typeof(Enemy), typeof(CombatPlayer), typeof(Avatar), typeof(MoonstoneNonAttackingUnit), typeof(ItemStack),
            typeof(StackableItem), typeof(Chest), typeof(DeadBody), typeof(BloodSpatter), typeof(ElementalField),
            typeof(Whirlpool)
        };


        /// <summary>
        ///     Detailed reference of current small map
        /// </summary>
        [IgnoreDataMember]
        public abstract SmallMapReferences.SingleMapReference CurrentSingleMapReference { get; }

        [IgnoreDataMember] public abstract MapUnitPosition CurrentPosition { get; set; }

        private readonly List<Type> _searchOrderPriority = new()
        {
            typeof(DiscoverableLoot), typeof(ItemStack), typeof(Chest), typeof(DeadBody), typeof(BloodSpatter)
        };

        public abstract int NumOfXTiles { get; }

        public abstract int NumOfYTiles { get; }

        public abstract byte[][] TheMap { get; protected set; }

        // ReSharper disable once UnusedAutoPropertyAccessor.Global
        // ReSharper disable once MemberCanBeProtected.Global
        public int RecalculatedHash { get; protected set; }

        protected abstract Dictionary<Point2D, TileOverrideReference> XyOverrides { get; }


        protected Map(SmallMapReferences.SingleMapReference.Location mapLocation, int mapFloor) : this()
        {
            MapLocation = mapLocation;
            MapFloor = mapFloor;
        }

        [JsonConstructor] protected Map() => TheMapOverrides = new MapOverrides(this);

        [OnDeserialized] private void PostDeserialize(StreamingContext context)
        {
            TheMapOverrides.TheMap = this;
        }


        internal abstract WalkableType GetWalkableTypeByMapUnit(MapUnit mapUnit);

        // ReSharper disable once UnusedMemberInSuper.Global
        [SuppressMessage("ReSharper", "UnusedParameter.Global")]
        internal abstract void ProcessTileEffectsForMapUnit(TurnResults turnResults, MapUnit mapUnit);

        internal void ClearMapUnit(MapUnit mapUnit)
        {
            int nIndex = CurrentMapUnits.AllMapUnits.IndexOf(mapUnit);
            CurrentMapUnits.AllMapUnits[nIndex] = new EmptyMapUnit();
        }

        internal TileReference GetOriginalTileReference(in Point2D xy) =>
            GameReferences.Instance.SpriteTileReferences.GetTileReference(
                IsXyOverride(xy, TileOverrideReference.TileType.Primary)
                    ? GetTileOverride(xy).SpriteNum
                    : TheMap[xy.X][xy.Y]);

        internal bool IsInsideBounds(in Point2D xy) =>
            !(xy.X >= VisibleOnMap.Length || xy.X < 0 ||
              xy.Y >= VisibleOnMap[xy.X].Length || xy.Y < 0);

        internal bool IsTileFreeToTravel(in Point2D xy, bool bNoStaircases, Avatar.AvatarState forcedAvatarState) =>
            IsTileFreeToTravel(CurrentPosition.XY, xy, bNoStaircases, forcedAvatarState);

        /// <summary>
        ///     Is the particular tile eligible to be moved onto
        /// </summary>
        /// <param name="currentPosition"></param>
        /// <param name="newPosition"></param>
        /// <param name="bNoStaircases"></param>
        /// <param name="forcedAvatarState"></param>
        /// <returns>true if you can move onto the tile</returns>
        internal bool IsTileFreeToTravel(in Point2D currentPosition, in Point2D newPosition,
            bool bNoStaircases, Avatar.AvatarState forcedAvatarState)
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
                !(bNoStaircases && TileReferences.IsStaircase(tileReference.Index));

            // if it's nighttime then the portcullises go down and you cannot pass
            bool bPortcullisDown =
                tileReference.Is(TileReference.SpriteIndex.BrickWallArchway) &&
                !GameStateReference.State.TheTimeOfDay.IsDayLight;

            // we check both the tile reference below as well as the map unit that occupies the tile
            bool bIsWalkable =
                // if the MapUnit is null then we do a basic evaluation 
                mapUnit?.CanStackMapUnitsOnTop ??
                (tileReference.IsPassable(forcedAvatarState) && bStaircaseWalkable && !bPortcullisDown);

            // there is not an NPC on the tile, it is walkable and the Avatar is not currently occupying it
            return bIsWalkable && !bIsAvatarTile;
        }

        internal bool IsTileWithinFourDirections(Point2D position, int nTileIndex)
        {
            List<Point2D> positions;
            if (IsRepeatingMap)
                positions = position.GetConstrainedFourDirectionSurroundingPointsWrapAround(
                    LargeMapLocationReferences.X_TILES,
                    LargeMapLocationReferences.Y_TILES);
            else
                positions = position.GetConstrainedFourDirectionSurroundingPoints(NumOfXTiles, NumOfYTiles);

            return positions.Any(testPosition => GetTileReference(testPosition).Index == nTileIndex);
        }

        internal bool ProcessSearchInnerItems(TurnResults turnResults, NonAttackingUnit nonAttackingUnit,
            bool bSearched,
            bool bOpened)
        {
            bool bHasInnerItems = nonAttackingUnit.HasInnerItemStack;
            ItemStack itemStack = nonAttackingUnit.InnerItemStack;
            if (bHasInnerItems)
            {
                turnResults.PushOutputToConsole(nonAttackingUnit.InnerItemStack.ThouFindStr);
            }
            else
            {
                if (nonAttackingUnit is DiscoverableLoot discoverableLoot)
                {
                    PlaceNonAttackingUnit(discoverableLoot.AlternateNonAttackingUnit,
                        discoverableLoot.AlternateNonAttackingUnit.MapUnitPosition);

                    bHasInnerItems = true;

                    if (discoverableLoot.IsDeadBody)
                    {
                        turnResults.PushOutputToConsole(U5StringRef.ThouDostFind(
                            GameReferences.Instance.DataOvlRef.StringReferences.GetString(DataOvlReference
                                .ThingsIFindStrings
                                .A_ROTTING_BODY_BANG_N)));
                    }
                    else if (discoverableLoot.IsBloodSpatter)
                    {
                        turnResults.PushOutputToConsole(U5StringRef.ThouDostFind(
                            GameReferences.Instance.DataOvlRef.StringReferences.GetString(DataOvlReference
                                .ThingsIFindStrings
                                .A_BLOOD_PULP_BANG_N)));
                    }
                    else
                    {
                        throw new Ultima5ReduxException(
                            "Tried to ProcessSearchInnerItems but it had no inner items, nor was it a dead body or blood spatter");
                    }
                }

                turnResults.PushOutputToConsole(
                    GameReferences.Instance.DataOvlRef.StringReferences.GetString(DataOvlReference.ThingsIFindStrings
                        .NOTHING_OF_NOTE_BANG_N));
            }

            // delete the deadbody and add stuff
            nonAttackingUnit.HasBeenSearched = bSearched;
            nonAttackingUnit.HasBeenOpened = bOpened;
            turnResults.PushTurnResult(new BasicResult(TurnResult.TurnResultType.ActionSearchThingDisappears));

            if (bHasInnerItems)
                // there were items inside the thing, so we place them 
                RePlaceNonAttackingUnit(nonAttackingUnit, itemStack, nonAttackingUnit.MapUnitPosition);

            return bHasInnerItems;
        }

        internal bool SearchNonAttackingMapUnit(in Point2D xy, TurnResults turnResults,
            PlayerCharacterRecord record, PlayerCharacterRecords records)
        {
            NonAttackingUnit nonAttackingMapUnit = GetNonAttackingMapUnitOnTileBySearchPriority(xy);

            if (nonAttackingMapUnit == null) return false;

            // we don't check everything for traps - only types specifically flagged with types
            if (nonAttackingMapUnit.NonAttackUnitTypeCanBeTrapped)
            {
                ProcessSearchNonAttackUnitTrap(turnResults, record, records, nonAttackingMapUnit);
            }

            // we only open the inner items up if it exposes on search like DeadBodies and Spatters
            return nonAttackingMapUnit.ExposeInnerItemsOnSearch &&
                   ProcessSearchInnerItems(turnResults, nonAttackingMapUnit, true, false);
        }

        internal void SwapTiles(Point2D tile1Pos, Point2D tile2Pos)
        {
            TileReference tileRef1 = GetTileReference(tile1Pos);
            TileReference tileRef2 = GetTileReference(tile2Pos);

            SetOverridingTileReference(tileRef1, tile2Pos);
            SetOverridingTileReference(tileRef2, tile1Pos);
        }

        /// <summary>
        ///     Filthy little map to assign single letter to map elements
        /// </summary>
        /// <param name="tile"></param>
        /// <returns></returns>
        private static char GetMapLetter(byte tile)
        {
            switch (tile)
            {
                case 0x03:
                case 0x02:
                case 0x01:
                    return 'W'; // water
                case 0x08:
                case 0x09:
                case 0x0A:
                    return 'T'; //  trees
                case 0x04:
                case 0x05:
                case 0x06:
                    return 'G'; // grass
                case 0x0B:
                case 0x0C:
                case 0x0D:
                case 0x0E:
                case 0x0F:
                    return 'M'; // mountains
                case 0x10:
                case 0x11:
                case 0x12:
                case 0x13:
                case 0x14:
                case 0x15:
                case 0x16:
                case 0x17:
                case 0x18:
                    return 'X'; // towns
                case 0x19:
                case 0x1A:
                    return 'S'; //shrines
                case 0x1B:
                    return 'L'; //lighthouse
                case 0x1C:
                    return '?';
                case 0x1D:
                    return 'B'; //bridge
                case 0x1E:
                    return 'F'; //field?? --- end of tile row (30dec)
                case 0x1F:
                case 0x20:
                case 0x21:
                case 0x22:
                case 0x23:
                case 0x24:
                case 0x25:
                    return 'P'; // path --ignore next 9 (0x2E)
                case 0x2F:
                case 0x31:
                case 0x32:
                case 0x33:
                    return 'G'; // more grass
                case 0x34:
                case 0x35:
                case 0x36:
                case 0x37:
                    return 'S'; // streams
            }

            return 'L';
        }

        private static void ProcessSearchNonAttackUnitTrap(TurnResults turnResults, PlayerCharacterRecord record,
            PlayerCharacterRecords records, NonAttackingUnit nonAttackingUnit)
        {
            if (!nonAttackingUnit.IsTrapped)
            {
                turnResults.PushOutputToConsole(
                    U5StringRef.ThouDostFind(GameReferences.Instance.DataOvlRef.StringReferences.GetString(
                        DataOvlReference.ThingsIFindStrings.NO_TRAP_BANG_N)));
                turnResults.PushTurnResult(new BasicResult(TurnResult.TurnResultType.ActionSearchNoTrap));
                return;
            }

            // A_SIMPLE_TRAP_BANG_N, A_COMPLEX_TRAP_BANG_N,
            // When I open it? A_TRAP_BANG_N
            bool bTriggeredTrap = nonAttackingUnit.DoesTriggerTrap(record);

            switch (nonAttackingUnit.CurrentTrapComplexity)
            {
                case NonAttackingUnit.TrapComplexity.Simple:
                    if (bTriggeredTrap)
                    {
                        turnResults.PushOutputToConsole(U5StringRef.ThouDostFind(
                            GameReferences.Instance.DataOvlRef.StringReferences.GetString(DataOvlReference
                                .ThingsIFindStrings
                                .A_SIMPLE_TRAP_BANG_N)));
                        nonAttackingUnit.TriggerTrap(turnResults, record.Stats, records);
                        turnResults.PushTurnResult(
                            new BasicResult(TurnResult.TurnResultType.ActionSearchTriggerSimpleTrap));
                    }
                    else
                    {
                        turnResults.PushOutputToConsole(U5StringRef.ThouDostFind(
                            GameReferences.Instance.DataOvlRef.StringReferences.GetString(DataOvlReference
                                .ThingsIFindStrings
                                .A_SIMPLE_TRAP_N)));
                        turnResults.PushTurnResult(new BasicResult(TurnResult.TurnResultType.ActionSearchRemoveSimple));
                    }

                    break;
                case NonAttackingUnit.TrapComplexity.Complex:
                    if (bTriggeredTrap)
                    {
                        turnResults.PushOutputToConsole(
                            U5StringRef.ThouDostFind(
                                GameReferences.Instance.DataOvlRef.StringReferences.GetString(DataOvlReference
                                    .ThingsIFindStrings
                                    .A_COMPLEX_TRAP_BANG_N)));
                        nonAttackingUnit.TriggerTrap(turnResults, record.Stats, records);
                        turnResults.PushTurnResult(
                            new BasicResult(TurnResult.TurnResultType.ActionSearchTriggerComplexTrap));
                    }
                    else
                    {
                        turnResults.PushOutputToConsole(
                            U5StringRef.ThouDostFind(
                                GameReferences.Instance.DataOvlRef.StringReferences.GetString(DataOvlReference
                                    .ThingsIFindStrings
                                    .A_COMPLEX_TRAP_N)));
                        turnResults.PushTurnResult(
                            new BasicResult(TurnResult.TurnResultType.ActionSearchRemoveComplex));
                    }

                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            turnResults.PushTurnResult(new BasicResult(TurnResult.TurnResultType.ActionSearchNoTrap));
            // HURT THE PLAYER IF THEY ARE AFFECTED BY TRAP
        }

        private NonAttackingUnit GetNonAttackingMapUnitOnTileBySearchPriority(in Point2D xy)
        {
            if (CurrentSingleMapReference == null)
                throw new Ultima5ReduxException("No single map is set in virtual map");

            List<MapUnit> mapUnitsAtPosition = GetMapUnitsOnTile(xy);

            // this is inefficient, but the lists are so small it is unlikely to matter
            foreach (MapUnit mapUnit in _searchOrderPriority.SelectMany(type =>
                         mapUnitsAtPosition.Where(mapUnit => mapUnit.GetType() == type)))
            {
                if (mapUnit is not NonAttackingUnit nonAttackingUnit) continue;
                // ReSharper disable once MergeIntoPattern
                if (!nonAttackingUnit.IsSearchable) continue;

                return nonAttackingUnit;
            }

            return null;
        }

        private TileOverrideReference GetTileOverride(in Point2D xy) => XyOverrides[xy];

        private bool IsCurrentPositionFreeToMoveDirection(in Point2D.Direction direction,
            Avatar.AvatarState avatarState) =>
            IsTileFreeToTravel(CurrentPosition.XY.GetAdjustedPosition(direction), true, avatarState);

        private bool IsTileWithinFourDirections(Point2D position, TileReference tileReference) =>
            IsTileWithinFourDirections(position, tileReference.Index);

        /// <summary>
        ///     Places an existing non attacking unit on a map
        ///     This is often used when an item stack exists in a chest OR if an enemy leaves a body or blood spatter
        /// </summary>
        /// <param name="nonAttackingUnit"></param>
        /// <param name="mapUnitPosition"></param>
        /// <returns></returns>
        // ReSharper disable once SuggestBaseTypeForParameter
        // ReSharper disable once UnusedMethodReturnValue.Local
        private bool PlaceNonAttackingUnit(NonAttackingUnit nonAttackingUnit, MapUnitPosition mapUnitPosition)
        {
            int nIndex = FindNextFreeMapUnitIndex();
            if (nIndex == -1) return false;

            nonAttackingUnit.MapUnitPosition = mapUnitPosition;

            // set position of frigate in the world
            AddNewMapUnit(nonAttackingUnit, nIndex);
            return true;
        }

        [SuppressMessage("ReSharper", "SuggestBaseTypeForParameter")]
        [SuppressMessage("ReSharper", "UnusedMethodReturnValue.Local")]
        private bool RePlaceNonAttackingUnit(NonAttackingUnit originalNonAttackingUnit,
            NonAttackingUnit replacementNonAttackingUnit, MapUnitPosition mapUnitPosition)
        {
            int nIndex = 0;
            bool bFound = false;
            foreach (MapUnit mapUnit in CurrentMapUnits.AllMapUnits)
            {
                if (mapUnit == originalNonAttackingUnit)
                {
                    bFound = true;
                    break;
                }

                nIndex++;
            }

            if (!bFound)
                throw new Ultima5ReduxException(
                    $"Tried to replace NonAttackingMapUnit but could find in the current MapUnit list (orig: {originalNonAttackingUnit.FriendlyName}");

            replacementNonAttackingUnit.MapUnitPosition = mapUnitPosition;

            // set position of frigate in the world
            AddNewMapUnit(replacementNonAttackingUnit, nIndex);
            return true;
        }

        public bool AreAnyTilesWithinFourDirections(Point2D position, IEnumerable<TileReference> tileReferences)
        {
            return tileReferences.Any(tileReference => IsTileWithinFourDirections(position, tileReference));
        }

        /// <summary>
        ///     Clear a current map unit, essentially removing it from the world
        ///     Commonly used when something is boarded, and collapses into the Avatar himself
        ///     note: the MapUnit is no longer referenced - but it will often exist within the Avatar
        ///     object if they have in fact boarded it
        /// </summary>
        /// <param name="mapUnitToClear"></param>
        /// <exception cref="Ultima5ReduxException"></exception>
        public void ClearAndSetEmptyMapUnits(MapUnit mapUnitToClear)
        {
            for (int index = 0; index < CurrentMapUnits.AllMapUnits.Count; index++)
            {
                MapUnit mapUnit = CurrentMapUnits.AllMapUnits[index];

                if (mapUnit != mapUnitToClear) continue;

                CurrentMapUnits.AllMapUnits[index] = new EmptyMapUnit();
                return;
            }

            throw new Ultima5ReduxException(
                "You provided a MapUnit to clear, but it is not in the active MapUnit list");
        }

        [SuppressMessage("ReSharper", "MemberCanBeProtected.Global")]
        public int ClosestTileReferenceAround(Point2D midPosition, int nRadius, Func<int, bool> checkTile)
        {
            double nShortestRadius = 255;
            bool bIsRepeatingMap = IsRepeatingMap;
            CurrentMapUnits.RefreshActiveDictionaryCache();
            // an optimization to speed up checking of map units
            Dictionary<Point2D, List<MapUnit>> cachedActive = CurrentMapUnits.CachedActiveDictionary;

            for (int nRow = midPosition.X - nRadius; nRow < midPosition.X + nRadius; nRow++)
            {
                for (int nCol = midPosition.Y - nRadius; nCol < midPosition.Y + nRadius; nCol++)
                {
                    Point2D adjustedPos;
                    if (bIsRepeatingMap)
                    {
                        adjustedPos = new Point2D(Point2D.AdjustToMax(nRow, NumOfXTiles),
                            Point2D.AdjustToMax(nCol, NumOfYTiles));
                    }
                    else
                    {
                        if (nRow < 0 || nRow >= NumOfXTiles || nCol < 0 || nCol >= NumOfYTiles)
                            continue;

                        adjustedPos = new Point2D(nRow, nCol);
                    }

                    int nTileIndex = GetTileReference(adjustedPos.X, adjustedPos.Y).Index;
                    bool bHasMapUnits = cachedActive.ContainsKey(adjustedPos);
                    MapUnit mapUnit = bHasMapUnits ? GetTopVisibleMapUnit(adjustedPos, true) : null;

                    bool bMapUnitMatches = mapUnit != null && checkTile(mapUnit.KeyTileReference.Index);

                    if (!checkTile(nTileIndex) && !bMapUnitMatches) continue;
                    double fDistance = Point2D.DistanceBetween(midPosition.X, midPosition.Y, nRow, nCol);
                    if (nShortestRadius < fDistance) continue;

                    // shortcut in case we hit it
                    if (nRadius == 1) return 1;
                    nShortestRadius = fDistance;
                }
            }

            if (Math.Abs(nShortestRadius - 255) < 0.05f) return 255;
            return (int)Math.Round(nShortestRadius);
        }

        public int ClosestTileReferenceAround(TileReference tileReference, Point2D midPosition, int nRadius)
        {
            return ClosestTileReferenceAround(midPosition, nRadius, i => tileReference.Index == i);
        }

        public int ClosestTileReferenceAround(TileReference tileReference, int nRadius)
        {
            return ClosestTileReferenceAround(CurrentPosition.XY, nRadius, i => tileReference.Index == i);
        }

        public int GetAlternateFlatSprite(in Point2D xy)
        {
            if (IsXyOverride(xy, TileOverrideReference.TileType.Flat))
                return GetTileOverride(xy).SpriteNum;

            int nSprite = GetTileReference(xy).FlatTileSubstitutionIndex;

            return nSprite is -2 or -3 ? GuessTile(xy) : nSprite;
        }

        /// <summary>
        ///     Builds the A* map to be used for NPC pathfinding
        /// </summary>
        public AStar GetAStarMap(WalkableType walkableType)
        {
            Debug.Assert(TheMap != null);
            Debug.Assert(TheMap.Length > 0);
            int nXTiles = TheMap[0].Length;
            int nYTiles = TheMap.Length;

            // load the A-Star compatible map into memory
            List<List<Node>> aStarNodesLists = Utils.Init2DList<Node>(nXTiles, nYTiles);

            Point2D position = new();
            for (int x = 0; x < nXTiles; x++)
            {
                position.X = x;
                for (int y = 0; y < nYTiles; y++)
                {
                    position.Y = y;
                    TileReference currentTile = TheMapOverrides.HasOverrideTile(position)
                        ? TheMapOverrides.GetOverrideTileReference(position)
                        : GetOriginalTileReference(position);

                    bool bIsWalkable = IsTileWalkable(currentTile, walkableType) && !IsTileOccupied(position);

                    float fWeight = GetAStarWeight(position);

                    Node node = new(position.Copy(), bIsWalkable, fWeight);
                    aStarNodesLists[x].Add(node);
                }
            }

            return new AStar(aStarNodesLists);
        }

        /// <summary>
        ///     Gets a particular map unit on a tile in a given location
        /// </summary>
        /// <param name="xy"></param>
        /// <param name="nFloor"></param>
        /// <returns>MapUnit or null if non exist at location</returns>
        public List<MapUnit> GetMapUnitsByPosition(in Point2D xy, int nFloor)
        {
            List<MapUnit> mapUnits = new();

            // ReSharper disable once ForeachCanBeConvertedToQueryUsingAnotherGetEnumerator
            foreach (MapUnit mapUnit in CurrentMapUnits.AllMapUnits)
            {
                if (!mapUnit.IsActive) continue;

                // ReSharper disable once InvertIf
                if (mapUnit.MapUnitPosition.X == xy.X && mapUnit.MapUnitPosition.Y == xy.Y &&
                    mapUnit.MapUnitPosition.Floor == nFloor)
                {
                    int nTileIndex = mapUnit.KeyTileReference.Index;
                    if (GameStateReference.State.PlayerInventory.DoIHaveSpecialTileReferenceIndex(nTileIndex)) continue;
                    mapUnits.Add(mapUnit);
                }
            }

            return mapUnits;
        }

        /// <summary>
        ///     If an NPC is on a tile, then it will get them
        ///     assumes it's on the same floor
        /// </summary>
        /// <param name="xy"></param>
        /// <returns>the NPC or null if one does not exist</returns>
        public List<MapUnit> GetMapUnitsOnTile(in Point2D xy)
        {
            if (CurrentSingleMapReference == null)
                throw new Ultima5ReduxException("No single map is set in virtual map");

            List<MapUnit> mapUnits =
                GetMapUnitsByPosition(xy, CurrentSingleMapReference.Floor);

            return mapUnits;
        }

        public T GetSpecificMapUnitByLocation<T>(Point2D xy, int nFloor, bool bCheckBaseToo = false)
            where T : MapUnit
        {
            foreach (T mapUnit in CurrentMapUnits.GetMapUnitByType<T>())
            {
                if (mapUnit == null)
                    throw new Ultima5ReduxException(
                        "Getting a specific map unit by location, but the list has a null entry");
                // sometimes characters are null because they don't exist - and that is OK
                if (!mapUnit.IsActive) continue;

                // ReSharper disable once InvertIf
                if (mapUnit.MapUnitPosition.XY == xy &&
                    mapUnit.MapUnitPosition.Floor == nFloor)
                {
                    if (bCheckBaseToo && mapUnit.GetType().BaseType == typeof(T)) return mapUnit;
                    // the map unit is at the right position AND is the correct type
                    Debug.Assert(mapUnit != null);
                    if (mapUnit.GetType() == typeof(T)) return mapUnit;
                }
            }

            return null;
        }


        /// <summary>
        ///     Gets a tile reference from the given coordinate
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <returns></returns>
        public TileReference
            GetTileReference(int x, int y) => GetTileReference(new Point2D(x, y));

        /// <summary>
        ///     Gets a tile reference from the given coordinate
        /// </summary>
        /// <param name="xy"></param>
        /// <returns></returns>
        public TileReference GetTileReference(in Point2D xy)
        {
            // we check to see if our override map has something on top of it
            if (TheMapOverrides.HasOverrideTile(xy))
                return TheMapOverrides.GetOverrideTileReference(xy.X, xy.Y) ??
                       throw new Ultima5ReduxException("Expected tile override at " + xy);

            // the GetTileReference accounts for any forced overrides across the entire world
            return GetOriginalTileReference(xy);
        }


        /// <summary>
        ///     Gets the top visible map unit - excluding the Avatar
        /// </summary>
        /// <param name="xy"></param>
        /// <param name="bExcludeAvatar"></param>
        /// <returns>MapUnit or null</returns>
        // ReSharper disable once MemberCanBePrivate.Global
        [SuppressMessage("ReSharper", "ForeachCanBeConvertedToQueryUsingAnotherGetEnumerator")]
        [SuppressMessage("ReSharper", "ForeachCanBePartlyConvertedToQueryUsingAnotherGetEnumerator")]
        public MapUnit GetTopVisibleMapUnit(in Point2D xy, bool bExcludeAvatar)
        {
            List<MapUnit> mapUnits = GetMapUnitsOnTile(xy);

            // this is inefficient, but the lists are so small it is unlikely to matter
            foreach (Type type in _visiblePriorityOrder)
            {
                if (bExcludeAvatar && type == typeof(Avatar)) continue;
                foreach (MapUnit mapUnit in mapUnits)
                {
                    if (!mapUnit.IsActive) continue;
                    // if it's a combat unit but they dead or gone then we skip
                    if (mapUnit is CombatMapUnit { HasEscaped: true } and not NonAttackingUnit)
                        continue;

                    // we don't show NPCs who are now in our party
                    // ReSharper disable once MergeIntoLogicalPattern
                    if (mapUnit is NonPlayerCharacter { IsInParty: true }) continue;

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
        public int GuessTile(in Point2D xy)
        {
            Dictionary<int, int> tileCountDictionary = new();

            if (IsXyOverride(xy, TileOverrideReference.TileType.Flat))
                return GetTileOverride(xy).SpriteNum;

            for (int i = -1; i <= 1; i++)
            {
                for (int j = -1; j <= 1; j++)
                {
                    // if it is out of bounds then we skips them altogether
                    if (xy.X + i < 0 || xy.X + i >= NumOfXTiles || xy.Y + j < 0 || xy.Y + j >= NumOfYTiles)
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

                if (nMostTile != -1 && nTotal <= nMostTileTotal) continue;

                nMostTile = nTile;
                nMostTileTotal = nTotal;
            }

            // just in case we didn't find a match - just use grass for now
            return nMostTile == -1 ? 5 : nMostTile;
        }

        /// <summary>
        ///     Is there food on a table within 1 (4 way) tile
        ///     Used for determining if eating animation should be used
        /// </summary>
        /// <param name="characterPos"></param>
        /// <returns>true if food is within a tile</returns>
        public bool IsFoodNearby(in Point2D characterPos)
        {
            bool isFoodTable(int nSprite) =>
                nSprite == GameReferences.Instance.SpriteTileReferences.GetTileReferenceByName("TableFoodTop").Index ||
                nSprite == GameReferences.Instance.SpriteTileReferences.GetTileReferenceByName("TableFoodBottom")
                    .Index ||
                nSprite == GameReferences.Instance.SpriteTileReferences.GetTileReferenceByName("TableFoodBoth").Index;

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
        [SuppressMessage("ReSharper", "UnusedMember.Global")]
        public bool IsHorizDoor(in Point2D xy)
        {
            if (xy.X - 1 < 0 || xy.X + 1 >= NumOfXTiles) return false;
            if (xy.Y - 1 < 0 || xy.Y + 1 >= NumOfYTiles) return true;

            int nOpenSpacesHorizOnX = (GetTileReference(xy.X - 1, xy.Y).IsSolidSpriteButNotNPC ? 0 : 1)
                                      + (GetTileReference(xy.X + 1, xy.Y).IsSolidSpriteButNotNPC ? 0 : 1);

            return nOpenSpacesHorizOnX == 0;
        }

        [SuppressMessage("ReSharper", "UnusedMember.Global")]
        public bool IsHorizTombstone(in Point2D xy)
        {
            if (xy.X - 1 < 0 || xy.X + 1 >= NumOfXTiles) return false;
            if (xy.Y - 1 < 0 || xy.Y + 1 >= NumOfYTiles) return true;

            int nOpenSpacesHorizOnX = (GetTileReference(xy.X - 1, xy.Y).IsSolidSpriteButNotNPC ? 0 : 1)
                                      + (GetTileReference(xy.X + 1, xy.Y).IsSolidSpriteButNotNPC ? 0 : 1);
            return nOpenSpacesHorizOnX == 0;
        }

        public bool IsLandNearby(in Avatar.AvatarState avatarState) =>
            IsCurrentPositionFreeToMoveDirection(Point2D.Direction.Down, avatarState) ||
            IsCurrentPositionFreeToMoveDirection(Point2D.Direction.Up, avatarState) ||
            IsCurrentPositionFreeToMoveDirection(Point2D.Direction.Left, avatarState) ||
            IsCurrentPositionFreeToMoveDirection(Point2D.Direction.Right, avatarState);

        /// <summary>
        ///     Is there an NPC on the tile specified?
        /// </summary>
        /// <param name="xy"></param>
        /// <returns></returns>
        public bool IsMapUnitOccupiedTile(in Point2D xy) =>
            IsMapUnitOccupiedFromList(xy, CurrentSingleMapReference.Floor,
                CurrentMapUnits.AllActiveMapUnits);

        public bool IsXyOverride(in Point2D xy, TileOverrideReference.TileType tileType) =>
            // this is kind of hacky - but the map is only aware of the primary tile, so if the override is 
            // FLAT then we ignore it and find it later with VirtualMap
            XyOverrides != null && XyOverrides.ContainsKey(xy) &&
            XyOverrides[xy].TheTileType == tileType;

        /// <summary>
        ///     Sets an override for the current tile which will be favoured over the static map tile
        /// </summary>
        /// <param name="tileReference">the reference (sprite)</param>
        /// <param name="xy"></param>
        public void SetOverridingTileReference(TileReference tileReference, Point2D xy)
        {
            TheMapOverrides.SetOverrideTile(xy, tileReference);
        }

        protected static bool IsMapUnitOccupiedFromList(in Point2D xy, int nFloor, IEnumerable<MapUnit> mapUnits)
        {
            // ReSharper disable once LoopCanBeConvertedToQuery
            foreach (MapUnit mapUnit in mapUnits)
                // sometimes characters are null because they don't exist - and that is OK
            {
                if (!mapUnit.MapUnitPosition.IsSameAs(xy.X, xy.Y, nFloor)) continue;
                // TEST: this may break stuff
                // if you can stack map units, then we will just leave it out of the list since this is 
                // used to set walkable tiles
                if (mapUnit.CanStackMapUnitsOnTop) continue;
                // check to see if the particular SPECIAL map unit is in your inventory, if so, then we exclude it
                // for example it looks for crown, sceptre and amulet
                if (!GameStateReference.State.PlayerInventory.DoIHaveSpecialTileReferenceIndex(
                        mapUnit.KeyTileReference.Index)) return true;
            }

            return false;
        }

        /// <summary>
        ///     Prints the map in ASCII on the console
        /// </summary>
        /// <param name="map">map object</param>
        /// <param name="xOffset">where to start the top left origin (row) </param>
        /// <param name="yOffset">where to start the top left origin (column) </param>
        /// <param name="xTilesToPrint">how many tiles to print vertically</param>
        /// <param name="yTilesToPrint">how many tiles to print horizontally</param>
        protected static void PrintMapSection(byte[][] map, int xOffset, int yOffset, int xTilesToPrint,
            int yTilesToPrint)
        {
            for (int curRow = yOffset; curRow < yTilesToPrint + yOffset; curRow++)
            {
                for (int curCol = xOffset; curCol < xTilesToPrint + xOffset; curCol++)
                {
                    if (curCol % xTilesToPrint == 0) Console.WriteLine("");
                    byte mapTile = map[curCol][curRow];
                    Console.Write(GetMapLetter(mapTile));
                }
            }
        }

        /// <summary>
        ///     Calculates an appropriate A* weight based on the current tile as well as the surrounding tiles
        /// </summary>
        /// <param name="xy"></param>
        /// <returns></returns>
        protected abstract float GetAStarWeight(in Point2D xy);

        protected virtual bool IsTileWalkable(TileReference tileReference, WalkableType walkableType)
        {
            if (walkableType == WalkableType.CombatWater)
            {
                return tileReference.IsWaterEnemyPassable;
            }

            bool bIsWalkable = tileReference.IsWalking_Passable ||
                               tileReference.Index == GameReferences.Instance.SpriteTileReferences
                                   .GetTileReferenceByName("RegularDoor").Index ||
                               tileReference.Index == GameReferences.Instance.SpriteTileReferences
                                   .GetTileReferenceByName("RegularDoorView").Index;
            return bIsWalkable;
        }

        // ReSharper disable once NotAccessedField.Global
        // ReSharper disable once MemberCanBePrivate.Global

        /// <summary>
        ///     Adds a new map unit to the next position available
        /// </summary>
        /// <param name="mapUnit"></param>
        /// <returns>true if successful, false if no room was found</returns>
        // ReSharper disable once UnusedMethodReturnValue.Local
        [SuppressMessage("ReSharper", "UnusedMethodReturnValue.Global")]
        protected bool AddNewMapUnit(MapUnit mapUnit)
        {
            int nIndex = FindNextFreeMapUnitIndex();
            return AddNewMapUnit(mapUnit, nIndex);
        }

        /// <summary>
        ///     Adds a new map unit to a given position
        /// </summary>
        /// <param name="mapUnit"></param>
        /// <param name="nIndex"></param>
        /// <returns></returns>
        protected bool AddNewMapUnit(MapUnit mapUnit, int nIndex)
        {
            if (nIndex == -1) return false;

            List<MapUnit> mapUnits = CurrentMapUnits.AllMapUnits;
            Debug.Assert(nIndex < mapUnits.Count);
            mapUnits[nIndex] = mapUnit;
            mapUnit.UseFourDirections = UseExtendedSprites;
            return true;
        }

        [SuppressMessage("ReSharper", "OutParameterValueIsAlwaysDiscarded.Global")]
        protected Enemy CreateEnemy(Point2D xy, EnemyReference enemyReference,
            SmallMapReferences.SingleMapReference singleMapReference, out int nIndex)
        {
            Debug.Assert(TheMapType != Maps.Combat);

            nIndex = FindNextFreeMapUnitIndex();
            if (nIndex == -1) return null;

            MapUnitPosition mapUnitPosition = new(xy.X, xy.Y, singleMapReference.Floor);
            Enemy enemy = new(new MapUnitMovement(0), enemyReference, singleMapReference.MapLocation, null,
                mapUnitPosition);

            CurrentMapUnits.AddMapUnit(enemy);

            enemy.UseFourDirections = UseExtendedSprites;

            return enemy;
        }

        /// <summary>
        ///     Finds the next available index in the available map unit list
        /// </summary>
        /// <returns>>= 0 is an index, or -1 means no room found</returns>
        protected int FindNextFreeMapUnitIndex()
        {
            int nIndex = 0;
            foreach (MapUnit mapUnit in CurrentMapUnits.AllMapUnits)
            {
                if (mapUnit is EmptyMapUnit) return nIndex;

                nIndex++;
            }

            return -1;
        }

        protected bool IsLandNearby(in Point2D xy, bool bNoStairCases, Avatar.AvatarState avatarState) =>
            IsTileFreeToTravel(xy.GetAdjustedPosition(Point2D.Direction.Down), bNoStairCases, avatarState) ||
            IsTileFreeToTravel(xy.GetAdjustedPosition(Point2D.Direction.Up), bNoStairCases, avatarState) ||
            IsTileFreeToTravel(xy.GetAdjustedPosition(Point2D.Direction.Left), bNoStairCases, avatarState) ||
            IsTileFreeToTravel(xy.GetAdjustedPosition(Point2D.Direction.Right), bNoStairCases, avatarState);

        protected bool IsTileOccupied(Point2D xy)
        {
            return CurrentMapUnits.AllActiveMapUnits.Any(m => m.MapUnitPosition.XY == xy);
        }

        protected bool IsTileWalkable(in Point2D xy, WalkableType walkableType)
        {
            if (IsOpenDoor(xy)) return true;
            TileReference tileReference = GetTileReference(xy);
            return IsTileWalkable(tileReference, walkableType);
        }

        #region FLOOD FILL

        // FLOOD FILL STUFF
        public bool[][] VisibleOnMap { get; protected set; }
        private readonly List<bool[][]> _testForVisibility = new();

        protected const int VISIBLE_IN_EACH_DIRECTION_OF_AVATAR = 10;
        protected Point2D AvatarXyPos;
        internal bool TouchedOuterBorder { get; set; }

        // ReSharper disable once MemberCanBeProtected.Global
        public abstract bool IsRepeatingMap { get; }

        internal void ClearOpenDoors()
        {
            _openDoors.Clear();
        }

        [SuppressMessage("ReSharper", "UnusedMethodReturnValue.Local")]
        private bool SetVisibleTile(int x, int y)
        {
            if (x < 0 || x > NumOfXTiles - 1 || y < 0 || y > NumOfYTiles - 1)
            {
                return false;
            }

            VisibleOnMap[x][y] = true;
            return true;
        }

        private void SetSurroundingTilesVisible(int x, int y, bool bIncludeDiagonal)
        {
            SetVisibleTile(x - 1, y);
            SetVisibleTile(x + 1, y);
            SetVisibleTile(x, y - 1);
            SetVisibleTile(x, y + 1);
            if (!bIncludeDiagonal) return;
            SetVisibleTile(x - 1, y - 1);
            SetVisibleTile(x + 1, y + 1);
            SetVisibleTile(x - 1, y + 1);
            SetVisibleTile(x + 1, y - 1);
        }

        /// <summary>
        ///     Recursive method for determining which tiles are visible and which are hidden based on the Avatar's
        ///     current position
        /// </summary>
        /// <param name="y"></param>
        /// <param name="bFirst">is this the initial call to the method?</param>
        /// <param name="nCharacterIndex"></param>
        /// <param name="overrideAvatarPos"></param>
        /// <param name="bAlwaysLookThroughWindows"></param>
        /// <param name="x"></param>
        protected void FloodFillMap(int x, int y, bool bFirst, int nCharacterIndex = 0,
            Point2D overrideAvatarPos = null, bool bAlwaysLookThroughWindows = false)
        {
            if (bFirst)
            {
                VisibleOnMap = Utils.Init2DBoolArray(NumOfXTiles, NumOfYTiles);
            }

            Point2D characterPosition = overrideAvatarPos == null ? AvatarXyPos : overrideAvatarPos;

            int nAdjustedX = x, nAdjustedY = y;

            // let's check to make sure it is within bounds
            if (IsRepeatingMap)
            {
                nAdjustedX = Point2D.AdjustToMax(x, NumOfXTiles);
                nAdjustedY = Point2D.AdjustToMax(y, NumOfYTiles);
            }
            else
            {
                if (nAdjustedX < 0 || nAdjustedY < 0) return;
                if (nAdjustedX > NumOfXTiles - 1 || nAdjustedY > NumOfYTiles - 1) return;
            }

            Point2D adjustedPosition = new(nAdjustedX, nAdjustedY);

            if (_testForVisibility[nCharacterIndex][nAdjustedX][nAdjustedY]) return; // already did it
            _testForVisibility[nCharacterIndex][nAdjustedX][nAdjustedY] = true;

            // if it blocks light then we make it visible but do not make subsequent tiles visible
            TileReference tileReference = GetTileReference(adjustedPosition);

            bool bBlocksLight = tileReference.BlocksLight // if it says it blocks light AND 
                                && !bFirst // it is not the first tile (aka the one you are on) AND
                                && !(tileReference.IsWindow &&
                                     (characterPosition.IsWithinNFourDirections(nAdjustedX, nAdjustedY) ||
                                      bAlwaysLookThroughWindows))
                                && !IsOpenDoor(new Point2D(x, y)) // it's not an open door 
                ; //  you are not next to a window

            // if we are on a tile that doesn't block light then we automatically see things in every direction
            if (!bBlocksLight)
            {
                SetSurroundingTilesVisible(nAdjustedX, nAdjustedY, true);
            }

            // if we are this far then we are certain that we will make this tile visible
            SetVisibleTile(nAdjustedX, nAdjustedY);

            // if the tile blocks the light then we don't calculate the surrounding tiles
            if (bBlocksLight) return;

            int nTilesMax = NumOfXTiles - 1;

            void floodFillIfInside(int nXDiff, int nYDiff)
            {
                // if we aren't on a repeating map then we check to see if it is out of bounds
                // if we are then we note that the flood fill hit the edges
                if (!bFirst && !IsRepeatingMap &&
                    Point2D.IsOutOfRangeStatic(nAdjustedX + nXDiff, nAdjustedY + nYDiff, nTilesMax, nTilesMax))
                {
                    TouchedOuterBorder = true;
                    return;
                }

                FloodFillMap(nAdjustedX + nXDiff, nAdjustedY + nYDiff, false, nCharacterIndex, characterPosition,
                    bAlwaysLookThroughWindows);
            }

            floodFillIfInside(0, 1);
            floodFillIfInside(1, 0);
            floodFillIfInside(0, -1);
            floodFillIfInside(-1, 0);

            if (!bFirst) return;

            // we ONLY do the diagonals during the very first 
            floodFillIfInside(-1, -1);
            floodFillIfInside(1, 1);
            floodFillIfInside(-1, +1);
            floodFillIfInside(1, -1);
        }

        // protected Point2D GetAdjustedPos(in Point2D.Direction direction, in Point2D xy) =>
        //     xy.GetAdjustedPosition(direction, NumOfXTiles - 1, NumOfYTiles - 1);

        /// <summary>
        ///     Refreshes the map that tracks which tiles have been tested for visibility
        /// </summary>
        /// <param name="nCharacters"></param>
        protected void RefreshTestForVisibility(int nCharacters)
        {
            _testForVisibility.Clear();
            for (int i = 0; i < nCharacters; i++)
            {
                _testForVisibility.Add(Utils.Init2DBoolArray(NumOfXTiles, NumOfYTiles));
            }
        }

        protected void SetMaxVisibleArea(in Point2D startPos, int nVisibleTiles)
        {
            if (nVisibleTiles < 3) throw new Ultima5ReduxException("Can't set visible area if smaller than 3");
            if (startPos == null) throw new Ultima5ReduxException("Must have a proper start position");
            if (_testForVisibility.Count <= 0)
                throw new Ultima5ReduxException("You must refresh the visible area before setting the max");

            int nVisibleTilesPerSide = nVisibleTiles / 2;
            int nStartX = startPos.X, nStartY = startPos.Y;

            for (int nXDiff = 0; nXDiff < nVisibleTiles; nXDiff++)
            {
                int nX = Point2D.AdjustToMax(nStartX - nVisibleTilesPerSide + nXDiff, NumOfXTiles);
                int nTopY = Point2D.AdjustToMax(nStartY - nVisibleTilesPerSide, NumOfYTiles);
                int nBottomY = Point2D.AdjustToMax(nStartY + nVisibleTilesPerSide, NumOfYTiles);

                _testForVisibility[0][nX][nTopY] = true;
                _testForVisibility[0][nX][nBottomY] = true;
            }

            for (int nYDiff = 0; nYDiff < nVisibleTiles; nYDiff++)
            {
                int nY = Point2D.AdjustToMax(nStartY - nVisibleTilesPerSide + nYDiff, NumOfYTiles);
                int nTopX = Point2D.AdjustToMax(nStartX - nVisibleTilesPerSide, NumOfXTiles);
                int nBottomX = Point2D.AdjustToMax(nStartX + nVisibleTilesPerSide, NumOfXTiles);

                _testForVisibility[0][nTopX][nY] = true;
                _testForVisibility[0][nBottomX][nY] = true;
            }
        }

        public virtual void RecalculateVisibleTiles(in Point2D initialFloodFillPosition)
        {
            // XRay Mode makes sure you can see every tile
            if (XRayMode)
            {
                Utils.Set2DArrayAllToValue(VisibleOnMap, true);
                return;
            }

            TouchedOuterBorder = false;
            AvatarXyPos = initialFloodFillPosition;

            RefreshTestForVisibility(1);
            SetMaxVisibleArea(AvatarXyPos, TOTAL_VISIBLE_TILES);
            FloodFillMap(initialFloodFillPosition.X, initialFloodFillPosition.Y, true);
            RecalculatedHash = Utils.Ran.Next();
        }

        internal void SetOpenDoor(in Point2D xy)
        {
            TileReference tileReference = GetOriginalTileReference(xy);
            Debug.Assert(TileReferences.IsDoor(tileReference.Index),
                "you tried to set an open door on a tile that is not an open door");

            _openDoors.Add(xy, 10);
        }

        private bool IsOpenDoor(in Point2D xy) => _openDoors.ContainsKey(xy) && _openDoors[xy] > 0;

        [SuppressMessage("ReSharper", "UnusedMember.Global")]
        public void CloseDoor(in Point2D xy)
        {
            TileReference tileReference = GetOriginalTileReference(xy);
            Debug.Assert(TileReferences.IsDoor(tileReference.Index),
                "you tried to set an open door on a tile that is not an open door");
            Debug.Assert(_openDoors.ContainsKey(xy), "tried to close a door that wasn't open");

            _openDoors.Remove(xy);
        }

        #endregion
    }
}