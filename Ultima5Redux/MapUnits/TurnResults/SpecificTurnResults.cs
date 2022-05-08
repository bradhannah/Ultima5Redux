using System.Diagnostics;
using Ultima5Redux.MapUnits.CombatMapUnits;
using Ultima5Redux.PlayerCharacters;
using Ultima5Redux.References.PlayerCharacters.Inventory;

namespace Ultima5Redux.MapUnits.TurnResults
{
    public sealed class LootDropped : TurnResult, ILoot
    {
        public LootDropped(NonAttackingUnit loot) : base(TurnResultType.Combat_LootDropped)
        {
            Loot = loot;
        }

        public NonAttackingUnit Loot { get; }
    }

    public sealed class AttackerMissed : TurnResult, IMissedPoint, IAttacker, IMissile
    {
        public AttackerMissed(TurnResultType theTurnResultType, CombatMapUnit attacker, Point2D missedPoint,
            CombatItemReference.MissileType missileType) : base(theTurnResultType)
        {
            Debug.Assert(attacker != null, nameof(attacker) + " != null");
            Attacker = attacker;
            Debug.Assert(missedPoint != null, nameof(missedPoint) + " != null");
            MissedPoint = missedPoint;
            MissileType = missileType;
        }

        public Point2D MissedPoint { get; }
        public CombatMapUnit Attacker { get; }
        public CombatItemReference.MissileType MissileType { get; }
    }

    public sealed class CombatPlayerKilled : TurnResult, IAttacker, IOpponent
    {
        public CombatPlayerKilled(CombatMapUnit attacker, CombatMapUnit opponent) : base(TurnResultType
            .Combat_CombatPlayerKilled)
        {
            Debug.Assert(attacker != null, nameof(attacker) + " != null");
            Attacker = attacker;
            Debug.Assert(opponent != null, nameof(opponent) + " != null");
            Opponent = opponent;
        }

        public CombatMapUnit Attacker { get; }
        public CombatMapUnit Opponent { get; }
    }

    public sealed class EnemyKilled : TurnResult, IAttacker, IOpponent
    {
        public EnemyKilled(CombatMapUnit attacker, CombatMapUnit opponent) : base(TurnResultType.Combat_EnemyKilled)
        {
            Debug.Assert(attacker != null, nameof(attacker) + " != null");
            Attacker = attacker;
            Debug.Assert(opponent != null, nameof(opponent) + " != null");
            Opponent = opponent;
        }

        public CombatMapUnit Attacker { get; }
        public CombatMapUnit Opponent { get; }
    }

    public sealed class AttackerHit : TurnResult, IAttacker, IOpponent, IMissile, IHitState
    {
        public AttackerHit(TurnResultType theTurnResultType, CombatMapUnit attacker, CombatMapUnit opponent,
            CombatItemReference.MissileType missileType, CombatMapUnit.HitState hitState) : base(theTurnResultType)
        {
            Debug.Assert(attacker != null, nameof(attacker) + " != null");
            Attacker = attacker;
            Debug.Assert(opponent != null, nameof(opponent) + " != null");
            Opponent = opponent;
            MissileType = missileType;
            HitState = hitState;
        }

        public CombatMapUnit Attacker { get; }
        public CombatMapUnit Opponent { get; }
        public CombatItemReference.MissileType MissileType { get; }
        public CombatMapUnit.HitState HitState { get; }
    }

    public sealed class MissedButHit : TurnResult, IAttacker, IOpponent, IMissile
    {
        public MissedButHit(TurnResultType theTurnResultType, CombatMapUnit attacker, CombatMapUnit opponent,
            CombatItemReference.MissileType missileType) : base(theTurnResultType)
        {
            Debug.Assert(attacker != null, nameof(attacker) + " != null");
            Attacker = attacker;
            Debug.Assert(opponent != null, nameof(opponent) + " != null");
            Opponent = opponent;
            MissileType = missileType;
        }

        public CombatMapUnit Attacker { get; }
        public CombatMapUnit Opponent { get; }
        public CombatItemReference.MissileType MissileType { get; }
    }

    public sealed class CombatMapUnitAttacks : TurnResult, IAttacker, IOpponent, IMissile
    {
        public CombatMapUnitAttacks(TurnResultType theTurnResultType, CombatMapUnit attacker, CombatMapUnit opponent,
            CombatItemReference.MissileType missileType) : base(theTurnResultType)
        {
            Debug.Assert(attacker != null, nameof(attacker) + " != null");
            Attacker = attacker;
            if (opponent != null) Opponent = opponent;
            MissileType = missileType;
        }

        public CombatMapUnit Attacker { get; }
        public CombatMapUnit Opponent { get; }
        public CombatItemReference.MissileType MissileType { get; }
    }

    public sealed class CombatMapUnitMissed : TurnResult, IAttacker, IOpponent, IMissile, IMissedPoint
    {
        public CombatMapUnitMissed(TurnResultType theTurnResultType, CombatMapUnit attacker, CombatMapUnit opponent,
            CombatItemReference.MissileType missileType, Point2D missedPoint) : base(theTurnResultType)
        {
            Debug.Assert(attacker != null, nameof(attacker) + " != null");
            Attacker = attacker;
            if (opponent != null) Opponent = opponent;
            MissileType = missileType;
            Debug.Assert(missedPoint != null, nameof(missedPoint) + " != null");
            MissedPoint = missedPoint;
        }

        public CombatMapUnit Attacker { get; }
        public CombatMapUnit Opponent { get; }
        public CombatItemReference.MissileType MissileType { get; }
        public Point2D MissedPoint { get; }
    }

    public sealed class AttackerAndOpponent : TurnResult, IAttacker, IOpponent
    {
        public AttackerAndOpponent(TurnResultType theTurnResultType, CombatMapUnit attacker, CombatMapUnit opponent) :
            base(theTurnResultType)
        {
            Debug.Assert(attacker != null, nameof(attacker) + " != null");
            Attacker = attacker;
            Debug.Assert(opponent != null, nameof(opponent) + " != null");
            Opponent = opponent;
        }

        public CombatMapUnit Attacker { get; }
        public CombatMapUnit Opponent { get; }
    }

    public sealed class OutputToConsole : TurnResult, IOutputString
    {
        public OutputToConsole(string outputString, bool bUseArrow = true, bool bForceNewLine = true) : base(
            TurnResultType.OutputToConsole)
        {
            OutputString = outputString;
            UseArrow = bUseArrow;
            ForceNewLine = bForceNewLine;
        }

        public string OutputString { get; }
        public bool UseArrow { get; }
        public bool ForceNewLine { get; }
    }

    public sealed class CombatMapUnitTakesDamage : TurnResult, ISinglePlayerCharacterAffected, IDamageAmount
    {
        public CombatMapUnitTakesDamage(TurnResultType theTurnResultType, CharacterStats combatMapUnitStats,
            int damageAmount) : base(theTurnResultType)
        {
            CombatMapUnitStats = combatMapUnitStats;
            DamageAmount = damageAmount;
        }

        public int DamageAmount { get; }
        public CharacterStats CombatMapUnitStats { get; }
    }

    public sealed class SinglePlayerCharacterAffected : TurnResult, ISinglePlayerCharacterAffected
    {
        public SinglePlayerCharacterAffected(TurnResultType theTurnResultType,
            CharacterStats stats) : base(theTurnResultType)
        {
            CombatMapUnitStats = stats;
        }

        public CharacterStats CombatMapUnitStats { get; }
        public PlayerCharacterRecord PlayerRecord { get; set; }
    }

    public sealed class EnemyFocusedTurnResult : TurnResult, IEnemyFocus
    {
        public EnemyFocusedTurnResult(TurnResultType theTurnResultType, Enemy theEnemy) : base(theTurnResultType)
        {
            TheEnemy = theEnemy;
        }

        public Enemy TheEnemy { get; }
    }

    public sealed class CombatPlayerMoved : TurnResult, ICombatPlayerFocus, IMovedPosition
    {
        public CombatPlayerMoved(TurnResultType theTurnResultType, CombatPlayer theCombatPlayer,
            Point2D movedFromPosition, Point2D moveToPosition) : base(theTurnResultType)
        {
            TheCombatPlayer = theCombatPlayer;
            MovedFromPosition = movedFromPosition;
            MoveToPosition = moveToPosition;
        }

        public CombatPlayer TheCombatPlayer { get; }
        public Point2D MovedFromPosition { get; }
        public Point2D MoveToPosition { get; }
    }

    public sealed class EnemyMoved : TurnResult, IEnemyFocus, IMovedPosition
    {
        public EnemyMoved(TurnResultType theTurnResultType, Enemy theEnemy, Point2D movedFromPosition,
            Point2D moveToPosition) : base(theTurnResultType)
        {
            TheEnemy = theEnemy;
            MovedFromPosition = movedFromPosition;
            MoveToPosition = moveToPosition;
        }

        public Point2D MovedFromPosition { get; }
        public Point2D MoveToPosition { get; }
        public Enemy TheEnemy { get; }
    }
}