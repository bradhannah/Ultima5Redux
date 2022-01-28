using System.Collections.Generic;
using System.Runtime.Serialization;
using Newtonsoft.Json;
using Ultima5Redux.Data;

namespace Ultima5Redux.MapUnits
{
    /// <summary>
    ///     Stores all movements of current NPC/monsters on current map
    /// </summary>
    [DataContract] public sealed class MapUnitMovements
    {
        private const int MAX_PLAYERS = 0x020;

        /// <summary>
        ///     All available movement lists
        /// </summary>
        [DataMember(Name = "MovementList")] private readonly List<MapUnitMovement> _movementList = new(MAX_PLAYERS);

        [JsonConstructor] public MapUnitMovements()
        {
            // empty
        }

        public MapUnitMovements(DataChunk movementInstructionDataChunk, DataChunk movementOffsetDataChunk)
        {
            for (int i = 0; i < MAX_PLAYERS; i++)
            {
                _movementList.Add(new MapUnitMovement(i, movementInstructionDataChunk, movementOffsetDataChunk));
            }
        }

        /// <summary>
        ///     Gets a movement from the list (often corresponds to the NPC index)
        /// </summary>
        /// <param name="nIndex"></param>
        /// <returns></returns>
        public MapUnitMovement GetMovement(int nIndex)
        {
            if (_movementList.Count <= nIndex) return new MapUnitMovement(nIndex);
            return _movementList[nIndex];
        }
    }
}