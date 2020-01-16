using System;

namespace Ultima5Redux
{
    public partial class NonPlayerCharacterMovement
    {
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
                if (Iterations == 0) { throw new Exception("You spent a single movement - but you didn't have any repeats left "); }
                return --Iterations;
            }
        }
    }
}
