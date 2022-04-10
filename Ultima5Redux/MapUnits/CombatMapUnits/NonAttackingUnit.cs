using System.Collections.Generic;
using System.Runtime.Serialization;
using Ultima5Redux.PlayerCharacters;

namespace Ultima5Redux.MapUnits.CombatMapUnits
{
    public abstract class NonAttackingUnit : CombatMapUnit
    {
        [IgnoreDataMember] public override Avatar.AvatarState BoardedAvatarState => Avatar.AvatarState.Hidden;
        [IgnoreDataMember] public override string BoardXitName => "Non Attacking Units don't not like to be boarded!";
        protected internal override Dictionary<Point2D.Direction, string> DirectionToTileName { get; } = new();
        protected override Dictionary<Point2D.Direction, string> DirectionToTileNameBoarded { get; } = new();

        // public override string Name { get; }
        // public override string PluralName { get; } 
        // public override string SingularName { get; }
        // public override string FriendlyName { get; }
        public override bool IsActive => true;
        public override bool IsAttackable => false;

        public override int ClosestAttackRange => 0;
        public override int Defense => 0;
        public override int Dexterity => 0;
        public override int Experience => 0;
        public override bool IsInvisible => false;
        public override CharacterStats Stats { get; protected set; } = new();
        public override bool IsMyEnemy(CombatMapUnit combatMapUnit) => false;
    }
}