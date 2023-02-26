using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Ultima5Redux.MapUnits.TurnResults.SpecificTurnResults;

namespace Ultima5Redux.MapUnits.TurnResults
{
    [SuppressMessage("ReSharper", "UnusedMember.Global")]
    public class TurnResults
    {
        private readonly Dictionary<TurnResult.TurnResultType, Type> _expectedTurnResults = new()
        {
            { TurnResult.TurnResultType.DamageOverTimePoisoned, typeof(CombatMapUnitTakesDamage) },
            { TurnResult.TurnResultType.DamageOverTimeBurning, typeof(CombatMapUnitTakesDamage) },
            { TurnResult.TurnResultType.PlayerCharacterPoisoned, typeof(SinglePlayerCharacterAffected) },
            { TurnResult.TurnResultType.OutputToConsole, typeof(OutputToConsole) },
            { TurnResult.TurnResultType.Combat_EnemyMoved, typeof(EnemyMoved) }
        };

        private readonly Queue<TurnResult> _turnResults = new();

        public bool HasTurnResult => _turnResults.Count > 0;
        public TurnResult PeekLastTurnResult { get; } = new BasicResult(TurnResult.TurnResultType.Ignore);
        public TurnResult PeekTurnResultType => _turnResults.Peek();

        public bool ContainsResultType(Type type)
        {
            return _turnResults.Any(t => t.GetType() == type);
        }

        public bool ContainsTurnResultType(TurnResult.TurnResultType turnResultType)
        {
            return _turnResults.Any(t => t.TheTurnResultType == turnResultType);
        }

        public T GetFirstTurnResult<T>() where T : TurnResult => _turnResults.OfType<T>().FirstOrDefault();

        public TurnResult PopTurnResult() => _turnResults.Dequeue();

        public void PushOutputToConsole(string str, bool bUseArrow = true, bool bForceNewLine = true)
        {
            PushTurnResult(new OutputToConsole(str, bUseArrow, bForceNewLine));
        }

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
    }
}