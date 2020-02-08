using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;

namespace Ultima5Redux
{
    public class SmallMaps
    {
        private List<SmallMap> smallMaps = new List<SmallMap>();

        private Dictionary<SmallMapReferences.SingleMapReference.Location, Dictionary<int, SmallMap>> mapLocationDictionary =
            new Dictionary<SmallMapReferences.SingleMapReference.Location, Dictionary<int, SmallMap>>();

        private SmallMapReferences smallMapRef;
        private TileReferences spriteTileReferences;

        public SmallMaps(SmallMapReferences smallMapRef, string u5Directory, TileReferences spriteTileReferences)
        {
            this.smallMapRef = smallMapRef;
            this.spriteTileReferences = spriteTileReferences;
            foreach (SmallMapReferences.SingleMapReference mapRef in smallMapRef.MapReferenceList)
            {
                // now I can go through each and every reference
                SmallMap smallMap = new SmallMap(u5Directory, mapRef, spriteTileReferences);
                smallMaps.Add(smallMap);

                // we make a map that allows us to map the Location and Floor number to the small map with 
                // details such as the grid
                if (!mapLocationDictionary.ContainsKey(mapRef.MapLocation))
                {
                    mapLocationDictionary.Add(mapRef.MapLocation, new Dictionary<int, SmallMap>());
                }
                mapLocationDictionary[mapRef.MapLocation].Add(mapRef.Floor, smallMap);
            }
        }

        public SmallMap GetSmallMap(SmallMapReferences.SingleMapReference.Location location, int nFloor)
        {
            return mapLocationDictionary[location][nFloor];
        }

        public bool DoStairsGoDown(SmallMapReferences.SingleMapReference.Location location, int nFloor, Point2D tilePos)
        {
            return !DoStrairsGoUp(location, nFloor, tilePos);
        }

        public bool DoStrairsGoUp(SmallMapReferences.SingleMapReference.Location location, int nFloor, Point2D tilePos)
        {   
            //SmallMapReferences smallMapref = smallMapRef.Get(location, nFloor);
            SmallMap currentFloorSmallMap = mapLocationDictionary[location][nFloor];
            bool bHasLowerFloor = mapLocationDictionary[location].ContainsKey(nFloor - 1);
            bool bHasHigherFllor = mapLocationDictionary[location].ContainsKey(nFloor + 1);

            // is it a stair case?
            Debug.Assert(spriteTileReferences.IsStaircase(currentFloorSmallMap.TheMap[tilePos.X][tilePos.Y]));
            // is it the bottom or top floor? if so, then we know
            if (!bHasLowerFloor) return true;
            if (!bHasHigherFllor) return false;
            
            // is there a stair case on the lower floor?
            if (spriteTileReferences.IsStaircase(mapLocationDictionary[location][nFloor-1].TheMap[tilePos.X][tilePos.Y]))
            {
                return false;
            }
            // is there a stair case on the upper floor?
            if (spriteTileReferences.IsStaircase(mapLocationDictionary[location][nFloor + 1].TheMap[tilePos.X][tilePos.Y]))
            {
                return true;
            }
            // if not - then WTF?
            throw new Ultima5ReduxException("There is staircase with apparently no matching stair case");
        }
    }
}
