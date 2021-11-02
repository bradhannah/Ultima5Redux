using System;
using Ultima5Redux.Data;
using Ultima5Redux.Maps;
using Ultima5Redux.MapUnits.Monsters;
using Ultima5Redux.MapUnits.NonPlayerCharacters;
using Ultima5Redux.PlayerCharacters;
using Ultima5Redux.PlayerCharacters.CombatItems;

namespace Ultima5Redux.MapUnits.CombatMapUnits
{
    public abstract class CombatMapUnit : MapUnit
    {
        public enum HitState
        {
            Grazed, Missed, BarelyWounded, LightlyWounded, HeavilyWounded, CriticallyWounded, Fleeing, Dead, None
        }

        private readonly Random _random = new Random(Guid.NewGuid().GetHashCode());

        protected CombatMapUnit()
        {
        }

        protected CombatMapUnit(NonPlayerCharacterReference npcRef, MapUnitState mapUnitState,
            SmallMapCharacterState smallMapTheSmallMapCharacterState, MapUnitMovement mapUnitMovement,
            PlayerCharacterRecords playerCharacterRecords, TileReferences tileReferences,
            SmallMapReferences.SingleMapReference.Location location, DataOvlReference dataOvlReference) : base(npcRef,
            mapUnitState,
            smallMapTheSmallMapCharacterState, mapUnitMovement, playerCharacterRecords, tileReferences,
            location, dataOvlReference, Point2D.Direction.None)
        {
        }

        internal HitState CurrentHitState
        {
            get
            {
                int nCriticalThreshold = Stats.MaximumHp >> 2; /* (MaximumHp / 4) */
                int nHeavyThreshold = Stats.MaximumHp >> 1; /* (MaximumHp / 2) */
                int nLightThreshold = nCriticalThreshold + nHeavyThreshold;

                if (Stats.CurrentHp <= 0)
                {
                    return HitState.Dead;
                }

                if (Stats.CurrentHp < 24)
                {
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

                if (Stats.CurrentHp < nLightThreshold)
                {
                    return HitState.LightlyWounded;
                }

                return HitState.BarelyWounded;
            }
        }

        public bool HasEscaped { get; set; } = false;

        public bool IsCharmed => Stats.Status == PlayerCharacterRecord.CharacterStatus.Charmed;
        public abstract bool IsInvisible { get; }
        public bool IsSleeping => Stats.Status == PlayerCharacterRecord.CharacterStatus.Asleep;

        public abstract CharacterStats Stats { get; }

        public CombatMapUnit PreviousAttackTarget { get; private set; }

        public abstract int ClosestAttackRange { get; }

        public abstract int Defense { get; }

        public abstract int Dexterity { get; }

        public abstract int Experience { get; }

        public PlayerCombatStats CombatStats { get; } = new PlayerCombatStats();

        public abstract string Name { get; }
        public abstract string PluralName { get; }

        public abstract string SingularName { get; }

        public abstract bool IsMyEnemy(CombatMapUnit combatMapUnit);

        public HitState Attack(CombatMapUnit enemyCombatMapUnit, int nAttackMax, out string stateOutput,
            out string debugStr, bool bForceHit = false)
        {
            bool bIsHit = IsHit(enemyCombatMapUnit, out debugStr) || bForceHit;

            PreviousAttackTarget = enemyCombatMapUnit;

            int nAttack = GetAttackDamage(enemyCombatMapUnit, nAttackMax);
            if (!bIsHit)
            {
                stateOutput = FriendlyName + DataOvlRef.StringReferences
                                               .GetString(DataOvlReference.BattleStrings._MISSED_BANG_N).TrimEnd()
                                               .Replace("!", " ")
                                           + enemyCombatMapUnit.FriendlyName + "!";
                return HitState.Missed;
            }

            if (nAttack == 0)
            {
                stateOutput = FriendlyName + DataOvlRef.StringReferences
                                               .GetString(DataOvlReference.BattleStrings._GRAZED_BANG_N).TrimEnd()
                                               .Replace("!", " ")
                                           + enemyCombatMapUnit.FriendlyName + "!";
                return HitState.Grazed;
            }

            // we track extra stats
            enemyCombatMapUnit.CombatStats.TotalDamageTaken += Math.Min(nAttack, enemyCombatMapUnit.Stats.CurrentHp);
            CombatStats.TotalDamageGiven += Math.Min(nAttack, enemyCombatMapUnit.Stats.CurrentHp);

            enemyCombatMapUnit.Stats.CurrentHp -= nAttack;

            // we only add experience to kills against other enemies
            if (enemyCombatMapUnit.Stats.CurrentHp <= 0)
            {
                CombatStats.TotalKills++;
                CombatStats.AdditionalExperience += enemyCombatMapUnit.Experience;
            }

            return GetState(enemyCombatMapUnit, out stateOutput);
        }

        public HitState Attack(CombatMapUnit enemyCombatMapUnit, CombatItem weapon, out string stateOutput,
            out string debugStr)
        {
            return Attack(enemyCombatMapUnit, weapon.TheCombatItemReference.AttackStat, out stateOutput, out debugStr);
        }

        private bool IsHit(CombatMapUnit enemyCombatMapUnit, out string debugStr)
        {
            const int nHitOffset = 128;
            int randomNum = _random.Next(255); // % 256;
            bool bWasHit = (enemyCombatMapUnit.Stats.Dexterity + nHitOffset) >= randomNum;
            debugStr =
                $"Ran:{randomNum} Dex:{enemyCombatMapUnit.Stats.Dexterity} Dex+128:{enemyCombatMapUnit.Stats.Dexterity + 128} Hit:{bWasHit}";
            return bWasHit;
        }

        // bool Creature::isHit(int hit_offset) {
        //     return (hit_offset + 128) >= xu4_random(0x100) ? true : false;
        // }

        private int GetAttackDamage(CombatMapUnit enemyCombatMapUnit, int nMaxDamage)
        {
            // start with the weapons attack value
            //int nMaxDamage = weapon?.AttackStat ?? BareHandAttack;
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

        private int GetAttackDamage(CombatMapUnit enemyCombatMapUnit, CombatItem weapon)
        {
            const int BareHandAttack = 3;

            int nMaxDamage = weapon?.TheCombatItemReference.AttackStat ?? BareHandAttack;

            return GetAttackDamage(enemyCombatMapUnit, nMaxDamage);
        }


        public bool CanReachForMeleeAttack(CombatMapUnit opponentCombatMapUnit, int nItemRange) =>
            (Math.Abs(opponentCombatMapUnit.MapUnitPosition.X - MapUnitPosition.X) <= nItemRange
             && Math.Abs(opponentCombatMapUnit.MapUnitPosition.Y - MapUnitPosition.Y) <= nItemRange);

        public HitState GetState(CombatMapUnit enemyCombatMapUnit, out string stateOutput)
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
                        DataOvlRef.StringReferences.GetString(DataOvlReference.BattleStrings._BARELY_WOUNDED_BANG_N);
                    break;
                case HitState.LightlyWounded:
                    stateOutput +=
                        DataOvlRef.StringReferences.GetString(DataOvlReference.BattleStrings._LIGHTLY_WOUNDED_BANG_N);
                    break;
                case HitState.HeavilyWounded:
                    stateOutput +=
                        DataOvlRef.StringReferences.GetString(DataOvlReference.BattleStrings.HEAVILY_WOUNDED_BANG_N);
                    break;
                case HitState.CriticallyWounded:
                    stateOutput +=
                        DataOvlRef.StringReferences.GetString(DataOvlReference.BattleStrings._CRITICAL_BANG_N);
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
                            DataOvlRef.StringReferences.GetString(DataOvlReference.BattleStrings._CRITICAL_BANG_N);
                    }

                    break;
                case HitState.Dead:
                    stateOutput += DataOvlRef.StringReferences.GetString(DataOvlReference.BattleStrings._KILLED_BANG_N);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            return enemyHitState;
        }

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