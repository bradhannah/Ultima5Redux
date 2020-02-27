using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Runtime.InteropServices;

namespace Ultima5Redux
{
    public class CombatMap : Map
    {
        // https://github.com/andrewschultz/rpg-mapping-tools/wiki/Ultima-V-Dungeon-File-Format

        //  Dungeon.cbt's format is different from U4. It has a 32x11 array for each room. There are 112 rooms.

        //11x11 leftmost is reserved for the room itself's icons.
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


        private CombatMapReference.SingleCombatMapReference MapRef;

        private const int MAX_X_COLS =11 , MAX_Y_ROWS = 11;
        private const int ROW_BYTES = 32;

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        private unsafe struct Map_Row
        {
            fixed byte tiles[11];
            fixed byte newTiles[8];
            fixed byte zeroes[13];
        };

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        private unsafe struct Map_PlayerPos_Row
        {
            fixed byte tiles[11];
            fixed byte initial_X[6]; // initial x position of each party member
            fixed byte initial_Y[6]; // initial y position of each party member
            fixed byte zeroes[9];
        };

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        private unsafe struct Map_MonsterTile_Row
        {
            fixed byte tiles[11];
            fixed byte monster_tiles[16]; // tile for each monster
            fixed byte zeroes[5];
        };

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        private unsafe struct Map_MonsterX_Row
        {
            fixed byte tiles[11];
            fixed byte initial_X[16]; // initial x position of each monster
            fixed byte zeroes[5];
        };

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        private unsafe struct Map_MonsterY_Row
        {
            fixed byte tiles[11];
            fixed byte initial_Y[16]; // initial y position of each monster
            fixed byte zeroes[5];
        };

        public string Description
        {
            get
            {
                return MapRef.Description;
            }
        }

        /// <summary>
        /// Special function to read the map data only, putting it into a 2D array
        /// </summary>
        /// <param name="byteList">the raw data</param>
        /// <returns>2D byte map</returns>
        static private byte[][] ReadCombatBytesIntoByteArray (List<byte> byteList)
        {
            byte[][] combatMap = Utils.Init2DByteArray(MAX_Y_ROWS, MAX_X_COLS);

            for (int i=0; i < MAX_Y_ROWS; i++)
            {
                byteList.CopyTo(i * ROW_BYTES, combatMap[i], 0, MAX_X_COLS);
            }

            return combatMap;
        }

        // These are the legacy structures read in from the file. They will need to be abstracted into useable data.
        private Map_Row Row0; // Information contains the new tiles once a trigger happens  
        private List<Map_PlayerPos_Row> Row1To4_Player=new List<Map_PlayerPos_Row>(4); // row 1 = east, row 2 = west, row 3 = south, row 4 = north
        private Map_MonsterTile_Row Row5_Monster; 
        private Map_MonsterX_Row Row6_MonsterX;
        private Map_MonsterY_Row Row7_MonsterY;  
        private Map_Row Row8_Trigger; // row 8: positions of triggers, one position hits to new tiles
        private Map_Row Row9_NewTilesX;// row 9: position X of the new tiles
        private Map_Row Row10_NewTilesY; // row 10: position Y of the new tiles

        private void InitializeCombatMapStructures(string dataFilenameAndPath, int fileOffset)
        {
            FileStream fs = File.OpenRead(dataFilenameAndPath);
            fs.Seek(fileOffset, SeekOrigin.Begin);

            Row0 = (Map_Row)Utils.ReadStruct(fs, typeof(Map_Row));
            Row1To4_Player.Add((Map_PlayerPos_Row)Utils.ReadStruct(fs, typeof(Map_PlayerPos_Row)));
            Row1To4_Player.Add((Map_PlayerPos_Row)Utils.ReadStruct(fs, typeof(Map_PlayerPos_Row)));
            Row1To4_Player.Add((Map_PlayerPos_Row)Utils.ReadStruct(fs, typeof(Map_PlayerPos_Row)));
            Row1To4_Player.Add((Map_PlayerPos_Row)Utils.ReadStruct(fs, typeof(Map_PlayerPos_Row)));
            Row5_Monster = (Map_MonsterTile_Row)Utils.ReadStruct(fs, typeof(Map_MonsterTile_Row));
            Row6_MonsterX = (Map_MonsterX_Row)Utils.ReadStruct(fs, typeof(Map_MonsterX_Row));
            Row7_MonsterY = (Map_MonsterY_Row)Utils.ReadStruct(fs, typeof(Map_MonsterY_Row));
            Row8_Trigger = (Map_Row)Utils.ReadStruct(fs, typeof(Map_Row));
            Row9_NewTilesX = (Map_Row)Utils.ReadStruct(fs, typeof(Map_Row));
            Row10_NewTilesY = (Map_Row)Utils.ReadStruct(fs, typeof(Map_Row));
            fs.Close();

            System.Console.WriteLine("");
        }

        /// <summary>
        /// Create the individual Combat Map object
        /// </summary>
        /// <param name="u5Directory">Directory of data files</param>
        /// <param name="mapRef">specific combat map reference</param>
        public CombatMap (string u5Directory, CombatMapReference.SingleCombatMapReference mapRef, TileOverrides tileOverrides) : base (u5Directory, tileOverrides, null)
        {
            string dataFilenameAndPath = Path.Combine(u5Directory, mapRef.MapFilename);

            MapRef = mapRef;

            // TOOD: reads in the data from the file - should read file into memory once and leave it there so quicker reference
            List<byte> mapList = Utils.GetFileAsByteList(dataFilenameAndPath, mapRef.FileOffset, CombatMapReference.SingleCombatMapReference.MAP_BYTE_COUNT);

            TheMap = ReadCombatBytesIntoByteArray(mapList);

            // initialize the legacy structures that describe the map details
            InitializeCombatMapStructures(dataFilenameAndPath, mapRef.FileOffset);


        }

        protected override float GetAStarWeight(TileReferences spriteTileReferences, Point2D xy)
        {
            return 1;
        }

    }
}
