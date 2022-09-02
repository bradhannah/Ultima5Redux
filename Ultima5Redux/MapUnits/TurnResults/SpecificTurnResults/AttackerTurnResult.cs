using System.Diagnostics;
using Ultima5Redux.MapUnits.CombatMapUnits;
using Ultima5Redux.References.PlayerCharacters.Inventory;

namespace Ultima5Redux.MapUnits.TurnResults.SpecificTurnResults
{
    public sealed class AttackerTurnResult : TurnResult, IAttacker, IOpponent, IMissile, IHitState, IMissedPoint
    {
        public AttackerTurnResult(TurnResultType theTurnResultType, CombatMapUnit attacker, CombatMapUnit opponent,
            CombatItemReference.MissileType missileType, CombatMapUnit.HitState hitState, Point2D missedPoint = null) :
            base(theTurnResultType)
        {
            Debug.Assert(attacker != null, nameof(attacker) + " != null");
            Attacker = attacker;
            Debug.Assert(opponent != null, nameof(opponent) + " != null");
            Opponent = opponent;
            MissileType = missileType;
            HitState = hitState;
            MissedPoint = missedPoint;
            // one must be true
            Debug.Assert(opponent != null || missedPoint != null);
        }

        public CombatMapUnit Attacker { get; }
        public CombatMapUnit.HitState HitState { get; }
        public Point2D MissedPoint { get; }
        public CombatItemReference.MissileType MissileType { get; }
        public CombatMapUnit Opponent { get; }
    }
}