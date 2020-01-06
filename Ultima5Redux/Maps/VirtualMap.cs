using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;

namespace Ultima5Redux
{
    public class VirtualMap
    {
        #region Private fields
        private NonPlayerCharacterReferences npcRefs;
        private GameState state;
        /// <summary>
        /// Reference to towne/keep etc locations on the large map
        /// </summary>
        private LargeMapReference largeMapReferences;
        /// <summary>
        /// Non player characters on current map
        /// </summary>
        private NonPlayerCharacterReferences nonPlayerCharacters;
        /// <summary>
        /// All the small maps
        /// </summary>
        private SmallMaps smallMaps;
        private MapCharacterAnimationStates characterStates;
        /// <summary>
        /// Both underworld and overworld maps
        /// </summary>
        private Dictionary<LargeMap.Maps, LargeMap> largeMaps = new Dictionary<LargeMap.Maps, LargeMap>(2);
        /// <summary>
        /// References to all tiles
        /// </summary>
        private TileReferences tileReferences;
        /// <summary>
        /// override map is responsible for overriding tiles that would otherwise be static
        /// </summary>
        private int[][] overrideMap;
        /// <summary>
        /// Current position of player character (avatar)
        /// </summary>
        private Point2D _currentPosition = new Point2D(0, 0);
        #endregion

        #region Public Properties 
        /// <summary>
        /// 4 way direction
        /// </summary>
        public enum Direction { Up, Down, Left, Right };

        /// <summary>
        /// Current position of player character (avatar)
        /// </summary>
        public Point2D CurrentPosition { 
            get
            {
                return _currentPosition;
            }
            set 
            {
                _currentPosition.X = value.X; 
                _currentPosition.Y = value.Y; 
            } 
        } 

        /// <summary>
        /// Number of total columns for current map
        /// </summary>
        public int NumberOfColumnTiles { get { return overrideMap[0].Length; } }
        /// <summary>
        /// Number of total rows for current map
        /// </summary>
        public int NumberOfRowTiles { get { return overrideMap.Length; } }
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
        public Map CurrentMap { get { return (IsLargeMap ? (Map)CurrentLargeMap : (Map)CurrentSmallMap); } }
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
        /// <summary>
        /// If we are on a large map - then are we on overworld or underworld
        /// </summary>
        public LargeMap.Maps LargeMapOverUnder { get; private set; } = (LargeMap.Maps)(-1);

        public MapCharacters TheMapCharacters { get; private set; }

        #endregion

        #region Constructor, Initializers and Loaders
        /// <summary>
        /// Construct the VirtualMap (requires initalization still)
        /// </summary>
        /// <param name="smallMapReferences"></param>
        /// <param name="smallMaps"></param>
        /// <param name="largeMapReferences"></param>
        /// <param name="overworldMap"></param>
        /// <param name="underworldMap"></param>
        /// <param name="nonPlayerCharacters"></param>
        /// <param name="tileReferences"></param>
        public VirtualMap(SmallMapReferences smallMapReferences, SmallMaps smallMaps, LargeMapReference largeMapReferences, 
            LargeMap overworldMap, LargeMap underworldMap, NonPlayerCharacterReferences nonPlayerCharacters, TileReferences tileReferences,
            GameState state, NonPlayerCharacterReferences npcRefs)
        {
            this.SmallMapRefs = smallMapReferences;
            this.smallMaps = smallMaps;
            this.nonPlayerCharacters = nonPlayerCharacters;
            this.largeMapReferences = largeMapReferences;
            this.tileReferences = tileReferences;
            this.state = state;
            this.npcRefs = npcRefs;
            //this.characterStates = characterStates;
            largeMaps.Add(LargeMap.Maps.Overworld, overworldMap);
            largeMaps.Add(LargeMap.Maps.Underworld, underworldMap);
        }

        /// <summary>
        /// Loads a small map based on the provided reference
        /// </summary>
        /// <param name="singleMapReference"></param>
        public void LoadSmallMap(SmallMapReferences.SingleMapReference singleMapReference)
        {
            CurrentSingleMapReference = singleMapReference;
            CurrentSmallMap = smallMaps.GetSmallMap(singleMapReference.MapLocation, singleMapReference.Floor);
            overrideMap = Utils.Init2DArray<int>(CurrentSmallMap.TheMap[0].Length, CurrentSmallMap.TheMap.Length);
            IsLargeMap = false;
            LargeMapOverUnder = (LargeMap.Maps)(-1);

            TheMapCharacters = new MapCharacters(tileReferences, npcRefs, singleMapReference, LargeMap.Maps.Small,
                state.CharacterAnimationStatesDataChunk,state.OverworldOverlayDataChunks, state.UnderworldOverlayDataChunks, state.CharacterStatesDataChunk,
                state.NonPlayerCharacterMovementLists, state.NonPlayerCharacterMovementOffsets);
        }

        /// <summary>
        /// Loads a large map -either overworld or underworld
        /// </summary>
        /// <param name="map"></param>
        public void LoadLargeMap(LargeMap.Maps map)
        {
            int nFloor = map == LargeMap.Maps.Overworld ? 0 : -1;
            CurrentSingleMapReference = null;//SmallMapRefs.GetSingleMapByLocation(SmallMapReferences.SingleMapReference.Location.Britainnia_Underworld, nFloor);
            CurrentLargeMap = largeMaps[map];
            overrideMap = Utils.Init2DArray<int>(CurrentLargeMap.TheMap[0].Length, CurrentLargeMap.TheMap.Length);
            IsLargeMap = true;
            LargeMapOverUnder = map;

            TheMapCharacters = new MapCharacters(tileReferences, npcRefs, null, LargeMapOverUnder,
                state.CharacterAnimationStatesDataChunk, state.OverworldOverlayDataChunks, state.UnderworldOverlayDataChunks, state.CharacterStatesDataChunk,
                state.NonPlayerCharacterMovementLists, state.NonPlayerCharacterMovementOffsets);

        }
        #endregion

        #region Tile references and character positioning
        /// <summary>
        /// Gets a tile reference from the given coordinate
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <returns></returns>
        public TileReference GetTileReference(int x, int y)
        {
            // we check to see if our override map has something on top of it
            if (overrideMap[x][y] != 0)
                return tileReferences.GetTileReference(overrideMap[x][y]);

            if (IsLargeMap)
            {
                return (tileReferences.GetTileReference(CurrentLargeMap.TheMap[x][y]));
            }
            else
            {
                return (tileReferences.GetTileReference(CurrentSmallMap.TheMap[x][y]));
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
            overrideMap[x][y] = tileReference.Index;
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
        /// If an NPC is on a tile, then it will get them
        /// assumes it's on the same floor
        /// </summary>
        /// <param name="xy"></param>
        /// <returns>the NPC or null if one does not exist</returns>
        public MapCharacter GetNPCOnTile(Point2D xy)
        {
            SmallMapReferences.SingleMapReference.Location location = CurrentSingleMapReference.MapLocation;

            MapCharacter mapCharacter = TheMapCharacters.GetMapCharacterByLocation(location, xy, CurrentSingleMapReference.Floor);
            //List<NonPlayerCharacterReference> npcs = nonPlayerCharacters.GetNonPlayerCharactersByLocation(location);

            //MapCharacterAnimationState characterState = characterStates.GetCharacterStateByPosition(xy, CurrentSingleMapReference.Floor);

            // get the NPC on the current tile
            //NonPlayerCharacterReference npc = nonPlayerCharacters.GetNonPlayerCharacter(location, xy, CurrentSingleMapReference.Floor);

            //if (npc == null)
            //throw new Exception("You asked for an NPC on a tile that one does not exist - you should have checked first!");

            return mapCharacter;
            //foreach (NonPlayerCharacters.NonPlayerCharacter npc in npcs)
            //{

            //    int nIndex = 1;
            //    Point2D npcXy = npc.Schedule.GetHardCoord(nIndex);

            //    // the NPC is a non-NPC, so we keep looking
            //    if (npcXy.X == 0 && npcXy.Y == 0) continue;

            //    // we found the right NPC and are they on the correct floor
            //    if (npcXy == xy && CurrentSingleMapReference.Floor == npc.Schedule.Coords[nIndex].Z)
            //    {
            //        return npc;
            //    }
            //}
            return null;
        }
        #endregion

        #region Private Methods
        internal bool MoveNPCs()
        {
            // if not on small map - then no NPCs!
            if (IsLargeMap) return false;

            // go through each of the NPCs on the map
            //foreach (NonPlayerCharacterReference npc in TheMapCharacters.NPCRefs.GetNonPlayerCharactersByLocation(CurrentSingleMapReference.MapLocation))
            foreach (MapCharacter mapChar in TheMapCharacters.Characters)
            {
                // this NPC has a command in the buffer, so let's execute!
                if (mapChar.Movement.IsNextCommandAvailable())
                {
                    // peek and see what we have before we pop it off
                    NonPlayerCharacterMovement.MovementCommandDirection direction = mapChar.Movement.GetNextMovementCommand(true);
                    Point2D adjustedPos = NonPlayerCharacterMovement.GetAdjustedPos(mapChar.CurrentMapPosition, direction);
                    // need to evaluate if I can even move to the next tile before actually popping out of the queue
                    if (GetTileReference(adjustedPos).IsNPCCapableSpace)
                    {
                        // pop the direction from the queue
                        direction = mapChar.Movement.GetNextMovementCommand(false);
                        mapChar.Move(adjustedPos, mapChar.CurrentFloor);
                    }
                }
            }

            return true;
        }
        #endregion

        #region Public Actions Methods
        // Action methods are things that the Avatar may do that will affect things around him like
        // getting a torch changes the tile underneath, openning a door may set a timer that closes it again
        // in a few turns
        //public void PickUpThing(Point2D xy)
        //{
        //    // todo: will need to actually poccket the thing I picked up
        //    SetOverridingTileReferece(tileReferences.GetTileReferenceByName("BrickFloor"), xy);
        //}


        public void UseStairs(Point2D xy)
        {
            bool bStairGoUp = IsStairGoingUp();

            LoadSmallMap(SmallMapRefs.GetSingleMapByLocation(CurrentSingleMapReference.MapLocation, CurrentSmallMap.MapFloor + (bStairGoUp ? 1 : -1)));
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
            //Todo: use TileReference lookups instead of hard coded values
            //if (currentSingleSmallMapReferences == null) return false;
            if (CurrentSingleMapReference == null) return false;
            // yuck, but if the food is up one tile or down one tile, then food is nearby
            bool bIsFoodNearby = (GetTileReference(characterPos.X,characterPos.Y - 1).Index >= 154 &&
                GetTileReference(characterPos.X, characterPos.Y - 1).Index <= 156) ||
                (GetTileReference(characterPos.X, characterPos.Y + 1).Index >= 154 &&
                GetTileReference(characterPos.X, characterPos.Y + 1).Index <= 156);
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
            bool bStairGoUp = smallMaps.DoStrairsGoUp(CurrentSmallMap.MapLocation, CurrentSmallMap.MapFloor, xy);
            return bStairGoUp;
        }

        /// <summary>
        /// Are the stairs at the given position going down?
        /// Be sure to check if they are stairs first
        /// </summary>
        /// <param name="xy"></param>
        /// <returns></returns>
        public bool IsStairsGoingDown(Point2D xy)
        {
            bool bStairGoUp = smallMaps.DoStairsGoDown(CurrentSmallMap.MapLocation, CurrentSmallMap.MapFloor, xy);
            return bStairGoUp;
        }

        /// <summary>
        /// Are the stairs at the player characters current position going down?
        /// </summary>
        /// <returns></returns>
        public bool IsStairGoingDown()
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
            throw new Exception("Can't get stair direction - something is amiss....");
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
                    nSpriteNum = bGoingUp ? tileReferences.GetTileReferenceByName("StairsNorth").Index
                        : tileReferences.GetTileReferenceByName("StairsSouth").Index;
                    break;
                case VirtualMap.Direction.Down:
                    nSpriteNum = bGoingUp ? tileReferences.GetTileReferenceByName("StairsSouth").Index
                        : tileReferences.GetTileReferenceByName("StairsNorth").Index;
                    break;
                case VirtualMap.Direction.Left:
                    nSpriteNum = bGoingUp ? tileReferences.GetTileReferenceByName("StairsWest").Index
                        : tileReferences.GetTileReferenceByName("StairsEast").Index;
                    break;
                case VirtualMap.Direction.Right:
                    nSpriteNum = bGoingUp ? tileReferences.GetTileReferenceByName("StairsEast").Index
                        : tileReferences.GetTileReferenceByName("StairsWest").Index;
                    break;
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

        /// <summary>
        /// Attempts to guess the tile underneath a thing that is upright such as a fountain
        /// </summary>
        /// <param name="xy">position of the thing</param>
        /// <returns>tile (sprite) number</returns>
        public int GuessTile(Point2D xy)
        {
            Dictionary<int, int> tileCountDictionary = new Dictionary<int, int>();
            for (int i = -1; i <= 1; i++)
            {
                for (int j = -1; j <= 1; j++)
                {
                    // if it is out of bounds then we skips them altogether
                    if (xy.X + i < 0 || xy.X + i >= NumberOfRowTiles || xy.Y + j < 0 || xy.Y + j >= NumberOfColumnTiles)
                        continue;
                    TileReference tileRef = GetTileReference(xy.X + i, xy.Y + j);
                    // only look at non-upright sprites
                    if (!tileRef.IsUpright)
                    {
                        int nTile = tileRef.Index;
                        if (tileCountDictionary.ContainsKey(nTile)) { tileCountDictionary[nTile] += 1; }
                        else { tileCountDictionary.Add(nTile, 1); }
                    }
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
            return nMostTile == -1?5:nMostTile;
        }
        #endregion

    }
}
