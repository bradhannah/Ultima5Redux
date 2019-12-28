using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using System.IO;

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
        public NonPlayerCharacters NpcRef { get; }
        public DataOvlReference DataOvlRef { get; }
        public TalkScripts TalkScriptsRef { get; }
        public GameState State { get; }
        #endregion
        public LargeMapReference LargeMapRef { get; }

        public Conversation CurrentConversation { get; set; }

        public enum SpecialLookCommand { None, Sign, GemCrystal }

        public World (string ultima5Directory) : base ()
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
            NpcRef = new NonPlayerCharacters(ultima5Directory, SmallMapRef, TalkScriptsRef, State);

            // sadly I have to initilize this after the Npcs are created because there is a circular dependency
            State.InitializeVirtualMap(SmallMapRef, AllSmallMaps, LargeMapRef, OverworldMap, UnderworldMap, NpcRef, SpriteTileReferences);

            State.PlayerInventory.MagicSpells.Items[Spell.SpellWords.An_Ex_Por].GetLiteralTranslation();
            
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
                foreach (NonPlayerCharacters.NonPlayerCharacter npc in NpcRef.NPCs)
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
        /// Looks at a particular tile, detecting if NPCs are present as well
        /// Provides string output or special instructions if it is "special"
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
                NonPlayerCharacters.NonPlayerCharacter npc = State.TheVirtualMap.GetNPCOnTile(xy);
                return DataOvlRef.StringReferences.GetString(DataOvlReference.VISION2_STRINGS.THOU_DOST_SEE).Trim()
                + " " + (LookRef.GetLookDescription(npc.NPCKeySprite).Trim());
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
                   + State.FormattedTime));
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
                State.TheVirtualMap.PickUpThing(xy);
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
            int ladderDown = SpriteTileReferences.GetTileNumberByName("LadderDown");
            int ladderUp = SpriteTileReferences.GetTileNumberByName("LadderUp");

            TileReference tileReference = State.TheVirtualMap.GetTileReference(State.TheVirtualMap.CurrentPosition);//GetCurrentTileNumber();
            if (tileReference.Index == ladderDown)
            {
                if ((hasBasement && nCurrentFloor >= 0) || nCurrentFloor > 0)
                {
                    State.TheVirtualMap.LoadSmallMap(SmallMapRef.GetSingleMapByLocation(location, nCurrentFloor - 1));
                    return DataOvlRef.StringReferences.GetString(DataOvlReference.TRAVEL_STRINGS.DOWN);
                }
            }
            else if (tileReference.Index == ladderUp)
            {
                if (nCurrentFloor + 1 < nTopFloor)
                {
                    State.TheVirtualMap.LoadSmallMap(SmallMapRef.GetSingleMapByLocation(location, nCurrentFloor + 1));
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
        public string TryToJimmyDoor(Point2D xy, CharacterRecord record, out bool bWasSuccesful)
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

        #endregion

        public Conversation CreateConverationAndBegin(NonPlayerCharacters.NonPlayerCharacter npc, Conversation.EnqueuedScriptItem enqueuedScriptItem)
        {
            CurrentConversation = new Conversation(npc, State, DataOvlRef);

            //Conversation.EnqueuedScriptItem enqueuedScriptItemDelegate = new Conversation.EnqueuedScriptItem(this.EnqueuedScriptItem);
            CurrentConversation.EnqueuedScriptItemCallback += enqueuedScriptItem;

            return CurrentConversation;
        }

    

    }
}
