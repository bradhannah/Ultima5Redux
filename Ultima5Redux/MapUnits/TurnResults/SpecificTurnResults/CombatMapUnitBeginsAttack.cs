using System.Diagnostics;
using Ultima5Redux.MapUnits.CombatMapUnits;
using Ultima5Redux.References.PlayerCharacters.Inventory;

namespace Ultima5Redux.MapUnits.TurnResults.SpecificTurnResults
{
    public sealed class CombatMapUnitBeginsAttack : TurnResult, IAttacker, IOpponent, IMissile
    {
        public CombatMapUnitBeginsAttack(TurnResultType theTurnResultType, CombatMapUnit attacker,
            CombatMapUnit opponent,
            CombatItemReference.MissileType missileType) : base(theTurnResultType, TurnResulActionType.ActionRequired)
        {
            Debug.Assert(attacker != null, nameof(attacker) + " != null");
            Attacker = attacker;
            if (opponent != null) Opponent = opponent;
            MissileType = missileType;
        }

        public override string GetDebugString() =>
            $@"Attacker: {Attacker.FriendlyName}
AttackerXY: {Attacker.MapUnitPosition.XY.GetFriendlyString()}
MissileType: {MissileType}
Opponent: {Opponent.FriendlyName}
OpponentXY: {Opponent.MapUnitPosition.XY.GetFriendlyString()}";

        public CombatMapUnit Attacker { get; }
        public CombatItemReference.MissileType MissileType { get; }
        public CombatMapUnit Opponent { get; }
    }
}