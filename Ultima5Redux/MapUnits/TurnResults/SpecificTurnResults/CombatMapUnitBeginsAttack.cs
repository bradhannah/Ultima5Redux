using System.Diagnostics;
using Ultima5Redux.MapUnits.CombatMapUnits;
using Ultima5Redux.References.PlayerCharacters.Inventory;

namespace Ultima5Redux.MapUnits.TurnResults.SpecificTurnResults
{
    public sealed class CombatMapUnitBeginsAttack : TurnResult, IAttacker, IOpponent, IMissile
    {
        public CombatMapUnitBeginsAttack(TurnResultType theTurnResultType, CombatMapUnit attacker,
            CombatMapUnit opponent,
            CombatItemReference.MissileType missileType) : base(theTurnResultType)
        {
            Debug.Assert(attacker != null, nameof(attacker) + " != null");
            Attacker = attacker;
            if (opponent != null) Opponent = opponent;
            MissileType = missileType;
        }

        public CombatMapUnit Attacker { get; }
        public CombatItemReference.MissileType MissileType { get; }
        public CombatMapUnit Opponent { get; }
    }
}