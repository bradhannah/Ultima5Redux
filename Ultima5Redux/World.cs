using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using Ultima5Redux.Dialogue;
using Ultima5Redux.Maps;
using Ultima5Redux.MapUnits;
using Ultima5Redux.MapUnits.CombatMapUnits;
using Ultima5Redux.MapUnits.NonPlayerCharacters;
using Ultima5Redux.MapUnits.NonPlayerCharacters.ExtendedNpc;
using Ultima5Redux.MapUnits.NonPlayerCharacters.ShoppeKeepers;
using Ultima5Redux.MapUnits.SeaFaringVessels;
using Ultima5Redux.MapUnits.TurnResults;
using Ultima5Redux.MapUnits.TurnResults.SpecificTurnResults;
using Ultima5Redux.PlayerCharacters;
using Ultima5Redux.PlayerCharacters.CombatItems;
using Ultima5Redux.PlayerCharacters.Inventory;
using Ultima5Redux.References;
using Ultima5Redux.References.Maps;
using Ultima5Redux.References.MapUnits.NonPlayerCharacters;
using Ultima5Redux.References.PlayerCharacters.Inventory;

namespace Ultima5Redux
{
    [SuppressMessage("ReSharper", "MemberCanBePrivate.Global")]
    [SuppressMessage("ReSharper", "UnusedMethodReturnValue.Global")]
    public class World
    {
        private enum SpecialSearchLocation
        {
            None,
            GlassSwords
        }

        public enum KlimbResult
        {
            Success,
            SuccessFell,
            CantKlimb,
            RequiresDirection
        }

        /// <summary>
        ///     Special things that can be looked at in the world that will require special consideration
        /// </summary>
        // ReSharper disable once UnusedMember.Global
        public enum SpecialLookCommand
        {
            None,
            Sign,
            GemCrystal
        }

        [SuppressMessage("ReSharper", "UnusedMember.Global")]
        public enum TryToAttackResult
        {
            Uninitialized,
            NothingToAttack,
            BrokenMirror,
            CombatMapEnemy,
            CombatMapNpc,
            NpcMurder,
            OnlyOnFoot,
            ShootAProjectile
        }

        private const int N_DEFAULT_ADVANCE_TIME = 2;
        public const byte N_DEFAULT_NUMBER_OF_TURNS_FOR_TORCH = 100;

        private readonly Dictionary<Potion.PotionColor, MagicReference.SpellWords> _potionColorToSpellMap =
            new()
            {
                { Potion.PotionColor.Blue, MagicReference.SpellWords.An_Zu },
                { Potion.PotionColor.Yellow, MagicReference.SpellWords.Mani },
                { Potion.PotionColor.Black, MagicReference.SpellWords.Sanct_Lor },
                { Potion.PotionColor.Red, MagicReference.SpellWords.An_Nox },
                { Potion.PotionColor.Green, MagicReference.SpellWords.Nox },
                { Potion.PotionColor.Orange, MagicReference.SpellWords.In_Zu },
                { Potion.PotionColor.White, MagicReference.SpellWords.Wis_An_Ylem },
                { Potion.PotionColor.Purple, MagicReference.SpellWords.Rel_Xen_Bet }
            };

        /// <summary>
        ///     The current conversation object
        /// </summary>
        public Conversation CurrentConversation { get; private set; }

        /// <summary>
        ///     Ultima 5 data files directory (static content)
        /// </summary>
        public string DataDirectory { get; }

        /// <summary>
        ///     Is the Avatar positioned to fall? When falling from multiple floors this will be activated
        /// </summary>
        // ReSharper disable once UnusedAutoPropertyAccessor.Global
        public bool IsPendingFall { get; private set; }

        public static bool MonsterAi => true;

        /// <summary>
        ///     Ultima 5 data and save files directory
        /// </summary>
        public string SaveGameDirectory { get; }

        /// <summary>
        ///     The current game state
        /// </summary>
        public GameState State { get; private set; }

        // ReSharper disable once UnusedMember.Local

        /// <summary>
        ///     Constructor
        /// </summary>
        /// <param name="bLegacySave"></param>
        /// <param name="saveGameDirectory">ultima 5 data and save game directory</param>
        /// <param name="dataDirectory"></param>
        /// <param name="bUseExtendedSprites"></param>
        /// <param name="bLoadedInitGam"></param>
        public World(bool bLegacySave, string saveGameDirectory, string dataDirectory = "",
            bool bUseExtendedSprites = false, bool bLoadedInitGam = false)
        {
            if (dataDirectory == "" && !bLegacySave)
                throw new Ultima5ReduxException("You cannot give an empty data directory with a non legacy save!");

            SaveGameDirectory = saveGameDirectory;
            DataDirectory = dataDirectory == "" ? SaveGameDirectory : dataDirectory;

            GameReferences.Initialize(DataDirectory);

            if (bLegacySave)
            {
                State = new GameState(bLoadedInitGam ? "" : SaveGameDirectory,
                    // OverworldMap,
                    // UnderworldMap, 
                    bUseExtendedSprites);
            }
            else
            {
                State = GameState.DeserializeFromFile(Path.Combine(SaveGameDirectory, FileConstants.NEW_SAVE_FILE));
            }
        }

        [SuppressMessage("ReSharper", "OutParameterValueIsAlwaysDiscarded.Local")]
        private static void CastSleep(TurnResults turnResults, PlayerCharacterRecord record, out bool bWasPutToSleep)
        {
            bWasPutToSleep = record.Stats.Sleep();
            turnResults.PushOutputToConsole(
                GameReferences.Instance.DataOvlRef.StringReferences.GetString(DataOvlReference.ExclaimStrings
                    .SLEPT_BANG_N),
                false);
        }

        /// <summary>
        ///     Gets a +/- 1 x/y adjustment based on the current position and given direction
        /// </summary>
        /// <param name="direction">direction to go</param>
        /// <param name="xAdjust">output X adjustment</param>
        /// <param name="yAdjust">output Y adjustment</param>
        private static void GetAdjustments(Point2D.Direction direction, out int xAdjust, out int yAdjust)
        {
            xAdjust = 0;
            yAdjust = 0;

            // if you are on a repeating map then you should assume that the adjust suc

            switch (direction)
            {
                case Point2D.Direction.Down:
                    yAdjust = 1;
                    break;
                case Point2D.Direction.Up:
                    yAdjust = -1;
                    break;
                case Point2D.Direction.Right:
                    xAdjust = 1;
                    break;
                case Point2D.Direction.Left:
                    xAdjust = -1;
                    break;
                case Point2D.Direction.None:
                    // do nothing, no adjustment
                    break;
                default:
                    throw new Ultima5ReduxException(
                        "Requested an adjustment but didn't provide a KeyCode that represents a direction.");
            }
        }

        /// <summary>
        ///     Gets the tile reference for a chair when pushed in a given direction
        /// </summary>
        /// <param name="chairDirection"></param>
        /// <returns></returns>
        /// <exception cref="Ultima5ReduxException"></exception>
        private static TileReference GetChairNewDirection(Point2D.Direction chairDirection)
        {
            switch (chairDirection)
            {
                case Point2D.Direction.Up:
                    return GameReferences.Instance.SpriteTileReferences.GetTileReference(TileReference.SpriteIndex
                        .ChairBackForward);
                case Point2D.Direction.Down:
                    return GameReferences.Instance.SpriteTileReferences.GetTileReference(TileReference.SpriteIndex
                        .ChairBackBack);
                case Point2D.Direction.Left:
                    return GameReferences.Instance.SpriteTileReferences.GetTileReference(
                        TileReference.SpriteIndex.ChairBackRight);
                case Point2D.Direction.Right:
                    return GameReferences.Instance.SpriteTileReferences.GetTileReference(TileReference.SpriteIndex
                        .ChairBackLeft);
                case Point2D.Direction.None:
                default:
                    throw new Ultima5ReduxException("Asked for a chair direction that I don't recognize");
            }
        }

        private static string GetKlimbOutput(string output = "")
        {
            if (output == "")
                return GameReferences.Instance.DataOvlRef.StringReferences.GetString(DataOvlReference.TravelStrings
                    .KLIMB);
            return GameReferences.Instance.DataOvlRef.StringReferences.GetString(DataOvlReference.TravelStrings
                       .KLIMB) +
                   output;
        }

        private static string GetMovementVerb(Avatar avatar, Point2D.Direction direction, bool bManualMovement)
        {
            switch (avatar.CurrentAvatarState)
            {
                case Avatar.AvatarState.Regular:
                    return GameReferences.Instance.DataOvlRef.StringReferences.GetDirectionString(direction);
                case Avatar.AvatarState.Carpet:
                    return GameReferences.Instance.DataOvlRef.StringReferences.GetString(DataOvlReference.WorldStrings
                               .FLY) +
                           GameReferences.Instance.DataOvlRef.StringReferences.GetDirectionString(direction);
                case Avatar.AvatarState.Horse:
                    return GameReferences.Instance.DataOvlRef.StringReferences.GetString(DataOvlReference.WorldStrings
                               .RIDE) +
                           GameReferences.Instance.DataOvlRef.StringReferences.GetDirectionString(direction);
                case Avatar.AvatarState.Frigate:
                    if (!avatar.AreSailsHoisted)
                        return
                            GameReferences.Instance.DataOvlRef.StringReferences.GetDirectionString(direction) +
                            GameReferences.Instance.DataOvlRef.StringReferences.GetString(DataOvlReference.WorldStrings
                                .ROWING);

                    if (bManualMovement)
                    {
                        return GameReferences.Instance.DataOvlRef.StringReferences.GetString(
                                   DataOvlReference.WorldStrings
                                       .HEAD) +
                               GameReferences.Instance.DataOvlRef.StringReferences.GetDirectionString(direction);
                    }

                    return GameReferences.Instance.DataOvlRef.StringReferences.GetDirectionString(direction);

                case Avatar.AvatarState.Skiff:
                    return GameReferences.Instance.DataOvlRef.StringReferences.GetString(DataOvlReference.WorldStrings
                               .ROW) +
                           GameReferences.Instance.DataOvlRef.StringReferences.GetDirectionString(direction);
                case Avatar.AvatarState.Hidden:
                default:
                    throw new ArgumentOutOfRangeException(nameof(avatar.CurrentAvatarState), avatar.CurrentAvatarState,
                        null);
            }
        }

        private static string GetOnFootResponse() =>
            GameReferences.Instance.DataOvlRef.StringReferences
                .GetString(DataOvlReference.KeypressCommandsStrings.BOARD).Trim() + "\n" + GameReferences.Instance
                .DataOvlRef
                .StringReferences.GetString(DataOvlReference.KeypressCommandsStrings.ON_FOOT).Trim();

        private static SpecialSearchLocation GetSpecialSearchLocation(
            SmallMapReferences.SingleMapReference.Location location,
            int nFloor, Point2D position)
        {
            if (location == SmallMapReferences.SingleMapReference.Location.Britannia_Underworld && nFloor == 0
                                                                             && position.X == 64 && position.Y == 80)
                return SpecialSearchLocation.GlassSwords;

            return SpecialSearchLocation.None;
        }

        private static string GetThouHolds(string shard) =>
            GameReferences.Instance.DataOvlRef.StringReferences.GetString(DataOvlReference.ShadowlordStrings
                .GEM_SHARD_THOU_HOLD_EVIL_SHARD) + "\n" + shard;

        private static TurnResult.TurnResultType GetTurnResultMovedByAvatarState(Avatar avatar, bool bManualMovement)
        {
            switch (avatar.CurrentAvatarState)
            {
                case Avatar.AvatarState.Regular:
                    return TurnResult.TurnResultType.ActionMoveRegular;
                case Avatar.AvatarState.Carpet:
                    return TurnResult.TurnResultType.ActionMoveCarpet;
                case Avatar.AvatarState.Horse:
                    return TurnResult.TurnResultType.ActionMoveHorse;
                case Avatar.AvatarState.Frigate:
                    if (avatar.AreSailsHoisted)
                    {
                        return bManualMovement
                            ? TurnResult.TurnResultType.ActionMoveFrigateWindSailsChangeDirection
                            : TurnResult.TurnResultType.ActionMoveFrigateWindSails;
                    }

                    return TurnResult.TurnResultType.ActionMoveFrigateRowing;
                case Avatar.AvatarState.Skiff:
                    return TurnResult.TurnResultType.ActionMoveSkiffRowing;
                case Avatar.AvatarState.Hidden:
                default:
                    throw new ArgumentOutOfRangeException(nameof(avatar),
                        @"Tried to move in an unknown avatar boarding state");
            }
        }


        /// <summary>
        ///     Checks if you can fire
        /// </summary>
        /// <returns></returns>
        private bool CanFireInPlace(TurnResults turnResults)
        {
            if (turnResults == null) throw new ArgumentNullException(nameof(turnResults));
            switch (State.TheVirtualMap.CurrentMap)
            {
                case LargeMap { IsAvatarInFrigate: true }:
                    return true;
                case LargeMap:
                    turnResults.PushOutputToConsole(
                        GameReferences.Instance.DataOvlRef.StringReferences.GetString(DataOvlReference.KeypressCommandsStrings
                            .FIRE) +
                        GameReferences.Instance.DataOvlRef.StringReferences.GetString(DataOvlReference.KeypressCommandsStrings
                            .D_WHAT));
                    turnResults.PushTurnResult(new BasicResult(TurnResult.TurnResultType.ActionFireWhat));
                    return false;
                case CombatMap:
                    turnResults.PushOutputToConsole(
                        GameReferences.Instance.DataOvlRef.StringReferences.GetString(DataOvlReference.KeypressCommandsStrings
                            .FIRE) +
                        GameReferences.Instance.DataOvlRef.StringReferences.GetString(DataOvlReference.ExclaimStrings
                            .DASH_NOT_HERE_BANG_N));
                    turnResults.PushTurnResult(new BasicResult(TurnResult.TurnResultType.ActionFireNotHere));
                    return false;
            }

            List<TileReference> cannonReferences = new()
            {
                GameReferences.Instance.SpriteTileReferences.GetTileReferenceByName("CannonUp"),
                GameReferences.Instance.SpriteTileReferences.GetTileReferenceByName("CannonLeft"),
                GameReferences.Instance.SpriteTileReferences.GetTileReferenceByName("CannonRight"),
                GameReferences.Instance.SpriteTileReferences.GetTileReferenceByName("CannonDown")
            };
            // small map
            // if a cannon is any of the four directions
            // 180 - 183
            if (State.TheVirtualMap.CurrentMap.AreAnyTilesWithinFourDirections(
                    State.TheVirtualMap.CurrentMap.CurrentPosition.XY,
                    cannonReferences)) return true;

            turnResults.PushOutputToConsole(
                GameReferences.Instance.DataOvlRef.StringReferences.GetString(DataOvlReference.KeypressCommandsStrings
                    .FIRE) +
                GameReferences.Instance.DataOvlRef.StringReferences.GetString(DataOvlReference.KeypressCommandsStrings
                    .D_WHAT));
            turnResults.PushTurnResult(new BasicResult(TurnResult.TurnResultType.ActionFireWhat));
            return false;
        }


        private bool IsLeavingMap(Point2D xyProposedPosition) =>
            xyProposedPosition.IsOutOfRange(State.TheVirtualMap.CurrentMap.NumOfXTiles - 1,
                State.TheVirtualMap.CurrentMap.NumOfYTiles - 1);

        // ReSharper disable once SuggestBaseTypeForParameter
        private void MurderNpc(NonPlayerCharacter npc, TurnResults turnResults)
        {
            if (State.TheVirtualMap.CurrentMap is not SmallMap smallMap)
                throw new Ultima5ReduxException("Should be small map");

            turnResults.PushTurnResult(new BasicResult(TurnResult.TurnResultType.ActionAttackMurder));
            npc.NpcState.IsDead = true;
            State.ChangeKarma(-10, turnResults);
            smallMap.IsWantedManByThePoPo = true;
        }

        /// <summary>
        ///     Processes any damage effects as you advance time, this can include getting
        /// </summary>
        /// <returns></returns>
        private void ProcessDamageOnAdvanceTimeNonCombat(TurnResults turnResults)
        {
            if (turnResults == null) throw new ArgumentNullException(nameof(turnResults));
            if (State.TheVirtualMap.CurrentMap is not RegularMap regularMap)
                throw new Ultima5ReduxException("ProcessDamageOnAdvanceTimeNonCombat called not on regular map");

            Avatar.AvatarState currentAvatarState =
                regularMap.GetAvatarMapUnit().CurrentAvatarState;
            TileReference currentTileReference = regularMap.GetTileReferenceOnCurrentTile();

            // swamp - we poison them, but the actual damage occurs further down in case they were already poisoned
            if (currentAvatarState != Avatar.AvatarState.Carpet && currentTileReference.Index == 4)
            {
                bool bWasPoisoned = State.CharacterRecords.SteppedOnSwamp();
                if (bWasPoisoned)
                {
                    turnResults.PushOutputToConsole(GameReferences.Instance.DataOvlRef.StringReferences.GetString(
                        DataOvlReference.ExclaimStrings.POISONED_BANG_N), false);
                }
            }
            // if on lava
            else if ((TileReference.SpriteIndex)currentTileReference.Index is
                     TileReference.SpriteIndex.Lava or
                     TileReference.SpriteIndex.Fireplace)
            {
                State.CharacterRecords.SteppedOnLava(turnResults);
                turnResults.PushOutputToConsole("Burning!", false);
            }

            // if already poisoned
            State.CharacterRecords.ProcessTurn(turnResults);
        }

        private List<VirtualMap.AggressiveMapUnitInfo> ProcessOpenDoor(Point2D xy, ref bool bWasSuccessful,
            TurnResults turnResults, TileReference tileReference)
        {
            bool bIsDoorMagical = TileReferences.IsDoorMagical(tileReference.Index);
            bool bIsDoorLocked = TileReferences.IsDoorLocked(tileReference.Index);

            if (bIsDoorMagical || bIsDoorLocked)
            {
                turnResults.PushOutputToConsole(GameReferences.Instance.DataOvlRef.StringReferences.GetString(
                    DataOvlReference.OpeningThingsStrings
                        .LOCKED_N), false);
                turnResults.PushTurnResult(new BasicResult(TurnResult.TurnResultType.ActionOpenDoorLocked));
                return AdvanceTime(N_DEFAULT_ADVANCE_TIME, turnResults);
            }

            State.TheVirtualMap.CurrentMap.SetOverridingTileReference(
                GameReferences.Instance.SpriteTileReferences.GetTileReferenceByName("BrickFloor"), xy);

            Map currentMap = State.TheVirtualMap.CurrentMap;
            currentMap.SetOpenDoor(xy);

            bWasSuccessful = true;
            turnResults.PushOutputToConsole(
                GameReferences.Instance.DataOvlRef.StringReferences.GetString(DataOvlReference.OpeningThingsStrings
                    .OPENED),
                false);
            turnResults.PushTurnResult(new BasicResult(TurnResult.TurnResultType.ActionOpenDoorOpened));

            return AdvanceTime(N_DEFAULT_ADVANCE_TIME, turnResults);
        }

        private List<VirtualMap.AggressiveMapUnitInfo> SearchAndFindGlassSword(Point2D xy, out bool bWasSuccessful,
            TurnResults turnResults)
        {
            var newGlassSword =
                new Weapon(
                    GameReferences.Instance.CombatItemRefs.GetWeaponReferenceFromEquipment(DataOvlReference.Equipment
                        .GlassSword), 1);
            List<InventoryItem> invItems = new() { newGlassSword };
            MapUnitPosition glassSwordMapUnitPosition = new()
            {
                Floor = State.TheVirtualMap.CurrentMap.CurrentSingleMapReference.Floor,
                XY = xy
            };
            DiscoverableLoot discoverableLoot = new(
                State.TheVirtualMap.CurrentMap.CurrentSingleMapReference.MapLocation,
                glassSwordMapUnitPosition, invItems);

            // I plant it, only so it can be then found immediately again
            State.TheVirtualMap.CurrentMap.CurrentMapUnits.AddMapUnit(discoverableLoot);

            bool bFoundGlassSword = State.TheVirtualMap.CurrentMap.SearchNonAttackingMapUnit(xy, turnResults,
                State.CharacterRecords.AvatarRecord, State.CharacterRecords);
            if (!bFoundGlassSword)
                throw new Ultima5ReduxException("I just created a glass sword and then couldn't find it!?");
            bWasSuccessful = true;
            return AdvanceTime(N_DEFAULT_ADVANCE_TIME, turnResults);
        }

        /// <summary>
        ///     Use a magic carpet from your inventory
        /// </summary>
        /// <param name="turnResults"></param>
        /// <param name="bWasUsed">was the magic carpet used?</param>
        /// <returns>string to print and show user</returns>
        private void UseMagicCarpet(TurnResults turnResults, out bool bWasUsed)
        {
            if (State.TheVirtualMap.CurrentMap is CombatMap)
            {
                bWasUsed = false;
                turnResults.PushOutputToConsole(GameReferences.Instance.DataOvlRef.StringReferences.GetString(
                    DataOvlReference.ExclaimStrings
                        .NOT_HERE_BANG), false);
                return;
            }

            if (State.TheVirtualMap.CurrentMap is not RegularMap regularMap)
                throw new Ultima5ReduxException("UseMagicCarpet called not on regular map");

            bWasUsed = true;
            Debug.Assert(State.PlayerInventory.SpecializedItems.Items[SpecialItem.SpecificItemType.Carpet]
                .HasOneOrMore);

            if (regularMap.GetAvatarMapUnit().IsAvatarOnBoardedThing)
            {
                bWasUsed = false;
                turnResults.PushOutputToConsole(GameReferences.Instance.DataOvlRef.StringReferences.GetString(
                    DataOvlReference.WearUseItemStrings
                        .ONLY_ON_FOOT), false);
                return;
            }

            State.PlayerInventory.SpecializedItems.Items[SpecialItem.SpecificItemType.Carpet].Quantity--;
            MagicCarpet carpet = regularMap.CreateMagicCarpet(
                State.TheVirtualMap.CurrentMap.CurrentPosition.XY,
                regularMap.GetAvatarMapUnit().Direction,
                out int _);
            regularMap.BoardAndCleanFromWorld(carpet);
            turnResults.PushOutputToConsole(GameReferences.Instance.DataOvlRef.StringReferences.GetString(
                DataOvlReference.WearUseItemStrings
                    .CARPET_BANG), false);
        }

        public void AdvanceClockNoComputation(int nMinutes)
        {
            State.TheTimeOfDay.AdvanceClock(nMinutes);
        }

        /// <summary>
        ///     Advances time and takes care of all day, month, year calculations
        /// </summary>
        /// <param name="nMinutes">Number of minutes to advance (maximum of 9*60)</param>
        /// <param name="turnResults"></param>
        /// <param name="bNoAggression"></param>
        public List<VirtualMap.AggressiveMapUnitInfo> AdvanceTime(int nMinutes, TurnResults turnResults,
            bool bNoAggression = false)
        {
            if (turnResults == null) throw new ArgumentNullException(nameof(turnResults));

            int nCurrentHour = State.TheTimeOfDay.Month;
            Dictionary<MapUnit, VirtualMap.AggressiveMapUnitInfo> aggressiveMapUnitInfos = new();

            AdvanceClockNoComputation(nMinutes);

            if (State.TheVirtualMap.CurrentMap is CombatMap)
            {
                turnResults.PushTurnResult(new BasicResult(TurnResult.TurnResultType.Ignore));
                ProcessDamageOnAdvanceTimeInCombat(turnResults);
            }
            else
            {
                if (State.TheVirtualMap.CurrentMap is not RegularMap regularMap)
                    throw new Ultima5ReduxException("Tried to advance time on non RegularMap");

                ProcessDamageOnAdvanceTimeNonCombat(turnResults);

                aggressiveMapUnitInfos = regularMap.GetNonCombatMapAggressiveMapUnitInfo(turnResults);
                regularMap.MoveNonCombatMapMapUnitsToNextMove(aggressiveMapUnitInfos);
                VirtualMap.AggressiveMapUnitInfo aggressiveMapUnitInfo = null;
                if (bNoAggression && State.TheVirtualMap.CurrentMap is LargeMap largeMap)
                {
                    largeMap.GenerateAndCleanupEnemies(State.TheVirtualMap.OneInXOddsOfNewMonster,
                        State.TheTimeOfDay.MinutesSinceBeginning);
                }
                else if (State.TheVirtualMap.CurrentMap is RegularMap theRegularMap)
                {
                    // this routine will check to see if a combat map load occured - if so then we only focus on it
                    // and ignore all other work
                    if (MonsterAi)
                    {
                        regularMap.ProcessNonCombatMapAggressiveMapUnitAttacks(State.CharacterRecords,
                            aggressiveMapUnitInfos, out aggressiveMapUnitInfo, turnResults);
                    }

                    // if there is an individual aggressive map unit, then we know that we are going to load a combat map
                    if (aggressiveMapUnitInfo != null)
                    {
                        Debug.Assert(aggressiveMapUnitInfo.CombatMapReference != null);
                        // issue here is I need to be able to tell the caller to load a combat map

                        MapUnitPosition avatarPosition = regularMap.CurrentAvatarPosition;
                        TileReference currentTile = State.TheVirtualMap.CurrentMap
                            .GetOriginalTileReference(avatarPosition.XY);

                        if (!theRegularMap.IsAvatarRidingSomething &&
                            aggressiveMapUnitInfo.AttackingMapUnit is Enemy enemy &&
                            enemy.EnemyReference.IsWaterEnemy && !currentTile.IsWalking_Passable &&
                            currentTile.IsWaterTile)
                        {
                            // if the avatar is on a water tile & getting attacked by a water monster
                            // and they are not riding anything, then we friendly indicate they are debugging
                            turnResults.PushOutputToConsole("It appears you are debugging - " +
                                                            enemy.FriendlyName + " tried to attack you.");
                        }
                        else
                        {
                            // let's delete the map unit from the map too since they disappear regardless of result
                            regularMap.ClearMapUnit(aggressiveMapUnitInfo.AttackingMapUnit);

                            State.TheVirtualMap.LoadCombatMapWithCalculation(aggressiveMapUnitInfo.CombatMapReference,
                                State.CharacterRecords, aggressiveMapUnitInfo.AttackingMapUnit);

                            turnResults.PushTurnResult(new BasicResult(TurnResult.TurnResultType.CombatMapLoaded));
                        }
                    }
                    else if (State.TheVirtualMap.CurrentMap is LargeMap theLargeMap)
                    {
                        theLargeMap.GenerateAndCleanupEnemies(State.TheVirtualMap.OneInXOddsOfNewMonster,
                            State.TheTimeOfDay.MinutesSinceBeginning);
                    }
                }
            }

            // if a whole month has advanced then we go and add one month to the "staying at the inn" count
            if (nCurrentHour < State.TheTimeOfDay.Month) State.CharacterRecords.IncrementStayingAtInnCounters();
            if (State.TurnsToExtinguish > 0) State.TurnsToExtinguish--;

            State.TurnsSinceStart++;
            return aggressiveMapUnitInfos.Values.ToList();
        }

        /// <summary>
        ///     Begins the conversation with a particular NPC
        /// </summary>
        /// <param name="npcState">the NPC to have a conversation with</param>
        /// <param name="enqueuedScriptItem">a handler to be called when script items are enqueued</param>
        /// <param name="customDialogueId">leave "" if you don't want a custom dialogue id</param>
        /// <returns>A conversation object to be used to follow along with the conversation</returns>
        [SuppressMessage("ReSharper", "UnusedMember.Global")]
        public Conversation CreateConversationAndBegin(NonPlayerCharacterState npcState,
            Conversation.EnqueuedScriptItem enqueuedScriptItem, string customDialogueId = "")
        {
            CurrentConversation = customDialogueId == ""
                ? new Conversation(State, npcState)
                : new Conversation(State, npcState, customDialogueId);

            CurrentConversation.EnqueuedScriptItemCallback += enqueuedScriptItem;

            AdvanceClockNoComputation(N_DEFAULT_ADVANCE_TIME);

            return CurrentConversation;
        }


        /// <summary>
        ///     Gets the angle of the 360 rotation of all moons where Sun is 0degrees (straight up) at 12pm Noon
        /// </summary>
        /// <returns>0-359 degrees</returns>
        // ReSharper disable once UnusedMember.Global
        public float GetMoonAngle() => MoonPhaseReferences.GetMoonAngle(State.TheTimeOfDay);

        /// <summary>
        ///     Gets the teleport location of the moongate the avatar is presently on
        ///     Avatar MUST be on a moongate teleport location
        /// </summary>
        /// <returns>the coordinates</returns>
        [SuppressMessage("ReSharper", "UnusedMember.Global")]
        public Point3D GetMoongateTeleportLocation()
        {
            Debug.Assert(State.TheVirtualMap.CurrentMap is LargeMap);

            return State.TheMoongates.GetMoongatePosition(
                (int)GameReferences.Instance.MoonPhaseRefs.GetMoonGateMoonPhase(State.TheTimeOfDay));
        }

        /// <summary>
        ///     Determines if the current tile the Avatar is on, is an ACTIVE moongate
        /// </summary>
        /// <returns>true if the Avatar is on an active moongate</returns>
        [SuppressMessage("ReSharper", "UnusedMember.Global")]
        public bool IsAvatarOnActiveMoongate()
        {
            if (State.TheVirtualMap.CurrentMap is not LargeMap || State.TheTimeOfDay.IsDayLight) return false;

            return State.TheMoongates.IsMoonstoneBuried(State.TheVirtualMap.GetCurrent3DPosition());
        }

        public void ProcessDamageOnAdvanceTimeInCombat(TurnResults turnResults)
        {
            if (State.TheVirtualMap.CurrentMap is not CombatMap combatMap)
                throw new Ultima5ReduxException("Should be combat map");

            CombatPlayer combatPlayer = combatMap.CurrentCombatPlayer;

            combatPlayer?.Record.ProcessPlayerTurn(turnResults);
        }

        /// <summary>
        ///     Reloads the game as if it was saved to disk and loaded fresh.
        ///     Generally only useful when testing the game is handling save states correct.
        /// </summary>
        [SuppressMessage("ReSharper", "UnusedMember.Global")]
        public void ReLoadFromJson()
        {
            string stateJsonOrig = State.Serialize();
            GameState newState = GameState.Deserialize(stateJsonOrig);
            State = newState;
        }

        // ReSharper disable once UnusedMember.Global
        // ReSharper disable once UnusedParameter.Global
        public void SetAggressiveGuards(bool bAggressiveGuards)
        {
            // empty because it's not implemented yet
        }

        /// <summary>
        ///     Called when the avatar is attempting to attack a particular target
        /// </summary>
        /// <param name="attackTargetPosition">where is the target</param>
        /// <param name="mapUnit">the mapunit (if any) that is the target</param>
        /// <param name="singleCombatMapReference">the combat map reference (if any) that should be loaded</param>
        /// <param name="tryToAttackResult"></param>
        /// <param name="turnResults"></param>
        /// <returns>the final decision on how to handle the attack</returns>
        /// <exception cref="Ultima5ReduxException"></exception>
        [SuppressMessage("ReSharper", "UnusedMember.Global")]
        public IEnumerable<VirtualMap.AggressiveMapUnitInfo> TryToAttackNonCombatMap(Point2D attackTargetPosition,
            out MapUnit mapUnit, out SingleCombatMapReference singleCombatMapReference,
            out TryToAttackResult tryToAttackResult, TurnResults turnResults)
        {
            if (State.TheVirtualMap.CurrentMap is not RegularMap regularMap)
                throw new Ultima5ReduxException("Called TryToAttackNonCombatMap on non RegularMap");

            singleCombatMapReference = null;
            mapUnit = null;

            TileReference tileReference = State.TheVirtualMap.CurrentMap.GetTileReference(attackTargetPosition);

            // if you are attacking an unbroken mirror - then we break it and override the tile
            if (TileReferences.IsUnbrokenMirror(tileReference.Index))
            {
                turnResults.PushOutputToConsole(
                    GameReferences.Instance.DataOvlRef.StringReferences.GetString(DataOvlReference.TravelStrings
                        .BROKEN),
                    false);
                TileReference brokenMirrorTileReference =
                    GameReferences.Instance.SpriteTileReferences.GetTileReferenceByName("MirrorBroken");
                regularMap.SetOverridingTileReference(brokenMirrorTileReference,
                    attackTargetPosition);

                tryToAttackResult = TryToAttackResult.BrokenMirror;
                turnResults.PushTurnResult(new BasicResult(TurnResult.TurnResultType.ActionAttackBrokeMirror));
                return AdvanceTime(N_DEFAULT_ADVANCE_TIME, turnResults);
            }

            mapUnit = State.TheVirtualMap.CurrentMap.GetTopVisibleMapUnit(attackTargetPosition, true);

            if (mapUnit is not { IsAttackable: true })
            {
                turnResults.PushOutputToConsole(GameReferences.Instance.DataOvlRef.StringReferences.GetString(
                    DataOvlReference.TravelStrings.NOTHING_TO_ATTACK), false);
                tryToAttackResult = TryToAttackResult.NothingToAttack;
                turnResults.PushTurnResult(new BasicResult(TurnResult.TurnResultType.ActionAttackNothingToAttack));
                return AdvanceTime(N_DEFAULT_ADVANCE_TIME, turnResults);
            }

            // we know there is a mapunit to attack at this point
            turnResults.PushOutputToConsole(
                GameReferences.Instance.DataOvlRef.StringReferences.GetString(DataOvlReference.TravelStrings.ATTACK) +
                mapUnit.FriendlyName, false);

            Avatar avatar = regularMap.GetAvatarMapUnit();

            // we get the combat map reference, if any - it also tells us if there should be a ranged attack in the overworld
            // instead of a combat map
            singleCombatMapReference =
                regularMap.GetCombatMapReferenceForAvatarAttacking(avatar.MapUnitPosition.XY,
                    attackTargetPosition, SingleCombatMapReference.Territory.Britannia);

            bool bIsMurderable = TileReferences.IsHeadOfBed(tileReference.Index) ||
                                 TileReferences.IsStocks(tileReference.Index) ||
                                 TileReferences.IsManacles(tileReference.Index);

            // if there is a mapunit - BUT - no 
            ////// NOTE - this doesn't make sense for the Avatar to attack like this
            if (singleCombatMapReference == null && !bIsMurderable)
            {
                // we were not able to attack, likely on a carpet or skiff and on the water
                // but may be other edge cases
                turnResults.PushOutputToConsole(
                    GameReferences.Instance.DataOvlRef.StringReferences.GetString(DataOvlReference.KeypressCommandsStrings
                        .ON_FOOT), false);
                tryToAttackResult = TryToAttackResult.OnlyOnFoot;

                turnResults.PushTurnResult(new BasicResult(TurnResult.TurnResultType.ActionAttackOnlyOnFoot));

                return AdvanceTime(N_DEFAULT_ADVANCE_TIME, turnResults);
            }

            tryToAttackResult = TryToAttackResult.Uninitialized;

            switch (mapUnit)
            {
                case Enemy:
                    State.TheVirtualMap.CurrentMap.ClearMapUnit(mapUnit);
                    tryToAttackResult = TryToAttackResult.CombatMapEnemy;
                    turnResults.PushTurnResult(new BasicResult(TurnResult.TurnResultType.ActionAttackCombatMapEnemy));
                    break;
                case NonPlayerCharacter npc:
                    // if they are in bed or in the stocks then it's instadeath and you are a bad person!
                    if (bIsMurderable)
                    {
                        turnResults.PushOutputToConsole(
                            GameReferences.Instance.DataOvlRef.StringReferences.GetString(
                                DataOvlReference.TravelStrings.MURDERED), false);
                        tryToAttackResult = TryToAttackResult.NpcMurder;
                        MurderNpc(npc, turnResults);
                        break;
                    }

                    //npc.NPCState.IsDead = true;
                    //// NEED TO FIGURE OUT WHAT IS AND ISN'T MURDER
                    MurderNpc(npc, turnResults);

                    turnResults.PushTurnResult(new BasicResult(TurnResult.TurnResultType.ActionAttackCombatMapNpc));
                    tryToAttackResult = TryToAttackResult.CombatMapNpc;

                    break;
            }

            if (tryToAttackResult is TryToAttackResult.CombatMapEnemy or TryToAttackResult.CombatMapNpc)
            {
                turnResults.PushOutputToConsole(mapUnit.FriendlyName + "\n" +
                                                GameReferences.Instance.DataOvlRef.StringReferences.GetString(
                                                    DataOvlReference.AdditionalStrings
                                                        .STARS_CONFLICT_STARS_N_N), false);
            }

            return AdvanceTime(N_DEFAULT_ADVANCE_TIME, turnResults);
        }

        /// <summary>
        ///     Board something such as a frigate, skiff, horse or carpet
        /// </summary>
        /// <param name="bWasSuccessful"></param>
        /// <param name="turnResults"></param>
        /// <returns></returns>
        [SuppressMessage("ReSharper", "UnusedMember.Global")]
        public List<VirtualMap.AggressiveMapUnitInfo> TryToBoard(out bool bWasSuccessful, TurnResults turnResults)
        {
            if (turnResults == null) throw new ArgumentNullException(nameof(turnResults));

            //MapUnit currentAvatarTileRef = State.TheVirtualMap.GetMapUnitOnCurrentTile();
            bWasSuccessful = true;

            if (!(State.TheVirtualMap.CurrentMap is RegularMap regularMap
                  && (regularMap.GetMapUnitOnCurrentTile()?.KeyTileReference.IsBoardable ?? false)))
            {
                bWasSuccessful = false;
                // can't board it
                turnResults.PushOutputToConsole(GameReferences.Instance.DataOvlRef.StringReferences
                                                    .GetString(DataOvlReference.KeypressCommandsStrings.BOARD)
                                                    .Trim() + " " +
                                                GameReferences.Instance.DataOvlRef.StringReferences.GetString(
                                                    DataOvlReference.TravelStrings.WHAT));
                turnResults.PushTurnResult(new BasicResult(TurnResult.TurnResultType.ActionBoardWhat));
                return AdvanceTime(N_DEFAULT_ADVANCE_TIME, turnResults);
            }

            bool bAvatarIsBoarded = regularMap.IsAvatarRidingSomething;
            MapUnit boardableMapUnit = regularMap.GetMapUnitOnCurrentTile();
            if (boardableMapUnit is null)
                throw new Ultima5ReduxException(
                    "Tried to board something but the tile appears to be boardable without a MapUnit");

            // at this point we are certain that the current tile is boardable AND the we know if the avatar has already
            // boarded something

            string retStr = GameReferences.Instance.DataOvlRef.StringReferences
                .GetString(DataOvlReference.KeypressCommandsStrings.BOARD).Trim() + " " + boardableMapUnit.BoardXitName;

            switch (boardableMapUnit)
            {
                case MagicCarpet when bAvatarIsBoarded:
                case Horse when bAvatarIsBoarded:
                case Skiff when bAvatarIsBoarded:
                    bWasSuccessful = false;
                    turnResults.PushOutputToConsole(GetOnFootResponse(), false);
                    turnResults.PushTurnResult(new BasicResult(TurnResult.TurnResultType.ActionBoardNoOnFoot));
                    return AdvanceTime(N_DEFAULT_ADVANCE_TIME, turnResults);
                case MagicCarpet:
                    regularMap.BoardAndCleanFromWorld(boardableMapUnit);
                    turnResults.PushTurnResult(new BasicResult(TurnResult.TurnResultType.ActionBoardCarpet));
                    break;
                case Horse:
                    // delete or deactivate the horse we just mounted
                    regularMap.BoardAndCleanFromWorld(boardableMapUnit);
                    turnResults.PushTurnResult(new BasicResult(TurnResult.TurnResultType.ActionBoardHorse));
                    break;
                case Skiff:
                    regularMap.BoardAndCleanFromWorld(boardableMapUnit);
                    turnResults.PushTurnResult(new BasicResult(TurnResult.TurnResultType.ActionBoardSkiff));
                    break;
                case Frigate boardableFrigate:
                {
                    if (bAvatarIsBoarded)
                    {
                        if (regularMap.IsAvatarRidingHorse)
                        {
                            bWasSuccessful = false;
                            turnResults.PushOutputToConsole(GetOnFootResponse());
                            turnResults.PushTurnResult(new BasicResult(TurnResult.TurnResultType.ActionBoardNoOnFoot));
                            return AdvanceTime(N_DEFAULT_ADVANCE_TIME, turnResults);
                        }

                        if (regularMap.IsAvatarRidingCarpet)
                            // we tuck the carpet away
                            State.PlayerInventory.SpecializedItems.Items[SpecialItem.SpecificItemType.Carpet]
                                .Quantity++;
                        // add a skiff the the frigate
                        if (regularMap.IsAvatarInSkiff) boardableFrigate.SkiffsAboard++;
                    }

                    if (boardableFrigate.SkiffsAboard == 0)
                        retStr += GameReferences.Instance.DataOvlRef.StringReferences
                            .GetString(DataOvlReference.SleepTransportStrings.M_WARNING_NO_SKIFFS_N).TrimEnd();
                    regularMap.BoardAndCleanFromWorld(boardableFrigate);
                    turnResults.PushTurnResult(new BasicResult(TurnResult.TurnResultType.ActionBoardFrigate));
                    if (State.PlayerInventory.SpecializedItems.Items[SpecialItem.SpecificItemType.HMSCape].Quantity > 0)
                    {
                        retStr += "\n" + GameReferences.Instance.DataOvlRef.StringReferences
                            .GetString(DataOvlReference.WearUseItemStrings.SHIP_RIGGED_DOUBLE_SPEED).TrimEnd();
                        turnResults.PushTurnResult(new BasicResult(TurnResult.TurnResultType.ActionUseHmsCapePlans));
                    }

                    break;
                }
            }

            turnResults.PushOutputToConsole(retStr);
            return AdvanceTime(N_DEFAULT_ADVANCE_TIME, turnResults);
        }

        /// <summary>
        ///     Attempt to enter a building at a coordinate
        ///     Will load new map if successful
        /// </summary>
        /// <param name="xy">position of building</param>
        /// <param name="bWasSuccessful">true if successfully entered</param>
        /// <param name="turnResults"></param>
        /// <returns>output string</returns>
        [SuppressMessage("ReSharper", "UnusedMember.Global")]
        public List<VirtualMap.AggressiveMapUnitInfo> TryToEnterBuilding(Point2D xy, out bool bWasSuccessful,
            TurnResults turnResults)
        {
            if (turnResults == null) throw new ArgumentNullException(nameof(turnResults));

            bool isOnBuilding = GameReferences.Instance.LargeMapRef.IsMapXyEnterable(xy);

            if (!isOnBuilding)
            {
                turnResults.PushOutputToConsole(
                    GameReferences.Instance.DataOvlRef.StringReferences.GetString(DataOvlReference.WorldStrings
                        .ENTER_SPACE) +
                    GameReferences.Instance.DataOvlRef.StringReferences.GetString(DataOvlReference.WorldStrings.WHAT),
                    false);
                turnResults.PushTurnResult(new BasicResult(TurnResult.TurnResultType.ActionEnterWhat));
                bWasSuccessful = false;
                return AdvanceTime(N_DEFAULT_ADVANCE_TIME, turnResults);
            }

            SmallMapReferences.SingleMapReference.Location location =
                GameReferences.Instance.LargeMapRef.GetLocationByMapXy(xy);
            SmallMapReferences.SingleMapReference singleMap =
                GameReferences.Instance.SmallMapRef.GetSingleMapByLocation(location, 0);

            if (singleMap.MapType == Map.Maps.Dungeon)
            {
                string retStr =
                    GameReferences.Instance.DataOvlRef.StringReferences.GetString(DataOvlReference.WorldStrings
                        .ENTER_SPACE) +
                    GameReferences.Instance.SmallMapRef.GetLocationTypeStr(location) + "\n" +
                    GameReferences.Instance.SmallMapRef.GetLocationName(location) +
                    "\nUnable to enter the dungeons at this time!";
                bWasSuccessful = false;
                turnResults.PushOutputToConsole(retStr, false);
                turnResults.PushTurnResult(new BasicResult(TurnResult.TurnResultType.ActionEnterDungeon));
                return new List<VirtualMap.AggressiveMapUnitInfo>();
            }

            State.TheVirtualMap.LoadSmallMap(singleMap);
            // set us to the front of the building
            State.TheVirtualMap.CurrentMap.CurrentPosition.XY = SmallMapReferences.GetStartingXyByLocation();

            turnResults.PushOutputToConsole(
                GameReferences.Instance.DataOvlRef.StringReferences.GetString(DataOvlReference.WorldStrings
                    .ENTER_SPACE) +
                GameReferences.Instance.SmallMapRef.GetLocationTypeStr(location) + "\n" +
                GameReferences.Instance.SmallMapRef.GetLocationName(location), false);
            turnResults.PushTurnResult(new BasicResult(TurnResult.TurnResultType.ActionEnterTowne));
            bWasSuccessful = true;

            AdvanceClockNoComputation(N_DEFAULT_ADVANCE_TIME);
            return new List<VirtualMap.AggressiveMapUnitInfo>();
        }

        /// <summary>
        ///     Try to fire from where ever you are
        /// </summary>
        /// <param name="direction"></param>
        /// <param name="turnResults"></param>
        /// <param name="cannonBallDestination"></param>
        [SuppressMessage("ReSharper", "UnusedMember.Global")]
        public List<VirtualMap.AggressiveMapUnitInfo> TryToFire(Point2D.Direction direction,
            TurnResults turnResults, out Point2D cannonBallDestination)
        {
            bool bCanFire = CanFireInPlace(turnResults);

            if (!bCanFire)
            {
                cannonBallDestination = null;
                return AdvanceTime(N_DEFAULT_ADVANCE_TIME, turnResults);
            }

            if (State.TheVirtualMap.CurrentMap is not RegularMap regularMap)
                throw new Ultima5ReduxException("TryToFire called on non RegularMap");

            turnResults.PushTurnResult(new BasicResult(TurnResult.TurnResultType.ActionFireCannon));

            Avatar avatar = regularMap.GetAvatarMapUnit();
            cannonBallDestination = null;

            // TODO: need small map code for cannons like in LBs castle

            // if overworld
            if (State.TheVirtualMap.CurrentMap is not LargeMap largeMap) return null;

            // we assume they are in a frigate
            if (!largeMap.IsAvatarInFrigate)
                throw new Ultima5ReduxException("can't fire on large map unless you're on a frigate");

            // make sure the boat is facing the correct direction given the direction of the cannon
            TileReference currentAvatarTileReference = avatar.IsAvatarOnBoardedThing
                ? avatar.GetBoardedTileReference()
                : avatar.KeyTileReference;

            bool bShipFacingUpDown = currentAvatarTileReference.Name.EndsWith("Up") ||
                                     currentAvatarTileReference.Name.EndsWith("Down");

            turnResults.PushOutputToConsole(
                GameReferences.Instance.DataOvlRef.StringReferences.GetString(DataOvlReference.KeypressCommandsStrings
                    .FIRE) + direction);
            // are we pointing the right direction to fire the cannons?
            if ((direction is Point2D.Direction.Down or Point2D.Direction.Up && bShipFacingUpDown)
                || (direction is Point2D.Direction.Left or Point2D.Direction.Right && !bShipFacingUpDown))
            {
                turnResults.PushOutputToConsole(GameReferences.Instance.DataOvlRef.StringReferences.GetString(
                    DataOvlReference.SleepTransportStrings
                        .FIRE_BROADSIDE_ONLY_BANG_N), false);
                turnResults.PushTurnResult(new BasicResult(TurnResult.TurnResultType.ActionFireBroadsideOnly));

                return AdvanceTime(N_DEFAULT_ADVANCE_TIME, turnResults);
            }

            // time to fire that cannon!
            const int frigateCannonFiresTiles = 3;

            GetAdjustments(direction, out int nXOffset, out int nYOffset);
            Point2D adjustedPosition = new(avatar.MapUnitPosition.X, avatar.MapUnitPosition.Y);
            // if 
            for (int i = 0; i < frigateCannonFiresTiles; i++)
            {
                adjustedPosition = new Point2D(adjustedPosition.X + nXOffset, adjustedPosition.Y + nYOffset);

                if (!State.TheVirtualMap.CurrentMap.IsMapUnitOccupiedTile(adjustedPosition)) continue;

                // kill them
                MapUnit targetedMapUnit = State.TheVirtualMap.CurrentMap.GetMapUnitsOnTile(adjustedPosition)[0];
                turnResults.PushOutputToConsole(
                    "Killed " + targetedMapUnit.FriendlyName, false);
                regularMap.ClearMapUnit(targetedMapUnit);
                cannonBallDestination = adjustedPosition;
                turnResults.PushTurnResult(new BasicResult(TurnResult.TurnResultType.ActionFireEnemyKilled));

                return AdvanceTime(N_DEFAULT_ADVANCE_TIME, turnResults);
            }

            cannonBallDestination = adjustedPosition;
            turnResults.PushTurnResult(new BasicResult(TurnResult.TurnResultType.ActionFireHitNothing));
            return AdvanceTime(N_DEFAULT_ADVANCE_TIME, turnResults);
        }

        /// <summary>
        ///     Gets a thing from the world, adding it the inventory and providing output for console
        /// </summary>
        /// <param name="xy">where is the thing?</param>
        /// <param name="bGotAThing">did I get a thing?</param>
        /// <param name="inventoryItem"></param>
        /// <param name="turnResults"></param>
        /// <param name="direction"></param>
        /// <returns>the output string</returns>
        // ReSharper disable once UnusedMethodReturnValue.Global
        [SuppressMessage("ReSharper", "UnusedMember.Global")]
        public List<VirtualMap.AggressiveMapUnitInfo> TryToGetAThing(Point2D xy, out bool bGotAThing,
            out InventoryItem inventoryItem, TurnResults turnResults, Point2D.Direction direction)
        {
            bGotAThing = true;
            inventoryItem = null;

            TileReference tileReference = State.TheVirtualMap.CurrentMap.GetTileReference(xy);

            if (State.TheVirtualMap.CurrentMap.CurrentSingleMapReference == null)
                throw new Ultima5ReduxException("No single map is set in virtual map");

            // wall sconces - BORROWED!
            if (tileReference.Index == GameReferences.Instance.SpriteTileReferences.GetTileNumberByName("LeftSconce") ||
                tileReference.Index == GameReferences.Instance.SpriteTileReferences.GetTileNumberByName("RightSconce"))
            {
                State.PlayerInventory.TheProvisions.Items[ProvisionReferences.SpecificProvisionType.Torches].Quantity++;

                State.TheVirtualMap.CurrentMap.SetOverridingTileReference(
                    GameReferences.Instance.SpriteTileReferences.GetTileReferenceByName("BrickFloor"), xy);
                turnResults.PushOutputToConsole(
                    GameReferences.Instance.DataOvlRef.StringReferences.GetString(DataOvlReference.GetThingsStrings
                        .BORROWED),
                    false);
                turnResults.PushTurnResult(new BasicResult(TurnResult.TurnResultType.ActionGetBorrowed));
                return AdvanceTime(N_DEFAULT_ADVANCE_TIME, turnResults);
            }

            var magicCarpet = State.TheVirtualMap.CurrentMap.GetSpecificMapUnitByLocation<MagicCarpet>(xy,
                State.TheVirtualMap.CurrentMap.CurrentSingleMapReference.Floor);
            if (magicCarpet != null)
            {
                // add the carpet to the players inventory and remove it from the map
                State.PlayerInventory.SpecializedItems.Items[SpecialItem.SpecificItemType.Carpet].Quantity++;
                State.TheVirtualMap.CurrentMap.ClearAndSetEmptyMapUnits(magicCarpet);
                turnResults.PushOutputToConsole(GameReferences.Instance.DataOvlRef.StringReferences.GetString(
                    DataOvlReference.GetThingsStrings.A_MAGIC_CARPET), false);
                turnResults.PushTurnResult(new BasicResult(TurnResult.TurnResultType.ActionGetMagicCarpet));
                return AdvanceTime(N_DEFAULT_ADVANCE_TIME, turnResults);
            }

            if (tileReference.Index == (int)TileReference.SpriteIndex.WheatInField)
            {
                State.TheVirtualMap.CurrentMap.SetOverridingTileReference(
                    GameReferences.Instance.SpriteTileReferences.GetTileReference(TileReference.SpriteIndex
                        .PlowedField), xy);
                turnResults.PushOutputToConsole(GameReferences.Instance.DataOvlRef.StringReferences.GetString(
                    DataOvlReference.GetThingsStrings.CROPS_PICKED), false);
                State.PlayerInventory.TheProvisions.Food++;
                State.ChangeKarma(-1, turnResults);
                return AdvanceTime(N_DEFAULT_ADVANCE_TIME, turnResults);
            }

            // are you trying to get food from a table
            if (tileReference.IsTableWithFood)
            {
                bool bAte = false;
                switch (direction)
                {
                    case Point2D.Direction.Down:
                        switch (tileReference.Index)
                        {
                            case (int)TileReference.SpriteIndex.TableFoodBottom:
                                // do nothing
                                break;
                            case (int)TileReference.SpriteIndex.TableFoodBoth:
                                State.TheVirtualMap.CurrentMap.SetOverridingTileReference(
                                    GameReferences.Instance.SpriteTileReferences.GetTileReference(TileReference.SpriteIndex
                                        .TableFoodBottom), xy);
                                bAte = true;
                                break;
                            case (int)TileReference.SpriteIndex.TableFoodTop:
                                State.TheVirtualMap.CurrentMap.SetOverridingTileReference(
                                    GameReferences.Instance.SpriteTileReferences.GetTileReference(TileReference.SpriteIndex
                                        .TableMiddle), xy);
                                bAte = true;
                                break;
                        }

                        break;
                    case Point2D.Direction.Up:
                        switch (tileReference.Index)
                        {
                            case (int)TileReference.SpriteIndex.TableFoodBottom:
                                State.TheVirtualMap.CurrentMap.SetOverridingTileReference(
                                    GameReferences.Instance.SpriteTileReferences.GetTileReference(TileReference.SpriteIndex
                                        .TableMiddle), xy);
                                bAte = true;
                                // do nothing
                                break;
                            case (int)TileReference.SpriteIndex.TableFoodBoth:
                                State.TheVirtualMap.CurrentMap.SetOverridingTileReference(
                                    GameReferences.Instance.SpriteTileReferences.GetTileReference(TileReference.SpriteIndex
                                        .TableFoodTop), xy);
                                bAte = true;
                                break;
                            case (int)TileReference.SpriteIndex.TableFoodTop:
                                break;
                        }

                        break;
                    case Point2D.Direction.Left:
                    case Point2D.Direction.Right:
                    case Point2D.Direction.None:
                        break;
                    default:
                        throw new ArgumentOutOfRangeException(nameof(direction), direction, null);
                }

                // else pass on by - something isn't quite right
                if (bAte)
                {
                    State.PlayerInventory.TheProvisions.Food++;
                    State.ChangeKarma(-1, turnResults);
                    turnResults.PushOutputToConsole(GameReferences.Instance.DataOvlRef.StringReferences.GetString(
                        DataOvlReference.GetThingsStrings.MMM_DOT3), false);
                    return AdvanceTime(N_DEFAULT_ADVANCE_TIME, turnResults);
                }
                // we just fall through for the default handling if there is no food directly in front of us
            }

            if (State.TheVirtualMap.CurrentMap.IsMapUnitOccupiedTile(xy))
            {
                MapUnit mapUnit = State.TheVirtualMap.CurrentMap.GetTopVisibleMapUnit(xy, true);
                switch (mapUnit)
                {
                    case ItemStack { HasStackableItems: true } itemStack:
                    {
                        StackableItem stackableItem = itemStack.PopStackableItem();
                        InventoryItem invItem = stackableItem.InvItem;
                        inventoryItem = invItem ?? throw new Ultima5ReduxException(
                            "Tried to get inventory item from StackableItem but it was null");
                        State.PlayerInventory.AddInventoryItemToInventory(inventoryItem);

                        // if the items are all gone then we delete the stack from the map
                        // NOTE: lets try not deleting it so the 3d program knows to not draw it
                        if (!itemStack.HasStackableItems)
                        {
                            State.TheVirtualMap.CurrentMap.ClearAndSetEmptyMapUnits(itemStack);
                        }

                        turnResults.PushOutputToConsole(U5StringRef.ThouDostFind(invItem.FindDescription), false);
                        turnResults.PushTurnResult(new BasicResult(TurnResult.TurnResultType.ActionGetStackableItem));
                        return AdvanceTime(N_DEFAULT_ADVANCE_TIME, turnResults);
                    }
                    case MoonstoneNonAttackingUnit moonstoneNonAttackingUnit:
                        // get the Moonstone!
                        State.PlayerInventory.AddInventoryItemToInventory(moonstoneNonAttackingUnit.TheMoonstone);
                        inventoryItem = moonstoneNonAttackingUnit.TheMoonstone;
                        turnResults.PushOutputToConsole(
                            U5StringRef.ThouDostFind(moonstoneNonAttackingUnit.TheMoonstone.FindDescription), false);
                        turnResults.PushTurnResult(new BasicResult(TurnResult.TurnResultType.ActionGetMoonstone));
                        // get rid of it from the map
                        State.TheVirtualMap.CurrentMap.ClearMapUnit(moonstoneNonAttackingUnit);
                        return AdvanceTime(N_DEFAULT_ADVANCE_TIME, turnResults);
                }
            }

            bGotAThing = false;
            turnResults.PushOutputToConsole(GameReferences.Instance.DataOvlRef.StringReferences.GetString(
                DataOvlReference.GetThingsStrings.NOTHING_TO_GET), false);
            turnResults.PushTurnResult(new BasicResult(TurnResult.TurnResultType.ActionGetNothingToGet));
            return AdvanceTime(N_DEFAULT_ADVANCE_TIME, turnResults);
        }

        /// <summary>
        ///     Ignites a torch, if available and set the number of turns for the torch to be burnt out
        /// </summary>
        /// <returns></returns>
        // ReSharper disable once UnusedMember.Global
        public List<VirtualMap.AggressiveMapUnitInfo> TryToIgniteTorch(TurnResults turnResults)
        {
            // if there are no torches then report back and make no change
            if (State.PlayerInventory.TheProvisions.Items[ProvisionReferences.SpecificProvisionType.Torches].Quantity <=
                0)
            {
                turnResults.PushOutputToConsole(GameReferences.Instance.DataOvlRef.StringReferences.GetString(
                    DataOvlReference.SleepTransportStrings
                        .NONE_OWNED_BANG_N), false);
                turnResults.PushTurnResult(new BasicResult(TurnResult.TurnResultType.ActionIgniteTorchNoTorch));
                return AdvanceTime(N_DEFAULT_ADVANCE_TIME, turnResults);
            }

            State.PlayerInventory.TheProvisions.Items[ProvisionReferences.SpecificProvisionType.Torches].Quantity--;
            State.TurnsToExtinguish = N_DEFAULT_NUMBER_OF_TURNS_FOR_TORCH;
            // this will trigger a re-read of time of day changes
            State.TheTimeOfDay.SetAllChangeTrackers();

            turnResults.PushOutputToConsole(GameReferences.Instance.DataOvlRef.StringReferences.GetString(
                DataOvlReference.KeypressCommandsStrings.IGNITE_TORCH), false);
            turnResults.PushTurnResult(new BasicResult(TurnResult.TurnResultType.ActionIgniteTorch));

            return AdvanceTime(N_DEFAULT_ADVANCE_TIME, turnResults);
        }

        /// <summary>
        ///     Try to jimmy the door with a given character
        /// </summary>
        /// <param name="xy">position of the door</param>
        /// <param name="record">character who will attempt to open it</param>
        /// <param name="bWasSuccessful">was it a successful jimmy?</param>
        /// <param name="turnResults"></param>
        /// <returns>the output string to write to console</returns>
        // ReSharper disable once UnusedMember.Global
        // ReSharper disable once UnusedParameter.Global
        public List<VirtualMap.AggressiveMapUnitInfo> TryToJimmyDoor(Point2D xy, PlayerCharacterRecord record,
            out bool bWasSuccessful, TurnResults turnResults)
        {
            bWasSuccessful = false;
            TileReference tileReference = State.TheVirtualMap.CurrentMap.GetTileReference(xy);

            //bool isDoorInDirection = tileReference.IsOpenable;
            bool bIsStocks = TileReferences.IsStocks(tileReference.Index);
            bool bIsManacles = TileReferences.IsManacles(tileReference.Index); // is it shackles/manacles

            bool bIsDoorMagical = TileReferences.IsDoorMagical(tileReference.Index);
            bool bIsDoorLocked = TileReferences.IsDoorLocked(tileReference.Index);

            // the stocks 
            MapUnit mapUnit = State.TheVirtualMap.CurrentMap.GetTopVisibleMapUnit(xy, true);
            bool bIsNpc = mapUnit is NonPlayerCharacter;

            if (!bIsDoorMagical &&
                !bIsDoorLocked &&
                !(bIsManacles && bIsNpc) &&
                !(bIsStocks && bIsNpc))
            {
                turnResults.PushOutputToConsole(GameReferences.Instance.DataOvlRef.StringReferences.GetString(
                    DataOvlReference.OpeningThingsStrings.NO_LOCK), false);
                turnResults.PushTurnResult(new BasicResult(TurnResult.TurnResultType.ActionJimmyNoLock));
                bWasSuccessful = false;
                return AdvanceTime(N_DEFAULT_ADVANCE_TIME, turnResults);
            }

            bool bBrokenKey = !OddsAndLogic.IsJimmySuccessful(record.Stats.Dexterity)
                              && State.TheGameOverrides.DebugTheLockPickingOverrides !=
                              GameOverrides.LockPickingOverrides.AlwaysSucceed;

            // we check to see if the lock pick (key) broke and the right conditions are met
            if (bIsDoorMagical || bBrokenKey)
            {
                // we use up a key
                State.PlayerInventory.TheProvisions.Items[ProvisionReferences.SpecificProvisionType.Keys]
                    .Quantity--;

                bWasSuccessful = false;

                turnResults.PushOutputToConsole(GameReferences.Instance.DataOvlRef.StringReferences.GetString(
                    DataOvlReference.OpeningThingsStrings.KEY_BROKE), false);
                turnResults.PushTurnResult(new BasicResult(TurnResult.TurnResultType.ActionJimmyKeyBroke));

                return AdvanceTime(N_DEFAULT_ADVANCE_TIME, turnResults);
            }

            // we know we actually have something to unlock at this point, and have NOT broke a key
            turnResults.PushOutputToConsole(GameReferences.Instance.DataOvlRef.StringReferences.GetString(
                DataOvlReference.OpeningThingsStrings
                    .UNLOCKED), false);
            bWasSuccessful = true;

            if (bIsDoorLocked)
            {
                State.TheVirtualMap.CurrentMap.SetOverridingTileReference(
                    TileReferences.IsDoorWithView(tileReference.Index)
                        ? GameReferences.Instance.SpriteTileReferences.GetTileReference(TileReference.SpriteIndex
                            .RegularDoorView)
                        : GameReferences.Instance.SpriteTileReferences.GetTileReference(
                            TileReference.SpriteIndex.RegularDoor), xy);

                turnResults.PushTurnResult(new BasicResult(TurnResult.TurnResultType.ActionJimmyUnlocked));
            }
            else if (bIsStocks)
            {
                turnResults.PushTurnResult(new BasicResult(TurnResult.TurnResultType.NpcFreedFromStocks));
                turnResults.PushOutputToConsole(GameReferences.Instance.DataOvlRef.StringReferences.GetString(
                    DataOvlReference.OpeningThingsStrings.N_N_I_THANK_THEE_N).Trim(), false);
                mapUnit.NpcState.OverrideAi(
                    NonPlayerCharacterSchedule.AiType.FollowAroundAndBeAnnoyingThenNeverSeeAgain);

                State.ChangeKarma(2, turnResults);
            }
            // ReSharper disable once ConditionIsAlwaysTrueOrFalse
            else if (bIsManacles)
            {
                turnResults.PushTurnResult(new BasicResult(TurnResult.TurnResultType.NpcFreedFromManacles));
                turnResults.PushOutputToConsole(GameReferences.Instance.DataOvlRef.StringReferences.GetString(
                    DataOvlReference.OpeningThingsStrings.N_N_I_THANK_THEE_N).Trim(), false);

                mapUnit.NpcState.OverrideAi(NonPlayerCharacterSchedule.AiType.BigWander);

                State.ChangeKarma(2, turnResults);
            }

            return AdvanceTime(N_DEFAULT_ADVANCE_TIME, turnResults);
        }

        /// <summary>
        ///     Climbs the ladder on the current tile that the Avatar occupies
        /// </summary>
        [SuppressMessage("ReSharper", "UnusedMember.Global")]
        public List<VirtualMap.AggressiveMapUnitInfo> TryToKlimb(out KlimbResult klimbResult, TurnResults turnResults)
        {
            switch (State.TheVirtualMap.CurrentMap)
            {
                // if it's a large map, we either klimb with the grapple or don't klimb at all
                case LargeMap when State.PlayerInventory.SpecializedItems.Items[SpecialItem.SpecificItemType.Grapple]
                    .HasOneOrMore:
                    klimbResult = KlimbResult.RequiresDirection;
                    turnResults.PushOutputToConsole(GetKlimbOutput(), false);
                    turnResults.PushTurnResult(new BasicResult(TurnResult.TurnResultType.ActionKlimbRequiresDirection));
                    return new List<VirtualMap.AggressiveMapUnitInfo>();
                // we don't have a grapple, so we can't klimb
                case LargeMap:
                    klimbResult = KlimbResult.CantKlimb;
                    turnResults.PushOutputToConsole(GetKlimbOutput(
                        GameReferences.Instance.DataOvlRef.StringReferences.GetString(DataOvlReference.KlimbingStrings
                            .WITH_WHAT)), false);
                    turnResults.PushTurnResult(new BasicResult(TurnResult.TurnResultType.ActionKlimbWithWhat));
                    return AdvanceTime(N_DEFAULT_ADVANCE_TIME, turnResults);
                case DungeonMap:
                    // TODO
                    klimbResult = KlimbResult.CantKlimb;
                    return AdvanceTime(N_DEFAULT_ADVANCE_TIME, turnResults);
                case CombatMap:
                    // TODO
                    klimbResult = KlimbResult.CantKlimb;
                    return AdvanceTime(N_DEFAULT_ADVANCE_TIME, turnResults);
            }

            // implied small map
            if (State.TheVirtualMap.CurrentMap is not SmallMap smallMap)
                throw new Ultima5ReduxException("Should be small map");
            TileReference curTileRef = smallMap.GetTileReferenceOnCurrentTile();

            // we can't klimb on the current tile, so we need to pick a direction
            if (!TileReferences.IsLadder(curTileRef.Index) &&
                !TileReferences.IsGrate(curTileRef.Index))
            {
                klimbResult = KlimbResult.RequiresDirection;
                turnResults.PushOutputToConsole(GetKlimbOutput(), false);
                turnResults.PushTurnResult(new BasicResult(TurnResult.TurnResultType.ActionKlimbRequiresDirection));
                return new List<VirtualMap.AggressiveMapUnitInfo>();
            }

            if (smallMap.CurrentSingleMapReference == null)
                throw new Ultima5ReduxException("No single map is set in virtual map");

            SmallMapReferences.SingleMapReference.Location location =
                smallMap.CurrentSingleMapReference.MapLocation;
            int nCurrentFloor = smallMap.CurrentSingleMapReference.Floor;
            bool hasBasement = GameReferences.Instance.SmallMapRef.HasBasement(location);
            int nTotalFloors = GameReferences.Instance.SmallMapRef.GetNumberOfFloors(location);
            int nTopFloor = hasBasement ? nTotalFloors - 1 : nTotalFloors;

            TileReference tileReference =
                State.TheVirtualMap.CurrentMap.GetTileReference(State.TheVirtualMap.CurrentMap.CurrentPosition.XY);
            if (TileReferences.IsLadderDown(tileReference.Index) ||
                TileReferences.IsGrate(tileReference.Index))
            {
                if ((hasBasement && nCurrentFloor >= 0) || nCurrentFloor > 0)
                {
                    State.TheVirtualMap.LoadSmallMap(
                        GameReferences.Instance.SmallMapRef.GetSingleMapByLocation(location, nCurrentFloor - 1),
                        State.TheVirtualMap.CurrentMap.CurrentPosition.XY);
                    klimbResult = KlimbResult.Success;
                    turnResults.PushOutputToConsole(GetKlimbOutput(
                            GameReferences.Instance.DataOvlRef.StringReferences.GetString(DataOvlReference.TravelStrings
                                .DOWN)),
                        false);
                    turnResults.PushTurnResult(new BasicResult(TurnResult.TurnResultType.ActionKlimbDown));
                    return AdvanceTime(N_DEFAULT_ADVANCE_TIME, turnResults);
                }
            }
            // else if there is a ladder up and we are not yet on the top floor
            else if (TileReferences.IsLadderUp(tileReference.Index) &&
                     nCurrentFloor + 1 < nTopFloor)
            {
                State.TheVirtualMap.LoadSmallMap(
                    GameReferences.Instance.SmallMapRef.GetSingleMapByLocation(location, nCurrentFloor + 1),
                    State.TheVirtualMap.CurrentMap.CurrentPosition.XY);
                klimbResult = KlimbResult.Success;
                turnResults.PushOutputToConsole(GetKlimbOutput(
                        GameReferences.Instance.DataOvlRef.StringReferences.GetString(DataOvlReference.TravelStrings
                            .UP)),
                    false);
                turnResults.PushTurnResult(new BasicResult(TurnResult.TurnResultType.ActionKlimbUp));
                return AdvanceTime(N_DEFAULT_ADVANCE_TIME, turnResults);
            }

            klimbResult = KlimbResult.RequiresDirection;
            turnResults.PushOutputToConsole(GetKlimbOutput(), false);
            turnResults.PushTurnResult(new BasicResult(TurnResult.TurnResultType.ActionKlimbRequiresDirection));
            return new List<VirtualMap.AggressiveMapUnitInfo>();
        }

        /// <summary>
        ///     Try to klimb the given tile - typically called after you select a direction
        /// </summary>
        /// <param name="xy"></param>
        /// <param name="klimbResult"></param>
        /// <param name="turnResults"></param>
        /// <returns></returns>
        // ReSharper disable once UnusedMember.Global
        public List<VirtualMap.AggressiveMapUnitInfo> TryToKlimbInDirection(Point2D xy, out KlimbResult klimbResult,
            TurnResults turnResults)
        {
            TileReference tileReference = State.TheVirtualMap.CurrentMap.GetTileReference(xy);
            if (State.TheVirtualMap.CurrentMap is LargeMap)
            {
                // is it even klimbable?
                if (tileReference.IsKlimable)
                {
                    if (tileReference.Index !=
                        GameReferences.Instance.SpriteTileReferences.GetTileNumberByName("SmallMountains"))
                        throw new Ultima5ReduxException(
                            "I am not personal aware of what on earth you would be klimbing that is not already stated in the following logic...");

                    State.GrapplingFall();
                    klimbResult = KlimbResult.SuccessFell;
                    turnResults.PushOutputToConsole(GameReferences.Instance.DataOvlRef.StringReferences.GetString(
                        DataOvlReference.KlimbingStrings.FELL), false);
                    turnResults.PushTurnResult(
                        new BasicResult(TurnResult.TurnResultType.ActionKlimbDirectionMovedFell));
                }
                // is it tall mountains? we can't klimb those
                else if (tileReference.Index ==
                         GameReferences.Instance.SpriteTileReferences.GetTileNumberByName("TallMountains"))
                {
                    klimbResult = KlimbResult.CantKlimb;
                    turnResults.PushOutputToConsole(GameReferences.Instance.DataOvlRef.StringReferences.GetString(
                        DataOvlReference.KlimbingStrings.IMPASSABLE), false);
                    turnResults.PushTurnResult(
                        new BasicResult(TurnResult.TurnResultType.ActionKlimbDirectionImpassable));
                }
                // there is no chance of klimbing the thing
                else
                {
                    klimbResult = KlimbResult.CantKlimb;
                    turnResults.PushOutputToConsole(GameReferences.Instance.DataOvlRef.StringReferences.GetString(
                        DataOvlReference.KlimbingStrings.NOT_CLIMABLE), false);
                    turnResults.PushTurnResult(
                        new BasicResult(TurnResult.TurnResultType.ActionKlimbDirectionUnKlimable));
                }
            }
            else // it's a small map
            {
                if (tileReference.IsKlimable)
                {
                    // ie. a fence
                    klimbResult = KlimbResult.Success;

                    turnResults.PushTurnResult(new BasicResult(TurnResult.TurnResultType.ActionKlimbDirectionSuccess));
                }
                else
                {
                    klimbResult = KlimbResult.CantKlimb;
                    turnResults.PushOutputToConsole(
                        GameReferences.Instance.DataOvlRef.StringReferences.GetString(DataOvlReference.TravelStrings
                            .WHAT),
                        false);
                    turnResults.PushTurnResult(new BasicResult(TurnResult.TurnResultType.ActionKlimbWhat));
                }
            }

            return AdvanceTime(N_DEFAULT_ADVANCE_TIME, turnResults);
        }

        /// <summary>
        ///     Looks at a particular tile, detecting if NPCs are present as well
        ///     Provides string output or special instructions if it is "special"B
        /// </summary>
        /// <param name="xy">position of tile to look at</param>
        /// <param name="specialLookCommand">Special command such as look at gem or sign</param>
        /// <param name="turnResults"></param>
        /// <returns>String to output to user</returns>
        // ReSharper disable once UnusedMember.Global
        public List<VirtualMap.AggressiveMapUnitInfo> TryToLook(Point2D xy, out SpecialLookCommand specialLookCommand,
            TurnResults turnResults)
        {
            specialLookCommand = SpecialLookCommand.None;
            string lookStr;

            TileReference tileReference = State.TheVirtualMap.CurrentMap.GetTileReference(xy);
            // if there is an NPC on the tile, then we assume they want to look at the NPC, not whatever else may be on the tiles
            if (State.TheVirtualMap.CurrentMap.IsMapUnitOccupiedTile(xy))
            {
                List<MapUnit> mapUnits = State.TheVirtualMap.CurrentMap.GetMapUnitsOnTile(xy);
                if (mapUnits.Count <= 0)
                {
                    throw new Ultima5ReduxException("Tried to look up Map Unit, but couldn't find the map character");
                }

                lookStr = GameReferences.Instance.DataOvlRef.StringReferences
                              .GetString(DataOvlReference.Vision2Strings.THOU_DOST_SEE).Trim() + " " +
                          GameReferences.Instance.LookRef.GetLookDescription(mapUnits[0].KeyTileReference.Index).Trim();
            }
            // if we are any one of these signs then we superimpose it on the screen
            else if (GameReferences.Instance.SpriteTileReferences.IsSign(tileReference.Index))
            {
                specialLookCommand = SpecialLookCommand.Sign;
                lookStr = string.Empty;
            }
            else
                switch (tileReference.Index)
                {
                    case (int)TileReference.SpriteIndex.Well:
                        var wishingWell =
                            WishingWell.Create(State.TheVirtualMap.CurrentMap.CurrentSingleMapReference.MapLocation,
                                xy, State.TheVirtualMap.CurrentMap.CurrentSingleMapReference.Floor);
                        turnResults.PushTurnResult(new NpcTalkInteraction(wishingWell, "WishingWell"));
                        return AdvanceTime(N_DEFAULT_ADVANCE_TIME, turnResults);
                    case (int)TileReference.SpriteIndex.Clock1 or (int)TileReference.SpriteIndex.Clock2:
                        lookStr = GameReferences.Instance.DataOvlRef.StringReferences
                                      .GetString(DataOvlReference.Vision2Strings.THOU_DOST_SEE).Trim() + " " +
                                  GameReferences.Instance.LookRef.GetLookDescription(tileReference.Index).TrimStart() +
                                  State.TheTimeOfDay.FormattedTime;
                        break;
                    default:
                        // lets see what we've got here!
                        lookStr = GameReferences.Instance.DataOvlRef.StringReferences
                                      .GetString(DataOvlReference.Vision2Strings.THOU_DOST_SEE).Trim() + " " +
                                  GameReferences.Instance.LookRef.GetLookDescription(tileReference.Index).TrimStart();
                        break;
                }

            // pass time at the end to make sure moving characters are accounted for
            turnResults.PushOutputToConsole(lookStr, false);
            turnResults.PushTurnResult(new BasicResult(TurnResult.TurnResultType.ActionLook));

            return AdvanceTime(N_DEFAULT_ADVANCE_TIME, turnResults);
        }

        [SuppressMessage("ReSharper", "UnusedMember.Global")]
        public void TryToMoveCombatMap(Point2D.Direction direction, TurnResults turnResults, bool bKlimb)
        {
            if (State.TheVirtualMap.CurrentMap is not CombatMap combatMap)
                throw new Ultima5ReduxException("Should be combat map");

            TryToMoveCombatMap(combatMap.CurrentCombatPlayer, direction, turnResults,
                bKlimb);
        }

        public void TryToMoveCombatMap(CombatPlayer combatPlayer, Point2D.Direction direction,
            TurnResults turnResults, bool bKlimb)
        {
            if (combatPlayer == null)
                throw new Ultima5ReduxException("Trying to move on combat map without a CombatPlayer");

            if (State.TheVirtualMap.CurrentMap is not CombatMap combatMap)
                throw new Ultima5ReduxException("Called TryToMoveCombatMap on non CombatMap");

            // if we were to move, which direction would we move
            GetAdjustments(direction, out int xAdjust, out int yAdjust);
            Point2D newPosition = new(combatPlayer.MapUnitPosition.X + xAdjust,
                combatPlayer.MapUnitPosition.Y + yAdjust);

            // if we are klimbing then we assume all the movement checks were done ahead of time
            if (!bKlimb)
            {
                turnResults.PushOutputToConsole(
                    GameReferences.Instance.DataOvlRef.StringReferences.GetDirectionString(direction), true, false);

                if (IsLeavingMap(newPosition))
                {
                    // can they escape in this particular direction?
                    bool _ = combatMap.TryToMakePlayerEscape(turnResults, combatPlayer,
                        CombatMap.DirectionToEscapeType(direction));

                    return;
                }

                if (!combatMap.IsTileFreeToTravel(combatPlayer.MapUnitPosition.XY, newPosition,
                        false,
                        Avatar.AvatarState.Regular))
                {
                    turnResults.PushOutputToConsole(" - " +
                                                    GameReferences.Instance.DataOvlRef.StringReferences.GetString(
                                                        DataOvlReference.TravelStrings.BLOCKED), false);
                    turnResults.PushTurnResult(new BasicResult(TurnResult.TurnResultType.ActionMoveBlocked));
                    return;
                }
            }

            ProcessDamageOnAdvanceTimeInCombat(turnResults);
            combatMap.MoveActiveCombatMapUnit(turnResults, newPosition);

            turnResults.PushTurnResult(new BasicResult(TurnResult.TurnResultType.ActionMovedCombatPlayerOnCombatMap));

            combatMap.AdvanceToNextCombatMapUnit();
        }

        /// <summary>
        ///     Tries to move the avatar in a given direction - if successful it will move him
        /// </summary>
        /// <param name="direction">the direction you want to move</param>
        /// <param name="bKlimb">is the avatar K-limbing?</param>
        /// <param name="bFreeMove">is "free move" on?</param>
        /// <param name="turnResults"></param>
        /// <param name="bManualMovement">true if movement is manual</param>
        /// <returns>output string (may be empty)</returns>
        [SuppressMessage("ReSharper", "UnusedMember.Global")]
        public List<VirtualMap.AggressiveMapUnitInfo> TryToMoveNonCombatMap(Point2D.Direction direction, bool bKlimb,
            bool bFreeMove, TurnResults turnResults, bool bManualMovement = true)
        {
            if (State.TheVirtualMap.CurrentMap is not RegularMap regularMap)
                throw new Ultima5ReduxException("TryToMoveNonCombatMap called on non RegularMap");

            int nTimeAdvanceFactor =
                regularMap is SmallMap ? 1 : N_DEFAULT_ADVANCE_TIME;
            int nTilesPerMapRow = regularMap.NumOfXTiles;
            int nTilesPerMapCol = regularMap.NumOfYTiles;

            // if we were to move, which direction would we move
            GetAdjustments(direction, out int xAdjust, out int yAdjust);

            // would we be leaving a small map if we went forward?
            if (regularMap is not LargeMap && IsLeavingMap(new Point2D(
                    regularMap.CurrentPosition.X + xAdjust, regularMap.CurrentPosition.Y + yAdjust)))
            {
                turnResults.PushTurnResult(new BasicResult(TurnResult.TurnResultType.OfferToExitScreen));
                // it is expected that the called will offer an exit option, but we won't move the avatar because the space
                // is empty
                return new List<VirtualMap.AggressiveMapUnitInfo>();
            }

            Point2D originalPos = regularMap.CurrentPosition.XY;

            // calculate our new x and y values based on the adjustments
            Point2D newPosition = new((regularMap.CurrentPosition.X + xAdjust) % nTilesPerMapCol,
                (regularMap.CurrentPosition.Y + yAdjust) % nTilesPerMapRow);
            Avatar avatar = regularMap.GetAvatarMapUnit();
            // we change the direction of the Avatar map unit
            // this will be used to determine which is the appropriate sprite to show
            bool bAvatarActuallyMoved = avatar.Move(direction);

            // if the avatar did not actually move at this point then it appears they are on a frigate 
            // and change direction
            if (!bAvatarActuallyMoved)
            {
                turnResults.PushOutputToConsole(
                    GameReferences.Instance.DataOvlRef.StringReferences.GetString(DataOvlReference.WorldStrings.HEAD)
                        .TrimEnd() +
                    " " + GameReferences.Instance.DataOvlRef.StringReferences.GetDirectionString(direction));

                turnResults.PushTurnResult(new BasicResult(TurnResult.TurnResultType.ActionMoveChangeFrigateDirection));

                List<VirtualMap.AggressiveMapUnitInfo> aggressiveMapUnitInfos =
                    AdvanceTime(nTimeAdvanceFactor, turnResults);
                return aggressiveMapUnitInfos;
            }

            // we know that if the avatar is on a frigate, then he hasn't just changed direction
            // so, if sails are hoisted and they are heading in a specific direction, then we will ignore
            // any additional keystrokes
            if (avatar.AreSailsHoisted && State.WindDirection != Point2D.Direction.None && bManualMovement)
            {
                turnResults.PushTurnResult(
                    new BasicResult(TurnResult.TurnResultType.ActionMoveFrigateSailsIgnoreMovement));
                return new List<VirtualMap.AggressiveMapUnitInfo>();
            }

            turnResults.PushOutputToConsole(GetMovementVerb(avatar, direction, bManualMovement));

            // if we have reached 0, and we are adjusting -1 then we should assume it's a round world and we are going to the opposite side
            // this should only be true if it is a RepeatMap
            if (newPosition.X < 0)
            {
                Debug.Assert(regularMap is LargeMap,
                    "You should not reach the very end of a map +/- 1 if you are not on a repeating map");
                newPosition.X += nTilesPerMapCol;
            }

            if (newPosition.Y < 0)
            {
                Debug.Assert(regularMap is LargeMap,
                    "You should not reach the very end of a map +/- 1 if you are not on a repeating map");
                newPosition.Y = nTilesPerMapRow + newPosition.Y;
            }

            // we get the newTile so that we can determine if it's passable
            TileReference newTileReference = regularMap.GetTileReference(newPosition.X, newPosition.Y);

            if (newTileReference.Index ==
                GameReferences.Instance.SpriteTileReferences.GetTileNumberByName("BrickFloorHole") &&
                !regularMap.IsAvatarRidingCarpet)
            {
                State.TheVirtualMap.UseStairs(newPosition, true);
                // we need to evaluate in the game and let the game know that they should continue to fall
                TileReference newTileRef = regularMap.GetTileReference(regularMap.CurrentPosition.XY);
                if (newTileRef.Index ==
                    GameReferences.Instance.SpriteTileReferences.GetTileNumberByName("BrickFloorHole"))
                    IsPendingFall = true;

                // todo: get string from data file
                turnResults.PushOutputToConsole("A TRAPDOOR!");
                turnResults.PushTurnResult(new BasicResult(TurnResult.TurnResultType.ActionMoveFell));
            }

            // we have evaluated and now know there is not a further fall (think Blackthorne's palace)
            IsPendingFall = false;

            // it's passable if it's marked as passable, 
            // but we double check if the portcullis is down
            bool bPassable = regularMap.IsTileFreeToTravelForAvatar(newPosition);

            // this is insufficient in case I am in a boat
            if ((bKlimb && newTileReference.IsKlimable) || bPassable || bFreeMove)
            {
                State.TheVirtualMap.CurrentMap.CurrentPosition.X = newPosition.X;
                State.TheVirtualMap.CurrentMap.CurrentPosition.Y = newPosition.Y;
            }
            else // it is not passable
            {
                if (!bManualMovement && avatar.AreSailsHoisted)
                {
                    regularMap.DamageShip(State.WindDirection, turnResults);
                }
                else
                {
                    turnResults.PushOutputToConsole(GameReferences.Instance.DataOvlRef.StringReferences.GetString(
                        DataOvlReference.TravelStrings.BLOCKED));
                    if (!newTileReference.Is(TileReference.SpriteIndex.Cactus))
                        return AdvanceTime(nTimeAdvanceFactor, turnResults);

                    turnResults.PushOutputToConsole(GameReferences.Instance.DataOvlRef.StringReferences.GetString(
                        DataOvlReference.WorldStrings.OUCH));
                    turnResults.PushTurnResult(
                        new BasicResult(TurnResult.TurnResultType.ActionBlockedRanIntoCactus));
                    State.CharacterRecords.RanIntoCactus(turnResults);
                }

                // if it's not passable then we have no more business here
                return AdvanceTime(nTimeAdvanceFactor, turnResults);
            }

            // the world is a circular - so when you get to the end, start over again
            // this will prevent a never ending growth or shrinking of character position in case the travel the world only moving right a bagillion times
            State.TheVirtualMap.CurrentMap.CurrentPosition.X %= nTilesPerMapCol;
            State.TheVirtualMap.CurrentMap.CurrentPosition.Y %= nTilesPerMapRow;

            // if you walk on top of a staircase then we will immediately jump to the next floor
            if (TileReferences.IsStaircase(newTileReference.Index) &&
                State.TheVirtualMap.CurrentMap is SmallMap smallMap)
            {
                State.TheVirtualMap.UseStairs(State.TheVirtualMap.CurrentMap.CurrentPosition.XY);
                turnResults.PushOutputToConsole(
                    smallMap.IsStairGoingDown(State.TheVirtualMap.CurrentMap.CurrentPosition.XY, out _)
                        ? GameReferences.Instance.DataOvlRef.StringReferences.GetString(DataOvlReference.TravelStrings
                            .DOWN)
                        : GameReferences.Instance.DataOvlRef.StringReferences.GetString(DataOvlReference.TravelStrings
                            .UP));

                turnResults.PushTurnResult(new BasicResult(TurnResult.TurnResultType.ActionMoveUsedStairs));
            }

            Avatar.AvatarState currentAvatarState =
                regularMap.GetAvatarMapUnit().CurrentAvatarState;

            bool bIsOnHorseOrCarpet =
                regularMap.IsAvatarRidingCarpet || regularMap.IsAvatarRidingHorse;
            int nTimeToPass = bIsOnHorseOrCarpet ? 1 : nTimeAdvanceFactor;

            // We are on a small map which means all movements use the time passing minutes
            // if we are on a big map then we may issue extra information about slow moving terrain
            if (State.TheVirtualMap.CurrentMap is not LargeMap)
            {
                turnResults.PushTurnResult(
                    new PlayerMoved(GetTurnResultMovedByAvatarState(avatar, bManualMovement),
                        originalPos, regularMap.CurrentPosition.XY,
                        regularMap.GetTileReferenceOnCurrentTile()));

                if (!bIsOnHorseOrCarpet || State.TheTimeOfDay.Tick % 2 != 0)
                    return AdvanceTime(nTimeAdvanceFactor, turnResults);

                AdvanceClockNoComputation(nTimeToPass);
                turnResults.PushTurnResult(new BasicResult(TurnResult.TurnResultType.AdvanceClockNoComputation));
                return new List<VirtualMap.AggressiveMapUnitInfo>();
            }

            // WE are DEFINITELY on a large map at this time
            bool bIsSailingWithSailsHoisted = false;
            if (regularMap.IsAvatarInFrigate)
            {
                if (regularMap.GetAvatarMapUnit().CurrentBoardedMapUnit is not Frigate
                    avatarsFrigate)
                    throw new Ultima5ReduxException("Tried get Avatar's frigate, but it was null");

                // if the sails are hoisted and the wind direction is the same behind you
                // then you will go twice as fast
                if (avatarsFrigate.SailsHoisted) bIsSailingWithSailsHoisted = true;
            }

            bool bIsSailingWithWindBehindMe = bIsSailingWithSailsHoisted &&
                                              !Point2D.IsOppositeDirection(State.WindDirection,
                                                  regularMap.GetAvatarMapUnit().Direction);

            int nDefaultMinutesToAdvance =
                GameReferences.Instance.SpriteTileReferences.GetMinuteIncrement(newTileReference.Index);
            if (State.TheVirtualMap.CurrentMap is SmallMap)
                nDefaultMinutesToAdvance = Math.Max(1, nTimeAdvanceFactor / 2);
            // if avatar is on a horse or carpet then we cut it in half, but let's make sure we don't 
            // end up with zero minutes
            int nMinutesToAdvance;

            // the HMS Cape makes you go twice as quick - which is quick!
            bool bHasHmsCape = State.PlayerInventory.SpecializedItems.Items[SpecialItem.SpecificItemType.HMSCape]
                .Quantity > 0;
            if (bIsSailingWithWindBehindMe && bHasHmsCape)
                nMinutesToAdvance = Math.Max(1, nDefaultMinutesToAdvance / 4);
            else if (bHasHmsCape && bIsSailingWithSailsHoisted)
                nMinutesToAdvance = Math.Max(1, nDefaultMinutesToAdvance / 2);
            else if (bIsOnHorseOrCarpet || bIsSailingWithWindBehindMe)
                nMinutesToAdvance = Math.Max(1, nDefaultMinutesToAdvance / 2);
            else nMinutesToAdvance = nDefaultMinutesToAdvance;

            string slowMovingStr = GameReferences.Instance.SpriteTileReferences
                .GetSlowMovementString(newTileReference.Index).TrimEnd();
            if (slowMovingStr != "") turnResults.PushOutputToConsole(slowMovingStr, false);

            // if you are on the carpet or skiff and hit rough seas then we injure the players and report it back 
            if (currentAvatarState is Avatar.AvatarState.Carpet or Avatar.AvatarState.Skiff
                && newTileReference.Is(TileReference.SpriteIndex.Water)) // rough seas
            {
                State.CharacterRecords.RoughSeasInjure(turnResults);
                turnResults.PushOutputToConsole(
                    GameReferences.Instance.DataOvlRef.StringReferences.GetString(DataOvlReference.WorldStrings
                        .ROUGH_SEAS), false);
                turnResults.PushTurnResult(new BasicResult(TurnResult.TurnResultType.ActionMoveRoughSeas));
            }
            else
            {
                turnResults.PushTurnResult(
                    new PlayerMoved(GetTurnResultMovedByAvatarState(avatar, bManualMovement),
                        originalPos, regularMap.CurrentPosition.XY,
                        regularMap.GetTileReferenceOnCurrentTile()));
            }

            // if you are on a horse or carpet then only every other move actually results in the enemies and npcs moving
            // around the map
            if ((!bIsOnHorseOrCarpet && !bIsSailingWithWindBehindMe) || State.TheTimeOfDay.Tick % 2 != 0)
                return AdvanceTime(nMinutesToAdvance, turnResults);

            AdvanceClockNoComputation(nTimeToPass);
            turnResults.PushTurnResult(new BasicResult(TurnResult.TurnResultType.AdvanceClockNoComputation));
            return new List<VirtualMap.AggressiveMapUnitInfo>();
        }

        /// <summary>
        ///     Opens a door at a specific position
        /// </summary>
        /// <param name="xy">position of door</param>
        /// <param name="bWasSuccessful">was the door opening successful?</param>
        /// <param name="turnResults"></param>
        /// <returns>the output string to write to console</returns>
        // ReSharper disable once UnusedMember.Global
        public List<VirtualMap.AggressiveMapUnitInfo> TryToOpenAThing(Point2D xy, out bool bWasSuccessful,
            TurnResults turnResults)
        {
            bWasSuccessful = false;

            TileReference tileReference = State.TheVirtualMap.CurrentMap.GetTileReference(xy);

            bool isDoorInDirection = tileReference.IsOpenable;
            if (isDoorInDirection) return ProcessOpenDoor(xy, ref bWasSuccessful, turnResults, tileReference);

            MapUnit mapUnit = State.TheVirtualMap.CurrentMap.GetTopVisibleMapUnit(xy, true);

            if (mapUnit is NonAttackingUnit { IsOpenable: true } nonAttackingUnit)
            {
                bWasSuccessful =
                    State.TheVirtualMap.CurrentMap.ProcessSearchInnerItems(turnResults, nonAttackingUnit, false, true);
                return AdvanceTime(N_DEFAULT_ADVANCE_TIME, turnResults);
            }

            // if we fell through here then there is nothing open - easy enough
            turnResults.PushOutputToConsole(GameReferences.Instance.DataOvlRef.StringReferences.GetString(
                DataOvlReference.OpeningThingsStrings
                    .NOTHING_TO_OPEN), false);
            turnResults.PushTurnResult(new BasicResult(TurnResult.TurnResultType.ActionOpenDoorNothingToOpen));
            return AdvanceTime(N_DEFAULT_ADVANCE_TIME, turnResults);
        }

        /// <summary>
        ///     Standard way of passing time, makes sure it passes the default amount of time (2 minutes)
        /// </summary>
        /// <returns>"" or a string that describes what happened when passing time</returns>
        [SuppressMessage("ReSharper", "UnusedMember.Global")]
        public List<VirtualMap.AggressiveMapUnitInfo> TryToPassTime(TurnResults turnResults)
        {
            if (turnResults == null) throw new ArgumentNullException(nameof(turnResults));

            List<VirtualMap.AggressiveMapUnitInfo> aggressiveMapUnitInfos =
                AdvanceTime(N_DEFAULT_ADVANCE_TIME, turnResults);
            turnResults.PushTurnResult(new BasicResult(TurnResult.TurnResultType.PassTurn));

            return aggressiveMapUnitInfos;
        }

        /// <summary>
        ///     Attempts to push (or pull!) a map item
        /// </summary>
        /// <param name="avatarXy">the avatar's current map position</param>
        /// <param name="direction">the direction of the thing the avatar wants to push</param>
        /// <param name="bPushedAThing">was a thing actually pushed?</param>
        /// <param name="turnResults"></param>
        /// <returns>the string to output to the user</returns>
        [SuppressMessage("ReSharper", "UnusedMember.Global")]
        public List<VirtualMap.AggressiveMapUnitInfo> TryToPushAThing(Point2D avatarXy, Point2D.Direction direction,
            out bool bPushedAThing, TurnResults turnResults)
        {
            bPushedAThing = false;
            Point2D adjustedPos = avatarXy.GetAdjustedPosition(direction);

            TileReference adjustedTileReference = State.TheVirtualMap.CurrentMap.GetTileReference(adjustedPos);

            // it's not pushable OR if an NPC occupies the tile -so let's bail
            if (!adjustedTileReference.IsPushable || State.TheVirtualMap.CurrentMap.IsMapUnitOccupiedTile(adjustedPos))
            {
                turnResults.PushOutputToConsole(GameReferences.Instance.DataOvlRef.StringReferences.GetString(
                    DataOvlReference.ExclaimStrings
                        .WONT_BUDGE_BANG_N), false);
                turnResults.PushTurnResult(new BasicResult(TurnResult.TurnResultType.ActionPushWontBudge));
                return AdvanceTime(N_DEFAULT_ADVANCE_TIME, turnResults);
            }

            // we get the thing one tile further than the thing to see if we have room to push it forward
            Point2D oneMoreTileAdjusted = adjustedPos.GetAdjustedPosition(direction);
            TileReference oneMoreTileReference = State.TheVirtualMap.CurrentMap.GetTileReference(oneMoreTileAdjusted);

            // if I'm sitting and the proceeding tile is an upright tile then I can't swap things 
            if (State.TheVirtualMap.CurrentMap is SmallMap smallMap && smallMap.IsAvatarSitting() &&
                oneMoreTileReference.IsUpright)
            {
                turnResults.PushOutputToConsole(GameReferences.Instance.DataOvlRef.StringReferences.GetString(
                    DataOvlReference.ExclaimStrings
                        .WONT_BUDGE_BANG_N), false);
                turnResults.PushTurnResult(new BasicResult(TurnResult.TurnResultType.ActionPushWontBudge));
                return AdvanceTime(N_DEFAULT_ADVANCE_TIME, turnResults);
            }

            bPushedAThing = true;

            // if you are pushing a chair then change the direction of chair when it's pushed
            if (TileReferences.IsChair(adjustedTileReference.Index))
            {
                adjustedTileReference = GetChairNewDirection(direction);
                State.TheVirtualMap.CurrentMap.SetOverridingTileReference(adjustedTileReference, adjustedPos);
            }

            // is there an NPC on the tile? if so, we won't move anything into them
            bool bIsNpcOneMoreTile = State.TheVirtualMap.CurrentMap.IsMapUnitOccupiedTile(oneMoreTileAdjusted);

            // is the next tile walkable and is there NOT an NPC on it
            if (oneMoreTileReference.IsWalking_Passable && !bIsNpcOneMoreTile)
                State.TheVirtualMap.CurrentMap.SwapTiles(adjustedPos, oneMoreTileAdjusted);
            else // the next tile isn't walkable so we just swap the avatar and the push tile
                // we will pull (swap) the thing
                State.TheVirtualMap.CurrentMap.SwapTiles(avatarXy, adjustedPos);

            // move the avatar to the new spot
            State.TheVirtualMap.CurrentMap.CurrentPosition.XY = adjustedPos.Copy();

            turnResults.PushOutputToConsole(
                GameReferences.Instance.DataOvlRef.StringReferences.GetString(DataOvlReference.ExclaimStrings
                    .PUSHED_BANG_N),
                false);

            turnResults.PushTurnResult(new BasicResult(TurnResult.TurnResultType.ActionPush));
            return AdvanceTime(N_DEFAULT_ADVANCE_TIME, turnResults);
        }


        /// <summary>
        ///     Try to search an area and see if you find anything!
        /// </summary>
        /// <param name="xy">location to search</param>
        /// <param name="bWasSuccessful">result if it was successful, true if you found one or more things</param>
        /// <param name="turnResults"></param>
        /// <returns>the output string to write to console</returns>
        [SuppressMessage("ReSharper", "UnusedMember.Global")]
        public List<VirtualMap.AggressiveMapUnitInfo> TryToSearch(Point2D xy, out bool bWasSuccessful,
            TurnResults turnResults)
        {
            bWasSuccessful = false;

            SpecialSearchLocation specialSearchLocation = GetSpecialSearchLocation(
                State.TheVirtualMap.CurrentMap.CurrentSingleMapReference.MapLocation,
                State.TheVirtualMap.CurrentMap.CurrentSingleMapReference.Floor, xy);

            if (specialSearchLocation == SpecialSearchLocation.GlassSwords)
            {
                Weapon glassSword =
                    State.PlayerInventory.TheWeapons.GetWeaponFromEquipment(DataOvlReference.Equipment.GlassSword);

                if (glassSword.Quantity <= 0)
                {
                    return SearchAndFindGlassSword(xy, out bWasSuccessful, turnResults);
                }
            }

            // we search the tile and expose any items that may be on it
            if (State.TheVirtualMap.CurrentMap is LargeMap largeMap)
            {
                // Search for Moonstones before whatever is next
                Moonstone moonstone = largeMap.SearchAndExposeMoonstone(xy);
                if (moonstone != null)
                {
                    bWasSuccessful = true;
                    turnResults.PushOutputToConsole(U5StringRef.ThouDostFind(moonstone.FindDescription), false);
                    return AdvanceTime(N_DEFAULT_ADVANCE_TIME, turnResults);
                }
            }

            // if there is something exposed already OR there is nothing found
            // OR if special search location has fallen through to here, then we want to make sure
            // we don't do any additional processing - such as glass swords
            if (!State.TheVirtualMap.ContainsSearchableMapUnits(xy) ||
                specialSearchLocation != SpecialSearchLocation.None)
            {
                turnResults.PushOutputToConsole(U5StringRef.ThouDostFind(
                    GameReferences.Instance.DataOvlRef.StringReferences.GetString(DataOvlReference.Vision2Strings
                        .NOTHING_OF_NOTE_DOT_N)), false);
                return AdvanceTime(N_DEFAULT_ADVANCE_TIME, turnResults);
            }

            bool bHasInnerNonAttackUnits = State.TheVirtualMap.CurrentMap.SearchNonAttackingMapUnit(xy, turnResults,
                State.CharacterRecords.AvatarRecord, State.CharacterRecords);

            TileReference tileReference = State.TheVirtualMap.CurrentMap.GetTileReference(xy);

            if (bHasInnerNonAttackUnits)
            {
                // do nothing, I think? The SearchNonAttackingMapUnit method takes care of the chatter
                bWasSuccessful = true;
            }
            else if (tileReference.HasSearchReplacement)
            {
                // this occurs when you search something - and once searched it turns into something else
                // like searching a wall that turns into a door
                TileReference replacementTile =
                    GameReferences.Instance.SpriteTileReferences.GetTileReference(tileReference.SearchReplacementIndex);
                State.TheVirtualMap.CurrentMap.SetOverridingTileReference(replacementTile, xy);
                turnResults.PushTurnResult(new OutputToConsole(U5StringRef.ThouDostFind(
                    GameReferences.Instance.DataOvlRef.StringReferences.GetString(DataOvlReference.ThingsIFindStrings
                        .A_HIDDEN_DOOR_BANG_N)), false));
            }
            // it could be a moongate, with a stone, but wrong time of day
            else
            {
                turnResults.PushOutputToConsole(U5StringRef.ThouDostFind(
                    GameReferences.Instance.DataOvlRef.StringReferences.GetString(DataOvlReference.Vision2Strings
                        .NOTHING_OF_NOTE_DOT_N)), false);
            }

            return AdvanceTime(N_DEFAULT_ADVANCE_TIME, turnResults);
        }

        [SuppressMessage("ReSharper", "UnusedMember.Global")]
        public List<VirtualMap.AggressiveMapUnitInfo> TryToTalk(MapUnitMovement.MovementCommandDirection direction,
            TurnResults turnResults)
        {
            NonPlayerCharacter npc = null;
            if (State.TheVirtualMap.CurrentMap is SmallMap npcSmallMap)
            {
                npc = npcSmallMap.GetNpcToTalkTo(direction);
            }

            if (npc == null)
            {
                turnResults.PushOutputToConsole(
                    GameReferences.Instance.DataOvlRef.StringReferences.GetString(DataOvlReference.KeypressCommandsStrings
                        .FUNNY_NO_RESPONSE), false,
                    false);
                turnResults.PushTurnResult(new BasicResult(TurnResult.TurnResultType.NoOneToTalkTo));
                return AdvanceTime(N_DEFAULT_ADVANCE_TIME, turnResults);
            }

            bool bHasNpcRef = npc.NpcRef != null;

            if (State.TheVirtualMap.CurrentMap is SmallMap smallMap)
            {
                if (smallMap.IsNpcInBed(npc))
                {
                    turnResults.PushOutputToConsole(
                        GameReferences.Instance.DataOvlRef.StringReferences.GetString(DataOvlReference.ChitChatStrings
                            .ZZZ),
                        false);
                    turnResults.PushTurnResult(new BasicResult(TurnResult.TurnResultType.CantTalkSleeping));
                    return AdvanceTime(N_DEFAULT_ADVANCE_TIME, turnResults);
                }

                if (smallMap.IsWantedManByThePoPo)
                {
                    turnResults.PushOutputToConsole(
                        GameReferences.Instance.DataOvlRef.StringReferences.GetString(DataOvlReference.ChitChatStrings
                            .DONT_HURT_ME),
                        false);
                    turnResults.PushTurnResult(new BasicResult(TurnResult.TurnResultType.DontHurtMeAfraid));
                    return AdvanceTime(N_DEFAULT_ADVANCE_TIME, turnResults);
                }
            }

            if (!bHasNpcRef)
            {
                turnResults.PushOutputToConsole(
                    GameReferences.Instance.DataOvlRef.StringReferences.GetString(DataOvlReference.ChitChatStrings
                        .NOBODY_HERE));
                turnResults.PushTurnResult(new BasicResult(TurnResult.TurnResultType.NoOneToTalkTo));
                return AdvanceTime(N_DEFAULT_ADVANCE_TIME, turnResults);
            }

            // if (npc.MapLocation == SmallMapReferences.SingleMapReference.Location.Palace_of_Blackthorn
            //     && npc.NPCRef.IsGuard)
            // {
            //     
            // }

            ////// HERE is where I will need to put in the custom dialog options for custom AI types
            NonPlayerCharacterSchedule.AiType aiType = npc.GetCurrentAiType(State.TheTimeOfDay);
            switch (aiType)
            {
                case NonPlayerCharacterSchedule.AiType.FollowAroundAndBeAnnoyingThenNeverSeeAgain:
                    turnResults.PushOutputToConsole(
                        GameReferences.Instance.DataOvlRef.StringReferences.GetString(DataOvlReference.ChitChatStrings
                            .NO_RESPONSE), false,
                        false);
                    break;
                case NonPlayerCharacterSchedule.AiType.ExtortOrAttackOrFollow:
                case NonPlayerCharacterSchedule.AiType.GenericExtortingGuard:
                    turnResults.PushTurnResult(new GuardExtortion(npc, GuardExtortion.ExtortionType.Generic,
                        OddsAndLogic.GetGuardExtortionAmount(OddsAndLogic.GetEraByTurn(State.TurnsSinceStart))));
                    break;
                case NonPlayerCharacterSchedule.AiType.HalfYourGoldExtortingGuard:
                    turnResults.PushTurnResult(new GuardExtortion(npc, GuardExtortion.ExtortionType.HalfGold, 0));
                    break;
                case NonPlayerCharacterSchedule.AiType.MerchantBuyingSellingCustom:
                case NonPlayerCharacterSchedule.AiType.MerchantBuyingSellingWander:
                case NonPlayerCharacterSchedule.AiType.MerchantBuyingSelling:
                    if (State.TheVirtualMap.CurrentMap is not SmallMap merchantSmallMap)
                        throw new Ultima5ReduxException("Cannot have merchant AI outside of a small map");
                    ShoppeKeeper shoppeKeeper = GameReferences.Instance.ShoppeKeeperDialogueReference.GetShoppeKeeper(
                        merchantSmallMap.MapLocation, npc.NpcRef.NpcType,
                        State.CharacterRecords, State.PlayerInventory);

                    if (npc.ArrivedAtLocation && shoppeKeeper.IsOnDuty(State.TheTimeOfDay))
                    {
                        turnResults.PushTurnResult(new ShoppeKeeperInteraction(shoppeKeeper));
                    }
                    else
                    {
                        turnResults.PushOutputToConsole(shoppeKeeper.GetComeLaterResponse(), false);
                        turnResults.PushTurnResult(new BasicResult(TurnResult.TurnResultType.ComeBackLater));
                    }

                    break;
                default:
                    if (npc.NpcRef.Script != null)
                    {
                        // just a plain old conversation
                        turnResults.PushTurnResult(new NpcTalkInteraction(npc));
                        return new List<VirtualMap.AggressiveMapUnitInfo>();
                    }

                    turnResults.PushOutputToConsole("They are not talkative...", false);

                    int nIndex = npc.NpcRef.Schedule.GetScheduleIndex(State.TheTimeOfDay);

                    turnResults.PushOutputToConsole($"Because their aiType is {aiType} with AiIndex: {nIndex}", false);
                    turnResults.PushTurnResult(new BasicResult(TurnResult.TurnResultType.NotTalkative));

                    break;
            }

            return AdvanceTime(N_DEFAULT_ADVANCE_TIME, turnResults);
        }

        [SuppressMessage("ReSharper", "UnusedMember.Global")]
        public List<VirtualMap.AggressiveMapUnitInfo> TryToUseLordBritishArtifactItem(
            LordBritishArtifact lordBritishArtifact, TurnResults turnResults)
        {
            switch (lordBritishArtifact.Artifact)
            {
                case LordBritishArtifact.ArtifactType.Amulet:
                    turnResults.PushOutputToConsole(GameReferences.Instance.DataOvlRef.StringReferences.GetString(
                                                        DataOvlReference.WearUseItemStrings
                                                            .AMULET_N_N) + "\n" +
                                                    GameReferences.Instance.DataOvlRef.StringReferences.GetString(
                                                        DataOvlReference.WearUseItemStrings
                                                            .WEARING_AMULET) + "\n" +
                                                    GameReferences.Instance.DataOvlRef.StringReferences.GetString(
                                                        DataOvlReference.WearUseItemStrings
                                                            .SPACE_OF_LORD_BRITISH_DOT_N), false);
                    break;
                case LordBritishArtifact.ArtifactType.Crown:
                    turnResults.PushOutputToConsole(GameReferences.Instance.DataOvlRef.StringReferences.GetString(
                                                        DataOvlReference.WearUseItemStrings
                                                            .CROWN_N_N) + "\n" +
                                                    GameReferences.Instance.DataOvlRef.StringReferences.GetString(
                                                        DataOvlReference.WearUseItemStrings
                                                            .DON_THE_CROWN) + "\n" + GameReferences.Instance.DataOvlRef
                                                        .StringReferences.GetString(
                                                            DataOvlReference.WearUseItemStrings
                                                                .SPACE_OF_LORD_BRITISH_DOT_N), false);
                    break;
                case LordBritishArtifact.ArtifactType.Sceptre:
                    turnResults.PushOutputToConsole(GameReferences.Instance.DataOvlRef.StringReferences.GetString(
                                                        DataOvlReference.WearUseItemStrings
                                                            .SCEPTRE_N_N) + "\n" +
                                                    GameReferences.Instance.DataOvlRef.StringReferences.GetString(
                                                        DataOvlReference.WearUseItemStrings
                                                            .WIELD_SCEPTRE) + "\n" + GameReferences.Instance.DataOvlRef
                                                        .StringReferences.GetString(
                                                            DataOvlReference.WearUseItemStrings
                                                                .SPACE_OF_LORD_BRITISH_DOT_N), false);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(lordBritishArtifact),
                        @"Tried to use an undefined lordBritishArtifact");
            }

            return AdvanceTime(N_DEFAULT_ADVANCE_TIME, turnResults);
        }

        [SuppressMessage("ReSharper", "UnusedMember.Global")]
        public List<VirtualMap.AggressiveMapUnitInfo> TryToUseMoonstone(Moonstone moonstone, out bool bMoonstoneBuried,
            TurnResults turnResults)
        {
            if (State.TheVirtualMap.CurrentMap is not LargeMap largeMap)
            {
                bMoonstoneBuried = false;
                return AdvanceTime(N_DEFAULT_ADVANCE_TIME, turnResults);
            }

            bMoonstoneBuried = false;

            if (!largeMap.IsAllowedToBuryMoongate())
            {
                turnResults.PushOutputToConsole(GameReferences.Instance.DataOvlRef.StringReferences.GetString(
                                                    DataOvlReference.ExclaimStrings
                                                        .MOONSTONE_SPACE) +
                                                GameReferences.Instance.DataOvlRef.StringReferences.GetString(
                                                    DataOvlReference.ExclaimStrings
                                                        .CANNOT_BE_BURIED_HERE_BANG_N), false);
                return AdvanceTime(N_DEFAULT_ADVANCE_TIME, turnResults);
            }

            State.TheMoongates.SetMoonstoneBuried(moonstone.MoongateIndex, true,
                State.TheVirtualMap.GetCurrent3DPosition());
            bMoonstoneBuried = true;

            turnResults.PushOutputToConsole(
                GameReferences.Instance.DataOvlRef.StringReferences.GetString(DataOvlReference.ExclaimStrings
                    .MOONSTONE_SPACE) +
                GameReferences.Instance.DataOvlRef.StringReferences.GetString(DataOvlReference.ExclaimStrings
                    .BURIED_BANG_N),
                false);
            return AdvanceTime(N_DEFAULT_ADVANCE_TIME, turnResults);
        }

        [SuppressMessage("ReSharper", "UnusedMember.Global")]
        public List<VirtualMap.AggressiveMapUnitInfo> TryToUsePotion(Potion potion, PlayerCharacterRecord record,
            out bool bSucceeded, out MagicReference.SpellWords spell, TurnResults turnResults)
        {
            bSucceeded = true;

            Debug.Assert(potion.Quantity > 0, $"Can't use potion {potion} because you have quantity {potion.Quantity}");
            if (potion.Quantity <= 0)
            {
                // this is a soft fail in case you try to use a potion but don't have any... 
                // I want to control the number of hard failures
                bSucceeded = false;
                turnResults.PushOutputToConsole(
                    $"Can't use potion {potion} because you have quantity {potion.Quantity}\n", false);
                spell = MagicReference.SpellWords.Mani;
                return new List<VirtualMap.AggressiveMapUnitInfo>();
            }

            spell = _potionColorToSpellMap[potion.Color];
            if (State.TheVirtualMap.CurrentMap is CombatMap combatMap) combatMap.AdvanceToNextCombatMapUnit();

            potion.Quantity--;

            turnResults.PushOutputToConsole($"{potion.Color} Potion\n", false);
            //ActionUseDrankPotion
            turnResults.PushTurnResult(new DrankPotion(potion.Color, _potionColorToSpellMap[potion.Color]));

            switch (potion.Color)
            {
                case Potion.PotionColor.Blue:
                    // awaken
                    record.WakeUp();
                    turnResults.PushOutputToConsole("Awoken!", false);
                    break;
                case Potion.PotionColor.Yellow:
                    // lesser heal - mani
                    int nHealedPoints = record.CastSpellMani();
                    bSucceeded = nHealedPoints >= 0;
                    turnResults.PushOutputToConsole(
                        bSucceeded
                            ? GameReferences.Instance.DataOvlRef.StringReferences.GetString(DataOvlReference.ExclaimStrings
                                .HEALED_BANG_N)
                            : GameReferences.Instance.DataOvlRef.StringReferences.GetString(DataOvlReference.ExclaimStrings
                                .FAILED_BANG_N), false);
                    break;
                case Potion.PotionColor.Red:
                    // cure poison
                    record.Cure();
                    turnResults.PushOutputToConsole(
                        GameReferences.Instance.DataOvlRef.StringReferences.GetString(DataOvlReference.ExclaimStrings
                            .POISON_CURED_BANG_N), false);
                    break;
                case Potion.PotionColor.Green:
                    // poison user
                    bool bWasPoisoned = record.Stats.Poison();
                    if (bWasPoisoned)
                        turnResults.PushOutputToConsole(GameReferences.Instance.DataOvlRef.StringReferences.GetString(
                            DataOvlReference.ExclaimStrings
                                .POISONED_BANG_N), false);
                    else
                        turnResults.PushOutputToConsole(GameReferences.Instance.DataOvlRef.StringReferences.GetString(
                            DataOvlReference.ExclaimStrings
                                .NO_EFFECT_BANG), false);
                    break;
                case Potion.PotionColor.Orange:
                    // sleep
                    CastSleep(turnResults, record, out bool _);
                    break;
                case Potion.PotionColor.Purple:
                    // turn me into a rat
                    record.TurnIntoRat();
                    turnResults.PushOutputToConsole(GameReferences.Instance.DataOvlRef.StringReferences.GetString(
                        DataOvlReference.ExclaimStrings
                            .POOF_BANG_N), false);
                    break;
                case Potion.PotionColor.Black:
                    // invisibility
                    record.TurnInvisible();
                    turnResults.PushOutputToConsole(GameReferences.Instance.DataOvlRef.StringReferences.GetString(
                        DataOvlReference.ExclaimStrings
                            .INVISIBLE_BANG_N), false);
                    break;
                case Potion.PotionColor.White:
                    // x-ray
                    bSucceeded = true; //_random.Next() % 2 == 0;
                    // bajh: failing this seems pretty dumb to me - why would it fail?
                    if (bSucceeded)
                    {
                        turnResults.PushOutputToConsole("X-Ray!", false);
                    }

                    // if you fail with the x-ray then 
                    //CastSleep(turnResults, record, out bool _);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(potion), @"Tried to use an undefined potion");
            }

            return AdvanceTime(N_DEFAULT_ADVANCE_TIME, turnResults);
        }

        [SuppressMessage("ReSharper", "UnusedMember.Global")]
        public List<VirtualMap.AggressiveMapUnitInfo> TryToUseScroll(Scroll scroll, PlayerCharacterRecord record,
            TurnResults turnResults)
        {
            turnResults.PushTurnResult(new ReadScroll(scroll.ScrollSpell, record, record));

            turnResults.PushOutputToConsole($"Scroll: {scroll.ScrollSpell}\n\nA-la-Kazam!", false);

            return AdvanceTime(N_DEFAULT_ADVANCE_TIME, turnResults);
        }

        [SuppressMessage("ReSharper", "UnusedMember.Global")]
        public List<VirtualMap.AggressiveMapUnitInfo> TryToUseShadowLordShard(ShadowlordShard shadowlordShard,
            TurnResults turnResults)
        {
            switch (shadowlordShard.Shard)
            {
                case ShadowlordShard.ShardType.Falsehood:
                    turnResults.PushOutputToConsole(GetThouHolds(
                        GameReferences.Instance.DataOvlRef.StringReferences.GetString(DataOvlReference.ShadowlordStrings
                            .FALSEHOOD_DOT)), false);
                    break;
                case ShadowlordShard.ShardType.Hatred:
                    turnResults.PushOutputToConsole(GetThouHolds(
                        GameReferences.Instance.DataOvlRef.StringReferences.GetString(DataOvlReference.ShadowlordStrings
                            .HATRED_DOT)), false);
                    break;
                case ShadowlordShard.ShardType.Cowardice:
                    turnResults.PushOutputToConsole(GetThouHolds(
                        GameReferences.Instance.DataOvlRef.StringReferences.GetString(DataOvlReference.ShadowlordStrings
                            .COWARDICE_DOT)), false);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(shadowlordShard),
                        @"Tried to use an undefined shadowlordShard");
            }

            return AdvanceTime(N_DEFAULT_ADVANCE_TIME, turnResults);
        }

        [SuppressMessage("ReSharper", "UnusedMember.Global")]
        public List<VirtualMap.AggressiveMapUnitInfo> TryToUseSpecialItem(SpecialItem spcItem, out bool bWasUsed,
            TurnResults turnResults)
        {
            bWasUsed = true;
            switch (spcItem.ItemType)
            {
                case SpecialItem.SpecificItemType.Carpet:
                    UseMagicCarpet(turnResults, out bWasUsed);
                    break;
                case SpecialItem.SpecificItemType.Grapple:
                    turnResults.PushOutputToConsole("Grapple\n\nYou need to K-limb with it!", false);
                    break;
                case SpecialItem.SpecificItemType.Spyglass:
                    turnResults.PushOutputToConsole(GameReferences.Instance.DataOvlRef.StringReferences.GetString(
                        DataOvlReference.WearUseItemStrings.SPYGLASS_N_N), false);
                    break;
                case SpecialItem.SpecificItemType.HMSCape:
                    turnResults.PushOutputToConsole(GameReferences.Instance.DataOvlRef.StringReferences.GetString(
                        DataOvlReference.WearUseItemStrings.PLANS_N_N), false);
                    break;
                case SpecialItem.SpecificItemType.PocketWatch:
                    turnResults.PushOutputToConsole(GameReferences.Instance.DataOvlRef.StringReferences.GetString(
                        DataOvlReference.WearUseItemStrings
                            .WATCH_N_N_THE_POCKET_W_READS) + " " + State.TheTimeOfDay.FormattedTime, false);
                    break;
                case SpecialItem.SpecificItemType.BlackBadge:
                    turnResults.PushOutputToConsole(GameReferences.Instance.DataOvlRef.StringReferences.GetString(
                        DataOvlReference.WearUseItemStrings.BADGE_N_N), false);
                    if (State.CharacterRecords.WearingBlackBadge)
                    {
                        turnResults.PushOutputToConsole(GameReferences.Instance.DataOvlRef.StringReferences.GetString(
                            DataOvlReference.WearUseItemStrings.REMOVED), false);
                    }
                    else
                    {
                        turnResults.PushOutputToConsole(GameReferences.Instance.DataOvlRef.StringReferences.GetString(
                            DataOvlReference.WearUseItemStrings.BADGE_WORN_BANG_N), false);
                    }

                    State.CharacterRecords.WearingBlackBadge = !State.CharacterRecords.WearingBlackBadge;
                    break;
                case SpecialItem.SpecificItemType.WoodenBox:
                    turnResults.PushOutputToConsole(GameReferences.Instance.DataOvlRef.StringReferences.GetString(
                        DataOvlReference.WearUseItemStrings.BOX_N_HOW_N), false);
                    break;
                case SpecialItem.SpecificItemType.Sextant:
                    turnResults.PushOutputToConsole(GameReferences.Instance.DataOvlRef.StringReferences.GetString(
                        DataOvlReference.WearUseItemStrings.SEXTANT_N_N), false);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(spcItem),
                        @"Tried to use an unknown special item");
            }

            return AdvanceTime(N_DEFAULT_ADVANCE_TIME, turnResults);
        }

        /// <summary>
        ///     eXit the current vehicle you are boarded on
        /// </summary>
        /// <param name="bWasSuccessful">did you successfully eXit the vehicle</param>
        /// <param name="turnResults"></param>
        /// <returns>string to print out for user</returns>
        [SuppressMessage("ReSharper", "UnusedMember.Global")]
        public List<VirtualMap.AggressiveMapUnitInfo> TryToXit(out bool bWasSuccessful, TurnResults turnResults)
        {
            if (State.TheVirtualMap.CurrentMap is not RegularMap regularMap)
                throw new Ultima5ReduxException("TryToXit on non-regular map");

            bWasSuccessful = true;

            MapUnit unboardedMapUnit =
                regularMap.XitCurrentMapUnit(out string retStr);

            bWasSuccessful = unboardedMapUnit != null;

            turnResults.PushOutputToConsole(retStr);

            turnResults.PushTurnResult(new BasicResult(bWasSuccessful
                ? TurnResult.TurnResultType.ActionXitSuccess
                : TurnResult.TurnResultType.ActionXitWhat));

            return AdvanceTime(N_DEFAULT_ADVANCE_TIME, turnResults);
        }

        /// <summary>
        ///     Yell to hoist or furl the sails
        /// </summary>
        /// <param name="bSailsHoisted">are they now hoisted?</param>
        /// <param name="turnResults"></param>
        /// <returns>output string</returns>
        [SuppressMessage("ReSharper", "UnusedMember.Global")]
        public List<VirtualMap.AggressiveMapUnitInfo> TryToYellForSails(out bool bSailsHoisted, TurnResults turnResults)
        {
            if (State.TheVirtualMap.CurrentMap is not RegularMap regularMap)
                throw new Ultima5ReduxException("TryToXit on non-regular map");

            Debug.Assert(regularMap.GetAvatarMapUnit().CurrentBoardedMapUnit is Frigate);

            if (regularMap.GetAvatarMapUnit().CurrentBoardedMapUnit is not Frigate avatarsFrigate)
                throw new Ultima5ReduxException("Tried get Avatar's frigate, but it was null");

            avatarsFrigate.SailsHoisted = !avatarsFrigate.SailsHoisted;
            bSailsHoisted = avatarsFrigate.SailsHoisted;
            if (bSailsHoisted)
            {
                turnResults.PushOutputToConsole(GameReferences.Instance.DataOvlRef.StringReferences.GetString(
                    DataOvlReference.KeypressCommandsStrings
                        .YELL) + GameReferences.Instance.DataOvlRef.StringReferences
                    .GetString(DataOvlReference.YellingStrings.HOIST_BANG_N).Trim());
                turnResults.PushTurnResult(new BasicResult(TurnResult.TurnResultType.ActionYellSailsHoisted));
                return AdvanceTime(N_DEFAULT_ADVANCE_TIME, turnResults);
            }

            turnResults.PushOutputToConsole(
                GameReferences.Instance.DataOvlRef.StringReferences.GetString(DataOvlReference.KeypressCommandsStrings
                    .YELL) +
                GameReferences.Instance.DataOvlRef.StringReferences
                    .GetString(DataOvlReference.YellingStrings.FURL_BANG_N)
                    .Trim());
            turnResults.PushTurnResult(new BasicResult(TurnResult.TurnResultType.ActionYellSailsFurl));
            return AdvanceTime(N_DEFAULT_ADVANCE_TIME, turnResults);
        }

        [SuppressMessage("ReSharper", "UnusedMember.Global")]
        public void YellWord(string word)
        {
            // not yet implemented
        }
    }
}