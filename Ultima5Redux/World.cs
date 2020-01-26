using System;
using System.Diagnostics;

namespace Ultima5Redux
{
    public class World
    {
        #region Private Variables

        //private List<SmallMap> smallMaps = new List<SmallMap>();
        public SmallMaps AllSmallMaps { get; }

        public LargeMap OverworldMap { get; }
        public LargeMap UnderworldMap { get; }

        public TileReferences SpriteTileReferences { get; }

        private string u5Directory;
        public SmallMapReferences SmallMapRef;
        private CombatMapReference combatMapRef = new CombatMapReference();
        public Look LookRef { get; }
        public Signs SignRef { get; }
        public NonPlayerCharacterReferences NpcRef { get; }
        public DataOvlReference DataOvlRef { get; }
        public TalkScripts TalkScriptsRef { get; }
        public GameState State { get; }
        #endregion
        public LargeMapReference LargeMapRef { get; }

        public Conversation CurrentConversation { get; set; }

        public enum SpecialLookCommand { None, Sign, GemCrystal }

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
            //CharacterRecord character = State.GetCharacterFromParty(0);
            //CharacterRecord character3 = State.GetCharacterFromParty(3);
            //CharacterRecord character4 = State.GetCharacterFromParty(4);
            //character.Equipped.Amulet = DataOvlReference.EQUIPMENT.Ankh;
            //CharacterRecord character2 = State.GetCharacterFromParty(0);


            // build all the small maps from the Small Map reference
            //foreach (SmallMapReferences.SingleMapReference mapRef in SmallMapRef.MapReferenceList)
            //{
            //    // now I can go through each and every reference
            //    SmallMap smallMap = new SmallMap(u5Directory, mapRef);
            //    smallMaps.Add(smallMap);
            //    //U5Map.PrintMapSection(smallMap.RawMap, 0, 0, 32, 32);
            //}

            // build all combat maps from the Combat Map References
            foreach (CombatMapReference.SingleCombatMapReference combatMapRef in combatMapRef.MapReferenceList)
            {
                CombatMap combatMap = new CombatMap(u5Directory, combatMapRef);
                //System.Console.WriteLine("\n");
                //System.Console.WriteLine(combatMap.Description);
                //Map.PrintMapSection(combatMap.RawMap, 0, 0, 11, 11);
            }

            // build a "look" table for all tiles
            LookRef = new Look(ultima5Directory);

            // build the sign tables
            SignRef = new Signs(ultima5Directory);

            //Signs.Sign sign = SignRef.GetSign(SmallMapReferences.SingleMapReference.Location.Yew, 16, 2);
            Signs.Sign sign = SignRef.GetSign(42);

            string str = sign.SignText;
            TalkScriptsRef = new TalkScripts(u5Directory, DataOvlRef);

            // build the NPC tables
            NpcRef = new NonPlayerCharacterReferences(ultima5Directory, SmallMapRef, TalkScriptsRef, State);

            //CharAnimationStates = new MapCharacterAnimationStates(State, SpriteTileReferences);
            //CharStates = new MapCharacterStates(State, SpriteTileReferences);



            // sadly I have to initilize this after the Npcs are created because there is a circular dependency
            State.InitializeVirtualMap(SmallMapRef, AllSmallMaps, LargeMapRef, OverworldMap, UnderworldMap, NpcRef, SpriteTileReferences, State, NpcRef);

            if (State.Location != SmallMapReferences.SingleMapReference.Location.Britainnia_Underworld)
            {
                State.TheVirtualMap.LoadSmallMap(SmallMapRef.GetSingleMapByLocation(State.Location, State.Floor), State.CharacterRecords, true);
            }
            else
            {
                State.TheVirtualMap.LoadLargeMap(LargeMap.Maps.Overworld);
            }


            //State.TheVirtualMap.LoadSmallMap()
            //State.PlayerInventory.MagicSpells.Items[Spell.SpellWords.An_Ex_Por].GetLiteralTranslation();

            //State.TheVirtualMap.LoadSmallMap(SmallMapRef.GetSingleMapByLocation(SmallMapReferences.SingleMapReference.Location.Serpents_Hold, 0), false);

            //int nSpriteGuess = State.TheVirtualMap.GuessTile(new Point2D(15, 15));
            //NpcRef.GetNonPlayerCharacter(SmallMapReferences.SingleMapReference.Location.Britain, new Point2D(0, 31), 0);


            //State.Year = 100;
            //State.Month = 13;
            //State.Day = 28;
            //State.Hour = 22;
            //State.Minute = 2;


            //Conversation convo = new Conversation(NpcRef.NPCs[21]); // dunkworth
            //            Conversation convo = new Conversation(NpcRef.NPCs[296], State, DataOvlRef); // Gwenno
            // 19 = Margarett
            //           NpcRef.NPCs[296].Script.PrintComprehensiveScript();

            int count = 0;
            if (false)
            {
                foreach (NonPlayerCharacterReference npc in NpcRef.NPCs)
                {
                    if (npc.NPCType != 0 && npc.Script != null)
                    {
                        Console.WriteLine("");
                        Console.WriteLine("---- SCRIPT for " + npc.Name.Trim() + " -----");
                        //Npc.Script.PrintScript();
                        npc.Script.PrintComprehensiveScript();

                        if (npc.Name.Trim() == "Geoffrey")
                        {
                            Console.WriteLine(npc.NPCType.ToString());

                        }
                    }
                    count++;
                }
            }

            // Scally
            //Conversation convo = new Conversation(NpcRef.NPCs[0xe6], State, DataOvlRef);

            // Bidney
            //Conversation convo = new Conversation(NpcRef.NPCs[0xe8], State);

            // Lord Dalgrin
            //Conversation convo = new Conversation(NpcRef.NPCs[0xea], State);

            // Geoffery
            //Conversation convo = new Conversation(NpcRef.NPCs[0xec], State, DataOvlRef);

            // Tierra 
            //Conversation convo = new Conversation(NpcRef.NPCs[0xeb], State, DataOvlRef);

            //            Conversation.EnqueuedScriptItem enqueuedScriptItemDelegate = new Conversation.EnqueuedScriptItem(this.EnqueuedScriptItem);
            //            convo.EnqueuedScriptItemCallback += enqueuedScriptItemDelegate;

            // convo.BeginConversation();

            //0x48 or 0x28
            //Console.WriteLine("Shutting down... Hit any key...");
            //Console.ReadKey(false);

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

            TileReference tileReference = State.TheVirtualMap.GetTileReference(State.TheVirtualMap.CurrentPosition);//GetCurrentTileNumber();
            if (SpriteTileReferences.IsLadderDown(tileReference.Index)) //tileReference.Index == ladderDown)
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
                // is it even klimable?
                if (tileReference.IsKlimable)
                {
                    if (tileReference.Index == SpriteTileReferences.GetTileNumberByName("SmallMountains"))
                    {
                        State.GrapplingFall();
                        klimbResult = KlimbResult.SuccessFell;
                        return DataOvlRef.StringReferences.GetString(DataOvlReference.KLIMBING_STRINGS.FELL);
                    }
                    throw new Exception("I am not personnaly aware of what on earth you would be klimbing that is not already stated in the following logic...");
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
        /// <param name="bWasSuccesful">was the door opening succesful?</param>
        /// <returns>the output string to write to console</returns>
        public string OpenDoor(Point2D xy, out bool bWasSuccesful)
        {
            bWasSuccesful = false;

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
                bWasSuccesful = true;
                return (DataOvlRef.StringReferences.GetString(DataOvlReference.OPENING_THINGS_STRINGS.OPENED));
            }
        }

        public enum TryToMoveResult { Moved, Blocked, OfferToExitScreen, UsedStairs }

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
                default:
                    throw new Exception("Requested an adjustment but didn't provide a KeyCode that represents a direction.");
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
            int xAdjust = 0;
            int yAdjust = 0;

            int nTilesPerMapRow = State.TheVirtualMap.NumberOfRowTiles;
            int nTilesPerMapCol = State.TheVirtualMap.NumberOfColumnTiles;

            // if we were to move, which direction would we move
            GetAdjustments(direction, out xAdjust, out yAdjust);

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
            // it's passable if it's marked as passable, 
            // but we double check if the portcullis is down
            bool bPassable = newTileReference.IsWalking_Passable &&
                !(SpriteTileReferences.GetTileNumberByName("BrickWallArchway") == newTileReference.Index && !State.TheTimeOfDay.IsDayLight)
                && !State.TheVirtualMap.IsNPCTile(newPos);


            // this isinsufficient in case I am in a boat
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
                Point2D startingXY = SmallMapReferences.GetStartingXYByLocation(location);
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

        public Conversation CreateConverationAndBegin(NonPlayerCharacterReference npc, Conversation.EnqueuedScriptItem enqueuedScriptItem)
        {
            CurrentConversation = new Conversation(npc, State, DataOvlRef);

            //Conversation.EnqueuedScriptItem enqueuedScriptItemDelegate = new Conversation.EnqueuedScriptItem(this.EnqueuedScriptItem);
            CurrentConversation.EnqueuedScriptItemCallback += enqueuedScriptItem;

            return CurrentConversation;
        }



    }
}
