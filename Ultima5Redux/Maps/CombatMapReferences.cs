using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.Serialization;
using Newtonsoft.Json;
using Ultima5Redux.Data;
using Ultima5Redux.MapUnits.NonPlayerCharacters.ShoppeKeepers;
using Ultima5Redux.Properties;

namespace Ultima5Redux.Maps
{
    public class CombatMapReferences
    {
        public class CombatMapData
        {
            [DataMember] public int Index;
            [DataMember] public string Description;
            [DataMember] public SingleCombatMapReference.Territory MapType;
        }
        
        // the master copy of the map references
        private const int MAPS_PER_TERRAIN = 16;

        /// <summary>
        ///     All the data chunks
        /// </summary>
        private readonly DataChunks<DataChunkName> _britDataChunks;
        private readonly DataChunks<DataChunkName> _dungeonDataChunks;
        
        private readonly Dictionary<SingleCombatMapReference.Territory, List<SingleCombatMapReference>>
            _singleCombatMapReferences =
                new Dictionary<SingleCombatMapReference.Territory, List<SingleCombatMapReference>>()
                {
                    {SingleCombatMapReference.Territory.Britannia, new List<SingleCombatMapReference>()},
                    {SingleCombatMapReference.Territory.Dungeon, new List<SingleCombatMapReference>()}
                };

        public List<SingleCombatMapReference> GetListOfSingleCombatMapReferences(
            SingleCombatMapReference.Territory territory)
        {
            return _singleCombatMapReferences[territory];
        }

        public enum DataChunkName {Unused = -1 }

        /// <summary>
        ///     Build the combat map reference
        /// </summary>
        public CombatMapReferences(string u5Directory)
        {
            Dictionary<SingleCombatMapReference.Territory, List<CombatMapData>> combatMapDataJson =
                JsonConvert.DeserializeObject<Dictionary<SingleCombatMapReference.Territory, List<CombatMapData>>>(Resources.CombatMaps);
            // Dictionary<string, CombatMapData> combatMapDataJson =
            //     JsonConvert.DeserializeObject<Dictionary<int, CombatMapData>>(Resources.ShoppeKeeperMap);
            
            string britCbtPath = Path.Combine(u5Directory, FileConstants.BRIT_CBT);
            _britDataChunks = new DataChunks<DataChunkName>(britCbtPath, DataChunkName.Unused);
            
            string dungeonCbtPath = Path.Combine(u5Directory, FileConstants.DUNGEON_CBT);
            _dungeonDataChunks = new DataChunks<DataChunkName>(dungeonCbtPath, DataChunkName.Unused);

            for (int nMap = 0; nMap < MAPS_PER_TERRAIN; nMap++)
            {
                // build the map of east, west, north and south player locations

                // create the map reference based on the static data
                _singleCombatMapReferences[SingleCombatMapReference.Territory.Britannia].Add(
                    new SingleCombatMapReference(SingleCombatMapReference.Territory.Britannia,
                        nMap, _britDataChunks, combatMapDataJson[SingleCombatMapReference.Territory.Britannia][nMap]));
            }

            for (int nMap = 0; nMap < 112; nMap++)
            {
                _singleCombatMapReferences[SingleCombatMapReference.Territory.Dungeon].Add(
                    new SingleCombatMapReference(SingleCombatMapReference.Territory.Dungeon,
                    nMap, _dungeonDataChunks, combatMapDataJson[SingleCombatMapReference.Territory.Dungeon][nMap]));
            }
        }

        public SingleCombatMapReference GetSingleCombatMapReference(SingleCombatMapReference.Territory territory,
            int nIndex)
        {
            Debug.Assert(nIndex >= 0);
            Debug.Assert(nIndex < _singleCombatMapReferences[territory].Count);
            return _singleCombatMapReferences[territory][nIndex];
        }
    }
}