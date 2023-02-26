using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.Serialization;
using Ultima5Redux.DayNightMoon;
using Ultima5Redux.Maps;
using Ultima5Redux.MapUnits.TurnResults;
using Ultima5Redux.MapUnits.TurnResults.SpecificTurnResults;
using Ultima5Redux.PlayerCharacters;

namespace Ultima5Redux.MapUnits.CombatMapUnits
{
    public abstract class NonAttackingUnit : CombatMapUnit
    {
        public enum TrapComplexity { Simple, Complex }

        [SuppressMessage("ReSharper", "InconsistentNaming")]
        public enum TrapType { NONE, ACID, SLEEP, POISON, BOMB, SLEEP_ALL, ELECTRIC_ALL, POISON_ALL }

        [DataMember] public bool IsTrapped => Trap != TrapType.NONE;

        [DataMember] public virtual bool IsLocked { get; set; }

        [DataMember] public virtual TrapType Trap { get; set; }

        [DataMember] public TrapComplexity CurrentTrapComplexity { get; protected set; } = TrapComplexity.Simple;

        [DataMember] public bool HasBeenOpened { get; set; }

        [DataMember] public bool HasBeenSearched { get; set; }

        [IgnoreDataMember] public override Avatar.AvatarState BoardedAvatarState => Avatar.AvatarState.Hidden;
        [IgnoreDataMember] public override string BoardXitName => "Non Attacking Units don't not like to be boarded!";

        [IgnoreDataMember] public virtual ItemStack InnerItemStack { get; protected set; }

        public override int ClosestAttackRange => 0;
        public override int Defense => 0;
        public override int Dexterity => 0;
        public override int Experience => 0;

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

        public virtual bool NonAttackUnitTypeCanBeTrapped => false;
        protected internal override Dictionary<Point2D.Direction, string> DirectionToTileNameBoarded { get; } = new();

        protected override Dictionary<Point2D.Direction, string> DirectionToTileName { get; } = new();

        internal override void CompleteNextNonCombatMove(RegularMap regularMap, TimeOfDay timeOfDay)
        {
            // by default the thing doesn't move on it's own
        }

        public override bool IsMyEnemy(CombatMapUnit combatMapUnit) => false;

        public abstract bool DoesTriggerTrap(PlayerCharacterRecord record);

        public void ClearTrapAndInnerStack()
        {
            Trap = TrapType.NONE;
            InnerItemStack = new ItemStack(MapUnitPosition);
        }

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
                    // NOTE: this is temporary - you don't automatically poison the whole party from a poison trap
                    foreach (PlayerCharacterRecord record in records.Records)
                    {
                        turnResult =
                            new SinglePlayerCharacterAffected(TurnResult.TurnResultType.PlayerCharacterPoisoned, record, stats);
                        turnResults.PushTurnResult(turnResult);
                    }

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

        protected override bool CanMoveToDumb(Map map, Point2D mapUnitPosition) => false;
    }
}