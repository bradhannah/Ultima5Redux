using System.Collections.Generic;
using Ultima5Redux.Data;

namespace Ultima5Redux.MapUnits
{
    /// <summary>
    ///     Stores all movements of current NPC/monsters on current map
    /// </summary>
    public class MapUnitMovements
    {
        private const int MAX_PLAYERS = 0x020;

        /// <summary>
        ///     All available movement lists
        /// </summary>
        private readonly List<MapUnitMovement> _movementList = new List<MapUnitMovement>(MAX_PLAYERS);

        /// <summary>
        ///     DataChunk of all loaded instructions (only needed during save and load)
        /// </summary>
        private DataChunk _movementInstructionDataChunk;

        /// <summary>
        ///     DataChunk of all loaded offsets into the movement lists (only needed during save and load)
        /// </summary>
        private DataChunk _movementOffsetDataChunk;

        public MapUnitMovements(DataChunk movementInstructionDataChunk, DataChunk movementOffsetDataChunk)
        {
            _movementInstructionDataChunk = movementInstructionDataChunk;
            _movementOffsetDataChunk = movementOffsetDataChunk;
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