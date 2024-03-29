﻿using System.Diagnostics.CodeAnalysis;

namespace Ultima5Redux.MapUnits
{
    public class MovementCommand
    {
        /// <summary>
        ///     the direction of the command
        /// </summary>
        public MapUnitMovement.MovementCommandDirection Direction { get; }

        /// <summary>
        ///     how many iterations of the command left
        /// </summary>
        public int Iterations { get; private set; }

        public MovementCommand(MapUnitMovement.MovementCommandDirection direction, int iterations)
        {
            Iterations = iterations;
            Direction = direction;
        }

        /// <summary>
        ///     Use a single iteration -decrement and return the number of remaining movements
        /// </summary>
        /// <returns></returns>
        [SuppressMessage("ReSharper", "UnusedMethodReturnValue.Global")]
        public int SpendSingleMovement()
        {
            if (Iterations == 0)
                throw new Ultima5ReduxException("You spent a single movement - but you didn't have any repeats left ");
            return --Iterations;
        }
    }
}