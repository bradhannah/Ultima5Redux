using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using Newtonsoft.Json;
using Ultima5Redux.DayNightMoon;
using Ultima5Redux.External;
using Ultima5Redux.Maps;
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

        [DataMember] public sealed override CharacterStats Stats { get; protected set; } = new();

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
        [IgnoreDataMember] public override string PluralName => EnemyReference.AllCapsPluralName;
        [IgnoreDataMember] public override string SingularName => EnemyReference.MixedCaseSingularName;
        [IgnoreDataMember] public override TileReference KeyTileReference => EnemyReference.KeyTileReference;

        [IgnoreDataMember]
        public EnemyReference EnemyReference
        {
            get => GameReferences.Instance.EnemyRefs.GetEnemyReference(_enemyReferenceIndex);
            private set => _enemyReferenceIndex = value.KeyTileReference.Index;
        }

        [IgnoreDataMember] public Stack<Node> FleeingPath { get; set; }

        [IgnoreDataMember] public bool IsFleeing { get; set; }

        [IgnoreDataMember]
        protected internal override Dictionary<Point2D.Direction, string> DirectionToTileName { get; } = new();

        [IgnoreDataMember]
        protected internal override Dictionary<Point2D.Direction, string> DirectionToTileNameBoarded { get; } =
            new();

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

        internal override void CompleteNextMove(VirtualMap virtualMap, TimeOfDay timeOfDay) 
        {
            if (EnemyReference.DoesNotMove) return;
            // are we water, sand or land?
            if (virtualMap.IsLargeMap)
            {
                ProcessNextMoveTowardsMapUnitDumb(virtualMap, MapUnitPosition.XY,
                    virtualMap.TheMapUnits.CurrentAvatarPosition.XY); //, aStar);
                return;
            }

            ProcessNextMoveTowardsAvatarAStar(virtualMap.CurrentMap, virtualMap.TheMapUnits.CurrentAvatarPosition.XY,
                virtualMap.CurrentMap.GetWalkableTypeByMapUnit(this), virtualMap.TheMapUnits,
                virtualMap.TheMapOverrides);
        }

        public override TileReference GetNonBoardedTileReference() => KeyTileReference;

        public override bool IsMyEnemy(CombatMapUnit combatMapUnit) => combatMapUnit is CombatPlayer;

        public override string ToString() => KeyTileReference.Name;

        public bool CanReachForMeleeAttack(CombatMapUnit combatMapUnit) =>
            CanReachForMeleeAttack(combatMapUnit, EnemyReference.AttackRange);

        protected override bool CanMoveToDumb(VirtualMap virtualMap, Point2D mapUnitPosition)
        {
            if (EnemyReference.DoesNotMove) return false;

            bool bCanMove = false;
            TileReference tileReference = virtualMap.GetTileReference(mapUnitPosition);

            bool bIsMapUnitOnTile = virtualMap.IsMapUnitOccupiedTile(mapUnitPosition);
            if (bIsMapUnitOnTile) return false;

            if (EnemyReference.IsSandEnemy)
            {
                // if tile is sand
                bCanMove |= tileReference.Name.IndexOf("sand", 0, StringComparison.CurrentCultureIgnoreCase) >= 0;
            }
            else if (EnemyReference.IsWaterEnemy)
            {
                // if tile is water
                bCanMove |= tileReference.IsWaterEnemyPassable;
            }
            else
            {
                // the enemy is a land monster by process of elimination
                bCanMove |= tileReference.IsLandEnemyPassable;
            }

            if (EnemyReference.CanFlyOverWater)
            {
                // if tile is water
                bCanMove |= tileReference.IsWaterTile;
            }

            if (EnemyReference.CanPassThroughWalls)
            {
                // if tile is wall
                bCanMove |=
                    tileReference.Name.IndexOf("wall", 0, StringComparison.CurrentCultureIgnoreCase) >= 0;
            }

            return bCanMove;
        }
    }
}