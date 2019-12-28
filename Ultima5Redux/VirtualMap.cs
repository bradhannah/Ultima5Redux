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

        public SmallMap CurrentSmallMap { get; private set; }
        public LargeMap CurrentLargeMap { get; private set; }
        public SmallMapReferences.SingleMapReference CurrentSingleMapReference { get; private set; }
        public SmallMapReferences SmallMapRefs { get; private set; }

        public bool IsLargeMap { get; private set; } = false;
        public LargeMap.Maps LargeMapOverUnder { get; private set; } = (LargeMap.Maps)(-1);

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

        public TileReference GetTileReference(int x, int y)
        {
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

        public void PickUpThing(Point2D xy)
        {
            overrideMap[xy.X][xy.Y] = tileReferences.GetTileNumberByName("BrickFloor");// 68;
        }

        public void SetCharacterPosition(Point2D xy)
        {
            CurrentPosition = xy;
        }

        #region Public Actions Methods
        // public void UseStairs()
        //{
        //    int nCurrentFloor = CurrentSmallMap.MapFloor;
        //    bool bStairGoUp = smallMaps.DoStrairsGoUp(CurrentSingleMapReference.MapLocation,
        //        nCurrentFloor, CurrentPosition);

        //    LoadSmallMap(currentSingleSmallMapReferences.MapLocation, nCurrentFloor + (bStairGoUp ? 1 : -1), Ultima3DWorldSmallMap.CharacterPositionOnMap, Camera.main);
        //    //LoadSmallMap(currentSingleSmallMapReferences.MapLocation, nCurrentFloor + (bStairGoUp ? 1 : -1), Ultima3DWorldSmallMap.CharacterPositionOnMap, Camera.main);
        //}
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
            //UltimaGlobal.currentSmallMap.TheMap[characterPos.X][characterPos.Y - 1] <= 156) ||
            //(UltimaGlobal.currentSmallMap.TheMap[characterPos.X][characterPos.Y + 1] >= 154 &&
            //UltimaGlobal.currentSmallMap.TheMap[characterPos.X][characterPos.Y + 1] <= 156);
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
            //bool bStairGoUp = UltimaGlobal.Ultima5World.AllSmallMaps.DoStrairsGoUp(UltimaGlobal.currentSmallMap.MapLocation, currentSmallMap.MapFloor,
            //      Vector2IntToPoint2D(Ultima3DWorldSmallMap.CharacterPositionOnMap));
            return IsStairGoingUp(CurrentPosition);
        }
        #endregion

    }
}
