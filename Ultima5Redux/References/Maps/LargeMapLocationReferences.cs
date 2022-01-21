﻿using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using Ultima5Redux.Maps;

namespace Ultima5Redux.References.Maps
{
    public class LargeMapLocationReferences
    {
        private const int N_TOTAL_LOCATIONS = 0x28;

        /// <summary>
        ///     Maps the xy based on the location
        /// </summary>
        private Dictionary<Point2D, SmallMapReferences.SingleMapReference.Location> LocationXYLocations { get; } =
            new();

        /// <summary>
        ///     Maps the location to an actual 0,0 based map xy coordinates
        /// </summary>
        [SuppressMessage("ReSharper", "MemberCanBePrivate.Global")]
        // ReSharper disable once CollectionNeverQueried.Global
        public Dictionary<SmallMapReferences.SingleMapReference.Location, Point2D> LocationXY { get; } =
            new();

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
                Point2D mapPoint = new(xPos[nVector], yPos[nVector]);
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

        private const int TOTAL_CHUNKS = 0x100; // total number of expected chunks in large maps
        private const long DAT_OVERLAY_BRIT_MAP = 0x3886; // address in data.ovl file for the Britannia map

        public const int
            XTiles = TILES_PER_CHUNK_X * TOTAL_CHUNKS_PER_X; // total number of tiles per column in the large map

        public const int
            YTiles = TILES_PER_CHUNK_Y * TOTAL_CHUNKS_PER_Y; // total number of tiles per row in the large map 

        private const int TILES_PER_CHUNK_X = 16; // number of tiles horizontal in each chunk
        private const int TILES_PER_CHUNK_Y = 16; // number of tiles vertically in each chunk
        private const int TOTAL_CHUNKS_PER_X = 16; // total number of chunks horizontally
        private const int TOTAL_CHUNKS_PER_Y = 16; // total number of chunks vertically

        public byte[][] GetMap(Map.Maps map)
        {
            switch (map)
            {
                case Map.Maps.Overworld:
                    return BuildGenericMap(
                        Path.Combine(GameReferences.DataOvlRef.DataDirectory, FileConstants.BRIT_DAT),
                        Path.Combine(GameReferences.DataOvlRef.DataDirectory, FileConstants.DATA_OVL), false);
                case Map.Maps.Underworld:
                    return BuildGenericMap(
                        Path.Combine(GameReferences.DataOvlRef.DataDirectory, FileConstants.UNDER_DAT), "", true);
                case Map.Maps.Small:
                case Map.Maps.Combat:
                default:
                    throw new Ultima5ReduxException($"Tried to get a large map with: {map}");
            }
        }

        /// <summary>
        ///     Build a generic map - compatible with Britannia and Underworld
        /// </summary>
        /// <param name="mapDatFilename">Map data filename and path</param>
        /// <param name="overlayFilename">If present, the special overlay file for Britannia</param>
        /// <param name="ignoreOverlay">Do we ignore the overlay?</param>
        /// <returns></returns>
        internal static byte[][] BuildGenericMap(string mapDatFilename, string overlayFilename, bool ignoreOverlay)
        {
            List<byte> theChunksSerial = Utils.GetFileAsByteList(mapDatFilename);

            byte[] dataOvlChunks = new byte[TOTAL_CHUNKS];
            BinaryReader dataOvl = null;

            // dirty little move - we just simply skip some code if ignoreOverlay is set
            if (!ignoreOverlay)
            {
                dataOvl = new BinaryReader(File.OpenRead(Utils.GetFirstFileAndPathCaseInsensitive(overlayFilename)));

                dataOvl.BaseStream.Seek(DAT_OVERLAY_BRIT_MAP, new SeekOrigin());
            }

            // declare the actual full map 4096*4096 tiles, with 255 (16*16) total chunks
            byte[][] theMap = Utils.Init2DByteArray(YTiles, XTiles);

            // counter for the serial chunks from brit.dat
            int britDatChunkCount = 0;

            // not really needed, because we read as we go... but here it is
            int chunkCount = 0;

            // these are the chunks describe in data.ovl
            for (int chunk = 0; chunk < TOTAL_CHUNKS; chunk++)
            {
                int col = chunk % TILES_PER_CHUNK_X; // get the chunk column
                int row = chunk / TILES_PER_CHUNK_Y; // get the chunk row

                // get the overlay chunk value... to help determine if it is a water only tile
                // but if we are ignoring the overlay - then just give it zero, so the map will be processed without overlay considerations
                dataOvlChunks[chunkCount] = ignoreOverlay ? (byte)0x00 : dataOvl.ReadByte();

                // go through each row on the outer loop, because we want to read each horizon first
                for (int curRow = row * TILES_PER_CHUNK_Y;
                     curRow < row * TILES_PER_CHUNK_Y + TILES_PER_CHUNK_Y;
                     curRow++)
                {
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
    }
}