using System.Collections.Generic;
using System.Runtime.Serialization;
using Ultima5Redux.Data;
using Ultima5Redux.External;
using Ultima5Redux.Maps;
using Ultima5Redux.MapUnits.CombatMapUnits;
using Ultima5Redux.MapUnits.NonPlayerCharacters;
using Ultima5Redux.PlayerCharacters;

namespace Ultima5Redux.MapUnits.Monsters
{
    public class Enemy : CombatMapUnit
    {
        public Enemy(MapUnitState mapUnitState, MapUnitMovement mapUnitMovement, TileReferences tileReferences,
            EnemyReference enemyReference, SmallMapReferences.SingleMapReference.Location location,
            DataOvlReference dataOvlReference, NonPlayerCharacterReference npcRef, 
            NonPlayerCharacterReferences npcRefs)
            // : base(mapUnitState, mapUnitMovement,  tileReferences, location, dataOvlReference, npcRef, npcRefs)
            : base(mapUnitState, null, mapUnitMovement, 
                tileReferences, location, dataOvlReference, npcRef, npcRefs) 
        {
            EnemyReference = enemyReference;
            mapUnitState.Tile1Ref = enemyReference.KeyTileReference;
            
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

        public override Avatar.AvatarState BoardedAvatarState => Avatar.AvatarState.Hidden;
        public override bool IsActive => Stats.CurrentHp > 0;

        public override bool IsAttackable => Stats.CurrentHp > 0;

        public bool IsFleeing { get; set; } = false;

        public override bool IsInvisible => false;

        public sealed override CharacterStats Stats { get; } = new CharacterStats();

        public EnemyReference EnemyReference { get; }

        public override int ClosestAttackRange => EnemyReference.AttackRange;

        public override int Defense => EnemyReference.TheDefaultEnemyStats.Armour;

        public override int Dexterity => EnemyReference.TheDefaultEnemyStats.Dexterity;

        // temporary until I read them in dynamically (somehow!?)
        public override int Experience => EnemyReference.Experience;

        public Stack<Node> FleeingPath { get; set; } = null;
        public override string BoardXitName => "Hostile creates don't not like to be boarded!";
        public override string FriendlyName => SingularName;
        public override string Name => EnemyReference.MixedCaseSingularName.Trim();
        public override string PluralName => EnemyReference.AllCapsPluralName;
        public override string SingularName => EnemyReference.MixedCaseSingularName;
        public override TileReference NonBoardedTileReference => TheMapUnitState.Tile1Ref;

        protected override Dictionary<Point2D.Direction, string> DirectionToTileName { get; }

        protected override Dictionary<Point2D.Direction, string> DirectionToTileNameBoarded { get; }
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