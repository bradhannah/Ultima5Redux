using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using Ultima5Redux.Data;

namespace Ultima5Redux.Maps
{
    public class LargeMapLocationReferences
    {
        private const int N_TOTAL_LOCATIONS = 0x28;

        /// <summary>
        ///     Maps the location to an actual 0,0 based map xy coordinates
        /// </summary>
        [SuppressMessage("ReSharper", "MemberCanBePrivate.Global")]
        // ReSharper disable once CollectionNeverQueried.Global
        public Dictionary<SmallMapReferences.SingleMapReference.Location, Point2D> LocationXY { get; } =
            new Dictionary<SmallMapReferences.SingleMapReference.Location, Point2D>();

        /// <summary>
        ///     Maps the xy based on the location
        /// </summary>
        private Dictionary<Point2D, SmallMapReferences.SingleMapReference.Location> LocationXYLocations { get; } =
            new Dictionary<Point2D, SmallMapReferences.SingleMapReference.Location>();

        /// <summary>
        ///     Constructor building xy table
        /// </summary>
        /// <param name="dataRef">data ovl reference for extracting xy coordinates</param>
        public LargeMapLocationReferences(DataOvlReference dataRef)
        {
            // Load the location XYs and map them against the location
            List<byte> xPos = dataRef.GetDataChunk(DataOvlReference.DataChunkName.LOCATIONS_X).GetAsByteList();
            List<byte> yPos = dataRef.GetDataChunk(DataOvlReference.DataChunkName.LOCATIONS_Y).GetAsByteList();
            Debug.Assert(xPos.Count == yPos.Count);

            for (int nVector = 0; nVector < N_TOTAL_LOCATIONS; nVector++)
            {
                Point2D mapPoint = new Point2D(xPos[nVector], yPos[nVector]);
                SmallMapReferences.SingleMapReference.Location location =
                    (SmallMapReferences.SingleMapReference.Location)nVector + 1;
                LocationXY.Add(location, mapPoint);
                LocationXYLocations.Add(mapPoint, location);
            }
        }

        /// <summary>
        ///     Gets the location at a particular xy
        /// </summary>
        /// <param name="mapXY"></param>
        /// <returns></returns>
        public SmallMapReferences.SingleMapReference.Location GetLocationByMapXY(Point2D mapXY)
        {
            return LocationXYLocations[mapXY];
        }

        /// <summary>
        ///     Tells you if an xy is enterable (command key E)
        /// </summary>
        /// <param name="mapXY"></param>
        /// <returns>true if it's enterable</returns>
        public bool IsMapXYEnterable(Point2D mapXY)
        {
            return LocationXYLocations.ContainsKey(mapXY);
        }
    }
}