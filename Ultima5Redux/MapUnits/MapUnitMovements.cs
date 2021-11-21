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
        [DataMember(Name = "MovementList")]
        private readonly List<MapUnitMovement> _movementList = new List<MapUnitMovement>(MAX_PLAYERS);

        [JsonConstructor] public MapUnitMovements()
        {
            // _movementList = new List<MapUnitMovement>() { new MapUnitMovement(0) };
            // _movementList = Enumerable.Repeat(new MapUnitMovement(MAX_PLAYERS), 
            // var bookList = Enumerable.Repeat(new Book(), 2).ToList();
            // _movementList.Add(new MapUnitMovement(0));
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
            return _movementList[nIndex];
        }
    }
}