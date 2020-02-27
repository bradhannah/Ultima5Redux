using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Security.Cryptography.Xml;


namespace Ultima5Redux
{
    public class SmallMap : Map
    {
        /// <summary>
        /// Total tiles per row
        /// </summary>
        public const int XTILES = 32;
        /// <summary>
        /// Total tiles per column
        /// </summary>
        public const int YTILES = 32;

        public SmallMapReferences.SingleMapReference.Location MapLocation { get { return MapRef.MapLocation; } }
        public int MapFloor { get { return MapRef.Floor; } }


        private SmallMapReferences.SingleMapReference MapRef;

     
        
        /// <summary>
        /// Creates a small map object using a pre-defined map reference
        /// </summary>
        /// <param name="u5Directory"></param>
        /// <param name="mapRef"></param>
        /// <param name="spriteTileReferences"></param>
        /// <param name="tileOverrides"></param>
        public SmallMap(string u5Directory, SmallMapReferences.SingleMapReference mapRef, TileReferences spriteTileReferences, TileOverrides tileOverrides) : base(u5Directory, tileOverrides, mapRef)
        {
            MapRef = mapRef;

            // load the map into memory
            TheMap = LoadSmallMapFile(Path.Combine(u5Directory, mapRef.MapFilename), mapRef.FileOffset);

            
            InitializeAStarMap(spriteTileReferences);
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

            byte[][] smallMap = Utils.ListTo2DArray<byte>(mapBytes, XTILES, fileOffset, XTILES * YTILES);
            
            // have to transpose the array because the ListTo2DArray function puts the map together backwards...
            return Utils.TransposeArray(smallMap);
        }

        /// <summary>
        /// Calculates an appropriate A* weight based on the current tile as well as the surrounding tiles
        /// </summary>
        /// <param name="spriteTileReferences"></param>
        /// <param name="xy"></param>
        /// <returns></returns>
        protected override float GetAStarWeight(TileReferences spriteTileReferences, Point2D xy)
        {
            bool isPreferredIndex(int nSprite)
            {
                bool bIsPreferredIndex = nSprite == spriteTileReferences.GetTileReferenceByName("BrickFloor").Index || spriteTileReferences.IsPath(nSprite);
                return bIsPreferredIndex;
            }
            
            const int fDefaultDeduction = 2;
            TileReference currentTile = spriteTileReferences.GetTileReference(TheMap[xy.X][xy.Y]);
            
            float fCost = 10;

            // we reduce the weight for the A* for each adjacent brick floor or path tile
            if (xy.X - 1 >= 0) fCost -= isPreferredIndex(TheMap[xy.X - 1][xy.Y]) ? fDefaultDeduction : 0;
            if (xy.X + 1 < XTILES) fCost -= isPreferredIndex(TheMap[xy.X + 1][xy.Y]) ? fDefaultDeduction : 0;
            if (xy.Y - 1 >= 0) fCost -= isPreferredIndex(TheMap[xy.X][xy.Y-1]) ? fDefaultDeduction : 0;
            if (xy.Y + 1 < YTILES) fCost -= isPreferredIndex(TheMap[xy.X][xy.Y+1]) ? fDefaultDeduction : 0;

            return fCost;
        }
    }
}
