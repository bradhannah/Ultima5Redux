using System.Diagnostics;
using Ultima5Redux.MapUnits.CombatMapUnits;
using Ultima5Redux.References.PlayerCharacters.Inventory;

namespace Ultima5Redux.MapUnits.TurnResults.SpecificTurnResults
{
    public sealed class AttackerTurnResult : TurnResult, IAttacker, IOpponent, IMissile, IHitState,
        IMissedOrTriggerPoint
    {
        public AttackerTurnResult(TurnResultType theTurnResultType, CombatMapUnit attacker, CombatMapUnit opponent,
            CombatItemReference.MissileType missileType, CombatMapUnit.HitState hitState,
            Point2D missedOrTriggerPoint = null) :
            base(theTurnResultType, TurnResulActionType.ActionAlreadyPerformed)
        {
            Debug.Assert(attacker != null, nameof(attacker) + " != null");
            Attacker = attacker;
            Debug.Assert(opponent != null, nameof(opponent) + " != null");
            Opponent = opponent;
            MissileType = missileType;
            HitState = hitState;
            MissedOrTriggerPoint = missedOrTriggerPoint;
            // one must be true
            Debug.Assert(opponent != null || missedOrTriggerPoint != null);
        }

        public override string GetDebugString()
        {
            string missedPointStr =
                MissedOrTriggerPoint == null ? "NotMissed" : MissedOrTriggerPoint.GetFriendlyString();
            string opponentName = Opponent == null ? "<NoOpponent>" : Opponent.FriendlyName;
            string opponentXy = Opponent == null ? "" : Opponent.MapUnitPosition.XY.GetFriendlyString();

            return $@"Attacker: {Attacker.FriendlyName}
AttackerXY: {Attacker.MapUnitPosition.XY.GetFriendlyString()}
MissileType: {MissileType}
Opponent: {opponentName}
OpponentXY: {opponentXy}
MissedPoint: {missedPointStr}
HitState: {HitState}
";
        }

        public CombatMapUnit Attacker { get; }
        public CombatMapUnit.HitState HitState { get; }
        public Point2D MissedOrTriggerPoint { get; }
        public CombatItemReference.MissileType MissileType { get; }
        public CombatMapUnit Opponent { get; }
    }
}