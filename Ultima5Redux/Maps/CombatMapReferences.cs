using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.Serialization;
using System.Text;
using Newtonsoft.Json;
using Ultima5Redux.Data;
using Ultima5Redux.Properties;

namespace Ultima5Redux.Maps
{
    public class CombatMapReferences
    {
        public enum DataChunkName { Unused = -1 }

        // the master copy of the map references
        private const int TOTAL_OVERWORLD_MAPS = 16;
        private const int TOTAL_DUNGEON_MAPS = 112;

        /// <summary>
        ///     All the data chunks
        /// </summary>
        private readonly DataChunks<DataChunkName> _britDataChunks;

        private readonly DataChunks<DataChunkName> _dungeonDataChunks;

        private readonly Dictionary<SingleCombatMapReference.Territory, List<SingleCombatMapReference>>
            _singleCombatMapReferences =
                new Dictionary<SingleCombatMapReference.Territory, List<SingleCombatMapReference>>
                {
                    { SingleCombatMapReference.Territory.Britannia, new List<SingleCombatMapReference>() },
                    { SingleCombatMapReference.Territory.Dungeon, new List<SingleCombatMapReference>() }
                };

        /// <summary>
        ///     Build the combat map reference
        /// </summary>
        public CombatMapReferences(string u5Directory, TileReferences tileReferences)
        {
            Dictionary<SingleCombatMapReference.Territory, List<CombatMapData>> combatMapDataJson =
                JsonConvert.DeserializeObject<Dictionary<SingleCombatMapReference.Territory, List<CombatMapData>>>(
                    Resources.CombatMaps);

            string britCbtPath = Path.Combine(u5Directory, FileConstants.BRIT_CBT);
            _britDataChunks = new DataChunks<DataChunkName>(britCbtPath, DataChunkName.Unused);

            string dungeonCbtPath = Path.Combine(u5Directory, FileConstants.DUNGEON_CBT);
            _dungeonDataChunks = new DataChunks<DataChunkName>(dungeonCbtPath, DataChunkName.Unused);

            for (int nMap = 0; nMap < TOTAL_OVERWORLD_MAPS; nMap++)
            {
                // build the map of east, west, north and south player locations

                // create the map reference based on the static data
                SingleCombatMapReference britanniaCombatMapReference = new SingleCombatMapReference(
                    SingleCombatMapReference.Territory.Britannia,
                    nMap, _britDataChunks, combatMapDataJson[SingleCombatMapReference.Territory.Britannia][nMap],
                    tileReferences);

                _singleCombatMapReferences[SingleCombatMapReference.Territory.Britannia]
                    .Add(britanniaCombatMapReference);
            }

            for (int nMap = 0; nMap < TOTAL_DUNGEON_MAPS; nMap++)
            {
                SingleCombatMapReference dungeonCombatMapReference = new SingleCombatMapReference(
                    SingleCombatMapReference.Territory.Dungeon,
                    nMap, _dungeonDataChunks, combatMapDataJson[SingleCombatMapReference.Territory.Dungeon][nMap],
                    tileReferences);
                _singleCombatMapReferences[SingleCombatMapReference.Territory.Dungeon].Add(dungeonCombatMapReference);
            }
        }

        public List<SingleCombatMapReference> GetListOfSingleCombatMapReferences(
            SingleCombatMapReference.Territory territory)
        {
            return _singleCombatMapReferences[territory];
        }

        public SingleCombatMapReference GetSingleCombatMapReference(SingleCombatMapReference.Territory territory,
            int nIndex)
        {
            Debug.Assert(nIndex >= 0);
            Debug.Assert(nIndex < _singleCombatMapReferences[territory].Count);
            return _singleCombatMapReferences[territory][nIndex];
        }

        public string GetAsCSV(SingleCombatMapReference.Territory territory)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append(SingleCombatMapReference.GetCSVHeader());
            foreach (SingleCombatMapReference singleCombatMapReference in _singleCombatMapReferences[territory])
            {
                sb.Append("\n" + singleCombatMapReference.GetAsCSVLine());
            }

            return sb.ToString();
        }

        public class CombatMapData
        {
            [DataMember] public string Description;
            [DataMember] public int Index;
            [DataMember] public SingleCombatMapReference.Territory MapType;
        }
    }
}