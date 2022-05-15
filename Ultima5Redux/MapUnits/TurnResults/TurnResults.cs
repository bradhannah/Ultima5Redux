using System;
using System.Collections.Generic;

namespace Ultima5Redux.MapUnits.TurnResults
{
    public class TurnResults
    {
        private readonly Queue<TurnResult> _turnResults = new();

        private readonly Dictionary<TurnResult.TurnResultType, Type> _expectedTurnResults = new()
        {
            { TurnResult.TurnResultType.DamageOverTimePoisoned, typeof(CombatMapUnitTakesDamage) },
            { TurnResult.TurnResultType.DamageOverTimeBurning, typeof(CombatMapUnitTakesDamage) },
            { TurnResult.TurnResultType.PlayerCharacterPoisoned, typeof(SinglePlayerCharacterAffected) },
            { TurnResult.TurnResultType.OutputToConsole, typeof(OutputToConsole) },
            { TurnResult.TurnResultType.Combat_EnemyMoved, typeof(EnemyMoved) }
        };

        public void PushTurnResult(TurnResult turnResult)
        {
            bool bHasExpectedType = _expectedTurnResults.ContainsKey(turnResult.TheTurnResultType);
            if (bHasExpectedType && turnResult.GetType() != _expectedTurnResults[turnResult.TheTurnResultType])
            {
                throw new Ultima5ReduxException(
                    $"Tried to submit a TurnResult of : {turnResult.GetType()} but expected type {_expectedTurnResults[turnResult.TheTurnResultType]}");
            }

            // at this point we know it's valid
            _turnResults.Enqueue(turnResult);
        }

        public TurnResult PopTurnResult() => _turnResults.Dequeue();

        public bool HasTurnResult => _turnResults.Count > 0;
        public TurnResult PeekTurnResultType => _turnResults.Peek();
        public TurnResult PeekLastTurnResult { get; } = new BasicResult(TurnResult.TurnResultType.Ignore);

        public void PushOutputToConsole(string str, bool bUseArrow = true, bool bForceNewLine = true)
            => PushTurnResult(new OutputToConsole(str));
    }
}