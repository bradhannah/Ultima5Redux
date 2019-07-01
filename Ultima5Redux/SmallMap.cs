using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;


namespace Ultima5Redux
{
    class SmallMap : Map
    {
        public const int XTILES = 32;
        public const int YTILES = 32;

        private SmallMapReference.SingleMapReference MapRef;

        /// <summary>
        /// Creates a small map object using a pre-defined map reference
        /// </summary>
        /// <param name="u5Directory"></param>
        /// <param name="mapRef"></param>
        public SmallMap(string u5Directory, SmallMapReference.SingleMapReference mapRef) : base(u5Directory)
        {
            MapRef = mapRef;

            theMap = LoadSmallMapFile(Path.Combine(u5Directory, mapRef.MapFilename), mapRef.FileOffset);
        }

        /// <summary>
        /// Loads a small map into a 2D array
        /// </summary>
        /// <param name="mapFilename">name of the file that contains the map</param>
        /// <param name="fileOffset">the file offset to begin reading the file at</param>
        /// <returns></returns>
        private static byte[][] LoadSmallMapFile(string mapFilename, int fileOffset)
        {
            List<byte> mapBytes = Utils.GetFileAsByteList(mapFilename);

            byte[][] smallMap = Utils.ByteListTo2DArray(mapBytes, XTILES, fileOffset, XTILES * YTILES);

            return smallMap;
        }
    }
}
