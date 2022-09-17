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
using Ultima5Redux.MapUnits.NonPlayerCharacters.ShoppeKeepers;
using Ultima5Redux.MapUnits.SeaFaringVessels;
using Ultima5Redux.MapUnits.TurnResults;
using Ultima5Redux.MapUnits.TurnResults.SpecificTurnResults;
using Ultima5Redux.PlayerCharacters;
using Ultima5Redux.PlayerCharacters.Inventory;
using Ultima5Redux.References;
using Ultima5Redux.References.Maps;
using Ultima5Redux.References.PlayerCharacters.Inventory;

// ProcessXXX methods will take a TurnResults and take care of all messages and command pushing
// TryToXXXX methods directly reflective of the user executing the command and could have complex branching logic 

namespace Ultima5Redux
{
    [SuppressMessage("ReSharper", "MemberCanBePrivate.Global")]
    [SuppressMessage("ReSharper", "UnusedMethodReturnValue.Global")]
    public class World
    {
        public enum KlimbResult { Success, SuccessFell, CantKlimb, RequiresDirection }

        /// <summary>
        ///     Special things that can be looked at in the world that will require special consideration
        /// </summary>
        // ReSharper disable once UnusedMember.Global
        public enum SpecialLookCommand { None, Sign, GemCrystal }

        public enum TryToAttackResult
        {
            Uninitialized, NothingToAttack, BrokenMirror, CombatMapEnemy, CombatMapNpc, NpcMurder, OnlyOnFoot,
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

        private readonly Random _random = new();

        /// <summary>
        ///     The overworld map object
        /// </summary>
        private LargeMap OverworldMap { get; }

        /// <summary>
        ///     the underworld map object
        /// </summary>
        private LargeMap UnderworldMap { get; }

        /// <summary>
        ///     A collection of all the available small maps
        /// </summary>
        public SmallMaps AllSmallMaps { get; }

        /// <summary>
        ///     The current conversation object
        /// </summary>
        public Conversation CurrentConversation { get; private set; }

        /// <summary>
        ///     Ultima 5 data files directory (static content)
        /// </summary>
        public string DataDirectory { get; }

        public bool IsCombatMap => State.TheVirtualMap.IsCombatMap;

        /// <summary>
        ///     Is the Avatar positioned to fall? When falling from multiple floors this will be activated
        /// </summary>
        // ReSharper disable once UnusedAutoPropertyAccessor.Global
        public bool IsPendingFall { get; private set; }

        public bool MonsterAi { get; set; } = true;

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

            // build the overworld map
            OverworldMap = new LargeMap(Map.Maps.Overworld);

            // build the underworld map
            UnderworldMap = new LargeMap(Map.Maps.Underworld);

            AllSmallMaps = new SmallMaps();

            if (bLegacySave)
            {
                State = new GameState(bLoadedInitGam ? "" : SaveGameDirectory, AllSmallMaps, OverworldMap,
                    UnderworldMap, bUseExtendedSprites);
            }
            else
            {
                State = GameState.DeserializeFromFile(Path.Combine(SaveGameDirectory, FileConstants.NEW_SAVE_FILE));
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
                    return GameReferences.SpriteTileReferences.GetTileReferenceByName("ChairBackForward");
                case Point2D.Direction.Down:
                    return GameReferences.SpriteTileReferences.GetTileReferenceByName("ChairBackBack");
                case Point2D.Direction.Left:
                    return GameReferences.SpriteTileReferences.GetTileReferenceByName("ChairBackRight");
                case Point2D.Direction.Right:
                    return GameReferences.SpriteTileReferences.GetTileReferenceByName("ChairBackLeft");
                case Point2D.Direction.None:
                default:
                    throw new Ultima5ReduxException("Asked for a chair direction that I don't recognize");
            }
        }

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
        ///     Safe method to board a MapUnit and removing it from the world
        /// </summary>
        /// <param name="mapUnit"></param>
        private void BoardAndCleanFromWorld(MapUnit mapUnit)
        {
            // board the unit
            State.TheVirtualMap.TheMapUnits.GetAvatarMapUnit().BoardMapUnit(mapUnit);
            // clean it from the world so it no longer appears
            State.TheVirtualMap.TheMapUnits.ClearAndSetEmptyMapUnits(mapUnit);
        }

        /// <summary>
        ///     Checks if you can fire
        /// </summary>
        /// <returns></returns>
        private bool CanFireInPlace(TurnResults turnResults)
        {
            if (turnResults == null) throw new ArgumentNullException(nameof(turnResults));
            if (State.TheVirtualMap.IsLargeMap)
            {
                if (State.TheVirtualMap.IsAvatarInFrigate)
                {
                    return true;
                }

                turnResults.PushOutputToConsole(
                    GameReferences.DataOvlRef.StringReferences.GetString(DataOvlReference.KeypressCommandsStrings
                        .FIRE) +
                    GameReferences.DataOvlRef.StringReferences.GetString(DataOvlReference.KeypressCommandsStrings
                        .D_WHAT));
                turnResults.PushTurnResult(new BasicResult(TurnResult.TurnResultType.ActionFireWhat));
                return false;
            }

            if (IsCombatMap)
            {
                turnResults.PushOutputToConsole(
                    GameReferences.DataOvlRef.StringReferences.GetString(DataOvlReference.KeypressCommandsStrings
                        .FIRE) +
                    GameReferences.DataOvlRef.StringReferences.GetString(DataOvlReference.ExclaimStrings
                        .DASH_NOT_HERE_BANG_N));
                turnResults.PushTurnResult(new BasicResult(TurnResult.TurnResultType.ActionFireNotHere));
                return false;
            }

            List<TileReference> cannonReferences = new()
            {
                GameReferences.SpriteTileReferences.GetTileReferenceByName("CannonUp"),
                GameReferences.SpriteTileReferences.GetTileReferenceByName("CannonLeft"),
                GameReferences.SpriteTileReferences.GetTileReferenceByName("CannonRight"),
                GameReferences.SpriteTileReferences.GetTileReferenceByName("CannonDown"),
            };
            // small map
            // if a cannon is any of the four directions
            // 180 - 183
            if (State.TheVirtualMap.AreAnyTilesWithinFourDirections(State.TheVirtualMap.CurrentPosition.XY,
                    cannonReferences)) return true;

            turnResults.PushOutputToConsole(
                GameReferences.DataOvlRef.StringReferences.GetString(DataOvlReference.KeypressCommandsStrings
                    .FIRE) +
                GameReferences.DataOvlRef.StringReferences.GetString(DataOvlReference.KeypressCommandsStrings
                    .D_WHAT));
            turnResults.PushTurnResult(new BasicResult(TurnResult.TurnResultType.ActionFireWhat));
            return false;
        }

        private void CastSleep(TurnResults turnResults, PlayerCharacterRecord record, out bool bWasPutToSleep)
        {
            bWasPutToSleep = record.Stats.Sleep();
            turnResults.PushOutputToConsole(
                GameReferences.DataOvlRef.StringReferences.GetString(DataOvlReference.ExclaimStrings.SLEPT_BANG_N),
                false);
        }

        /// <summary>
        ///     Gets a +/- 1 x/y adjustment based on the current position and given direction
        /// </summary>
        /// <param name="direction">direction to go</param>
        /// <param name="xAdjust">output X adjustment</param>
        /// <param name="yAdjust">output Y adjustment</param>
        private void GetAdjustments(Point2D.Direction direction, out int xAdjust, out int yAdjust)
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

        private string GetMovementVerb(Avatar avatar, Point2D.Direction direction, bool bManualMovement)
        {
            switch (avatar.CurrentAvatarState)
            {
                case Avatar.AvatarState.Regular:
                    return GameReferences.DataOvlRef.StringReferences.GetDirectionString(direction);
                case Avatar.AvatarState.Carpet:
                    return GameReferences.DataOvlRef.StringReferences.GetString(DataOvlReference.WorldStrings.FLY) +
                           GameReferences.DataOvlRef.StringReferences.GetDirectionString(direction);
                case Avatar.AvatarState.Horse:
                    return GameReferences.DataOvlRef.StringReferences.GetString(DataOvlReference.WorldStrings
                               .RIDE) +
                           GameReferences.DataOvlRef.StringReferences.GetDirectionString(direction);
                case Avatar.AvatarState.Frigate:
                    if (avatar.AreSailsHoisted)
                    {
                        if (bManualMovement)
                        {
                            return GameReferences.DataOvlRef.StringReferences.GetString(
                                DataOvlReference.WorldStrings
                                    .HEAD) + GameReferences.DataOvlRef.StringReferences.GetDirectionString(direction);
                        }

                        return GameReferences.DataOvlRef.StringReferences.GetDirectionString(direction);
                    }
                    else
                    {
                        return
                            GameReferences.DataOvlRef.StringReferences.GetDirectionString(direction) +
                            GameReferences.DataOvlRef.StringReferences.GetString(DataOvlReference.WorldStrings
                                .ROWING);
                    }

                case Avatar.AvatarState.Skiff:
                    return GameReferences.DataOvlRef.StringReferences.GetString(DataOvlReference.WorldStrings.ROW) +
                           GameReferences.DataOvlRef.StringReferences.GetDirectionString(direction);
                case Avatar.AvatarState.Hidden:
                default:
                    throw new ArgumentOutOfRangeException(nameof(avatar.CurrentAvatarState), avatar.CurrentAvatarState,
                        null);
            }
        }

        private bool IsAllowedToBuryMoongate()
        {
            if (State.TheVirtualMap.LargeMapOverUnder != Map.Maps.Overworld &&
                State.TheVirtualMap.LargeMapOverUnder != Map.Maps.Underworld)
                return false;

            // don't bury one on top of the other
            if (State.TheMoongates.IsMoonstoneBuried(State.TheVirtualMap.CurrentPosition.XYZ)) return false;

            // we check the current terrain and make sure it's buriable
            TileReference tileRef = State.TheVirtualMap.GetTileReferenceOnCurrentTile();
            return GameReferences.SpriteTileReferences.IsMoonstoneBuriable(tileRef.Index);
        }

        private bool IsLeavingMap(Point2D xyProposedPosition)
        {
            return (xyProposedPosition.IsOutOfRange(State.TheVirtualMap.NumberOfColumnTiles - 1,
                State.TheVirtualMap.NumberOfRowTiles - 1));
        }

        private void MurderNpc(NonPlayerCharacter npc)
        {
            npc.NPCState.IsDead = true;
            State.Karma -= 10;
            State.TheVirtualMap.IsWantedManByThePoPo = true;
        }

        /// <summary>
        ///     Processes any damage effects as you advance time, this can include getting
        /// </summary>
        /// <returns></returns>
        private void ProcessDamageOnAdvanceTimeNonCombat(TurnResults turnResults)
        {
            if (turnResults == null) throw new ArgumentNullException(nameof(turnResults));

            Avatar.AvatarState currentAvatarState =
                State.TheVirtualMap.TheMapUnits.GetAvatarMapUnit().CurrentAvatarState;
            TileReference currentTileReference = State.TheVirtualMap.GetTileReferenceOnCurrentTile();

            // swamp - we poison them, but the actual damage occurs further down in case they were already poisoned
            if (currentAvatarState != Avatar.AvatarState.Carpet && currentTileReference.Index == 4)
            {
                bool bWasPoisoned = State.CharacterRecords.SteppedOnSwamp();
                if (bWasPoisoned)
                {
                    turnResults.PushOutputToConsole(GameReferences.DataOvlRef.StringReferences.GetString(
                        DataOvlReference.ExclaimStrings.POISONED_BANG_N), false);
                }
            }
            // if on lava
            else if (currentTileReference.Index == 143)
            {
                State.CharacterRecords.SteppedOnLava();
                turnResults.PushTurnResult(new BasicResult(TurnResult.TurnResultType.DamageOverTimeBurning));
                turnResults.PushOutputToConsole("Burning!", false);
            }

            // if already poisoned
            State.CharacterRecords.ProcessTurn(turnResults);
        }

        private List<VirtualMap.AggressiveMapUnitInfo> ProcessOpenDoor(Point2D xy, ref bool bWasSuccessful,
            TurnResults turnResults, TileReference tileReference)
        {
            bool bIsDoorMagical = GameReferences.SpriteTileReferences.IsDoorMagical(tileReference.Index);
            bool bIsDoorLocked = GameReferences.SpriteTileReferences.IsDoorLocked(tileReference.Index);

            if (bIsDoorMagical || bIsDoorLocked)
            {
                turnResults.PushOutputToConsole(GameReferences.DataOvlRef.StringReferences.GetString(
                    DataOvlReference.OpeningThingsStrings
                        .LOCKED_N), false);
                turnResults.PushTurnResult(new BasicResult(TurnResult.TurnResultType.ActionOpenDoorLocked));
                return AdvanceTime(N_DEFAULT_ADVANCE_TIME, turnResults);
            }

            State.TheVirtualMap.SetOverridingTileReferece(
                GameReferences.SpriteTileReferences.GetTileReferenceByName("BrickFloor"), xy);

            Map currentMap = State.TheVirtualMap.CurrentMap;
            currentMap.SetOpenDoor(xy);

            bWasSuccessful = true;
            turnResults.PushOutputToConsole(
                GameReferences.DataOvlRef.StringReferences.GetString(DataOvlReference.OpeningThingsStrings.OPENED),
                false);
            turnResults.PushTurnResult(new BasicResult(TurnResult.TurnResultType.ActionOpenDoorOpened));

            return AdvanceTime(N_DEFAULT_ADVANCE_TIME, turnResults);
        }

        /// <summary>
        ///     Use a magic carpet from your inventory
        /// </summary>
        /// <param name="bWasUsed">was the magic carpet used?</param>
        /// <returns>string to print and show user</returns>
        private void UseMagicCarpet(TurnResults turnResults, out bool bWasUsed)
        {
            if (IsCombatMap)
            {
                bWasUsed = false;
                turnResults.PushOutputToConsole(GameReferences.DataOvlRef.StringReferences.GetString(
                    DataOvlReference.ExclaimStrings
                        .NOT_HERE_BANG), false);
                return;
            }

            bWasUsed = true;
            Debug.Assert(State.PlayerInventory.SpecializedItems.Items[SpecialItem.SpecificItemType.Carpet]
                .HasOneOrMore);

            if (State.TheVirtualMap.TheMapUnits.GetAvatarMapUnit().IsAvatarOnBoardedThing)
            {
                bWasUsed = false;
                turnResults.PushOutputToConsole(GameReferences.DataOvlRef.StringReferences.GetString(
                    DataOvlReference.WearUseItemStrings
                        .ONLY_ON_FOOT), false);
                return;
            }

            State.PlayerInventory.SpecializedItems.Items[SpecialItem.SpecificItemType.Carpet].Quantity--;
            MagicCarpet carpet = State.TheVirtualMap.TheMapUnits.CreateMagicCarpet(
                State.TheVirtualMap.CurrentPosition.XY,
                State.TheVirtualMap.TheMapUnits.GetAvatarMapUnit().Direction,
                out int _);
            BoardAndCleanFromWorld(carpet);
            turnResults.PushOutputToConsole(GameReferences.DataOvlRef.StringReferences.GetString(DataOvlReference
                .WearUseItemStrings
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

            if (IsCombatMap)
            {
                turnResults.PushTurnResult(new BasicResult(TurnResult.TurnResultType.Ignore));
                ProcessDamageOnAdvanceTimeInCombat(turnResults);
            }
            else
            {
                ProcessDamageOnAdvanceTimeNonCombat(turnResults);

                aggressiveMapUnitInfos = State.TheVirtualMap.GetAggressiveMapUnitInfo(turnResults);
                State.TheVirtualMap.MoveMapUnitsToNextMove(aggressiveMapUnitInfos);
                VirtualMap.AggressiveMapUnitInfo aggressiveMapUnitInfo = null;
                if (bNoAggression)
                {
                    State.TheVirtualMap.GenerateAndCleanupEnemies(State.TheTimeOfDay.MinutesSinceBeginning);
                }
                else
                {
                    // this routine will check to see if a combat map load occured - if so then we only focus on it
                    // and ignore all other work
                    if (MonsterAi)
                    {
                        State.TheVirtualMap.ProcessAggressiveMapUnitAttacks(State.CharacterRecords,
                            aggressiveMapUnitInfos, out aggressiveMapUnitInfo, turnResults);
                    }

                    // if there is an individual aggressive map unit, then we know that we are going to load a combat map
                    if (aggressiveMapUnitInfo != null)
                    {
                        Debug.Assert(aggressiveMapUnitInfo.CombatMapReference != null);
                        // issue here is I need to be able to tell the caller to load a combat map

                        MapUnitPosition avatarPosition = State.TheVirtualMap.TheMapUnits.CurrentAvatarPosition;
                        TileReference currentTile = State.TheVirtualMap.CurrentMap
                            .GetTileReference(avatarPosition.XY);

                        if (!State.TheVirtualMap.IsAvatarRidingSomething &&
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
                            State.TheVirtualMap.TheMapUnits.ClearMapUnit(aggressiveMapUnitInfo.AttackingMapUnit);

                            State.TheVirtualMap.LoadCombatMapWithCalculation(aggressiveMapUnitInfo.CombatMapReference,
                                State.CharacterRecords, aggressiveMapUnitInfo.AttackingMapUnit);

                            turnResults.PushTurnResult(new BasicResult(TurnResult.TurnResultType.CombatMapLoaded));
                        }
                    }
                    else
                    {
                        State.TheVirtualMap.GenerateAndCleanupEnemies(State.TheTimeOfDay.MinutesSinceBeginning);
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
        /// <returns>A conversation object to be used to follow along with the conversation</returns>
        public Conversation CreateConversationAndBegin(NonPlayerCharacterState npcState,
            Conversation.EnqueuedScriptItem enqueuedScriptItem)
        {
            CurrentConversation = new Conversation(State, npcState);

            CurrentConversation.EnqueuedScriptItemCallback += enqueuedScriptItem;

            AdvanceClockNoComputation(N_DEFAULT_ADVANCE_TIME);

            return CurrentConversation;
        }


        /// <summary>
        ///     Gets the angle of the 360 rotation of all moons where Sun is 0degrees (straight up) at 12pm Noon
        /// </summary>
        /// <returns>0-359 degrees</returns>
        // ReSharper disable once UnusedMember.Global
        public float GetMoonAngle()
        {
            return MoonPhaseReferences.GetMoonAngle(State.TheTimeOfDay);
        }

        /// <summary>
        ///     Gets the teleport location of the moongate the avatar is presently on
        ///     Avatar MUST be on a moongate teleport location
        /// </summary>
        /// <returns>the coordinates</returns>
        public Point3D GetMoongateTeleportLocation()
        {
            Debug.Assert(State.TheVirtualMap.IsLargeMap);

            return State.TheMoongates.GetMoongatePosition(
                (int)GameReferences.MoonPhaseRefs.GetMoonGateMoonPhase(State.TheTimeOfDay));
        }

        /// <summary>
        ///     Determines if the current tile the Avatar is on, is an ACTIVE moongate
        /// </summary>
        /// <returns>true if the Avatar is on an active moongate</returns>
        public bool IsAvatarOnActiveMoongate()
        {
            if (!State.TheVirtualMap.IsLargeMap || State.TheTimeOfDay.IsDayLight) return false;

            return State.TheMoongates.IsMoonstoneBuried(State.TheVirtualMap.GetCurrent3DPosition());
        }

        public void ProcessDamageOnAdvanceTimeInCombat(TurnResults turnResults)
        {
            CombatPlayer combatPlayer = State.TheVirtualMap.CurrentCombatMap.CurrentCombatPlayer;

            combatPlayer?.Record.ProcessPlayerTurn(turnResults);
        }

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
        public List<VirtualMap.AggressiveMapUnitInfo> TryToAttackNonCombatMap(Point2D attackTargetPosition,
            out MapUnit mapUnit, out SingleCombatMapReference singleCombatMapReference,
            out TryToAttackResult tryToAttackResult, TurnResults turnResults)
        {
            singleCombatMapReference = null;
            mapUnit = null;

            TileReference tileReference = State.TheVirtualMap.GetTileReference(attackTargetPosition);

            // if you are attacking an unbroken mirror - then we break it and override the tile
            if (GameReferences.SpriteTileReferences.IsUnbrokenMirror(tileReference.Index))
            {
                turnResults.PushOutputToConsole(
                    GameReferences.DataOvlRef.StringReferences.GetString(DataOvlReference.TravelStrings.BROKEN),
                    false);
                TileReference brokenMirrorTileReference =
                    GameReferences.SpriteTileReferences.GetTileReferenceByName("MirrorBroken");
                State.TheVirtualMap.SetOverridingTileReferece(brokenMirrorTileReference,
                    attackTargetPosition);

                tryToAttackResult = TryToAttackResult.BrokenMirror;
                turnResults.PushTurnResult(new BasicResult(TurnResult.TurnResultType.ActionAttackBrokeMirror));
                return AdvanceTime(N_DEFAULT_ADVANCE_TIME, turnResults);
            }

            mapUnit = State.TheVirtualMap.GetTopVisibleMapUnit(attackTargetPosition, true);

            if (mapUnit is not { IsAttackable: true })
            {
                turnResults.PushOutputToConsole(GameReferences.DataOvlRef.StringReferences.GetString(
                    DataOvlReference.TravelStrings.NOTHING_TO_ATTACK), false);
                tryToAttackResult = TryToAttackResult.NothingToAttack;
                turnResults.PushTurnResult(new BasicResult(TurnResult.TurnResultType.ActionAttackNothingToAttack));
                return AdvanceTime(N_DEFAULT_ADVANCE_TIME, turnResults);
            }

            // we know there is a mapunit to attack at this point
            turnResults.PushOutputToConsole(
                GameReferences.DataOvlRef.StringReferences.GetString(DataOvlReference.TravelStrings.ATTACK) +
                mapUnit.FriendlyName, false);

            Avatar avatar = State.TheVirtualMap.TheMapUnits.GetAvatarMapUnit();

            // we get the combat map reference, if any - it also tells us if there should be a ranged attack in the overworld
            // instead of a combat map
            singleCombatMapReference =
                State.TheVirtualMap.GetCombatMapReferenceForAvatarAttacking(avatar.MapUnitPosition.XY,
                    attackTargetPosition, SingleCombatMapReference.Territory.Britannia);

            bool bIsMurderable = GameReferences.SpriteTileReferences.IsHeadOfBed(tileReference.Index) ||
                                 GameReferences.SpriteTileReferences.IsStocks(tileReference.Index) ||
                                 GameReferences.SpriteTileReferences.IsManacles(tileReference.Index);

            // if there is a mapunit - BUT - no 
            ////// NOTE - this doesn't make sense for the Avatar to attack like this
            if (singleCombatMapReference == null && !bIsMurderable)
            {
                // we were not able to attack, likely on a carpet or skiff and on the water
                // but may be other edge cases
                // if (missileType != CombatItemReference.MissileType.None)
                //     throw new Ultima5ReduxException(
                //         "Single combat map reference was null, but missile type wasn't null when avatar attacks");

                turnResults.PushOutputToConsole(
                    GameReferences.DataOvlRef.StringReferences.GetString(DataOvlReference.KeypressCommandsStrings
                        .ON_FOOT), false);
                tryToAttackResult = TryToAttackResult.OnlyOnFoot;

                turnResults.PushTurnResult(new BasicResult(TurnResult.TurnResultType.ActionAttackOnlyOnFoot));

                return AdvanceTime(N_DEFAULT_ADVANCE_TIME, turnResults);
            }

            tryToAttackResult = TryToAttackResult.Uninitialized;

            switch (mapUnit)
            {
                case Enemy:
                    State.TheVirtualMap.TheMapUnits.ClearMapUnit(mapUnit);
                    tryToAttackResult = TryToAttackResult.CombatMapEnemy;
                    turnResults.PushTurnResult(new BasicResult(TurnResult.TurnResultType.ActionAttackCombatMapEnemy));
                    break;
                case NonPlayerCharacter npc:
                    // if they are in bed or in the stocks then it's instadeath and you are a bad person!
                    if (bIsMurderable)
                    {
                        turnResults.PushOutputToConsole(
                            GameReferences.DataOvlRef.StringReferences.GetString(
                                DataOvlReference.TravelStrings.MURDERED), false);
                        tryToAttackResult = TryToAttackResult.NpcMurder;
                        turnResults.PushTurnResult(new BasicResult(TurnResult.TurnResultType.ActionAttackMurder));
                        MurderNpc(npc);
                        break;
                    }

                    //State.TheVirtualMap.TheMapUnits.ClearMapUnit(mapUnit);
                    npc.NPCState.IsDead = true;

                    turnResults.PushTurnResult(new BasicResult(TurnResult.TurnResultType.ActionAttackCombatMapNpc));
                    tryToAttackResult = TryToAttackResult.CombatMapNpc;

                    break;
            }

            if (tryToAttackResult is TryToAttackResult.CombatMapEnemy or TryToAttackResult.CombatMapNpc)
            {
                turnResults.PushOutputToConsole(mapUnit.FriendlyName + "\n" +
                                                GameReferences.DataOvlRef.StringReferences.GetString(
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
        public List<VirtualMap.AggressiveMapUnitInfo> TryToBoard(out bool bWasSuccessful, TurnResults turnResults)
        {
            if (turnResults == null) throw new ArgumentNullException(nameof(turnResults));

            MapUnit currentAvatarTileRef = State.TheVirtualMap.GetMapUnitOnCurrentTile();
            bWasSuccessful = true;

            if (currentAvatarTileRef is null || !currentAvatarTileRef.KeyTileReference.IsBoardable)
            {
                bWasSuccessful = false;
                // can't board it
                turnResults.PushOutputToConsole(GameReferences.DataOvlRef.StringReferences
                                                    .GetString(DataOvlReference.KeypressCommandsStrings.BOARD)
                                                    .Trim() + " " +
                                                GameReferences.DataOvlRef.StringReferences.GetString(
                                                    DataOvlReference.TravelStrings.WHAT));
                turnResults.PushTurnResult(new BasicResult(TurnResult.TurnResultType.ActionBoardWhat));
                return AdvanceTime(N_DEFAULT_ADVANCE_TIME, turnResults);
            }

            bool bAvatarIsBoarded = State.TheVirtualMap.IsAvatarRidingSomething;
            MapUnit boardableMapUnit = State.TheVirtualMap.GetMapUnitOnCurrentTile();
            if (boardableMapUnit is null)
                throw new Ultima5ReduxException(
                    "Tried to board something but the tile appears to be boardable without a MapUnit");

            // at this point we are certain that the current tile is boardable AND the we know if the avatar has already
            // boarded something
            string getOnFootResponse()
            {
                return GameReferences.DataOvlRef.StringReferences
                    .GetString(DataOvlReference.KeypressCommandsStrings.BOARD).Trim() + "\n" + GameReferences.DataOvlRef
                    .StringReferences.GetString(DataOvlReference.KeypressCommandsStrings.ON_FOOT).Trim();
            }

            string retStr = GameReferences.DataOvlRef.StringReferences
                .GetString(DataOvlReference.KeypressCommandsStrings.BOARD).Trim() + " " + boardableMapUnit.BoardXitName;

            switch (boardableMapUnit)
            {
                case MagicCarpet _ when bAvatarIsBoarded:
                case Horse _ when bAvatarIsBoarded:
                case Skiff _ when bAvatarIsBoarded:
                    bWasSuccessful = false;
                    turnResults.PushOutputToConsole(getOnFootResponse(), false);
                    turnResults.PushTurnResult(new BasicResult(TurnResult.TurnResultType.ActionBoardNoOnFoot));
                    return AdvanceTime(N_DEFAULT_ADVANCE_TIME, turnResults);
                case MagicCarpet _:
                    BoardAndCleanFromWorld(boardableMapUnit);
                    turnResults.PushTurnResult(new BasicResult(TurnResult.TurnResultType.ActionBoardCarpet));
                    break;
                case Horse _:
                    // delete or deactivate the horse we just mounted
                    BoardAndCleanFromWorld(boardableMapUnit);
                    turnResults.PushTurnResult(new BasicResult(TurnResult.TurnResultType.ActionBoardHorse));
                    break;
                case Skiff _:
                    BoardAndCleanFromWorld(boardableMapUnit);
                    turnResults.PushTurnResult(new BasicResult(TurnResult.TurnResultType.ActionBoardSkiff));
                    break;
                case Frigate boardableFrigate:
                {
                    if (bAvatarIsBoarded)
                    {
                        if (State.TheVirtualMap.IsAvatarRidingHorse)
                        {
                            bWasSuccessful = false;
                            turnResults.PushOutputToConsole(getOnFootResponse());
                            turnResults.PushTurnResult(new BasicResult(TurnResult.TurnResultType.ActionBoardNoOnFoot));
                            return AdvanceTime(N_DEFAULT_ADVANCE_TIME, turnResults);
                        }

                        if (State.TheVirtualMap.IsAvatarRidingCarpet)
                            // we tuck the carpet away
                            State.PlayerInventory.SpecializedItems.Items[SpecialItem.SpecificItemType.Carpet]
                                .Quantity++;
                        // add a skiff the the frigate
                        if (State.TheVirtualMap.IsAvatarInSkiff) boardableFrigate.SkiffsAboard++;
                    }

                    if (boardableFrigate.SkiffsAboard == 0)
                        retStr += GameReferences.DataOvlRef.StringReferences
                            .GetString(DataOvlReference.SleepTransportStrings.M_WARNING_NO_SKIFFS_N).TrimEnd();
                    BoardAndCleanFromWorld(boardableFrigate);
                    turnResults.PushTurnResult(new BasicResult(TurnResult.TurnResultType.ActionBoardFrigate));
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
        public List<VirtualMap.AggressiveMapUnitInfo> TryToEnterBuilding(Point2D xy, out bool bWasSuccessful,
            TurnResults turnResults)
        {
            if (turnResults == null) throw new ArgumentNullException(nameof(turnResults));

            bool isOnBuilding = GameReferences.LargeMapRef.IsMapXYEnterable(xy);

            if (!isOnBuilding)
            {
                turnResults.PushOutputToConsole(
                    GameReferences.DataOvlRef.StringReferences.GetString(DataOvlReference.WorldStrings.ENTER_SPACE) +
                    GameReferences.DataOvlRef.StringReferences.GetString(DataOvlReference.WorldStrings.WHAT), false);
                turnResults.PushTurnResult(new BasicResult(TurnResult.TurnResultType.ActionEnterWhat));
                bWasSuccessful = false;
                return AdvanceTime(N_DEFAULT_ADVANCE_TIME, turnResults);
            }

            SmallMapReferences.SingleMapReference.Location location = GameReferences.LargeMapRef.GetLocationByMapXY(xy);
            SmallMapReferences.SingleMapReference singleMap =
                GameReferences.SmallMapRef.GetSingleMapByLocation(location, 0);

            if (singleMap.MapType == Map.Maps.Dungeon)
            {
                string retStr =
                    GameReferences.DataOvlRef.StringReferences.GetString(DataOvlReference.WorldStrings.ENTER_SPACE) +
                    GameReferences.SmallMapRef.GetLocationTypeStr(location) + "\n" +
                    GameReferences.SmallMapRef.GetLocationName(location) +
                    "\nUnable to enter the dungeons at this time!";
                bWasSuccessful = false;
                turnResults.PushOutputToConsole(retStr, false);
                turnResults.PushTurnResult(new BasicResult(TurnResult.TurnResultType.ActionEnterDungeon));
                return new List<VirtualMap.AggressiveMapUnitInfo>();
            }

            State.TheVirtualMap.LoadSmallMap(singleMap);
            // set us to the front of the building
            State.TheVirtualMap.CurrentPosition.XY = SmallMapReferences.GetStartingXYByLocation();

            turnResults.PushOutputToConsole(
                GameReferences.DataOvlRef.StringReferences.GetString(DataOvlReference.WorldStrings.ENTER_SPACE) +
                GameReferences.SmallMapRef.GetLocationTypeStr(location) + "\n" +
                GameReferences.SmallMapRef.GetLocationName(location), false);
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
        public List<VirtualMap.AggressiveMapUnitInfo> TryToFire(Point2D.Direction direction,
            TurnResults turnResults, out Point2D cannonBallDestination)
        {
            // if (!CanFireInPlace(turnResults))
            //     throw new Ultima5ReduxException("Tried to fire, but are not able to - use CheckIfCanFire first");
            bool bCanFire = CanFireInPlace(turnResults);

            if (!bCanFire)
            {
                cannonBallDestination = null;
                return AdvanceTime(N_DEFAULT_ADVANCE_TIME, turnResults);
            }

            turnResults.PushTurnResult(new BasicResult(TurnResult.TurnResultType.ActionFireCannon));

            Avatar avatar = State.TheVirtualMap.TheMapUnits.GetAvatarMapUnit();
            cannonBallDestination = null;

            // TODO: need small map code for cannons like in LBs castle

            // if overworld
            if (!State.TheVirtualMap.IsLargeMap) return null;

            // we assume they are in a frigate
            if (!State.TheVirtualMap.IsAvatarInFrigate)
                throw new Ultima5ReduxException("can't fire on large map unless you're on a frigate");

            // make sure the boat is facing the correct direction given the direction of the cannon
            TileReference currentAvatarTileReference = avatar.IsAvatarOnBoardedThing
                ? avatar.GetBoardedTileReference()
                : avatar.KeyTileReference;

            bool bShipFacingUpDown = currentAvatarTileReference.Name.EndsWith("Up") ||
                                     currentAvatarTileReference.Name.EndsWith("Down");

            turnResults.PushOutputToConsole(
                GameReferences.DataOvlRef.StringReferences.GetString(DataOvlReference.KeypressCommandsStrings
                    .FIRE) + direction);
            // are we pointing the right direction to fire the cannons?
            if (((direction is Point2D.Direction.Down or Point2D.Direction.Up) && bShipFacingUpDown)
                || ((direction is Point2D.Direction.Left or Point2D.Direction.Right) && !bShipFacingUpDown))
            {
                turnResults.PushOutputToConsole(GameReferences.DataOvlRef.StringReferences.GetString(
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

                if (!State.TheVirtualMap.IsMapUnitOccupiedTile(adjustedPosition)) continue;

                // kill them
                MapUnit targetedMapUnit = State.TheVirtualMap.GetMapUnitsOnTile(adjustedPosition)[0];
                turnResults.PushOutputToConsole(
                    "Killed " + targetedMapUnit.FriendlyName, false);
                State.TheVirtualMap.TheMapUnits.ClearMapUnit(targetedMapUnit);
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
        public List<VirtualMap.AggressiveMapUnitInfo> TryToGetAThing(Point2D xy, out bool bGotAThing,
            out InventoryItem inventoryItem, TurnResults turnResults, Point2D.Direction direction)
        {
            bGotAThing = true;
            inventoryItem = null;

            TileReference tileReference = State.TheVirtualMap.GetTileReference(xy);

            if (State.TheVirtualMap.CurrentSingleMapReference == null)
                throw new Ultima5ReduxException("No single map is set in virtual map");

            // wall sconces - BORROWED!
            if (tileReference.Index == GameReferences.SpriteTileReferences.GetTileNumberByName("LeftSconce") ||
                tileReference.Index == GameReferences.SpriteTileReferences.GetTileNumberByName("RightSconce"))
            {
                State.PlayerInventory.TheProvisions.Items[ProvisionReferences.SpecificProvisionType.Torches].Quantity++;

                State.TheVirtualMap.SetOverridingTileReferece(
                    GameReferences.SpriteTileReferences.GetTileReferenceByName("BrickFloor"), xy);
                turnResults.PushOutputToConsole(
                    GameReferences.DataOvlRef.StringReferences.GetString(DataOvlReference.GetThingsStrings.BORROWED),
                    false);
                turnResults.PushTurnResult(new BasicResult(TurnResult.TurnResultType.ActionGetBorrowed));
                return AdvanceTime(N_DEFAULT_ADVANCE_TIME, turnResults);
            }

            MagicCarpet magicCarpet = State.TheVirtualMap.TheMapUnits.GetSpecificMapUnitByLocation<MagicCarpet>(
                State.TheVirtualMap.LargeMapOverUnder, xy, State.TheVirtualMap.CurrentSingleMapReference.Floor);
            if (magicCarpet != null)
            {
                // add the carpet to the players inventory and remove it from the map
                State.PlayerInventory.SpecializedItems.Items[SpecialItem.SpecificItemType.Carpet].Quantity++;
                State.TheVirtualMap.TheMapUnits.ClearAndSetEmptyMapUnits(magicCarpet);
                turnResults.PushOutputToConsole(GameReferences.DataOvlRef.StringReferences.GetString(
                    DataOvlReference.GetThingsStrings.A_MAGIC_CARPET), false);
                turnResults.PushTurnResult(new BasicResult(TurnResult.TurnResultType.ActionGetMagicCarpet));
                return AdvanceTime(N_DEFAULT_ADVANCE_TIME, turnResults);
            }

            if (tileReference.Index == (int)TileReference.SpriteIndex.WheatInField)
            {
                State.TheVirtualMap.SetOverridingTileReferece(
                    GameReferences.SpriteTileReferences.GetTileReference(TileReference.SpriteIndex
                        .PlowedField), xy);
                turnResults.PushOutputToConsole(GameReferences.DataOvlRef.StringReferences.GetString(
                    DataOvlReference.GetThingsStrings.CROPS_PICKED), false);
                State.PlayerInventory.TheProvisions.Food++;
                State.Karma--;
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
                                State.TheVirtualMap.SetOverridingTileReferece(
                                    GameReferences.SpriteTileReferences.GetTileReference(TileReference.SpriteIndex
                                        .TableFoodBottom), xy);
                                bAte = true;
                                break;
                            case (int)TileReference.SpriteIndex.TableFoodTop:
                                State.TheVirtualMap.SetOverridingTileReferece(
                                    GameReferences.SpriteTileReferences.GetTileReference(TileReference.SpriteIndex
                                        .TableMiddle), xy);
                                bAte = true;
                                break;
                        }

                        break;
                    case Point2D.Direction.Up:
                        switch (tileReference.Index)
                        {
                            case (int)TileReference.SpriteIndex.TableFoodBottom:
                                State.TheVirtualMap.SetOverridingTileReferece(
                                    GameReferences.SpriteTileReferences.GetTileReference(TileReference.SpriteIndex
                                        .TableMiddle), xy);
                                bAte = true;
                                // do nothing
                                break;
                            case (int)TileReference.SpriteIndex.TableFoodBoth:
                                State.TheVirtualMap.SetOverridingTileReferece(
                                    GameReferences.SpriteTileReferences.GetTileReference(TileReference.SpriteIndex
                                        .TableFoodTop), xy);
                                bAte = true;
                                break;
                            case (int)TileReference.SpriteIndex.TableFoodTop:
                                break;
                        }

                        break;
                }

                // else pass on by - something isn't quite right
                if (bAte)
                {
                    State.PlayerInventory.TheProvisions.Food++;
                    State.Karma--;
                    turnResults.PushOutputToConsole(GameReferences.DataOvlRef.StringReferences.GetString(
                        DataOvlReference.GetThingsStrings.MMM_DOT3), false);
                    return AdvanceTime(N_DEFAULT_ADVANCE_TIME, turnResults);
                }
                // we just fall through for the default handling if there is no food directly in front of us
            }

            if (State.TheVirtualMap.IsMapUnitOccupiedTile(xy))
            {
                MapUnit mapUnit = State.TheVirtualMap.GetTopVisibleMapUnit(xy, true);
                if (mapUnit is ItemStack { HasStackableItems: true } itemStack)
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
                        State.TheVirtualMap.TheMapUnits.ClearAndSetEmptyMapUnits(itemStack);
                        State.TheVirtualMap.CurrentMap.RecalculateWalkableTileForAllAstarsWithMapUnits(
                            itemStack.MapUnitPosition.XY,
                            State.TheVirtualMap.AllVisibleActiveMapUnits.ToList());
                        // State.TheVirtualMap.CurrentMap.GetAStarByMapUnit(itemStack)
                        //     .SetWalkable(itemStack.MapUnitPosition.XY, true);
                    }

                    turnResults.PushOutputToConsole(U5StringRef.ThouDostFind(invItem.FindDescription), false);
                    turnResults.PushTurnResult(new BasicResult(TurnResult.TurnResultType.ActionGetStackableItem));
                    return AdvanceTime(N_DEFAULT_ADVANCE_TIME, turnResults);
                }

                if (mapUnit is MoonstoneNonAttackingUnit moonstoneNonAttackingUnit)
                {
                    // get the Moonstone!
                    State.PlayerInventory.AddInventoryItemToInventory(moonstoneNonAttackingUnit.TheMoonstone);
                    inventoryItem = moonstoneNonAttackingUnit.TheMoonstone;
                    turnResults.PushOutputToConsole(
                        U5StringRef.ThouDostFind(moonstoneNonAttackingUnit.TheMoonstone.FindDescription), false);
                    turnResults.PushTurnResult(new BasicResult(TurnResult.TurnResultType.ActionGetMoonstone));
                    // get rid of it from the map
                    State.TheVirtualMap.TheMapUnits.ClearMapUnit(moonstoneNonAttackingUnit);
                    return AdvanceTime(N_DEFAULT_ADVANCE_TIME, turnResults);
                }
            }

            bGotAThing = false;
            turnResults.PushOutputToConsole(GameReferences.DataOvlRef.StringReferences.GetString(
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
                turnResults.PushOutputToConsole(GameReferences.DataOvlRef.StringReferences.GetString(
                    DataOvlReference.SleepTransportStrings
                        .NONE_OWNED_BANG_N), false);
                turnResults.PushTurnResult(new BasicResult(TurnResult.TurnResultType.ActionIgniteTorchNoTorch));
                return AdvanceTime(N_DEFAULT_ADVANCE_TIME, turnResults);
            }

            State.PlayerInventory.TheProvisions.Items[ProvisionReferences.SpecificProvisionType.Torches].Quantity--;
            State.TurnsToExtinguish = N_DEFAULT_NUMBER_OF_TURNS_FOR_TORCH;
            // this will trigger a re-read of time of day changes
            State.TheTimeOfDay.SetAllChangeTrackers();

            turnResults.PushOutputToConsole(GameReferences.DataOvlRef.StringReferences.GetString(
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
            TileReference tileReference = State.TheVirtualMap.GetTileReference(xy);

            bool isDoorInDirection = tileReference.IsOpenable;

            if (!isDoorInDirection)
            {
                turnResults.PushOutputToConsole(GameReferences.DataOvlRef.StringReferences.GetString(
                    DataOvlReference.OpeningThingsStrings
                        .NO_LOCK), false);
                turnResults.PushTurnResult(new BasicResult(TurnResult.TurnResultType.ActionJimmyNoLock));
            }
            else
            {
                bool bIsDoorMagical = GameReferences.SpriteTileReferences.IsDoorMagical(tileReference.Index);
                bool bIsDoorLocked = GameReferences.SpriteTileReferences.IsDoorLocked(tileReference.Index);

                if (bIsDoorMagical)
                {
                    // we use up a key
                    State.PlayerInventory.TheProvisions.Items[ProvisionReferences.SpecificProvisionType.Keys]
                        .Quantity--;

                    // for now we will also just open the door so we can get around - will address when we have spells
                    State.TheVirtualMap.SetOverridingTileReferece(
                        GameReferences.SpriteTileReferences.IsDoorWithView(tileReference.Index)
                            ? GameReferences.SpriteTileReferences.GetTileReferenceByName("RegularDoorView")
                            : GameReferences.SpriteTileReferences.GetTileReferenceByName("RegularDoor"), xy);

                    bWasSuccessful = true;

                    turnResults.PushOutputToConsole(GameReferences.DataOvlRef.StringReferences.GetString(
                        DataOvlReference.OpeningThingsStrings
                            .KEY_BROKE), false);
                    turnResults.PushTurnResult(new BasicResult(TurnResult.TurnResultType.ActionJimmyKeyBroke));
                }
                else if (bIsDoorLocked)
                {
                    // we use up a key
                    State.PlayerInventory.TheProvisions.Items[ProvisionReferences.SpecificProvisionType.Keys]
                        .Quantity--;

                    // todo: bh: we will need to determine the likelihood of lock picking success, for now, we always succeed

                    State.TheVirtualMap.SetOverridingTileReferece(
                        GameReferences.SpriteTileReferences.IsDoorWithView(tileReference.Index)
                            ? GameReferences.SpriteTileReferences.GetTileReferenceByName("RegularDoorView")
                            : GameReferences.SpriteTileReferences.GetTileReferenceByName("RegularDoor"), xy);

                    bWasSuccessful = true;
                    turnResults.PushOutputToConsole(GameReferences.DataOvlRef.StringReferences.GetString(
                        DataOvlReference.OpeningThingsStrings
                            .UNLOCKED), false);
                    turnResults.PushTurnResult(new BasicResult(TurnResult.TurnResultType.ActionJimmyUnlocked));
                }
                else
                {
                    turnResults.PushOutputToConsole(GameReferences.DataOvlRef.StringReferences.GetString(
                        DataOvlReference.OpeningThingsStrings
                            .NO_LOCK), false);
                    turnResults.PushTurnResult(new BasicResult(TurnResult.TurnResultType.ActionJimmyNoLock));
                }
            }

            return AdvanceTime(N_DEFAULT_ADVANCE_TIME, turnResults);
        }

        /// <summary>
        ///     Climbs the ladder on the current tile that the Avatar occupies
        /// </summary>
        public List<VirtualMap.AggressiveMapUnitInfo> TryToKlimb(out KlimbResult klimbResult, TurnResults turnResults)
        {
            string getKlimbOutput(string output = "")
            {
                if (output == "")
                    return GameReferences.DataOvlRef.StringReferences.GetString(DataOvlReference.TravelStrings.KLIMB);
                return GameReferences.DataOvlRef.StringReferences.GetString(DataOvlReference.TravelStrings.KLIMB) +
                       output;
            }

            TileReference curTileRef = State.TheVirtualMap.GetTileReferenceOnCurrentTile();

            // if it's a large map, we either klimb with the grapple or don't klimb at all
            if (State.TheVirtualMap.IsLargeMap)
            {
                if (State.PlayerInventory.SpecializedItems.Items[SpecialItem.SpecificItemType.Grapple]
                    .HasOneOrMore)
                {
                    klimbResult = KlimbResult.RequiresDirection;
                    turnResults.PushOutputToConsole(getKlimbOutput(), false);
                    turnResults.PushTurnResult(new BasicResult(TurnResult.TurnResultType.ActionKlimbRequiresDirection));
                    return new List<VirtualMap.AggressiveMapUnitInfo>();
                }

                // we don't have a grapple, so we can't klimb
                klimbResult = KlimbResult.CantKlimb;
                turnResults.PushOutputToConsole(getKlimbOutput(
                    GameReferences.DataOvlRef.StringReferences.GetString(DataOvlReference.KlimbingStrings
                        .WITH_WHAT)), false);
                turnResults.PushTurnResult(new BasicResult(TurnResult.TurnResultType.ActionKlimbWithWhat));
                return AdvanceTime(N_DEFAULT_ADVANCE_TIME, turnResults);
            }

            // we can't klimb on the current tile, so we need to pick a direction
            if (!GameReferences.SpriteTileReferences.IsLadder(curTileRef.Index) &&
                !GameReferences.SpriteTileReferences.IsGrate(curTileRef.Index))
            {
                klimbResult = KlimbResult.RequiresDirection;
                turnResults.PushOutputToConsole(getKlimbOutput(), false);
                turnResults.PushTurnResult(new BasicResult(TurnResult.TurnResultType.ActionKlimbRequiresDirection));
                return new List<VirtualMap.AggressiveMapUnitInfo>();
            }

            if (State.TheVirtualMap.CurrentSingleMapReference == null)
                throw new Ultima5ReduxException("No single map is set in virtual map");

            SmallMapReferences.SingleMapReference.Location location =
                State.TheVirtualMap.CurrentSingleMapReference.MapLocation;
            int nCurrentFloor = State.TheVirtualMap.CurrentSingleMapReference.Floor;
            bool hasBasement = GameReferences.SmallMapRef.HasBasement(location);
            int nTotalFloors = GameReferences.SmallMapRef.GetNumberOfFloors(location);
            int nTopFloor = hasBasement ? nTotalFloors - 1 : nTotalFloors;

            TileReference tileReference = State.TheVirtualMap.GetTileReference(State.TheVirtualMap.CurrentPosition.XY);
            if (GameReferences.SpriteTileReferences.IsLadderDown(tileReference.Index) ||
                GameReferences.SpriteTileReferences.IsGrate(tileReference.Index))
            {
                if (hasBasement && nCurrentFloor >= 0 || nCurrentFloor > 0)
                {
                    State.TheVirtualMap.LoadSmallMap(
                        GameReferences.SmallMapRef.GetSingleMapByLocation(location, nCurrentFloor - 1),
                        State.TheVirtualMap.CurrentPosition.XY);
                    klimbResult = KlimbResult.Success;
                    turnResults.PushOutputToConsole(getKlimbOutput(
                            GameReferences.DataOvlRef.StringReferences.GetString(DataOvlReference.TravelStrings.DOWN)),
                        false);
                    turnResults.PushTurnResult(new BasicResult(TurnResult.TurnResultType.ActionKlimbDown));
                    return AdvanceTime(N_DEFAULT_ADVANCE_TIME, turnResults);
                }
            }
            // else if there is a ladder up and we are not yet on the top floor
            else if (GameReferences.SpriteTileReferences.IsLadderUp(tileReference.Index) &&
                     nCurrentFloor + 1 < nTopFloor)
            {
                State.TheVirtualMap.LoadSmallMap(
                    GameReferences.SmallMapRef.GetSingleMapByLocation(location, nCurrentFloor + 1),
                    State.TheVirtualMap.CurrentPosition.XY);
                klimbResult = KlimbResult.Success;
                turnResults.PushOutputToConsole(getKlimbOutput(
                    GameReferences.DataOvlRef.StringReferences.GetString(DataOvlReference.TravelStrings.UP)), false);
                turnResults.PushTurnResult(new BasicResult(TurnResult.TurnResultType.ActionKlimbUp));
                return AdvanceTime(N_DEFAULT_ADVANCE_TIME, turnResults);
            }

            klimbResult = KlimbResult.RequiresDirection;
            turnResults.PushOutputToConsole(getKlimbOutput(), false);
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
            TileReference tileReference = State.TheVirtualMap.GetTileReference(xy);
            if (State.TheVirtualMap.IsLargeMap)
            {
                // is it even klimbable?
                if (tileReference.IsKlimable)
                {
                    if (tileReference.Index !=
                        GameReferences.SpriteTileReferences.GetTileNumberByName("SmallMountains"))
                        throw new Ultima5ReduxException(
                            "I am not personal aware of what on earth you would be klimbing that is not already stated in the following logic...");

                    State.GrapplingFall();
                    klimbResult = KlimbResult.SuccessFell;
                    turnResults.PushOutputToConsole(GameReferences.DataOvlRef.StringReferences.GetString(
                        DataOvlReference.KlimbingStrings.FELL), false);
                    turnResults.PushTurnResult(
                        new BasicResult(TurnResult.TurnResultType.ActionKlimbDirectionMovedFell));
                }
                // is it tall mountains? we can't klimb those
                else if (tileReference.Index ==
                         GameReferences.SpriteTileReferences.GetTileNumberByName("TallMountains"))
                {
                    klimbResult = KlimbResult.CantKlimb;
                    turnResults.PushOutputToConsole(GameReferences.DataOvlRef.StringReferences.GetString(
                        DataOvlReference.KlimbingStrings.IMPASSABLE), false);
                    turnResults.PushTurnResult(
                        new BasicResult(TurnResult.TurnResultType.ActionKlimbDirectionImpassable));
                }
                // there is no chance of klimbing the thing
                else
                {
                    klimbResult = KlimbResult.CantKlimb;
                    turnResults.PushOutputToConsole(GameReferences.DataOvlRef.StringReferences.GetString(
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
                        GameReferences.DataOvlRef.StringReferences.GetString(DataOvlReference.TravelStrings.WHAT),
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

            TileReference tileReference = State.TheVirtualMap.GetTileReference(xy);
            // if there is an NPC on the tile, then we assume they want to look at the NPC, not whatever else may be on the tiles
            if (State.TheVirtualMap.IsMapUnitOccupiedTile(xy))
            {
                List<MapUnit> mapUnits = State.TheVirtualMap.GetMapUnitsOnTile(xy);
                if (mapUnits.Count <= 0)
                {
                    throw new Ultima5ReduxException("Tried to look up Map Unit, but couldn't find the map character");
                }

                lookStr = GameReferences.DataOvlRef.StringReferences
                              .GetString(DataOvlReference.Vision2Strings.THOU_DOST_SEE).Trim() + " " +
                          GameReferences.LookRef.GetLookDescription(mapUnits[0].KeyTileReference.Index).Trim();
            }
            // if we are any one of these signs then we superimpose it on the screen
            else if (GameReferences.SpriteTileReferences.IsSign(tileReference.Index))
            {
                specialLookCommand = SpecialLookCommand.Sign;
                lookStr = string.Empty;
            }
            else if (GameReferences.SpriteTileReferences.GetTileNumberByName("Clock1") == tileReference.Index)
            {
                lookStr = GameReferences.DataOvlRef.StringReferences
                              .GetString(DataOvlReference.Vision2Strings.THOU_DOST_SEE).Trim() + " " +
                          GameReferences.LookRef.GetLookDescription(tileReference.Index).TrimStart() +
                          State.TheTimeOfDay.FormattedTime;
            }
            else // lets see what we've got here!
            {
                lookStr = GameReferences.DataOvlRef.StringReferences
                              .GetString(DataOvlReference.Vision2Strings.THOU_DOST_SEE).Trim() + " " +
                          GameReferences.LookRef.GetLookDescription(tileReference.Index).TrimStart();
            }

            // pass time at the end to make sure moving characters are accounted for
            turnResults.PushOutputToConsole(lookStr, false);
            turnResults.PushTurnResult(new BasicResult(TurnResult.TurnResultType.ActionLook));

            return AdvanceTime(N_DEFAULT_ADVANCE_TIME, turnResults);
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
        public List<VirtualMap.AggressiveMapUnitInfo> TryToMove(Point2D.Direction direction, bool bKlimb,
            bool bFreeMove, TurnResults turnResults, bool bManualMovement = true)
        {
            int nTilesPerMapRow = State.TheVirtualMap.NumberOfRowTiles;
            int nTilesPerMapCol = State.TheVirtualMap.NumberOfColumnTiles;

            // if we were to move, which direction would we move
            GetAdjustments(direction, out int xAdjust, out int yAdjust);

            // would we be leaving a small map if we went forward?
            if (!State.TheVirtualMap.IsLargeMap && IsLeavingMap(new Point2D(
                    State.TheVirtualMap.CurrentPosition.X + xAdjust, State.TheVirtualMap.CurrentPosition.Y + yAdjust)))
            {
                turnResults.PushTurnResult(new BasicResult(TurnResult.TurnResultType.OfferToExitScreen));
                // it is expected that the called will offer an exit option, but we won't move the avatar because the space
                // is empty
                return new List<VirtualMap.AggressiveMapUnitInfo>();
            }

            Point2D originalPos = State.TheVirtualMap.CurrentPosition.XY;

            // calculate our new x and y values based on the adjustments
            Point2D newPosition = new((State.TheVirtualMap.CurrentPosition.X + xAdjust) % nTilesPerMapCol,
                (State.TheVirtualMap.CurrentPosition.Y + yAdjust) % nTilesPerMapRow);
            Avatar avatar = State.TheVirtualMap.TheMapUnits.GetAvatarMapUnit();
            // we change the direction of the Avatar map unit
            // this will be used to determine which is the appropriate sprite to show
            bool bAvatarActuallyMoved = avatar.Move(direction);

            // if the avatar did not actually move at this point then it appears they are on a frigate 
            // and change direction
            if (!bAvatarActuallyMoved)
            {
                turnResults.PushOutputToConsole(
                    GameReferences.DataOvlRef.StringReferences.GetString(DataOvlReference.WorldStrings.HEAD).TrimEnd() +
                    " " + GameReferences.DataOvlRef.StringReferences.GetDirectionString(direction));

                turnResults.PushTurnResult(new BasicResult(TurnResult.TurnResultType.ActionMoveChangeFrigateDirection));

                List<VirtualMap.AggressiveMapUnitInfo> aggressiveMapUnitInfos =
                    AdvanceTime(N_DEFAULT_ADVANCE_TIME, turnResults);
                return aggressiveMapUnitInfos;
            }

            // we know that if the avatar is on a frigate, then he hasn't just changed direction
            // so, if sails are hoisted and they are heading in a specific direction, then we will ignore
            // any additional keystrokes
            if (avatar.AreSailsHoisted &&
                State.WindDirection != Point2D.Direction.None && bManualMovement)
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
                Debug.Assert(State.TheVirtualMap.IsLargeMap,
                    "You should not reach the very end of a map +/- 1 if you are not on a repeating map");
                newPosition.X += nTilesPerMapCol;
            }

            if (newPosition.Y < 0)
            {
                Debug.Assert(State.TheVirtualMap.IsLargeMap,
                    "You should not reach the very end of a map +/- 1 if you are not on a repeating map");
                newPosition.Y = nTilesPerMapRow + newPosition.Y;
            }

            // we get the newTile so that we can determine if it's passable
            TileReference newTileReference = State.TheVirtualMap.GetTileReference(newPosition.X, newPosition.Y);

            if (newTileReference.Index == GameReferences.SpriteTileReferences.GetTileNumberByName("BrickFloorHole") &&
                !State.TheVirtualMap.IsAvatarRidingCarpet)
            {
                State.TheVirtualMap.UseStairs(newPosition, true);
                // we need to evaluate in the game and let the game know that they should continue to fall
                TileReference newTileRef = State.TheVirtualMap.GetTileReference(State.TheVirtualMap.CurrentPosition.XY);
                if (newTileRef.Index == GameReferences.SpriteTileReferences.GetTileNumberByName("BrickFloorHole"))
                    IsPendingFall = true;

                // todo: get string from data file
                turnResults.PushOutputToConsole("A TRAPDOOR!");
                turnResults.PushTurnResult(new BasicResult(TurnResult.TurnResultType.ActionMoveFell));
            }

            // we have evaluated and now know there is not a further fall (think Blackthorne's palace)
            IsPendingFall = false;

            // it's passable if it's marked as passable, 
            // but we double check if the portcullis is down
            bool bPassable = State.TheVirtualMap.IsTileFreeToTravel(newPosition);

            // this is insufficient in case I am in a boat
            if ((bKlimb && newTileReference.IsKlimable) || bPassable || bFreeMove)
            {
                State.TheVirtualMap.CurrentPosition.X = newPosition.X;
                State.TheVirtualMap.CurrentPosition.Y = newPosition.Y;
            }
            else // it is not passable
            {
                if (!bManualMovement && avatar.AreSailsHoisted)
                {
                    State.TheVirtualMap.DamageShip(State.WindDirection, turnResults);
                }
                else
                {
                    turnResults.PushOutputToConsole(GameReferences.DataOvlRef.StringReferences.GetString(
                        DataOvlReference.TravelStrings.BLOCKED));
                    if (newTileReference.Index == 47)
                    {
                        turnResults.PushOutputToConsole(GameReferences.DataOvlRef.StringReferences.GetString(
                            DataOvlReference.WorldStrings.OUCH));
                        turnResults.PushTurnResult(
                            new BasicResult(TurnResult.TurnResultType.ActionBlockedRanIntoCactus));
                        State.CharacterRecords.RanIntoCactus();
                    }
                }

                // if it's not passable then we have no more business here
                return AdvanceTime(N_DEFAULT_ADVANCE_TIME, turnResults);
            }

            // the world is a circular - so when you get to the end, start over again
            // this will prevent a never ending growth or shrinking of character position in case the travel the world only moving right a bagillion times
            State.TheVirtualMap.CurrentPosition.X %= nTilesPerMapCol;
            State.TheVirtualMap.CurrentPosition.Y %= nTilesPerMapRow;
            State.TheVirtualMap.SavePreviousPosition(State.TheVirtualMap.CurrentPosition);

            // if you walk on top of a staircase then we will immediately jump to the next floor
            if (GameReferences.SpriteTileReferences.IsStaircase(newTileReference.Index))
            {
                State.TheVirtualMap.UseStairs(State.TheVirtualMap.CurrentPosition.XY);
                turnResults.PushOutputToConsole(
                    State.TheVirtualMap.IsStairGoingDown(State.TheVirtualMap.CurrentPosition.XY)
                        ? GameReferences.DataOvlRef.StringReferences.GetString(DataOvlReference.TravelStrings.DOWN)
                        : GameReferences.DataOvlRef.StringReferences.GetString(DataOvlReference.TravelStrings.UP));

                turnResults.PushTurnResult(new BasicResult(TurnResult.TurnResultType.ActionMoveUsedStairs));
            }

            Avatar.AvatarState currentAvatarState =
                State.TheVirtualMap.TheMapUnits.GetAvatarMapUnit().CurrentAvatarState;

            // if we are on a big map then we may issue extra information about slow moving terrain
            if (!State.TheVirtualMap.IsLargeMap)
            {
                turnResults.PushTurnResult(
                    new PlayerMoved(GetTurnResultMovedByAvatarState(avatar, bManualMovement),
                        originalPos, State.TheVirtualMap.CurrentPosition.XY,
                        State.TheVirtualMap.GetTileReferenceOnCurrentTile()));

                return AdvanceTime(N_DEFAULT_ADVANCE_TIME, turnResults);
            }

            int nMinutesToAdvance = GameReferences.SpriteTileReferences.GetMinuteIncrement(newTileReference.Index);

            string slowMovingStr = GameReferences.SpriteTileReferences
                .GetSlowMovementString(newTileReference.Index).TrimEnd();
            if (slowMovingStr != "") turnResults.PushOutputToConsole(slowMovingStr, false);

            // if you are on the carpet or skiff and hit rough seas then we injure the players and report it back 
            if (currentAvatarState is Avatar.AvatarState.Carpet or Avatar.AvatarState.Skiff
                && newTileReference.Index == 1) // rough seas
            {
                State.CharacterRecords.RoughSeasInjure();
                turnResults.PushOutputToConsole(
                    GameReferences.DataOvlRef.StringReferences.GetString(DataOvlReference.WorldStrings
                        .ROUGH_SEAS), false);
                turnResults.PushTurnResult(new BasicResult(TurnResult.TurnResultType.ActionMoveRoughSeas));
            }
            else
            {
                turnResults.PushTurnResult(
                    new PlayerMoved(GetTurnResultMovedByAvatarState(avatar, bManualMovement),
                        originalPos, State.TheVirtualMap.CurrentPosition.XY,
                        State.TheVirtualMap.GetTileReferenceOnCurrentTile()));
            }

            // if we are indoors then all walking takes 2 minutes
            return AdvanceTime(nMinutesToAdvance, turnResults);
        }

        public void TryToMoveCombatMap(Point2D.Direction direction, TurnResults turnResults) =>
            TryToMoveCombatMap(State.TheVirtualMap.CurrentCombatMap.CurrentCombatPlayer, direction, turnResults);

        public void TryToMoveCombatMap(CombatPlayer combatPlayer, Point2D.Direction direction,
            TurnResults turnResults)
        {
            if (combatPlayer == null)
                throw new Ultima5ReduxException("Trying to move on combat map without a CombatPlayer");

            turnResults.PushOutputToConsole(
                GameReferences.DataOvlRef.StringReferences.GetDirectionString(direction), true, false);

            CombatMap currentCombatMap = State.TheVirtualMap.CurrentCombatMap;
            Debug.Assert(currentCombatMap != null);

            // if we were to move, which direction would we move
            GetAdjustments(direction, out int xAdjust, out int yAdjust);
            Point2D newPosition = new(combatPlayer.MapUnitPosition.X + xAdjust,
                combatPlayer.MapUnitPosition.Y + yAdjust);

            if (IsLeavingMap(newPosition))
            {
                turnResults.PushOutputToConsole("LEAVING", false);

                currentCombatMap.MakePlayerEscape(combatPlayer);

                turnResults.PushTurnResult(new BasicResult(TurnResult.TurnResultType.OfferToExitScreen));
                return;
            }

            if (!State.TheVirtualMap.IsTileFreeToTravelForAvatar(combatPlayer.MapUnitPosition.XY, newPosition, false,
                    Avatar.AvatarState.Regular))
            {
                turnResults.PushOutputToConsole(" - " +
                                                GameReferences.DataOvlRef.StringReferences.GetString(
                                                    DataOvlReference.TravelStrings.BLOCKED), false);
                turnResults.PushTurnResult(new BasicResult(TurnResult.TurnResultType.ActionMoveBlocked));
                return;
            }

            ProcessDamageOnAdvanceTimeInCombat(turnResults);
            currentCombatMap.MoveActiveCombatMapUnit(turnResults, newPosition);

            turnResults.PushTurnResult(new BasicResult(TurnResult.TurnResultType.ActionMovedCombatPlayerOnCombatMap));

            currentCombatMap.AdvanceToNextCombatMapUnit();
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

            TileReference tileReference = State.TheVirtualMap.GetTileReference(xy);

            bool isDoorInDirection = tileReference.IsOpenable;
            if (isDoorInDirection) return ProcessOpenDoor(xy, ref bWasSuccessful, turnResults, tileReference);

            MapUnit mapUnit = State.TheVirtualMap.GetTopVisibleMapUnit(xy, true);

            if (mapUnit is NonAttackingUnit { IsOpenable: true } nonAttackingUnit)
            {
                bWasSuccessful =
                    State.TheVirtualMap.ProcessSearchInnerItems(turnResults, nonAttackingUnit, false, true);
                return AdvanceTime(N_DEFAULT_ADVANCE_TIME, turnResults);
            }

            // if we fell through here then there is nothing open - easy enough
            turnResults.PushOutputToConsole(GameReferences.DataOvlRef.StringReferences.GetString(
                DataOvlReference.OpeningThingsStrings
                    .NOTHING_TO_OPEN), false);
            turnResults.PushTurnResult(new BasicResult(TurnResult.TurnResultType.ActionOpenDoorNothingToOpen));
            return AdvanceTime(N_DEFAULT_ADVANCE_TIME, turnResults);
        }

        /// <summary>
        ///     Standard way of passing time, makes sure it passes the default amount of time (2 minutes)
        /// </summary>
        /// <returns>"" or a string that describes what happened when passing time</returns>
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
        public List<VirtualMap.AggressiveMapUnitInfo> TryToPushAThing(Point2D avatarXy, Point2D.Direction direction,
            out bool bPushedAThing, TurnResults turnResults)
        {
            bPushedAThing = false;
            Point2D adjustedPos = avatarXy.GetAdjustedPosition(direction);

            TileReference adjustedTileReference = State.TheVirtualMap.GetTileReference(adjustedPos);

            // it's not pushable OR if an NPC occupies the tile -so let's bail
            if (!adjustedTileReference.IsPushable || State.TheVirtualMap.IsMapUnitOccupiedTile(adjustedPos))
            {
                turnResults.PushOutputToConsole(GameReferences.DataOvlRef.StringReferences.GetString(
                    DataOvlReference.ExclaimStrings
                        .WONT_BUDGE_BANG_N), false);
                turnResults.PushTurnResult(new BasicResult(TurnResult.TurnResultType.ActionPushWontBudge));
                return AdvanceTime(N_DEFAULT_ADVANCE_TIME, turnResults);
            }

            // we get the thing one tile further than the thing to see if we have room to push it forward
            Point2D oneMoreTileAdjusted = adjustedPos.GetAdjustedPosition(direction);
            TileReference oneMoreTileReference = State.TheVirtualMap.GetTileReference(oneMoreTileAdjusted);

            // if I'm sitting and the proceeding tile is an upright tile then I can't swap things 
            if (State.TheVirtualMap.IsAvatarSitting() && oneMoreTileReference.IsUpright)
            {
                turnResults.PushOutputToConsole(GameReferences.DataOvlRef.StringReferences.GetString(
                    DataOvlReference.ExclaimStrings
                        .WONT_BUDGE_BANG_N), false);
                turnResults.PushTurnResult(new BasicResult(TurnResult.TurnResultType.ActionPushWontBudge));
                return AdvanceTime(N_DEFAULT_ADVANCE_TIME, turnResults);
            }

            bPushedAThing = true;

            // if you are pushing a chair then change the direction of chair when it's pushed
            if (GameReferences.SpriteTileReferences.IsChair(adjustedTileReference.Index))
            {
                adjustedTileReference = GetChairNewDirection(direction);
                State.TheVirtualMap.SetOverridingTileReferece(adjustedTileReference, adjustedPos);
            }

            // is there an NPC on the tile? if so, we won't move anything into them
            bool bIsNpcOneMoreTile = State.TheVirtualMap.IsMapUnitOccupiedTile(oneMoreTileAdjusted);

            // is the next tile walkable and is there NOT an NPC on it
            if (oneMoreTileReference.IsWalking_Passable && !bIsNpcOneMoreTile)
                State.TheVirtualMap.SwapTiles(adjustedPos, oneMoreTileAdjusted);
            else // the next tile isn't walkable so we just swap the avatar and the push tile
                // we will pull (swap) the thing
                State.TheVirtualMap.SwapTiles(avatarXy, adjustedPos);

            // move the avatar to the new spot
            State.TheVirtualMap.CurrentPosition.XY = adjustedPos.Copy();

            turnResults.PushOutputToConsole(
                GameReferences.DataOvlRef.StringReferences.GetString(DataOvlReference.ExclaimStrings.PUSHED_BANG_N),
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
        public List<VirtualMap.AggressiveMapUnitInfo> TryToSearch(Point2D xy, out bool bWasSuccessful,
            TurnResults turnResults)
        {
            bWasSuccessful = false;

            // if there is something exposed already OR there is nothing found 
            if (!State.TheVirtualMap.ContainsSearchableThings(xy))
            {
                turnResults.PushOutputToConsole(U5StringRef.ThouDostFind(
                    GameReferences.DataOvlRef.StringReferences.GetString(DataOvlReference.Vision2Strings
                        .NOTHING_OF_NOTE_DOT_N)), false);
                return AdvanceTime(N_DEFAULT_ADVANCE_TIME, turnResults);
            }

            bool bHasInnerNonAttackUnits = false;
            TileReference tileReference = State.TheVirtualMap.GetTileReference(xy);

            // we search the tile and expose any items that may be on it
            Moonstone moonstone = State.TheVirtualMap.SearchAndExposeMoonstone(xy);
            if (moonstone == null)
                bHasInnerNonAttackUnits =
                    State.TheVirtualMap.SearchNonAttackingMapUnit(xy, turnResults, State.CharacterRecords.AvatarRecord,
                        State.CharacterRecords);

            if (moonstone != null)
            {
                bWasSuccessful = true;
                turnResults.PushOutputToConsole(U5StringRef.ThouDostFind(moonstone.FindDescription), false);
                //turnResults.PushOutputToConsole(, false);
            }
            else if (bHasInnerNonAttackUnits)
            {
                // do nothing, I think? The SearchNonAttackingMapUnit method takes care of the chatter
            }
            else if (tileReference.HasSearchReplacement)
            {
                // this occurs when you search something - and once searched it turns into something else
                // like searching a wall that turns into a door
                TileReference replacementTile =
                    GameReferences.SpriteTileReferences.GetTileReference(tileReference.SearchReplacementIndex);
                State.TheVirtualMap.SetOverridingTileReferece(replacementTile, xy);
                turnResults.PushTurnResult(new OutputToConsole(U5StringRef.ThouDostFind(
                    GameReferences.DataOvlRef.StringReferences.GetString(DataOvlReference.ThingsIFindStrings
                        .A_HIDDEN_DOOR_BANG_N)), false));
            }
            // it could be a moongate, with a stone, but wrong time of day
            else
            {
                turnResults.PushOutputToConsole(U5StringRef.ThouDostFind(
                    GameReferences.DataOvlRef.StringReferences.GetString(DataOvlReference.Vision2Strings
                        .NOTHING_OF_NOTE_DOT_N)), false);
            }

            return AdvanceTime(N_DEFAULT_ADVANCE_TIME, turnResults);
        }

        public List<VirtualMap.AggressiveMapUnitInfo> TryToTalk(MapUnitMovement.MovementCommandDirection direction,
            TurnResults turnResults)
        {
            NonPlayerCharacter npc = State.TheVirtualMap.GetNpcToTalkTo(direction);

            if (npc == null)
            {
                turnResults.PushOutputToConsole(
                    GameReferences.DataOvlRef.StringReferences.GetString(DataOvlReference.KeypressCommandsStrings
                        .FUNNY_NO_RESPONSE), false,
                    false);
                turnResults.PushTurnResult(new BasicResult(TurnResult.TurnResultType.NoOneToTalkTo));

                return AdvanceTime(N_DEFAULT_ADVANCE_TIME, turnResults);
            }

            bool bIsNpc = npc.NPCRef != null;
            bool bIsShoppeKeeper = bIsNpc && npc.NPCRef.IsShoppeKeeper;

            if (State.TheVirtualMap.IsNPCInBed(npc))
            {
                turnResults.PushOutputToConsole(
                    GameReferences.DataOvlRef.StringReferences.GetString(DataOvlReference.ChitChatStrings.ZZZ), false);
                turnResults.PushTurnResult(new BasicResult(TurnResult.TurnResultType.CantTalkSleeping));
                return AdvanceTime(N_DEFAULT_ADVANCE_TIME, turnResults);
            }

            if (State.TheVirtualMap.IsWantedManByThePoPo)
            {
                turnResults.PushOutputToConsole(
                    GameReferences.DataOvlRef.StringReferences.GetString(DataOvlReference.ChitChatStrings.DONT_HURT_ME),
                    false);
                turnResults.PushTurnResult(new BasicResult(TurnResult.TurnResultType.DontHurtMeAfraid));
                return AdvanceTime(N_DEFAULT_ADVANCE_TIME, turnResults);
            }

            if (bIsShoppeKeeper)
            {
                ShoppeKeeper shoppeKeeper = GameReferences.ShoppeKeeperDialogueReference.GetShoppeKeeper(
                    State.TheVirtualMap.CurrentSmallMap.MapLocation, npc.NPCRef.NpcType,
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
            }
            else if (bIsNpc)
            {
                if (npc.NPCRef.Script == null)
                {
                    turnResults.PushOutputToConsole("They are not talkative...", false);
                    turnResults.PushTurnResult(new BasicResult(TurnResult.TurnResultType.NotTalkative));
                    return AdvanceTime(N_DEFAULT_ADVANCE_TIME, turnResults);
                }

                turnResults.PushTurnResult(new NpcTalkInteraction(npc));
            }
            else
            {
                turnResults.PushOutputToConsole(
                    GameReferences.DataOvlRef.StringReferences.GetString(DataOvlReference.ChitChatStrings.NOBODY_HERE));
                turnResults.PushTurnResult(new BasicResult(TurnResult.TurnResultType.NoOneToTalkTo));
            }

            return AdvanceTime(N_DEFAULT_ADVANCE_TIME, turnResults);
        }

        public List<VirtualMap.AggressiveMapUnitInfo> TryToUseLordBritishArtifactItem(
            LordBritishArtifact lordBritishArtifact, TurnResults turnResults)
        {
            switch (lordBritishArtifact.Artifact)
            {
                case LordBritishArtifact.ArtifactType.Amulet:
                    turnResults.PushOutputToConsole(GameReferences.DataOvlRef.StringReferences.GetString(
                                                        DataOvlReference.WearUseItemStrings
                                                            .AMULET_N_N) + "\n" +
                                                    GameReferences.DataOvlRef.StringReferences.GetString(
                                                        DataOvlReference.WearUseItemStrings
                                                            .WEARING_AMULET) + "\n" +
                                                    GameReferences.DataOvlRef.StringReferences.GetString(
                                                        DataOvlReference.WearUseItemStrings
                                                            .SPACE_OF_LORD_BRITISH_DOT_N), false);
                    break;
                case LordBritishArtifact.ArtifactType.Crown:
                    turnResults.PushOutputToConsole(GameReferences.DataOvlRef.StringReferences.GetString(
                                                        DataOvlReference.WearUseItemStrings
                                                            .CROWN_N_N) + "\n" +
                                                    GameReferences.DataOvlRef.StringReferences.GetString(
                                                        DataOvlReference.WearUseItemStrings
                                                            .DON_THE_CROWN) + "\n" + GameReferences.DataOvlRef
                                                        .StringReferences.GetString(
                                                            DataOvlReference.WearUseItemStrings
                                                                .SPACE_OF_LORD_BRITISH_DOT_N), false);
                    break;
                case LordBritishArtifact.ArtifactType.Sceptre:
                    turnResults.PushOutputToConsole(GameReferences.DataOvlRef.StringReferences.GetString(
                                                        DataOvlReference.WearUseItemStrings
                                                            .SCEPTRE_N_N) + "\n" +
                                                    GameReferences.DataOvlRef.StringReferences.GetString(
                                                        DataOvlReference.WearUseItemStrings
                                                            .WIELD_SCEPTRE) + "\n" + GameReferences.DataOvlRef
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

        public List<VirtualMap.AggressiveMapUnitInfo> TryToUseMoonstone(Moonstone moonstone, out bool bMoonstoneBuried,
            TurnResults turnResults)
        {
            bMoonstoneBuried = false;

            if (!IsAllowedToBuryMoongate())
            {
                turnResults.PushOutputToConsole(GameReferences.DataOvlRef.StringReferences.GetString(
                                                    DataOvlReference.ExclaimStrings
                                                        .MOONSTONE_SPACE) +
                                                GameReferences.DataOvlRef.StringReferences.GetString(
                                                    DataOvlReference.ExclaimStrings
                                                        .CANNOT_BE_BURIED_HERE_BANG_N), false);
                return AdvanceTime(N_DEFAULT_ADVANCE_TIME, turnResults);
            }

            State.TheMoongates.SetMoonstoneBuried(moonstone.MoongateIndex, true,
                State.TheVirtualMap.GetCurrent3DPosition());
            bMoonstoneBuried = true;

            turnResults.PushOutputToConsole(
                GameReferences.DataOvlRef.StringReferences.GetString(DataOvlReference.ExclaimStrings.MOONSTONE_SPACE) +
                GameReferences.DataOvlRef.StringReferences.GetString(DataOvlReference.ExclaimStrings.BURIED_BANG_N),
                false);
            return AdvanceTime(N_DEFAULT_ADVANCE_TIME, turnResults);
        }

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
            if (IsCombatMap) State.TheVirtualMap.CurrentCombatMap.AdvanceToNextCombatMapUnit();

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
                            ? GameReferences.DataOvlRef.StringReferences.GetString(DataOvlReference.ExclaimStrings
                                .HEALED_BANG_N)
                            : GameReferences.DataOvlRef.StringReferences.GetString(DataOvlReference.ExclaimStrings
                                .FAILED_BANG_N), false);
                    break;
                case Potion.PotionColor.Red:
                    // cure poison
                    record.Cure();
                    turnResults.PushOutputToConsole(
                        GameReferences.DataOvlRef.StringReferences.GetString(DataOvlReference.ExclaimStrings
                            .POISON_CURED_BANG_N), false);
                    break;
                case Potion.PotionColor.Green:
                    // poison user
                    bool bWasPoisoned = record.Stats.Poison();
                    if (bWasPoisoned)
                        turnResults.PushOutputToConsole(GameReferences.DataOvlRef.StringReferences.GetString(
                            DataOvlReference.ExclaimStrings
                                .POISONED_BANG_N), false);
                    else
                        turnResults.PushOutputToConsole(GameReferences.DataOvlRef.StringReferences.GetString(
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
                    turnResults.PushOutputToConsole(GameReferences.DataOvlRef.StringReferences.GetString(
                        DataOvlReference.ExclaimStrings
                            .POOF_BANG_N), false);
                    break;
                case Potion.PotionColor.Black:
                    // invisibility
                    record.TurnInvisible();
                    turnResults.PushOutputToConsole(GameReferences.DataOvlRef.StringReferences.GetString(
                        DataOvlReference.ExclaimStrings
                            .INVISIBLE_BANG_N), false);
                    break;
                case Potion.PotionColor.White:
                    // x-ray
                    bSucceeded = _random.Next() % 2 == 0;
                    if (bSucceeded)
                    {
                        turnResults.PushOutputToConsole("X-Ray!", false);
                        break;
                    }

                    // if you fail with the x-ray then 
                    CastSleep(turnResults, record, out bool _);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(potion), @"Tried to use an undefined potion");
            }

            return AdvanceTime(N_DEFAULT_ADVANCE_TIME, turnResults);
        }

        public List<VirtualMap.AggressiveMapUnitInfo> TryToUseScroll(Scroll scroll, PlayerCharacterRecord record,
            TurnResults turnResults)
        {
            turnResults.PushTurnResult(new ReadScroll(scroll.ScrollSpell, record, record));

            turnResults.PushOutputToConsole($"Scroll: {scroll.ScrollSpell}\n\nA-la-Kazam!", false);

            return AdvanceTime(N_DEFAULT_ADVANCE_TIME, turnResults);
        }

        public List<VirtualMap.AggressiveMapUnitInfo> TryToUseShadowLordShard(ShadowlordShard shadowlordShard,
            TurnResults turnResults)
        {
            string thouHolds(string shard)
            {
                return GameReferences.DataOvlRef.StringReferences.GetString(DataOvlReference.ShadowlordStrings
                    .GEM_SHARD_THOU_HOLD_EVIL_SHARD) + "\n" + shard;
            }

            switch (shadowlordShard.Shard)
            {
                case ShadowlordShard.ShardType.Falsehood:
                    turnResults.PushOutputToConsole(thouHolds(
                        GameReferences.DataOvlRef.StringReferences.GetString(DataOvlReference.ShadowlordStrings
                            .FALSEHOOD_DOT)), false);
                    break;
                case ShadowlordShard.ShardType.Hatred:
                    turnResults.PushOutputToConsole(thouHolds(
                        GameReferences.DataOvlRef.StringReferences.GetString(DataOvlReference.ShadowlordStrings
                            .HATRED_DOT)), false);
                    break;
                case ShadowlordShard.ShardType.Cowardice:
                    turnResults.PushOutputToConsole(thouHolds(
                        GameReferences.DataOvlRef.StringReferences.GetString(DataOvlReference.ShadowlordStrings
                            .COWARDICE_DOT)), false);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(shadowlordShard),
                        @"Tried to use an undefined shadowlordShard");
            }

            return AdvanceTime(N_DEFAULT_ADVANCE_TIME, turnResults);
        }

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
                    turnResults.PushOutputToConsole(GameReferences.DataOvlRef.StringReferences.GetString(
                        DataOvlReference.WearUseItemStrings
                            .SPYGLASS_N_N), false);
                    break;
                case SpecialItem.SpecificItemType.HMSCape:
                    turnResults.PushOutputToConsole(GameReferences.DataOvlRef.StringReferences.GetString(
                        DataOvlReference.WearUseItemStrings
                            .PLANS_N_N), false);
                    break;
                case SpecialItem.SpecificItemType.PocketWatch:
                    turnResults.PushOutputToConsole(GameReferences.DataOvlRef.StringReferences.GetString(
                        DataOvlReference.WearUseItemStrings
                            .WATCH_N_N_THE_POCKET_W_READS) + " " + State.TheTimeOfDay.FormattedTime, false);
                    break;
                case SpecialItem.SpecificItemType.BlackBadge:
                    turnResults.PushOutputToConsole(GameReferences.DataOvlRef.StringReferences.GetString(
                        DataOvlReference.WearUseItemStrings
                            .BADGE_N_N), false);
                    break;
                case SpecialItem.SpecificItemType.WoodenBox:
                    turnResults.PushOutputToConsole(GameReferences.DataOvlRef.StringReferences.GetString(
                        DataOvlReference.WearUseItemStrings
                            .BOX_N_HOW_N), false);
                    break;
                case SpecialItem.SpecificItemType.Sextant:
                    turnResults.PushOutputToConsole(GameReferences.DataOvlRef.StringReferences.GetString(
                        DataOvlReference.WearUseItemStrings
                            .SEXTANT_N_N), false);
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
        public List<VirtualMap.AggressiveMapUnitInfo> TryToXit(out bool bWasSuccessful, TurnResults turnResults)
        {
            bWasSuccessful = true;

            MapUnit unboardedMapUnit =
                State.TheVirtualMap.TheMapUnits.XitCurrentMapUnit(State.TheVirtualMap, out string retStr);

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
        public List<VirtualMap.AggressiveMapUnitInfo> TryToYellForSails(out bool bSailsHoisted, TurnResults turnResults)
        {
            Debug.Assert(State.TheVirtualMap.TheMapUnits.GetAvatarMapUnit().CurrentBoardedMapUnit is Frigate);

            if (State.TheVirtualMap.TheMapUnits.GetAvatarMapUnit().CurrentBoardedMapUnit is not Frigate avatarsFrigate)
                throw new Ultima5ReduxException("Tried get Avatar's frigate, but it was null");

            avatarsFrigate.SailsHoisted = !avatarsFrigate.SailsHoisted;
            bSailsHoisted = avatarsFrigate.SailsHoisted;
            if (bSailsHoisted)
            {
                turnResults.PushOutputToConsole(GameReferences.DataOvlRef.StringReferences.GetString(
                    DataOvlReference.KeypressCommandsStrings
                        .YELL) + GameReferences.DataOvlRef.StringReferences
                    .GetString(DataOvlReference.YellingStrings.HOIST_BANG_N).Trim());
                turnResults.PushTurnResult(new BasicResult(TurnResult.TurnResultType.ActionYellSailsHoisted));
                return AdvanceTime(N_DEFAULT_ADVANCE_TIME, turnResults);
            }

            turnResults.PushOutputToConsole(
                GameReferences.DataOvlRef.StringReferences.GetString(DataOvlReference.KeypressCommandsStrings.YELL) +
                GameReferences.DataOvlRef.StringReferences.GetString(DataOvlReference.YellingStrings.FURL_BANG_N)
                    .Trim());
            turnResults.PushTurnResult(new BasicResult(TurnResult.TurnResultType.ActionYellSailsFurl));
            return AdvanceTime(N_DEFAULT_ADVANCE_TIME, turnResults);
        }

        public void YellWord(string word)
        {
            // not yet implemented
        }
    }
}