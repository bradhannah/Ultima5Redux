using System;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.Serialization;
using Newtonsoft.Json;
using Ultima5Redux.MapUnits.NonPlayerCharacters;
using Ultima5Redux.MapUnits.TurnResults;
using Ultima5Redux.MapUnits.TurnResults.SpecificTurnResults;
using Ultima5Redux.PlayerCharacters;
using Ultima5Redux.PlayerCharacters.CombatItems;
using Ultima5Redux.References;
using Ultima5Redux.References.Maps;
using Ultima5Redux.References.PlayerCharacters.Inventory;

namespace Ultima5Redux.MapUnits.CombatMapUnits
{
    [SuppressMessage("ReSharper", "CommentTypo")]
    public abstract class CombatMapUnit : MapUnit
    {
        public enum HitState
        {
            Grazed,
            Missed,
            BarelyWounded,
            LightlyWounded,
            HeavilyWounded,
            CriticallyWounded,
            Fleeing,
            Dead,
            None,
            Blocked,
            HitTrigger,
            HitNothingOfNote
        }

        [IgnoreDataMember] private readonly Random _random = new(Guid.NewGuid().GetHashCode());


        [IgnoreDataMember]
        private HitState CurrentHitState
        {
            get
            {
                int nCriticalThreshold = Stats.MaximumHp >> 2; /* (MaximumHp / 4) */
                int nHeavyThreshold = Stats.MaximumHp >> 1; /* (MaximumHp / 2) */
                int nLightThreshold = nCriticalThreshold + nHeavyThreshold;
                const int nFleeingThreshold = 24;
                switch (Stats.CurrentHp)
                {
                    case <= 0:
                        return HitState.Dead;
                    case < nFleeingThreshold:
                        return HitState.Fleeing;
                }

                if (Stats.CurrentHp < nCriticalThreshold)
                {
                    return HitState.CriticallyWounded;
                }

                if (Stats.CurrentHp < nHeavyThreshold)
                {
                    return HitState.HeavilyWounded;
                }

                // ReSharper disable once ConvertIfStatementToReturnStatement
                if (Stats.CurrentHp < nLightThreshold)
                {
                    return HitState.LightlyWounded;
                }

                return HitState.BarelyWounded;
            }
        }

        [IgnoreDataMember] public abstract int ClosestAttackRange { get; }

        [IgnoreDataMember] public abstract int Defense { get; }

        [IgnoreDataMember] public abstract int Dexterity { get; }

        [IgnoreDataMember] public abstract int Experience { get; }
        [IgnoreDataMember] public abstract bool IsInvisible { get; }
        [IgnoreDataMember] public abstract string PluralName { get; }

        [IgnoreDataMember] public abstract string SingularName { get; }

        [IgnoreDataMember] public PlayerCombatStats CombatStats { get; } = new();

        [IgnoreDataMember] public bool IsCharmed => Stats.Status == PlayerCharacterRecord.CharacterStatus.Charmed;
        [IgnoreDataMember] public bool IsSleeping => Stats.Status == PlayerCharacterRecord.CharacterStatus.Asleep;

        [IgnoreDataMember] public bool HasEscaped { get; set; }

        [IgnoreDataMember] public CombatMapUnit PreviousAttackTarget { get; private set; }

        public abstract string Name { get; }

        public abstract CharacterStats Stats { get; protected set; }

        [JsonConstructor]
        protected CombatMapUnit()
        {
        }

        protected CombatMapUnit(SmallMapCharacterState smallMapTheSmallMapCharacterState,
            MapUnitMovement mapUnitMovement, SmallMapReferences.SingleMapReference.Location location,
            NonPlayerCharacterState npcState, TileReference tileReference) : base(smallMapTheSmallMapCharacterState,
            mapUnitMovement, location, Point2D.Direction.None, npcState, tileReference, new MapUnitPosition())
        {
        }

        private static HitState GetState(CombatMapUnit enemyCombatMapUnit, out string stateOutput)
        {
            stateOutput = enemyCombatMapUnit.FriendlyName;

            HitState enemyHitState = enemyCombatMapUnit.CurrentHitState;
            switch (enemyHitState)
            {
                case HitState.Grazed:
                    stateOutput += " grazed!";
                    break;
                case HitState.Missed:
                    stateOutput += " missed!";
                    break;
                case HitState.BarelyWounded:
                    stateOutput +=
                        GameReferences.Instance.DataOvlRef.StringReferences.GetString(DataOvlReference.BattleStrings
                            ._BARELY_WOUNDED_BANG_N);
                    break;
                case HitState.LightlyWounded:
                    stateOutput +=
                        GameReferences.Instance.DataOvlRef.StringReferences.GetString(DataOvlReference.BattleStrings
                            ._LIGHTLY_WOUNDED_BANG_N);
                    break;
                case HitState.HeavilyWounded:
                    stateOutput +=
                        GameReferences.Instance.DataOvlRef.StringReferences.GetString(DataOvlReference.BattleStrings
                            .HEAVILY_WOUNDED_BANG_N);
                    break;
                case HitState.CriticallyWounded:
                    stateOutput +=
                        GameReferences.Instance.DataOvlRef.StringReferences.GetString(DataOvlReference.BattleStrings
                            ._CRITICAL_BANG_N);
                    break;
                case HitState.Fleeing:
                    if (enemyCombatMapUnit is Enemy enemy)
                    {
                        stateOutput += " fleeing!";
                        enemy.IsFleeing = true;
                    }
                    else
                    {
                        stateOutput +=
                            GameReferences.Instance.DataOvlRef.StringReferences.GetString(DataOvlReference.BattleStrings
                                ._CRITICAL_BANG_N);
                    }

                    break;
                case HitState.Dead:
                    stateOutput +=
                        GameReferences.Instance.DataOvlRef.StringReferences.GetString(DataOvlReference.BattleStrings
                            ._KILLED_BANG_N);
                    break;
                default:
                    throw new InvalidEnumArgumentException(((int)enemyHitState).ToString());
            }

            return enemyHitState;
        }

        // bool Creature::isHit(int hit_offset) {
        //     return (hit_offset + 128) >= xu4_random(0x100) ? true : false;
        // }

        private int GetAttackDamage(CombatMapUnit enemyCombatMapUnit, int nMaxDamage)
        {
            // add the characters strength
            nMaxDamage += Stats.Strength;
            // subtract the defense of unit being attacked
            nMaxDamage -= enemyCombatMapUnit.Defense;
            // choose 0-max damage as attack value
            int nDamage = nMaxDamage <= 0 ? 0 : _random.Next() % nMaxDamage;
            // 99 is max damage no matter what
            nDamage = Math.Min(nDamage, 99);

            return nDamage;
        }

        private bool IsHit(CombatMapUnit enemyCombatMapUnit, out string debugStr)
        {
            const int nHitOffset = 128;
            int randomNum = _random.Next(255);
            bool bWasHit = enemyCombatMapUnit.Stats.Dexterity + nHitOffset >= randomNum;
            debugStr =
                $"Ran:{randomNum} Dex:{enemyCombatMapUnit.Stats.Dexterity} Dex+128:{enemyCombatMapUnit.Stats.Dexterity + 128} Hit:{bWasHit}";
            return bWasHit;
        }

        public abstract bool IsMyEnemy(CombatMapUnit combatMapUnit);

        public HitState Attack(TurnResults.TurnResults turnResults, CombatMapUnit opposingCombatMapUnit, int nAttackMax,
            CombatItemReference.MissileType missileType, out NonAttackingUnit nonAttackingUnitDrop,
            bool bIsEnemyAttacking, bool bForceHit = false)
        {
            // is it a portcullis in a combat map? When you attack they go away

            bool bIsHit = IsHit(opposingCombatMapUnit, out string debugStr) || bForceHit;

            // drop nothing by default
            nonAttackingUnitDrop = null;

            PreviousAttackTarget = opposingCombatMapUnit;

            int nAttack = GetAttackDamage(opposingCombatMapUnit, nAttackMax);
            // this just says that an attack is being attempted - it is not the result of the attack
            turnResults.PushTurnResult(new CombatMapUnitBeginsAttack(
                bIsEnemyAttacking
                    ? TurnResult.TurnResultType.Combat_EnemyBeginsAttack
                    : TurnResult.TurnResultType.Combat_CombatPlayerBeginsAttack,
                this, opposingCombatMapUnit, missileType));
            if (!bIsHit)
            {
                string missedOutput = FriendlyName + GameReferences.Instance.DataOvlRef.StringReferences
                                          .GetString(DataOvlReference.BattleStrings._MISSED_BANG_N).TrimEnd()
                                          .Replace("!", " ") +
                                      opposingCombatMapUnit.FriendlyName + "!" + "\n" + debugStr;
                turnResults.PushOutputToConsole(missedOutput);
                // bajh: I wonder if this is unimportant information since it has a more specific turn result later
                // for ranged or melee
                if (bIsEnemyAttacking)
                {
                    turnResults.PushTurnResult(new AttackerTurnResult(
                        TurnResult.TurnResultType.Combat_EnemyMissedTarget,
                        this, opposingCombatMapUnit, missileType, HitState.Missed,
                        opposingCombatMapUnit.MapUnitPosition.XY));
                }
                else
                {
                    turnResults.PushTurnResult(new AttackerTurnResult(
                        TurnResult.TurnResultType.Combat_CombatPlayerMissedTarget,
                        this, opposingCombatMapUnit, missileType, HitState.Missed,
                        opposingCombatMapUnit.MapUnitPosition.XY));
                }

                return HitState.Missed;
            }

            if (nAttack == 0)
            {
                string grazedOutput = FriendlyName + GameReferences.Instance.DataOvlRef.StringReferences
                                          .GetString(DataOvlReference.BattleStrings._GRAZED_BANG_N).TrimEnd()
                                          .Replace("!", " ") +
                                      opposingCombatMapUnit.FriendlyName + "!" + "\n" + debugStr;
                turnResults.PushOutputToConsole(grazedOutput);
                if (bIsEnemyAttacking)
                {
                    turnResults.PushTurnResult(new AttackerTurnResult(
                        TurnResult.TurnResultType.Combat_Result_EnemyGrazedTarget,
                        this, opposingCombatMapUnit, missileType, HitState.Grazed));
                }
                else
                {
                    turnResults.PushTurnResult(new AttackerTurnResult(
                        TurnResult.TurnResultType.Combat_Result_CombatPlayerGrazedTarget,
                        this, opposingCombatMapUnit, missileType, HitState.Grazed));
                }

                return HitState.Grazed;
            }

            // we track extra stats
            opposingCombatMapUnit.CombatStats.TotalDamageTaken +=
                Math.Min(nAttack, opposingCombatMapUnit.Stats.CurrentHp);
            CombatStats.TotalDamageGiven += Math.Min(nAttack, opposingCombatMapUnit.Stats.CurrentHp);

            opposingCombatMapUnit.Stats.CurrentHp -= nAttack;

            HitState hitState;
            bool bOpposingUnitIsEnemy = opposingCombatMapUnit is Enemy;
            // we only add experience to kills against other enemies
            // OR
            // check to see if they are an enemy - if you killed your own PC then we don't give you credit
            if (opposingCombatMapUnit.Stats.CurrentHp > 0)
            {
                hitState = GetState(opposingCombatMapUnit, out string stateOutput);

                if (bOpposingUnitIsEnemy)
                {
                    turnResults.PushTurnResult(new AttackerTurnResult(
                        TurnResult.TurnResultType.Combat_Result_HitAndEnemyReceivedDamage,
                        this, opposingCombatMapUnit, missileType, hitState));
                }
                else
                {
                    turnResults.PushTurnResult(new AttackerTurnResult(
                        TurnResult.TurnResultType.Combat_Result_CombatPlayerReceivedDamage,
                        this, opposingCombatMapUnit, missileType, hitState));
                }

                turnResults.PushOutputToConsole(stateOutput);
                return hitState;
            }

            hitState = GetState(opposingCombatMapUnit, out string opposingEnemyKilledOutput);

            // we know the combat player was attacking and the other guy is dead
            if (bIsEnemyAttacking)
            {
                turnResults.PushOutputToConsole(opposingEnemyKilledOutput);
                turnResults.PushTurnResult(new AttackerTurnResult(
                    TurnResult.TurnResultType.Combat_Result_CombatPlayerKilled,
                    this, opposingCombatMapUnit, missileType, hitState));

                return hitState;
            }

            // was the opponent we killed an enemy?
            // if they are then we get XP and a chance to drop loot
            if (opposingCombatMapUnit is Enemy enemy)
            {
                CombatStats.TotalKills++;
                CombatStats.AdditionalExperience += opposingCombatMapUnit.Experience;

                TileReference.SpriteIndex dropped =
                    OddsAndLogic.GetIsDropAfterKillingEnemy(enemy.EnemyReference);

                nonAttackingUnitDrop = OddsAndLogic.GenerateDropForDeadEnemy(enemy.EnemyReference,
                    // TEMP force to generate chests
                    //NonAttackingUnitFactory.DropSprites.Chest,
                    dropped, opposingCombatMapUnit.MapLocation, opposingCombatMapUnit.MapUnitPosition);

                if (nonAttackingUnitDrop != null) turnResults.PushTurnResult(new LootDropped(nonAttackingUnitDrop));
            }

            turnResults.PushOutputToConsole(opposingEnemyKilledOutput);

            if (bOpposingUnitIsEnemy)
            {
                turnResults.PushTurnResult(new AttackerTurnResult(TurnResult.TurnResultType.Combat_Result_EnemyKilled,
                    this, opposingCombatMapUnit, missileType, hitState));
            }
            else
            {
                turnResults.PushTurnResult(new AttackerTurnResult(
                    TurnResult.TurnResultType.Combat_Result_CombatPlayerKilled,
                    this, opposingCombatMapUnit, missileType, hitState));
            }

            return hitState;
        }

        public HitState Attack(TurnResults.TurnResults turnResults, CombatMapUnit enemyCombatMapUnit, CombatItem weapon,
            out NonAttackingUnit nonAttackingUnitDrop, bool bIsEnemy) =>
            Attack(turnResults, enemyCombatMapUnit, weapon.TheCombatItemReference.AttackStat,
                weapon.TheCombatItemReference.Missile, out nonAttackingUnitDrop, bIsEnemy);

        public bool CanReachForMeleeAttack(CombatMapUnit opponentCombatMapUnit, int nItemRange) =>
            Math.Abs(opponentCombatMapUnit.MapUnitPosition.X - MapUnitPosition.X) <= nItemRange &&
            Math.Abs(opponentCombatMapUnit.MapUnitPosition.Y - MapUnitPosition.Y) <= nItemRange;

        //        /**
        // * Calculate damage for an attack.
        // */
        //        int PartyMember::getDamage() {
        //            int maxDamage;
        //
        //            maxDamage = Weapon::get(player->weapon)->getDamage();
        //            maxDamage += player->str;
        //            if (maxDamage > 255)
        //                maxDamage = 255;
        //
        //            return xu4_random(maxDamage);
        //        }

        //        /**
        // * Determine whether a player's attack hits or not.
        // */
        //        bool PartyMember::attackHit(Creature *m) {
        //            if (!m)
        //                return false;
        //            if (Weapon::get(player->weapon)->alwaysHits() || player->dex >= 40)
        //                return true;
        //
        //            return(m->isHit(player->dex));
        //        }

        // CreatureStatus Creature::getState() const {
        //     int heavy_threshold, light_threshold, crit_threshold;
        //
        //     crit_threshold = basehp >> 2;  /* (basehp / 4) */
        //     heavy_threshold = basehp >> 1; /* (basehp / 2) */
        //     light_threshold = crit_threshold + heavy_threshold;
        //
        //     if (hp <= 0)
        //         return MSTAT_DEAD;
        //     else if (hp < 24)
        //         return MSTAT_FLEEING;
        //     else if (hp < crit_threshold)
        //         return MSTAT_CRITICAL;
        //     else if (hp < heavy_threshold)
        //         return MSTAT_HEAVILYWOUNDED;
        //     else if (hp < light_threshold)
        //         return MSTAT_LIGHTLYWOUNDED;
        //     else
        //         return MSTAT_BARELYWOUNDED;
        //
        // }
    }
}