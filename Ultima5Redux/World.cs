using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Net.Configuration;
using System.Resources;
using Ultima5Redux.Data;
using Ultima5Redux.DayNightMoon;
using Ultima5Redux.Dialogue;
using Ultima5Redux.Maps;
using Ultima5Redux.MapUnits;
using Ultima5Redux.MapUnits.NonPlayerCharacters;
using Ultima5Redux.MapUnits.SeaFaringVessels;
using Ultima5Redux.PlayerCharacters;
using Ultima5Redux.PlayerCharacters.Inventory;

namespace Ultima5Redux
{
    [SuppressMessage("ReSharper", "MemberCanBePrivate.Global")]
    public class World
    {
        /// <summary>
        /// Ultima 5 data and save files directory 
        /// </summary>
        private readonly string _u5Directory;
        private readonly CombatMapReference _combatMapRef = new CombatMapReference();
        private readonly TileOverrides _tileOverrides = new TileOverrides();

        private const int N_DEFAULT_ADVANCE_TIME = 2;
        
        /// <summary>
        /// The overworld map object
        /// </summary>
        private LargeMap OverworldMap { get; }
        
        /// <summary>
        /// the underworld map object
        /// </summary>
        private LargeMap UnderworldMap { get; }        

        /// <summary>
        /// Is the Avatar positioned to fall? When falling from multiple floors this will be activated
        /// </summary>
        public bool IsPendingFall { get; private set; }
        
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
        /// Detailed inventory information reference
        /// </summary>
        public InventoryReferences InvRef { get; }
        /// <summary>
        /// A large map reference
        /// </summary>
        /// <remarks>needs to be reviewed</remarks>
        public LargeMapLocationReferences LargeMapRef { get; }
        /// <summary>
        /// The current conversation object
        /// </summary>
        public Conversation CurrentConversation { get; private set; }

        public ShoppeKeeperDialogueReference ShoppeKeeperDialogueReference { get; private set; }

        public MoonPhaseReferences MoonPhaseRefs { get; private set; }
        
        /// <summary>
        /// Special things that can be looked at in the world that will require special consideration
        /// </summary>
        public enum SpecialLookCommand { None, Sign, GemCrystal }

        /// <summary>
        /// Constructor 
        /// </summary>
        /// <param name="ultima5Directory">ultima 5 data and save game directory</param>
        public World(string ultima5Directory) : base()
        {
            _u5Directory = ultima5Directory;

            DataOvlRef = new DataOvlReference(_u5Directory);

            SmallMapRef = new SmallMapReferences(DataOvlRef);

            // build the overworld map
            OverworldMap = new LargeMap(_u5Directory, LargeMap.Maps.Overworld, _tileOverrides);

            // build the underworld map
            UnderworldMap = new LargeMap(_u5Directory, LargeMap.Maps.Underworld, _tileOverrides);

            SpriteTileReferences = new TileReferences(DataOvlRef.StringReferences);

            InvRef = new InventoryReferences();

            LargeMapRef = new LargeMapLocationReferences(DataOvlRef);

            AllSmallMaps = new SmallMaps(SmallMapRef, _u5Directory, SpriteTileReferences, _tileOverrides);

            MoonPhaseRefs = new MoonPhaseReferences(DataOvlRef);
            
            State = new GameState(_u5Directory, DataOvlRef);

            // build all combat maps from the Combat Map References
            foreach (CombatMapReference.SingleCombatMapReference combatMapRef in _combatMapRef.MapReferenceList)
            {
                CombatMap combatMap = new CombatMap(_u5Directory, combatMapRef, _tileOverrides);
            }

            // build a "look" table for all tiles
            LookRef = new Look(_u5Directory);

            // build the sign tables
            SignRef = new Signs(_u5Directory);

            TalkScriptsRef = new TalkScripts(_u5Directory, DataOvlRef);

            // build the NPC tables
            NpcRef = new NonPlayerCharacterReferences(_u5Directory, SmallMapRef, TalkScriptsRef, State);

            ShoppeKeeperDialogueReference = new ShoppeKeeperDialogueReference(_u5Directory, DataOvlRef, NpcRef, State.PlayerInventory);
            
            // sadly I have to initialize this after the NPCs are created because there is a circular dependency
            State.InitializeVirtualMap(SmallMapRef, AllSmallMaps, LargeMapRef, OverworldMap, UnderworldMap, 
                NpcRef, SpriteTileReferences, State, NpcRef, InvRef, DataOvlRef);
        }

        #region World actions - do a thing, report or change the state
        /// <summary>
        /// Advances time and takes care of all day, month, year calculations
        /// </summary>
        /// <param name="nMinutes">Number of minutes to advance (maximum of 9*60)</param>
        public void AdvanceTime(int nMinutes)
        {
            State.TheTimeOfDay.AdvanceClock(nMinutes);
            if (State.TorchTurnsLeft > 0) State.TorchTurnsLeft--;
            State.TheVirtualMap.MoveMapUnitsToNextMove();
        }

        /// <summary>
        /// Determines if the current tile the Avatar is on, is an ACTIVE moongate
        /// </summary>
        /// <returns>true if the Avatar is on an active moongate</returns>
        public bool IsAvatarOnActiveMoongate()
        {
            if (!State.TheVirtualMap.IsLargeMap) return false;
            if (State.TheTimeOfDay.IsDayLight) return false;

            return (State.TheMoongates.IsMoonstoneBuried(State.TheVirtualMap.GetCurrent3DPosition()));
        }
        
        /// <summary>
        /// Gets the teleport location of the moongate the avatar is presently on
        /// Avatar MUST be on a moongate teleport location
        /// </summary>
        /// <returns>the coordinates</returns>
        public Point3D GetMoongateTeleportLocation()
        {
            Debug.Assert(State.TheVirtualMap.IsLargeMap);

            Point3D currentPos = new Point3D(State.TheVirtualMap.CurrentPosition.X, 
                State.TheVirtualMap.CurrentPosition.Y, State.TheVirtualMap.LargeMapOverUnder == LargeMap.Maps.Overworld ? 0 : 0xFF);

            return State.TheMoongates.GetMoongatePosition((int)MoonPhaseRefs.GetMoonGateMoonPhase(State.TheTimeOfDay));
        }        
        
        /// <summary>
        /// Gets the angle of the 360 rotation of all moons where Sun is 0degrees (straight up) at 12pm Noon
        /// </summary>
        /// <returns>0-359 degrees</returns>
        public float GetMoonAngle()
        {
            return MoonPhaseRefs.GetMoonAngle(State.TheTimeOfDay);
        }

        /// <summary>
        /// Looks at a particular tile, detecting if NPCs are present as well
        /// Provides string output or special instructions if it is "special"B
        /// </summary>
        /// <param name="xy">position of tile to look at</param>
        /// <param name="specialLookCommand">Special command such as look at gem or sign</param>
        /// <returns>String to output to user</returns>
        public string Look(Point2D xy, out SpecialLookCommand specialLookCommand)
        {
            specialLookCommand = SpecialLookCommand.None;
            string retStr;

            TileReference tileReference = State.TheVirtualMap.GetTileReference(xy);
            // if there is an NPC on the tile, then we assume they want to look at the NPC, not whatever else may be on the tiles
            if (State.TheVirtualMap.IsMapUnitOccupiedTile(xy))
            {
                List<MapUnit> mapUnits = State.TheVirtualMap.GetMapUnitOnTile(xy);
                if (mapUnits.Count <= 0)
                {
                    throw new Ultima5ReduxException("Tried to look up Map Unit, but couldn't find the map character");
                }
                retStr = DataOvlRef.StringReferences.GetString(DataOvlReference.Vision2Strings.THOU_DOST_SEE).Trim()
                    + " " + (LookRef.GetLookDescription(mapUnits[0].KeyTileReference.Index).Trim());
            }
            // if we are any one of these signs then we superimpose it on the screen
            else if (SpriteTileReferences.IsSign(tileReference.Index))
            {
                specialLookCommand = SpecialLookCommand.Sign;
                retStr = string.Empty;
            }
            else if (SpriteTileReferences.GetTileNumberByName("Clock1") == tileReference.Index)
            {
                retStr = (DataOvlRef.StringReferences.GetString(DataOvlReference.Vision2Strings.THOU_DOST_SEE).Trim()
                          + " " + (LookRef.GetLookDescription(tileReference.Index).TrimStart()
                                   + State.TheTimeOfDay.FormattedTime));
            }
            else // lets see what we've got here!
            {
                retStr = (DataOvlRef.StringReferences.GetString(DataOvlReference.Vision2Strings.THOU_DOST_SEE).Trim()
                          + " " + (LookRef.GetLookDescription(tileReference.Index).TrimStart()));
            }

            // pass time at the end to make sure moving characters are accounted for
            PassTime();
            return retStr;
        }
        
        private string ThouDostFind(string thingYouFound)
        {
            return DataOvlRef.StringReferences.GetString(DataOvlReference.ThingsIFindStrings.N_THOU_DOST_FIND_N)
                   + thingYouFound;
        }

        /// <summary>
        /// Gets a thing from the world, adding it the inventory and providing output for console
        /// </summary>
        /// <param name="xy">where is the thing?</param>
        /// <param name="bGotAThing">did I get a thing?</param>
        /// <param name="inventoryItem"></param>
        /// <returns>the output string</returns>
        public string TryToGetAThing(Point2D xy, out bool bGotAThing, out InventoryItem inventoryItem)
        {
            bGotAThing = false;
            inventoryItem = null;
            
            TileReference tileReference = State.TheVirtualMap.GetTileReference(xy);

            // wall sconces - BORROWED!
            if (tileReference.Index == SpriteTileReferences.GetTileNumberByName("LeftSconce") ||
                tileReference.Index == SpriteTileReferences.GetTileNumberByName("RightSconce"))
            {
                State.PlayerInventory.TheProvisions.Items[Provision.ProvisionTypeEnum.Torches].Quantity++;
                
                State.TheVirtualMap.SetOverridingTileReferece(SpriteTileReferences.GetTileReferenceByName("BrickFloor"), xy);
                bGotAThing = true;
                return (DataOvlRef.StringReferences.GetString(DataOvlReference.GetThingsStrings.BORROWED));
            }
            // are there any exposed items (generic call)
            else if (State.TheVirtualMap.IsAnyExposedItems(xy))
            {
                bGotAThing = true;
                InventoryItem invItem = State.TheVirtualMap.DequeuExposedItem(xy);
                inventoryItem = invItem;
                invItem.Quantity++;
                
                return ThouDostFind(invItem.FindDescription);
                //
            }
            
            PassTime();

            return DataOvlRef.StringReferences.GetString(DataOvlReference.GetThingsStrings.NOTHING_TO_GET);
        }

        /// <summary>
        /// Gets the tile reference for a chair when pushed in a given direction
        /// </summary>
        /// <param name="chairDirection"></param>
        /// <returns></returns>
        /// <exception cref="Ultima5ReduxException"></exception>
        private TileReference GetChairNewDirection(VirtualMap.Direction chairDirection)
        {
            switch (chairDirection)
            {
                case VirtualMap.Direction.Up:
                    return SpriteTileReferences.GetTileReferenceByName("ChairBackForward");
                case VirtualMap.Direction.Down:
                    return SpriteTileReferences.GetTileReferenceByName("ChairBackBack");
                case VirtualMap.Direction.Left:
                    return SpriteTileReferences.GetTileReferenceByName("ChairBackRight");
                case VirtualMap.Direction.Right:
                    return SpriteTileReferences.GetTileReferenceByName("ChairBackLeft");
                case VirtualMap.Direction.None:
                default:
                    throw new Ultima5ReduxException("Asked for a chair direction that I don't recognize");
            }
        }        
        
        /// <summary>
        /// Attempts to push (or pull!) a map item
        /// </summary>
        /// <param name="avatarXy">the avatar's current map position</param>
        /// <param name="direction">the direction of the thing the avatar wants to push</param>
        /// <param name="bPushedAThing">was a thing actually pushed?</param>
        /// <returns>the string to output to the user</returns>
        public string PushAThing(Point2D avatarXy, VirtualMap.Direction direction, out bool bPushedAThing)
        {
            bPushedAThing = false;
            Point2D adjustedPos = MapUnitMovement.GetAdjustedPos(avatarXy, direction);

            TileReference adjustedTileReference = State.TheVirtualMap.GetTileReference(adjustedPos);

            // it's not pushable OR if an NPC occupies the tile -so let's bail
            if (!adjustedTileReference.IsPushable || State.TheVirtualMap.IsMapUnitOccupiedTile(adjustedPos))
            {
                return DataOvlRef.StringReferences.GetString(DataOvlReference.ExclaimStrings.WONT_BUDGE_BANG_N);
            }

            bPushedAThing = true;

            // we get the thing one tile further than the thing to see if we have room to push it forward
            Point2D oneMoreTileAdjusted = MapUnitMovement.GetAdjustedPos(adjustedPos, direction);
            TileReference oneMoreTileReference = State.TheVirtualMap.GetTileReference(oneMoreTileAdjusted);
            
            // if I'm sitting and the proceeding tile is an upright tile then I can't swap things 
            if (State.TheVirtualMap.IsAvatarSitting() && oneMoreTileReference.IsUpright)  return DataOvlRef.StringReferences.GetString(DataOvlReference.ExclaimStrings.WONT_BUDGE_BANG_N);

            // if you are pushing a chair then change the direction of chair when it's pushed
            if (SpriteTileReferences.IsChair(adjustedTileReference.Index))
            {
                adjustedTileReference = GetChairNewDirection(direction);
                State.TheVirtualMap.SetOverridingTileReferece(adjustedTileReference, adjustedPos);
            }
            
            // is there an NPC on the tile? if so, we won't move anything into them
            bool bIsNpcOneMoreTile = State.TheVirtualMap.IsMapUnitOccupiedTile(oneMoreTileAdjusted);

            // is the next tile walkable and is there NOT an NPC on it
            if (oneMoreTileReference.IsWalking_Passable && !bIsNpcOneMoreTile)
            {
                State.TheVirtualMap.SwapTiles(adjustedPos, oneMoreTileAdjusted);
            }
            else // the next tile isn't walkable so we just swap the avatar and the push tile
            {
                // we will pull (swap) the thing
                State.TheVirtualMap.SwapTiles(avatarXy, adjustedPos);
            }
            
            // move the avatar to the new spot
            State.TheVirtualMap.CurrentPosition.XY = adjustedPos.Copy();

            PassTime();

            return DataOvlRef.StringReferences.GetString(DataOvlReference.ExclaimStrings.PUSHED_BANG_N);
        }
        
        
        /// <summary>
        /// Climbs the ladder on the current tile that the Avatar occupies
        /// </summary>
        public string TryToKlimb(out KlimbResult klimbResult)
        {
            string GetKlimbOutput(string output = "")
            {
                if (output == "") return DataOvlRef.StringReferences.GetString(DataOvlReference.TravelStrings.KLIMB);
                return DataOvlRef.StringReferences.GetString(DataOvlReference.TravelStrings.KLIMB) + output;
            }
            
            TileReference curTileRef = State.TheVirtualMap.GetTileReferenceOnCurrentTile();
            
            // if it's a large map, we either klimb with the grapple or don't klimb at all
            if (State.TheVirtualMap.IsLargeMap)
            {
                if (State.HasGrapple) // we don't have a grapple, so we can't klimb
                {
                    klimbResult = KlimbResult.RequiresDirection;
                    return GetKlimbOutput();
                }
                klimbResult = KlimbResult.CantKlimb;
                return GetKlimbOutput(DataOvlRef.StringReferences.GetString(DataOvlReference.KlimbingStrings.WITH_WHAT));
            }
                
            // we can't klimb on the current tile, so we need to pick a direction
            if (!SpriteTileReferences.IsLadder(curTileRef.Index) && !SpriteTileReferences.IsGrate(curTileRef.Index))
            {
                klimbResult = KlimbResult.RequiresDirection;
                return GetKlimbOutput();
            }
            
            SmallMapReferences.SingleMapReference.Location location = State.TheVirtualMap.CurrentSingleMapReference.MapLocation;
            int nCurrentFloor = State.TheVirtualMap.CurrentSingleMapReference.Floor; 
            bool hasBasement = State.TheVirtualMap.SmallMapRefs.HasBasement(location);
            int nTotalFloors = State.TheVirtualMap.SmallMapRefs.GetNumberOfFloors(location);
            int nTopFloor = hasBasement ? nTotalFloors - 1 : nTotalFloors;

            TileReference tileReference = State.TheVirtualMap.GetTileReference(State.TheVirtualMap.CurrentPosition.XY);
            if (SpriteTileReferences.IsLadderDown(tileReference.Index) || SpriteTileReferences.IsGrate(tileReference.Index)) 
            {
                if ((hasBasement && nCurrentFloor >= 0) || nCurrentFloor > 0)
                {
                    State.TheVirtualMap.LoadSmallMap(SmallMapRef.GetSingleMapByLocation(location, nCurrentFloor - 1),
                        State.TheVirtualMap.CurrentPosition.XY);
                    klimbResult = KlimbResult.Success;
                    return GetKlimbOutput(DataOvlRef.StringReferences.GetString(DataOvlReference.TravelStrings.DOWN));
                }
            }
            else if (SpriteTileReferences.IsLadderUp(tileReference.Index))
            {
                if (nCurrentFloor + 1 < nTopFloor)
                {
                    State.TheVirtualMap.LoadSmallMap(SmallMapRef.GetSingleMapByLocation(location, nCurrentFloor + 1),
                        State.TheVirtualMap.CurrentPosition.XY);
                    klimbResult = KlimbResult.Success;
                    return GetKlimbOutput(DataOvlRef.StringReferences.GetString(DataOvlReference.TravelStrings.UP));
                }
            }

            klimbResult = KlimbResult.RequiresDirection;
            return GetKlimbOutput();
        }

        public enum KlimbResult { Success, SuccessFell, CantKlimb, RequiresDirection }
        /// <summary>
        /// Try to klimb the given tile - typically called after you select a direction
        /// </summary>
        /// <param name="xy"></param>
        /// <param name="klimbResult"></param>
        /// <returns></returns>
        public string TryToKlimbInDirection(Point2D xy, out KlimbResult klimbResult)
        {
            TileReference tileReference = State.TheVirtualMap.GetTileReference(xy);
            string retStr;
            if (State.TheVirtualMap.IsLargeMap)
            {
                // is it even klimbable?
                if (tileReference.IsKlimable)
                {
                    if (tileReference.Index != SpriteTileReferences.GetTileNumberByName("SmallMountains"))
                    {
                        throw new Ultima5ReduxException(
                            "I am not personal aware of what on earth you would be klimbing that is not already stated in the following logic...");
                    }

                    State.GrapplingFall();
                    klimbResult = KlimbResult.SuccessFell;
                    retStr = DataOvlRef.StringReferences.GetString(DataOvlReference.KlimbingStrings.FELL);
                }
                // is it tall mountains? we can't klimb those
                else if (tileReference.Index == SpriteTileReferences.GetTileNumberByName("TallMountains"))
                {
                    klimbResult = KlimbResult.CantKlimb;
                    retStr = DataOvlRef.StringReferences.GetString(DataOvlReference.KlimbingStrings.IMPASSABLE);
                }
                // there is no chance of klimbing the thing
                else
                {
                    klimbResult = KlimbResult.CantKlimb;
                    retStr = DataOvlRef.StringReferences.GetString(DataOvlReference.KlimbingStrings.NOT_CLIMABLE);
                }
            }
            else // it's a small map
            {
                if (tileReference.IsKlimable)
                {
                    // ie. a fence
                    klimbResult = KlimbResult.Success;
                    retStr = string.Empty;
                }
                else
                {
                    klimbResult = KlimbResult.CantKlimb;
                    retStr = DataOvlRef.StringReferences.GetString(DataOvlReference.TravelStrings.WHAT);
                }
            }
            PassTime();
            return retStr;
        }

        /// <summary>
        /// Try to search an area and see if you find anything!
        /// </summary>
        /// <param name="xy">location to search</param>
        /// <param name="bWasSuccessful">result if it was successful, true if you found one or more things</param>
        /// <returns>the output string to write to console</returns>
        public string TryToSearch(Point2D xy, out bool bWasSuccessful)
        {
            PassTime();
            bWasSuccessful = false;

            // if there is something exposed already OR there is nothing found 
            if (State.TheVirtualMap.IsAnyExposedItems(xy) || !State.TheVirtualMap.ContainsSearchableThings(xy))
            {
                return ThouDostFind(DataOvlRef.StringReferences.GetString(DataOvlReference.Vision2Strings.NOTHING_OF_NOTE_DOT_N));
            }
            
            // we search the tile and expose any items that may be on it
            int nItems = State.TheVirtualMap.SearchAndExposeItems(xy);

            // it could be a moongate, with a stone, but wrong time of day
            if (nItems == 0)
            {
                return ThouDostFind(
                    DataOvlRef.StringReferences.GetString(DataOvlReference.Vision2Strings.NOTHING_OF_NOTE_DOT_N));
            }

            string searchResultStr = string.Empty;
            bWasSuccessful = true;
            foreach (InventoryItem invRef in State.TheVirtualMap.GetExposedInventoryItems(xy))
            {
                searchResultStr += invRef.FindDescription + "\n";
            }
            return ThouDostFind(searchResultStr);
        }
        
        /// <summary>
        /// Try to jimmy the door with a given character
        /// </summary>
        /// <param name="xy">position of the door</param>
        /// <param name="record">character who will attempt to open it</param>
        /// <param name="bWasSuccessful">was it a successful jimmy?</param>
        /// <returns>the output string to write to console</returns>
        public string TryToJimmyDoor(Point2D xy, PlayerCharacterRecord record, out bool bWasSuccessful)
        {
            bWasSuccessful = false;
            TileReference tileReference = State.TheVirtualMap.GetTileReference(xy);
            string retStr;
            
            bool isDoorInDirection = tileReference.IsOpenable;

            if (!isDoorInDirection)
            {
                retStr = (DataOvlRef.StringReferences.GetString(DataOvlReference.OpeningThingsStrings.NO_LOCK));
            }
            else
            {
                bool bIsDoorMagical = SpriteTileReferences.IsDoorMagical(tileReference.Index);
                bool bIsDoorLocked = SpriteTileReferences.IsDoorLocked(tileReference.Index);

                if (bIsDoorMagical)
                {
                    // we use up a key
                    State.PlayerInventory.TheProvisions.Items[Provision.ProvisionTypeEnum.Keys].Quantity--;

                    // for now we will also just open the door so we can get around - will address when we have spells
                    if (SpriteTileReferences.IsDoorWithView(tileReference.Index))
                    {
                        State.TheVirtualMap.SetOverridingTileReferece(
                            SpriteTileReferences.GetTileReferenceByName("RegularDoorView"), xy);
                    }
                    else
                    {
                        State.TheVirtualMap.SetOverridingTileReferece(
                            SpriteTileReferences.GetTileReferenceByName("RegularDoor"), xy);
                    }

                    bWasSuccessful = true;

                    retStr =  (DataOvlRef.StringReferences.GetString(DataOvlReference.OpeningThingsStrings.KEY_BROKE));
                } else if (bIsDoorLocked)
                {
                    // we use up a key
                    State.PlayerInventory.TheProvisions.Items[Provision.ProvisionTypeEnum.Keys].Quantity--;

                    // todo: bh: we will need to determine the likelihood of lock picking success, for now, we always succeed

                    if (SpriteTileReferences.IsDoorWithView(tileReference.Index))
                    {
                        State.TheVirtualMap.SetOverridingTileReferece(
                            SpriteTileReferences.GetTileReferenceByName("RegularDoorView"), xy);
                    }
                    else
                    {
                        State.TheVirtualMap.SetOverridingTileReferece(
                            SpriteTileReferences.GetTileReferenceByName("RegularDoor"), xy);
                    }

                    bWasSuccessful = true;
                    retStr =  (DataOvlRef.StringReferences.GetString(DataOvlReference.OpeningThingsStrings.UNLOCKED));
                }
                else
                {
                    retStr =  (DataOvlRef.StringReferences.GetString(DataOvlReference.OpeningThingsStrings.NO_LOCK));
                }
            }

            PassTime();
            return retStr;
        }

        /// <summary>
        /// Opens a door at a specific position
        /// </summary>
        /// <param name="xy">position of door</param>
        /// <param name="bWasSuccessful">was the door opening successful?</param>
        /// <returns>the output string to write to console</returns>
        public string TryToOpenDoor(Point2D xy, out bool bWasSuccessful)
        {
            bWasSuccessful = false;

            TileReference tileReference = State.TheVirtualMap.GetTileReference(xy);

            bool isDoorInDirection = tileReference.IsOpenable;

            if (!isDoorInDirection)
            {
                return (DataOvlRef.StringReferences.GetString(DataOvlReference.OpeningThingsStrings.NOTHING_TO_OPEN));
            }

            bool bIsDoorMagical = SpriteTileReferences.IsDoorMagical(tileReference.Index);
            bool bIsDoorLocked = SpriteTileReferences.IsDoorLocked(tileReference.Index);

            if (bIsDoorMagical || bIsDoorLocked)
            {
                return (DataOvlRef.StringReferences.GetString(DataOvlReference.OpeningThingsStrings.LOCKED_N));
            }
            else
            {
                State.TheVirtualMap.SetOverridingTileReferece(SpriteTileReferences.GetTileReferenceByName("BrickFloor"), xy);
                bWasSuccessful = true;
                return (DataOvlRef.StringReferences.GetString(DataOvlReference.OpeningThingsStrings.OPENED));
            }
        }

        public enum TryToMoveResult { Moved, ShipChangeDirection, Blocked, OfferToExitScreen, UsedStairs, Fell }

        /// <summary>
        /// Gets a +/- 1 x/y adjustment based on the current position and given direction
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
        /// Tries to move the avatar in a given direction - if successful it will move him
        /// </summary>
        /// <param name="direction">the direction you want to move</param>
        /// <param name="bKlimb">is the avatar K-limbing?</param>
        /// <param name="bFreeMove">is "free move" on?</param>
        /// <param name="tryToMoveResult">outputs the result of the attempt</param>
        /// <returns>output string (may be empty)</returns>
        public string TryToMove(VirtualMap.Direction direction, bool bKlimb, bool bFreeMove, out TryToMoveResult tryToMoveResult)
        {
            string retStr = "";
            int nTilesPerMapRow = State.TheVirtualMap.NumberOfRowTiles;
            int nTilesPerMapCol = State.TheVirtualMap.NumberOfColumnTiles;

            // if we were to move, which direction would we move
            GetAdjustments(direction, out int xAdjust, out int yAdjust);

            // would we be leaving a small map if we went forward?
            if (!State.TheVirtualMap.IsLargeMap && (
                (State.TheVirtualMap.CurrentPosition.Y == (nTilesPerMapRow - 1) && direction == VirtualMap.Direction.Down) ||
                (State.TheVirtualMap.CurrentPosition.Y == (0) && direction == VirtualMap.Direction.Up) ||
                (State.TheVirtualMap.CurrentPosition.X == (nTilesPerMapCol - 1) && direction == VirtualMap.Direction.Right) ||
                (State.TheVirtualMap.CurrentPosition.X == (0) && direction == VirtualMap.Direction.Left)))
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
            
            // we change the direction of the Avatar map unit
            // this will be used to determine which is the appropriate sprite to show
            bool bAvatarActuallyMoved = State.TheVirtualMap.TheMapUnits.AvatarMapUnit.Move(direction);

            if (!bAvatarActuallyMoved)
            {
                tryToMoveResult = TryToMoveResult.ShipChangeDirection;
                AdvanceTime(N_DEFAULT_ADVANCE_TIME);
                return DataOvlRef.StringReferences.GetString(DataOvlReference.WorldStrings.HEAD) + " " +
                       DataOvlRef.StringReferences.GetDirectionString(direction);
            }

            // we start with a different descriptor depending on the vehicle the Avatar is currently on
            switch (State.TheVirtualMap.TheMapUnits.AvatarMapUnit.CurrentAvatarState)
            {
                case Avatar.AvatarState.Regular:
                    retStr = DataOvlRef.StringReferences.GetDirectionString(direction);
                    break;
                case Avatar.AvatarState.Carpet:
                    retStr = DataOvlRef.StringReferences.GetString(DataOvlReference.WorldStrings.FLY) +  
                        DataOvlRef.StringReferences.GetDirectionString(direction);
                    break;
                case Avatar.AvatarState.Horse:
                    retStr = DataOvlRef.StringReferences.GetString(DataOvlReference.WorldStrings.RIDE) +  
                        DataOvlRef.StringReferences.GetDirectionString(direction);
                    break;
                case Avatar.AvatarState.Frigate:
                    retStr = DataOvlRef.StringReferences.GetDirectionString(direction) + 
                             DataOvlRef.StringReferences.GetString(DataOvlReference.WorldStrings.ROWING);
                    break;
                case Avatar.AvatarState.Skiff:
                    retStr = DataOvlRef.StringReferences.GetString(DataOvlReference.WorldStrings.ROW) +  
                        DataOvlRef.StringReferences.GetDirectionString(direction);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

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
                TileReference newTileRef = State.TheVirtualMap.GetTileReference(State.TheVirtualMap.CurrentPosition.XY);
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
                && !State.TheVirtualMap.IsMapUnitOccupiedTile(newPos);


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
                AdvanceTime(N_DEFAULT_ADVANCE_TIME);
                return (retStr.TrimEnd() + "\n" + DataOvlRef.StringReferences.GetString(DataOvlReference.TravelStrings.BLOCKED));
            }

            // the world is a circular - so when you get to the end, start over again
            // this will prevent a never ending growth or shrinking of character position in case the travel the world only moving right a bagillion times
            State.TheVirtualMap.CurrentPosition.X %= nTilesPerMapCol;
            State.TheVirtualMap.CurrentPosition.Y %= nTilesPerMapRow;

            // if you walk on top of a staircase then we will immediately jump to the next floor
            if (SpriteTileReferences.IsStaircase(newTileReference.Index))
            {
                tryToMoveResult = TryToMoveResult.UsedStairs;
                State.TheVirtualMap.UseStairs(State.TheVirtualMap.CurrentPosition.XY);
                // todo: i need to figure out if I am going up or down stairs
                return retStr.TrimEnd() + "\n" + "Up!";
                //return string.Empty;
            }

            // if we are on a big map then we may issue extra information about slow moving terrain
            if (State.TheVirtualMap.IsLargeMap)
            {
                AdvanceTime(SpriteTileReferences.GetMinuteIncrement(newTileReference.Index));
                tryToMoveResult = TryToMoveResult.Moved;
                return retStr.TrimEnd() + "\n" + SpriteTileReferences.GetSlowMovementString(newTileReference.Index).TrimEnd();
            }
            else
            {
                tryToMoveResult = TryToMoveResult.Moved;

                // if we are indoors then all walking takes 2 minutes
                AdvanceTime(2);

                return retStr.TrimEnd();
            }
        }

        public void PassTime()
        {
            AdvanceTime(2); 
        }

        /// <summary>
        /// Ignites a torch, if available and set the number of turns for the torch to be burnt out
        /// </summary>
        /// <returns></returns>
        public string IgniteTorch()
        {
            PassTime();
            const byte nDefaultNumberOfTurnsForTorch = 100;
            // if there are no torches then report back and make no change
            if (State.PlayerInventory.TheProvisions.Items[Provision.ProvisionTypeEnum.Torches].Quantity <= 0) return DataOvlRef.StringReferences.GetString(DataOvlReference.SleepTransportStrings.NONE_OWNED_BANG_N);

            State.PlayerInventory.TheProvisions.Items[Provision.ProvisionTypeEnum.Torches].Quantity--;
            State.TorchTurnsLeft = nDefaultNumberOfTurnsForTorch;
            // this will trigger a re-read of time of day changes
            State.TheTimeOfDay.SetAllChangeTrackers();
            return DataOvlRef.StringReferences.GetString(DataOvlReference.KeypressCommandsStrings.IGNITE_TORCH);
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
            bool isOnBuilding = LargeMapRef.IsMapXYEnterable(xy);
            string retStr;
            if (isOnBuilding)
            {
                SmallMapReferences.SingleMapReference.Location location = LargeMapRef.GetLocationByMapXY(xy);
                State.TheVirtualMap.LoadSmallMap(SmallMapRef.GetSingleMapByLocation(location, 0));
                // set us to the front of the building
                State.TheVirtualMap.CurrentPosition.XY = SmallMapReferences.GetStartingXYByLocation(location);

                retStr =
                    (DataOvlRef.StringReferences.GetString(DataOvlReference.WorldStrings.ENTER_SPACE)
                    + SmallMapRef.GetLocationTypeStr(location)) + "\n" +
                    SmallMapRef.GetLocationName(location);
                bWasSuccessful = true;
            }
            else
            {
                retStr = DataOvlRef.StringReferences.GetString(DataOvlReference.WorldStrings.ENTER_SPACE)
                         + DataOvlRef.StringReferences.GetString(DataOvlReference.WorldStrings.WHAT);
                bWasSuccessful = false;
            }
            PassTime();
            return retStr;
        }

        /// <summary>
        /// Board something such as a frigate, skiff, horse or carpet
        /// </summary>
        /// <param name="bWasSuccessful"></param>
        /// <returns></returns>
        public string Board(out bool bWasSuccessful)
        {
            MapUnit currentAvatarTileRef = State.TheVirtualMap.GetMapUnitOnCurrentTile();
            bWasSuccessful = true;
            
            if (currentAvatarTileRef is null
                || !currentAvatarTileRef.KeyTileReference.IsBoardable)
            {
                bWasSuccessful = false;
                // can't board it
                return DataOvlRef.StringReferences.GetString(DataOvlReference.KeypressCommandsStrings.BOARD).Trim() +
                       " " + DataOvlRef.StringReferences.GetString(DataOvlReference.TravelStrings.WHAT);
            }

            //OutputStreamHelpers.WriteOutput("Gonna board something...");
            bool bAvatarIsBoarded = State.TheVirtualMap.IsAvatarRidingSomething;
            Avatar avatar = State.TheVirtualMap.TheMapUnits.AvatarMapUnit;
            MapUnit boardableMapUnit = State.TheVirtualMap.GetMapUnitOnCurrentTile();

            // at this point we are certain that the current tile is boardable AND the we know if the avatar has already
            // boarded something
            string getOnFootResponse()
            {
                    return DataOvlRef.StringReferences.GetString(DataOvlReference.KeypressCommandsStrings.BOARD).Trim() +
                           "\n" + DataOvlRef.StringReferences.GetString(DataOvlReference.KeypressCommandsStrings.ON_FOOT)
                               .Trim();
            }

            string retStr = DataOvlRef.StringReferences.GetString(DataOvlReference.KeypressCommandsStrings.BOARD)
                .Trim() + " " + boardableMapUnit.BoardXitName;

            switch (boardableMapUnit)
            {
                case MagicCarpet _ when bAvatarIsBoarded:
                    bWasSuccessful = false;
                    return getOnFootResponse();
                case MagicCarpet _:
                    avatar.BoardMapUnit(boardableMapUnit);
                    break;
                case Horse _ when bAvatarIsBoarded:
                    bWasSuccessful = false;
                    return getOnFootResponse();
                // delete or deactivate the horse we just mounted
                case Horse _:
                    avatar.BoardMapUnit(boardableMapUnit);
                    break;
                case Frigate boardableFrigate:
                {
                    if (bAvatarIsBoarded)
                    {
                        if (State.TheVirtualMap.IsAvatarRidingHorse)
                        {
                            bWasSuccessful = false;
                            return getOnFootResponse();
                        }
                        if (State.TheVirtualMap.IsAvatarRidingCarpet)
                        {
                            // we tuck the carpet away
                            State.PlayerInventory.MagicCarpets++;
                        }
                        if (State.TheVirtualMap.IsAvatarInSkiff)
                        {
                            // add a skiff the the frigate
                            boardableFrigate.SkiffsAboard++;
                        }
                    }

                    if (boardableFrigate.SkiffsAboard == 0)
                    {
                        retStr += DataOvlRef.StringReferences.GetString(DataOvlReference.SleepTransportStrings.M_WARNING_NO_SKIFFS_N).TrimEnd();
                    }
                    avatar.BoardMapUnit(boardableFrigate);
                    break;
                }
                case Skiff _ when bAvatarIsBoarded:
                    bWasSuccessful = false;
                    return getOnFootResponse();
                case Skiff _:
                    avatar.BoardMapUnit(boardableMapUnit);
                    break;
            }

            State.TheVirtualMap.TheMapUnits.ClearMapUnit(currentAvatarTileRef);
            
            // throw new Ultima5ReduxException("Tried to board a thing that is not boardable: "+boardableMapUnitType.FullName);
            return retStr;
        }

        public string Xit(out bool bWasSuccessful)
        {
            bWasSuccessful = true;
            string retStr = DataOvlRef.StringReferences.GetString(DataOvlReference.KeypressCommandsStrings.XIT).TrimEnd();

            if (!State.TheVirtualMap.TheMapUnits.AvatarMapUnit.IsAvatarOnBoardedThing)
            {
                return retStr += " " + DataOvlRef.StringReferences.GetString(DataOvlReference.KeypressCommandsStrings.WHAT_Q).Trim();
            }

            MapUnit unboardedMapUnit = State.TheVirtualMap.TheMapUnits.XitCurrentMapUnit();
            if (unboardedMapUnit == null) return "FAILED TO UNBOARD";
            retStr += " " + unboardedMapUnit.BoardXitName.Trim();
            
            return retStr;
        }
        
        // public void DismountCarpet()
        // {
        //     Debug.Assert(State.TheVirtualMap.IsAvatarRidingCarpet);
        //
        //     // show the Avatar as an unboarded player (regular ole Avatar) 
        //     State.TheVirtualMap.TheMapUnits.AvatarMapUnit.UnboardedAvatar();
        // }

        private string UseSpecialItem(SpecialItem spcItem)
        {
            switch (spcItem.ItemType)
            {
                case SpecialItem.ItemTypeSpriteEnum.Carpet:
                    return DataOvlRef.StringReferences.GetString(DataOvlReference.WearUseItemStrings.CARPET_BANG);
                case SpecialItem.ItemTypeSpriteEnum.Grapple:
                    return ("Grapple\n\nYou need to K-limb with it!");
                case SpecialItem.ItemTypeSpriteEnum.Spyglass:
                    return DataOvlRef.StringReferences.GetString(DataOvlReference.WearUseItemStrings.SPYGLASS_N_N);
                case SpecialItem.ItemTypeSpriteEnum.HMSCape:
                    return DataOvlRef.StringReferences.GetString(DataOvlReference.WearUseItemStrings.PLANS_N_N);
                case SpecialItem.ItemTypeSpriteEnum.PocketWatch:
                    return DataOvlRef.StringReferences.GetString(DataOvlReference.WearUseItemStrings
                        .WATCH_N_N_THE_POCKET_W_READS) + " " + State.TheTimeOfDay.FormattedTime;
                case SpecialItem.ItemTypeSpriteEnum.BlackBadge:
                    return DataOvlRef.StringReferences.GetString(DataOvlReference.WearUseItemStrings.BADGE_N_N);
                case SpecialItem.ItemTypeSpriteEnum.WoodenBox:
                    return DataOvlRef.StringReferences.GetString(DataOvlReference.WearUseItemStrings.BOX_N_HOW_N);
                case SpecialItem.ItemTypeSpriteEnum.Sextant:
                    return DataOvlRef.StringReferences.GetString(DataOvlReference.WearUseItemStrings.SEXTANT_N_N);
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private string UseLordBritishArtifactItem(LordBritishArtifact lordBritishArtifact)
        {
            switch (lordBritishArtifact.Artifact)
            {
                case LordBritishArtifact.ArtifactType.Amulet:
                    return (DataOvlRef.StringReferences.GetString(DataOvlReference.WearUseItemStrings.AMULET_N_N) +
                            "\n" +
                            DataOvlRef.StringReferences.GetString(
                                DataOvlReference.WearUseItemStrings.WEARING_AMULET) + "\n" +
                            DataOvlRef.StringReferences.GetString(DataOvlReference.WearUseItemStrings
                                .SPACE_OF_LORD_BRITISH_DOT_N));
                case LordBritishArtifact.ArtifactType.Crown:
                    return (DataOvlRef.StringReferences.GetString(DataOvlReference.WearUseItemStrings.CROWN_N_N) +
                            "\n" +
                            DataOvlRef.StringReferences.GetString(DataOvlReference.WearUseItemStrings
                                .DON_THE_CROWN) + "\n" +
                            DataOvlRef.StringReferences.GetString(DataOvlReference.WearUseItemStrings
                                .SPACE_OF_LORD_BRITISH_DOT_N));
                case LordBritishArtifact.ArtifactType.Sceptre:
                    return (DataOvlRef.StringReferences.GetString(DataOvlReference.WearUseItemStrings.SCEPTRE_N_N) +
                            "\n" +
                            DataOvlRef.StringReferences.GetString(DataOvlReference.WearUseItemStrings
                                .WIELD_SCEPTRE) + "\n" +
                            DataOvlRef.StringReferences.GetString(DataOvlReference.WearUseItemStrings
                                .SPACE_OF_LORD_BRITISH_DOT_N));
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private string UseShadowLordShard(ShadowlordShard shadowlordShard)
        {
            string thouHolds(string shard)
            {
                return DataOvlRef.StringReferences.GetString(DataOvlReference.ShadowlordStrings
                    .GEM_SHARD_THOU_HOLD_EVIL_SHARD) + "\n" + shard;
            }

            switch (shadowlordShard.Shard)
            {
                case ShadowlordShard.ShardType.Falsehood:
                    return thouHolds(DataOvlRef.StringReferences.GetString((DataOvlReference.ShadowlordStrings.FALSEHOOD_DOT)));
                case ShadowlordShard.ShardType.Hatred:
                    return thouHolds(DataOvlRef.StringReferences.GetString((DataOvlReference.ShadowlordStrings.HATRED_DOT)));
                case ShadowlordShard.ShardType.Cowardice:
                    return thouHolds(DataOvlRef.StringReferences.GetString((DataOvlReference.ShadowlordStrings.COWARDICE_DOT)));
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private string UseMoonstone(Moonstone moonstone, out bool bMoonstoneBuried)
        {
            bMoonstoneBuried = false;

            if (!IsAllowedToBuryMoongate())
            {
                return DataOvlRef.StringReferences.GetString((DataOvlReference.ExclaimStrings.MOONSTONE_SPACE)) +
                       DataOvlRef.StringReferences.GetString((DataOvlReference
                           .ExclaimStrings.CANNOT_BE_BURIED_HERE_BANG_N));
            }
            
            State.TheMoongates.SetMoonstoneBuried(moonstone.MoongateIndex, true, State.TheVirtualMap.GetCurrent3DPosition());
            bMoonstoneBuried = true;
            
            return DataOvlRef.StringReferences.GetString((DataOvlReference.ExclaimStrings.MOONSTONE_SPACE)) +
                                                         DataOvlRef.StringReferences.GetString((DataOvlReference
                                                             .ExclaimStrings.BURIED_BANG_N));
        }

        private bool IsAllowedToBuryMoongate()
        {
            if (State.TheVirtualMap.LargeMapOverUnder == LargeMap.Maps.Small) return false;
            if (State.TheVirtualMap.IsAnyExposedItems(State.TheVirtualMap.CurrentPosition.XY)) return false;
            TileReference tileRef = State.TheVirtualMap.GetTileReferenceOnCurrentTile();

            return (SpriteTileReferences.IsMoonstoneBuriable(tileRef.Index));
        }

        public string TryToUseAnInventoryItem(InventoryItem item, out bool bAbleToUseItem)
        {
            bAbleToUseItem = false;
            string retStr;
            if (item.GetType() == typeof(SpecialItem))
            {
                retStr = UseSpecialItem((SpecialItem) item);
            }
            else if (item.GetType() == typeof(Potion))
            {
                retStr = ("Potion\n\nPoof!");
            }
            else if (item.GetType() == typeof(Scroll))
            {
                retStr = ("Scroll\n\nAllaKazam!");
            }
            else if (item.GetType() == typeof(LordBritishArtifact))
            {
                retStr = UseLordBritishArtifactItem((LordBritishArtifact) item);
            }
            else if (item.GetType() == typeof(ShadowlordShard))
            {
                retStr = UseShadowLordShard((ShadowlordShard) item);
            }
            else if (item.GetType() == typeof(Moonstone))
            {
                retStr = UseMoonstone((Moonstone) item, out bAbleToUseItem);
            }
            else
            {
                throw new Exception("You are trying to use an item that can't be used: " + item.LongName);
            }

            PassTime();
            return retStr;
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

            PassTime();
            return CurrentConversation;
        }

        public void FinishConversation()
        {
            if (CurrentConversation != null)
            {
                throw new Ultima5ReduxException("Can't close a conversation if haven't got one open!");
            }
        }

        #region Test Procedures
        /// <summary>
        /// Constructor only used for testing
        /// </summary>
        public World() : this(@"C:\Games\Ultima_5\Gold")
        {
            
        }

        public void Test_BasicConversation()
        {
            foreach (NonPlayerCharacterReference npc in NpcRef.NPCs)
            {
                Conversation conversation = new Conversation(npc, State, DataOvlRef); 
                Debug.Assert(conversation != null);
            }
        }
        

        public void Test_Signs()
        {
            Signs.Sign sign = SignRef.GetSign(SmallMapReferences.SingleMapReference.Location.Yew, 16, 2);
            Signs.Sign sign2 = SignRef.GetSign(42);

            Debug.Assert(sign != null);
            Debug.Assert(sign2 != null);
        }
        #endregion
    }
}
