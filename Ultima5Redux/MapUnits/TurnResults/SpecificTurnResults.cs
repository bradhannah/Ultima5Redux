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
        public CombatMapUnit Opponent { get; }
        public CombatItemReference.MissileType MissileType { get; }
        public CombatMapUnit.HitState HitState { get; }
        public Point2D MissedPoint { get; }
    }

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
        public CombatMapUnit Opponent { get; }
        public CombatItemReference.MissileType MissileType { get; }
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