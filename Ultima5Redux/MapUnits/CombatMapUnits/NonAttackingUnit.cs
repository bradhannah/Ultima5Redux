using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using Ultima5Redux.PlayerCharacters;

namespace Ultima5Redux.MapUnits.CombatMapUnits
{
    public abstract class NonAttackingUnit : CombatMapUnit
    {
        public enum TrapType { NONE, ACID, SLEEP, POISON, BOMB, SLEEP_ALL, ELECTRIC_ALL, POISON_ALL }

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

        public virtual TrapType Trap { get; set; }
        public bool IsTrapped => Trap != TrapType.NONE;
        public virtual bool IsLocked { get; set; }

        public abstract bool IsOpenable { get; }
        public abstract bool IsSearchable { get; }
        public abstract bool ExposeInnerItemsOnSearch { get; }
        public abstract bool ExposeInnerItemsOnOpen { get; }

        public virtual ItemStack InnerItemStack { get; protected set; } //= new();
        public virtual bool HasInnerItemStack => InnerItemStack is { HasStackableItems: true };
        public abstract bool DoesTriggerTrap(PlayerCharacterRecord record);

        public TrapComplexity CurrentTrapComplexity { get; protected set; } = TrapComplexity.Simple;

        public enum TrapComplexity { Simple, Complex }

        public void TriggerTrap(PlayerCharacterRecord record, PlayerCharacterRecords records)
        {
            switch (Trap)
            {
                case TrapType.NONE:
                    return;
                case TrapType.ACID:
                    record.ProcessTurnAcid();
                    break;
                case TrapType.SLEEP:
                    record.Sleep();
                    break;
                case TrapType.POISON:
                    record.Poison();
                    break;
                case TrapType.BOMB:
                    records.Records.ForEach(r => r.ProcessTurnBomb());
                    break;
                case TrapType.SLEEP_ALL:
                    records.Records.ForEach(r => r.Sleep());
                    break;
                case TrapType.ELECTRIC_ALL:
                    records.Records.ForEach(r => r.ProcessTurnElectric());
                    break;
                case TrapType.POISON_ALL:
                    records.Records.ForEach(r => r.Poison());
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            // the trap is triggered and is therefor gone!
            Trap = TrapType.NONE;
        }
    }
}