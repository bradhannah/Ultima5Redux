using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.Serialization;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Ultima5Redux.Data;

namespace Ultima5Redux.MapUnits
{
    [DataContract] public class MapUnitMovement
    {
        /// <summary>
        ///     The direction of the movement as defined in saved.gam
        /// </summary>
        [JsonConverter(typeof(StringEnumConverter))]
        public enum MovementCommandDirection { None = 0, East = 1, North = 2, West = 3, South = 4 }

        /// <summary>
        ///     Maximum number of movement commands per map character
        /// </summary>
        private const int MAX_COMMAND_LIST_ENTRIES = 0x10;

        /// <summary>
        ///     Maximum size of each command (iterations+direction)
        /// </summary>
        private const int MAX_MOVEMENT_COMMAND_SIZE = sizeof(byte) * 2;

        /// <summary>
        ///     all movements
        /// </summary>
        [DataMember(Name = "MovementQueue")]
        private readonly Queue<MovementCommand> _movementQueue = new(MAX_COMMAND_LIST_ENTRIES);

        /// <summary>
        ///     Dialog index of the map character
        /// </summary>
        // ReSharper disable once NotAccessedField.Local
        [DataMember(Name = "DialogIndex")] private int _nDialogIndex;

        [JsonConstructor] private MapUnitMovement()
        {
        }

        public MapUnitMovement(int nDialogIndex) => _nDialogIndex = nDialogIndex;

        /// <summary>
        ///     Construct a MapUnitMovement from old save game
        /// </summary>
        /// <param name="nDialogIndex">the index of an NPC</param>
        /// <param name="movementInstructionDataChunk">The full memory chunk of all movement instructions</param>
        /// <param name="movementOffsetDataChunk">the full memory chunk of the movement offsets</param>
        public MapUnitMovement(int nDialogIndex, DataChunk movementInstructionDataChunk,
            DataChunk movementOffsetDataChunk)
        {
            // we totally ignore the first entry, since it's bad stuff
            if (nDialogIndex == 0) return;

            _nDialogIndex = nDialogIndex;

            // Note: not a very efficient method of getting a UINT16 from the list -> it has to create a brand new list!
            ushort offset = movementOffsetDataChunk.GetChunkAsUint16List()[nDialogIndex];

            // if it has the value of 0xFFFF then it indicates there are currently no instructions
            if (offset == 0xFFFF) return;

            // calculate the offset
            int nOffsetIndex = nDialogIndex * MAX_COMMAND_LIST_ENTRIES * MAX_MOVEMENT_COMMAND_SIZE;

            // get a copy because the GetAsByteList is an expensive method call
            List<byte> rawData = movementInstructionDataChunk.GetAsByteList();

            // gets a smaller version of it - much easier to keep track of
            List<byte> loadedData = rawData.GetRange(nOffsetIndex, MAX_COMMAND_LIST_ENTRIES * 2);

            int nIndex = offset;
            for (int i = 0; i < MAX_COMMAND_LIST_ENTRIES; i++)
            {
                byte nIterations = loadedData[nIndex];
                var direction = (MovementCommandDirection)loadedData[nIndex + 1];

                // if we have hit 0xFF then there is nothing else in the list and we can just return
                if (nIterations is 0xFF or 0) return;

                if (direction is not (MovementCommandDirection.East or MovementCommandDirection.West
                    or MovementCommandDirection.North or MovementCommandDirection.South))
                    throw new Ultima5ReduxException("a bad direction was set: " + direction);

                // we have a proper movement instruction so let's add it to the queue
                MovementCommand movementCommand = new(direction, nIterations);
                AddNewMovementInstruction(movementCommand);

                // we actually grab from the offset, but it is circular, so we need to mod it
                nIndex = (nIndex + 2) % (MAX_COMMAND_LIST_ENTRIES * 2);
            }
        }

        /// <summary>
        ///     Provides an adjust xy coordinate based on a given direction
        /// </summary>
        /// <param name="xy"></param>
        /// <param name="direction"></param>
        /// <param name="nSpaces"></param>
        /// <returns></returns>
        internal static Point2D GetAdjustedPos(in Point2D xy, MovementCommandDirection direction, int nSpaces = 1)
        {
            Point2D adjustedPos = new(xy.X, xy.Y);

            switch (direction)
            {
                case MovementCommandDirection.None:
                    // no movement
                    break;
                case MovementCommandDirection.East:
                    adjustedPos.X += nSpaces;
                    break;
                case MovementCommandDirection.North:
                    adjustedPos.Y -= nSpaces;
                    break;
                case MovementCommandDirection.West:
                    adjustedPos.X -= nSpaces;
                    break;
                case MovementCommandDirection.South:
                    adjustedPos.Y += nSpaces;
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(direction), direction, null);
            }

            return adjustedPos;
        }

        /// <summary>
        ///     ToString override to simplify debug reading
        /// </summary>
        /// <returns></returns>
        public override string ToString() =>
            _movementQueue.Count == 0
                ? "Empty"
                : $"First: {_movementQueue.Peek().Direction} for {_movementQueue.Peek().Iterations} times";

        /// <summary>
        ///     Adds a new movement instruction to the end of the queue
        /// </summary>
        /// <param name="movementCommand"></param>
        public void AddNewMovementInstruction(MovementCommand movementCommand)
        {
            _movementQueue.Enqueue(movementCommand);
        }

        /// <summary>
        ///     Clear all movements from the current list
        /// </summary>
        public void ClearMovements()
        {
            _movementQueue.Clear();
        }

        /// <summary>
        ///     Gets the next movement command - expects you to have confirmed there is a movement first
        /// </summary>
        /// <returns></returns>
        public MovementCommandDirection GetNextMovementCommandDirection(bool bPeek = false)
        {
            if (_movementQueue.Count <= 0)
                throw new Ultima5ReduxException("You have requested to GetNextMovementCommand but there are non left.");
            MovementCommandDirection direction = _movementQueue.Peek().Direction;

            // calculate how many you will have left after you 
            int nRemaining = _movementQueue.Peek().Iterations - 1;

            Debug.Assert(nRemaining >= 0);

            if (nRemaining == 0 && !bPeek)
            {
                // we are done with it, so let's toss it
                _movementQueue.Dequeue();
            }
            else if (!bPeek)
            {
                // we have more moves, but we are going to spend one 
                _movementQueue.Peek().SpendSingleMovement();
            }

            return direction;
        }

        /// <summary>
        ///     Checks to see if any movement commands are available
        /// </summary>
        /// <returns>true if there are commands available</returns>
        public bool IsNextCommandAvailable()
        {
            if (_movementQueue.Count > 0)
                Debug.Assert(_movementQueue.Peek().Iterations > 0,
                    "You have no iterations left on your movement command but it's still in the queue");
            return _movementQueue.Count > 0;
        }
    }
}