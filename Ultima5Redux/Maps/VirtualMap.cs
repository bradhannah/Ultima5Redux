using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Numerics;
using Ultima5Redux.DayNightMoon;
using Ultima5Redux.External;
using Ultima5Redux.MapCharacters;
using Ultima5Redux.PlayerCharacters;

// ReSharper disable IdentifierTypo

namespace Ultima5Redux.Maps
{
    public class VirtualMap
    {
        #region Private fields
        private NonPlayerCharacterReferences _npcRefs;
        private readonly GameState _state;
        /// <summary>
        /// Reference to towne/keep etc locations on the large map
        /// </summary>
        private LargeMapLocationReferences _largeMapLocationReferenceses;
        /// <summary>
        /// Non player characters on current map
        /// </summary>
        private NonPlayerCharacterReferences _nonPlayerCharacters;
        /// <summary>
        /// All the small maps
        /// </summary>
        private readonly SmallMaps _smallMaps;
        /// <summary>
        /// Both underworld and overworld maps
        /// </summary>
        private readonly Dictionary<LargeMap.Maps, LargeMap> _largeMaps = new Dictionary<LargeMap.Maps, LargeMap>(2);

        /// <summary>
        /// Exposed searched or loot items 
        /// </summary>
        private Queue<InventoryItem>[][] _exposedSearchItems;
        /// <summary>
        /// References to all tiles
        /// </summary>
        private readonly TileReferences _tileReferences;
        /// <summary>
        /// override map is responsible for overriding tiles that would otherwise be static
        /// </summary>
        private int[][] _overrideMap;
        /// <summary>
        /// Current position of player character (avatar)
        /// </summary>
        private readonly Point2D _currentPosition = new Point2D(0, 0);
        /// <summary>
        /// Current time of day
        /// </summary>
        private readonly TimeOfDay _timeOfDay;

        /// <summary>
        /// Details of where the moongates are
        /// </summary>
        private readonly Moongates _moongates;

        private readonly InventoryReferences _inventoryReferences;

        /// <summary>
        /// All overriden tiles
        /// </summary>
        private TileOverrides _overridenTiles; 
        #endregion

        /// <summary>
        /// 4 way direction
        /// </summary>
        public enum Direction { Up, Down, Left, Right, None };
        private enum LadderOrStairDirection { Up, Down };


        #region Public Properties 

        /// <summary>
        /// Current position of player character (avatar)
        /// </summary>
        public Point2D CurrentPosition
        {
            get => _currentPosition;
            set
            {
                _currentPosition.X = value.X;
                _currentPosition.Y = value.Y;
            }
        }

        /// <summary>
        /// Number of total columns for current map
        /// </summary>
        public int NumberOfColumnTiles => _overrideMap[0].Length;

        /// <summary>
        /// Number of total rows for current map
        /// </summary>
        public int NumberOfRowTiles => _overrideMap.Length;

        /// <summary>
        /// The current small map (null if on large map)
        /// </summary>
        public SmallMap CurrentSmallMap { get; private set; }
        /// <summary>
        /// Current large map (null if on small map)
        /// </summary>
        public LargeMap CurrentLargeMap { get; private set; }
        /// <summary>
        /// The abstracted Map object for the current map 
        /// Returns large or small depending on what is active
        /// </summary>
        public Map CurrentMap => (IsLargeMap ? (Map)CurrentLargeMap : (Map)CurrentSmallMap);

        /// <summary>
        /// Detailed reference of current small map
        /// </summary>
        public SmallMapReferences.SingleMapReference CurrentSingleMapReference { get; private set; }
        /// <summary>
        /// All small map references
        /// </summary>
        public SmallMapReferences SmallMapRefs { get; private set; }
        /// <summary>
        /// Are we currently on a large map?
        /// </summary>
        public bool IsLargeMap { get; private set; } = false;

        public bool IsBasement => !IsLargeMap && CurrentSingleMapReference.Floor == -1; 

        /// <summary>
        /// If we are on a large map - then are we on overworld or underworld
        /// </summary>
        public LargeMap.Maps LargeMapOverUnder { get; private set; } = (LargeMap.Maps)(-1);

        public MapCharacters.MapCharacters TheMapCharacters { get; private set; }

        #endregion

        #region Constructor, Initializers and Loaders

        /// <summary>
        /// Construct the VirtualMap (requires initialization still)
        /// </summary>
        /// <param name="smallMapReferences"></param>
        /// <param name="smallMaps"></param>
        /// <param name="largeMapLocationReferenceses"></param>
        /// <param name="overworldMap"></param>
        /// <param name="underworldMap"></param>
        /// <param name="nonPlayerCharacters"></param>
        /// <param name="tileReferences"></param>
        /// <param name="state"></param>
        /// <param name="npcRefs"></param>
        /// <param name="timeOfDay"></param>
        /// <param name="moongates"></param>
        /// <param name="inventoryReferences"></param>
        /// <param name="playerCharacterRecords"></param>
        /// <param name="initialMap"></param>
        /// <param name="currentSmallMapReference"></param>
        public VirtualMap(SmallMapReferences smallMapReferences, SmallMaps smallMaps, LargeMapLocationReferences largeMapLocationReferenceses,
            LargeMap overworldMap, LargeMap underworldMap, NonPlayerCharacterReferences nonPlayerCharacters, TileReferences tileReferences,
            GameState state, NonPlayerCharacterReferences npcRefs, TimeOfDay timeOfDay, Moongates moongates, InventoryReferences inventoryReferences,
            PlayerCharacterRecords playerCharacterRecords, LargeMap.Maps initialMap, 
            SmallMapReferences.SingleMapReference currentSmallMapReference)
        {
            // let's make sure they are using the correct combination
            Debug.Assert((initialMap == LargeMap.Maps.Small &&
                          currentSmallMapReference.MapLocation != SmallMapReferences.SingleMapReference.Location.Britannia_Underworld)
                         || initialMap != LargeMap.Maps.Small);
            
            SmallMapRefs = smallMapReferences;
            
            _smallMaps = smallMaps;
            _nonPlayerCharacters = nonPlayerCharacters;
            _largeMapLocationReferenceses = largeMapLocationReferenceses;
            _tileReferences = tileReferences;
            _state = state;
            _npcRefs = npcRefs;
            _timeOfDay = timeOfDay;
            _moongates = moongates;
            _inventoryReferences = inventoryReferences;
            _overridenTiles = new TileOverrides();
            
            //this.characterStates = characterStates;
            _largeMaps.Add(LargeMap.Maps.Overworld, overworldMap);
            _largeMaps.Add(LargeMap.Maps.Underworld, underworldMap);

            SmallMapReferences.SingleMapReference.Location mapLocation = currentSmallMapReference?.MapLocation 
                                                                    ?? SmallMapReferences.SingleMapReference.Location.Britannia_Underworld;
            
            // load the characters for the very first time from disk
            // subsequent loads may not have all the data stored on disk and will need to recalculate
            TheMapCharacters = new MapCharacters.MapCharacters(tileReferences, npcRefs,
               state.CharacterAnimationStatesDataChunk, state.OverworldOverlayDataChunks, 
               state.UnderworldOverlayDataChunks, state.CharacterStatesDataChunk,
               state.NonPlayerCharacterMovementLists, state.NonPlayerCharacterMovementOffsets,
               timeOfDay, playerCharacterRecords, initialMap, mapLocation);

            switch (initialMap)
            {
                case LargeMap.Maps.Small:
                    LoadSmallMap(currentSmallMapReference);
                    break;
                case LargeMap.Maps.Overworld:
                case LargeMap.Maps.Underworld:
                    LoadLargeMap(initialMap);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(initialMap), initialMap, null);
            }
        }

        /// <summary>
        /// Loads a small map based on the provided reference
        /// </summary>
        /// <param name="singleMapReference"></param>
        public void LoadSmallMap(SmallMapReferences.SingleMapReference singleMapReference)
        {
            CurrentSingleMapReference = singleMapReference;
            CurrentSmallMap = _smallMaps.GetSmallMap(singleMapReference.MapLocation, singleMapReference.Floor);
            
            _overrideMap = Utils.Init2DArray<int>(CurrentSmallMap.TheMap[0].Length, CurrentSmallMap.TheMap.Length);
            _exposedSearchItems = Utils.Init2DArray<Queue<InventoryItem>>(CurrentSmallMap.TheMap[0].Length, CurrentSmallMap.TheMap.Length);
            
            IsLargeMap = false;
            LargeMapOverUnder = (LargeMap.Maps)(-1);

            TheMapCharacters.SetCurrentMapType(singleMapReference.MapLocation, LargeMap.Maps.Small);
        }
      
        /// <summary>
        /// Loads a large map -either overworld or underworld
        /// </summary>
        /// <param name="map"></param>
        public void LoadLargeMap(LargeMap.Maps map)
        {
            CurrentLargeMap = _largeMaps[map];
            int nFloor = map == LargeMap.Maps.Overworld ? 0 : -1;
            switch (map)
            {
                case LargeMap.Maps.Underworld:
                case LargeMap.Maps.Overworld:
                    CurrentSingleMapReference = CurrentLargeMap.CurrentSingleMapReference;
                    break;
                case LargeMap.Maps.Small:
                    throw new Ultima5ReduxException("You can't load a small large map!");
                default:
                    throw new ArgumentOutOfRangeException(nameof(map), map, null);
            }

            _overrideMap = Utils.Init2DArray<int>(CurrentLargeMap.TheMap[0].Length, CurrentLargeMap.TheMap.Length);
            _exposedSearchItems = Utils.Init2DArray<Queue<InventoryItem>>(CurrentLargeMap.TheMap[0].Length, CurrentLargeMap.TheMap.Length);
            
            IsLargeMap = true;
            LargeMapOverUnder = map;

            TheMapCharacters.SetCurrentMapType(SmallMapReferences.SingleMapReference.Location.Britannia_Underworld, map);
        }
        #endregion

        #region Tile references and character positioning

        public IEnumerable<InventoryItem> GetExposedInventoryItems(Point2D xy)
        {
            return _exposedSearchItems[xy.X][xy.Y];
        }

        /// <summary>
        /// Gets a tile reference from the given coordinate
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="bIgnoreExposed"></param>
        /// <param name="bIgnoreMoongate"></param>
        /// <returns></returns>
        public TileReference GetTileReference(int x, int y, bool bIgnoreExposed = false, bool bIgnoreMoongate = false)
        {
            // we FIRST check if there is an exposed item to show - this takes precedence over an overriden tile
            if (!bIgnoreExposed)
            {
                if (_exposedSearchItems[x][y] != null)
                {
                    if (_exposedSearchItems[x][y].Count > 0)
                    {
                        // we get the top most exposed item and only show it
                        return _tileReferences.GetTileReference(_exposedSearchItems[x][y].Peek().SpriteNum);
                    }
                }
            }

            // if it's a large map and there should be a moongate and it's nighttime then it's a moongate!
            // bajh: March 22, 2020 - we are going to try to always include the Moongate, and let the game decide what it wants to do with it
            if (!bIgnoreMoongate && IsLargeMap && 
                    _moongates.IsMoonstoneBuried(new Point3D(x, y, LargeMapOverUnder == LargeMap.Maps.Overworld ? 0 : 0xFF)))
            {
                return _tileReferences.GetTileReferenceByName("Moongate");
            }
            
            // we check to see if our override map has something on top of it
            if (_overrideMap[x][y] != 0)
                return _tileReferences.GetTileReference(_overrideMap[x][y]);

            if (IsLargeMap)
            {
                return (_tileReferences.GetTileReference(CurrentLargeMap.TheMap[x][y]));
            }
            else
            {
                return (_tileReferences.GetTileReference(CurrentSmallMap.TheMap[x][y]));
            }
        }

        /// <summary>
        /// Gets a tile reference from the tile the avatar currently resides on
        /// </summary>
        /// <returns></returns>
        public TileReference GetTileReferenceOnCurrentTile()
        {
            return GetTileReference(CurrentPosition);
        }

        /// <summary>
        /// Gets a tile reference from the given coordinate
        /// </summary>
        /// <param name="xy"></param>
        /// <returns></returns>
        public TileReference GetTileReference(Point2D xy)
        {
            return GetTileReference(xy.X, xy.Y);
        }

        internal int SearchAndExposeItems(Point2D xy)
        {
            // check for moonstones
            // moonstone check
            if (IsLargeMap && _moongates.IsMoonstoneBuried(xy, LargeMapOverUnder) && _timeOfDay.IsDayLight)
            {
                InventoryItem invItem =
                    _state.PlayerInventory.TheMoonstones.
                        Items[_moongates.GetMoonPhaseByPosition(xy, LargeMapOverUnder)];
                if (_exposedSearchItems[xy.X][xy.Y] == null)
                {
                    _exposedSearchItems[xy.X][xy.Y] = new Queue<InventoryItem>();
                }
                _exposedSearchItems[xy.X][xy.Y].Enqueue(invItem);
                
                return 1;
            }

            return 0;
        }

        internal bool IsAnyExposedItems(Point2D xy)
        {
            if (_exposedSearchItems[xy.X][xy.Y] == null) return false;
            return (_exposedSearchItems[xy.X][xy.Y].Count > 0);
        }
        
        internal InventoryItem DequeuExposedItem(Point2D xy)
        {
            if (IsAnyExposedItems(xy))
            {
                return _exposedSearchItems[xy.X][xy.Y].Dequeue();
            }
            throw new Ultima5ReduxException("Tried to deque an item at "+xy+" but there is no item on it");
        }
        
        /// <summary>
        /// Gets the Avatar's current position in 3D spaces
        /// </summary>
        /// <returns></returns>
        public Point3D GetCurrent3DPosition()
        {
            if (LargeMapOverUnder == LargeMap.Maps.Small)
            {
                return new Point3D(CurrentPosition.X, 
                    CurrentPosition.Y, CurrentSmallMap.MapFloor);                
            }

            return new Point3D(CurrentPosition.X, 
                CurrentPosition.Y,LargeMapOverUnder == LargeMap.Maps.Overworld ? 0 : 0xFF);
        }
        
        /// <summary>
        /// Sets an override for the current tile which will be favoured over the static map tile
        /// </summary>
        /// <param name="tileReference">the reference (sprite)</param>
        /// <param name="xy"></param>
        public void SetOverridingTileReferece(TileReference tileReference, Point2D xy)
        {
            SetOverridingTileReferece(tileReference, xy.X, xy.Y);
        }

        /// <summary>
        /// Sets an override for the current tile which will be favoured over the static map tile
        /// </summary>
        /// <param name="tileReference"></param>
        /// <param name="x"></param>
        /// <param name="y"></param>
        public void SetOverridingTileReferece(TileReference tileReference, int x, int y)
        {
            _overrideMap[x][y] = tileReference.Index;
        }


        /// <summary>
        /// Moves the player character to the specified coordinate
        /// </summary>
        /// <param name="xy"></param>
        public void SetCharacterPosition(Point2D xy)
        {
            CurrentPosition = xy;
        }

        
        
        /// <summary>
        /// Gets the NPC you want to talk to in the given direction
        /// If you are in front of a table then you can talk over top of it too
        /// </summary>
        /// <param name="direction"></param>
        /// <returns>the NPC mapcharacter or null if non are found</returns>
        public MapCharacter GetNPCToTalkTo(NonPlayerCharacterMovement.MovementCommandDirection direction)
        {
            Point2D adjustedPosition = NonPlayerCharacterMovement.GetAdjustedPos(_currentPosition, direction, 1);
            MapCharacter npc = GetNPCOnTile(adjustedPosition);
            if (npc != null) return npc;

            if (!GetTileReference(adjustedPosition).IsTalkOverable)
                return null;
            
            Point2D adjustedPosition2Away = NonPlayerCharacterMovement.GetAdjustedPos(_currentPosition, direction, 2);
            return GetNPCOnTile(adjustedPosition2Away);
        }
        
        
        /// <summary>
        /// If an NPC is on a tile, then it will get them
        /// assumes it's on the same floor
        /// </summary>
        /// <param name="xy"></param>
        /// <returns>the NPC or null if one does not exist</returns>
        public MapCharacter GetNPCOnTile(Point2D xy)
        {
            if (IsLargeMap) return null;
            SmallMapReferences.SingleMapReference.Location location = CurrentSingleMapReference.MapLocation;

            MapCharacter mapCharacter = TheMapCharacters.GetMapCharacterByLocation(location, xy, CurrentSingleMapReference.Floor);
            
            return mapCharacter;
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Is the particular tile eligable to be moved onto
        /// </summary>
        /// <param name="xy"></param>
        /// <param name="bNoStaircases"></param>
        /// <returns>true if you can move onto the tile</returns>
        private bool IsTileFreeToTravel(Point2D xy, bool bNoStaircases)
        {
            if (xy.X < 0 || xy.Y < 0) return false;

            bool bIsAvatarTile = CurrentPosition == xy;
            bool bIsNpcTile = IsNPCTile(xy);
            TileReference tileReference = GetTileReference(xy);
            // if we want to eliminate staircases as an option then we need to make sure it isn't a staircase
            // true indicates that it is walkable
            bool bStaircaseWalkable = bNoStaircases ? !_tileReferences.IsStaircase(tileReference.Index) : true; 
            bool bIsWalkable = tileReference.IsWalking_Passable && bStaircaseWalkable;

            // there is not an NPC on the tile, it is walkable and the Avatar is not currently occupying it
            return (!bIsNpcTile && bIsWalkable && !bIsAvatarTile);
        }

        /// <summary>
        /// Gets possible directions that are accessible from a particular point
        /// </summary>
        /// <param name="characterPosition">the curent position of the character</param>
        /// <param name="scheduledPosition">the place they are supposed to be</param>
        /// <param name="nMaxDistance">max distance they can travel from that position</param>
        /// <param name="bNoStaircases"></param>
        /// <returns></returns>
        private List<NonPlayerCharacterMovement.MovementCommandDirection> GetPossibleDirectionsList(Point2D characterPosition, Point2D scheduledPosition, 
            int nMaxDistance, bool bNoStaircases)
        {
            List<NonPlayerCharacterMovement.MovementCommandDirection> directionList = new List<NonPlayerCharacterMovement.MovementCommandDirection>();

            // gets an adjusted position OR returns null if the position is not valid

            foreach (NonPlayerCharacterMovement.MovementCommandDirection direction in Enum.GetValues(typeof(NonPlayerCharacterMovement.MovementCommandDirection)))
            {
                // we may be asked to avoid including .None in the list
                if (direction == NonPlayerCharacterMovement.MovementCommandDirection.None) continue;
                
                Point2D adjustedPos = GetPositionIfUserCanMove(direction, characterPosition, bNoStaircases, scheduledPosition, nMaxDistance);
                // if adjustedPos == null then the particular direction was not allowed for one reason or another
                if (adjustedPos != null) { directionList.Add(direction); }
            }

            return directionList;
        }

        private Point2D GetPositionIfUserCanMove(NonPlayerCharacterMovement.MovementCommandDirection direction, Point2D characterPosition, bool bNoStaircases, Point2D scheduledPosition, int nMaxDistance)
        {
            Point2D adjustedPosition = NonPlayerCharacterMovement.GetAdjustedPos(characterPosition, direction);

            // always include none
            if (direction == NonPlayerCharacterMovement.MovementCommandDirection.None) return adjustedPosition;

            if (adjustedPosition.X < 0 || adjustedPosition.X >= CurrentMap.TheMap.Length || adjustedPosition.Y < 0 || adjustedPosition.Y >= CurrentMap.TheMap[0].Length) return null;

            // is the tile free to travel to? even if it is, is it within N tiles of the scheduled tile?
            if (IsTileFreeToTravel(adjustedPosition, bNoStaircases) && scheduledPosition.WithinN(adjustedPosition, nMaxDistance))
            {
                return adjustedPosition;
            }

            return null;
        }

        /// <summary>
        /// Gets a suitable random position when wandering 
        /// </summary>
        /// <param name="characterPosition">position of character</param>
        /// <param name="scheduledPosition">scheduled position of the character</param>
        /// <param name="nMaxDistance">max number of tiles the wander can be from the scheduled position</param>
        /// <param name="direction">OUT - the direction that the character should travel</param>
        /// <returns></returns>
        private Point2D GetWanderCharacterPosition(Point2D characterPosition, Point2D scheduledPosition,
            int nMaxDistance, out NonPlayerCharacterMovement.MovementCommandDirection direction)
        {
            Random ran = new Random();
            List<NonPlayerCharacterMovement.MovementCommandDirection> possibleDirections =
                GetPossibleDirectionsList(characterPosition, scheduledPosition, nMaxDistance, true);

            // if no directions are returned then we tell them not to move
            if (possibleDirections.Count == 0)
            {
                direction = NonPlayerCharacterMovement.MovementCommandDirection.None;
                
                return characterPosition.Copy();
            }

            direction = possibleDirections[ran.Next() % possibleDirections.Count];

            Point2D adjustedPosition = NonPlayerCharacterMovement.GetAdjustedPos(characterPosition, direction);

            return adjustedPosition;
        }

        /// <summary>
        /// Points a character in a random position within a certain number of tiles to their scheduled position
        /// </summary>
        /// <param name="mapCharacter">character to wander</param>
        /// <param name="nMaxDistance">max distance character should be from their scheduled position</param>
        /// <param name="bForceWander">force a wander? if not forced then there is a chance they will not move anywhere</param>
        /// <returns>the direction they should move</returns>
        private void WanderWithinN(MapCharacter mapCharacter, int nMaxDistance, bool bForceWander = false)
        {
            Random ran = new Random();

            // 50% of the time we won't even try to move at all
            int nRan = ran.Next(2);
            if (nRan == 0 && !bForceWander) return;

            CharacterPosition characterPosition = mapCharacter.CurrentCharacterPosition;
            CharacterPosition scheduledPosition = mapCharacter.NPCRef.Schedule.GetCharacterDefaultPositionByTime(_timeOfDay);

            // i could get the size dynamically, but that's a waste of CPU cycles
            Point2D adjustedPosition = GetWanderCharacterPosition(characterPosition.XY, scheduledPosition.XY, nMaxDistance, out NonPlayerCharacterMovement.MovementCommandDirection direction);

            // check to see if the random direction is within the correct distance
            if (direction != NonPlayerCharacterMovement.MovementCommandDirection.None && !scheduledPosition.XY.WithinN(adjustedPosition, nMaxDistance))
            {
                throw new Ultima5ReduxException("GetWanderCharacterPosition has told us to go outside of our expected maximum area");
            }
            // can we even travel onto the tile?
            if (!IsTileFreeToTravel(adjustedPosition, true))
            {
                if (direction != NonPlayerCharacterMovement.MovementCommandDirection.None)
                {
                    throw new Ultima5ReduxException("Was sent to a tile, but it isn't in free in WanderWithinN");
                }
                // something else is on the tile, so we don't move
                return;
            }

            // add the single instruction to the queue
            mapCharacter.Movement.AddNewMovementInstruction(new NonPlayerCharacterMovement.MovementCommand(direction, 1));
        }

        /// <summary>
        /// calculates and stores new path for NPC
        /// Placed outside into the VirtualMap since it will need information from the active map, VMap and the MapCharacter itself
        /// </summary>
        private void CalculateNextPath(MapCharacter mapCharacter, int nMapCurrentFloor)
        {
            CharacterPosition npcXy = mapCharacter.NPCRef.Schedule.GetCharacterDefaultPositionByTime(_timeOfDay);

            // the NPC is a non-NPC, so we keep looking
            if (npcXy.X == 0 && npcXy.Y == 0) return;

            // if the NPC is destined for the floor you are on, but are on a different floor, then they need to find a ladder or staircase

            // if the NPC is destined for a different floor then we watch to see if they are on stairs on a ladder
            bool bDifferentFloor = npcXy.Floor != mapCharacter.CurrentCharacterPosition.Floor;
            if (bDifferentFloor)
            {
                // if the NPC is supposed to be on a different floor then the floor we are currently on
                // and we they are already on that other floor - AND they are supposed be on our current floor
                if (nMapCurrentFloor != mapCharacter.CurrentCharacterPosition.Floor && nMapCurrentFloor != npcXy.Floor)
                {
                    return;
                }

                if (nMapCurrentFloor == npcXy.Floor) // destined for the current floor
                {
                    // we already know they aren't on this floor, so that is safe to assume
                    // so we find the closest and best ladder or stairs for them, make sure they are not occupied and send them down
                    CharacterPosition npcPrevXy = mapCharacter.NPCRef.Schedule.GetCharacterPreviousPositionByTime(_timeOfDay);
                    LadderOrStairDirection ladderOrStairDirection = nMapCurrentFloor > npcPrevXy.Floor ?
                        LadderOrStairDirection.Down : LadderOrStairDirection.Up;

                    List<Point2D> stairsAndLadderLocations = GetBestStairsAndLadderLocation(ladderOrStairDirection, npcXy.XY);
                    
                    // let's make sure we have a path we can travel
                    if (stairsAndLadderLocations.Count <= 0)
                    {
                        Debug.WriteLine(mapCharacter.NPCRef.FriendlyName + " can't find a damn ladder or staircase at "+_timeOfDay.FormattedTime);
                        
                        // there is a rare situation (Gardner in Serpents hold) where he needs to go down, but only has access to an up ladder
                        stairsAndLadderLocations = GetBestStairsAndLadderLocation(ladderOrStairDirection==LadderOrStairDirection.Down?LadderOrStairDirection.Up:LadderOrStairDirection.Down, 
                            npcXy.XY);
                        Debug.WriteLine(mapCharacter.NPCRef.FriendlyName + " couldn't find a ladder or stair going "+ladderOrStairDirection+" "+_timeOfDay.FormattedTime);
                        if (stairsAndLadderLocations.Count <= 0)
                        {
                            throw new Ultima5ReduxException(mapCharacter.NPCRef.FriendlyName + " can't find a damn ladder or staircase at "+_timeOfDay.FormattedTime);
                        }
                            
                    }
                    

                    // sloppy, but fine for now
                    CharacterPosition characterPosition = new CharacterPosition
                    {
                        XY = stairsAndLadderLocations[0], Floor = nMapCurrentFloor
                    };
                    mapCharacter.Move(characterPosition, _timeOfDay, false);
                    return;

                    // we now need to build a path to the best choice of ladder or stair
                    // the list returned will be prioritized based on vicinity
                    //foreach (Point2D xy in stairsAndLadderLocations)
                    //{
                    //    bool bPathBuilt = BuildPath(mapCharacter, xy);
                    //    // if a path was successfully built, then we have no need to build another path since this is the "best" path
                    //    if (bPathBuilt) { return; }
                    //}
                    //System.Diagnostics.Debug.WriteLine("Tried to build a path for " + mapCharacter.NPCRef.FriendlyName + " to " + npcXy + " but it failed, keep an eye on it...");
                }
                else // map character is destined for a higher or lower floor
                {
                    TileReference currentTileReference = GetTileReference(mapCharacter.CurrentCharacterPosition.XY);

                    // if we are going to the next floor, then look for something that goes up, otherwise look for something that goes down
                    LadderOrStairDirection ladderOrStairDirection = npcXy.Floor > mapCharacter.CurrentCharacterPosition.Floor ?
                        LadderOrStairDirection.Up : LadderOrStairDirection.Down;
                    bool bNpcShouldKlimb = CheckNPCAndKlimb(currentTileReference, ladderOrStairDirection, mapCharacter.CurrentCharacterPosition.XY);
                    if (bNpcShouldKlimb)
                    {
                        // teleport them and return immediately
                        mapCharacter.MoveNPCToDefaultScheduledPosition(_timeOfDay);
                        System.Diagnostics.Debug.WriteLine(mapCharacter.NPCRef.FriendlyName + " just went to a different floor");
                        return;
                    }

                    // we now need to build a path to the best choice of ladder or stair
                    // the list returned will be prioritized based on vicinity
                    List<Point2D> stairsAndLadderLocations = getBestStairsAndLadderLocationBasedOnCurrentPosition(ladderOrStairDirection, 
                        npcXy.XY, mapCharacter.CurrentCharacterPosition.XY);
                    foreach (Point2D xy in stairsAndLadderLocations)
                    {
                        bool bPathBuilt = BuildPath(mapCharacter, xy);
                        // if a path was successfully built, then we have no need to build another path since this is the "best" path
                        if (bPathBuilt) { return; }
                    }
                    System.Diagnostics.Debug.WriteLine("Tried to build a path for "+mapCharacter.NPCRef.FriendlyName + " to "+npcXy +" but it failed, keep an eye on it...");
                    return;
                }
            }

            NonPlayerCharacterReference.NonPlayerCharacterSchedule.AiType aiType = mapCharacter.NPCRef.Schedule.GetCharacterAiTypeByTime(_timeOfDay);

            // is the character is in their prescribed location?
            if (mapCharacter.CurrentCharacterPosition == npcXy)
            {
                // test all the possibilities, special calculations for all of them
                switch (aiType)
                {
                    case NonPlayerCharacterReference.NonPlayerCharacterSchedule.AiType.Fixed:
                        // do nothing, they are where they are supposed to be 
                        break;
                    case NonPlayerCharacterReference.NonPlayerCharacterSchedule.AiType.DrudgeWorthThing:
                    case NonPlayerCharacterReference.NonPlayerCharacterSchedule.AiType.Wander:
                        // choose a tile within N tiles that is not blocked, and build a single path
                        WanderWithinN(mapCharacter, 2);
                        break;
                    case NonPlayerCharacterReference.NonPlayerCharacterSchedule.AiType.BigWander:
                        // choose a tile within N tiles that is not blocked, and build a single path
                        WanderWithinN(mapCharacter, 4);
                        break;
                    case NonPlayerCharacterReference.NonPlayerCharacterSchedule.AiType.ChildRunAway:
                        break;
                    case NonPlayerCharacterReference.NonPlayerCharacterSchedule.AiType.MerchantThing:
                        // don't think they move....?
                        break;
                    case NonPlayerCharacterReference.NonPlayerCharacterSchedule.AiType.ExtortOrAttackOrFollow:
                        // set location of Avatar as way point, but only set the first movement from the list if within N of Avatar
                        break;
                    default:
                        throw new Ultima5ReduxException("An unexpected movement AI was encountered: " + aiType.ToString() + " for NPC: " + mapCharacter.NPCRef.Name);
                }
            }
            else // character not in correct position
            {
                switch (aiType)
                {
                    case NonPlayerCharacterReference.NonPlayerCharacterSchedule.AiType.MerchantThing:
                    case NonPlayerCharacterReference.NonPlayerCharacterSchedule.AiType.Fixed:
                        // move to the correct position
                        BuildPath(mapCharacter, npcXy.XY);
                        break;
                    case NonPlayerCharacterReference.NonPlayerCharacterSchedule.AiType.DrudgeWorthThing:
                    case NonPlayerCharacterReference.NonPlayerCharacterSchedule.AiType.Wander:
                    case NonPlayerCharacterReference.NonPlayerCharacterSchedule.AiType.BigWander:
                        // different wanders have different different radius'
                        int nWanderTiles =
                            aiType == NonPlayerCharacterReference.NonPlayerCharacterSchedule.AiType.Wander ? 2 : 4;
                        // we check to see how many moves it would take to get to their destination, if it takes
                        // more than the allotted amount then we first build a path to the destination
                        // note: because you are technically within X tiles doesn't mean you can access it
                        int nMoves = GetTotalMovesToLocation(mapCharacter.CurrentCharacterPosition.XY, npcXy.XY);
                        // 
                        if (nMoves <= nWanderTiles)
                        {
                            WanderWithinN(mapCharacter, nWanderTiles);
                        }
                        else
                        {
                            // move to the correct position
                            BuildPath(mapCharacter, npcXy.XY);
                        }
                        break;
                    case NonPlayerCharacterReference.NonPlayerCharacterSchedule.AiType.ChildRunAway:
                        // if the avatar is close by then move away from him, otherwise return to original path, one move at a time
                        break;
                    case NonPlayerCharacterReference.NonPlayerCharacterSchedule.AiType.ExtortOrAttackOrFollow:
                        // set location of Avatar as way point, but only set the first movement from the list if within N of Avatar
                        break;
                    default:
                        throw new Ultima5ReduxException("An unexpected movement AI was encountered: " + aiType.ToString() + " for NPC: " + mapCharacter.NPCRef.Name);
                }
            }
        }

        /// <summary>
        /// Gets a list of points for all stairs and ladders  
        /// </summary>
        /// <param name="ladderOrStairDirection">direction of all stairs and ladders</param>
        /// <returns></returns>
        private List<Point2D> GetListOfAllLaddersAndStairs(LadderOrStairDirection ladderOrStairDirection)
        {
            List<Point2D> laddersAndStairs = new List<Point2D>();

            // go through every single tile on the map looking for ladders and stairs
            for (int x = 0; x < SmallMap.XTILES; x++)
            {
                for (int y = 0; y < SmallMap.YTILES; y++)
                {
                    TileReference tileReference = GetTileReference(x, y);
                    if (ladderOrStairDirection == LadderOrStairDirection.Down)
                    {
                        // if this is a ladder or staircase and it's in the right direction, then add it to the list
                        if (_tileReferences.IsLadderDown(tileReference.Index) || IsStairsGoingDown(new Point2D(x, y)))
                        {
                            laddersAndStairs.Add(new Point2D(x, y));
                        }
                    }
                    else // otherwise we know you are going up
                    {   
                        
                        if (_tileReferences.IsLadderUp(tileReference.Index) || (_tileReferences.IsStaircase(tileReference.Index) && IsStairGoingUp(new Point2D(x, y))))
                        {
                            laddersAndStairs.Add(new Point2D(x, y));
                        }
                    }
                } // end y for
            } // end x for
            
            return laddersAndStairs;
        }

        /// <summary>
        /// Gets the shortest path between a list of 
        /// </summary>
        /// <param name="positionList">list of positions</param>
        /// <param name="destinedPosition">the destination position</param>
        /// <returns>an ordered directory list of paths based on the shortest path (straight line path)</returns>
        private SortedDictionary<double, Point2D> GetShortestPaths(List<Point2D> positionList, Point2D destinedPosition)
        {
            SortedDictionary<double, Point2D> sortedPoints = new SortedDictionary<double, Point2D>();

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

        /// <summary>
        /// Gets the best possible stair or ladder location
        /// to go to the destinedPosition
        /// Ladder/Stair -> destinedPositon
        /// </summary>
        /// <param name="ladderOrStairDirection">go up or down a ladder/stair</param>
        /// <param name="destinedPosition">the position to go to</param>
        /// <returns></returns>
        private List<Point2D> GetBestStairsAndLadderLocation(LadderOrStairDirection ladderOrStairDirection,
            Point2D destinedPosition)
        {
            // get all ladder and stairs locations based (only up or down ladders/stairs)
            List<Point2D> allLaddersAndStairList = GetListOfAllLaddersAndStairs(ladderOrStairDirection);

            // get an ordered dictionary of the shortest straight line paths
            SortedDictionary<double, Point2D> sortedPoints = GetShortestPaths(allLaddersAndStairList, destinedPosition);

            // ordered list of the best choice paths (only valid paths) 
            List<Point2D> bestChoiceList = new List<Point2D>(sortedPoints.Count);

            // to make it more familiar, we will transfer to an ordered list
            foreach (Point2D xy in sortedPoints.Values)
            {
                bool bPathBuilt = GetTotalMovesToLocation(destinedPosition, xy) > 0;
                // we first make sure that the path even exists before we add it to the list
                if (bPathBuilt) bestChoiceList.Add(xy);

                continue;
            }

            return bestChoiceList;
        }

        /// <summary>
        /// Gets the best possible stair or ladder locations from the current position to the given ladder/stair direction
        /// currentPosition -> best ladder/stair
        /// </summary>
        /// <param name="ladderOrStairDirection">which direction will we try to get to</param>
        /// <param name="destinedPosition">the position you are trying to get to</param>
        /// <param name="currentPosition">the current position of the character</param>
        /// <returns></returns>
        private List<Point2D> getBestStairsAndLadderLocationBasedOnCurrentPosition(LadderOrStairDirection ladderOrStairDirection, Point2D destinedPosition, Point2D currentPosition)
        {
            // get all ladder and stairs locations based (only up or down ladders/stairs)
            List<Point2D> allLaddersAndStairList = GetListOfAllLaddersAndStairs(ladderOrStairDirection);

            // get an ordered dictionary of the shortest straight line paths
            SortedDictionary<double, Point2D> sortedPoints = GetShortestPaths(allLaddersAndStairList, destinedPosition);

            // ordered list of the best choice paths (only valid paths) 
            List<Point2D> bestChoiceList = new List<Point2D>(sortedPoints.Count);
            
            // to make it more familiar, we will transfer to an ordered list
            foreach (Point2D xy in sortedPoints.Values)
            {
                bool bPathBuilt = GetTotalMovesToLocation(currentPosition, xy) > 0;
                // we first make sure that the path even exists before we add it to the list
                if (bPathBuilt) bestChoiceList.Add(xy);
                
                continue;
            }

            return bestChoiceList;
        }

        /// <summary>
        /// Returns the total number of moves to the number of moves for the character to reach a point
        /// </summary>
        /// <param name="currentXy"></param>
        /// <param name="targetXy">where the character would move</param>
        /// <returns>the number of moves to the targetXy</returns>
        /// <remarks>This is expensive, and would be wonderful if we had a better way to get this info</remarks>
        private int GetTotalMovesToLocation(Point2D currentXy, Point2D targetXy)
        {            
            Stack<Node> nodeStack = CurrentMap.AStar.FindPath(new System.Numerics.Vector2(currentXy.X, currentXy.Y),
            new System.Numerics.Vector2(targetXy.X, targetXy.Y));

            return nodeStack?.Count ?? 0;
        }
        
        /// <summary>
        /// Builds the actual path for the character to travel based on their current position and their target position
        /// </summary>
        /// <param name="mapCharacter">where the character is presently</param>
        /// <param name="targetXy">where you want them to go</param>
        /// <returns>returns true if a path was found, false if it wasn't</returns>
        private bool BuildPath(MapCharacter mapCharacter, Point2D targetXy)
        {
            NonPlayerCharacterMovement.MovementCommandDirection getCommandDirection(Point2D fromXy, Point2D toXy)
            {
                if (fromXy == toXy) return NonPlayerCharacterMovement.MovementCommandDirection.None;
                if (fromXy.X < toXy.X) return NonPlayerCharacterMovement.MovementCommandDirection.East;
                if (fromXy.Y < toXy.Y) return NonPlayerCharacterMovement.MovementCommandDirection.South;
                if (fromXy.X > toXy.X) return NonPlayerCharacterMovement.MovementCommandDirection.West;
                if (fromXy.Y > toXy.Y) return NonPlayerCharacterMovement.MovementCommandDirection.North;
                throw new Ultima5ReduxException("For some reason we couldn't determine the path of the command direction in getCommandDirection");
            }

            Point2D vector2ToPoint2D(Vector2 vector)
            {
                return new Point2D((int)vector.X, (int)vector.Y);
            }

            if (mapCharacter.CurrentCharacterPosition.XY == targetXy)
            {
                throw new Ultima5ReduxException("Asked to build a path, but " + mapCharacter.NPCRef.Name + " is already at " + targetXy.X.ToString() + "," + targetXy.Y); //+ "," + targetXy.Floor);
            }

            // todo: need some code that checks for different floors and directs them to closest ladder or staircase instead of same floor position

            Stack<Node> nodeStack = CurrentMap.AStar.FindPath(new System.Numerics.Vector2(mapCharacter.CurrentCharacterPosition.XY.X, mapCharacter.CurrentCharacterPosition.XY.Y),
                new System.Numerics.Vector2(targetXy.X, targetXy.Y));

            NonPlayerCharacterMovement.MovementCommandDirection prevDirection = NonPlayerCharacterMovement.MovementCommandDirection.None;
            NonPlayerCharacterMovement.MovementCommandDirection newDirection = NonPlayerCharacterMovement.MovementCommandDirection.None;
            Point2D prevPosition = mapCharacter.CurrentCharacterPosition.XY;

            // temporary while I figure out why this happens
            if (nodeStack == null) return false;

            int nInARow = 0;
            // builds the movement list that is compatible with the original U5 movement instruction queue stored in the state file
            foreach (Node node in nodeStack)
            {
                Point2D newPosition = vector2ToPoint2D(node.Position);
                newDirection = getCommandDirection(prevPosition, newPosition);

                // if the previous direction is the same as the current direction, then we keep track so that we can issue a single instruction
                // that has N iterations (ie. move East 5 times)
                if (prevDirection == newDirection || prevDirection == NonPlayerCharacterMovement.MovementCommandDirection.None)
                {
                    nInARow++;
                }
                else
                {
                    // if the direction has changed then we add the previous direction and reset the concurrent counter
                    mapCharacter.Movement.AddNewMovementInstruction(new NonPlayerCharacterMovement.MovementCommand(prevDirection, nInARow));
                    nInARow = 1;
                }
                prevDirection = newDirection;
                prevPosition = newPosition;
            }
            if (nInARow > 0) { mapCharacter.Movement.AddNewMovementInstruction(new NonPlayerCharacterMovement.MovementCommand(newDirection, nInARow)); }
            return true;
        }

        /// <summary>
        /// Checks if an NPC is on a stair or ladder, and if it goes in the correct direction then it returns true indicating they can teleport
        /// </summary>
        /// <param name="currentTileRef">the tile they are currently on</param>
        /// <param name="ladderOrStairDirection"></param>
        /// <param name="xy"></param>
        /// <returns></returns>
        private bool CheckNPCAndKlimb(TileReference currentTileRef, LadderOrStairDirection ladderOrStairDirection, Point2D xy)
        {
            // is player on a ladder or staircase going in the direction they intend to go?
            bool bIsOnStairCaseOrLadder = _tileReferences.IsStaircase(currentTileRef.Index) || _tileReferences.IsLadder(currentTileRef.Index);

            if (!bIsOnStairCaseOrLadder) return false;
            
            // are they destined to go up or down it?
            if (_tileReferences.IsStaircase(currentTileRef.Index))
            {
                if (IsStairGoingUp(xy))
                {
                    return ladderOrStairDirection == LadderOrStairDirection.Up;
                }
                else
                {
                    return ladderOrStairDirection == LadderOrStairDirection.Down;
                }
            }
            else // it's a ladder
            {
                if (_tileReferences.IsLadderUp(currentTileRef.Index))
                {
                    return ladderOrStairDirection == LadderOrStairDirection.Up;
                }
                else
                {
                    return ladderOrStairDirection == LadderOrStairDirection.Down;
                }
            }
        }

        /// <summary>
        /// Advances each of the NPCs by one movement each
        /// </summary>
        /// <returns></returns>
        internal bool MoveNPCs()
        {
            // if not on small map - then no NPCs!
            if (IsLargeMap) return false;

            // go through each of the NPCs on the map
            foreach (MapCharacter mapChar in TheMapCharacters.Characters.Where(mapChar => mapChar.IsActive))
            {
                // if there is no next available movement then we gotta recalculate and see if they should move
                if (!mapChar.Movement.IsNextCommandAvailable())
                {
                    CalculateNextPath(mapChar, CurrentSingleMapReference.Floor);
                }
                
                // if this NPC has a command in the buffer, so let's execute!
                if (!mapChar.Movement.IsNextCommandAvailable()) continue;
                
                // it's possible that CalculateNextPath came up empty for a variety of reasons, and that's okay
                // peek and see what we have before we pop it off
                NonPlayerCharacterMovement.MovementCommandDirection direction = mapChar.Movement.GetNextMovementCommandDirection(true);
                Point2D adjustedPos = NonPlayerCharacterMovement.GetAdjustedPos(mapChar.CurrentCharacterPosition.XY, direction);

                // need to evaluate if I can even move to the next tile before actually popping out of the queue
                bool bIsNpcOnSpace = IsNPCTile(adjustedPos);
                //TileReference adjustedTile = GetTileReference(adjustedPos);
                if (GetTileReference(adjustedPos).IsNPCCapableSpace && !bIsNpcOnSpace)
                {
                    // pop the direction from the queue
                    direction = mapChar.Movement.GetNextMovementCommandDirection(false);
                    mapChar.Move(adjustedPos, mapChar.CurrentCharacterPosition.Floor, _timeOfDay);
                    mapChar.MovementAttempts = 0;
                }
                else
                {
                    mapChar.MovementAttempts++;
                }

                if (mapChar.ForcedWandering > 0)
                {
                    WanderWithinN(mapChar, 32, true);
                    mapChar.ForcedWandering--;
                    continue;
                }
                    
                // if we have tried a few times and failed then we will recalculate
                // could have been a fixed NPC, stubborn Avatar or whatever
                if (mapChar.MovementAttempts <= 2) continue;

                // a little clunky - but basically if a the NPC can't move then it picks a random direction to move (as long as it's legal)
                // and moves that single tile, which will then ultimately follow up with a recalculated route, hopefully breaking and deadlocks with other
                // NPCs
                Debug.WriteLine(mapChar.NPCRef.FriendlyName + " got stuck after " + mapChar.MovementAttempts + " so we are going to find a new direction for them");
                        
                mapChar.Movement.ClearMovements();
                        
                // we are sick of waiting and will force a wander for a random number of turns to try to let the little
                // dummies figure it out on their own
                Random ran = new Random();
                int nTimes = ran.Next(0, 2) + 1;
                WanderWithinN(mapChar, 32, true);

                mapChar.ForcedWandering = nTimes;
                mapChar.MovementAttempts = 0;
            }

            return true;
        }
        #endregion

        #region Public Actions Methods
        // Action methods are things that the Avatar may do that will affect things around him like
        // getting a torch changes the tile underneath, opening a door may set a timer that closes it again
        // in a few turns
        //public void PickUpThing(Point2D xy)
        //{
        //    // todo: will need to actually poccket the thing I picked up
        //    SetOverridingTileReferece(tileReferences.GetTileReferenceByName("BrickFloor"), xy);
        //}


        /// <summary>
        /// Use the stairs and change floors, loading a new map
        /// </summary>
        /// <param name="xy">the position of the stairs, ladder or trapdoor</param>
        /// <param name="bForceDown">force a downward stairs</param>
        public void UseStairs(Point2D xy, bool bForceDown = false)
        {
            bool bStairGoUp = IsStairGoingUp() && !bForceDown;
            CurrentPosition = xy.Copy();
            LoadSmallMap(SmallMapRefs.GetSingleMapByLocation(CurrentSingleMapReference.MapLocation, 
                CurrentSmallMap.MapFloor + (bStairGoUp ? 1 : -1)));
        }

        #endregion

        #region Public Boolean Method
        /// <summary>
        /// Is there food on a table within 1 (4 way) tile
        /// Used for determining if eating animation should be used
        /// </summary>
        /// <param name="characterPos"></param>
        /// <returns>true if food is within a tile</returns>
        public bool IsFoodNearby(Point2D characterPos)
        {
            bool isFoodTable(int nSprite)
            {
                return (nSprite == _tileReferences.GetTileReferenceByName("TableFoodTop").Index
                    || nSprite == _tileReferences.GetTileReferenceByName("TableFoodBottom").Index
                    || nSprite == _tileReferences.GetTileReferenceByName("TableFoodBoth").Index);
            }

            //Todo: use TileReference lookups instead of hard coded values
            if (CurrentSingleMapReference == null) return false;
            // yuck, but if the food is up one tile or down one tile, then food is nearby
            bool bIsFoodNearby = (isFoodTable(GetTileReference(characterPos.X, characterPos.Y - 1).Index)
                                  || isFoodTable(GetTileReference(characterPos.X, characterPos.Y + 1).Index));
            return bIsFoodNearby;

        }

        /// <summary>
        /// Are the stairs at the given position going up?
        /// Be sure to check if they are stairs first
        /// </summary>
        /// <param name="xy"></param>
        /// <returns></returns>
        public bool IsStairGoingUp(Point2D xy)
        {
            if (!_tileReferences.IsStaircase(GetTileReference(xy).Index)) return false;

            bool bStairGoUp = _smallMaps.DoStrairsGoUp(CurrentSmallMap.MapLocation, CurrentSmallMap.MapFloor, xy);
            return bStairGoUp;
        }

        
        
        public bool IsAvatarSitting()
        {
            return (_tileReferences.IsChair(GetTileReferenceOnCurrentTile().Index));
        }
        

        /// <summary>
        /// Are the stairs at the given position going down?
        /// Be sure to check if they are stairs first
        /// </summary>
        /// <param name="xy"></param>
        /// <returns></returns>
        public bool IsStairsGoingDown(Point2D xy)
        {
            if (!_tileReferences.IsStaircase(GetTileReference(xy).Index)) return false;
            bool bStairGoUp = _smallMaps.DoStairsGoDown(CurrentSmallMap.MapLocation, CurrentSmallMap.MapFloor, xy);
            return bStairGoUp;
        }

        /// <summary>
        /// Are the stairs at the player characters current position going down?
        /// </summary>
        /// <returns></returns>
        public bool IsStairsGoingDown()
        {
            return IsStairsGoingDown(CurrentPosition);
        }

        /// <summary>
        /// Are the stairs at the player characters current position going up?
        /// </summary>
        /// <returns></returns>
        public bool IsStairGoingUp()
        {
            return IsStairGoingUp(CurrentPosition);
        }

        /// <summary>
        /// When orienting the stairs, which direction should they be drawn 
        /// </summary>
        /// <param name="xy"></param>
        /// <returns></returns>
        public Direction GetStairsDirection(Point2D xy)
        {
            // we are making a BIG assumption at this time that a stair case ONLY ever has a single
            // entrance point, and solid walls on all other sides... hopefully this is true
            if (!GetTileReference(xy.X - 1, xy.Y).IsSolidSprite) return Direction.Left;
            if (!GetTileReference(xy.X + 1, xy.Y).IsSolidSprite) return Direction.Right;
            if (!GetTileReference(xy.X, xy.Y - 1).IsSolidSprite) return Direction.Up;
            if (!GetTileReference(xy.X, xy.Y + 1).IsSolidSprite) return Direction.Down;
            throw new Ultima5ReduxException("Can't get stair direction - something is amiss....");
        }

        /// <summary>
        /// Given the orientation of the stairs, it returns the correct sprite to display
        /// </summary>
        /// <param name="xy">position of stairs</param>
        /// <returns>stair sprite</returns>
        public int GetStairsSprite(Point2D xy)
        {
            bool bGoingUp = IsStairGoingUp(xy);//UltimaGlobal.IsStairGoingUp(voxelPos);
            VirtualMap.Direction direction = GetStairsDirection(xy);
            int nSpriteNum = -1;
            switch (direction)
            {
                case VirtualMap.Direction.Up:
                    nSpriteNum = bGoingUp ? _tileReferences.GetTileReferenceByName("StairsNorth").Index
                        : _tileReferences.GetTileReferenceByName("StairsSouth").Index;
                    break;
                case VirtualMap.Direction.Down:
                    nSpriteNum = bGoingUp ? _tileReferences.GetTileReferenceByName("StairsSouth").Index
                        : _tileReferences.GetTileReferenceByName("StairsNorth").Index;
                    break;
                case VirtualMap.Direction.Left:
                    nSpriteNum = bGoingUp ? _tileReferences.GetTileReferenceByName("StairsWest").Index
                        : _tileReferences.GetTileReferenceByName("StairsEast").Index;
                    break;
                case VirtualMap.Direction.Right:
                    nSpriteNum = bGoingUp ? _tileReferences.GetTileReferenceByName("StairsEast").Index
                        : _tileReferences.GetTileReferenceByName("StairsWest").Index;
                    break;
                case Direction.None:
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
            return nSpriteNum;
        }

        /// <summary>
        /// Is the door at the specified coordinate horizontal?
        /// </summary>
        /// <param name="xy"></param>
        /// <returns></returns>
        public bool IsHorizDoor(Point2D xy)
        {
            if ((GetTileReference(xy.X - 1, xy.Y).IsSolidSpriteButNotDoor
                || GetTileReference(xy.X + 1, xy.Y).IsSolidSpriteButNotDoor))
                return true;
            
            return false;
        }

        /// <summary>
        /// Is there an NPC on the tile specified?
        /// </summary>
        /// <param name="xy"></param>
        /// <returns></returns>
        public bool IsNPCTile(Point2D xy)
        {
            // this method isnt super efficient, may want to optimize in the future
            if (IsLargeMap) return false;
            return (GetNPCOnTile(xy) != null);
        }

        public bool ContainsSearchableThings(Point2D xy)
        {
            // moonstone check
            if (IsLargeMap && _moongates.IsMoonstoneBuried(xy, LargeMapOverUnder))
            {
                return true;
            }

            return false;
        }

        /// <summary>
        /// Once you have confirmed that there is something searchable with ContainsSearchableThings, you
        /// will need to "stir them up", which means they become visible to the user on the map and
        /// made available for "getting" from the virtual map
        /// </summary>
        /// <param name="xy"></param>
        public void StirUpTileSearch(Point2D xy)
        {
            
        }
        

        public void SwapTiles(Point2D tile1Pos, Point2D tile2Pos)
        {
            TileReference tileRef1 = GetTileReference(tile1Pos);
            TileReference tileRef2 = GetTileReference(tile2Pos);
            
            SetOverridingTileReferece(tileRef1, tile2Pos);
            SetOverridingTileReferece(tileRef2, tile1Pos);
        }

        /// <summary>
        /// Attempts to guess the tile underneath a thing that is upright such as a fountain
        /// </summary>
        /// <param name="xy">position of the thing</param>
        /// <returns>tile (sprite) number</returns>
        public int GuessTile(Point2D xy)
        {
            Dictionary<int, int> tileCountDictionary = new Dictionary<int, int>();
            
            // we check our high level tile override
            // todo: technically this is only for 3D worlds, we should consider that
            // this method is much quicker because we only load the data once in the maps 
            if (!IsLargeMap && CurrentMap.IsXYOverride(xy))
            {
                //Debug.WriteLine("Wanted to guess a tile but it was overriden: "+new CharacterPosition(xy.X,xy.Y,CurrentSingleMapReference.Floor));
                return CurrentMap.GetTileOverride(xy).SpriteNum;
            }
            else if (IsLargeMap && CurrentMap.IsXYOverride(xy))
            {
                return CurrentMap.GetTileOverride(xy).SpriteNum;
            }
            
            // if has exposed search then we evaluate and see if it is actually a normal tile underneath
            int nExposedCount = _exposedSearchItems[xy.X][xy.Y]?.Count ?? 0; 
            if (nExposedCount > 0)
            {
                // there are exposed items on this tile
                TileReference tileRef = GetTileReference(xy.X, xy.Y, bIgnoreExposed: true, bIgnoreMoongate: true);
                if (tileRef.FlatTileSubstitionIndex != -2)
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
                    // only look at non-upright sprites
                    if (tileRef.IsUpright) continue;
                    
                    int nTile = tileRef.Index;
                    if (tileCountDictionary.ContainsKey(nTile)) { tileCountDictionary[nTile] += 1; }
                    else { tileCountDictionary.Add(nTile, 1); }
                }
            }

            int nMostTile = -1;
            int nMostTileTotal = -1;
            // go through each of the tiles we saw and record the tile with the most instances
            foreach (int nTile in tileCountDictionary.Keys)
            {
                int nTotal = tileCountDictionary[nTile];
                if (nMostTile == -1 || nTotal > nMostTileTotal) { nMostTile = nTile; nMostTileTotal = nTotal; }
            }

            // just in case we didn't find a match - just use grass for now
            return nMostTile == -1 ? 5 : nMostTile;
        }
        #endregion
    }
}