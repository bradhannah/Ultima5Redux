using System.Collections.Generic;
using System.Runtime.Serialization;
using Newtonsoft.Json;
using Ultima5Redux.External;
using Ultima5Redux.MapUnits.NonPlayerCharacters;
using Ultima5Redux.PlayerCharacters;
using Ultima5Redux.References;
using Ultima5Redux.References.Maps;
using Ultima5Redux.References.MapUnits.NonPlayerCharacters;

namespace Ultima5Redux.MapUnits.CombatMapUnits
{
    [DataContract] public class Enemy : CombatMapUnit
    {
        [DataMember(Name = "EnemyReferenceIndex")]
        private int _enemyReferenceIndex = -1;

        [IgnoreDataMember] public override Avatar.AvatarState BoardedAvatarState => Avatar.AvatarState.Hidden;
        [IgnoreDataMember] public override string BoardXitName => "Hostile creates don't not like to be boarded!";

        [IgnoreDataMember] public override int ClosestAttackRange => EnemyReference.AttackRange;

        [IgnoreDataMember] public override int Defense => EnemyReference.TheDefaultEnemyStats.Armour;

        [IgnoreDataMember] public override int Dexterity => EnemyReference.TheDefaultEnemyStats.Dexterity;

        // temporary until I read them in dynamically (somehow!?)
        [IgnoreDataMember] public override int Experience => EnemyReference.Experience;
        [IgnoreDataMember] public override string FriendlyName => SingularName;
        [IgnoreDataMember] public override bool IsActive => Stats.CurrentHp > 0;

        [IgnoreDataMember] public override bool IsAttackable => Stats.CurrentHp > 0;

        [IgnoreDataMember] public override bool IsInvisible => false;
        [IgnoreDataMember] public override string Name => EnemyReference.MixedCaseSingularName.Trim();
        [IgnoreDataMember] public override TileReference NonBoardedTileReference => KeyTileReference;
        [IgnoreDataMember] public override string PluralName => EnemyReference.AllCapsPluralName;
        [IgnoreDataMember] public override string SingularName => EnemyReference.MixedCaseSingularName;
        [IgnoreDataMember] public override TileReference KeyTileReference => EnemyReference.KeyTileReference;

        [IgnoreDataMember]
        public EnemyReference EnemyReference
        {
            get => GameReferences.EnemyRefs.GetEnemyReference(_enemyReferenceIndex);
            private set => _enemyReferenceIndex = value.KeyTileReference.Index;
        }

        [IgnoreDataMember] public Stack<Node> FleeingPath { get; set; }

        [IgnoreDataMember] public bool IsFleeing { get; set; }

        [IgnoreDataMember]
        protected override Dictionary<Point2D.Direction, string> DirectionToTileName { get; } = new();

        [IgnoreDataMember]
        protected override Dictionary<Point2D.Direction, string> DirectionToTileNameBoarded { get; } =
            new();

        [DataMember] public override CharacterStats Stats { get; protected set; } = new();

        [JsonConstructor] private Enemy()
        {
        }

        public Enemy(MapUnitMovement mapUnitMovement, EnemyReference enemyReference,
            SmallMapReferences.SingleMapReference.Location location, NonPlayerCharacterState npcState,
            MapUnitPosition mapUnitPosition) : base(null,
            mapUnitMovement, location, npcState, enemyReference.KeyTileReference)
        {
            EnemyReference = enemyReference;

            MapUnitPosition = mapUnitPosition;

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