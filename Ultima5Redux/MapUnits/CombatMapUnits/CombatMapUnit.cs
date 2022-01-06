using System;
using System.Runtime.Serialization;
using Newtonsoft.Json;
using Ultima5Redux.MapUnits.NonPlayerCharacters;
using Ultima5Redux.PlayerCharacters;
using Ultima5Redux.PlayerCharacters.CombatItems;
using Ultima5Redux.References;
using Ultima5Redux.References.Maps;

namespace Ultima5Redux.MapUnits.CombatMapUnits
{
    public abstract class CombatMapUnit : MapUnit
    {
        public enum HitState
        {
            Grazed, Missed, BarelyWounded, LightlyWounded, HeavilyWounded, CriticallyWounded, Fleeing, Dead, None
        }

        [IgnoreDataMember]
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

        [IgnoreDataMember] private readonly Random _random = new(Guid.NewGuid().GetHashCode());

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

        [JsonConstructor] protected CombatMapUnit()
        {
        }

        protected CombatMapUnit(SmallMapCharacterState smallMapTheSmallMapCharacterState,
            MapUnitMovement mapUnitMovement, SmallMapReferences.SingleMapReference.Location location,
            NonPlayerCharacterState npcState, TileReference tileReference) : base(smallMapTheSmallMapCharacterState,
            mapUnitMovement, location, Point2D.Direction.None, npcState, tileReference, new MapUnitPosition())
        {
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

        // ReSharper disable once UnusedMember.Local
        private int GetAttackDamage(CombatMapUnit enemyCombatMapUnit, CombatItem weapon)
        {
            const int BareHandAttack = 3;

            int nMaxDamage = weapon?.TheCombatItemReference.AttackStat ?? BareHandAttack;

            return GetAttackDamage(enemyCombatMapUnit, nMaxDamage);
        }

        private bool IsHit(CombatMapUnit enemyCombatMapUnit, out string debugStr)
        {
            const int nHitOffset = 128;
            int randomNum = _random.Next(255);
            bool bWasHit = (enemyCombatMapUnit.Stats.Dexterity + nHitOffset) >= randomNum;
            debugStr =
                $"Ran:{randomNum} Dex:{enemyCombatMapUnit.Stats.Dexterity} Dex+128:{enemyCombatMapUnit.Stats.Dexterity + 128} Hit:{bWasHit}";
            return bWasHit;
        }

        public abstract bool IsMyEnemy(CombatMapUnit combatMapUnit);

        public HitState Attack(CombatMapUnit enemyCombatMapUnit, int nAttackMax, out string stateOutput,
            out string debugStr, bool bForceHit = false)
        {
            bool bIsHit = IsHit(enemyCombatMapUnit, out debugStr) || bForceHit;

            PreviousAttackTarget = enemyCombatMapUnit;

            int nAttack = GetAttackDamage(enemyCombatMapUnit, nAttackMax);
            if (!bIsHit)
            {
                stateOutput = FriendlyName + GameReferences.DataOvlRef.StringReferences
                                  .GetString(DataOvlReference.BattleStrings._MISSED_BANG_N).TrimEnd()
                                  .Replace("!", " ") +
                              enemyCombatMapUnit.FriendlyName + "!";
                return HitState.Missed;
            }

            if (nAttack == 0)
            {
                stateOutput = FriendlyName + GameReferences.DataOvlRef.StringReferences
                                  .GetString(DataOvlReference.BattleStrings._GRAZED_BANG_N).TrimEnd()
                                  .Replace("!", " ") +
                              enemyCombatMapUnit.FriendlyName + "!";
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

        public bool CanReachForMeleeAttack(CombatMapUnit opponentCombatMapUnit, int nItemRange) =>
            (Math.Abs(opponentCombatMapUnit.MapUnitPosition.X - MapUnitPosition.X) <= nItemRange &&
             Math.Abs(opponentCombatMapUnit.MapUnitPosition.Y - MapUnitPosition.Y) <= nItemRange);

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
                        GameReferences.DataOvlRef.StringReferences.GetString(DataOvlReference.BattleStrings
                            ._BARELY_WOUNDED_BANG_N);
                    break;
                case HitState.LightlyWounded:
                    stateOutput +=
                        GameReferences.DataOvlRef.StringReferences.GetString(DataOvlReference.BattleStrings
                            ._LIGHTLY_WOUNDED_BANG_N);
                    break;
                case HitState.HeavilyWounded:
                    stateOutput +=
                        GameReferences.DataOvlRef.StringReferences.GetString(DataOvlReference.BattleStrings
                            .HEAVILY_WOUNDED_BANG_N);
                    break;
                case HitState.CriticallyWounded:
                    stateOutput +=
                        GameReferences.DataOvlRef.StringReferences.GetString(DataOvlReference.BattleStrings
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
                            GameReferences.DataOvlRef.StringReferences.GetString(DataOvlReference.BattleStrings
                                ._CRITICAL_BANG_N);
                    }

                    break;
                case HitState.Dead:
                    stateOutput +=
                        GameReferences.DataOvlRef.StringReferences.GetString(DataOvlReference.BattleStrings
                            ._KILLED_BANG_N);
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