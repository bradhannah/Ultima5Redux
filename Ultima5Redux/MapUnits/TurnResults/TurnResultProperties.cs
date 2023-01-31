using System.Diagnostics;
using Ultima5Redux.MapUnits.CombatMapUnits;
using Ultima5Redux.MapUnits.NonPlayerCharacters;
using Ultima5Redux.PlayerCharacters;
using Ultima5Redux.References.Maps;
using Ultima5Redux.References.PlayerCharacters.Inventory;

// ReSharper disable UnusedMemberInSuper.Global
namespace Ultima5Redux.MapUnits.TurnResults
{
    public interface INonPlayerCharacterInteraction
    {
        public NonPlayerCharacter NPC { get; }
    }

    public interface IHitState
    {
        public CombatMapUnit.HitState HitState { get; }
    }

    public interface IMissedOrTriggerPoint
    {
        public Point2D MissedOrTriggerPoint { get; }
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
        public bool ForceNewLine { get; }
        public string OutputString { get; }
        public bool UseArrow { get; }
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

    public interface IMovedToTileReference
    {
        public TileReference MovedToTileReference { get; }
    }

    public interface IStackTrace
    {
        public StackTrace TheStacktrace { get; }
    }

    public interface IQuantityChanged
    {
        public int AdjustedBy { get; }
        public int PreviousQuantity { get; }
    }
}