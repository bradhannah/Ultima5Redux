using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Ultima5Redux
{
    public partial class NonPlayerCharacters
    {
        public class NonPlayerCharacterMovement
        {
            public NonPlayerCharacterMovement(int nDialogIndex, DataChunk dataChunk)
            {
                this.dataChunk = dataChunk;
                this.nDialogIndex = nDialogIndex;

                // calculate the offset
                int nOffsetIndex = nDialogIndex * (MAX_COMMAND_LIST_ENTRIES * MAX_MOVEMENT_COMMAND_SIZE);

                List<byte> rawData = dataChunk.GetAsByteList();

                for (int i = 0; i < MAX_COMMAND_LIST_ENTRIES; i++)
                {
                    byte nIterations = rawData[nOffsetIndex + (i * MAX_MOVEMENT_COMMAND_SIZE)];
                    MovementCommandDirection direction = (MovementCommandDirection)rawData[nOffsetIndex + (i * MAX_MOVEMENT_COMMAND_SIZE) + 1];

                    // if we have hit 0xFF then there is nothing else in the list and we can just return
                    if (nIterations == 0xFF) return;

                    // we have a proper movement instruction so let's add it to the queue
                    MovementCommand movementCommand = new MovementCommand(direction, nIterations);
                    this.MovementQueue.Enqueue(movementCommand);
                }
            }

            private DataChunk dataChunk;
            private int nDialogIndex;

            /// <summary>
            /// The direction of the movement as defined in saved.gam
            /// </summary>
            public enum MovementCommandDirection { East = 1, North = 2, West = 3, South = 4 }

            private const int MAX_COMMAND_LIST_ENTRIES = 0x10;
            private const int MAX_MOVEMENT_COMMAND_SIZE = sizeof(byte) * 2;

            public class MovementCommand
            {

                public MovementCommand(MovementCommandDirection direction, int iterations)
                {
                    Iterations = iterations;
                    Direction = direction;
                }

                /// <summary>
                /// the direction of the command
                /// </summary>
                public MovementCommandDirection Direction { get; set; }
                /// <summary>
                /// how many iterations of the command left
                /// </summary>
                public int Iterations { get; set; }

                /// <summary>
                /// Use a single iteration -decrement and return the number of remaining movements
                /// </summary>
                /// <returns></returns>
                public int SpendSingleMovement()
                {
                    if (Iterations == 0) { throw new Exception("You spent a single movement - but you didn't have any repeats left ");  }
                    return Iterations--;
                }
            }

            #region Private Fields
            /// <summary>
            /// all movements 
            /// </summary>
            private Queue<MovementCommand> MovementQueue = new Queue<MovementCommand>(MAX_COMMAND_LIST_ENTRIES);
            #endregion

            #region Public Methods
            /// <summary>
            /// Checks to see if any movement commands are available
            /// </summary>
            /// <returns>true if there are commands available</returns>
            public bool IsNextCommandAvailable()
            {
                if (MovementQueue.Count > 0) { Debug.Assert(MovementQueue.Peek().Iterations > 0, 
                    "You have no iterations left on your movement command but it's still in the queue"); }
                return MovementQueue.Count > 0;
            }
            
            /// <summary>
            /// Gets the next movement command - expects you to have confirmed there is a movement first
            /// </summary>
            /// <returns></returns>
            public MovementCommandDirection GetNextMovementCommand()
            {
                if (MovementQueue.Count <= 0) { throw new Exception("You have requested to GetNextMovementCommand but there are non left."); }
                MovementCommandDirection direction = MovementQueue.Peek().Direction;
                int nRemaining = MovementQueue.Peek().SpendSingleMovement();
                Debug.Assert(nRemaining >= 0);

                if (nRemaining == 0)
                {
                    // we are done with it, so let's toss it
                    MovementQueue.Dequeue();
                }

                return direction;
            }
            #endregion
        }



    }
}
