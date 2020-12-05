using System;
using System.Collections.Generic;
using System.IO;

namespace Ultima5Redux.Maps
{
    public class LargeMap : Map
    {
        public enum Maps { Small = -1, Overworld, Underworld }

        private const int TILES_PER_CHUNK_X = 16; // number of tiles horizontal in each chunk
        private const int TILES_PER_CHUNK_Y = 16; // number of tiles vertically in each chunk
        private const int TOTAL_CHUNKS_PER_X = 16; // total number of chunks horizontally
        private const int TOTAL_CHUNKS_PER_Y = 16; // total number of chunks vertically
        private const int TOTAL_CHUNKS = 0x100; // total number of expected chunks in large maps
        private const long DAT_OVERLAY_BRIT_MAP = 0x3886; // address in data.ovl file for the Britannia map

        public override byte[][] TheMap { get; protected set; }

        // ReSharper disable once MemberCanBePrivate.Global
        public static int
            YTILES => TILES_PER_CHUNK_Y * TOTAL_CHUNKS_PER_Y; // total number of tiles per row in the large map 

        // ReSharper disable once MemberCanBePrivate.Global
        public static int 
            XTILES => TILES_PER_CHUNK_X * TOTAL_CHUNKS_PER_X; // total number of tiles per column in the large map

        public override int NumOfXTiles => XTILES;
        public override int NumOfYTiles => YTILES;

        /// <summary>
        ///     Build a large map. There are essentially two choices - Overworld and Underworld
        /// </summary>
        /// <param name="u5Directory"></param>
        /// <param name="mapChoice"></param>
        /// <param name="tileOverrides"></param>
        public LargeMap(string u5Directory, Maps mapChoice, TileOverrides tileOverrides) : base(tileOverrides,
            SmallMapReferences.SingleMapReference.GetLargeMapSingleInstance(mapChoice))
        {
            switch (mapChoice)
            {
                case Maps.Overworld:
                    TheMap = BuildGenericMap(Path.Combine(u5Directory, FileConstants.BRIT_DAT),
                        Path.Combine(u5Directory, FileConstants.DATA_OVL), false);
                    break;
                case Maps.Underworld:
                    TheMap = BuildGenericMap(Path.Combine(u5Directory, FileConstants.UNDER_DAT), "", true);
                    break;
                case Maps.Small:
                    throw new Ultima5ReduxException("tried to create a LargeMap with the .Small map enum");
                default:
                    throw new ArgumentOutOfRangeException(nameof(mapChoice), mapChoice, null);
            }
        }

        // ReSharper disable once UnusedMember.Global
        public void PrintMap()
        {
            PrintMapSection(TheMap, 0, 0, 160, 80);
        }

        /// <summary>
        ///     Build a generic map - compatible with Britannia and Underworld
        /// </summary>
        /// <param name="mapDatFilename">Map data filename and path</param>
        /// <param name="overlayFilename">If present, the special overlay file for Britannia</param>
        /// <param name="ignoreOverlay">Do we ignore the overlay?</param>
        /// <returns></returns>
        private static byte[][] BuildGenericMap(string mapDatFilename, string overlayFilename, bool ignoreOverlay)
        {
            List<byte> theChunksSerial = Utils.GetFileAsByteList(mapDatFilename);

            byte[] dataOvlChunks = new byte[TOTAL_CHUNKS];
            BinaryReader dataOvl = null;

            // dirty little move - we just simply skip some code if ignoreOverlay is set
            if (!ignoreOverlay)
            {
                dataOvl = new BinaryReader(File.OpenRead(overlayFilename));

                dataOvl.BaseStream.Seek(DAT_OVERLAY_BRIT_MAP, new SeekOrigin());
            }

            // declare the actual full map 4096*4096 tiles, with 255 (16*16) total chunks
            byte[][] theMap = Utils.Init2DByteArray(YTILES, XTILES);

            // counter for the serial chunks from brit.dat
            int britDatChunkCount = 0;

            // not really needed, because we read as we go... but here it is
            int chunkCount = 0;

            // these are the chunks describe in data.ovl
            for (int chunk = 0; chunk < TOTAL_CHUNKS; chunk++)
            {
                int col = chunk % TILES_PER_CHUNK_X; // get the chunk column
                int row = chunk / TILES_PER_CHUNK_Y; // get the chunk row
                //System.Console.WriteLine("Row: " + row + "    Col: " + col + "   chunk: " + chunk);

                // get the overlay chunk value... to help determine if it is a water only tile
                // but if we are ignoring the overlay - then just give it zero, so the map will be processed without overlay considerations
                dataOvlChunks[chunkCount] = ignoreOverlay ? (byte) 0x00 : dataOvl.ReadByte();

                // go through each row on the outer loop, because we want to read each horizon first
                for (int curRow = row * TILES_PER_CHUNK_Y;
                    curRow < row * TILES_PER_CHUNK_Y + TILES_PER_CHUNK_Y;
                    curRow++)
                {
                    //System.Console.WriteLine("CurRow : " + curRow);
                    // go through each horizon
                    for (int curCol = col * 16; curCol < col * TILES_PER_CHUNK_X + TILES_PER_CHUNK_X; curCol++)
                    {
                        if (dataOvlChunks[chunkCount] == 0xFF)
                            // welp, it's a water tile
                            theMap[curCol][curRow] = 0x01;
                        else
                            // it contains land tiles (look in brit.dat)
                            theMap[curCol][curRow] = theChunksSerial[britDatChunkCount++];
                    }
                }

                chunkCount++;
            }

            return theMap;
        }

        protected override float GetAStarWeight(TileReferences spriteTileReferences, Point2D xy)
        {
            return 1;
        }
        
   }
}