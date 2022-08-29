using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using Ultima5Redux.DayNightMoon;
using Ultima5Redux.External;
using Ultima5Redux.Maps;
using Ultima5Redux.MapUnits.TurnResults;
using Ultima5Redux.PlayerCharacters;

namespace Ultima5Redux.MapUnits.CombatMapUnits
{
    public abstract class NonAttackingUnit : CombatMapUnit
    {
        public enum TrapComplexity { Simple, Complex }

        public enum TrapType { NONE, ACID, SLEEP, POISON, BOMB, SLEEP_ALL, ELECTRIC_ALL, POISON_ALL }

        [IgnoreDataMember] public override Avatar.AvatarState BoardedAvatarState => Avatar.AvatarState.Hidden;
        [IgnoreDataMember] public override string BoardXitName => "Non Attacking Units don't not like to be boarded!";

        public override int ClosestAttackRange => 0;
        public override int Defense => 0;
        public override int Dexterity => 0;
        public override int Experience => 0;

        // public override string Name { get; }
        // public override string PluralName { get; } 
        // public override string SingularName { get; }
        // public override string FriendlyName { get; }
        public override bool IsActive => (!HasBeenSearched && ExposeInnerItemsOnSearch) ||
                                         (!HasBeenOpened && ExposeInnerItemsOnOpen);

        public override bool IsAttackable => false;
        public override bool IsInvisible => false;
        public override CharacterStats Stats { get; protected set; } = new();
        public abstract bool ExposeInnerItemsOnOpen { get; }
        public abstract bool ExposeInnerItemsOnSearch { get; }

        public abstract bool IsOpenable { get; }
        public abstract bool IsSearchable { get; }
        public virtual bool HasInnerItemStack => InnerItemStack is { HasStackableItems: true };

        public virtual ItemStack InnerItemStack { get; protected set; } //= new();
        public virtual bool IsLocked { get; set; }

        public virtual TrapType Trap { get; set; }

        public TrapComplexity CurrentTrapComplexity { get; protected set; } = TrapComplexity.Simple;
        public bool HasBeenOpened { get; set; } = false;

        public bool HasBeenSearched { get; set; } = false;
        public bool IsTrapped => Trap != TrapType.NONE;
        protected internal override Dictionary<Point2D.Direction, string> DirectionToTileName { get; } = new();
        protected internal override Dictionary<Point2D.Direction, string> DirectionToTileNameBoarded { get; } = new();

        internal override void CompleteNextMove(VirtualMap virtualMap, TimeOfDay timeOfDay, AStar aStar)
        {
            // by default the thing doesn't move on it's own
        }

        public override bool IsMyEnemy(CombatMapUnit combatMapUnit) => false;
        public abstract bool DoesTriggerTrap(PlayerCharacterRecord record);

        public void TriggerTrap(TurnResults.TurnResults turnResults, CharacterStats stats,
            PlayerCharacterRecords records)
        {
            TurnResult turnResult;
            switch (Trap)
            {
                case TrapType.NONE:
                    return;
                case TrapType.ACID:
                    int nDamageAmount = stats.ProcessTurnAcid();
                    turnResult =
                        new CombatMapUnitTakesDamage(TurnResult.TurnResultType.DamageFromAcid, stats, nDamageAmount);
                    turnResults.PushTurnResult(turnResult);
                    break;
                case TrapType.SLEEP:
                    stats.Sleep();
                    break;
                case TrapType.POISON:
                    turnResult =
                        new SinglePlayerCharacterAffected(TurnResult.TurnResultType.PlayerCharacterPoisoned, stats);
                    turnResults.PushTurnResult(turnResult);
                    stats.Poison();
                    break;
                case TrapType.BOMB:
                    records.Records.ForEach(r => r.Stats.ProcessTurnBomb());
                    break;
                case TrapType.SLEEP_ALL:
                    records.Records.ForEach(r => r.Stats.Sleep());
                    break;
                case TrapType.ELECTRIC_ALL:
                    records.Records.ForEach(r => r.Stats.ProcessTurnElectric());
                    break;
                case TrapType.POISON_ALL:
                    records.Records.ForEach(r => r.Stats.Poison());
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            // the trap is triggered and is therefor gone!
            Trap = TrapType.NONE;
        }

        protected override bool CanMoveToDumb(VirtualMap virtualMap, Point2D mapUnitPosition)
        {
            return false;
        }
    }
}