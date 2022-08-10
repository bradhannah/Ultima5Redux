using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.Serialization;
using Newtonsoft.Json;
using Ultima5Redux.External;
using Ultima5Redux.MapUnits;
using Ultima5Redux.MapUnits.CombatMapUnits;
using Ultima5Redux.MapUnits.NonPlayerCharacters;
using Ultima5Redux.MapUnits.SeaFaringVessels;
using Ultima5Redux.MapUnits.TurnResults;
using Ultima5Redux.PlayerCharacters;
using Ultima5Redux.PlayerCharacters.Inventory;
using Ultima5Redux.References;
using Ultima5Redux.References.Maps;
using Ultima5Redux.References.MapUnits.NonPlayerCharacters;
using Ultima5Redux.References.PlayerCharacters.Inventory;

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
        [DataMember(Name = "LargeMaps")] private readonly Dictionary<Map.Maps, LargeMap> _largeMaps = new(2);

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
        private MapUnitPosition PreMapUnitPosition { get; set; } = new();

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

        [DataMember] public int OneInXOddsOfNewMonster { get; set; } = 16;

        [DataMember] public MapUnits.MapUnits TheMapUnits { get; private set; }

        [IgnoreDataMember] private SmallMapReferences.SingleMapReference _currentSingleMapReference;

        /// <summary>
        ///     Current large map (null if on small map)
        /// </summary>
        // ReSharper disable once MemberCanBePrivate.Global
        [IgnoreDataMember]
        public LargeMap CurrentLargeMap => _largeMaps[LargeMapOverUnder];

        /// <summary>
        ///     The abstracted Map object for the current map
        ///     Returns large or small depending on what is active
        /// </summary>
        [IgnoreDataMember]
        public Map CurrentMap
        {
            get
            {
                SmallMapReferences.SingleMapReference currentMap = CurrentSingleMapReference;
                if (currentMap == null)
                    return null;

                switch (currentMap.MapLocation)
                {
                    case SmallMapReferences.SingleMapReference.Location.Combat_resting_shrine:
                        return CurrentCombatMap;
                    case SmallMapReferences.SingleMapReference.Location.Britannia_Underworld:
                        return currentMap.Floor == 0 ? OverworldMap : UnderworldMap;
                    default:
                        return CurrentSmallMap;
                }
            }
        }

        [IgnoreDataMember]
        public bool IsAvatarInFrigate => TheMapUnits.GetAvatarMapUnit().CurrentBoardedMapUnit is Frigate;

        [IgnoreDataMember] public bool IsAvatarInSkiff => TheMapUnits.GetAvatarMapUnit().CurrentBoardedMapUnit is Skiff;

        [IgnoreDataMember]
        public bool IsAvatarRidingCarpet =>
            TheMapUnits.GetAvatarMapUnit().CurrentBoardedMapUnit is MagicCarpet;

        [IgnoreDataMember]
        public bool IsAvatarRidingHorse => TheMapUnits.GetAvatarMapUnit().CurrentBoardedMapUnit is Horse;

        [IgnoreDataMember] public bool IsAvatarRidingSomething => TheMapUnits.GetAvatarMapUnit().IsAvatarOnBoardedThing;

        [IgnoreDataMember]
        public bool IsBasement
        {
            get
            {
                if (CurrentSingleMapReference == null) return false;

                return !IsLargeMap && CurrentSingleMapReference.Floor == -1;
            }
        }

        [IgnoreDataMember] public bool IsCombatMap => CurrentMap is CombatMap;

        /// <summary>
        ///     Are we currently on a large map?
        /// </summary>
        [IgnoreDataMember]
        public bool IsLargeMap => LargeMapOverUnder != Map.Maps.Small;

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

        [IgnoreDataMember]
        public MapUnitPosition CurrentPosition
        {
            get
            {
                if (CurrentMap is CombatMap combatMap)
                {
                    return combatMap?.CurrentCombatPlayer?.MapUnitPosition;
                }

                return TheMapUnits?.CurrentAvatarPosition;
            }
            set
            {
                if (CurrentMap is not CombatMap combatMap)
                {
                    TheMapUnits.CurrentAvatarPosition = value;
                    return;
                }

                // although this could work - it's really not how we should change the player characters position
                combatMap.CurrentCombatPlayer.MapUnitPosition = value;
            }
        }

        private readonly List<Type> _visibilePriorityOrder = new()
        {
            typeof(Horse), typeof(MagicCarpet), typeof(Skiff), typeof(Frigate), typeof(NonPlayerCharacter),
            typeof(Enemy), typeof(CombatPlayer), typeof(Avatar), typeof(ItemStack), typeof(StackableItem),
            typeof(Chest), typeof(DeadBody), typeof(BloodSpatter), typeof(ElementalField), typeof(Whirlpool)
        };

        public IEnumerable<MapUnit> AllVisibleActiveMapUnits
        {
            get
            {
                IEnumerable<Point2D> allPoints =
                    TheMapUnits.CurrentMapUnits.AllActiveMapUnits.Select(e => e.MapUnitPosition.XY);
                IEnumerable<MapUnit> visibleMapUnits =
                    allPoints.Select(point => GetTopVisibleMapUnit(point, false));
                return visibleMapUnits;
            }
        }

        public MapUnitPosition PreviousPosition { get; } = new();

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
                    LoadSmallMap(currentSmallMapReference, null, !importedGameState.IsInitialSaveFile);
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

        [OnDeserialized] private void PostDeserialize(StreamingContext context)
        {
            TheMapOverrides.TheMap = CurrentMap;
        }

        internal void DamageShip(Point2D.Direction windDirection, TurnResults turnResults)
        {
            Avatar avatar = TheMapUnits.GetAvatarMapUnit();

            int nDamage = Utils.Ran.Next(5, 15);

            Debug.Assert(avatar.CurrentBoardedMapUnit is Frigate);
            if (avatar.CurrentBoardedMapUnit is not Frigate frigate)
                throw new Ultima5ReduxException("Tried to get Avatar's frigate, but it returned  null");

            // if the wind is blowing the same direction then we double the damage
            if (avatar.Direction == windDirection) nDamage *= 2;
            // decrement the damage from the frigate
            frigate.Hitpoints -= nDamage;

            StreamingOutput.Instance.PushMessage(
                GameReferences.DataOvlRef.StringReferences.GetString(DataOvlReference.WorldStrings
                    .BREAKING_UP), false);
            // if we hit zero hitpoints then the ship is destroyed and a skiff is boarded
            if (frigate.Hitpoints <= 0)
            {
                turnResults.PushTurnResult(new BasicResult(TurnResult.TurnResultType.ActionMoveShipDestroyed));
                // destroy the ship and leave board the Avatar onto a skiff
                StreamingOutput.Instance.PushMessage(GameReferences.DataOvlRef.StringReferences.GetString(
                    DataOvlReference.WorldStrings2
                        .SHIP_SUNK_BANG_N), false);
                StreamingOutput.Instance.PushMessage(GameReferences.DataOvlRef.StringReferences
                    .GetString(DataOvlReference.WorldStrings2.ABANDON_SHIP_BANG_N).TrimEnd(), false);

                MapUnit newFrigate =
                    TheMapUnits.XitCurrentMapUnit(this, out string _);
                TheMapUnits.ClearAndSetEmptyMapUnits(newFrigate);
                TheMapUnits.MakeAndBoardSkiff();
            }
            else
            {
                if (frigate.Hitpoints <= 10)
                {
                    StreamingOutput.Instance.PushMessage(GameReferences.DataOvlRef.StringReferences.GetString(
                        DataOvlReference.WorldStrings
                            .HULL_WEAK), false);
                }

                turnResults.PushTurnResult(new BasicResult(TurnResult.TurnResultType.ActionMoveShipBreakingUp));
            }
        }

        /// <summary>
        ///     Decides if any enemies needed to be spawned or despawned
        /// </summary>
        internal void GenerateAndCleanupEnemies(int nTurn)
        {
            switch (CurrentSingleMapReference.MapType)
            {
                case Map.Maps.Overworld:
                case Map.Maps.Underworld:
                    // let's do this!
                    //const int nOddsOfNewMonster = 2;
                    //const int nMaxEnemyDistance = 16;

                    TheMapUnits.ClearEnemiesIfFarAway();

                    if (TheMapUnits.TotalMapUnitsOnMap >= MapUnits.MapUnits.MAX_MAP_CHARACTERS) break;
                    if (OneInXOddsOfNewMonster > 0 && Utils.OneInXOdds(OneInXOddsOfNewMonster))
                    {
                        // make a random monster
                        CreateRandomMonster(nTurn);
                    }

                    break;
                case Map.Maps.Combat:
                    break;
                case Map.Maps.Small:
                    break;
                case Map.Maps.Dungeon:
                    break;
                default:
                    throw new InvalidEnumArgumentException();
            }
        }

        /// <summary>
        ///     Gathers the details of what if any aggressive action the mapunits would do this turn
        /// </summary>
        /// <returns></returns>
        internal Dictionary<MapUnit, AggressiveMapUnitInfo> GetAggressiveMapUnitInfo()
        {
            Dictionary<MapUnit, AggressiveMapUnitInfo> aggressiveMapUnitInfos = new();

            foreach (MapUnit mapUnit in TheMapUnits.CurrentMapUnits.AllActiveMapUnits)
            {
                // we don't want to add anything that can never attack, so we keep only enemies and NPCs 
                // in the list of aggressors
                if (mapUnit is not Enemy && mapUnit is not NonPlayerCharacter)
                {
                    continue;
                }

                AggressiveMapUnitInfo mapUnitInfo =
                    GetAggressiveMapUnitInfo(
                        mapUnit.MapUnitPosition.XY,
                        TheMapUnits.CurrentAvatarPosition.XY,
                        SingleCombatMapReference.Territory.Britannia, mapUnit);

                if (mapUnitInfo.CombatMapReference != null)
                    StreamingOutput.Instance.PushMessage(mapUnitInfo.AttackingMapUnit.FriendlyName + " fight me in " +
                                                         mapUnitInfo.CombatMapReference.Description);
                // it's not an aggressive Npc or Enemy so skip on past - nothing to see here
                aggressiveMapUnitInfos.Add(mapUnit, mapUnitInfo);
            }

            return aggressiveMapUnitInfos;
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
            List<Point2D> bestChoiceList = new(sortedPoints.Count);

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
            List<Point2D> bestChoiceList = new(sortedPoints.Count);

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
            Random ran = new();
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

        internal bool IsTileFreeToTravel(in Point2D xy, bool bNoStaircases = false)
        {
            return IsTileFreeToTravel(xy, bNoStaircases, TheMapUnits.GetAvatarMapUnit().CurrentAvatarState);
        }

        internal bool IsTileFreeToTravel(in Point2D xy, bool bNoStaircases, Avatar.AvatarState forcedAvatarState)
        {
            return IsTileFreeToTravelForAvatar(CurrentPosition.XY, xy, bNoStaircases, forcedAvatarState);
        }

        /// <summary>
        ///     Is the particular tile eligible to be moved onto
        /// </summary>
        /// <param name="currentPosition"></param>
        /// <param name="newPosition"></param>
        /// <param name="bNoStaircases"></param>
        /// <param name="forcedAvatarState"></param>
        /// <returns>true if you can move onto the tile</returns>
        internal bool IsTileFreeToTravelForAvatar(in Point2D currentPosition, in Point2D newPosition,
            bool bNoStaircases,
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
            return bIsWalkable && !bIsAvatarTile;
        }

        /// <summary>
        ///     Advances each of the NPCs by one movement each
        /// </summary>
        internal void MoveMapUnitsToNextMove(Dictionary<MapUnit, AggressiveMapUnitInfo> aggressiveMapUnitInfos)
        {
            // go through each of the NPCs on the map
            foreach (MapUnit mapUnit in TheMapUnits.CurrentMapUnits.AllActiveMapUnits)
            {
                AggressiveMapUnitInfo aggressiveMapUnitInfo =
                    aggressiveMapUnitInfos.ContainsKey(mapUnit) ? aggressiveMapUnitInfos[mapUnit] : null;
                // if we don't match the aggressive map unit then it means the map unit is not mobile
                if (aggressiveMapUnitInfo == null) continue;

                // if the map unit doesn't haven't a particular aggression then it moves 
                if (aggressiveMapUnitInfo.GetDecidedAction() == AggressiveMapUnitInfo.DecidedAction.MoveUnit)
                    mapUnit.CompleteNextMove(this, GameStateReference.State.TheTimeOfDay,
                        CurrentMap.GetAStarByMapUnit(mapUnit));
            }
        }

        // Performs all of the aggressive actions and stores results
        internal void ProcessAggressiveMapUnitAttacks(PlayerCharacterRecords records,
            Dictionary<MapUnit, AggressiveMapUnitInfo> aggressiveMapUnitInfos,
            out AggressiveMapUnitInfo combatMapAggressor, TurnResults turnResults)
        {
            combatMapAggressor = null;

            // if there are monsters with combat maps attached to them - then we look at them first
            // if you are going to a combat map then we will never process overworld ranged and melee attacks
            List<AggressiveMapUnitInfo> aggressiveMapUnitInfosWithCombatMaps =
                aggressiveMapUnitInfos.Values.Where(ag => ag.CombatMapReference != null).ToList();

            if (aggressiveMapUnitInfosWithCombatMaps.Count > 0)
            {
                // there is at least one combat map reference
                int nCombatMapEnemies = aggressiveMapUnitInfosWithCombatMaps.Count;

                int nChoice = Utils.GetNumberFromAndTo(0, nCombatMapEnemies - 1);

                combatMapAggressor = aggressiveMapUnitInfosWithCombatMaps[nChoice];
                aggressiveMapUnitInfos.Clear();
                aggressiveMapUnitInfos[combatMapAggressor.AttackingMapUnit] = combatMapAggressor;
                return;
            }

            //IsAvatarInFrigate
            //IsAvatarInSkiff

            // we are certain at this point that there is no combat map, so it's all ranged if anything at all
            foreach (KeyValuePair<MapUnit, AggressiveMapUnitInfo> kvp in aggressiveMapUnitInfos)
            {
                AggressiveMapUnitInfo aggressiveMapUnitInfo = kvp.Value;
                MapUnit mapUnit = kvp.Key;

                Debug.Assert(aggressiveMapUnitInfo.CombatMapReference == null);

                // bajh: I know all the conditions look identical now - but I suspect they have different attack
                // powers I will tweak later

                // it's possible that the aggressor may not actually be attacking even if they can
                if (aggressiveMapUnitInfo.GetDecidedAction() !=
                    AggressiveMapUnitInfo.DecidedAction.RangedAttack) continue;

                switch (aggressiveMapUnitInfo.AttackingMissileType)
                {
                    case CombatItemReference.MissileType.None:
                        break;

                    case CombatItemReference.MissileType.Arrow:
                        // do they have any melee attacks? Melee attacks are noted with .Arrow for now
                        // if on skiff then party takes damage
                        // if on frigate then frigate takes damage
                        if (IsAvatarInFrigate)
                        {
                            // frigate takes damage instead
                            DamageShip(Point2D.Direction.None, turnResults);
                        }
                        else
                        {
                            records.DamageEachCharacter(1, 9);
                        }

                        StreamingOutput.Instance.PushMessage(
                            $"{mapUnit.FriendlyName} attacks {records.AvatarRecord.Name} and party (melee)", false);
                        continue;
                    case CombatItemReference.MissileType.CannonBall:
                        if (IsAvatarInFrigate)
                        {
                            DamageShip(Point2D.Direction.None, turnResults);
                        }
                        else
                        {
                            records.DamageEachCharacter(1, 9);
                        }

                        StreamingOutput.Instance.PushMessage(
                            $"{mapUnit.FriendlyName} attacks {records.AvatarRecord.Name} and party (cannonball)",
                            false);

                        continue;
                    case CombatItemReference.MissileType.Red:
                        // if on a frigate then only the frigate takes damage, like a shield!
                        if (IsAvatarInFrigate)
                        {
                            DamageShip(Point2D.Direction.None, turnResults);
                        }
                        else
                        {
                            records.DamageEachCharacter(1, 9);
                        }

                        StreamingOutput.Instance.PushMessage(
                            $"{mapUnit.FriendlyName} attacks {records.AvatarRecord.Name} and party (ranged)", false);

                        continue;
                    default:
                        throw new Ultima5ReduxException(
                            "Only \"Red\" and CannonBall ranged attacks have been configured");
                }
            }
        }

        internal void SavePreviousPosition(MapUnitPosition mapUnitPosition)
        {
            PreviousPosition.X = mapUnitPosition.X;
            PreviousPosition.Y = mapUnitPosition.Y;
            PreviousPosition.Floor = mapUnitPosition.Floor;
        }

        internal InventoryItem SearchAndExposeInventoryItem(in Point2D xy)
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

                return invItem;
            }

            return null;
        }

        internal bool SearchNonAttackingMapUnit(in Point2D xy, TurnResults turnResults,
            PlayerCharacterRecord record, PlayerCharacterRecords records)
        {
            List<MapUnit> mapUnits = GetMapUnitsOnTile(xy);
            foreach (MapUnit mapUnit in mapUnits)
            {
                if (mapUnit is not NonAttackingUnit nonAttackingUnit) continue;
                if (!nonAttackingUnit.IsSearchable) continue;

                ProcessSearchNonAttackUnitTrap(turnResults, record, records, nonAttackingUnit);

                // we only open the inner items up if it exposes on search like DeadBodies and Spatters
                if (nonAttackingUnit.ExposeInnerItemsOnSearch)
                    return ProcessSearchInnerItems(turnResults, nonAttackingUnit, true, false);
            }

            return false;
        }

        private static void ProcessSearchNonAttackUnitTrap(TurnResults turnResults, PlayerCharacterRecord record,
            PlayerCharacterRecords records, NonAttackingUnit nonAttackingUnit)
        {
            if (!nonAttackingUnit.IsTrapped)
            {
                StreamingOutput.Instance.PushMessage(
                    U5StringRef.ThouDostFind(GameReferences.DataOvlRef.StringReferences.GetString(
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
                        StreamingOutput.Instance.PushMessage(
                            U5StringRef.ThouDostFind(
                                GameReferences.DataOvlRef.StringReferences.GetString(DataOvlReference.ThingsIFindStrings
                                    .A_SIMPLE_TRAP_BANG_N)));
                        nonAttackingUnit.TriggerTrap(turnResults, record.Stats, records);
                        turnResults.PushTurnResult(
                            new BasicResult(TurnResult.TurnResultType.ActionSearchTriggerSimpleTrap));
                    }
                    else
                    {
                        StreamingOutput.Instance.PushMessage(
                            U5StringRef.ThouDostFind(
                                GameReferences.DataOvlRef.StringReferences.GetString(DataOvlReference.ThingsIFindStrings
                                    .A_SIMPLE_TRAP_N)));
                        turnResults.PushTurnResult(new BasicResult(TurnResult.TurnResultType.ActionSearchRemoveSimple));
                    }

                    break;
                case NonAttackingUnit.TrapComplexity.Complex:
                    if (bTriggeredTrap)
                    {
                        StreamingOutput.Instance.PushMessage(
                            U5StringRef.ThouDostFind(
                                GameReferences.DataOvlRef.StringReferences.GetString(DataOvlReference.ThingsIFindStrings
                                    .A_COMPLEX_TRAP_BANG_N)));
                        nonAttackingUnit.TriggerTrap(turnResults, record.Stats, records);
                        turnResults.PushTurnResult(
                            new BasicResult(TurnResult.TurnResultType.ActionSearchTriggerComplexTrap));
                    }
                    else
                    {
                        StreamingOutput.Instance.PushMessage(
                            U5StringRef.ThouDostFind(
                                GameReferences.DataOvlRef.StringReferences.GetString(DataOvlReference.ThingsIFindStrings
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

        /// <summary>
        /// </summary>
        /// <returns>true if a monster was created</returns>
        /// <remarks>see gameSpawnCreature in xu4 for similar method</remarks>
        private bool CreateRandomMonster(int nTurn)
        {
            const int maxTries = 10;
            const int nDistanceAway = 7;

            // find a position or give up
            int dX = nDistanceAway;
            int dY;
            for (int i = 0; i < maxTries; i++)
            {
                dY = Utils.Ran.Next() % nDistanceAway;

                // this logic borrowed from Xu4 to create some randomness
                if (Utils.OneInXOdds(2)) dX = -dX;
                if (Utils.OneInXOdds(2)) dY = -dY;
                if (Utils.OneInXOdds(2)) Utils.SwapInts(ref dX, ref dY);

                Point2D tilePosition = new((CurrentPosition.X + dX) % NumberOfColumnTiles,
                    (CurrentPosition.Y + dY) % NumberOfRowTiles);
                tilePosition.AdjustXAndYToMax(CurrentMap.NumOfXTiles);

                if (TheMapUnits.IsTileOccupied(tilePosition)) continue;

                // it's not occupied so we can create a monster
                EnemyReference enemyRef =
                    GameReferences.EnemyRefs.GetRandomEnemyReferenceByEraAndTile(nTurn, GetTileReference(tilePosition));
                if (enemyRef == null) continue;

                // add the new character to our list of characters currently on the map
                Enemy enemy = TheMapUnits.CreateEnemy(tilePosition, enemyRef, CurrentLargeMap.CurrentSingleMapReference,
                    out int _);

                return true;
            }

            return false;
        }

        /// <summary>
        ///     Create a list of the free spaces surrounding around the Avatar suitable for something to be generated onto
        ///     Uses all 8 directions
        /// </summary>
        /// <returns></returns>
        private List<Point2D> GetFreeSpacesSurroundingAvatar()
        {
            List<Point2D> freeSpacesAroundAvatar = new();

            for (int x = -1; x <= 1; x++)
            {
                for (int y = -1; y <= 1; y++)
                {
                    Point2D pointToCheck = new(Math.Max(CurrentPosition.X + x, 0),
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
            List<Point2D> laddersAndStairs = new();

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
        private List<MapUnitMovement.MovementCommandDirection> GetPossibleDirectionsList(in Point2D characterPosition,
            in Point2D scheduledPosition, int nMaxDistance, bool bNoStaircases)
        {
            List<MapUnitMovement.MovementCommandDirection> directionList = new();

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
        private SortedDictionary<double, Point2D> GetShortestPaths(List<Point2D> positionList,
            in Point2D destinedPosition)
        {
            SortedDictionary<double, Point2D> sortedPoints = new();

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


        private bool IsInsideBounds(in Point2D xy)
        {
            Map currentMap = CurrentMap;
            return !(xy.X >= currentMap.VisibleOnMap.Length || xy.X < 0 ||
                     xy.Y >= currentMap.VisibleOnMap[xy.X].Length || xy.Y < 0);
        }

        private bool IsMapUnitOccupiedFromList(in Point2D xy, IEnumerable<MapUnit> mapUnits)
        {
            //IEnumerable<MapUnit> mapUnits = TheMapUnits.GetMapUnitCollection(LargeMapOverUnder).AllActiveMapUnits;
            int nFloor = _currentSingleMapReference.Floor;

            foreach (MapUnit mapUnit in mapUnits)
            {
                // sometimes characters are null because they don't exist - and that is OK
                if (mapUnit.MapUnitPosition.IsSameAs(xy.X, xy.Y, nFloor))
                {
                    // check to see if the particular SPECIAL map unit is in your inventory, if so, then we exclude it
                    // for example it looks for crown, sceptre and amulet
                    if (GameStateReference.State.PlayerInventory.DoIHaveSpecialTileReferenceIndex(mapUnit
                            .KeyTileReference
                            .Index))
                        return false;
                    return true;
                }
            }

            return false;
        }

        private bool IsTileFreeToTravelLocal(in Point2D.Direction direction, Avatar.AvatarState avatarState)
        {
            return IsTileFreeToTravel(CurrentPosition.XY.GetAdjustedPosition(direction), true, avatarState);
        }

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
                new()
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

        public bool AreAnyTilesWithinFourDirections(Point2D position, IEnumerable<TileReference> tileReferences) =>
            tileReferences.Any(tileReference => IsTileWithinFourDirections(position, tileReference));

        public int ClosestTileReferenceAround(int nRadius, Func<int, bool> checkTile) =>
            ClosestTileReferenceAround(CurrentPosition.XY, nRadius, checkTile);

        public int ClosestTileReferenceAround(Point2D midPosition, int nRadius, Func<int, bool> checkTile)
        {
            double nShortestRadius = 255;
            Map currentMap = CurrentMap;
            bool bIsRepeatingMap = CurrentMap.IsRepeatingMap;
            TheMapUnits.CurrentMapUnits.RefreshActiveDictionaryCache();
            // an optimization to speed up checking of map units
            Dictionary<Point2D, List<MapUnit>> cachedActive = TheMapUnits.CurrentMapUnits.CachedActiveDictionary;

            for (int nRow = midPosition.X - nRadius; nRow < midPosition.X + nRadius; nRow++)
            {
                for (int nCol = midPosition.Y - nRadius; nCol < midPosition.Y + nRadius; nCol++)
                {
                    Point2D adjustedPos;
                    if (bIsRepeatingMap)
                    {
                        adjustedPos = new Point2D(Point2D.AdjustToMax(nRow, currentMap.NumOfXTiles),
                            Point2D.AdjustToMax(nCol, currentMap.NumOfYTiles));
                    }
                    else
                    {
                        if (nRow < 0 || nRow >= currentMap.NumOfXTiles || nCol < 0 || nCol >= currentMap.NumOfYTiles)
                            continue;

                        adjustedPos = new Point2D(nRow, nCol);
                    }

                    int nTileIndex = GetTileReference(adjustedPos.X, adjustedPos.Y).Index;
                    bool bHasMapUnits = cachedActive.ContainsKey(adjustedPos);
                    MapUnit mapUnit = bHasMapUnits ? GetTopVisibleMapUnit(adjustedPos, true) : null;

                    //if (mapUnit != null) _ = "";
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

        public int ClosestTileReferenceAround(TileReference tileReference, Point2D midPosition, int nRadius) =>
            ClosestTileReferenceAround(midPosition, nRadius, i => tileReference.Index == i);

        public int ClosestTileReferenceAround(TileReference tileReference, int nRadius) =>
            ClosestTileReferenceAround(CurrentPosition.XY, nRadius, i => tileReference.Index == i);

        public bool ContainsSearchableThings(in Point2D xy)
        {
            // moonstone check
            List<MapUnit> mapUnits = GetMapUnitsOnTile(xy);

            bool bIsSearchableMapUnit = mapUnits.Any(m => m is Chest or DeadBody or BloodSpatter);

            return IsLargeMap && GameStateReference.State.TheMoongates.IsMoonstoneBuried(xy, LargeMapOverUnder) ||
                   bIsSearchableMapUnit;
        }

        /// <summary>
        ///     Creates a horse MapUnit in the surrounding tiles of the Avatar - if one exists
        /// </summary>
        /// <returns>the new horse or null if there was no where to put it</returns>
        public Horse CreateHorseAroundAvatar()
        {
            List<Point2D> freeSpacesAroundAvatar = GetFreeSpacesSurroundingAvatar();
            if (freeSpacesAroundAvatar.Count <= 0) return null;

            Random ran = new();
            Point2D chosenLocation = freeSpacesAroundAvatar[ran.Next() % freeSpacesAroundAvatar.Count];
            Horse horse = TheMapUnits.CreateHorse(
                new MapUnitPosition(chosenLocation.X, chosenLocation.Y, CurrentPosition.Floor), LargeMapOverUnder,
                out int nIndex);

            if (nIndex == -1 || horse == null) return null;

            return horse;
        }

        public InventoryItem DequeuExposedSearchItems(in Point2D xy)
        {
            return TheMapOverrides.DequeueSearchItem(xy);
        }

        /// <summary>
        ///     Gets the appropriate (if any) SingleCombatMapReference based on the map and mapunits attempting to engage in
        ///     combat
        /// </summary>
        /// <param name="attackFromPosition">where are they attacking from</param>
        /// <param name="attackToPosition">where are they attack to</param>
        /// <param name="territory"></param>
        /// <param name="aggressorMapUnit">who is the one attacking?</param>
        /// <returns></returns>
        public AggressiveMapUnitInfo GetAggressiveMapUnitInfo(Point2D attackFromPosition,
            Point2D attackToPosition, SingleCombatMapReference.Territory territory, MapUnit aggressorMapUnit)
        {
            SingleCombatMapReference getSingleCombatMapReference(SingleCombatMapReference.BritanniaCombatMaps map)
                => GameReferences.CombatMapRefs.GetSingleCombatMapReference(territory, (int)map);

            TileReference attackToTileReference = GetTileReference(attackToPosition);
            TileReference attackFromTileReference = GetTileReference(attackFromPosition);

            List<MapUnit> mapUnits = TheMapUnits.GetMapUnitsByPosition(LargeMapOverUnder, attackToPosition,
                CurrentSingleMapReference.Floor);

            MapUnit targettedMapUnit = null;
            TileReference targettedMapUniTileReference = null;

            switch (mapUnits.Count)
            {
                case 0:
                    break;
                //return null;
                case > 1:
                    // the only excuse you can have for having more than one is if the avatar is on top of a known map unit
                    if (mapUnits.Any(m => m is Avatar))
                    {
                        targettedMapUnit = mapUnits.OfType<Avatar>().First();
                        targettedMapUniTileReference = targettedMapUnit.KeyTileReference;
                    }
                    else
                    {
                        throw new Ultima5ReduxException($"Did not expect {mapUnits.Count} mapunits on targeted tile");
                    }

                    break;
                default:
                    // a little lazy for now
                    targettedMapUnit = mapUnits[0];
                    targettedMapUniTileReference = targettedMapUnit.KeyTileReference;
                    break;
            }

            AggressiveMapUnitInfo mapUnitInfo = new(aggressorMapUnit);

            // if they are not Enemy type (probably NPC) then we are certain they don't have a range attack
            if (aggressorMapUnit is not Enemy enemy)
            {
                return mapUnitInfo;
            }

            bool bIsPirate = enemy.EnemyReference.LargeMapMissileType == CombatItemReference.MissileType.CannonBall;

            // if avatar is being attacked..
            // we get to assume that the avatar is not necessarily next to the enemy
            bool bNextToEachOther = attackFromPosition.IsWithinNFourDirections(attackToPosition);

            if (!bNextToEachOther)
            {
                switch (enemy.EnemyReference.LargeMapMissileType)
                {
                    case CombatItemReference.MissileType.None:
                        return mapUnitInfo;
                    // pirates = cannonball, snakes = poison, serpents = red, squid =
                    // the aggressor is an enemy so let's check to see if they have LargeMap projectiles
                    case CombatItemReference.MissileType.CannonBall:
                        // if it's a cannon ball, and they are on the same X or Y then it can fire!
                        if ((attackFromPosition.X == attackToPosition.X ||
                             attackFromPosition.Y == attackToPosition.Y) &&
                            attackFromPosition.IsWithinN(attackToPosition, 3))
                        {
                            mapUnitInfo.AttackingMissileType = CombatItemReference.MissileType.CannonBall;
                        }

                        break;
                    default:
                        // it's not a cannon ball but it is a missile
                        if (attackFromPosition.IsWithinN(attackToPosition, 3))
                            mapUnitInfo.AttackingMissileType = enemy.EnemyReference.LargeMapMissileType;
                        break;
                }

                return mapUnitInfo;
            }

            // if avatar on skiff or carpet and avatar is on water then it's immediate ouch, no map
            if ((IsAvatarInSkiff || IsAvatarRidingCarpet) &&
                attackToTileReference.CombatMapIndex is SingleCombatMapReference.BritanniaCombatMaps.BoatCalc)
            {
                // we will use Arrow to denote the enemy attacking on the overworld, but no combat map
                mapUnitInfo.AttackingMissileType = CombatItemReference.MissileType.Arrow;
            }
            // avatar not on a boat
            // return avatar's current tile combat map
            else if (!IsAvatarInFrigate)
            {
                // when the avatar is not in boat and a water enemy attacks - they will always fight in the bay
                if (enemy.EnemyReference.IsWaterEnemy)
                {
                    mapUnitInfo.CombatMapReference = getSingleCombatMapReference(bIsPirate
                        ? SingleCombatMapReference.BritanniaCombatMaps.BoatNorth
                        : SingleCombatMapReference.BritanniaCombatMaps.Bay);
                }
                else
                {
                    // if you end up on a bay tile, but the monster is not a water monster, then we need to either
                    // substitute another map, or have them attack them in the overworld
                    if (attackToTileReference.CombatMapIndex is SingleCombatMapReference.BritanniaCombatMaps.Bay)
                    {
                        mapUnitInfo.CombatMapReference = null;
                        mapUnitInfo.AttackingMissileType = CombatItemReference.MissileType.Arrow;
                    }
                    else
                    {
                        mapUnitInfo.CombatMapReference =
                            getSingleCombatMapReference(attackToTileReference.CombatMapIndex);
                    }
                }
            }
            // if the enemy is a water enemy and we know the avatar is on a frigate, then we fight on the ocean
            else if (enemy.EnemyReference.IsWaterEnemy)
            {
                // we are on a frigate AND we are fighting a pirate ship
                mapUnitInfo.CombatMapReference = getSingleCombatMapReference(bIsPirate
                    ? SingleCombatMapReference.BritanniaCombatMaps.BoatBoat
                    : SingleCombatMapReference.BritanniaCombatMaps.BoatOcean);
            }
            else
            {
                if (!enemy.EnemyReference.IsWaterEnemy)
                {
                    mapUnitInfo.CombatMapReference =
                        getSingleCombatMapReference(SingleCombatMapReference.BritanniaCombatMaps.BoatSouth);
                }
                else
                {
                    mapUnitInfo.CombatMapReference = GameReferences.CombatMapRefs.GetSingleCombatMapReference(territory,
                        (int)attackToTileReference.CombatMapIndex);
                }
            }

            return mapUnitInfo;
        }

        public Dictionary<Point2D, bool> GetAllMapOccupiedTiles()
        {
            Dictionary<Point2D, bool> occupiedDictionary = new();

            IEnumerable<MapUnit> mapUnits = TheMapUnits.GetMapUnitCollection(LargeMapOverUnder).AllActiveMapUnits;
            int nFloor = CurrentPosition.Floor;

            foreach (MapUnit mapUnit in mapUnits)
            {
                if (mapUnit.MapUnitPosition.Floor != nFloor) continue;
                if (!occupiedDictionary.ContainsKey(mapUnit.MapUnitPosition.XY))
                    occupiedDictionary.Add(mapUnit.MapUnitPosition.XY, true);
            }

            return occupiedDictionary;
        }

        public Dictionary<int, Dictionary<int, bool>> GetAllMapOccupiedTilesFast()
        {
            Dictionary<int, Dictionary<int, bool>> occupiedTiles = new();

            IEnumerable<MapUnit> mapUnits = TheMapUnits.GetMapUnitCollection(LargeMapOverUnder).AllActiveMapUnits;
            int nFloor = CurrentPosition.Floor;

            foreach (MapUnit mapUnit in mapUnits)
            {
                if (mapUnit.MapUnitPosition.Floor != nFloor) continue;

                int x = mapUnit.MapUnitPosition.XY.X;
                int y = mapUnit.MapUnitPosition.XY.Y;
                if (!occupiedTiles.ContainsKey(x)) occupiedTiles.Add(x, new Dictionary<int, bool>());
                if (!occupiedTiles[x].ContainsKey(y)) occupiedTiles[x].Add(y, true);
            }

            return occupiedTiles;
        }


        public int GetAlternateFlatSprite(in Point2D xy)
        {
            if (CurrentMap.IsXYOverride(xy, TileOverrideReference.TileType.Flat))
            {
                return CurrentMap.GetTileOverride(xy).SpriteNum;
            }

            int nSprite = GetTileReference(xy).FlatTileSubstitutionIndex;

            return nSprite is -2 or -3 ? GuessTile(xy) : nSprite;
        }

        public int GetCalculatedSpriteIndexByTile(TileReference tileReference, in Point2D tilePosInMap,
            bool bIsAvatarTile, bool bIsMapUnitOccupiedTile, out bool bDrawCharacterOnTile)
        {
            int nSprite = tileReference.Index;
            bool bIsMirror = GameReferences.SpriteTileReferences.IsUnbrokenMirror(nSprite);
            bDrawCharacterOnTile = false;

            if (bIsMirror)
            {
                // if the avatar is south of the mirror then show his image
                Point2D expectedAvatarPos = new(tilePosInMap.X, tilePosInMap.Y + 1);
                if (expectedAvatarPos == CurrentPosition.XY)
                {
                    return GameReferences.SpriteTileReferences.GetTileNumberByName("MirrorAvatar");
                }
            }

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

            int nNewSpriteIndex;

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

            // quick hack to reassign non animated avatar to animated version
            //if (bDrawCharacterOnTile && nNewSpriteIndex == 284) nNewSpriteIndex = 332;

            return nNewSpriteIndex;
        }

        public Point2D GetCameraCenter()
        {
            if (IsCombatMap)
            {
                return new Point2D(CurrentMap.NumOfXTiles / 2, CurrentMap.NumOfYTiles / 2);
            }

            return CurrentPosition.XY;
        }

        public SingleCombatMapReference GetCombatMapReferenceForAvatarAttacking(Point2D attackFromPosition,
            Point2D attackToPosition, SingleCombatMapReference.Territory territory)
        {
            SingleCombatMapReference getSingleCombatMapReference(SingleCombatMapReference.BritanniaCombatMaps map)
                => GameReferences.CombatMapRefs.GetSingleCombatMapReference(territory, (int)map);

            // note - attacking from a skiff OR carpet is NOT permitted unless touching a piece of land 
            // otherwise is twill say Attack-On foot!
            // note - cannot exit a skiff unless land is nearby

            // let's use this method to also determine if an enemy CAN attack the avatar from afar

            TileReference attackToTileReference = GetTileReference(attackToPosition);
            if (attackToTileReference.CombatMapIndex == SingleCombatMapReference.BritanniaCombatMaps.None) return null;

            TileReference attackFromTileReference = GetTileReference(attackFromPosition);

            List<MapUnit> mapUnits = TheMapUnits.GetMapUnitsByPosition(LargeMapOverUnder, attackToPosition,
                CurrentSingleMapReference.Floor);

            MapUnit targettedMapUnit = null;
            TileReference targettedMapUniTileReference = null;

            switch (mapUnits.Count)
            {
                case 0:
                    // the avatar is attacking, but actually doesn't have anyone directly in their sights
                    // nothing to do
                    return null;
                case >= 1:
                    // the only excuse you can have for having more than one is if the avatar is on top of a known map unit
                    if (mapUnits.Any(m => m is Avatar))
                    {
                        throw new Ultima5ReduxException(
                            "Did not expect Avatar mapunit on targeted tile when Avatar is attacking");
                    }

                    // a little lazy for now
                    targettedMapUnit = mapUnits[0];
                    targettedMapUniTileReference = targettedMapUnit.KeyTileReference;
                    break;
            }

            // if the avatar is in a skiff of on a carpet, but is in the ocean then they aren't allowed to attack
            if ((IsAvatarInSkiff || IsAvatarRidingCarpet))
            {
                bool bAvatarOnWaterTile = attackFromTileReference.IsWaterTile;

                if (bAvatarOnWaterTile)
                {
                    if (attackToTileReference.CombatMapIndex is SingleCombatMapReference.BritanniaCombatMaps.BoatCalc)
                    {
                        // if no surrounding tiles are water tile then we skip the attack
                        return null;
                    }
                }
            }

            // there is someone to target
            if (!IsAvatarInFrigate)
            {
                // last second check for water enemy - they can occasionally appear on a "land" tile like bridges
                // so we take the chance to force a Bay map just in case
                if (targettedMapUnit is Enemy waterCheckEnemy)
                {
                    if (waterCheckEnemy.EnemyReference.IsWaterEnemy)
                        return getSingleCombatMapReference(SingleCombatMapReference.BritanniaCombatMaps.Bay);
                    // if the enemy is on bay but is not a water creature then we cannot attack them
                    if (attackToTileReference.CombatMapIndex == SingleCombatMapReference.BritanniaCombatMaps.Bay)
                    {
                        return null;
                    }
                }

                return getSingleCombatMapReference(attackToTileReference.CombatMapIndex);
            }

            // BoatCalc indicates it is a water tile and requires special consideration
            if (attackToTileReference.CombatMapIndex != SingleCombatMapReference.BritanniaCombatMaps.BoatCalc)
            {
                if (attackToTileReference.IsWaterEnemyPassable)
                    return getSingleCombatMapReference(SingleCombatMapReference.BritanniaCombatMaps.BoatOcean);

                // BoatSouth indicates the avatar is on the frigate, and the enemy on land
                return getSingleCombatMapReference(SingleCombatMapReference.BritanniaCombatMaps.BoatSouth);
            }

            // if attacking another frigate, then it's boat to boat
            if (GameReferences.SpriteTileReferences.IsFrigate(targettedMapUniTileReference.Index))
            {
                return getSingleCombatMapReference(SingleCombatMapReference.BritanniaCombatMaps.BoatBoat);
            }

            // otherwise it's boat (ours) to ocean
            return getSingleCombatMapReference(SingleCombatMapReference.BritanniaCombatMaps.BoatOcean);
            // it is not boat calc, but there is an enemy, so refer to our default combat map
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

        public IEnumerable<InventoryItem> GetExposedSearchItems(in Point2D xy)
        {
            return TheMapOverrides.GetExposedSearchItems(xy);
        }

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
        public List<MapUnit> GetMapUnitsOnTile(in Point2D xy)
        {
            if (CurrentSingleMapReference == null)
                throw new Ultima5ReduxException("No single map is set in virtual map");

            List<MapUnit> mapUnits =
                TheMapUnits.GetMapUnitsByPosition(LargeMapOverUnder, xy, CurrentSingleMapReference.Floor);

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
        public Point2D.Direction GetStairsDirection(in Point2D xy)
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
        public int GetStairsSprite(in Point2D xy)
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
                    throw new ArgumentOutOfRangeException(nameof(xy), @"Point2D is out of range for stairs check");
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
            return GetTileReference(new Point2D(x, y));
        }

        /// <summary>
        ///     Gets a tile reference from the given coordinate
        /// </summary>
        /// <param name="xy"></param>
        /// <returns></returns>
        public TileReference GetTileReference(in Point2D xy, bool bIgnoreExposed = false, bool bIgnoreMoongate = false)
        {
            // we FIRST check if there is an exposed item to show - this takes precedence over an overriden tile
            if (!bIgnoreExposed && TheMapOverrides.HasExposedSearchItems(xy.X, xy.Y))
            {
                Queue<InventoryItem> searchItems = TheMapOverrides.GetExposedSearchItems(xy.X, xy.Y);
                if (searchItems.Count > 0)
                    // we get the top most exposed item and only show it
                    return GameReferences.SpriteTileReferences.GetTileReference(searchItems.Peek().SpriteNum);
            }

            // if it's a large map and there should be a moongate and it's nighttime then it's a moongate!
            // bajh: March 22, 2020 - we are going to try to always include the Moongate, and let the game decide what it wants to do with it
            if (!bIgnoreMoongate && IsLargeMap &&
                GameStateReference.State.TheMoongates.IsMoonstoneBuried(new Point3D(xy.X, xy.Y,
                    LargeMapOverUnder == Map.Maps.Overworld ? 0 : 0xFF)))
            {
                return GameReferences.SpriteTileReferences.GetTileReferenceByName("Moongate") ??
                       throw new Ultima5ReduxException("Supposed to get a moongate override: " + xy);
            }

            // we check to see if our override map has something on top of it
            if (TheMapOverrides.HasOverrideTile(xy))
            {
                return TheMapOverrides.GetOverrideTileReference(xy.X, xy.Y) ??
                       throw new Ultima5ReduxException("Expected tile override at " + xy);
            }

            // the GetTileReference accounts for any forced overrides across the entire world
            return CurrentMap.GetTileReference(xy);
        }

        /// <summary>
        ///     Gets a tile reference from the tile the avatar currently resides on
        /// </summary>
        /// <returns></returns>
        public TileReference GetTileReferenceOnCurrentTile()
        {
            return GetTileReference(CurrentPosition.XY);
        }

        public TileStack GetTileStack(Point2D xy, bool bSkipMapUnit)
        {
            TileStack tileStack = new TileStack(xy);

            // this checks to see if you are on the outer bounds of a small map, and if the flood fill touched it
            // if it has touched it then we draw the outer tiles
            if (CurrentMap is SmallMap smallMap && !smallMap.IsInBounds(xy) && smallMap.TouchedOuterBorder)
            {
                TileReference outerTileReference = GameReferences.SpriteTileReferences.GetTileReference(
                    smallMap.GetOutOfBoundsSprite(xy));
                tileStack.PushTileReference(outerTileReference);
                return tileStack;
            }

            // if the position of the tile is no longer inside the bounds of the visibility
            // or has become invisible, then destroy the voxels and return right away
            bool bOutsideOfVisibilityArray = !IsInsideBounds(xy);
            if (bOutsideOfVisibilityArray || !CurrentMap.VisibleOnMap[xy.X][xy.Y])
            {
                return tileStack;
            }

            // get the reference as per the original game data
            TileReference origTileReference = GetTileReference(xy);
            // get ALL active map units on the tile
            // IEnumerable mapUnits =
            //     TheMapUnits.CurrentMapUnits.AllMapUnits.Where(m => m.MapUnitPosition.XY == xy);
            MapUnit topMostMapUnit = bSkipMapUnit ? null : GetTopVisibleMapUnit(xy, false);

            bool bIsAvatarTile = !IsCombatMap && xy == CurrentPosition?.XY;
            bool bIsMapUnitOccupiedTile = topMostMapUnit != null;

            // if there is an alternate flat sprite (for example, trees have grass)
            if (origTileReference.HasAlternateFlatSprite ||
                CurrentMap.IsXYOverride(xy, TileOverrideReference.TileType.Flat))
            {
                TileReference flatTileReference =
                    GameReferences.SpriteTileReferences.GetTileReference(GetAlternateFlatSprite(xy));
                tileStack.PushTileReference(flatTileReference);
            }

            // we always push the original tile reference 
            int nCalculatedIndex = GetCalculatedSpriteIndexByTile(origTileReference, xy, bIsAvatarTile,
                bIsMapUnitOccupiedTile, out bool bDrawCharacterOnTile);
            TileReference calculatedTileReference =
                GameReferences.SpriteTileReferences.GetAnimatedTileReference(nCalculatedIndex);

            // there are times we will not draw a calculated reference - such as when an NPC is on a door 
            // which indicates it is open, and therefor hidden
            bool bSkipCalculatedTileReference =
                (GameReferences.SpriteTileReferences.IsDoor(calculatedTileReference.Index) && bIsMapUnitOccupiedTile)
                || calculatedTileReference.DontDraw;

            if (!bSkipCalculatedTileReference) tileStack.PushTileReference(calculatedTileReference);

            // next we have a more complicated operation. We need to figure out which mapunits are on the map
            // and which ones we should show. There are often cases where we need to combine them such
            // as walking on top of a horse

            // if the GetCalculatedSpriteIndexByTile routine has told us not to draw the map unit,
            // then we skip it all together
            if (!bDrawCharacterOnTile) return tileStack;
            // if there are no map units, then we skip this bit all together
            if (topMostMapUnit == null) return tileStack;

            // we need to determine WHICH MapUnit to draw at this point
            // it could be the original, or boarded or even a different one all together?
            TileReference mapUnitTileReference = topMostMapUnit.GetAnimatedTileReference();
            Avatar avatarMapUnit = null;
            if (bIsAvatarTile)
            {
                avatarMapUnit = TheMapUnits.GetAvatarMapUnit();
            }

            switch (topMostMapUnit)
            {
                case Horse when bIsAvatarTile: // if we are on a horse, let's show the mounted tile
                case Avatar { IsAvatarOnBoardedThing: true }: // we always show the Avatar on the thing he is boarded on
                    tileStack.PushTileReference(topMostMapUnit.GetBoardedTileReference(), true);
                    break;
                case MagicCarpet when bIsAvatarTile:
                {
                    tileStack.PushTileReference(mapUnitTileReference, true);
                    if (avatarMapUnit == null)
                        throw new Ultima5ReduxException(
                            "Avatar map unit was null, when expected on top of magic carpet");
                    tileStack.PushTileReference(avatarMapUnit.GetAnimatedTileReference(), true);
                    break;
                }
                default:
                    tileStack.PushTileReference(mapUnitTileReference, true);
                    break;
            }

            return tileStack;
        }

        /// <summary>
        ///     Gets the top visible map unit - excluding the Avatar
        /// </summary>
        /// <param name="xy"></param>
        /// <param name="bExcludeAvatar"></param>
        /// <returns>MapUnit or null</returns>
        // ReSharper disable once MemberCanBePrivate.Global
        public MapUnit GetTopVisibleMapUnit(in Point2D xy, bool bExcludeAvatar)
        {
            List<MapUnit> mapUnits = GetMapUnitsOnTile(xy);

            // this is inefficient, but the lists are so small it is unlikely to matter
            foreach (Type type in _visibilePriorityOrder)
            {
                if (bExcludeAvatar && type == typeof(Avatar)) continue;
                foreach (MapUnit mapUnit in mapUnits)
                {
                    if (!mapUnit.IsActive) continue;
                    // if it's a combat unit but they dead or gone then we skip
                    if (mapUnit is CombatMapUnit { HasEscaped: true } combatMapUnit)
                        //|| (combatMapUnit.Stats.CurrentHp <= 0 && !bIncludeDeadPlayers)))
                    {
                        if (combatMapUnit is not NonAttackingUnit) continue;
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
        public int GuessTile(in Point2D xy)
        {
            Dictionary<int, int> tileCountDictionary = new();

            // we check our high level tile override
            // this method is much quicker because we only load the data once in the maps 
            if (!IsLargeMap && CurrentMap.IsXYOverride(xy, TileOverrideReference.TileType.Flat))
                return CurrentMap.GetTileOverride(xy).SpriteNum;
            if (IsLargeMap && CurrentMap.IsXYOverride(xy, TileOverrideReference.TileType.Flat))
                return CurrentMap.GetTileOverride(xy).SpriteNum;

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

                if (nMostTile != -1 && nTotal <= nMostTileTotal) continue;

                nMostTile = nTile;
                nMostTileTotal = nTotal;
            }

            // just in case we didn't find a match - just use grass for now
            return nMostTile == -1 ? 5 : nMostTile;
        }

        public bool HasAnyExposedSearchItems(Point2D xy) => TheMapOverrides.HasExposedSearchItems(xy);

        public bool IsAvatarSitting()
        {
            return GameReferences.SpriteTileReferences.IsChair(GetTileReferenceOnCurrentTile().Index);
        }

        public bool IsCombatMapUnitOccupiedTile(in Point2D xy)
        {
            return IsMapUnitOccupiedFromList(xy, TheMapUnits.GetMapUnitCollection(LargeMapOverUnder).AllCombatMapUnits);
        }

        /// <summary>
        ///     Is there food on a table within 1 (4 way) tile
        ///     Used for determining if eating animation should be used
        /// </summary>
        /// <param name="characterPos"></param>
        /// <returns>true if food is within a tile</returns>
        public bool IsFoodNearby(in Point2D characterPos)
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
        public bool IsHorizDoor(in Point2D xy)
        {
            if (xy.X - 1 < 0 || xy.X + 1 >= NumberOfColumnTiles) return false;
            if (xy.Y - 1 < 0 || xy.Y + 1 >= NumberOfRowTiles) return true;

            return (GetTileReference(xy.X - 1, xy.Y).IsSolidSpriteButNotDoorAndNotNPC) ||
                   (GetTileReference(xy.X + 1, xy.Y).IsSolidSpriteButNotDoorAndNotNPC);
        }

        public bool IsHorizTombstone(in Point2D xy)
        {
            if (xy.X - 1 < 0 || xy.X + 1 >= NumberOfColumnTiles) return false;
            if (xy.Y - 1 < 0 || xy.Y + 1 >= NumberOfRowTiles) return true;

            bool bHasHorizBlock = (GetTileReference(xy.X - 1, xy.Y).IsSolidSpriteButNotDoorAndNotNPC) ||
                                  (GetTileReference(xy.X + 1, xy.Y).IsSolidSpriteButNotDoorAndNotNPC);
            if (bHasHorizBlock) return bHasHorizBlock;
            bool bHasVertBlock = (GetTileReference(xy.X, xy.Y - 1).IsSolidSpriteButNotDoorAndNotNPC) ||
                                 (GetTileReference(xy.X, xy.Y + 1).IsSolidSpriteButNotDoorAndNotNPC);
            return !bHasVertBlock;
        }


        public bool IsLandNearby() =>
            IsLandNearby(CurrentPosition.XY, false, TheMapUnits.GetAvatarMapUnit().CurrentAvatarState);

        public bool IsLandNearby(in Point2D xy, bool bNoStairCases, Avatar.AvatarState avatarState)
        {
            return IsTileFreeToTravel(xy.GetAdjustedPosition(Point2D.Direction.Down), bNoStairCases, avatarState) ||
                   IsTileFreeToTravel(xy.GetAdjustedPosition(Point2D.Direction.Up), bNoStairCases, avatarState) ||
                   IsTileFreeToTravel(xy.GetAdjustedPosition(Point2D.Direction.Left), bNoStairCases, avatarState) ||
                   IsTileFreeToTravel(xy.GetAdjustedPosition(Point2D.Direction.Right), bNoStairCases, avatarState);
        }

        public bool IsLandNearby(in Avatar.AvatarState avatarState)
        {
            return IsTileFreeToTravelLocal(Point2D.Direction.Down, avatarState) ||
                   IsTileFreeToTravelLocal(Point2D.Direction.Up, avatarState) ||
                   IsTileFreeToTravelLocal(Point2D.Direction.Left, avatarState) ||
                   IsTileFreeToTravelLocal(Point2D.Direction.Right, avatarState);
        }

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
        public bool IsMapUnitOccupiedTile(in Point2D xy)
        {
            return IsMapUnitOccupiedFromList(xy, TheMapUnits.GetMapUnitCollection(LargeMapOverUnder).AllActiveMapUnits);
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
        public bool IsStairGoingDown(in Point2D xy)
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
        public bool IsStairGoingUp(in Point2D xy)
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

        public bool IsTileWithinFourDirections(Point2D position, TileReference tileReference)
        {
            List<Point2D> positions;
            if (IsLargeMap)
                positions = position.GetConstrainedFourDirectionSurroundingPointsWrapAround(
                    LargeMapLocationReferences.XTiles,
                    LargeMapLocationReferences.YTiles);
            else
            {
                positions = position.GetConstrainedFourDirectionSurroundingPoints(CurrentSmallMap.NumOfXTiles,
                    CurrentSmallMap.NumOfYTiles);
            }

            return positions.Any(testTosition => GetTileReference(testTosition).Index == tileReference.Index);
        }

        public void LoadCombatMap(SingleCombatMapReference singleCombatMapReference,
            SingleCombatMapReference.EntryDirection entryDirection, PlayerCharacterRecords records,
            EnemyReference primaryEnemyReference = null, int nPrimaryEnemies = 0,
            EnemyReference secondaryEnemyReference = null, int nSecondaryEnemies = 0)
        {
            LoadCombatMap(singleCombatMapReference, entryDirection, records, primaryEnemyReference, nPrimaryEnemies,
                secondaryEnemyReference, nSecondaryEnemies, null);
        }

        /// <summary>
        ///     Specifically for loading combat maps when fighting NPCs instead of the typical Enemy
        /// </summary>
        /// <param name="singleCombatMapReference"></param>
        /// <param name="entryDirection"></param>
        /// <param name="records"></param>
        /// <param name="primaryEnemyReference"></param>
        /// <param name="npcRef"></param>
        public void LoadCombatMap(SingleCombatMapReference singleCombatMapReference,
            SingleCombatMapReference.EntryDirection entryDirection, PlayerCharacterRecords records,
            EnemyReference primaryEnemyReference, NonPlayerCharacterReference npcRef)
        {
            LoadCombatMap(singleCombatMapReference, entryDirection, records, primaryEnemyReference, 1, null, 0, npcRef);
        }

        public void LoadCombatMapWithCalculation(SingleCombatMapReference singleCombatMapReference,
            PlayerCharacterRecords records, MapUnit mapUnit)
        {
            switch (mapUnit)
            {
                case Enemy enemy:
                    LoadCombatMapWithCalculation(singleCombatMapReference,
                        SingleCombatMapReference.EntryDirection.South, records, enemy.EnemyReference);
                    return;
                case NonPlayerCharacter npc:
                    LoadCombatMap(singleCombatMapReference, SingleCombatMapReference.EntryDirection.South, records,
                        null, npc.NPCRef);
                    break;
            }

            throw new Ultima5ReduxException("You can only calculate combat map loading with Enemies and NPCs");
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
            //CurrentLargeMap = _largeMaps[map];
            LargeMapOverUnder = map;
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
            // bajh: maybe we store each map override indefinitely so we never lose anything 
            TheMapOverrides = new MapOverrides(CurrentLargeMap);

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
            TheMapUnits.GetAvatarMapUnit().MapUnitPosition.Floor = singleMapReference.Floor;

            if (xy != null) CurrentPosition.XY = xy;
        }

        public void MoveAvatar(Point2D newPosition)
        {
            TheMapUnits.CurrentAvatarPosition =
                new MapUnitPosition(newPosition.X, newPosition.Y, TheMapUnits.CurrentAvatarPosition.Floor);
        }

        public bool ProcessSearchInnerItems(TurnResults turnResults, NonAttackingUnit nonAttackingUnit, bool bSearched,
            bool bOpened)
        {
            bool bHasInnerItems = nonAttackingUnit.HasInnerItemStack;
            ItemStack itemStack = nonAttackingUnit.InnerItemStack;
            if (bHasInnerItems)
            {
                StreamingOutput.Instance.PushMessage(nonAttackingUnit.InnerItemStack.ThouFindStr);
            }
            else
            {
                StreamingOutput.Instance.PushMessage(
                    GameReferences.DataOvlRef.StringReferences.GetString(DataOvlReference.ThingsIFindStrings
                        .NOTHING_OF_NOTE_BANG_N));
            }

            // delete the deadbody and add stuff
            //TheMapUnits.ClearAndSetEmptyMapUnits(nonAttackingUnit);
            nonAttackingUnit.HasBeenSearched = bSearched;
            nonAttackingUnit.HasBeenOpened = bOpened;
            turnResults.PushTurnResult(new BasicResult(TurnResult.TurnResultType.ActionSearchThingDisappears));

            if (bHasInnerItems)
            {
                // there were items inside the thing, so we place them 
                TheMapUnits.PlaceNonAttackingUnit(itemStack, nonAttackingUnit.MapUnitPosition, LargeMapOverUnder);
            }

            return bHasInnerItems;
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
                    TheMapUnits.GetAvatarMapUnit().MapUnitPosition = PreMapUnitPosition;
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

        public class AggressiveMapUnitInfo
        {
            public enum DecidedAction
            {
                Unset = -1, MoveUnit = 0, RangedAttack, MeleeOverworldAttack, Stay, EnemyAttackCombatMap
            }

            private DecidedAction _decidedAction = DecidedAction.Unset;
            public MapUnit AttackingMapUnit { get; }
            public CombatItemReference.MissileType AttackingMissileType { get; internal set; }
            public SingleCombatMapReference CombatMapReference { get; internal set; }

            public AggressiveMapUnitInfo(MapUnit attackingMapUnit,
                CombatItemReference.MissileType attackingMissileType = CombatItemReference.MissileType.None,
                SingleCombatMapReference combatMapReference = null)
            {
                AttackingMapUnit = attackingMapUnit;
                AttackingMissileType = attackingMissileType;
                CombatMapReference = combatMapReference;
            }

            public DecidedAction GetDecidedAction()
            {
                if (_decidedAction != DecidedAction.Unset) return _decidedAction;
                // if they have a combat map - then they are next to them and could go into combat
                // if they have a missile type then they are within range and will attack with that
                // if they have a Arrow missile type, then they will attack them melee in the overworld
                if (CombatMapReference != null)
                {
                    _decidedAction = DecidedAction.EnemyAttackCombatMap;
                }
                else if (AttackingMissileType == CombatItemReference.MissileType.Arrow)
                {
                    _decidedAction = DecidedAction.MeleeOverworldAttack;
                }
                else if (AttackingMissileType != CombatItemReference.MissileType.None)
                {
                    // we will not ALWAYS range attack, sometimes they will try to get closer to the avatar
                    _decidedAction = Utils.OneInXOdds(2) ? DecidedAction.RangedAttack : DecidedAction.MoveUnit;
                }
                else
                {
                    _decidedAction = DecidedAction.MoveUnit;
                }

                return _decidedAction;
            }
        }
    }
}