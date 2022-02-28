using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using Ultima5Redux.Dialogue;
using Ultima5Redux.Maps;
using Ultima5Redux.MapUnits;
using Ultima5Redux.MapUnits.CombatMapUnits;
using Ultima5Redux.MapUnits.NonPlayerCharacters;
using Ultima5Redux.MapUnits.SeaFaringVessels;
using Ultima5Redux.PlayerCharacters;
using Ultima5Redux.PlayerCharacters.Inventory;
using Ultima5Redux.References;
using Ultima5Redux.References.Maps;
using Ultima5Redux.References.PlayerCharacters.Inventory;

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

        public enum TryToMoveResult
        {
            Moved, ShipChangeDirection, Blocked, OfferToExitScreen, UsedStairs, Fell, ShipBreakingUp, ShipDestroyed,
            RoughSeas, MovedSelectionCursor, IgnoredMovement, Poisoned, Burning, Ignore
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

        private void CastSleep(PlayerCharacterRecord record, out bool bWasPutToSleep)
        {
            bWasPutToSleep = record.Sleep();
            StreamingOutput.Instance.PushMessage(
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

        private bool IsAllowedToBuryMoongate()
        {
            if (State.TheVirtualMap.LargeMapOverUnder != Map.Maps.Overworld &&
                State.TheVirtualMap.LargeMapOverUnder != Map.Maps.Underworld)
                return false;
            if (State.TheVirtualMap.HasAnyExposedSearchItems(State.TheVirtualMap.CurrentPosition.XY)) return false;
            TileReference tileRef = State.TheVirtualMap.GetTileReferenceOnCurrentTile();

            return GameReferences.SpriteTileReferences.IsMoonstoneBuriable(tileRef.Index);
        }

        private bool IsLeavingMap(Point2D xyProposedPosition)
        {
            return (xyProposedPosition.IsOutOfRange(State.TheVirtualMap.NumberOfColumnTiles - 1,
                State.TheVirtualMap.NumberOfRowTiles - 1));
        }

        private string ThouDostFind(string thingYouFound)
        {
            return GameReferences.DataOvlRef.StringReferences.GetString(DataOvlReference.ThingsIFindStrings
                .N_THOU_DOST_FIND_N) + thingYouFound;
        }

        /// <summary>
        ///     Use a magic carpet from your inventory
        /// </summary>
        /// <param name="bWasUsed">was the magic carpet used?</param>
        /// <returns>string to print and show user</returns>
        private void UseMagicCarpet(out bool bWasUsed)
        {
            if (IsCombatMap)
            {
                bWasUsed = false;
                StreamingOutput.Instance.PushMessage(GameReferences.DataOvlRef.StringReferences.GetString(
                    DataOvlReference.ExclaimStrings
                        .NOT_HERE_BANG), false);
                return;
            }

            bWasUsed = true;
            Debug.Assert((State.PlayerInventory.SpecializedItems.Items[SpecialItem.SpecificItemTypeSprite.Carpet]
                .HasOneOfMore));

            if (State.TheVirtualMap.TheMapUnits.GetAvatarMapUnit().IsAvatarOnBoardedThing)
            {
                bWasUsed = false;
                StreamingOutput.Instance.PushMessage(GameReferences.DataOvlRef.StringReferences.GetString(
                    DataOvlReference.WearUseItemStrings
                        .ONLY_ON_FOOT), false);
                return;
            }

            State.PlayerInventory.SpecializedItems.Items[SpecialItem.SpecificItemTypeSprite.Carpet].Quantity--;
            MagicCarpet carpet = State.TheVirtualMap.TheMapUnits.CreateMagicCarpet(
                State.TheVirtualMap.CurrentPosition.XY,
                State.TheVirtualMap.TheMapUnits.GetAvatarMapUnit().CurrentDirection,
                out int _);
            BoardAndCleanFromWorld(carpet);
            StreamingOutput.Instance.PushMessage(GameReferences.DataOvlRef.StringReferences.GetString(DataOvlReference
                .WearUseItemStrings
                .CARPET_BANG), false);
        }

        /// <summary>
        ///     Processes any damage effects as you advance time, this can include getting
        /// </summary>
        /// <returns></returns>
        private TryToMoveResult ProcessDamageOnAdvanceTime()
        {
            Avatar.AvatarState currentAvatarState =
                State.TheVirtualMap.TheMapUnits.GetAvatarMapUnit().CurrentAvatarState;
            TileReference currentTileReference = State.TheVirtualMap.GetTileReferenceOnCurrentTile();
            TryToMoveResult tryToMoveResult = TryToMoveResult.Ignore;

            // swamp - we poison them, but the actual damage occurs further down in case they were already poisoned
            if (currentAvatarState != Avatar.AvatarState.Carpet && currentTileReference.Index == 4)
            {
                bool bWasPoisoned = State.CharacterRecords.SteppedOnSwamp();
                if (!bWasPoisoned)
                {
                    tryToMoveResult = TryToMoveResult.Moved;
                }
                else
                {
                    tryToMoveResult = TryToMoveResult.Poisoned;
                    StreamingOutput.Instance.PushMessage(
                        GameReferences.DataOvlRef.StringReferences.GetString(DataOvlReference.ExclaimStrings
                            .POISONED_BANG_N), false);
                }
            }
            // if on lava
            else if (currentTileReference.Index == 143)
            {
                State.CharacterRecords.SteppedOnLava();
                tryToMoveResult = TryToMoveResult.Burning;
                StreamingOutput.Instance.PushMessage("Burning!", false);
            }

            // if already poisoned
            State.CharacterRecords.ProcessTurn();

            return tryToMoveResult;
        }

        /// <summary>
        ///     Advances time and takes care of all day, month, year calculations
        /// </summary>
        /// <param name="nMinutes">Number of minutes to advance (maximum of 9*60)</param>
        public TryToMoveResult AdvanceTime(int nMinutes)
        {
            int nCurrentHour = State.TheTimeOfDay.Month;

            State.TheTimeOfDay.AdvanceClock(nMinutes);
            TryToMoveResult tryToMoveResult;
            if (IsCombatMap)
            {
                tryToMoveResult = TryToMoveResult.Ignore;
            }
            else
            {
                tryToMoveResult = ProcessDamageOnAdvanceTime();
                Dictionary<MapUnit, VirtualMap.AggressiveMapUnitInfo> aggressiveMapUnitInfos =
                    State.TheVirtualMap.GetAggressiveMapUnitInfo();
                State.TheVirtualMap.MoveMapUnitsToNextMove(aggressiveMapUnitInfos);
                State.TheVirtualMap.GenerateAndCleanupEnemies(State.TheTimeOfDay.MinutesSinceBeginning);
            }

            // if a whole month has advanced then we go and add one month to the "staying at the inn" count
            if (nCurrentHour < State.TheTimeOfDay.Month) State.CharacterRecords.IncrementStayingAtInnCounters();
            if (State.TurnsToExtinguish > 0) State.TurnsToExtinguish--;

            State.TurnsSinceStart++;
            return tryToMoveResult;
        }

        /// <summary>
        ///     Board something such as a frigate, skiff, horse or carpet
        /// </summary>
        /// <param name="bWasSuccessful"></param>
        /// <returns></returns>
        public void Board(out bool bWasSuccessful)
        {
            MapUnit currentAvatarTileRef = State.TheVirtualMap.GetMapUnitOnCurrentTile();
            bWasSuccessful = true;

            if (currentAvatarTileRef is null || !currentAvatarTileRef.KeyTileReference.IsBoardable)
            {
                bWasSuccessful = false;
                // can't board it
                StreamingOutput.Instance.PushMessage(GameReferences.DataOvlRef.StringReferences
                                                         .GetString(DataOvlReference.KeypressCommandsStrings.BOARD)
                                                         .Trim() + " " +
                                                     GameReferences.DataOvlRef.StringReferences.GetString(
                                                         DataOvlReference.TravelStrings.WHAT));
                return;
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
                    bWasSuccessful = false;
                    StreamingOutput.Instance.PushMessage(getOnFootResponse(), false);
                    return;
                case MagicCarpet _:
                    BoardAndCleanFromWorld(boardableMapUnit);
                    break;
                case Horse _ when bAvatarIsBoarded:
                    bWasSuccessful = false;
                    StreamingOutput.Instance.PushMessage(getOnFootResponse());
                    return;
                // delete or deactivate the horse we just mounted
                case Horse _:
                    BoardAndCleanFromWorld(boardableMapUnit);
                    break;
                case Frigate boardableFrigate:
                {
                    if (bAvatarIsBoarded)
                    {
                        if (State.TheVirtualMap.IsAvatarRidingHorse)
                        {
                            bWasSuccessful = false;
                            StreamingOutput.Instance.PushMessage(getOnFootResponse());
                            return;
                        }

                        if (State.TheVirtualMap.IsAvatarRidingCarpet)
                            // we tuck the carpet away
                            State.PlayerInventory.SpecializedItems.Items[SpecialItem.SpecificItemTypeSprite.Carpet]
                                .Quantity++;
                        if (State.TheVirtualMap.IsAvatarInSkiff)
                            // add a skiff the the frigate
                            boardableFrigate.SkiffsAboard++;
                    }

                    if (boardableFrigate.SkiffsAboard == 0)
                        retStr += GameReferences.DataOvlRef.StringReferences
                            .GetString(DataOvlReference.SleepTransportStrings.M_WARNING_NO_SKIFFS_N).TrimEnd();
                    BoardAndCleanFromWorld(boardableFrigate);
                    break;
                }
                case Skiff _ when bAvatarIsBoarded:
                    bWasSuccessful = false;
                    retStr = getOnFootResponse();
                    break;
                case Skiff _:
                    BoardAndCleanFromWorld(boardableMapUnit);
                    break;
            }

            StreamingOutput.Instance.PushMessage(retStr);
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

            PassTime();
            return CurrentConversation;
        }

        /// <summary>
        ///     Attempt to enter a building at a coordinate
        ///     Will load new map if successful
        /// </summary>
        /// <param name="xy">position of building</param>
        /// <param name="bWasSuccessful">true if successfully entered</param>
        /// <returns>output string</returns>
        public void EnterBuilding(Point2D xy, out bool bWasSuccessful)
        {
            bool isOnBuilding = GameReferences.LargeMapRef.IsMapXYEnterable(xy);
            if (isOnBuilding)
            {
                SmallMapReferences.SingleMapReference.Location location =
                    GameReferences.LargeMapRef.GetLocationByMapXY(xy);
                SmallMapReferences.SingleMapReference singleMap =
                    GameReferences.SmallMapRef.GetSingleMapByLocation(location, 0);
                if (singleMap.MapType == Map.Maps.Dungeon)
                {
                    string retStr =
                        GameReferences.DataOvlRef.StringReferences.GetString(DataOvlReference.WorldStrings
                            .ENTER_SPACE) +
                        GameReferences.SmallMapRef.GetLocationTypeStr(location) + "\n" +
                        GameReferences.SmallMapRef.GetLocationName(location);
                    retStr += "\nUnable to enter the dungeons at this time!";
                    bWasSuccessful = false;
                    StreamingOutput.Instance.PushMessage(retStr, false);
                    return;
                }

                State.TheVirtualMap.LoadSmallMap(singleMap);
                // set us to the front of the building
                State.TheVirtualMap.CurrentPosition.XY = SmallMapReferences.GetStartingXYByLocation();

                StreamingOutput.Instance.PushMessage(
                    GameReferences.DataOvlRef.StringReferences.GetString(DataOvlReference.WorldStrings.ENTER_SPACE) +
                    GameReferences.SmallMapRef.GetLocationTypeStr(location) + "\n" +
                    GameReferences.SmallMapRef.GetLocationName(location), false);
                bWasSuccessful = true;
            }
            else
            {
                StreamingOutput.Instance.PushMessage(
                    GameReferences.DataOvlRef.StringReferences.GetString(DataOvlReference.WorldStrings.ENTER_SPACE) +
                    GameReferences.DataOvlRef.StringReferences.GetString(DataOvlReference.WorldStrings.WHAT), false);
                bWasSuccessful = false;
            }

            PassTime();
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
        ///     Ignites a torch, if available and set the number of turns for the torch to be burnt out
        /// </summary>
        /// <returns></returns>
        // ReSharper disable once UnusedMember.Global
        public void IgniteTorch()
        {
            PassTime();
            // if there are no torches then report back and make no change
            if (State.PlayerInventory.TheProvisions.Items[Provision.SpecificProvisionType.Torches].Quantity <= 0)
            {
                StreamingOutput.Instance.PushMessage(GameReferences.DataOvlRef.StringReferences.GetString(
                    DataOvlReference.SleepTransportStrings
                        .NONE_OWNED_BANG_N), false);
                return;
            }

            State.PlayerInventory.TheProvisions.Items[Provision.SpecificProvisionType.Torches].Quantity--;
            State.TurnsToExtinguish = N_DEFAULT_NUMBER_OF_TURNS_FOR_TORCH;
            // this will trigger a re-read of time of day changes
            State.TheTimeOfDay.SetAllChangeTrackers();
            StreamingOutput.Instance.PushMessage(GameReferences.DataOvlRef.StringReferences.GetString(DataOvlReference
                .KeypressCommandsStrings
                .IGNITE_TORCH), false);
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

        /// <summary>
        ///     Looks at a particular tile, detecting if NPCs are present as well
        ///     Provides string output or special instructions if it is "special"B
        /// </summary>
        /// <param name="xy">position of tile to look at</param>
        /// <param name="specialLookCommand">Special command such as look at gem or sign</param>
        /// <returns>String to output to user</returns>
        // ReSharper disable once UnusedMember.Global
        public void Look(Point2D xy, out SpecialLookCommand specialLookCommand)
        {
            specialLookCommand = SpecialLookCommand.None;
            string retStr;

            TileReference tileReference = State.TheVirtualMap.GetTileReference(xy);
            // if there is an NPC on the tile, then we assume they want to look at the NPC, not whatever else may be on the tiles
            if (State.TheVirtualMap.IsMapUnitOccupiedTile(xy))
            {
                List<MapUnit> mapUnits = State.TheVirtualMap.GetMapUnitOnTile(xy);
                if (mapUnits.Count <= 0)
                    throw new Ultima5ReduxException("Tried to look up Map Unit, but couldn't find the map character");
                retStr = GameReferences.DataOvlRef.StringReferences
                             .GetString(DataOvlReference.Vision2Strings.THOU_DOST_SEE).Trim() + " " +
                         GameReferences.LookRef.GetLookDescription(mapUnits[0].KeyTileReference.Index).Trim();
            }
            // if we are any one of these signs then we superimpose it on the screen
            else if (GameReferences.SpriteTileReferences.IsSign(tileReference.Index))
            {
                specialLookCommand = SpecialLookCommand.Sign;
                retStr = string.Empty;
            }
            else if (GameReferences.SpriteTileReferences.GetTileNumberByName("Clock1") == tileReference.Index)
            {
                retStr = GameReferences.DataOvlRef.StringReferences
                             .GetString(DataOvlReference.Vision2Strings.THOU_DOST_SEE).Trim() + " " +
                         GameReferences.LookRef.GetLookDescription(tileReference.Index).TrimStart() +
                         State.TheTimeOfDay.FormattedTime;
            }
            else // lets see what we've got here!
            {
                retStr = GameReferences.DataOvlRef.StringReferences
                             .GetString(DataOvlReference.Vision2Strings.THOU_DOST_SEE).Trim() + " " +
                         GameReferences.LookRef.GetLookDescription(tileReference.Index).TrimStart();
            }

            // pass time at the end to make sure moving characters are accounted for
            PassTime();
            StreamingOutput.Instance.PushMessage(retStr, false);
        }

        /// <summary>
        ///     Standard way of passing time, makes sure it passes the default amount of time (2 minutes)
        /// </summary>
        /// <returns>"" or a string that describes what happened when passing time</returns>
        public TryToMoveResult PassTime()
        {
            return AdvanceTime(2);
        }

        /// <summary>
        ///     Attempts to push (or pull!) a map item
        /// </summary>
        /// <param name="avatarXy">the avatar's current map position</param>
        /// <param name="direction">the direction of the thing the avatar wants to push</param>
        /// <param name="bPushedAThing">was a thing actually pushed?</param>
        /// <returns>the string to output to the user</returns>
        public void PushAThing(Point2D avatarXy, Point2D.Direction direction, out bool bPushedAThing)
        {
            bPushedAThing = false;
            Point2D adjustedPos = avatarXy.GetAdjustedPosition(direction);

            TileReference adjustedTileReference = State.TheVirtualMap.GetTileReference(adjustedPos);

            // it's not pushable OR if an NPC occupies the tile -so let's bail
            if (!adjustedTileReference.IsPushable || State.TheVirtualMap.IsMapUnitOccupiedTile(adjustedPos))
            {
                StreamingOutput.Instance.PushMessage(GameReferences.DataOvlRef.StringReferences.GetString(
                    DataOvlReference.ExclaimStrings
                        .WONT_BUDGE_BANG_N), false);
                return;
            }

            bPushedAThing = true;

            // we get the thing one tile further than the thing to see if we have room to push it forward
            Point2D oneMoreTileAdjusted = adjustedPos.GetAdjustedPosition(direction);
            TileReference oneMoreTileReference = State.TheVirtualMap.GetTileReference(oneMoreTileAdjusted);

            // if I'm sitting and the proceeding tile is an upright tile then I can't swap things 
            if (State.TheVirtualMap.IsAvatarSitting() && oneMoreTileReference.IsUpright)
            {
                StreamingOutput.Instance.PushMessage(GameReferences.DataOvlRef.StringReferences.GetString(
                    DataOvlReference.ExclaimStrings
                        .WONT_BUDGE_BANG_N), false);
                return;
            }

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

            PassTime();

            StreamingOutput.Instance.PushMessage(
                GameReferences.DataOvlRef.StringReferences.GetString(DataOvlReference.ExclaimStrings.PUSHED_BANG_N),
                false);
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

        public enum TryToAttackResult
        {
            Uninitialized, NothingToAttack, BrokenMirror, CombatMapEnemy, CombatMapNpc, NpcMurder, OnlyOnFoot,
            ShootAProjectile
        }

        /// <summary>
        ///     Called when the avatar is attempting to attack a particular target
        /// </summary>
        /// <param name="attackTargetPosition">where is the target</param>
        /// <param name="mapUnit">the mapunit (if any) that is the target</param>
        /// <param name="singleCombatMapReference">the combat map reference (if any) that should be loaded</param>
        /// <returns>the final decision on how to handle the attack</returns>
        /// <exception cref="Ultima5ReduxException"></exception>
        public TryToAttackResult TryToAttack(Point2D attackTargetPosition, out MapUnit mapUnit,
            out SingleCombatMapReference singleCombatMapReference)
        {
            singleCombatMapReference = null;
            mapUnit = null;

            TileReference tileReference = State.TheVirtualMap.GetTileReference(attackTargetPosition);

            // if you are attacking an unbroken mirror - then we break it and override the tile
            if (GameReferences.SpriteTileReferences.IsUnbrokenMirror(tileReference.Index))
            {
                StreamingOutput.Instance.PushMessage(
                    GameReferences.DataOvlRef.StringReferences.GetString(DataOvlReference.TravelStrings.BROKEN),
                    false);
                TileReference brokenMirrorTileReference =
                    GameReferences.SpriteTileReferences.GetTileReferenceByName("MirrorBroken");
                State.TheVirtualMap.SetOverridingTileReferece(brokenMirrorTileReference,
                    attackTargetPosition);
                PassTime();
                return TryToAttackResult.BrokenMirror;
            }

            mapUnit = State.TheVirtualMap.GetTopVisibleMapUnit(attackTargetPosition, true);

            if (mapUnit is not { IsAttackable: true })
            {
                StreamingOutput.Instance.PushMessage(GameReferences.DataOvlRef.StringReferences.GetString(
                    DataOvlReference.TravelStrings.NOTHING_TO_ATTACK), false);
                return TryToAttackResult.NothingToAttack;
            }

            // we know there is a mapunit to attack at this point
            StreamingOutput.Instance.PushMessage(
                GameReferences.DataOvlRef.StringReferences.GetString(DataOvlReference.TravelStrings.ATTACK) +
                mapUnit.FriendlyName, false);

            Avatar avatar = State.TheVirtualMap.TheMapUnits.GetAvatarMapUnit();

            // we get the combat map reference, if any - it also tells us if there should be a ranged attack in the overworld
            // instead of a combat map
            singleCombatMapReference =
                State.TheVirtualMap.GetCombatMapReferenceForAvatarAttacking(
                    avatar.MapUnitPosition.XY,
                    attackTargetPosition,
                    SingleCombatMapReference.Territory.Britannia);

            // if there is a mapunit - BUT - no 
            ////// NOTE - this doesn't make sense for the Avatar to attack like this
            if (singleCombatMapReference == null)
            {
                // we were not able to attack, likely on a carpet or skiff and on the water
                // but may be other edge cases
                // if (missileType != CombatItemReference.MissileType.None)
                //     throw new Ultima5ReduxException(
                //         "Single combat map reference was null, but missile type wasn't null when avatar attacks");

                StreamingOutput.Instance.PushMessage(
                    GameReferences.DataOvlRef.StringReferences.GetString(DataOvlReference.KeypressCommandsStrings
                        .ON_FOOT), false);
                return TryToAttackResult.OnlyOnFoot;

                // StreamingOutput.Instance.PushMessage("Pretend I shot a projectile", false);
                // return TryToAttackResult.ShootAProjectile;
            }

            // if (singleCombatMapReference == null)
            //     throw new Ultima5ReduxException(
            //         $"Have no single combat map reference while trying to attack {mapUnit.FriendlyName}");

            TryToAttackResult tryToAttackResult = TryToAttackResult.Uninitialized;

            switch (mapUnit)
            {
                case Enemy enemy:
                    tryToAttackResult = TryToAttackResult.CombatMapEnemy;
                    break;
                case NonPlayerCharacter npc:
                    if (GameReferences.SpriteTileReferences.IsHeadOfBed(tileReference.Index) ||
                        GameReferences.SpriteTileReferences.IsStocks(tileReference.Index))
                    {
                        StreamingOutput.Instance.PushMessage(
                            GameReferences.DataOvlRef.StringReferences.GetString(
                                DataOvlReference.TravelStrings.MURDERED), false);
                        tryToAttackResult = TryToAttackResult.NpcMurder;
                        MurderNPC(npc);
                        break;
                    }

                    tryToAttackResult = TryToAttackResult.CombatMapNpc;

                    break;
            }

            if (tryToAttackResult is TryToAttackResult.CombatMapEnemy or TryToAttackResult.CombatMapNpc)
            {
                StreamingOutput.Instance.PushMessage(mapUnit.FriendlyName + "\n" +
                                                     GameReferences.DataOvlRef.StringReferences.GetString(
                                                         DataOvlReference.AdditionalStrings
                                                             .STARS_CONFLICT_STARS_N_N), false);
            }

            PassTime();
            return tryToAttackResult;
        }

        private void MurderNPC(NonPlayerCharacter npc)
        {
            npc.NPCState.IsDead = true;
            State.Karma -= 10;
        }

        /// <summary>
        ///     Gets a thing from the world, adding it the inventory and providing output for console
        /// </summary>
        /// <param name="xy">where is the thing?</param>
        /// <param name="bGotAThing">did I get a thing?</param>
        /// <param name="inventoryItem"></param>
        /// <returns>the output string</returns>
        // ReSharper disable once UnusedMethodReturnValue.Global
        public void TryToGetAThing(Point2D xy, out bool bGotAThing, out InventoryItem inventoryItem)
        {
            bGotAThing = false;
            inventoryItem = null;

            TileReference tileReference = State.TheVirtualMap.GetTileReference(xy);

            if (State.TheVirtualMap.CurrentSingleMapReference == null)
                throw new Ultima5ReduxException("No single map is set in virtual map");

            MagicCarpet magicCarpet = State.TheVirtualMap.TheMapUnits.GetSpecificMapUnitByLocation<MagicCarpet>(
                State.TheVirtualMap.LargeMapOverUnder, xy, State.TheVirtualMap.CurrentSingleMapReference.Floor);

            PassTime();

            // wall sconces - BORROWED!
            if (tileReference.Index == GameReferences.SpriteTileReferences.GetTileNumberByName("LeftSconce") ||
                tileReference.Index == GameReferences.SpriteTileReferences.GetTileNumberByName("RightSconce"))
            {
                State.PlayerInventory.TheProvisions.Items[Provision.SpecificProvisionType.Torches].Quantity++;

                State.TheVirtualMap.SetOverridingTileReferece(
                    GameReferences.SpriteTileReferences.GetTileReferenceByName("BrickFloor"), xy);
                bGotAThing = true;
                StreamingOutput.Instance.PushMessage(
                    GameReferences.DataOvlRef.StringReferences.GetString(DataOvlReference.GetThingsStrings.BORROWED),
                    false);
                return;
            }

            if (magicCarpet != null)
            {
                // add the carpet to the players inventory and remove it from the map
                State.PlayerInventory.SpecializedItems.Items[SpecialItem.SpecificItemTypeSprite.Carpet].Quantity++;
                State.TheVirtualMap.TheMapUnits.ClearAndSetEmptyMapUnits(magicCarpet);
                bGotAThing = true;
                StreamingOutput.Instance.PushMessage(GameReferences.DataOvlRef.StringReferences.GetString(
                    DataOvlReference.GetThingsStrings
                        .A_MAGIC_CARPET), false);
                return;
            }

            // are there any exposed items (generic call)
            if (State.TheVirtualMap.HasAnyExposedSearchItems(xy))
            {
                bGotAThing = true;
                InventoryItem invItem = State.TheVirtualMap.DequeuExposedSearchItems(xy);
                inventoryItem = invItem;
                invItem.Quantity++;

                StreamingOutput.Instance.PushMessage(ThouDostFind(invItem.FindDescription), false);
                return;
                //
            }

            StreamingOutput.Instance.PushMessage(GameReferences.DataOvlRef.StringReferences.GetString(
                DataOvlReference.GetThingsStrings.NOTHING_TO_GET), false);
        }

        /// <summary>
        ///     Try to jimmy the door with a given character
        /// </summary>
        /// <param name="xy">position of the door</param>
        /// <param name="record">character who will attempt to open it</param>
        /// <param name="bWasSuccessful">was it a successful jimmy?</param>
        /// <returns>the output string to write to console</returns>
        // ReSharper disable once UnusedMember.Global
        // ReSharper disable once UnusedParameter.Global
        public void TryToJimmyDoor(Point2D xy, PlayerCharacterRecord record, out bool bWasSuccessful)
        {
            bWasSuccessful = false;
            TileReference tileReference = State.TheVirtualMap.GetTileReference(xy);

            bool isDoorInDirection = tileReference.IsOpenable;

            if (!isDoorInDirection)
            {
                StreamingOutput.Instance.PushMessage(GameReferences.DataOvlRef.StringReferences.GetString(
                    DataOvlReference.OpeningThingsStrings
                        .NO_LOCK), false);
            }
            else
            {
                bool bIsDoorMagical = GameReferences.SpriteTileReferences.IsDoorMagical(tileReference.Index);
                bool bIsDoorLocked = GameReferences.SpriteTileReferences.IsDoorLocked(tileReference.Index);

                if (bIsDoorMagical)
                {
                    // we use up a key
                    State.PlayerInventory.TheProvisions.Items[Provision.SpecificProvisionType.Keys].Quantity--;

                    // for now we will also just open the door so we can get around - will address when we have spells
                    State.TheVirtualMap.SetOverridingTileReferece(
                        GameReferences.SpriteTileReferences.IsDoorWithView(tileReference.Index)
                            ? GameReferences.SpriteTileReferences.GetTileReferenceByName("RegularDoorView")
                            : GameReferences.SpriteTileReferences.GetTileReferenceByName("RegularDoor"), xy);

                    bWasSuccessful = true;

                    StreamingOutput.Instance.PushMessage(GameReferences.DataOvlRef.StringReferences.GetString(
                        DataOvlReference.OpeningThingsStrings
                            .KEY_BROKE), false);
                }
                else if (bIsDoorLocked)
                {
                    // we use up a key
                    State.PlayerInventory.TheProvisions.Items[Provision.SpecificProvisionType.Keys].Quantity--;

                    // todo: bh: we will need to determine the likelihood of lock picking success, for now, we always succeed

                    State.TheVirtualMap.SetOverridingTileReferece(
                        GameReferences.SpriteTileReferences.IsDoorWithView(tileReference.Index)
                            ? GameReferences.SpriteTileReferences.GetTileReferenceByName("RegularDoorView")
                            : GameReferences.SpriteTileReferences.GetTileReferenceByName("RegularDoor"), xy);

                    bWasSuccessful = true;
                    StreamingOutput.Instance.PushMessage(GameReferences.DataOvlRef.StringReferences.GetString(
                        DataOvlReference.OpeningThingsStrings
                            .UNLOCKED), false);
                }
                else
                {
                    StreamingOutput.Instance.PushMessage(GameReferences.DataOvlRef.StringReferences.GetString(
                        DataOvlReference.OpeningThingsStrings
                            .NO_LOCK), false);
                }
            }

            PassTime();
        }

        /// <summary>
        ///     Climbs the ladder on the current tile that the Avatar occupies
        /// </summary>
        public void TryToKlimb(out KlimbResult klimbResult)
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
                if (State.PlayerInventory.SpecializedItems.Items[SpecialItem.SpecificItemTypeSprite.Grapple]
                    .HasOneOfMore) // we don't have a grapple, so we can't klimb
                {
                    klimbResult = KlimbResult.RequiresDirection;
                    StreamingOutput.Instance.PushMessage(getKlimbOutput(), false);
                    return;
                }

                klimbResult = KlimbResult.CantKlimb;
                StreamingOutput.Instance.PushMessage(getKlimbOutput(
                        GameReferences.DataOvlRef.StringReferences.GetString(DataOvlReference.KlimbingStrings
                            .WITH_WHAT)),
                    false);
                return;
            }

            // we can't klimb on the current tile, so we need to pick a direction
            if (!GameReferences.SpriteTileReferences.IsLadder(curTileRef.Index) &&
                !GameReferences.SpriteTileReferences.IsGrate(curTileRef.Index))
            {
                klimbResult = KlimbResult.RequiresDirection;
                StreamingOutput.Instance.PushMessage(getKlimbOutput(), false);
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
                    StreamingOutput.Instance.PushMessage(getKlimbOutput(
                            GameReferences.DataOvlRef.StringReferences.GetString(DataOvlReference.TravelStrings.DOWN)),
                        false);
                    return;
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
                StreamingOutput.Instance.PushMessage(getKlimbOutput(
                    GameReferences.DataOvlRef.StringReferences.GetString(DataOvlReference.TravelStrings.UP)), false);
                return;
            }

            klimbResult = KlimbResult.RequiresDirection;
            StreamingOutput.Instance.PushMessage(getKlimbOutput(), false);
        }

        /// <summary>
        ///     Try to klimb the given tile - typically called after you select a direction
        /// </summary>
        /// <param name="xy"></param>
        /// <param name="klimbResult"></param>
        /// <returns></returns>
        // ReSharper disable once UnusedMember.Global
        public void TryToKlimbInDirection(Point2D xy, out KlimbResult klimbResult)
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
                    StreamingOutput.Instance.PushMessage(GameReferences.DataOvlRef.StringReferences.GetString(
                        DataOvlReference.KlimbingStrings
                            .FELL), false);
                }
                // is it tall mountains? we can't klimb those
                else if (tileReference.Index ==
                         GameReferences.SpriteTileReferences.GetTileNumberByName("TallMountains"))
                {
                    klimbResult = KlimbResult.CantKlimb;
                    StreamingOutput.Instance.PushMessage(GameReferences.DataOvlRef.StringReferences.GetString(
                        DataOvlReference.KlimbingStrings
                            .IMPASSABLE), false);
                }
                // there is no chance of klimbing the thing
                else
                {
                    klimbResult = KlimbResult.CantKlimb;
                    StreamingOutput.Instance.PushMessage(GameReferences.DataOvlRef.StringReferences.GetString(
                        DataOvlReference.KlimbingStrings
                            .NOT_CLIMABLE), false);
                }
            }
            else // it's a small map
            {
                if (tileReference.IsKlimable)
                {
                    // ie. a fence
                    klimbResult = KlimbResult.Success;
                }
                else
                {
                    klimbResult = KlimbResult.CantKlimb;
                    StreamingOutput.Instance.PushMessage(
                        GameReferences.DataOvlRef.StringReferences.GetString(DataOvlReference.TravelStrings.WHAT),
                        false);
                }
            }

            PassTime();
        }

        /// <summary>
        ///     Tries to move the avatar in a given direction - if successful it will move him
        /// </summary>
        /// <param name="direction">the direction you want to move</param>
        /// <param name="bKlimb">is the avatar K-limbing?</param>
        /// <param name="bFreeMove">is "free move" on?</param>
        /// <param name="tryToMoveResult">outputs the result of the attempt</param>
        /// <param name="bManualMovement">true if movement is manual</param>
        /// <returns>output string (may be empty)</returns>
        public void TryToMove(Point2D.Direction direction, bool bKlimb, bool bFreeMove,
            out TryToMoveResult tryToMoveResult, bool bManualMovement = true)
        {
            int nTilesPerMapRow = State.TheVirtualMap.NumberOfRowTiles;
            int nTilesPerMapCol = State.TheVirtualMap.NumberOfColumnTiles;

            // if we were to move, which direction would we move
            GetAdjustments(direction, out int xAdjust, out int yAdjust);

            // would we be leaving a small map if we went forward?
            if (!State.TheVirtualMap.IsLargeMap && IsLeavingMap(new Point2D(
                    State.TheVirtualMap.CurrentPosition.X + xAdjust, State.TheVirtualMap.CurrentPosition.Y + yAdjust)))
            {
                tryToMoveResult = TryToMoveResult.OfferToExitScreen;
                // it is expected that the called will offer an exit option, but we won't move the avatar because the space
                // is empty
                return;
            }

            // calculate our new x and y values based on the adjustments
            Point2D newPosition = new((State.TheVirtualMap.CurrentPosition.X + xAdjust) % nTilesPerMapCol,
                (State.TheVirtualMap.CurrentPosition.Y + yAdjust) % nTilesPerMapRow);

            // we change the direction of the Avatar map unit
            // this will be used to determine which is the appropriate sprite to show
            bool bAvatarActuallyMoved = State.TheVirtualMap.TheMapUnits.GetAvatarMapUnit().Move(direction);

            if (!bAvatarActuallyMoved)
            {
                tryToMoveResult = TryToMoveResult.ShipChangeDirection;
                AdvanceTime(N_DEFAULT_ADVANCE_TIME);
                StreamingOutput.Instance.PushMessage(
                    GameReferences.DataOvlRef.StringReferences.GetString(DataOvlReference.WorldStrings.HEAD) + " " +
                    GameReferences.DataOvlRef.StringReferences.GetDirectionString(direction));
            }

            // we know that if the avatar is on a frigate, then he hasn't just changed direction
            // so, if sails are hoisted and they are heading in a specific direction, then we will ignore
            // any additional keystrokes
            if (State.TheVirtualMap.TheMapUnits.GetAvatarMapUnit().AreSailsHoisted &&
                State.WindDirection != Point2D.Direction.None && bManualMovement)
            {
                tryToMoveResult = TryToMoveResult.IgnoredMovement;
                return;
            }

            // we start with a different descriptor depending on the vehicle the Avatar is currently on
            switch (State.TheVirtualMap.TheMapUnits.GetAvatarMapUnit().CurrentAvatarState)
            {
                case Avatar.AvatarState.Regular:
                    StreamingOutput.Instance.PushMessage(
                        GameReferences.DataOvlRef.StringReferences.GetDirectionString(direction));
                    break;
                case Avatar.AvatarState.Carpet:
                    StreamingOutput.Instance.PushMessage(
                        GameReferences.DataOvlRef.StringReferences.GetString(DataOvlReference.WorldStrings.FLY) +
                        GameReferences.DataOvlRef.StringReferences.GetDirectionString(direction));
                    break;
                case Avatar.AvatarState.Horse:
                    StreamingOutput.Instance.PushMessage(
                        GameReferences.DataOvlRef.StringReferences.GetString(DataOvlReference.WorldStrings.RIDE) +
                        GameReferences.DataOvlRef.StringReferences.GetDirectionString(direction));
                    break;
                case Avatar.AvatarState.Frigate:
                    if (State.TheVirtualMap.TheMapUnits.GetAvatarMapUnit().AreSailsHoisted)
                    {
                        if (bManualMovement)
                        {
                            StreamingOutput.Instance.PushMessage(GameReferences.DataOvlRef.StringReferences.GetString(
                                DataOvlReference.WorldStrings
                                    .HEAD) + GameReferences.DataOvlRef.StringReferences.GetDirectionString(direction));
                        }
                        else
                        {
                            StreamingOutput.Instance.PushMessage(
                                GameReferences.DataOvlRef.StringReferences.GetDirectionString(direction));
                        }
                    }
                    else
                    {
                        StreamingOutput.Instance.PushMessage(
                            GameReferences.DataOvlRef.StringReferences.GetDirectionString(direction) +
                            GameReferences.DataOvlRef.StringReferences.GetString(DataOvlReference.WorldStrings
                                .ROWING));
                    }

                    break;
                case Avatar.AvatarState.Skiff:
                    StreamingOutput.Instance.PushMessage(
                        GameReferences.DataOvlRef.StringReferences.GetString(DataOvlReference.WorldStrings.ROW) +
                        GameReferences.DataOvlRef.StringReferences.GetDirectionString(direction));
                    break;
                case Avatar.AvatarState.Hidden:
                default:
                    throw new ArgumentOutOfRangeException(nameof(direction),
                        @"Tried to move in an unknown avatar boarding state");
            }

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
                tryToMoveResult = TryToMoveResult.Fell;
                // we need to evaluate in the game and let the game know that they should continue to fall
                TileReference newTileRef = State.TheVirtualMap.GetTileReference(State.TheVirtualMap.CurrentPosition.XY);
                if (newTileRef.Index == GameReferences.SpriteTileReferences.GetTileNumberByName("BrickFloorHole"))
                    IsPendingFall = true;

                // todo: get string from data file
                StreamingOutput.Instance.PushMessage("A TRAPDOOR!");
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
                Avatar avatar = State.TheVirtualMap.TheMapUnits.GetAvatarMapUnit();
                if (!bManualMovement && avatar.AreSailsHoisted)
                {
                    int nDamage = _random.Next(5, 15);

                    Debug.Assert(avatar.CurrentBoardedMapUnit is Frigate);
                    if (avatar.CurrentBoardedMapUnit is not Frigate frigate)
                        throw new Ultima5ReduxException("Tried to get Avatar's frigate, but it returned  null");

                    // if the wind is blowing the same direction then we double the damage
                    if (avatar.CurrentDirection == State.WindDirection) nDamage *= 2;
                    // decrement the damage from the frigate
                    frigate.Hitpoints -= nDamage;

                    StreamingOutput.Instance.PushMessage(
                        GameReferences.DataOvlRef.StringReferences.GetString(DataOvlReference.WorldStrings
                            .BREAKING_UP), false);
                    // if we hit zero hitpoints then the ship is destroyed and a skiff is boarded
                    if (frigate.Hitpoints <= 0)
                    {
                        tryToMoveResult = TryToMoveResult.ShipDestroyed;
                        // destroy the ship and leave board the Avatar onto a skiff
                        StreamingOutput.Instance.PushMessage(GameReferences.DataOvlRef.StringReferences.GetString(
                            DataOvlReference.WorldStrings2
                                .SHIP_SUNK_BANG_N), false);
                        StreamingOutput.Instance.PushMessage(GameReferences.DataOvlRef.StringReferences
                            .GetString(DataOvlReference.WorldStrings2.ABANDON_SHIP_BANG_N).TrimEnd(), false);

                        MapUnit newFrigate =
                            State.TheVirtualMap.TheMapUnits.XitCurrentMapUnit(State.TheVirtualMap, out string _);
                        State.TheVirtualMap.TheMapUnits.ClearAndSetEmptyMapUnits(newFrigate);
                        State.TheVirtualMap.TheMapUnits.MakeAndBoardSkiff();
                    }
                    else
                    {
                        if (frigate.Hitpoints <= 10)
                        {
                            StreamingOutput.Instance.PushMessage(GameReferences.DataOvlRef.StringReferences.GetString(
                                DataOvlReference.WorldStrings
                                    .HULL_WEAK), false);
                        }

                        tryToMoveResult = TryToMoveResult.ShipBreakingUp;
                    }
                }
                else
                {
                    tryToMoveResult = TryToMoveResult.Blocked;
                    StreamingOutput.Instance.PushMessage(GameReferences.DataOvlRef.StringReferences.GetString(
                        DataOvlReference.TravelStrings
                            .BLOCKED));
                }

                // if it's not passable then we have no more business here
                AdvanceTime(N_DEFAULT_ADVANCE_TIME);
            }

            // the world is a circular - so when you get to the end, start over again
            // this will prevent a never ending growth or shrinking of character position in case the travel the world only moving right a bagillion times
            State.TheVirtualMap.CurrentPosition.X %= nTilesPerMapCol;
            State.TheVirtualMap.CurrentPosition.Y %= nTilesPerMapRow;

            // if you walk on top of a staircase then we will immediately jump to the next floor
            if (GameReferences.SpriteTileReferences.IsStaircase(newTileReference.Index))
            {
                tryToMoveResult = TryToMoveResult.UsedStairs;
                State.TheVirtualMap.UseStairs(State.TheVirtualMap.CurrentPosition.XY);
                if (State.TheVirtualMap.IsStairGoingDown(State.TheVirtualMap.CurrentPosition.XY))
                {
                    StreamingOutput.Instance.PushMessage(
                        GameReferences.DataOvlRef.StringReferences.GetString(DataOvlReference.TravelStrings.DOWN));
                }

                StreamingOutput.Instance.PushMessage(
                    GameReferences.DataOvlRef.StringReferences.GetString(DataOvlReference.TravelStrings.UP));
            }

            Avatar.AvatarState currentAvatarState =
                State.TheVirtualMap.TheMapUnits.GetAvatarMapUnit().CurrentAvatarState;

            int nMinutesToAdvance = 2;

            // if we are on a big map then we may issue extra information about slow moving terrain
            if (State.TheVirtualMap.IsLargeMap)
            {
                nMinutesToAdvance = GameReferences.SpriteTileReferences.GetMinuteIncrement(newTileReference.Index);

                string slowMovingStr = GameReferences.SpriteTileReferences
                    .GetSlowMovementString(newTileReference.Index).TrimEnd();
                if (slowMovingStr != "") StreamingOutput.Instance.PushMessage(slowMovingStr, false);

                // if you are on the carpet or skiff and hit rough seas then we injure the players and report it back 
                if (currentAvatarState is Avatar.AvatarState.Carpet or Avatar.AvatarState.Skiff
                    && newTileReference.Index == 1) // rough seas
                {
                    State.CharacterRecords.RoughSeasInjure();
                    tryToMoveResult = TryToMoveResult.RoughSeas;
                    StreamingOutput.Instance.PushMessage(
                        GameReferences.DataOvlRef.StringReferences.GetString(DataOvlReference.WorldStrings
                            .ROUGH_SEAS), false);
                }
                else
                {
                    tryToMoveResult = TryToMoveResult.Moved;
                }
            }

            tryToMoveResult = TryToMoveResult.Moved;

            // if we are indoors then all walking takes 2 minutes
            AdvanceTime(nMinutesToAdvance);
        }

        public void TryToMoveCombatMap(Point2D.Direction direction, out TryToMoveResult tryToMoveResult) =>
            TryToMoveCombatMap(State.TheVirtualMap.CurrentCombatMap.CurrentCombatPlayer, direction,
                out tryToMoveResult);

        public void TryToMoveCombatMap(CombatPlayer combatPlayer, Point2D.Direction direction,
            out TryToMoveResult tryToMoveResult)
        {
            if (combatPlayer == null)
                throw new Ultima5ReduxException("Trying to move on combat map without a CombatPlayer");

            StreamingOutput.Instance.PushMessage(
                GameReferences.DataOvlRef.StringReferences.GetDirectionString(direction), true, false);

            CombatMap currentCombatMap = State.TheVirtualMap.CurrentCombatMap;
            Debug.Assert(currentCombatMap != null);

            // if we were to move, which direction would we move
            GetAdjustments(direction, out int xAdjust, out int yAdjust);
            Point2D newPosition = new(combatPlayer.MapUnitPosition.X + xAdjust,
                combatPlayer.MapUnitPosition.Y + yAdjust);

            if (IsLeavingMap(newPosition))
            {
                StreamingOutput.Instance.PushMessage("LEAVING", false);

                currentCombatMap.MakePlayerEscape(combatPlayer);

                tryToMoveResult = TryToMoveResult.OfferToExitScreen;
                return;
            }

            if (!State.TheVirtualMap.IsTileFreeToTravel(combatPlayer.MapUnitPosition.XY, newPosition, false,
                    Avatar.AvatarState.Regular))
            {
                StreamingOutput.Instance.PushMessage(" - " +
                                                     GameReferences.DataOvlRef.StringReferences.GetString(
                                                         DataOvlReference.TravelStrings.BLOCKED), false);
                tryToMoveResult = TryToMoveResult.Blocked;
                return;
            }

            currentCombatMap.MoveActiveCombatMapUnit(newPosition);

            tryToMoveResult = TryToMoveResult.Moved;

            currentCombatMap.AdvanceToNextCombatMapUnit();
        }

        /// <summary>
        ///     Opens a door at a specific position
        /// </summary>
        /// <param name="xy">position of door</param>
        /// <param name="bWasSuccessful">was the door opening successful?</param>
        /// <returns>the output string to write to console</returns>
        // ReSharper disable once UnusedMember.Global
        public void TryToOpenDoor(Point2D xy, out bool bWasSuccessful)
        {
            bWasSuccessful = false;

            TileReference tileReference = State.TheVirtualMap.GetTileReference(xy);

            bool isDoorInDirection = tileReference.IsOpenable;

            AdvanceTime(2);

            if (!isDoorInDirection)
            {
                StreamingOutput.Instance.PushMessage(GameReferences.DataOvlRef.StringReferences.GetString(
                    DataOvlReference.OpeningThingsStrings
                        .NOTHING_TO_OPEN), false);
                return;
            }

            bool bIsDoorMagical = GameReferences.SpriteTileReferences.IsDoorMagical(tileReference.Index);
            bool bIsDoorLocked = GameReferences.SpriteTileReferences.IsDoorLocked(tileReference.Index);

            if (bIsDoorMagical || bIsDoorLocked)
            {
                StreamingOutput.Instance.PushMessage(GameReferences.DataOvlRef.StringReferences.GetString(
                    DataOvlReference.OpeningThingsStrings
                        .LOCKED_N), false);
                return;
            }

            State.TheVirtualMap.SetOverridingTileReferece(
                GameReferences.SpriteTileReferences.GetTileReferenceByName("BrickFloor"), xy);

            Map currentMap = State.TheVirtualMap.CurrentMap;
            currentMap.SetOpenDoor(xy);

            bWasSuccessful = true;
            StreamingOutput.Instance.PushMessage(
                GameReferences.DataOvlRef.StringReferences.GetString(DataOvlReference.OpeningThingsStrings.OPENED),
                false);
        }

        /// <summary>
        ///     Try to search an area and see if you find anything!
        /// </summary>
        /// <param name="xy">location to search</param>
        /// <param name="bWasSuccessful">result if it was successful, true if you found one or more things</param>
        /// <returns>the output string to write to console</returns>
        public void TryToSearch(Point2D xy, out bool bWasSuccessful)
        {
            PassTime();
            bWasSuccessful = false;

            // if there is something exposed already OR there is nothing found 
            if (State.TheVirtualMap.HasAnyExposedSearchItems(xy) || !State.TheVirtualMap.ContainsSearchableThings(xy))
            {
                StreamingOutput.Instance.PushMessage(ThouDostFind(
                    GameReferences.DataOvlRef.StringReferences.GetString(DataOvlReference.Vision2Strings
                        .NOTHING_OF_NOTE_DOT_N)), false);
                return;
            }

            // we search the tile and expose any items that may be on it
            int nItems = State.TheVirtualMap.SearchAndExposeItems(xy);

            // it could be a moongate, with a stone, but wrong time of day
            if (nItems == 0)
            {
                StreamingOutput.Instance.PushMessage(ThouDostFind(
                    GameReferences.DataOvlRef.StringReferences.GetString(DataOvlReference.Vision2Strings
                        .NOTHING_OF_NOTE_DOT_N)), false);
                return;
            }

            string searchResultStr = string.Empty;
            bWasSuccessful = true;
            foreach (InventoryItem invRef in State.TheVirtualMap.GetExposedSearchItems(xy))
            {
                searchResultStr += invRef.FindDescription + "\n";
            }

            StreamingOutput.Instance.PushMessage(ThouDostFind(searchResultStr), false);
        }

        public void TryToUsePotion(Potion potion, PlayerCharacterRecord record, out bool bSucceeded,
            out MagicReference.SpellWords spell)
        {
            PassTime();

            bSucceeded = true;

            Debug.Assert(potion.Quantity > 0, $"Can't use potion {potion} because you have quantity {potion.Quantity}");

            spell = _potionColorToSpellMap[potion.Color];
            if (IsCombatMap) State.TheVirtualMap.CurrentCombatMap.AdvanceToNextCombatMapUnit();

            potion.Quantity--;

            StreamingOutput.Instance.PushMessage($"{potion.Color} Potion\n", false);

            switch (potion.Color)
            {
                case Potion.PotionColor.Blue:
                    // awaken
                    record.WakeUp();
                    StreamingOutput.Instance.PushMessage("Awoken!", false);
                    break;
                case Potion.PotionColor.Yellow:
                    // lesser heal - mani
                    int nHealedPoints = record.CastSpellMani();
                    bSucceeded = nHealedPoints >= 0;
                    StreamingOutput.Instance.PushMessage(
                        bSucceeded
                            ? GameReferences.DataOvlRef.StringReferences.GetString(DataOvlReference.ExclaimStrings
                                .HEALED_BANG_N)
                            : GameReferences.DataOvlRef.StringReferences.GetString(DataOvlReference.ExclaimStrings
                                .FAILED_BANG_N), false);
                    break;
                case Potion.PotionColor.Red:
                    // cure poison
                    record.Cure();
                    StreamingOutput.Instance.PushMessage(
                        GameReferences.DataOvlRef.StringReferences.GetString(DataOvlReference.ExclaimStrings
                            .POISON_CURED_BANG_N), false);
                    break;
                case Potion.PotionColor.Green:
                    // poison user
                    bool bWasPoisoned = record.Poison();
                    if (bWasPoisoned)
                        StreamingOutput.Instance.PushMessage(GameReferences.DataOvlRef.StringReferences.GetString(
                            DataOvlReference.ExclaimStrings
                                .POISONED_BANG_N), false);
                    else
                        StreamingOutput.Instance.PushMessage(GameReferences.DataOvlRef.StringReferences.GetString(
                            DataOvlReference.ExclaimStrings
                                .NO_EFFECT_BANG), false);
                    break;
                case Potion.PotionColor.Orange:
                    // sleep
                    CastSleep(record, out bool _);
                    break;
                case Potion.PotionColor.Purple:
                    // turn me into a rat
                    record.TurnIntoRat();
                    StreamingOutput.Instance.PushMessage(GameReferences.DataOvlRef.StringReferences.GetString(
                        DataOvlReference.ExclaimStrings
                            .POOF_BANG_N), false);
                    break;
                case Potion.PotionColor.Black:
                    // invisibility
                    record.TurnInvisible();
                    StreamingOutput.Instance.PushMessage(GameReferences.DataOvlRef.StringReferences.GetString(
                        DataOvlReference.ExclaimStrings
                            .INVISIBLE_BANG_N), false);
                    break;
                case Potion.PotionColor.White:
                    // x-ray
                    bSucceeded = _random.Next() % 2 == 0;
                    if (bSucceeded)
                    {
                        StreamingOutput.Instance.PushMessage("X-Ray!", false);
                        break;
                    }

                    // if you fail with the x-ray then 
                    CastSleep(record, out bool _);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(potion), @"Tried to use an undefined potion");
            }
        }

        public void TryToUseScroll(Scroll scroll, PlayerCharacterRecord record)
        {
            StreamingOutput.Instance.PushMessage($"Scroll: {scroll.ScrollSpell}\n\nA-la-Kazam!", false);

            PassTime();
        }

        public void UseLordBritishArtifactItem(LordBritishArtifact lordBritishArtifact)
        {
            PassTime();

            switch (lordBritishArtifact.Artifact)
            {
                case LordBritishArtifact.ArtifactType.Amulet:
                    StreamingOutput.Instance.PushMessage(GameReferences.DataOvlRef.StringReferences.GetString(
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
                    StreamingOutput.Instance.PushMessage(GameReferences.DataOvlRef.StringReferences.GetString(
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
                    StreamingOutput.Instance.PushMessage(GameReferences.DataOvlRef.StringReferences.GetString(
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
        }

        public void UseMoonstone(Moonstone moonstone, out bool bMoonstoneBuried)
        {
            bMoonstoneBuried = false;
            PassTime();

            if (!IsAllowedToBuryMoongate())
            {
                StreamingOutput.Instance.PushMessage(GameReferences.DataOvlRef.StringReferences.GetString(
                                                         DataOvlReference.ExclaimStrings
                                                             .MOONSTONE_SPACE) +
                                                     GameReferences.DataOvlRef.StringReferences.GetString(
                                                         DataOvlReference.ExclaimStrings
                                                             .CANNOT_BE_BURIED_HERE_BANG_N), false);
                return;
            }

            State.TheMoongates.SetMoonstoneBuried(moonstone.MoongateIndex, true,
                State.TheVirtualMap.GetCurrent3DPosition());
            bMoonstoneBuried = true;

            StreamingOutput.Instance.PushMessage(
                GameReferences.DataOvlRef.StringReferences.GetString(DataOvlReference.ExclaimStrings.MOONSTONE_SPACE) +
                GameReferences.DataOvlRef.StringReferences.GetString(DataOvlReference.ExclaimStrings.BURIED_BANG_N),
                false);
        }

        public void UseShadowLordShard(ShadowlordShard shadowlordShard)
        {
            PassTime();

            string thouHolds(string shard)
            {
                return GameReferences.DataOvlRef.StringReferences.GetString(DataOvlReference.ShadowlordStrings
                    .GEM_SHARD_THOU_HOLD_EVIL_SHARD) + "\n" + shard;
            }

            switch (shadowlordShard.Shard)
            {
                case ShadowlordShard.ShardType.Falsehood:
                    StreamingOutput.Instance.PushMessage(thouHolds(
                        GameReferences.DataOvlRef.StringReferences.GetString(DataOvlReference.ShadowlordStrings
                            .FALSEHOOD_DOT)), false);
                    return;
                case ShadowlordShard.ShardType.Hatred:
                    StreamingOutput.Instance.PushMessage(thouHolds(
                        GameReferences.DataOvlRef.StringReferences.GetString(DataOvlReference.ShadowlordStrings
                            .HATRED_DOT)), false);
                    return;
                case ShadowlordShard.ShardType.Cowardice:
                    StreamingOutput.Instance.PushMessage(thouHolds(
                        GameReferences.DataOvlRef.StringReferences.GetString(DataOvlReference.ShadowlordStrings
                            .COWARDICE_DOT)), false);
                    return;
                default:
                    throw new ArgumentOutOfRangeException(nameof(shadowlordShard),
                        @"Tried to use an undefined shadowlordShard");
            }
        }

        public void UseSpecialItem(SpecialItem spcItem, out bool bWasUsed)
        {
            PassTime();

            bWasUsed = true;
            switch (spcItem.ItemType)
            {
                case SpecialItem.SpecificItemTypeSprite.Carpet:
                    UseMagicCarpet(out bWasUsed);
                    return;
                case SpecialItem.SpecificItemTypeSprite.Grapple:
                    StreamingOutput.Instance.PushMessage("Grapple\n\nYou need to K-limb with it!", false);
                    return;
                case SpecialItem.SpecificItemTypeSprite.Spyglass:
                    StreamingOutput.Instance.PushMessage(GameReferences.DataOvlRef.StringReferences.GetString(
                        DataOvlReference.WearUseItemStrings
                            .SPYGLASS_N_N), false);
                    return;
                case SpecialItem.SpecificItemTypeSprite.HMSCape:
                    StreamingOutput.Instance.PushMessage(GameReferences.DataOvlRef.StringReferences.GetString(
                        DataOvlReference.WearUseItemStrings
                            .PLANS_N_N), false);
                    return;
                case SpecialItem.SpecificItemTypeSprite.PocketWatch:
                    StreamingOutput.Instance.PushMessage(GameReferences.DataOvlRef.StringReferences.GetString(
                        DataOvlReference.WearUseItemStrings
                            .WATCH_N_N_THE_POCKET_W_READS) + " " + State.TheTimeOfDay.FormattedTime, false);
                    return;
                case SpecialItem.SpecificItemTypeSprite.BlackBadge:
                    StreamingOutput.Instance.PushMessage(GameReferences.DataOvlRef.StringReferences.GetString(
                        DataOvlReference.WearUseItemStrings
                            .BADGE_N_N), false);
                    return;
                case SpecialItem.SpecificItemTypeSprite.WoodenBox:
                    StreamingOutput.Instance.PushMessage(GameReferences.DataOvlRef.StringReferences.GetString(
                        DataOvlReference.WearUseItemStrings
                            .BOX_N_HOW_N), false);
                    return;
                case SpecialItem.SpecificItemTypeSprite.Sextant:
                    StreamingOutput.Instance.PushMessage(GameReferences.DataOvlRef.StringReferences.GetString(
                        DataOvlReference.WearUseItemStrings
                            .SEXTANT_N_N), false);
                    return;
                default:
                    throw new ArgumentOutOfRangeException(nameof(spcItem),
                        @"Tried to use an unknown special item");
            }
        }

        /// <summary>
        ///     eXit the current vehicle you are boarded on
        /// </summary>
        /// <param name="bWasSuccessful">did you successfully eXit the vehicle</param>
        /// <returns>string to print out for user</returns>
        public void Xit(out bool bWasSuccessful)
        {
            bWasSuccessful = true;

            MapUnit unboardedMapUnit =
                State.TheVirtualMap.TheMapUnits.XitCurrentMapUnit(State.TheVirtualMap, out string retStr);

            bWasSuccessful = unboardedMapUnit != null;
            StreamingOutput.Instance.PushMessage(retStr);
        }

        /// <summary>
        ///     Yell to hoist or furl the sails
        /// </summary>
        /// <param name="bSailsHoisted">are they now hoisted?</param>
        /// <returns>output string</returns>
        public void YellForSails(out bool bSailsHoisted)
        {
            Debug.Assert(State.TheVirtualMap.TheMapUnits.GetAvatarMapUnit().CurrentBoardedMapUnit is Frigate);

            if (!(State.TheVirtualMap.TheMapUnits.GetAvatarMapUnit().CurrentBoardedMapUnit is Frigate avatarsFrigate))
                throw new Ultima5ReduxException("Tried get Avatar's frigate, but it was null");

            avatarsFrigate.SailsHoisted = !avatarsFrigate.SailsHoisted;
            bSailsHoisted = avatarsFrigate.SailsHoisted;
            if (bSailsHoisted)
            {
                StreamingOutput.Instance.PushMessage(GameReferences.DataOvlRef.StringReferences.GetString(
                    DataOvlReference.KeypressCommandsStrings
                        .YELL) + GameReferences.DataOvlRef.StringReferences
                    .GetString(DataOvlReference.YellingStrings.HOIST_BANG_N).Trim());
                return;
            }

            StreamingOutput.Instance.PushMessage(
                GameReferences.DataOvlRef.StringReferences.GetString(DataOvlReference.KeypressCommandsStrings.YELL) +
                GameReferences.DataOvlRef.StringReferences.GetString(DataOvlReference.YellingStrings.FURL_BANG_N)
                    .Trim());
        }

        public void YellWord(string word)
        {
            // not yet implemented
        }
    }
}