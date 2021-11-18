namespace Ultima5Redux.MapUnits
{
    public partial class MapUnitMovement
    {
        public class MovementCommand
        {
            /// <summary>
            ///     the direction of the command
            /// </summary>
            public MovementCommandDirection Direction { get; }

            /// <summary>
            ///     how many iterations of the command left
            /// </summary>
            public int Iterations { get; private set; }

            public MovementCommand(MovementCommandDirection direction, int iterations)
            {
                Iterations = iterations;
                Direction = direction;
            }

            /// <summary>
            ///     Use a single iteration -decrement and return the number of remaining movements
            /// </summary>
            /// <returns></returns>
            public int SpendSingleMovement()
            {
                if (Iterations == 0)
                    throw new Ultima5ReduxException(
                        "You spent a single movement - but you didn't have any repeats left ");
                return --Iterations;
            }
        }
    }
}