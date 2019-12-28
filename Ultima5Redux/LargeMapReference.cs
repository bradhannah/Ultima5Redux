using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;

namespace Ultima5Redux
{
    public class LargeMapReference
    {
        /// <summary>
        /// Maps the locatin to an actual 0,0 based map xy coord
        /// </summary>
        public Dictionary<SmallMapReferences.SingleMapReference.Location, Point2D> LocationXY { get; } = new Dictionary<SmallMapReferences.SingleMapReference.Location, Point2D>();
        public Dictionary<Point2D, SmallMapReferences.SingleMapReference.Location> LocationXYLocations { get; } = new Dictionary<Point2D, SmallMapReferences.SingleMapReference.Location>();

        private enum MasterFileOrder_LocationXY { Towns = 0, Dwellings, Castles, Keeps, Dungeons };
//        private enum MasterFileOrder_LocationXY { Towns = 0, Dwellings, Castles, Keeps, Dungeons };

        public SmallMapReferences.SingleMapReference.Location GetLocationByMapXY (Point2D mapXY)
        {
            return LocationXYLocations[mapXY];
        }


        public bool IsMapXYEnterable(Point2D mapXY)
        {
            return LocationXYLocations.ContainsKey(mapXY);
        }

        public LargeMapReference(DataOvlReference dataRef, SmallMapReferences smallMapRef)
        {

            // Load the location XYs and map them against the location
            List<byte> xLocs = dataRef.GetDataChunk(DataOvlReference.DataChunkName.LOCATIONS_X).GetAsByteList();
            List<byte> yLocs = dataRef.GetDataChunk(DataOvlReference.DataChunkName.LOCATIONS_Y).GetAsByteList();
            Debug.Assert(xLocs.Count == yLocs.Count);

            // Towns, Dwellings, Castles, Keeps, Dungeons

            //List<Point2D> vectors = new List<Point2D>();
            for (int nVector = 0; nVector < 0x28; nVector++)
            {
                Point2D mapPoint = new Point2D(xLocs[nVector], yLocs[nVector]);
                SmallMapReferences.SingleMapReference.Location location = (SmallMapReferences.SingleMapReference.Location)nVector + 1;
                //vectors.Add(new Point2D(xLocs[nVector], yLocs[nVector]));
                //                LocationXY.Add((SmallMapReferences.SingleMapReference.Location)nVector + 1, new Point2D(xLocs[nVector], yLocs[nVector]));
                LocationXY.Add(location, mapPoint);
                LocationXYLocations.Add(mapPoint, location);
            }
        }
    }
}
