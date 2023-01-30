using System;
using System.Collections.Generic;
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
        [DataMember] public MapHolder TheMapHolder { get; private set; }
        [DataMember] public SavedMapRefs SavedMapRefs { get; private set; }
        [IgnoreDataMember] private SavedMapRefs PreCombatMapSavedMapRefs { get; set; }

        [DataMember] public int OneInXOddsOfNewMonster { get; set; } = 16;


        /// <summary>
        ///     The abstracted Map object for the current map
        ///     Returns large or small depending on what is active
        /// </summary>
        [IgnoreDataMember]
        public Map CurrentMap
        {
            get
            {
                SmallMapReferences.SingleMapReference singleMapReference = SavedMapRefs.GetSingleMapReference();
                if (singleMapReference == null)
                    throw new Ultima5ReduxException("Tried to get CurrentMap but it was false");

                if (singleMapReference.IsDungeon) return TheMapHolder.TheDungeonMap;

                switch (SavedMapRefs.Location)
                {
                    case SmallMapReferences.SingleMapReference.Location.Combat_resting_shrine
                        when SavedMapRefs.MapType == Map.Maps.Combat:
                        return TheMapHolder.TheCombatMap;
                    case SmallMapReferences.SingleMapReference.Location.Combat_resting_shrine:
                        throw new Ultima5ReduxException("Resting and Shrines have not been implemented yet");
                    case SmallMapReferences.SingleMapReference.Location.Britannia_Underworld:
                        return SavedMapRefs.Floor == 0 ? TheMapHolder.OverworldMap : TheMapHolder.UnderworldMap;
                    default:
                        return TheMapHolder.GetSmallMap(singleMapReference);
                }
            }
        }


        [IgnoreDataMember]
        public bool IsBasement
        {
            get
            {
                if (CurrentMap.CurrentSingleMapReference == null) return false;

                return CurrentMap is not LargeMap && CurrentMap.CurrentSingleMapReference.Floor == -1;
            }
        }

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

        // public MapUnitPosition PreviousPosition { get; } = new();

        [DataMember] public SearchItems TheSearchItems { get; private set; }

        /// <summary>
        ///     Creates a new frigate at a dock of a given location
        /// </summary>
        /// <param name="location"></param>
        // ReSharper disable once UnusedMethodReturnValue.Global
        public Frigate CreateFrigateAtDock(SmallMapReferences.SingleMapReference.Location location) =>
            TheMapHolder.OverworldMap.CreateFrigate(LargeMapLocationReferences.GetLocationOfDock(location),
                Point2D.Direction.Right, out _, 1);


        /// <summary>
        ///     Creates a new skiff and places it at a given location
        /// </summary>
        /// <param name="location"></param>
        /// <returns></returns>
        // ReSharper disable once UnusedMethodReturnValue.Global
        public Skiff CreateSkiffAtDock(SmallMapReferences.SingleMapReference.Location location) =>
            TheMapHolder.OverworldMap.CreateSkiff(LargeMapLocationReferences.GetLocationOfDock(location),
                Point2D.Direction.Right, out _);

        /// <summary>
        ///     Construct the VirtualMap (requires initialization still)
        ///
        ///     This is only called on legacy game initiation - future 'new' loads will use deserialization
        /// </summary>
        /// <param name="initialMap"></param>
        /// <param name="currentSmallMapReference"></param>
        /// <param name="bUseExtendedSprites"></param>
        /// <param name="importedGameState"></param>
        /// <param name="theSearchItems"></param>
        internal VirtualMap(
            Map.Maps initialMap,
            SmallMapReferences.SingleMapReference currentSmallMapReference, bool bUseExtendedSprites,
            ImportedGameState importedGameState, SearchItems theSearchItems)
        {
            TheSearchItems = theSearchItems;
            TheMapHolder = new MapHolder();

            TheMapHolder.OverworldMap.InitializeFromLegacy(theSearchItems, importedGameState);
            TheMapHolder.UnderworldMap.InitializeFromLegacy(theSearchItems, importedGameState);

            switch (initialMap)
            {
                case Map.Maps.Small:
                    LoadSmallMap(currentSmallMapReference, null, !importedGameState.IsInitialSaveFile,
                        importedGameState);
                    if (CurrentMap is not SmallMap smallMap)
                        throw new Ultima5ReduxException("Tried to load Small Map initially but wasn't set correctly");
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
            // // this is so we don't break old save games
            // if (TheSearchItems == null)
            // {
            TheSearchItems = new SearchItems();
            TheSearchItems.Initialize();
            // }
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
                        if (regularMap.IsAvatarInFrigate)
                            // frigate takes damage instead
                            regularMap.DamageShip(Point2D.Direction.None, turnResults);
                        else
                            records.DamageEachCharacter(turnResults, 1, 9);

                        turnResults.PushOutputToConsole(
                            $"{mapUnit.FriendlyName} attacks {records.AvatarRecord.Name} and party (melee)", false);
                        continue;
                    case CombatItemReference.MissileType.CannonBall:
                        if (regularMap.IsAvatarInFrigate)
                            regularMap.DamageShip(Point2D.Direction.None, turnResults);
                        else
                            records.DamageEachCharacter(turnResults, 1, 9);

                        turnResults.PushOutputToConsole(
                            $"{mapUnit.FriendlyName} attacks {records.AvatarRecord.Name} and party (cannonball)",
                            false);

                        continue;
                    case CombatItemReference.MissileType.Red:
                        // if on a frigate then only the frigate takes damage, like a shield!
                        if (regularMap.IsAvatarInFrigate)
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

        // internal void SavePreviousPosition(MapUnitPosition mapUnitPosition)
        // {
        //     PreviousPosition.X = mapUnitPosition.X;
        //     PreviousPosition.Y = mapUnitPosition.Y;
        //     PreviousPosition.Floor = mapUnitPosition.Floor;
        // }


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

            if (PreCombatMapSavedMapRefs == null || CurrentMap is not CombatMap)
            {
                Debug.Assert(CurrentMap is RegularMap,
                    "You can't load a combat map when you are already in a combat map");
                // save the existing map details to return to
                // we also grab the latest XYZ and save it since we don't keep track of the 
                // the XY coords expect at load and save
                PreCombatMapSavedMapRefs = SavedMapRefs.Copy();
                PreCombatMapSavedMapRefs.MapUnitPosition.Floor = CurrentPosition.Floor;
                PreCombatMapSavedMapRefs.MapUnitPosition.X = CurrentPosition.X;
                PreCombatMapSavedMapRefs.MapUnitPosition.Y = CurrentPosition.Y;
            }

            TheMapHolder.TheCombatMap = new CombatMap(singleCombatMapReference);

            // if the PreCombatMap is not set OR the existing map is not already a combat map then
            // we set the PreCombatMap so we know which map to return to
            // if (PreCombatMap == null || CurrentMap is not CombatMap)
            // {
            //     Debug.Assert(CurrentMap is RegularMap,
            //         "You can't load a combat map when you are already in a combat map");
            //     PreCombatMap = (RegularMap)CurrentMap;
            //     PreMapUnitPosition.Floor = CurrentPosition.Floor;
            //     PreMapUnitPosition.X = CurrentPosition.X;
            //     PreMapUnitPosition.Y = CurrentPosition.Y;
            // }

            SavedMapRefs.SetBySingleCombatMapReference(singleCombatMapReference);
            // CombatMap c;
            // c.TheCombatMapReference
            //
            // CurrentMap.CurrentSingleMapReference = SmallMapReferences.SingleMapReference.GetCombatMapSingleInstance();

            // we only want to push the exposed items and override map if we are on a small or large map 
            // not if we are going combat to combat map (think Debug)
            // if (CurrentCombatMap != null && TheMapOverrides.NumOfRows > CurrentCombatMap.NumOfXTiles)
            //     PreTheMapOverrides = TheMapOverrides;
            if (CurrentMap is not CombatMap combatMap)
                throw new Ultima5ReduxException("Loading combat map, but it didn't flip to combat map");

            //CurrentCombatMap = new CombatMap(singleCombatMapReference);

            // CurrentMap.SetCurrentMapType(CurrentMap.CurrentSingleMapReference, Map.Maps.Combat, null);
            // TheCurrentMapType = Map.Maps.Combat;

            combatMap.CreateParty(entryDirection, records);

            combatMap.CreateEnemies(singleCombatMapReference, primaryEnemyReference, nPrimaryEnemies,
                secondaryEnemyReference, nSecondaryEnemies, npcRef);

            combatMap.InitializeInitiativeQueue();
        }


        public bool ContainsSearchableMapUnits(in Point2D xy)
        {
            // moonstone check
            List<MapUnit> mapUnits = CurrentMap.GetMapUnitsOnTile(xy);
            TileReference tileReference = CurrentMap.GetTileReference(xy);

            bool bIsSearchableMapUnit = mapUnits.Any(m => m is Chest or DeadBody or BloodSpatter or DiscoverableLoot) ||
                                        tileReference.HasSearchReplacement;
            bool bIsMoonstoneBuried =
                GameStateReference.State.TheMoongates.IsMoonstoneBuried(xy, CurrentMap.TheMapType);

            return (CurrentMap is not LargeMap && bIsMoonstoneBuried) || bIsSearchableMapUnit;
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
            bool bIsFoodNearby = TileReferences.IsChair(nSprite) && CurrentMap.IsFoodNearby(tilePosInMap);

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

        public Point2D GetCameraCenter() =>
            CurrentMap is CombatMap
                ? new Point2D(CurrentMap.NumOfXTiles / 2, CurrentMap.NumOfYTiles / 2)
                : CurrentPosition.XY;


        /// <summary>
        ///     Gets the Avatar's current position in 3D spaces
        /// </summary>
        /// <returns></returns>
        public Point3D GetCurrent3DPosition()
        {
            if (CurrentMap is SmallMap smallMap)
                return new Point3D(CurrentPosition.X, CurrentPosition.Y, smallMap.MapFloor);

            return new Point3D(CurrentPosition.X, CurrentPosition.Y,
                CurrentMap.TheMapType == Map.Maps.Overworld ? 0 : 0xFF);
        }


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



        // ReSharper disable once MemberCanBePrivate.Global
        public SeaFaringVessel GetSeaFaringVesselAtDock(SmallMapReferences.SingleMapReference.Location location)
        {
            // 0 = Jhelom
            // 1 = Minoc
            // 2 = East Brittany
            // 3 = Buccaneer's Den

            var seaFaringVessel =
                TheMapHolder.OverworldMap.GetSpecificMapUnitByLocation<SeaFaringVessel>(
                    LargeMapLocationReferences.GetLocationOfDock(location), 0,
                    true);
            return seaFaringVessel;
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
        /// <param name="playerPosition"></param>
        public void LoadLargeMap(LargeMapLocationReferences.LargeMapType largeMapType, Point2D playerPosition = null)
        {
            // if you are somehow transported between two different small map locations, then the guards
            // forget about your transgressions
            ClearAllFlagsBeforeMapLoad();

            SavedMapRefs ??= new SavedMapRefs();
            SavedMapRefs.SetByLargeMapType(largeMapType, playerPosition);
        }


        public void LoadDungeonMap(SingleDungeonMapFloorReference singleDungeonMapFloorReference,
            Point2D startingPosition)
        {
            // if you are somehow transported between two different small map locations, then the guards
            // forget about your transgressions
            ClearAllFlagsBeforeMapLoad();

            SavedMapRefs ??= new SavedMapRefs();
            SavedMapRefs.SetBySingleDungeonMapFloorReference(singleDungeonMapFloorReference, startingPosition);

            TheMapHolder.TheDungeonMap = new DungeonMap(singleDungeonMapFloorReference);

            if (startingPosition != null) CurrentPosition.XY = startingPosition;
        }


        private void ClearAllFlagsBeforeMapLoad()
        {
            // this likely means it's our first load and there is nothing to clear
            if (SavedMapRefs == null) return;
            switch (CurrentMap)
            {
                case SmallMap smallMap:
                    smallMap.ClearSmallMapFlags();
                    break;
                case CombatMap:
                    GameStateReference.State.CharacterRecords.ClearCombatStatuses();
                    break;
            }
        }

        /// <summary>
        ///     Used in initial legacy loads, as well as pretty much any other small map load
        /// </summary>
        /// <param name="singleMapReference"></param>
        /// <param name="xy"></param>
        /// <param name="bLoadFromDisk"></param>
        /// <param name="importedGameState"></param>
        /// <exception cref="Ultima5ReduxException"></exception>
        public void LoadSmallMap(SmallMapReferences.SingleMapReference singleMapReference, Point2D xy = null,
            bool bLoadFromDisk = false, ImportedGameState importedGameState = null)
        {
            // if you are somehow transported between two different small map locations, then the guards
            // forget about your transgressions
            ClearAllFlagsBeforeMapLoad();

            // setting this will make everything think we have a new map
            // CurrentMap will now point to the new map as well
            SavedMapRefs ??= new SavedMapRefs();
            SavedMapRefs.SetBySingleMapReference(singleMapReference, xy);

            // safety check
            if (CurrentMap is not SmallMap smallMap)
                throw new Ultima5ReduxException("CurrentMap did not switch to SmallMap on load");

            smallMap.InitializeFromLegacy(TheMapHolder.SmallMaps, singleMapReference.MapLocation,
                importedGameState, bLoadFromDisk,
                TheSearchItems);

            smallMap.HandleSpecialCasesForSmallMapLoad();
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
                        discoverableLoot.AlternateNonAttackingUnit.MapUnitPosition, CurrentMap.TheMapType);

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
                    CurrentMap.TheMapType);

            return bHasInnerItems;
        }

        public void ReturnToPreviousMapAfterCombat()
        {
            switch (PreCombatMapSavedMapRefs.MapType)
            {
                case Map.Maps.Small:
                case Map.Maps.Overworld:
                case Map.Maps.Underworld:
                    SavedMapRefs = PreCombatMapSavedMapRefs.Copy();
                    break;
                case Map.Maps.Dungeon:
                    break;
                case Map.Maps.Combat:
                default:
                    throw new Ultima5ReduxException(
                        "Attempting to return to previous map after combat with an unsupported map type: " +
                        PreCombatMapSavedMapRefs.MapType);
            }
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
                smallMap.MapFloor + (bStairGoUp ? 1 : -1)), xy.Copy());
        }
    }
}