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
        private LargeMapReference largeMapReferences;
        private NonPlayerCharacters nonPlayerCharacters;
        private SmallMaps smallMaps;
        private Dictionary<LargeMap.Maps, LargeMap> largeMaps = new Dictionary<LargeMap.Maps, LargeMap>(2);
        private TileReferences tileReferences;
        public enum Direction { Up, Down, Left, Right };

        // override map is responsible for overriding tiles that would otherwise be static
        private int[][] overrideMap;

        private Point2D _currentPosition = new Point2D(0, 0);
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

        public int NumberOfColumnTiles { get { return overrideMap[0].Length; } }
        public int NumberOfRowTiles { get { return overrideMap.Length; } }


        public SmallMap CurrentSmallMap { get; private set; }
        public LargeMap CurrentLargeMap { get; private set; }
        public SmallMapReferences.SingleMapReference CurrentSingleMapReference { get; private set; }
        public SmallMapReferences SmallMapRefs { get; private set; }

        public bool IsLargeMap { get; private set; } = false;
        public LargeMap.Maps LargeMapOverUnder { get; private set; } = (LargeMap.Maps)(-1);

        #region Constructor, Initializers and Loaders
        public VirtualMap(SmallMapReferences smallMapReferences, SmallMaps smallMaps, LargeMapReference largeMapReferences, 
            LargeMap overworldMap, LargeMap underworldMap, NonPlayerCharacters nonPlayerCharacters, TileReferences tileReferences)
        {
            this.SmallMapRefs = smallMapReferences;
            this.smallMaps = smallMaps;
            this.nonPlayerCharacters = nonPlayerCharacters;
            this.largeMapReferences = largeMapReferences;
            this.tileReferences = tileReferences;
            largeMaps.Add(LargeMap.Maps.Overworld, overworldMap);
            largeMaps.Add(LargeMap.Maps.Underworld, underworldMap);
                //new LargeMap[] { overworldMap, underworldMap };
        }

        public void LoadSmallMap(SmallMapReferences.SingleMapReference singleMapReference)
        {
            CurrentSingleMapReference = singleMapReference;
            CurrentSmallMap = smallMaps.GetSmallMap(singleMapReference.MapLocation, singleMapReference.Floor);
            overrideMap = Utils.Init2DArray<int>(CurrentSmallMap.TheMap[0].Length, CurrentSmallMap.TheMap.Length);
            IsLargeMap = false;
            LargeMapOverUnder = (LargeMap.Maps)(-1);
        }

        public void LoadLargeMap(SmallMapReferences.SingleMapReference singleMapReference)
        {
            Debug.Assert(singleMapReference.MapLocation == SmallMapReferences.SingleMapReference.Location.Britainnia_Underworld);

            LoadLargeMap(singleMapReference.Floor == 0 ? LargeMap.Maps.Overworld : LargeMap.Maps.Underworld);
        }

        public void LoadLargeMap(LargeMap.Maps map)
        {
            int nFloor = map == LargeMap.Maps.Overworld ? 0 : -1;
            CurrentSingleMapReference = null;//SmallMapRefs.GetSingleMapByLocation(SmallMapReferences.SingleMapReference.Location.Britainnia_Underworld, nFloor);
            CurrentLargeMap = largeMaps[map];
            overrideMap = Utils.Init2DArray<int>(CurrentLargeMap.TheMap[0].Length, CurrentLargeMap.TheMap.Length);
            IsLargeMap = true;
            LargeMapOverUnder = map;
        }
        #endregion

        #region Tile references and character positioning
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

        public TileReference GetTileReference(Point2D xy)
        {
            return GetTileReference(xy.X, xy.Y);
        }

        public void SetOverridingTileReferece(TileReference tileReference, Point2D xy)
        {
            SetOverridingTileReferece(tileReference, xy.X, xy.Y);
        }

        public void SetOverridingTileReferece(TileReference tileReference, int x, int y)
        {
            overrideMap[x][y] = tileReference.Index;
        }

        public void SetCharacterPosition(Point2D xy)
        {
            CurrentPosition = xy;
        }
        #endregion

        public NonPlayerCharacters.NonPlayerCharacter GetNPCOnTile(Point2D point2D)
        {
            SmallMapReferences.SingleMapReference.Location location = CurrentSingleMapReference.MapLocation;
            List<NonPlayerCharacters.NonPlayerCharacter> npcs = nonPlayerCharacters.GetNonPlayerCharactersByLocation(location);
            foreach (NonPlayerCharacters.NonPlayerCharacter npc in npcs)
            {
                int nIndex = 1;
                Point2D npcXy = npc.Schedule.GetHardCoord(nIndex);

                // the NPC is a non-NPC, so we keep looking
                if (npcXy.X == 0 && npcXy.Y == 0) continue;

                // we found the right NPC and are they on the correct floor
                if (npcXy == point2D && CurrentSingleMapReference.Floor == npc.Schedule.Coords[nIndex].Z)
                {
                    return npc;
                }
            }
            return null;
        }

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

        #region Public Boolean Properties/Method
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

        public bool IsStairGoingUp(Point2D characterPos)
        {
            bool bStairGoUp = smallMaps.DoStrairsGoUp(CurrentSmallMap.MapLocation, CurrentSmallMap.MapFloor,
                  characterPos);
            return bStairGoUp;
        }

        public bool IsStairsGoingDown(Point2D characterPos)
        {
            bool bStairGoUp = smallMaps.DoStairsGoDown(CurrentSmallMap.MapLocation, CurrentSmallMap.MapFloor,
                  characterPos);
            return bStairGoUp;
        }

        public bool IsStairGoingDown()
        {
            return IsStairsGoingDown(CurrentPosition);
        }

        public bool IsStairGoingUp()
        {
            return IsStairGoingUp(CurrentPosition);
        }

        public Direction GetStairsDirection(Point2D stairsXY)
        {
            // we are making a BIG assumption at this time that a stair case ONLY ever has a single
            // entrance point, and solid walls on all other sides... hopefully this is true
            if (!GetTileReference(stairsXY.X - 1, stairsXY.Y).IsSolidSprite) return Direction.Left;
            if (!GetTileReference(stairsXY.X + 1, stairsXY.Y).IsSolidSprite) return Direction.Right;
            if (!GetTileReference(stairsXY.X, stairsXY.Y - 1).IsSolidSprite) return Direction.Up;
            if (!GetTileReference(stairsXY.X, stairsXY.Y + 1).IsSolidSprite) return Direction.Down;
            throw new Exception("Can't get stair direction - something is amiss....");
        }

        public bool IsHorizDoor(Point2D doorXY)
        {
            if ((GetTileReference(doorXY.X - 1, doorXY.Y).IsSolidSpriteButNotDoor
                || GetTileReference(doorXY.X + 1, doorXY.Y).IsSolidSpriteButNotDoor))
                return true;
            return false;
        }

        public bool IsNPCTile(Point2D point2D)
        {
            // this method isnt super efficient, may want to optimize in the future
            if (IsLargeMap) return false;
            return (GetNPCOnTile(point2D) != null);
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
