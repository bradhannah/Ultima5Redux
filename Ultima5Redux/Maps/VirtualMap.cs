using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.Serialization;
using Newtonsoft.Json;
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
using Ultima5Redux.References.PlayerCharacters.Inventory;

// ReSharper disable UnusedMember.Global

// ReSharper disable IdentifierTypo

namespace Ultima5Redux.Maps
{
    [DataContract] public partial class VirtualMap
    {
        internal enum LadderOrStairDirection { Up, Down }

        /// <summary>
        ///     Both underworld and overworld maps
        /// </summary>
        [DataMember(Name = "LargeMaps")] private readonly Dictionary<Map.Maps, LargeMap> _largeMaps = new(2)
        {
            { Map.Maps.Overworld, new LargeMap(LargeMapLocationReferences.LargeMapType.Overworld) },
            { Map.Maps.Underworld, new LargeMap(LargeMapLocationReferences.LargeMapType.Underworld) }
        };

        /// <summary>
        ///     All the small maps
        /// </summary>
        //[DataMember(Name = "SmallMaps")]
        private readonly SmallMaps _smallMaps = new();

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


        /// <summary>
        ///     The current small map (null if on large map)
        /// </summary>
        // ReSharper disable once MemberCanBePrivate.Global
        [DataMember]
        public SmallMap CurrentSmallMap { get; private set; }

        [DataMember] public DungeonMap CurrentDungeonMap { get; private set; }


        /// <summary>
        ///     If we are on a large map - then are we on overworld or underworld
        /// </summary>
        [DataMember]
        public Map.Maps TheCurrentMapType { get; private set; } = (Map.Maps)(-1);

        [DataMember] public int OneInXOddsOfNewMonster { get; set; } = 16;

        //[DataMember] public MapUnits.MapUnits CurrentMap { get; private set; }


        /// <summary>
        ///     Current large map (null if on small map)
        /// </summary>
        // ReSharper disable once MemberCanBePrivate.Global
        [IgnoreDataMember]
        public LargeMap CurrentLargeMap => _largeMaps[TheCurrentMapType];

        /// <summary>
        ///     The abstracted Map object for the current map
        ///     Returns large or small depending on what is active
        /// </summary>
        [IgnoreDataMember]
        public Map CurrentMap
        {
            get
            {
                //SmallMapReferences.SingleMapReference CurrentMap.CurrentSingleMapReference = CurrentMap.CurrentSingleMapReference;
                if (CurrentMap.CurrentSingleMapReference == null)
                    throw new Ultima5ReduxException("Tried to get CurrentMap but it was false");

                if (CurrentMap.CurrentSingleMapReference.IsDungeon) return CurrentDungeonMap;

                switch (CurrentMap.CurrentSingleMapReference.MapLocation)
                {
                    case SmallMapReferences.SingleMapReference.Location.Combat_resting_shrine:
                        if (CurrentMap is CombatMap) return CurrentMap;
                        throw new Ultima5ReduxException("Resting and Shrines have not been implemented yet");
                    case SmallMapReferences.SingleMapReference.Location.Britannia_Underworld:
                        return CurrentMap.CurrentSingleMapReference.Floor == 0 ? OverworldMap : UnderworldMap;
                    default:
                        return CurrentSmallMap;
                }
            }
        }

        [IgnoreDataMember]
        public bool IsAvatarInFrigate =>
            CurrentMap is RegularMap regularMap &&
            regularMap.GetAvatarMapUnit()?.CurrentBoardedMapUnit is Frigate;

        [IgnoreDataMember]
        public bool IsAvatarInSkiff =>
            CurrentMap is RegularMap regularMap &&
            regularMap.GetAvatarMapUnit().CurrentBoardedMapUnit is Skiff;

        [IgnoreDataMember]
        public bool IsAvatarRidingCarpet =>
            CurrentMap is RegularMap regularMap &&
            regularMap.GetAvatarMapUnit().CurrentBoardedMapUnit is MagicCarpet;

        [IgnoreDataMember]
        public bool IsAvatarRidingHorse =>
            CurrentMap is RegularMap regularMap &&
            regularMap.GetAvatarMapUnit().CurrentBoardedMapUnit is Horse;

        [IgnoreDataMember]
        public bool IsAvatarRidingSomething => CurrentMap is RegularMap regularMap &&
                                               regularMap.GetAvatarMapUnit().IsAvatarOnBoardedThing;

        [IgnoreDataMember]
        public bool IsBasement
        {
            get
            {
                if (CurrentMap.CurrentSingleMapReference == null) return false;

                return CurrentMap is not LargeMap && CurrentMap.CurrentSingleMapReference.Floor == -1;
            }
        }

        // [IgnoreDataMember] public bool IsCombatMap => CurrentMap is CombatMap;
        // [IgnoreDataMember] public bool IsDungeonMap => CurrentMap is DungeonMap;

        // [IgnoreDataMember]
        // public bool IsLargeMap => TheCurrentMapType is Map.Maps.Overworld or Map.Maps.Underworld;

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

        //[IgnoreDataMember] public CombatMap CurrentCombatMap { get; private set; }

        [IgnoreDataMember] public MapUnitPosition CurrentPosition => CurrentMap.CurrentPosition;

        public IEnumerable<MapUnit> AllVisibleActiveMapUnits
        {
            get
            {
                IEnumerable<Point2D> allPoints =
                    CurrentMap.CurrentMapUnits.AllActiveMapUnits.Select(e => e.MapUnitPosition.XY);
                // filter out NULL mapunits - this is when a MapUnit is Active but not visible, like DiscoverableLoot
                IEnumerable<MapUnit> visibleMapUnits =
                    allPoints.Select(point => CurrentMap.GetTopVisibleMapUnit(point, false)).Where(p => p != null);
                return visibleMapUnits;
            }
        }

        public MapUnitPosition PreviousPosition { get; } = new();

        [DataMember] public SearchItems TheSearchItems { get; private set; }

        /// <summary>
        ///     Creates a new frigate at a dock of a given location
        /// </summary>
        /// <param name="location"></param>
        // ReSharper disable once UnusedMethodReturnValue.Global
        public Frigate CreateFrigateAtDock(SmallMapReferences.SingleMapReference.Location location) =>
            OverworldMap.CreateFrigate(GetLocationOfDock(location), Point2D.Direction.Right, out _, 1);


        /// <summary>
        ///     Creates a new skiff and places it at a given location
        /// </summary>
        /// <param name="location"></param>
        /// <returns></returns>
        // ReSharper disable once UnusedMethodReturnValue.Global
        public Skiff CreateSkiffAtDock(SmallMapReferences.SingleMapReference.Location location) =>
            OverworldMap.CreateSkiff(GetLocationOfDock(location), Point2D.Direction.Right, out _);

        /// <summary>
        ///     Construct the VirtualMap (requires initialization still)
        /// </summary>
        /// <param name="initialMap"></param>
        /// <param name="currentSmallMapReference"></param>
        /// <param name="bUseExtendedSprites"></param>
        /// <param name="importedGameState"></param>
        /// <param name="theSearchItems"></param>
        internal VirtualMap(
            //LargeMap overworldMap, LargeMap underworldMap, 
            Map.Maps initialMap,
            SmallMapReferences.SingleMapReference currentSmallMapReference, bool bUseExtendedSprites,
            ImportedGameState importedGameState, SearchItems theSearchItems)
        {
            TheSearchItems = theSearchItems;

            SmallMapReferences.SingleMapReference.Location mapLocation = currentSmallMapReference?.MapLocation ??
                                                                         SmallMapReferences.SingleMapReference.Location
                                                                             .Britannia_Underworld;

            // load the characters for the very first time from disk
            // subsequent loads may not have all the data stored on disk and will need to recalculate
            CurrentMap = new MapUnits.MapUnits(initialMap, bUseExtendedSprites, importedGameState, TheSearchItems,
                mapLocation);

            switch (initialMap)
            {
                case Map.Maps.Small:
                    LoadSmallMap(currentSmallMapReference, null, !importedGameState.IsInitialSaveFile);
                    break;
                case Map.Maps.Overworld:
                case Map.Maps.Underworld:
                    LoadLargeMap(LargeMap.GetMapTypeToLargeMapType(initialMap));
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
            // TheMapOverrides.TheMap = CurrentMap;
            // this is so we don't break old save games
            if (TheSearchItems == null)
            {
                TheSearchItems = new SearchItems();
                TheSearchItems.Initialize();
            }
        }


        /// <summary>
        ///     Decides if any enemies needed to be spawned or despawned
        /// </summary>
        internal void GenerateAndCleanupEnemies(int nTurn)
        {
            if (CurrentMap.CurrentSingleMapReference == null)
                throw new Ultima5ReduxException(
                    "Tried to GenerateAndCleanupEnemies but CurrentMap.CurrentSingleMapReference was null");

            //switch (CurrentMap.CurrentSingleMapReference.MapType)
            switch (CurrentMap)
            {
                case LargeMap largeMap:
                    //case Map.Maps.Underworld:
                    // let's do this!
                    largeMap.ClearEnemiesIfFarAway();

                    if (largeMap.TotalMapUnitsOnMap >= Map.MAX_MAP_CHARACTERS) break;
                    if (OneInXOddsOfNewMonster > 0 && Utils.OneInXOdds(OneInXOddsOfNewMonster))
                        // make a random monster
                        CreateRandomMonster(nTurn);

                    break;
                case CombatMap:
                case SmallMap:
                case DungeonMap:
                    break;
                default:
                    throw new InvalidEnumArgumentException();
            }
        }

        /// <summary>
        ///     Gathers the details of what if any aggressive action the mapunits would do this turn
        /// </summary>
        /// <returns></returns>
        internal Dictionary<MapUnit, AggressiveMapUnitInfo> GetNonCombatMapAggressiveMapUnitInfo(
            TurnResults turnResults)
        {
            Dictionary<MapUnit, AggressiveMapUnitInfo> aggressiveMapUnitInfos = new();

            SmallMapReferences.SingleMapReference singleMapReference = CurrentMap.CurrentSingleMapReference;
            if (singleMapReference == null)
                throw new Ultima5ReduxException(
                    "Tried to GetAggressiveMapUnitInfo but CurrentMap.CurrentSingleMapReference was null");

            if (CurrentMap is not RegularMap regularMap)
                throw new Ultima5ReduxException("Tried to call GetNonCombatMapAggressiveMapUnitInfo on combat map");

            foreach (MapUnit mapUnit in CurrentMap.CurrentMapUnits.AllActiveMapUnits)
            {
                // we don't calculate any special movement or events for map units on different floors
                if (mapUnit.MapUnitPosition.Floor != CurrentMap.CurrentSingleMapReference.Floor)
                    continue;

                // we don't want to add anything that can never attack, so we keep only enemies and NPCs 
                // in the list of aggressors
                switch (mapUnit)
                {
                    case Horse:
                    case Enemy:
                    case NonPlayerCharacter:
                        break;
                    default:
                        // it's not an aggressive Npc or Enemy so skip on past - nothing to see here
                        continue;
                }

                AggressiveMapUnitInfo mapUnitInfo =
                    GetNonCombatMapAggressiveMapUnitInfo(mapUnit.MapUnitPosition.XY,
                        regularMap.CurrentAvatarPosition.XY,
                        SingleCombatMapReference.Territory.Britannia, mapUnit);

                if (mapUnitInfo.CombatMapReference != null)
                    turnResults.PushOutputToConsole(mapUnitInfo.AttackingMapUnit.FriendlyName + " fight me in " +
                                                    mapUnitInfo.CombatMapReference.Description);
                aggressiveMapUnitInfos.Add(mapUnit, mapUnitInfo);
            }

            return aggressiveMapUnitInfos;
        }





        /// <summary>
        ///     Advances each of the NPCs by one movement each
        /// </summary>
        internal void MoveNonCombatMapMapUnitsToNextMove(
            Dictionary<MapUnit, AggressiveMapUnitInfo> aggressiveMapUnitInfos)
        {
            if (CurrentMap is not RegularMap regularMap)
                throw new Ultima5ReduxException("MoveNonCombatMapMapUnitsToNextMove called with non RegularMap");

            // go through each of the NPCs on the map
            foreach (MapUnit mapUnit in CurrentMap.CurrentMapUnits.AllActiveMapUnits)
            {
                AggressiveMapUnitInfo aggressiveMapUnitInfo =
                    aggressiveMapUnitInfos.ContainsKey(mapUnit) ? aggressiveMapUnitInfos[mapUnit] : null;
                // if we don't match the aggressive map unit then it means the map unit is not mobile
                if (aggressiveMapUnitInfo == null) continue;

                // if the map unit doesn't haven't a particular aggression then it moves 
                if (aggressiveMapUnitInfo.GetDecidedAction() == AggressiveMapUnitInfo.DecidedAction.MoveUnit)
                    mapUnit.CompleteNextNonCombatMove(regularMap, GameStateReference.State.TheTimeOfDay); //,
            }
        }

        // Performs all of the aggressive actions and stores results
        internal void ProcessNonCombatMapAggressiveMapUnitAttacks(PlayerCharacterRecords records,
            Dictionary<MapUnit, AggressiveMapUnitInfo> aggressiveMapUnitInfos,
            out AggressiveMapUnitInfo combatMapAggressor, TurnResults turnResults)
        {
            combatMapAggressor = null;

            // should not be called on Combat Maps
            if (CurrentMap is not RegularMap regularMap) return;

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

                AggressiveMapUnitInfo.DecidedAction decidedAction = aggressiveMapUnitInfo.GetDecidedAction();
                switch (decidedAction)
                {
                    case AggressiveMapUnitInfo.DecidedAction.AttemptToArrest:
                    {
                        if (mapUnit is not NonPlayerCharacter npc)
                            throw new Ultima5ReduxException(
                                $"A non-npc tried to arrest me. They are a {mapUnit.GetType()}");
                        turnResults.PushTurnResult(
                            new AttemptToArrest(TurnResult.TurnResultType.NPCAttemptingToArrest, npc));
                        continue;
                    }
                    case AggressiveMapUnitInfo.DecidedAction.WantsToChat:
                    {
                        if (mapUnit is not NonPlayerCharacter npc)
                            throw new Ultima5ReduxException(
                                $"A non-npc tried to arrest me. They are a {mapUnit.GetType()}");
                        turnResults.PushTurnResult(new NpcTalkInteraction(npc));
                        // if they want to chat, then we start a pissed off counter
                        // it only really matters for guards though 
                        if (npc.NPCRef.IsGuard && npc.NPCState.PissedOffCountDown <= 0)
                            npc.NPCState.PissedOffCountDown = OddsAndLogic.TURNS_UNTIL_PISSED_OFF_GUARD_ARRESTS_YOU;
                        continue;
                    }
                    case AggressiveMapUnitInfo.DecidedAction.Begging:
                    {
                        if (mapUnit is not NonPlayerCharacter npc)
                            throw new Ultima5ReduxException(
                                $"A non-npc tried beg. They are a {mapUnit.GetType()}");
                        turnResults.PushTurnResult(new NpcTalkInteraction(npc));
                        continue;
                    }
                    case AggressiveMapUnitInfo.DecidedAction.BlackthornGuardPasswordCheck:
                    {
                        if (mapUnit is not NonPlayerCharacter npc)
                            throw new Ultima5ReduxException(
                                $"A non-npc tried extort half my gold. They are a {mapUnit.GetType()}");
                        if (npc.NPCState.HasExtortedAvatar) continue;
                        npc.NPCState.HasExtortedAvatar = true;
                        turnResults.PushTurnResult(new GuardExtortion(npc,
                            GuardExtortion.ExtortionType.BlackthornPassword, 0));
                        continue;
                    }
                    case AggressiveMapUnitInfo.DecidedAction.StraightToBlackthornDungeon:
                        if (mapUnit is not NonPlayerCharacter blackthornGuard)
                            throw new Ultima5ReduxException(
                                $"A non-npc tried extort half my gold. They are a {mapUnit.GetType()}");

                        turnResults.PushTurnResult(new GoToBlackthornDungeon(blackthornGuard));
                        break;
                    case AggressiveMapUnitInfo.DecidedAction.HalfYourGoldExtortion:
                    {
                        // we only extort once per load of a map, we aren't monsters after all!
                        if (mapUnit is not NonPlayerCharacter npc)
                            throw new Ultima5ReduxException(
                                $"A non-npc tried extort half my gold. They are a {mapUnit.GetType()}");
                        if (npc.NPCState.HasExtortedAvatar) continue;
                        npc.NPCState.HasExtortedAvatar = true;
                        turnResults.PushTurnResult(new GuardExtortion(npc, GuardExtortion.ExtortionType.HalfGold, 0));
                        continue;
                    }
                    case AggressiveMapUnitInfo.DecidedAction.GenericGuardExtortion:
                    {
                        // we only extort once per load of a map, we aren't monsters after all!
                        if (mapUnit is not NonPlayerCharacter npc)
                            throw new Ultima5ReduxException(
                                $"A non-npc tried generic extortion. They are a {mapUnit.GetType()}");
                        if (npc.NPCState.HasExtortedAvatar) continue;
                        npc.NPCState.HasExtortedAvatar = true;
                        turnResults.PushTurnResult(new GuardExtortion(npc, GuardExtortion.ExtortionType.Generic,
                            OddsAndLogic.GetGuardExtortionAmount(
                                OddsAndLogic.GetEraByTurn(GameStateReference.State.TurnsSinceStart))));
                        continue;
                    }
                }

                // it's possible that the aggressor may not actually be attacking even if they can
                if (decidedAction != AggressiveMapUnitInfo.DecidedAction.RangedAttack) continue;

                switch (aggressiveMapUnitInfo.AttackingMissileType)
                {
                    case CombatItemReference.MissileType.None:
                        break;

                    case CombatItemReference.MissileType.Arrow:
                        // do they have any melee attacks? Melee attacks are noted with .Arrow for now
                        // if on skiff then party takes damage
                        // if on frigate then frigate takes damage
                        if (IsAvatarInFrigate)
                            // frigate takes damage instead
                            regularMap.DamageShip(Point2D.Direction.None, turnResults);
                        else
                            records.DamageEachCharacter(turnResults, 1, 9);

                        turnResults.PushOutputToConsole(
                            $"{mapUnit.FriendlyName} attacks {records.AvatarRecord.Name} and party (melee)", false);
                        continue;
                    case CombatItemReference.MissileType.CannonBall:
                        if (IsAvatarInFrigate)
                            regularMap.DamageShip(Point2D.Direction.None, turnResults);
                        else
                            records.DamageEachCharacter(turnResults, 1, 9);

                        turnResults.PushOutputToConsole(
                            $"{mapUnit.FriendlyName} attacks {records.AvatarRecord.Name} and party (cannonball)",
                            false);

                        continue;
                    case CombatItemReference.MissileType.Red:
                        // if on a frigate then only the frigate takes damage, like a shield!
                        if (IsAvatarInFrigate)
                            regularMap.DamageShip(Point2D.Direction.None, turnResults);
                        else
                            records.DamageEachCharacter(turnResults, 1, 9);

                        turnResults.PushOutputToConsole(
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
            if (nonAttackingMapUnit.ExposeInnerItemsOnSearch)
            {
                return ProcessSearchInnerItems(turnResults, nonAttackingMapUnit, true, false);
            }

            return false;
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
                            GameReferences.Instance.DataOvlRef.StringReferences.GetString(DataOvlReference.ThingsIFindStrings
                                .A_SIMPLE_TRAP_BANG_N)));
                        nonAttackingUnit.TriggerTrap(turnResults, record.Stats, records);
                        turnResults.PushTurnResult(
                            new BasicResult(TurnResult.TurnResultType.ActionSearchTriggerSimpleTrap));
                    }
                    else
                    {
                        turnResults.PushOutputToConsole(U5StringRef.ThouDostFind(
                            GameReferences.Instance.DataOvlRef.StringReferences.GetString(DataOvlReference.ThingsIFindStrings
                                .A_SIMPLE_TRAP_N)));
                        turnResults.PushTurnResult(new BasicResult(TurnResult.TurnResultType.ActionSearchRemoveSimple));
                    }

                    break;
                case NonAttackingUnit.TrapComplexity.Complex:
                    if (bTriggeredTrap)
                    {
                        turnResults.PushOutputToConsole(
                            U5StringRef.ThouDostFind(
                                GameReferences.Instance.DataOvlRef.StringReferences.GetString(DataOvlReference.ThingsIFindStrings
                                    .A_COMPLEX_TRAP_BANG_N)));
                        nonAttackingUnit.TriggerTrap(turnResults, record.Stats, records);
                        turnResults.PushTurnResult(
                            new BasicResult(TurnResult.TurnResultType.ActionSearchTriggerComplexTrap));
                    }
                    else
                    {
                        turnResults.PushOutputToConsole(
                            U5StringRef.ThouDostFind(
                                GameReferences.Instance.DataOvlRef.StringReferences.GetString(DataOvlReference.ThingsIFindStrings
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

                if (CurrentMap.IsTileOccupied(tilePosition)) continue;

                // it's not occupied so we can create a monster
                EnemyReference enemyRef =
                    GameReferences.Instance.EnemyRefs.GetRandomEnemyReferenceByEraAndTile(nTurn,
                        CurrentMap.GetTileReference(tilePosition));
                if (enemyRef == null) continue;

                // add the new character to our list of characters currently on the map
                Enemy _ = CurrentMap.CreateEnemy(tilePosition, enemyRef, CurrentLargeMap.CurrentSingleMapReference,
                    out int _);

                return true;
            }

            return false;
        }

        /// <summary>
        ///     Gets the appropriate (if any) SingleCombatMapReference based on the map and mapunits attempting to engage in
        ///     combat
        /// </summary>
        /// <param name="attackFromPosition">where are they attacking from</param>
        /// <param name="attackToPosition">where are they attack to</param>
        /// <param name="territory"></param>
        /// <param name="aggressorMapUnit">who is the one attacking?</param>
        /// <exception cref="ArgumentOutOfRangeException"></exception>
        /// <returns></returns>
        private AggressiveMapUnitInfo GetNonCombatMapAggressiveMapUnitInfo(Point2D attackFromPosition,
            Point2D attackToPosition, SingleCombatMapReference.Territory territory, MapUnit aggressorMapUnit)
        {
            SingleCombatMapReference getSingleCombatMapReference(SingleCombatMapReference.BritanniaCombatMaps map) =>
                GameReferences.Instance.CombatMapRefs.GetSingleCombatMapReference(territory, (int)map);

            TileReference attackToTileReference = CurrentMap.GetTileReference(attackToPosition);
            TileReference attackFromTileReference = CurrentMap.GetTileReference(attackFromPosition);

            List<MapUnit> mapUnits = CurrentMap.GetMapUnitsByPosition(attackToPosition,
                CurrentMap.CurrentSingleMapReference.Floor);

            switch (mapUnits.Count)
            {
                case 0:
                    break;
                case > 1:
                    // the only excuse you can have for having more than one is if the avatar is on top of a known map unit
                    if (mapUnits.Any(m => m is Avatar))
                    {
                    }
                    else
                    {
                        throw new Ultima5ReduxException($"Did not expect {mapUnits.Count} mapunits on targeted tile");
                    }

                    break;
            }

            AggressiveMapUnitInfo mapUnitInfo = new(aggressorMapUnit);

            // if they are not Enemy type (probably NPC) then we are certain they don't have a range attack
            // UNLESS you are a wanted man - then the guards will try to attack you!
            bool isWantedManByThePoPo = CurrentMap is SmallMap { IsWantedManByThePoPo: true };
            bool declinedExtortion = CurrentMap is SmallMap { DeclinedExtortion: true };
            bool bIsMadGuard = false;
            if (aggressorMapUnit is NonPlayerCharacter npc) bIsMadGuard = isWantedManByThePoPo && npc.NPCRef.IsGuard;

            // if the guard is next to you, then they will ask you to come quietly
            bool bNextToEachOther = attackFromPosition.IsWithinNFourDirections(attackToPosition);
            if (bIsMadGuard && bNextToEachOther && !TileReferences.IsHeadOfBed(
                    CurrentMap.GetTileReference(aggressorMapUnit.MapUnitPosition.XY).Index))
            {
                // if they are at the head of the bed then we don't try to arrest, this keeps guards who are "injured" 
                // at the healers from trying to arrest
                // if avatar is being attacked..
                // we get to assume that the avatar is not necessarily next to the enemy
                mapUnitInfo.ForceDecidedAction(AggressiveMapUnitInfo.DecidedAction.AttemptToArrest);
            }

            // IF NPC is next to Avatar then we check for any AI behaviours such as arrests or extortion
            if (bNextToEachOther && aggressorMapUnit is NonPlayerCharacter nextToEachOtherNpc)
            {
                NonPlayerCharacterSchedule.AiType aiType =
                    aggressorMapUnit.GetCurrentAiType(GameStateReference.State.TheTimeOfDay);

                // because this overrides a LOT of AI behaviours, I just let all guards try to attack
                // you if you turned down the extortion
                if (nextToEachOtherNpc.NPCRef.IsGuard && isWantedManByThePoPo && declinedExtortion)
                {
                    ForceAttack(mapUnitInfo, attackFromTileReference);
                }
                else
                {
                    switch (aiType)
                    {
                        case NonPlayerCharacterSchedule.AiType.BlackthornGuardFixed:
                        case NonPlayerCharacterSchedule.AiType.BlackthornGuardWander:
                            // let's add some randomness and only check them half the time
                            if (Utils.OneInXOdds(2))
                            {
                                // are you wearing the black badge? 
                                // temporary - if you have the badge then that's good enough
                                if (GameStateReference.State.CharacterRecords.WearingBlackBadge)
                                {
                                    // if the guard has already harassed the Avatar, then they won't bug him
                                    // again until he re-enters the castle
                                    mapUnitInfo.ForceDecidedAction(AggressiveMapUnitInfo.DecidedAction
                                        .BlackthornGuardPasswordCheck);
                                }
                                else
                                {
                                    mapUnitInfo.ForceDecidedAction(AggressiveMapUnitInfo.DecidedAction
                                        .StraightToBlackthornDungeon);
                                }
                            }

                            break;
                        case NonPlayerCharacterSchedule.AiType.Begging:
                            mapUnitInfo.ForceDecidedAction(AggressiveMapUnitInfo.DecidedAction.Begging);
                            break;
                        case NonPlayerCharacterSchedule.AiType.HalfYourGoldExtortingGuard:
                        case NonPlayerCharacterSchedule.AiType.GenericExtortingGuard:
                        case NonPlayerCharacterSchedule.AiType.ExtortOrAttackOrFollow:
                            // even if they are extortionists, if you did some super bad, they will try to arrest 
                            if (isWantedManByThePoPo && declinedExtortion)
                            {
                                // attack them
                                goto case NonPlayerCharacterSchedule.AiType.DrudgeWorthThing;
                            }

                            if (isWantedManByThePoPo)
                            {
                                mapUnitInfo.ForceDecidedAction(AggressiveMapUnitInfo.DecidedAction.AttemptToArrest);
                                break;
                            }

                            mapUnitInfo.ForceDecidedAction(
                                aiType == NonPlayerCharacterSchedule.AiType.HalfYourGoldExtortingGuard
                                    ? AggressiveMapUnitInfo.DecidedAction.HalfYourGoldExtortion
                                    : AggressiveMapUnitInfo.DecidedAction.GenericGuardExtortion);
                            break;
                        case NonPlayerCharacterSchedule.AiType.SmallWanderWantsToChat:
                            // if they wanted to chat and they are a guard they can get pissed off and arrest you
                            if (isWantedManByThePoPo || (nextToEachOtherNpc.NPCState.PissedOffCountDown == 0 &&
                                                         nextToEachOtherNpc.NPCRef.IsGuard))
                            {
                                mapUnitInfo.ForceDecidedAction(AggressiveMapUnitInfo.DecidedAction.AttemptToArrest);
                            }
                            else
                            {
                                // some times non guard NPCs are just keen to chat
                                mapUnitInfo.ForceDecidedAction(AggressiveMapUnitInfo.DecidedAction.WantsToChat);
                            }

                            break;
                        case NonPlayerCharacterSchedule.AiType.FixedExceptAttackWhenIsWantedByThePoPo:
                            // wow - I just leaned how to do this goto
                            // they only attack when you are wanted by the popo
                            if (isWantedManByThePoPo) ForceAttack(mapUnitInfo, attackFromTileReference);

                            break;
                        case NonPlayerCharacterSchedule.AiType.DrudgeWorthThing:
                            ForceAttack(mapUnitInfo, attackFromTileReference);
                            break;
                    }
                }
            }
            // if a guard wants to chat, they lose patience after a while and want to arrest you
            // so we count down like a stern parent
            else if (aggressorMapUnit is NonPlayerCharacter pissedOffNonPlayerCharacter
                     && pissedOffNonPlayerCharacter.NPCState.PissedOffCountDown > 0
                     && pissedOffNonPlayerCharacter.NPCRef.IsGuard
                     && pissedOffNonPlayerCharacter.NPCState.OverridenAiType ==
                     NonPlayerCharacterSchedule.AiType.SmallWanderWantsToChat)
            {
                pissedOffNonPlayerCharacter.NPCState.PissedOffCountDown--;
            }

            if (aggressorMapUnit is not Enemy enemy) return mapUnitInfo;

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
                            mapUnitInfo.AttackingMissileType = CombatItemReference.MissileType.CannonBall;
                        break;
                    default:
                        // it's not a cannon ball but it is a missile
                        if (attackFromPosition.IsWithinN(attackToPosition, 3))
                            mapUnitInfo.AttackingMissileType = enemy.EnemyReference.LargeMapMissileType;
                        break;
                }

                return mapUnitInfo;
            }

            bool bIsPirate = enemy.EnemyReference.LargeMapMissileType == CombatItemReference.MissileType.CannonBall;
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
                    mapUnitInfo.CombatMapReference =
                        getSingleCombatMapReference(SingleCombatMapReference.BritanniaCombatMaps.BoatSouth);
                else
                    mapUnitInfo.CombatMapReference = GameReferences.Instance.CombatMapRefs.GetSingleCombatMapReference(
                        territory,
                        (int)attackToTileReference.CombatMapIndex);
            }

            return mapUnitInfo;
        }

        private void ForceAttack(AggressiveMapUnitInfo mapUnitInfo, TileReference attackFromTileReference)
        {
            mapUnitInfo.ForceDecidedAction(AggressiveMapUnitInfo.DecidedAction.EnemyAttackCombatMap);
            SingleCombatMapReference singleCombatMapReference = GetSingleCombatMapReference(
                attackFromTileReference.CombatMapIndex,
                SingleCombatMapReference.Territory.Britannia);

            mapUnitInfo.CombatMapReference = singleCombatMapReference;
        }




        private bool IsInsideBounds(in Point2D xy)
        {
            Map currentMap = CurrentMap;
            return !(xy.X >= currentMap.VisibleOnMap.Length || xy.X < 0 ||
                     xy.Y >= currentMap.VisibleOnMap[xy.X].Length || xy.Y < 0);
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
            if (PreCombatMap == null || CurrentMap is not CombatMap)
            {
                Debug.Assert(CurrentMap is RegularMap,
                    "You can't load a combat map when you are already in a combat map");
                PreCombatMap = (RegularMap)CurrentMap;
                PreMapUnitPosition.Floor = CurrentPosition.Floor;
                PreMapUnitPosition.X = CurrentPosition.X;
                PreMapUnitPosition.Y = CurrentPosition.Y;
            }

            CurrentMap.CurrentSingleMapReference = SmallMapReferences.SingleMapReference.GetCombatMapSingleInstance();

            // we only want to push the exposed items and override map if we are on a small or large map 
            // not if we are going combat to combat map (think Debug)
            // if (CurrentCombatMap != null && TheMapOverrides.NumOfRows > CurrentCombatMap.NumOfXTiles)
            //     PreTheMapOverrides = TheMapOverrides;
            if (CurrentMap is not CombatMap combatMap)
                throw new Ultima5ReduxException("Loading combat map, but it didn't flip to combat map");

            //CurrentCombatMap = new CombatMap(singleCombatMapReference);

            CurrentMap.SetCurrentMapType(CurrentMap.CurrentSingleMapReference, Map.Maps.Combat, null);
            TheCurrentMapType = Map.Maps.Combat;

            combatMap.CreateParty(entryDirection, records);

            combatMap.CreateEnemies(singleCombatMapReference, primaryEnemyReference, nPrimaryEnemies,
                secondaryEnemyReference, nSecondaryEnemies, npcRef);

            combatMap.InitializeInitiativeQueue();
        }

        public static Point2D GetLocationOfDock(SmallMapReferences.SingleMapReference.Location location)
        {
            List<byte> xDockCoords = GameReferences.Instance.DataOvlRef
                .GetDataChunk(DataOvlReference.DataChunkName.X_DOCKS)
                .GetAsByteList();
            List<byte> yDockCoords = GameReferences.Instance.DataOvlRef
                .GetDataChunk(DataOvlReference.DataChunkName.Y_DOCKS)
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


        public int ClosestTileReferenceAround(int nRadius, Func<int, bool> checkTile) =>
            ClosestTileReferenceAround(CurrentPosition.XY, nRadius, checkTile);

        public int ClosestTileReferenceAround(Point2D midPosition, int nRadius, Func<int, bool> checkTile)
        {
            double nShortestRadius = 255;
            Map currentMap = CurrentMap;
            bool bIsRepeatingMap = CurrentMap.IsRepeatingMap;
            CurrentMap.CurrentMapUnits.RefreshActiveDictionaryCache();
            // an optimization to speed up checking of map units
            Dictionary<Point2D, List<MapUnit>> cachedActive = CurrentMap.CurrentMapUnits.CachedActiveDictionary;

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

                    int nTileIndex = CurrentMap.GetTileReference(adjustedPos.X, adjustedPos.Y).Index;
                    bool bHasMapUnits = cachedActive.ContainsKey(adjustedPos);
                    MapUnit mapUnit = bHasMapUnits ? CurrentMap.GetTopVisibleMapUnit(adjustedPos, true) : null;

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

        public int ClosestTileReferenceAround(TileReference tileReference, Point2D midPosition, int nRadius)
        {
            return ClosestTileReferenceAround(midPosition, nRadius, i => tileReference.Index == i);
        }

        public int ClosestTileReferenceAround(TileReference tileReference, int nRadius)
        {
            return ClosestTileReferenceAround(CurrentPosition.XY, nRadius, i => tileReference.Index == i);
        }

        public bool ContainsSearchableMapUnits(in Point2D xy)
        {
            // moonstone check
            List<MapUnit> mapUnits = CurrentMap.GetMapUnitsOnTile(xy);
            TileReference tileReference = CurrentMap.GetTileReference(xy);

            bool bIsSearchableMapUnit = mapUnits.Any(m => m is Chest or DeadBody or BloodSpatter or DiscoverableLoot) ||
                                        tileReference.HasSearchReplacement;
            bool bIsMoonstoneBuried = GameStateReference.State.TheMoongates.IsMoonstoneBuried(xy, TheCurrentMapType);

            return (CurrentMap is not LargeMap && bIsMoonstoneBuried) || bIsSearchableMapUnit;
        }


        public Dictionary<Point2D, bool> GetAllMapOccupiedTiles()
        {
            Dictionary<Point2D, bool> occupiedDictionary = new();

            IEnumerable<MapUnit> mapUnits = CurrentMap.CurrentMapUnits.AllActiveMapUnits;
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

            IEnumerable<MapUnit> mapUnits = CurrentMap.CurrentMapUnits.AllActiveMapUnits;
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
                return CurrentMap.GetTileOverride(xy).SpriteNum;

            int nSprite = CurrentMap.GetTileReference(xy).FlatTileSubstitutionIndex;

            return nSprite is -2 or -3 ? GuessTile(xy) : nSprite;
        }

        public int GetCalculatedSpriteIndexByTile(TileReference tileReference, in Point2D tilePosInMap,
            bool bIsAvatarTile, bool bIsMapUnitOccupiedTile, MapUnit mapUnit, out bool bDrawCharacterOnTile)
        {
            int nSprite = tileReference.Index;
            bool bIsMirror = TileReferences.IsUnbrokenMirror(nSprite);
            bDrawCharacterOnTile = false;

            if (bIsMirror)
            {
                // if the avatar is south of the mirror then show his image
                Point2D expectedAvatarPos = new(tilePosInMap.X, tilePosInMap.Y + 1);
                if (expectedAvatarPos == CurrentPosition.XY)
                    return GameReferences.Instance.SpriteTileReferences.GetTileNumberByName("MirrorAvatar");
            }

            // is the sprite a Chair? if so, we need to figure out if someone is sitting on it
            bool bIsChair = TileReferences.IsChair(nSprite);
            // bh: i should clean this up so that it doesn't need to call all this - since it's being called in GetCorrectSprite
            bool bIsLadder = GameReferences.Instance.SpriteTileReferences.IsLadder(nSprite);
            // is it the human sleeping side of the bed?
            bool bIsHeadOfBed = TileReferences.IsHeadOfBed(nSprite);
            // we need to check these before they get "corrected"
            // is it the stocks
            bool bIsStocks = TileReferences.IsStocks(nSprite);
            bool bIsManacles = TileReferences.IsManacles(nSprite); // is it shackles/manacles

            // this is unfortunate since I would prefer the GetCorrectSprite took care of all of this
            bool bIsFoodNearby = TileReferences.IsChair(nSprite) && IsFoodNearby(tilePosInMap);

            bool bIsStaircase = TileReferences.IsStaircase(nSprite); // is it a staircase

            int nNewSpriteIndex;

            if (bIsStaircase && CurrentMap is SmallMap smallMap)
                nNewSpriteIndex = smallMap.GetStairsSprite(tilePosInMap).Index;
            else
            {
                // kinda hacky - but check if it's a minstrel because when they are on a chair, they play!
                if (bIsMapUnitOccupiedTile && mapUnit is NonPlayerCharacter { IsMinstrel: true } npc
                                           && nSprite == (int)TileReference.SpriteIndex.ChairBackBack && !bIsFoodNearby)
                {
                    nNewSpriteIndex = npc.AlternateSittingTileReference.Index;
                }
                else
                {
                    nNewSpriteIndex = GameReferences.Instance.SpriteTileReferences.GetCorrectSprite(nSprite,
                        bIsMapUnitOccupiedTile,
                        bIsAvatarTile, bIsFoodNearby, GameStateReference.State.TheTimeOfDay.IsDayLight);
                }
            }

            if (nNewSpriteIndex == -2) nNewSpriteIndex = GuessTile(tilePosInMap);

            bDrawCharacterOnTile = !bIsChair && !bIsLadder && !bIsHeadOfBed && !bIsStocks && !bIsManacles &&
                                   bIsMapUnitOccupiedTile;

            return nNewSpriteIndex;
        }

        public Point2D GetCameraCenter()
        {
            if (CurrentMap is CombatMap) return new Point2D(CurrentMap.NumOfXTiles / 2, CurrentMap.NumOfYTiles / 2);

            return CurrentPosition.XY;
        }

        public SingleCombatMapReference GetCombatMapReferenceForAvatarAttacking(Point2D attackFromPosition,
            Point2D attackToPosition, SingleCombatMapReference.Territory territory)
        {
            // note - attacking from a skiff OR carpet is NOT permitted unless touching a piece of land 
            // otherwise is twill say Attack-On foot!
            // note - cannot exit a skiff unless land is nearby

            // let's use this method to also determine if an enemy CAN attack the avatar from afar

            TileReference attackToTileReference = CurrentMap.GetTileReference(attackToPosition);
            if (attackToTileReference.CombatMapIndex == SingleCombatMapReference.BritanniaCombatMaps.None) return null;

            TileReference attackFromTileReference = CurrentMap.GetTileReference(attackFromPosition);

            List<MapUnit> mapUnits = CurrentMap.GetMapUnitsByPosition(attackToPosition,
                CurrentMap.CurrentSingleMapReference.Floor);

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
                        throw new Ultima5ReduxException(
                            "Did not expect Avatar mapunit on targeted tile when Avatar is attacking");

                    // a little lazy for now
                    targettedMapUnit = mapUnits[0];
                    targettedMapUniTileReference = targettedMapUnit.KeyTileReference;
                    break;
            }

            // if the avatar is in a skiff of on a carpet, but is in the ocean then they aren't allowed to attack
            if (IsAvatarInSkiff || IsAvatarRidingCarpet)
            {
                bool bAvatarOnWaterTile = attackFromTileReference.IsWaterTile;

                if (bAvatarOnWaterTile &&
                    attackToTileReference.CombatMapIndex is
                        SingleCombatMapReference.BritanniaCombatMaps
                            .BoatCalc) // if no surrounding tiles are water tile then we skip the attack
                    return null;
            }

            // there is someone to target
            if (!IsAvatarInFrigate)
            {
                // last second check for water enemy - they can occasionally appear on a "land" tile like bridges
                // so we take the chance to force a Bay map just in case
                if (targettedMapUnit is Enemy waterCheckEnemy)
                {
                    if (waterCheckEnemy.EnemyReference.IsWaterEnemy)
                        return GetSingleCombatMapReference(SingleCombatMapReference.BritanniaCombatMaps.Bay, territory);
                    // if the enemy is on bay but is not a water creature then we cannot attack them
                    if (attackToTileReference.CombatMapIndex == SingleCombatMapReference.BritanniaCombatMaps.Bay)
                        return null;
                }

                return GetSingleCombatMapReference(attackToTileReference.CombatMapIndex, territory);
            }

            // BoatCalc indicates it is a water tile and requires special consideration
            if (attackToTileReference.CombatMapIndex != SingleCombatMapReference.BritanniaCombatMaps.BoatCalc)
            {
                if (attackToTileReference.IsWaterEnemyPassable)
                    return GetSingleCombatMapReference(SingleCombatMapReference.BritanniaCombatMaps.BoatOcean,
                        territory);

                // BoatSouth indicates the avatar is on the frigate, and the enemy on land
                return GetSingleCombatMapReference(SingleCombatMapReference.BritanniaCombatMaps.BoatSouth, territory);
            }

            // if attacking another frigate, then it's boat to boat
            if (GameReferences.Instance.SpriteTileReferences.IsFrigate(targettedMapUniTileReference.Index))
                return GetSingleCombatMapReference(SingleCombatMapReference.BritanniaCombatMaps.BoatBoat, territory);

            // otherwise it's boat (ours) to ocean
            return GetSingleCombatMapReference(SingleCombatMapReference.BritanniaCombatMaps.BoatOcean, territory);
            // it is not boat calc, but there is an enemy, so refer to our default combat map
        }

        private SingleCombatMapReference GetSingleCombatMapReference(SingleCombatMapReference.BritanniaCombatMaps map,
            SingleCombatMapReference.Territory territory) =>
            GameReferences.Instance.CombatMapRefs.GetSingleCombatMapReference(territory, (int)map);

        /// <summary>
        ///     Gets the Avatar's current position in 3D spaces
        /// </summary>
        /// <returns></returns>
        public Point3D GetCurrent3DPosition()
        {
            if (TheCurrentMapType == Map.Maps.Small)
                return new Point3D(CurrentPosition.X, CurrentPosition.Y, CurrentSmallMap.MapFloor);

            return new Point3D(CurrentPosition.X, CurrentPosition.Y,
                TheCurrentMapType == Map.Maps.Overworld ? 0 : 0xFF);
        }

        /// <summary>
        ///     Gets a map unit on the current tile (that ISN'T the Avatar)
        /// </summary>
        /// <returns>MapUnit or null if none exist</returns>
        public MapUnit GetMapUnitOnCurrentTile() => CurrentMap.GetTopVisibleMapUnit(CurrentPosition.XY, true);


        private readonly List<Type> _searchOrderPriority = new()
        {
            typeof(DiscoverableLoot), typeof(ItemStack), typeof(Chest), typeof(DeadBody), typeof(BloodSpatter)
        };

        public NonAttackingUnit GetNonAttackingMapUnitOnTileBySearchPriority(in Point2D xy)
        {
            if (CurrentMap.CurrentSingleMapReference == null)
                throw new Ultima5ReduxException("No single map is set in virtual map");

            List<MapUnit> mapUnitsAtPosition = CurrentMap.GetMapUnitsOnTile(xy);

            // this is inefficient, but the lists are so small it is unlikely to matter
            foreach (Type type in _searchOrderPriority)
            {
                foreach (MapUnit mapUnit in mapUnitsAtPosition.Where(mapUnit => mapUnit.GetType() == type))
                {
                    if (mapUnit is not NonAttackingUnit nonAttackingUnit) continue;
                    if (!nonAttackingUnit.IsSearchable) continue;

                    return nonAttackingUnit;
                }
            }

            return null;
            // List<MapUnit> mapUnits =
            //     CurrentMap.GetMapUnitsByPosition(LargeMapOverUnder, xy, CurrentMap.CurrentSingleMapReference.Floor);

            //return mapUnits;
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

            if (CurrentMap.CurrentSingleMapReference == null)
                throw new Ultima5ReduxException("No single map is set in virtual map");

            var npc = CurrentMap.GetSpecificMapUnitByLocation<NonPlayerCharacter>(adjustedPosition,
                CurrentMap.CurrentSingleMapReference.Floor);

            if (npc != null) return npc;

            if (!CurrentMap.GetTileReference(adjustedPosition).IsTalkOverable)
                return null;

            Point2D adjustedPosition2Away = MapUnitMovement.GetAdjustedPos(CurrentPosition.XY, direction, 2);

            if (CurrentMap.CurrentSingleMapReference == null)
                throw new Ultima5ReduxException("No single map is set in virtual map");

            return CurrentMap.GetSpecificMapUnitByLocation<NonPlayerCharacter>(adjustedPosition2Away,
                CurrentMap.CurrentSingleMapReference.Floor);
        }

        // ReSharper disable once MemberCanBePrivate.Global
        public SeaFaringVessel GetSeaFaringVesselAtDock(SmallMapReferences.SingleMapReference.Location location)
        {
            // 0 = Jhelom
            // 1 = Minoc
            // 2 = East Brittany
            // 3 = Buccaneer's Den

            var seaFaringVessel =
                CurrentMap.GetSpecificMapUnitByLocation<SeaFaringVessel>(GetLocationOfDock(location), 0, true);
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
            if (!CurrentMap.GetTileReference(xy.X - 1, xy.Y).IsSolidSprite) return Point2D.Direction.Left;
            if (!CurrentMap.GetTileReference(xy.X + 1, xy.Y).IsSolidSprite) return Point2D.Direction.Right;
            if (!CurrentMap.GetTileReference(xy.X, xy.Y - 1).IsSolidSprite) return Point2D.Direction.Up;
            if (!CurrentMap.GetTileReference(xy.X, xy.Y + 1).IsSolidSprite) return Point2D.Direction.Down;
            throw new Ultima5ReduxException("Can't get stair direction - something is amiss....");
        }




        public TileStack GetTileStack(Point2D xy, bool bSkipMapUnit)
        {
            var tileStack = new TileStack(xy);

            // this checks to see if you are on the outer bounds of a small map, and if the flood fill touched it
            // if it has touched it then we draw the outer tiles
            if (CurrentMap is SmallMap smallMap && !smallMap.IsInBounds(xy) && smallMap.TouchedOuterBorder)
            {
                TileReference outerTileReference = GameReferences.Instance.SpriteTileReferences.GetTileReference(
                    smallMap.GetOutOfBoundsSprite(xy));
                tileStack.PushTileReference(outerTileReference);
                return tileStack;
            }

            // if the position of the tile is no longer inside the bounds of the visibility
            // or has become invisible, then destroy the voxels and return right away
            bool bOutsideOfVisibilityArray = !IsInsideBounds(xy);
            if (bOutsideOfVisibilityArray || !CurrentMap.VisibleOnMap[xy.X][xy.Y])
            {
                if (!bOutsideOfVisibilityArray)
                    tileStack.PushTileReference(GameReferences.Instance.SpriteTileReferences.GetTileReference(255));
                return tileStack;
            }

            // get the reference as per the original game data
            TileReference origTileReference = CurrentMap.GetTileReference(xy);
            // get topmost active map units on the tile
            MapUnit topMostMapUnit = bSkipMapUnit ? null : CurrentMap.GetTopVisibleMapUnit(xy, false);

            bool bIsAvatarTile = CurrentMap is not CombatMap && xy == CurrentPosition?.XY;
            bool bIsMapUnitOccupiedTile = topMostMapUnit != null;

            // if there is an alternate flat sprite (for example, trees have grass)
            if (origTileReference.HasAlternateFlatSprite ||
                CurrentMap.IsXYOverride(xy, TileOverrideReference.TileType.Flat))
            {
                TileReference flatTileReference =
                    GameReferences.Instance.SpriteTileReferences.GetTileReference(GetAlternateFlatSprite(xy));
                tileStack.PushTileReference(flatTileReference);
            }

            // we always push the original tile reference 
            int nCalculatedIndex = GetCalculatedSpriteIndexByTile(origTileReference, xy, bIsAvatarTile,
                bIsMapUnitOccupiedTile, topMostMapUnit, out bool bDrawCharacterOnTile);
            TileReference calculatedTileReference =
                GameReferences.Instance.SpriteTileReferences.GetAnimatedTileReference(nCalculatedIndex);

            // there are times we will not draw a calculated reference - such as when an NPC is on a door 
            // which indicates it is open, and therefor hidden
            bool bSkipCalculatedTileReference =
                (TileReferences.IsDoor(calculatedTileReference.Index) && bIsMapUnitOccupiedTile)
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
                avatarMapUnit =
                    CurrentMap is RegularMap regularMap ? regularMap.GetAvatarMapUnit() : null;

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
            if (CurrentMap is not LargeMap && CurrentMap.IsXYOverride(xy, TileOverrideReference.TileType.Flat))
                return CurrentMap.GetTileOverride(xy).SpriteNum;

            if (CurrentMap is LargeMap && CurrentMap.IsXYOverride(xy, TileOverrideReference.TileType.Flat))
                return CurrentMap.GetTileOverride(xy).SpriteNum;

            for (int i = -1; i <= 1; i++)
            {
                for (int j = -1; j <= 1; j++)
                {
                    // if it is out of bounds then we skips them altogether
                    if (xy.X + i < 0 || xy.X + i >= NumberOfRowTiles || xy.Y + j < 0 || xy.Y + j >= NumberOfColumnTiles)
                        continue;
                    TileReference tileRef = CurrentMap.GetTileReference(xy.X + i, xy.Y + j);
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


        public bool IsCombatMapUnitOccupiedTile(in Point2D xy) =>
            Map.IsMapUnitOccupiedFromList(xy, CurrentMap.CurrentSingleMapReference.Floor,
                CurrentMap.CurrentMapUnits.AllCombatMapUnits);

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
            bool bIsFoodNearby = isFoodTable(CurrentMap.GetTileReference(characterPos.X, characterPos.Y - 1).Index) ||
                                 isFoodTable(CurrentMap.GetTileReference(characterPos.X, characterPos.Y + 1).Index);
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

            int nOpenSpacesHorizOnX = (CurrentMap.GetTileReference(xy.X - 1, xy.Y).IsSolidSpriteButNotNPC ? 0 : 1)
                                      + (CurrentMap.GetTileReference(xy.X + 1, xy.Y).IsSolidSpriteButNotNPC ? 0 : 1);

            return nOpenSpacesHorizOnX == 0;
        }

        public bool IsHorizTombstone(in Point2D xy)
        {
            if (xy.X - 1 < 0 || xy.X + 1 >= NumberOfColumnTiles) return false;
            if (xy.Y - 1 < 0 || xy.Y + 1 >= NumberOfRowTiles) return true;

            int nOpenSpacesHorizOnX = (CurrentMap.GetTileReference(xy.X - 1, xy.Y).IsSolidSpriteButNotNPC ? 0 : 1)
                                      + (CurrentMap.GetTileReference(xy.X + 1, xy.Y).IsSolidSpriteButNotNPC ? 0 : 1);
            return nOpenSpacesHorizOnX == 0;
        }


        /// <summary>
        ///     Determines if a specific Dock is occupied by a Sea Faring Vessel
        /// </summary>
        /// <returns></returns>
        public bool IsShipOccupyingDock(SmallMapReferences.SingleMapReference.Location location) =>
            GetSeaFaringVesselAtDock(location) != null;


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
                    EnemyReference enemyReference =
                        GameReferences.Instance.EnemyRefs.GetEnemyReference(npc.KeyTileReference.Index);
                    LoadCombatMap(singleCombatMapReference, SingleCombatMapReference.EntryDirection.South, records,
                        enemyReference, npc.NPCRef);
                    return;
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
            EnemyReference secondaryEnemyReference =
                GameReferences.Instance.EnemyRefs.GetFriendReference(primaryEnemyReference);

            LoadCombatMap(singleCombatMapReference, entryDirection, records, primaryEnemyReference, nPrimaryEnemies,
                secondaryEnemyReference, nSecondaryEnemies);
        }

        /// <summary>
        ///     Loads a large map -either overworld or underworld
        /// </summary>
        /// <param name="largeMapType"></param>
        public void LoadLargeMap(LargeMapLocationReferences.LargeMapType largeMapType) //Map.Maps map)
        {
            // if you are somehow transported between two different small map locations, then the guards
            // forget about your transgressions
            if (CurrentMap is SmallMap smallMap)
            {
                smallMap.ClearSmallMapFlags();
            }

            TheCurrentMapType = LargeMap.GetLargeMapTypeToMapType(largeMapType);

            switch (largeMapType)
            {
                case LargeMapLocationReferences.LargeMapType.Overworld:
                case LargeMapLocationReferences.LargeMapType.Underworld:
                    CurrentMap.CurrentSingleMapReference = CurrentLargeMap.CurrentSingleMapReference;
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(largeMapType), largeMapType, null);
            }

            // this will probably fail, which means creating a new map was a good idea
            // bajh: maybe we store each map override indefinitely so we never lose anything 
            //TheMapOverrides = new MapOverrides(CurrentLargeMap);

            CurrentMap.SetCurrentMapType(SmallMapReferences.SingleMapReference.GetLargeMapSingleInstance(largeMapType),
                map, null);

            // // you got out, and the guards have short memories
            // IsWantedManByThePoPo = false;
        }


        public void LoadDungeonMap(SingleDungeonMapFloorReference singleDungeonMapFloorReference,
            Point2D startingPosition)
        {
            // if you are somehow transported between two different small map locations, then the guards
            // forget about your transgressions
            if (CurrentMap is SmallMap smallMap)
            {
                smallMap.ClearSmallMapFlags();
            }

            // CurrentMap.CurrentSingleMapReference = ...
            CurrentMap.CurrentSingleMapReference = singleDungeonMapFloorReference.SingleMapReference;

            // SingleDungeonMapFloorReference thing = GameReferences.Instance.DungeonReferences.GetDungeon(CurrentMap.CurrentSingleMapReference.MapLocation)
            //     .GetSingleDungeonMapFloorReferenceByFloor(singleDungeonMapFloorReference.DungeonFloor);
            CurrentDungeonMap = new DungeonMap(singleDungeonMapFloorReference);
            //Dungeons.GetDungeonMap();

            TheCurrentMapType = Map.Maps.Dungeon;
            //TheMapOverrides = new();

            CurrentMap.SetCurrentMapType(singleDungeonMapFloorReference.SingleMapReference, Map.Maps.Dungeon, null);

            if (startingPosition != null) CurrentPosition.XY = startingPosition;
        }

        /// <summary>
        ///     Some logic has to be processed afterwards and requires special conditions
        /// </summary>
        /// <exception cref="Ultima5ReduxException"></exception>
        private void HandleSpecialCasesForSmallMapLoad()
        {
            if (CurrentMap.CurrentSingleMapReference.MapLocation ==
                SmallMapReferences.SingleMapReference.Location.Iolos_Hut
                && GameStateReference.State.TheTimeOfDay.IsFirstDay())
            {
                // this is a bit redundant, but left in just in case
                // it it is the first day then we don't include Smith or the Rats. Let's start the day off
                // on a positive note!
                NonPlayerCharacter smith = CurrentMap.CurrentMapUnits.NonPlayerCharacters.FirstOrDefault(m =>
                                               (TileReference.SpriteIndex)m.NPCRef.NPCKeySprite is TileReference.SpriteIndex.HorseLeft
                                               or TileReference.SpriteIndex.HorseRight) ??
                                           throw new Ultima5ReduxException("Smith was not in Iolo's hut");

                CurrentMap.CurrentMapUnits.ClearMapUnit(smith);
                CurrentMap.CurrentMapUnits.AllMapUnits.RemoveAll(m => m is NonPlayerCharacter);

                // foreach (NonPlayerCharacter npc in CurrentMap.CurrentMapUnits.NonPlayerCharacters)
                // {
                //     CurrentMap.CurrentMapUnits.ClearMapUnit(npc);
                // }
            }
        }

        public void LoadSmallMap(SmallMapReferences.SingleMapReference singleMapReference, Point2D xy = null,
            bool bLoadFromDisk = false)
        {
            // if you are somehow transported between two different small map locations, then the guards
            // forget about your transgressions
            if (CurrentMap is SmallMap smallMap)
            {
                smallMap.ClearSmallMapFlags();
            }

            CurrentMap.CurrentSingleMapReference = singleMapReference ??
                                                   throw new Ultima5ReduxException(
                                                       "Tried to load a small map, but null map reference was given");

            // setting these two will result in CurrentMap  
            CurrentSmallMap = _smallMaps.GetSmallMap(singleMapReference.MapLocation, singleMapReference.Floor);
            TheCurrentMapType = Map.Maps.Small;

            if (CurrentMap is not SmallMap newSmallMap)
                throw new Ultima5ReduxException("CurrentMap did not switch to SmallMap on load");

            newSmallMap.SetCurrentMapType(singleMapReference, Map.Maps.Small, TheSearchItems, bLoadFromDisk);

            // change the floor that the Avatar is on, otherwise he will be on the last floor he started on
            newSmallMap.GetAvatarMapUnit().MapUnitPosition.Floor = singleMapReference.Floor;

            if (xy != null) CurrentPosition.XY = xy;

            HandleSpecialCasesForSmallMapLoad();
        }


        public bool ProcessSearchInnerItems(TurnResults turnResults, NonAttackingUnit nonAttackingUnit, bool bSearched,
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
                    CurrentMap.PlaceNonAttackingUnit(discoverableLoot.AlternateNonAttackingUnit,
                        discoverableLoot.AlternateNonAttackingUnit.MapUnitPosition, TheCurrentMapType);

                    bHasInnerItems = true;

                    if (discoverableLoot.IsDeadBody)
                    {
                        turnResults.PushOutputToConsole(U5StringRef.ThouDostFind(
                            GameReferences.Instance.DataOvlRef.StringReferences.GetString(DataOvlReference.ThingsIFindStrings
                                .A_ROTTING_BODY_BANG_N)));
                    }
                    else if (discoverableLoot.IsBloodSpatter)
                    {
                        turnResults.PushOutputToConsole(U5StringRef.ThouDostFind(
                            GameReferences.Instance.DataOvlRef.StringReferences.GetString(DataOvlReference.ThingsIFindStrings
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
                CurrentMap.RePlaceNonAttackingUnit(nonAttackingUnit, itemStack, nonAttackingUnit.MapUnitPosition,
                    TheCurrentMapType);

            return bHasInnerItems;
        }

        public void ReturnToPreviousMapAfterCombat()
        {
            switch (PreCombatMap)
            {
                case LargeMap _:
                case SmallMap _:
                    // restore our old map overrides
                    //TheMapOverrides = PreTheMapOverrides;

                    TheCurrentMapType = PreCombatMap.CurrentSingleMapReference.MapType;
                    CurrentMap.CurrentSingleMapReference = PreCombatMap.CurrentSingleMapReference;

                    // this ensure the old small map is not reloaded entirely
                    // it is a dirty function, but any "cleared" units before the combat will stay cleared
                    PreCombatMap.SetCurrentMapTypeNoLoad(PreCombatMap.CurrentSingleMapReference, TheCurrentMapType);

                    PreCombatMap.GetAvatarMapUnit().MapUnitPosition = PreMapUnitPosition;
                    PreCombatMap = null;
                    break;
                default:
                    throw new Ultima5ReduxException(
                        "Attempting to return to previous map after combat with an unsupported map type: " +
                        PreCombatMap?.GetType());
            }
        }


        public void SwapTiles(Point2D tile1Pos, Point2D tile2Pos)
        {
            TileReference tileRef1 = CurrentMap.GetTileReference(tile1Pos);
            TileReference tileRef2 = CurrentMap.GetTileReference(tile2Pos);

            CurrentMap.SetOverridingTileReferece(tileRef1, tile2Pos);
            CurrentMap.SetOverridingTileReferece(tileRef2, tile1Pos);
        }

        /// <summary>
        ///     Use the stairs and change floors, loading a new map
        /// </summary>
        /// <param name="xy">the position of the stairs, ladder or trapdoor</param>
        /// <param name="bForceDown">force a downward stairs</param>
        public void UseStairs(Point2D xy, bool bForceDown = false)
        {
            if (CurrentMap is not SmallMap smallMap)
                throw new Ultima5ReduxException("Cannot UseStairs unless you are in a small map");

            bool bStairGoUp = smallMap.IsStairGoingUp(out _) && !bForceDown;

            if (CurrentMap.CurrentSingleMapReference == null)
                throw new Ultima5ReduxException("No single map is set in virtual map");

            LoadSmallMap(GameReferences.Instance.SmallMapRef.GetSingleMapByLocation(
                CurrentMap.CurrentSingleMapReference.MapLocation,
                CurrentSmallMap.MapFloor + (bStairGoUp ? 1 : -1)), xy.Copy());
        }
    }
}