using Ultima5Redux.MapUnits.CombatMapUnits;
using Ultima5Redux.PlayerCharacters;
using Ultima5Redux.References.PlayerCharacters.Inventory;

namespace Ultima5Redux.MapUnits.TurnResults
{
    public interface IHitState
    {
        public CombatMapUnit.HitState HitState { get; }
    }

    public interface IMissedPoint
    {
        public Point2D MissedPoint { get; }
    }

    public interface IAttacker
    {
        public CombatMapUnit Attacker { get; }
    }

    public interface IMissile
    {
        public CombatItemReference.MissileType MissileType { get; }
    }

    public interface IOpponent
    {
        public CombatMapUnit Opponent { get; }
    }

    public interface IOutputString
    {
        public string OutputString { get; }
        public bool UseArrow { get; }
        public bool ForceNewLine { get; }
    }

    public interface IDamageAmount
    {
        public int DamageAmount { get; }
    }

    public interface ISinglePlayerCharacterAffected
    {
        public CharacterStats CombatMapUnitStats { get; }
    }

    public interface IEnemyFocus
    {
        public Enemy TheEnemy { get; }
    }

    public interface ICombatPlayerFocus
    {
        public CombatPlayer TheCombatPlayer { get; }
    }

    public interface ILoot
    {
        public NonAttackingUnit Loot { get; }
    }

    public interface IMovedPosition
    {
        public Point2D MovedFromPosition { get; }
        public Point2D MoveToPosition { get; }
    }
}