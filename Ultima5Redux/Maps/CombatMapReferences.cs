using System.Collections.Generic;
using System.IO;
using Ultima5Redux.Data;

namespace Ultima5Redux.Maps
{
    public class CombatMapReferences
    {
        // the master copy of the map references
        private const int MAPS_PER_TERRAIN = 16;

        /// <summary>
        ///     All the data chunks
        /// </summary>
        private readonly DataChunks<DataChunkName> _britDataChunks;
        private readonly DataChunks<DataChunkName> _dungeonDataChunks;

        private readonly List<List<DataChunk>> _britTileMappingDataChunks = new List<List<DataChunk>>(MAPS_PER_TERRAIN);
        
        private enum DataChunkName {Unused = -1 }

        /// <summary>
        ///     Build the combat map reference
        /// </summary>
        public CombatMapReferences(string u5Directory)
        {
            string britCbtPath = Path.Combine(u5Directory, FileConstants.BRIT_CBT);
            _britDataChunks = new DataChunks<DataChunkName>(britCbtPath, DataChunkName.Unused);
            
            string dungeonCbtPath = Path.Combine(u5Directory, FileConstants.DUNGEON_CBT);
            _dungeonDataChunks = new DataChunks<DataChunkName>(dungeonCbtPath, DataChunkName.Unused);


            for (int nMap = 0; nMap < MAPS_PER_TERRAIN; nMap++)
            {
                // the compatible byte representation of the map 
                List<List<byte>> _britBytes = new List<List<byte>>();

                // add new a list of tiles, and add to master list
                List<DataChunk> _britTileIndexChunks = new List<DataChunk>(CombatMapLegacy.YTILES);
                _britTileMappingDataChunks.Add(_britTileIndexChunks);
                int nMapOffset = nMap * 0x160;
                for (int nRow = 0; nRow < CombatMapLegacy.XTILES; nRow++)
                {
                    _britTileIndexChunks.Add(_britDataChunks.AddDataChunk(DataChunk.DataFormatType.ByteList, "Tiles for row " + nRow.ToString(),
                        nMapOffset + (0x20 * nRow), CombatMapLegacy.YTILES));
                    _britBytes.Add(_britTileIndexChunks[nRow].GetAsByteList());
                }

                // create the map reference based on the static data
                MapReferenceList.Add(new SingleCombatMapReference(SingleCombatMapReference.Territory.Britannia, nMap,
                    _britBytes));
            }
           
        }

        /// <summary>
        /// The list of map references
        /// </summary>
        public List<SingleCombatMapReference> MapReferenceList { get; } = new List<SingleCombatMapReference>();
    }
}