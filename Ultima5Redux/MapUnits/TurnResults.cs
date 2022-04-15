using System.Collections.Generic;

namespace Ultima5Redux.MapUnits
{
    public class TurnResults
    {
        private readonly Queue<TurnResult> _turnResults = new();

        public enum TurnResult
        {
            Moved, ShipChangeDirection, Blocked, OfferToExitScreen, UsedStairs, Fell, ShipBreakingUp, ShipDestroyed,
            RoughSeas, MovedSelectionCursor, IgnoredMovement, Poisoned, Burning, Ignore, CombatMapLoaded, PassTurn,
            ActionPerformed, Opened, FireCannon
        }

        public void PushTurnResult(TurnResult turnResult)
        {
            PeekLastTurnResult = turnResult;
            _turnResults.Enqueue(turnResult);
        }

        public TurnResult PopTurnResult() => _turnResults.Dequeue();

        public bool HasTurnResult => _turnResults.Count > 0;
        public TurnResult PeekTurnResult => _turnResults.Peek();
        public TurnResult PeekLastTurnResult { get; private set; } = TurnResult.Ignore;
    }
}