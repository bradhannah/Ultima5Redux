using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Net.Configuration;
using NUnit.Framework;


namespace Ultima5Redux
{
    [SuppressMessage("ReSharper", "MemberCanBePrivate.Global")]
    [TestFixture]
    public class World
    {
        #region Private Variables
        private string u5Directory;
        private CombatMapReference combatMapRef = new CombatMapReference();
        #endregion
        
        #region Private Properties
        /// <summary>
        /// The overworld map object
        /// </summary>
        private LargeMap OverworldMap { get; }
        
        /// <summary>
        /// the underworld map object
        /// </summary>
        private LargeMap UnderworldMap { get; }        
        #endregion

        #region Public Properties

        /// <summary>
        /// Is the Avatar positioned to fall? When falling from multiple floors this will be activated
        /// </summary>
        public bool IsPendingFall { get; private set; } = false;
        
        /// <summary>
        /// A collection of all the available small maps
        /// </summary>
        public SmallMaps AllSmallMaps { get; }

        /// <summary>
        /// A collection of all tile references
        /// </summary>
        public TileReferences SpriteTileReferences { get; }

        /// <summary>
        /// A collection of all small map references
        /// </summary>
        public SmallMapReferences SmallMapRef { get; }
        
        /// <summary>
        /// A collection of all Look references
        /// </summary>
        public Look LookRef { get; }
        /// <summary>
        /// A collection of all Sign references
        /// </summary>
        public Signs SignRef { get; }
        /// <summary>
        /// A collection of all NPC references
        /// </summary>
        public NonPlayerCharacterReferences NpcRef { get; }
        /// <summary>
        /// A collection of data.ovl references
        /// </summary>
        public DataOvlReference DataOvlRef { get; }
        /// <summary>
        /// A collection of all talk script references
        /// </summary>
        public TalkScripts TalkScriptsRef { get; }
        /// <summary>
        /// The current game state
        /// </summary>
        public GameState State { get; }        
        /// <summary>
        /// A large map reference
        /// </summary>
        /// <remarks>needs to be reviewed</remarks>
        public LargeMapReference LargeMapRef { get; }
        /// <summary>
        /// The current conversation object
        /// </summary>
        public Conversation CurrentConversation { get; private set; }
        #endregion
        
        #region Public enumerations
        public enum SpecialLookCommand { None, Sign, GemCrystal }
        #endregion

        public World(string ultima5Directory) : base()
        {
            u5Directory = ultima5Directory;

            // build the overworld map
            OverworldMap = new LargeMap(u5Directory, LargeMap.Maps.Overworld);

            // build the underworld map
            UnderworldMap = new LargeMap(u5Directory, LargeMap.Maps.Underworld);

            DataOvlRef = new DataOvlReference(u5Directory);

            SpriteTileReferences = new TileReferences(DataOvlRef.StringReferences);

            SmallMapRef = new SmallMapReferences(DataOvlRef);

            LargeMapRef = new LargeMapReference(DataOvlRef, SmallMapRef);

            AllSmallMaps = new SmallMaps(SmallMapRef, u5Directory, SpriteTileReferences);

            State = new GameState(u5Directory, DataOvlRef);

            // build all combat maps from the Combat Map References
            foreach (CombatMapReference.SingleCombatMapReference combatMapRef in combatMapRef.MapReferenceList)
            {
                CombatMap combatMap = new CombatMap(u5Directory, combatMapRef);
            }

            // build a "look" table for all tiles
            LookRef = new Look(ultima5Directory);

            // build the sign tables
            SignRef = new Signs(ultima5Directory);

            TalkScriptsRef = new TalkScripts(u5Directory, DataOvlRef);

            // build the NPC tables
            NpcRef = new NonPlayerCharacterReferences(ultima5Directory, SmallMapRef, TalkScriptsRef, State);


            // sadly I have to initialize this after the NPCs are created because there is a circular dependency
            State.InitializeVirtualMap(SmallMapRef, AllSmallMaps, LargeMapRef, OverworldMap, UnderworldMap, NpcRef, SpriteTileReferences, State, NpcRef);

            if (State.Location != SmallMapReferences.SingleMapReference.Location.Britannia_Underworld)
            {
                State.TheVirtualMap.LoadSmallMap(SmallMapRef.GetSingleMapByLocation(State.Location, State.Floor), State.CharacterRecords, true);
            }
            else
            {
                State.TheVirtualMap.LoadLargeMap(LargeMap.Maps.Overworld);
            }

        }

        #region World actions - do a thing, report or change the state
        /// <summary>
        /// Advances time and takes care of all day, month, year calculations
        /// </summary>
        /// <param name="nMinutes">Number of minutes to advance (maximum of 9*60)</param>
        public void AdvanceTime(int nMinutes)
        {
            State.TheTimeOfDay.AdvanceClock(nMinutes);

            State.TheVirtualMap.MoveNPCs();
        }

        /// <summary>
        /// Looks at a particular tile, detecting if NPCs are present as well
        /// Provides string output or special instructions if it is "special"B
        /// </summary>
        /// <param name="xy">positon of tile to look at</param>
        /// <param name="specialLookCommand">Special command such as look at gem or sign</param>
        /// <returns>String to output to user</returns>
        public string Look(Point2D xy, out SpecialLookCommand specialLookCommand)
        {
            specialLookCommand = SpecialLookCommand.None;

            TileReference tileReference = State.TheVirtualMap.GetTileReference(xy);
            // if there is an NPC on the tile, then we assume they want to look at the NPC, not whatever else may be on the tiles
            if (State.TheVirtualMap.IsNPCTile(xy))
            {
                MapCharacter mapCharacter = State.TheVirtualMap.GetNPCOnTile(xy);
                return DataOvlRef.StringReferences.GetString(DataOvlReference.VISION2_STRINGS.THOU_DOST_SEE).Trim()
                + " " + (LookRef.GetLookDescription(mapCharacter.NPCRef.NPCKeySprite).Trim());
            }
            // if we are any one of these signs then we superimpose it on the screen
            else if (SpriteTileReferences.IsSign(tileReference.Index))
            {
                specialLookCommand = SpecialLookCommand.Sign;
                return String.Empty;
            }
            else if (SpriteTileReferences.GetTileNumberByName("Clock1") == tileReference.Index)
            {
                return (DataOvlRef.StringReferences.GetString(DataOvlReference.VISION2_STRINGS.THOU_DOST_SEE).Trim()
                   + " " + (LookRef.GetLookDescription(tileReference.Index).TrimStart()
                   + State.TheTimeOfDay.FormattedTime));
            }
            else // lets see what we've got here!
            {
                return (DataOvlRef.StringReferences.GetString(DataOvlReference.VISION2_STRINGS.THOU_DOST_SEE).Trim()
                    + " " + (LookRef.GetLookDescription(tileReference.Index).TrimStart()));
            }
        }

        /// <summary>
        /// Gets a thing from the world, adding it the inventory and providing output for console
        /// </summary>
        /// <param name="xy">where is the thing?</param>
        /// <param name="bGotAThing">did I get a thing?</param>
        /// <returns>the output string</returns>
        public string GetAThing(Point2D xy, out bool bGotAThing)
        {
            bGotAThing = false;
            TileReference tileReference = State.TheVirtualMap.GetTileReference(xy);

            if (tileReference.Index == SpriteTileReferences.GetTileNumberByName("LeftSconce") ||
                tileReference.Index == SpriteTileReferences.GetTileNumberByName("RightSconce"))
            {
                State.Torches++;
                State.TheVirtualMap.SetOverridingTileReferece(SpriteTileReferences.GetTileReferenceByName("BrickFloor"), xy);//PickUpThing(xy);
                bGotAThing = true;
                return (DataOvlRef.StringReferences.GetString(DataOvlReference.GET_THINGS_STRINGS.BORROWED));
            }
            return DataOvlRef.StringReferences.GetString(DataOvlReference.GET_THINGS_STRINGS.NOTHING_TO_GET);
        }

        /// <summary>
        /// Climbs the ladder on the current tile that the Avatar occupies
        /// </summary>
        public string KlimbLadder()
        {
            SmallMapReferences.SingleMapReference.Location location = State.TheVirtualMap.CurrentSingleMapReference.MapLocation;
            int nCurrentFloor = State.TheVirtualMap.CurrentSingleMapReference.Floor; //currentSingleSmallMapReferences.Floor;
            bool hasBasement = State.TheVirtualMap.SmallMapRefs.HasBasement(location);
            int nTotalFloors = State.TheVirtualMap.SmallMapRefs.GetNumberOfFloors(location);
            int nTopFloor = hasBasement ? nTotalFloors - 1 : nTotalFloors;

            TileReference tileReference = State.TheVirtualMap.GetTileReference(State.TheVirtualMap.CurrentPosition);
            if (SpriteTileReferences.IsLadderDown(tileReference.Index) || SpriteTileReferences.IsGrate(tileReference.Index)) 
            {
                if ((hasBasement && nCurrentFloor >= 0) || nCurrentFloor > 0)
                {
                    State.TheVirtualMap.LoadSmallMap(SmallMapRef.GetSingleMapByLocation(location, nCurrentFloor - 1), State.CharacterRecords, false);
                    return DataOvlRef.StringReferences.GetString(DataOvlReference.TRAVEL_STRINGS.DOWN);
                }
            }
            else if (SpriteTileReferences.IsLadderUp(tileReference.Index))
            {
                if (nCurrentFloor + 1 < nTopFloor)
                {
                    State.TheVirtualMap.LoadSmallMap(SmallMapRef.GetSingleMapByLocation(location, nCurrentFloor + 1), State.CharacterRecords, false);
                    return DataOvlRef.StringReferences.GetString(DataOvlReference.TRAVEL_STRINGS.UP);
                }
            }
            return string.Empty;
        }

        public enum KlimbResult { Success, SuccessFell, CantKlimb }
        /// <summary>
        /// Try to klimb the given tile - typically called after you select a direction
        /// </summary>
        /// <param name="xy"></param>
        /// <param name="klimbResult"></param>
        /// <returns></returns>
        public string TryToKlimb(Point2D xy, out KlimbResult klimbResult)
        {
            //Point2D klimbTilePos = GetAdustedPos(CurrentVirtualMap.CurrentPosition, keyCode);
            TileReference tileReference = State.TheVirtualMap.GetTileReference(xy);

            if (State.TheVirtualMap.IsLargeMap)
            {
                // is it even klimbable?
                if (tileReference.IsKlimable)
                {
                    if (tileReference.Index == SpriteTileReferences.GetTileNumberByName("SmallMountains"))
                    {
                        State.GrapplingFall();
                        klimbResult = KlimbResult.SuccessFell;
                        return DataOvlRef.StringReferences.GetString(DataOvlReference.KLIMBING_STRINGS.FELL);
                    }
                    throw new Ultima5ReduxException("I am not personal aware of what on earth you would be klimbing that is not already stated in the following logic...");
                }
                // is it tall mountains? we can't klimb those
                else if (tileReference.Index == SpriteTileReferences.GetTileNumberByName("TallMountains"))
                {
                    klimbResult = KlimbResult.CantKlimb;
                    return DataOvlRef.StringReferences.GetString(DataOvlReference.KLIMBING_STRINGS.IMPASSABLE);
                }
                // there is no chance of klimbing the thing
                else
                {
                    klimbResult = KlimbResult.CantKlimb;
                    return DataOvlRef.StringReferences.GetString(DataOvlReference.KLIMBING_STRINGS.NOT_CLIMABLE);
                }
            }
            else // it's a small map
            {
                if (tileReference.IsKlimable)
                {
                    // ie. a fence
                    klimbResult = KlimbResult.Success;
                    return String.Empty;
                }
                else
                {
                    klimbResult = KlimbResult.CantKlimb;
                    return DataOvlRef.StringReferences.GetString(DataOvlReference.TRAVEL_STRINGS.WHAT);
                }
            }

        }

        /// <summary>
        /// Try to jimmy the door with a given character
        /// </summary>
        /// <param name="xy">position of the door</param>
        /// <param name="record">character who will attempt to open it</param>
        /// <param name="bWasSuccesful">was it a successful jimmy?</param>
        /// <returns></returns>
        public string TryToJimmyDoor(Point2D xy, PlayerCharacterRecord record, out bool bWasSuccesful)
        {
            bWasSuccesful = false;
            //Point2D doorPos = GetAdustedPos(CurrentVirtualMap.CurrentPosition, keyCode);
            //int doorTileSprite = GetTileNumber(doorPos.X, doorPos.Y);
            TileReference tileReference = State.TheVirtualMap.GetTileReference(xy);
            bool isDoorInDirection = tileReference.IsOpenable;

            if (!isDoorInDirection)
            {
                return (DataOvlRef.StringReferences.GetString(DataOvlReference.OPENING_THINGS_STRINGS.NO_LOCK));
            }

            bool bIsDoorMagical = SpriteTileReferences.IsDoorMagical(tileReference.Index);
            bool bIsDoorLocked = SpriteTileReferences.IsDoorLocked(tileReference.Index);

            if (bIsDoorMagical)
            {
                // we use up a key
                State.Keys--;

                // for now we will also just open the door so we can get around - will address when we have spells
                if (SpriteTileReferences.IsDoorWithView(tileReference.Index))
                {
                    State.TheVirtualMap.SetOverridingTileReferece(SpriteTileReferences.GetTileReferenceByName("RegularDoorView"), xy);
                }
                else
                {
                    State.TheVirtualMap.SetOverridingTileReferece(SpriteTileReferences.GetTileReferenceByName("RegularDoor"), xy);
                }
                //ReassignSprites();
                bWasSuccesful = true;

                return (DataOvlRef.StringReferences.GetString(DataOvlReference.OPENING_THINGS_STRINGS.KEY_BROKE));
            }
            if (bIsDoorLocked)
            {
                // we use up a key
                State.Keys--;

                // todo: bh: we will need to determine the likelihood of lock picking success, for now, we always succeed

                // bh: open player selection dialog here
                //record.

                // bh: assume it's me for now

                if (SpriteTileReferences.IsDoorWithView(tileReference.Index))
                {
                    State.TheVirtualMap.SetOverridingTileReferece(SpriteTileReferences.GetTileReferenceByName("RegularDoorView"), xy);
                }
                else
                {
                    State.TheVirtualMap.SetOverridingTileReferece(SpriteTileReferences.GetTileReferenceByName("RegularDoor"), xy);
                }
                //ReassignSprites();
                bWasSuccesful = true;
                return (DataOvlRef.StringReferences.GetString(DataOvlReference.OPENING_THINGS_STRINGS.UNLOCKED));
            }
            else
            {
                return (DataOvlRef.StringReferences.GetString(DataOvlReference.OPENING_THINGS_STRINGS.NO_LOCK));
            }
        }

        /// <summary>
        /// Opens a door at a specific position
        /// </summary>
        /// <param name="xy">position of door</param>
        /// <param name="bWasSuccessful">was the door opening successful?</param>
        /// <returns>the output string to write to console</returns>
        public string OpenDoor(Point2D xy, out bool bWasSuccessful)
        {
            bWasSuccessful = false;

            TileReference tileReference = State.TheVirtualMap.GetTileReference(xy);

            bool isDoorInDirection = tileReference.IsOpenable;

            if (!isDoorInDirection)
            {
                return (DataOvlRef.StringReferences.GetString(DataOvlReference.OPENING_THINGS_STRINGS.NOTHING_TO_OPEN));
            }

            bool bIsDoorMagical = SpriteTileReferences.IsDoorMagical(tileReference.Index);
            bool bIsDoorLocked = SpriteTileReferences.IsDoorLocked(tileReference.Index);

            if (bIsDoorMagical || bIsDoorLocked)
            {
                return (DataOvlRef.StringReferences.GetString(DataOvlReference.OPENING_THINGS_STRINGS.LOCKED_N));
            }
            else
            {
                State.TheVirtualMap.SetOverridingTileReferece(SpriteTileReferences.GetTileReferenceByName("BrickFloor"), xy);
                bWasSuccessful = true;
                return (DataOvlRef.StringReferences.GetString(DataOvlReference.OPENING_THINGS_STRINGS.OPENED));
            }
        }

        public enum TryToMoveResult { Moved, Blocked, OfferToExitScreen, UsedStairs, Fell }

        /// <summary>
        /// Gets a +/- 1 x/y adjustement based on the current position and given direction
        /// </summary>
        /// <param name="direction">direction to go</param>
        /// <param name="xAdjust">output X adjustment</param>
        /// <param name="yAdjust">output Y adjustment</param>
        private void GetAdjustments(VirtualMap.Direction direction, out int xAdjust, out int yAdjust)
        {
            xAdjust = 0;
            yAdjust = 0;

            // if you are on a repeating map then you should assume that the adjust suc

            switch (direction)
            {
                case VirtualMap.Direction.Down:
                    if (State.TheVirtualMap.CurrentPosition.Y < State.TheVirtualMap.NumberOfRowTiles - 1 || State.TheVirtualMap.IsLargeMap) yAdjust = 1;
                    break;
                case VirtualMap.Direction.Up:
                    if (State.TheVirtualMap.CurrentPosition.Y > 0 || State.TheVirtualMap.IsLargeMap) yAdjust = -1;
                    break;
                case VirtualMap.Direction.Right:
                    if (State.TheVirtualMap.CurrentPosition.X < State.TheVirtualMap.NumberOfColumnTiles - 1 || State.TheVirtualMap.IsLargeMap) xAdjust = 1;
                    break;
                case VirtualMap.Direction.Left:
                    if (State.TheVirtualMap.CurrentPosition.X > 0 || State.TheVirtualMap.IsLargeMap) xAdjust = -1;
                    break;
                case VirtualMap.Direction.None:
                    // do nothing, no adjustment
                    break;
                default:
                    throw new Ultima5ReduxException("Requested an adjustment but didn't provide a KeyCode that represents a direction.");
            }
        }

        /// <summary>
        /// Tries to move the avatar in a given direction - if succesful it will move him
        /// </summary>
        /// <param name="direction">the direction you want to move</param>
        /// <param name="bKlimb">is the avatar K-limbing?</param>
        /// <param name="bFreeMove">is "free move" on?</param>
        /// <param name="tryToMoveResult">outputs the result of the attempt</param>
        /// <returns>output string (may be empty)</returns>
        public string TryToMove(VirtualMap.Direction direction, bool bKlimb, bool bFreeMove, out TryToMoveResult tryToMoveResult)
        {
            int nTilesPerMapRow = State.TheVirtualMap.NumberOfRowTiles;
            int nTilesPerMapCol = State.TheVirtualMap.NumberOfColumnTiles;

            // if we were to move, which direction would we move
            GetAdjustments(direction, out int xAdjust, out int yAdjust);

            // would we be leaving a small map if we went forward?
            if (!State.TheVirtualMap.IsLargeMap &&
                (State.TheVirtualMap.CurrentPosition.Y == (nTilesPerMapRow - 1) && direction == VirtualMap.Direction.Down) ||
                (State.TheVirtualMap.CurrentPosition.Y == (0) && direction == VirtualMap.Direction.Up) ||
                (State.TheVirtualMap.CurrentPosition.X == (nTilesPerMapCol - 1) && direction == VirtualMap.Direction.Right) ||
                (State.TheVirtualMap.CurrentPosition.X == (0) && direction == VirtualMap.Direction.Left))

            {
                tryToMoveResult = TryToMoveResult.OfferToExitScreen;
                // it is expected that the called will offer an exit option, but we won't move the avatar because the space
                // is empty
                return string.Empty;
            }

            // calculate our new x and y values based on the adjustments
            int newX = (State.TheVirtualMap.CurrentPosition.X + xAdjust) % nTilesPerMapCol;
            int newY = (State.TheVirtualMap.CurrentPosition.Y + yAdjust) % nTilesPerMapRow;
            Point2D newPos = new Point2D(newX, newY);

            // if we have reached 0, and we are adjusting -1 then we should assume it's a round world and we are going to the opposite side
            // this should only be true if it is a RepeatMap
            if (newX < 0) { Debug.Assert(State.TheVirtualMap.IsLargeMap, "You should not reach the very end of a map +/- 1 if you are not on a repeating map"); newX = nTilesPerMapCol + newX; }
            if (newY < 0) { Debug.Assert(State.TheVirtualMap.IsLargeMap, "You should not reach the very end of a map +/- 1 if you are not on a repeating map"); newY = nTilesPerMapRow + newY; }

            // we get the newTile so that we can determine if it's passable
            //int newTile = GetTileNumber(newX, newY);
            TileReference newTileReference = State.TheVirtualMap.GetTileReference(newX, newY);

            if (newTileReference.Index == SpriteTileReferences.GetTileNumberByName("BrickFloorHole"))
            {
                State.TheVirtualMap.UseStairs(newPos, true);
                tryToMoveResult = TryToMoveResult.Fell;
                // we need to evaluate in the game and let the game know that they should continue to fall
                TileReference newTileRef = State.TheVirtualMap.GetTileReference(State.TheVirtualMap.CurrentPosition);
                if (newTileRef.Index == SpriteTileReferences.GetTileNumberByName("BrickFloorHole"))
                {
                    IsPendingFall = true;
                }

                // todo: get string from data file
                return ("A TRAPDOOR!");
            }

            // we have evaluated and now know there is not a further fall (think Blackthorne's palace)
            IsPendingFall = false;
            
            // it's passable if it's marked as passable, 
            // but we double check if the portcullis is down
            bool bPassable = newTileReference.IsWalking_Passable &&
                !(SpriteTileReferences.GetTileNumberByName("BrickWallArchway") == newTileReference.Index && !State.TheTimeOfDay.IsDayLight)
                && !State.TheVirtualMap.IsNPCTile(newPos);


            // this is insufficient in case I am in a boat
            if (bPassable || bFreeMove || (bKlimb && newTileReference.IsKlimable))
            {
                State.TheVirtualMap.CurrentPosition.X = newX;
                State.TheVirtualMap.CurrentPosition.Y = newY;
            }
            else
            {
                tryToMoveResult = TryToMoveResult.Blocked;
                // if it's not passable then we have no more business here
                AdvanceTime(2);
                return (DataOvlRef.StringReferences.GetString(DataOvlReference.TRAVEL_STRINGS.BLOCKED));
            }

            // the world is a circular - so when you get to the end, start over again
            // this will prevent a never ending growth or shrinking of character position in case the travel the world only moving right a bagillion times
            State.TheVirtualMap.CurrentPosition.X %= nTilesPerMapCol;
            State.TheVirtualMap.CurrentPosition.Y %= nTilesPerMapRow;

            // if you walk on top of a staircase then we will immediately jump to the next floor
            if (SpriteTileReferences.IsStaircase(newTileReference.Index))
            {
                tryToMoveResult = TryToMoveResult.UsedStairs;
                State.TheVirtualMap.UseStairs(State.TheVirtualMap.CurrentPosition);
                return string.Empty;
            }

            // if we are on a big map then we may issue extra information about slow moving terrain
            if (State.TheVirtualMap.IsLargeMap)
            {
                AdvanceTime(SpriteTileReferences.GetMinuteIncrement(newTileReference.Index));

                string strMovement = String.Empty;
                // we don't want to lookup slow moving strings if we are moving freely over all tiles
                if (!bFreeMove)
                {
                    strMovement = SpriteTileReferences.GetSlowMovementString(newTileReference.Index);
                }
                if (strMovement != String.Empty)
                {
                }
                tryToMoveResult = TryToMoveResult.Moved;
                return strMovement;
            }
            else
            {
                tryToMoveResult = TryToMoveResult.Moved;

                // if we are indoors then all walking takes 2 minutes
                AdvanceTime(2);

                return string.Empty;
            }
        }

        /// <summary>
        /// Attempt to enter a building at a coordinate
        /// Will load new map if successful
        /// </summary>
        /// <param name="xy">position of building</param>
        /// <param name="bWasSuccessful">true if successfully entered</param>
        /// <returns>output string</returns>
        public string EnterBuilding(Point2D xy, out bool bWasSuccessful)
        {
            bool isOnBuilding = LargeMapRef.IsMapXYEnterable(State.TheVirtualMap.CurrentPosition);

            if (isOnBuilding)
            {
                SmallMapReferences.SingleMapReference.Location location = LargeMapRef.GetLocationByMapXY(State.TheVirtualMap.CurrentPosition);
                //Point2D startingXY = SmallMapReferences.GetStartingXYByLocation(location);
                State.TheVirtualMap.LoadSmallMap(SmallMapRef.GetSingleMapByLocation(location, 0), State.CharacterRecords, false);
                // set us to the front of the building
                State.TheVirtualMap.CurrentPosition = SmallMapReferences.GetStartingXYByLocation(location);

                string returnStr =
                    (DataOvlRef.StringReferences.GetString(DataOvlReference.WORLD_STRINGS.ENTER_SPACE)
                    + SmallMapRef.GetLocationTypeStr(location)) + "\n" +
                    SmallMapRef.GetLocationName(location);
                bWasSuccessful = true;
                return returnStr;
            }
            else
            {
                string enterWhatStr = DataOvlRef.StringReferences.GetString(DataOvlReference.WORLD_STRINGS.ENTER_SPACE)
                    + DataOvlRef.StringReferences.GetString(DataOvlReference.WORLD_STRINGS.WHAT);
                bWasSuccessful = false;
                return enterWhatStr;
            }
        }

        #endregion

        /// <summary>
        /// Begins the conversation with a particular NPC 
        /// </summary>
        /// <param name="npc">the NPC to have a conversation with</param>
        /// <param name="enqueuedScriptItem">a handler to be called when script items are enqueued</param>
        /// <returns>A conversation object to be used to follow along with the conversation</returns>
        public Conversation CreateConversationAndBegin(NonPlayerCharacterReference npc, Conversation.EnqueuedScriptItem enqueuedScriptItem)
        {
            CurrentConversation = new Conversation(npc, State, DataOvlRef);

            CurrentConversation.EnqueuedScriptItemCallback += enqueuedScriptItem;

            return CurrentConversation;
        }

        #region Test Procedures
        /// <summary>
        /// Constructor only used for testing
        /// </summary>
        public World() : this(@"C:\Games\Ultima_5\Gold")
        {
            
        }

        [Test]
        public void Test_BasicConversation()
        {
            foreach (NonPlayerCharacterReference npc in NpcRef.NPCs)
            {
                Conversation convo = new Conversation(npc, State, DataOvlRef); 
                Debug.Assert(convo != null);
            }
        }
        

        [Test]
        public void Test_Signs()
        {
            Signs.Sign sign = SignRef.GetSign(SmallMapReferences.SingleMapReference.Location.Yew, 16, 2);
            Signs.Sign sign2 = SignRef.GetSign(42);
            
            Assert.True(sign != null);
            Assert.True(sign2 != null);
        }
        #endregion
    }
}
