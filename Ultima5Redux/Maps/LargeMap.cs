using System;
using System.Collections.Generic;
using System.IO;
using Ultima5Redux.MapUnits;
using Ultima5Redux.MapUnits.CombatMapUnits;
using Ultima5Redux.PlayerCharacters;

namespace Ultima5Redux.Maps
{
    public sealed class LargeMap : RegularMap
    {
        private const long DAT_OVERLAY_BRIT_MAP = 0x3886; // address in data.ovl file for the Britannia map
        private const int TILES_PER_CHUNK_X = 16; // number of tiles horizontal in each chunk
        private const int TILES_PER_CHUNK_Y = 16; // number of tiles vertically in each chunk
        private const int TOTAL_CHUNKS = 0x100; // total number of expected chunks in large maps
        private const int TOTAL_CHUNKS_PER_X = 16; // total number of chunks horizontally
        private const int TOTAL_CHUNKS_PER_Y = 16; // total number of chunks vertically
        private Point2D _bottomRightExtent;

        private Point2D _topLeftExtent;

        // ReSharper disable once MemberCanBePrivate.Global
        public static int
            XTiles => TILES_PER_CHUNK_X * TOTAL_CHUNKS_PER_X; // total number of tiles per column in the large map

        // ReSharper disable once MemberCanBePrivate.Global
        public static int
            YTiles => TILES_PER_CHUNK_Y * TOTAL_CHUNKS_PER_Y; // total number of tiles per row in the large map 

        protected override bool IsRepeatingMap => true;

        public override int NumOfXTiles => XTiles;
        public override int NumOfYTiles => YTiles;

        public override bool ShowOuterSmallMapTiles => false;

        public override byte[][] TheMap { get; protected set; }

        /// <summary>
        ///     Build a large map. There are essentially two choices - Overworld and Underworld
        /// </summary>
        /// <param name="u5Directory"></param>
        /// <param name="mapChoice"></param>
        public LargeMap(string u5Directory, Maps mapChoice) : base(
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

            InitializeAStarMap(WalkableType.StandardWalking);
            InitializeAStarMap(WalkableType.CombatWater);
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
                //System.Console.WriteLine("Row: " + row + "    Col: " + col + "   chunk: " + chunk);

                // get the overlay chunk value... to help determine if it is a water only tile
                // but if we are ignoring the overlay - then just give it zero, so the map will be processed without overlay considerations
                dataOvlChunks[chunkCount] = ignoreOverlay ? (byte)0x00 : dataOvl.ReadByte();

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

        /// <summary>
        ///     Gets a positive based Point2D for LargeMaps - it was return null if it outside of the
        ///     current extends
        /// </summary>
        /// <param name="direction"></param>
        /// <param name="xy"></param>
        /// <returns></returns>
        protected override Point2D GetAdjustedPos(Point2D.Direction direction, Point2D xy)
        {
            int nPositiveX = xy.X + NumOfXTiles;
            int nPositiveY = xy.Y + NumOfYTiles;

            if (nPositiveX <= _topLeftExtent.X + NumOfXTiles || xy.X >= _bottomRightExtent.X)
                return null;
            if (nPositiveY <= _topLeftExtent.Y + NumOfYTiles || xy.Y >= _bottomRightExtent.Y)
                return null;

            return xy.GetAdjustedPosition(direction);
        }

        protected override float GetAStarWeight(Point2D xy)
        {
            return 1;
        }

        protected override WalkableType GetWalkableTypeByMapUnit(MapUnit mapUnit)
        {
            switch (mapUnit)
            {
                case Enemy enemy:
                    return enemy.EnemyReference.IsWaterEnemy ? WalkableType.CombatWater : WalkableType.StandardWalking;
                case CombatPlayer _:
                    return WalkableType.StandardWalking;
                default:
                    return WalkableType.StandardWalking;
            }
        }

        public override void RecalculateVisibleTiles(Point2D initialFloodFillPosition)
        {
            if (XRayMode)
            {
                VisibleOnMap = Utils.Init2DBoolArray(NumOfXTiles, NumOfYTiles, true);
                return;
            }

            NVisibleLargeMapTiles = VisibleInEachDirectionOfAvatar * 2 + 1;

            VisibleOnMap = Utils.Init2DBoolArray(NumOfXTiles, NumOfYTiles);
            //TestForVisibility = Utils.Init2DBoolArray(NumOfXTiles, NumOfYTiles);
            TestForVisibility = new List<bool[][]>();
            // reinitialize the array for all potential party members
            for (int i = 0; i < PlayerCharacterRecords.MAX_PARTY_MEMBERS; i++)
            {
                TestForVisibility.Add(Utils.Init2DBoolArray(NumOfXTiles, NumOfYTiles));
            }

            TouchedOuterBorder = false;

            AvatarXyPos = initialFloodFillPosition;

            _topLeftExtent = new Point2D(AvatarXyPos.X - VisibleInEachDirectionOfAvatar,
                AvatarXyPos.Y - VisibleInEachDirectionOfAvatar);
            _bottomRightExtent = new Point2D(AvatarXyPos.X + VisibleInEachDirectionOfAvatar,
                AvatarXyPos.Y + VisibleInEachDirectionOfAvatar);

            FloodFillMap(AvatarXyPos, true);
        }


        // ReSharper disable once UnusedMember.Global
        public void PrintMap()
        {
            PrintMapSection(TheMap, 0, 0, 160, 80);
        }
    }
}