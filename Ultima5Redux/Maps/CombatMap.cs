using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;

namespace Ultima5Redux.Maps
{
    public class CombatMap : Map
    {
        //private const int MAX_X_COLS = 11, MAX_Y_ROWS = 11;

        public static int XTILES => 11;
        public static int YTILES => 11;
        public override byte[][] TheMap { get; protected set; }

        public override int NumOfXTiles => XTILES;
        public override int NumOfYTiles => YTILES;

        private const int ROW_BYTES = 32;
        // https://github.com/andrewschultz/rpg-mapping-tools/wiki/Ultima-V-Dungeon-File-Format

        //  Dungeon.cbt's format is different from U4. It has a 32x11 array for each room. There are 112 rooms.

        //11x11 leftmost is reserved for the room it's own icons.
        //11,0 to 18,0 is the replacement tiles used by triggers.Only 8 tiles are allowed, but up to 16 locations may be replaced by re-using tiles.
        //11,1 to 22,1 is the x, then y location of your party members when entering from the north.Avatar = 11 & 17, #2 = 12 & 18, etc.
        //11,2 to 22,2 is for entering from the east.
        //11,3 to 22,3 is for entering from the south.
        //11,4 to 22,4 is for entering from the west.
        //11,5 to 26,5 tells what monsters are present.
        //11,6 to 26,6 gives their x-coordinates.
        //11,7 to 26,7 gives their y-coordinates.
        //11,8 to 26,8 is the x, then y location of the room's triggers. Up to 8 triggers.
        //11,9 to 26,9 is the x, then y coordinate of the first location replaced by the respective tile from 11,0-18,0.
        //11,10 to 26,10 is the x, then y coordinate of the second location replaced by the respective tile from 11,0-18,0.

        private readonly CombatMapReference.SingleCombatMapReference _mapRef;

        private readonly List<MapPlayerPosRow>
            _row1To4Player = new List<MapPlayerPosRow>(4); // row 1 = east, row 2 = west, row 3 = south, row 4 = north

        // These are the legacy structures read in from the file. They will need to be abstracted into useable data.
        private MapRow _row0; // Information contains the new tiles once a trigger happens  
        private MapRow _row10NewTilesY; // row 10: position Y of the new tiles
        private MapMonsterTileRow _row5Monster;
        private MapMonsterXRow _row6MonsterX;
        private MapMonsterYRow _row7MonsterY;
        private MapRow _row8Trigger; // row 8: positions of triggers, one position hits to new tiles
        private MapRow _row9NewTilesX; // row 9: position X of the new tiles

        /// <summary>
        ///     Create the individual Combat Map object
        /// </summary>
        /// <param name="u5Directory">Directory of data files</param>
        /// <param name="mapRef">specific combat map reference</param>
        /// <param name="tileOverrides"></param>
        public CombatMap(string u5Directory, CombatMapReference.SingleCombatMapReference mapRef,
            TileOverrides tileOverrides) : base(tileOverrides, null)
        {
            string dataFilenameAndPath = Path.Combine(u5Directory, mapRef.MapFilename);

            _mapRef = mapRef;

            // TOOD: reads in the data from the file - should read file into memory once and leave it there so quicker reference
            List<byte> mapList = Utils.GetFileAsByteList(dataFilenameAndPath, mapRef.FileOffset,
                CombatMapReference.SingleCombatMapReference.MAP_BYTE_COUNT);

            TheMap = ReadCombatBytesIntoByteArray(mapList);

            // initialize the legacy structures that describe the map details
            InitializeCombatMapStructures(dataFilenameAndPath, mapRef.FileOffset);
        }

        public string Description => _mapRef.Description;

        /// <summary>
        ///     Special function to read the map data only, putting it into a 2D array
        /// </summary>
        /// <param name="byteList">the raw data</param>
        /// <returns>2D byte map</returns>
        private byte[][] ReadCombatBytesIntoByteArray(List<byte> byteList)
        {
            byte[][] combatMap = Utils.Init2DByteArray(XTILES, YTILES);

            for (int i = 0; i < XTILES; i++)
            {
                byteList.CopyTo(i * ROW_BYTES, combatMap[i], 0, XTILES);
            }

            return combatMap;
        }

        private void InitializeCombatMapStructures(string dataFilenameAndPath, int fileOffset)
        {
            FileStream fs = File.OpenRead(dataFilenameAndPath);
            fs.Seek(fileOffset, SeekOrigin.Begin);

            _row0 = (MapRow) Utils.ReadStruct(fs, typeof(MapRow));
            _row1To4Player.Add((MapPlayerPosRow) Utils.ReadStruct(fs, typeof(MapPlayerPosRow)));
            _row1To4Player.Add((MapPlayerPosRow) Utils.ReadStruct(fs, typeof(MapPlayerPosRow)));
            _row1To4Player.Add((MapPlayerPosRow) Utils.ReadStruct(fs, typeof(MapPlayerPosRow)));
            _row1To4Player.Add((MapPlayerPosRow) Utils.ReadStruct(fs, typeof(MapPlayerPosRow)));
            _row5Monster = (MapMonsterTileRow) Utils.ReadStruct(fs, typeof(MapMonsterTileRow));
            _row6MonsterX = (MapMonsterXRow) Utils.ReadStruct(fs, typeof(MapMonsterXRow));
            _row7MonsterY = (MapMonsterYRow) Utils.ReadStruct(fs, typeof(MapMonsterYRow));
            _row8Trigger = (MapRow) Utils.ReadStruct(fs, typeof(MapRow));
            _row9NewTilesX = (MapRow) Utils.ReadStruct(fs, typeof(MapRow));
            _row10NewTilesY = (MapRow) Utils.ReadStruct(fs, typeof(MapRow));
            fs.Close();

            Console.WriteLine("");
        }

        protected override float GetAStarWeight(TileReferences spriteTileReferences, Point2D xy)
        {
            return 1;
        }
        

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        private unsafe struct MapRow
        {
            private fixed byte tiles[11];
            private fixed byte newTiles[8];
            private fixed byte zeroes[13];
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        private unsafe struct MapPlayerPosRow
        {
            private fixed byte tiles[11];
            private fixed byte initial_X[6]; // initial x position of each party member
            private fixed byte initial_Y[6]; // initial y position of each party member
            private fixed byte zeroes[9];
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        private unsafe struct MapMonsterTileRow
        {
            private fixed byte tiles[11];
            private fixed byte monster_tiles[16]; // tile for each monster
            private fixed byte zeroes[5];
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        private unsafe struct MapMonsterXRow
        {
            private fixed byte tiles[11];
            private fixed byte initial_X[16]; // initial x position of each monster
            private fixed byte zeroes[5];
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        private unsafe struct MapMonsterYRow
        {
            private fixed byte tiles[11];
            private fixed byte initial_Y[16]; // initial y position of each monster
            private fixed byte zeroes[5];
        }
    }
}