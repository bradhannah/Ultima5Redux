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

        private const int TOTAL_DUNGEON_MAPS = 112;

        // the master copy of the map references
        private const int TOTAL_OVERWORLD_MAPS = 16;

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
            DataChunks<DataChunkName> britDataChunks = new DataChunks<DataChunkName>(britCbtPath, DataChunkName.Unused);

            string dungeonCbtPath = Path.Combine(u5Directory, FileConstants.DUNGEON_CBT);
            DataChunks<DataChunkName> dungeonDataChunks =
                new DataChunks<DataChunkName>(dungeonCbtPath, DataChunkName.Unused);

            if (combatMapDataJson == null || !combatMapDataJson.ContainsKey(
                                              SingleCombatMapReference.Territory.Britannia)
                                          || !combatMapDataJson.ContainsKey(SingleCombatMapReference.Territory.Dungeon))
                throw new Ultima5ReduxException("combat map json is missing maps");

            for (int nMap = 0; nMap < TOTAL_OVERWORLD_MAPS; nMap++)
            {
                // build the map of east, west, north and south player locations

                // create the map reference based on the static data
                SingleCombatMapReference britanniaCombatMapReference = new SingleCombatMapReference(
                    SingleCombatMapReference.Territory.Britannia,
                    nMap, britDataChunks, combatMapDataJson[SingleCombatMapReference.Territory.Britannia][nMap],
                    tileReferences);

                _singleCombatMapReferences[SingleCombatMapReference.Territory.Britannia]
                    .Add(britanniaCombatMapReference);
            }

            for (int nMap = 0; nMap < TOTAL_DUNGEON_MAPS; nMap++)
            {
                SingleCombatMapReference dungeonCombatMapReference = new SingleCombatMapReference(
                    SingleCombatMapReference.Territory.Dungeon,
                    nMap, dungeonDataChunks, combatMapDataJson[SingleCombatMapReference.Territory.Dungeon][nMap],
                    tileReferences);
                _singleCombatMapReferences[SingleCombatMapReference.Territory.Dungeon].Add(dungeonCombatMapReference);
            }
        }

        public string GetAsCsv(SingleCombatMapReference.Territory territory)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append(SingleCombatMapReference.GetCsvHeader());
            foreach (SingleCombatMapReference singleCombatMapReference in _singleCombatMapReferences[territory])
            {
                sb.Append("\n" + singleCombatMapReference.GetAsCsvLine());
            }

            return sb.ToString();
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

        public class CombatMapData
        {
            [DataMember] public string Description;
            [DataMember] public int Index;
            [DataMember] public SingleCombatMapReference.Territory MapType;
        }
    }
}