using System.Diagnostics.CodeAnalysis;

namespace Ultima5Redux.References
{
    internal static class GameStateReference
    {
        [SuppressMessage("ReSharper", "MemberCanBePrivate.Global")]
        [SuppressMessage("ReSharper", "UnusedAutoPropertyAccessor.Global")]
        public static GameState PreviousState { get; private set; }
        public static GameState State { get; private set; }

        public static void SetState(GameState state)
        {
            PreviousState = State;
            State = state;
        }
    }
}