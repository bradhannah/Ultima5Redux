using System.Collections.Generic;
using Ultima5Redux.Data;
using Ultima5Redux.DayNightMoon;
using Ultima5Redux.Maps;
using Ultima5Redux.MapUnits.CombatMapUnits;
using Ultima5Redux.MapUnits.NonPlayerCharacters;
using Ultima5Redux.PlayerCharacters;

namespace Ultima5Redux.MapUnits.Monsters
{
    public class Enemy : CombatMapUnit
    {
        public EnemyReference EnemyReference { get; }

        public override int Defense => EnemyReference.TheDefaultEnemyStats.Armour;
        public override string Name => EnemyReference.MixedCaseSingularName;

        public Enemy(MapUnitState mapUnitState, MapUnitMovement mapUnitMovement, TileReferences tileReferences,  
            EnemyReference enemyReference, SmallMapReferences.SingleMapReference.Location location,
            DataOvlReference dataOvlReference)
        : base(null, mapUnitState, null, mapUnitMovement, null, 
            tileReferences, location, dataOvlReference)
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
        
        protected override Dictionary<Point2D.Direction, string> DirectionToTileName { get; }

        protected override Dictionary<Point2D.Direction, string> DirectionToTileNameBoarded { get; }

        public override Avatar.AvatarState BoardedAvatarState => Avatar.AvatarState.Hidden;
        public override string BoardXitName => "Hostile creates don't not like to be boarded!";
        public override TileReference NonBoardedTileReference => TheMapUnitState.Tile1Ref;
        public override bool IsActive => true;

        public override bool IsAttackable => true;
        public override string FriendlyName => Name;

        public override int Dexterity => EnemyReference.TheDefaultEnemyStats.Dexterity;

        public override string ToString()
        {
            return KeyTileReference.Name;
        }

        public sealed override CharacterStats Stats { get; } = new CharacterStats();
    }
}