using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace Ultima5Redux 
{
    class LargeMap : Map
    {
        private const int TilesPerChunkX = 16; // number of tiles horizontal in each chunk
        private const int TilesPerChunkY = 16; // number of tiles vertically in each chunk
        private const int TotalChunksPerX = 16; // total number of chunks horizontally
        private const int TotalChunksPerY = 16; // total number of chunks vertically
        private const int TotalChunks = 0x100; // total number of expected chunks in large maps
        private const int TilesPerMapRow = TilesPerChunkY * TotalChunksPerX; // total number of tiles per row in the large map 
        private const int TilesPerMapCol = TilesPerChunkX * TotalChunksPerX; // total number of tiles per column in the large map
        private const long DatOverlayBritMap = 0x3886; // address in data.ovl file for the Britannia map

        public enum Maps { Overworld, Underworld};

        /// <summary>
        /// Build a large map. There are essentially two choices - Overworld and Underworld
        /// </summary>
        /// <param name="u5Directory"></param>
        /// <param name="mapChoice"></param>
        public LargeMap (string u5Directory, Maps mapChoice) : base(u5Directory)
        {
            switch (mapChoice)
            {
                case Maps.Overworld:
                    theMap = BuildGenericMap(Path.Combine(u5Directory, FileConstants.BRIT_DAT), Path.Combine(u5Directory, FileConstants.DATA_OVL), false);
                    break;
                case Maps.Underworld:
                theMap = BuildGenericMap(Path.Combine(u5Directory, FileConstants.UNDER_DAT), "", true);
                    break;
            }
        }

        public void PrintMap()
        {
            Map.PrintMapSection(theMap, 0, 0, 160, 80);
        }

        /// <summary>
        /// Build a generic map - compatible with Britannia and Underworld
        /// </summary>
        /// <param name="mapDatFilename">Map data filename and path</param>
        /// <param name="overlayFilename">If present, the special overlay file for Britannia</param>
        /// <param name="ignoreOverlay">Do we ignore the overlay?</param>
        /// <returns></returns>
        static private byte[][] BuildGenericMap(String mapDatFilename, String overlayFilename, bool ignoreOverlay)
        {
            List<byte> theChunksSerial = Utils.GetFileAsByteList(mapDatFilename);

            byte[] dataOvlChunks = new byte[TotalChunks];
            BinaryReader dataOvl = null;

            // dirty little move - we just simply skip some code if ignoreOverlay is set
            if (!ignoreOverlay)
            {
                dataOvl = new BinaryReader(File.OpenRead(overlayFilename));

                dataOvl.BaseStream.Seek(DatOverlayBritMap, new SeekOrigin());
            }

            // declare the actual full map 4096*4096 tiles, with 255 (16*16) total chunks
            byte[][] theMap = Utils.Init2DByteArray(TilesPerMapRow, TilesPerMapCol);

            // counter for the serial chunks from brit.dat
            int britDatChunkCount = 0;

            // not relaly needed, because we read as we go... but here it is
            int chunkCount = 0;

            // these are the chunks describe in data.ovl
            for (int chunk = 0; chunk < TotalChunks; chunk++)
            {
                int col = chunk % TilesPerChunkX; // get the chunk column
                int row = chunk / TilesPerChunkY; // get the chunk row
                //System.Console.WriteLine("Row: " + row + "    Col: " + col + "   chunk: " + chunk);

                // get the overlay chunk value... to help determine if it is a water only tile
                // but if we are ignoring the overlay - then just give it zero, so the map will be processed without overlay considerations
                dataOvlChunks[chunkCount] = ignoreOverlay ? (byte)0x00 : dataOvl.ReadByte();

                // go through each row on the outer loop, becuase we want to read each horizon first
                for (int curRow = row * TilesPerChunkY; curRow < (row * TilesPerChunkY) + TilesPerChunkY; curRow++)
                {
                    //System.Console.WriteLine("CurRow : " + curRow);
                    // go through each horizon
                    for (int curCol = col * 16; curCol < (col * TilesPerChunkX) + TilesPerChunkX; curCol++)
                    {
                        if (dataOvlChunks[chunkCount] == 0xFF)
                        {
                            // welp, it's a water tile
                            theMap[curRow][curCol] = 0x01;
                        }
                        else
                        {
                            // it contains land tiles (look in brit.dat)
                            theMap[curRow][curCol] = theChunksSerial[britDatChunkCount++];
                        }
                    }
                }
                chunkCount++;
            }
            return theMap;
        }
    }
}
