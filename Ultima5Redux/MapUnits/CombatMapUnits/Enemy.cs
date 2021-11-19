using System.Collections.Generic;
using Ultima5Redux.External;
using Ultima5Redux.Maps;
using Ultima5Redux.MapUnits.NonPlayerCharacters;
using Ultima5Redux.PlayerCharacters;

namespace Ultima5Redux.MapUnits.CombatMapUnits
{
    public class Enemy : CombatMapUnit
    {
        public sealed override CharacterStats Stats { get; } = new CharacterStats();
        public override Avatar.AvatarState BoardedAvatarState => Avatar.AvatarState.Hidden;
        public override string BoardXitName => "Hostile creates don't not like to be boarded!";

        public override int ClosestAttackRange => EnemyReference.AttackRange;

        public override int Defense => EnemyReference.TheDefaultEnemyStats.Armour;

        public override int Dexterity => EnemyReference.TheDefaultEnemyStats.Dexterity;

        protected override Dictionary<Point2D.Direction, string> DirectionToTileName { get; } =
            new Dictionary<Point2D.Direction, string>();

        protected override Dictionary<Point2D.Direction, string> DirectionToTileNameBoarded { get; } =
            new Dictionary<Point2D.Direction, string>();

        // temporary until I read them in dynamically (somehow!?)
        public override int Experience => EnemyReference.Experience;
        public override string FriendlyName => SingularName;
        public override bool IsActive => Stats.CurrentHp > 0;

        public override bool IsAttackable => Stats.CurrentHp > 0;

        public override bool IsInvisible => false;
        public override TileReference KeyTileReference => EnemyReference.KeyTileReference;
        public override string Name => EnemyReference.MixedCaseSingularName.Trim();
        public override TileReference NonBoardedTileReference => KeyTileReference;
        public override string PluralName => EnemyReference.AllCapsPluralName;
        public override string SingularName => EnemyReference.MixedCaseSingularName;

        public EnemyReference EnemyReference { get; }

        public Stack<Node> FleeingPath { get; set; } = null;

        public bool IsFleeing { get; set; } = false;

        public Enemy(MapUnitMovement mapUnitMovement, EnemyReference enemyReference,
            SmallMapReferences.SingleMapReference.Location location, NonPlayerCharacterState npcState) : base(null,
            mapUnitMovement, location, npcState, enemyReference.KeyTileReference)
        {
            EnemyReference = enemyReference;

            Stats.Level = 1;
            Stats.Dexterity = EnemyReference.TheDefaultEnemyStats.Dexterity;
            Stats.Intelligence = EnemyReference.TheDefaultEnemyStats.Intelligence;
            Stats.Strength = EnemyReference.TheDefaultEnemyStats.Strength;
            Stats.Status = PlayerCharacterRecord.CharacterStatus.Good;
            Stats.CurrentHp = EnemyReference.TheDefaultEnemyStats.HitPoints;
            Stats.MaximumHp = Stats.CurrentHp;
            Stats.ExperiencePoints = 0;
            Stats.CurrentMp = 0;
        }

        public override bool IsMyEnemy(CombatMapUnit combatMapUnit) => combatMapUnit is CombatPlayer;

        public override string ToString()
        {
            return KeyTileReference.Name;
        }

        public bool CanReachForMeleeAttack(CombatMapUnit combatMapUnit)
        {
            return CanReachForMeleeAttack(combatMapUnit, EnemyReference.AttackRange);
        }
    }
}