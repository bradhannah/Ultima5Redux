using System;
using System.Collections.Generic;
using System.Linq;
using Ultima5Redux.Data;
using Ultima5Redux.DayNightMoon;
using Ultima5Redux.Maps;
using Ultima5Redux.MapUnits.Monsters;
using Ultima5Redux.MapUnits.NonPlayerCharacters;
using Ultima5Redux.PlayerCharacters;
using Ultima5Redux.PlayerCharacters.CombatItems;

namespace Ultima5Redux.MapUnits.CombatMapUnits
{
    public abstract class CombatMapUnit : MapUnit
    {
        private readonly Random _random = new Random();

        public abstract CharacterStats Stats { get; }
        
        public abstract int Defense { get; }
        
        public abstract int ClosestAttackRange { get; }

        public abstract string Name { get; }
        
        public abstract int Dexterity { get; }

        public abstract bool IsMyEnemy(CombatMapUnit combatMapUnit);
        
        public CombatMapUnit PreviousAttackTarget { get; private set; }
        
        public bool HasEscaped { get; set; } = false;

        public PlayerCombatStats CombatStats { get; } = new PlayerCombatStats();

        public bool IsCharmed => Stats.Status == PlayerCharacterRecord.CharacterStatus.Charmed;
        
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
        
        public enum HitState { Grazed, Missed, BarelyWounded, LightlyWounded, HeavilyWounded, CriticallyWounded, Fleeing, Dead }

        public HitState Attack(CombatMapUnit enemyCombatMapUnit, int nAttackMax, out string stateOutput)
        {
            bool bIsHit = IsHit(enemyCombatMapUnit);

            PreviousAttackTarget = enemyCombatMapUnit;

            int nAttack = GetAttackDamage(enemyCombatMapUnit, nAttackMax);
            if (!bIsHit)
            {
                stateOutput = FriendlyName + DataOvlRef.StringReferences.GetString(DataOvlReference.BattleStrings._MISSED_BANG_N).TrimEnd().Replace("!"," ")
                              + enemyCombatMapUnit.Name + "!";
                return HitState.Missed;
            }

            if (nAttack == 0)
            {
                stateOutput = FriendlyName + DataOvlRef.StringReferences.GetString(DataOvlReference.BattleStrings._GRAZED_BANG_N).TrimEnd().Replace("!"," ")
                              + enemyCombatMapUnit.Name + "!";
                return HitState.Grazed;
            }

            // we track extra stats
            enemyCombatMapUnit.CombatStats.TotalDamageTaken += Math.Min(nAttack, enemyCombatMapUnit.Stats.CurrentHp);
            CombatStats.TotalDamageGiven += Math.Min(nAttack, enemyCombatMapUnit.Stats.CurrentHp);

            enemyCombatMapUnit.Stats.CurrentHp -= nAttack;

            if (enemyCombatMapUnit.Stats.CurrentHp <= 0)
            {
                CombatStats.TotalKills++;
                CombatStats.AdditionalExperience += ((Enemy) enemyCombatMapUnit).Experience;
            }
            
            return GetState(enemyCombatMapUnit, out stateOutput);
        }

        public HitState Attack(CombatMapUnit enemyCombatMapUnit, CombatItem weapon, out string stateOutput)
        {
            return Attack(enemyCombatMapUnit, weapon.AttackStat, out stateOutput);
        }

        private bool IsHit(CombatMapUnit enemyCombatMapUnit)
        {
            return (enemyCombatMapUnit.Stats.Dexterity + 128) >= (_random.Next() % 256);
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

            int nMaxDamage = weapon?.AttackStat ?? BareHandAttack;

            return GetAttackDamage(enemyCombatMapUnit, nMaxDamage);
        }

        
        public bool CanReachForAttack(CombatMapUnit opponentCombatMapUnit, int nItemRange) =>
            (Math.Abs(opponentCombatMapUnit.MapUnitPosition.X - MapUnitPosition.X) <= nItemRange 
             && Math.Abs(opponentCombatMapUnit.MapUnitPosition.Y - MapUnitPosition.Y) <= nItemRange);

        HitState GetState(CombatMapUnit enemyCombatMapUnit, out string stateOutput)
        {
            int nCriticalThreshold = enemyCombatMapUnit.Stats.MaximumHp >> 2; /* (MaximumHp / 4) */
            int nHeavyThreshold = enemyCombatMapUnit.Stats.MaximumHp >> 1; /* (MaximumHp / 2) */
            int nLightThreshold = nCriticalThreshold + nHeavyThreshold;
            stateOutput = enemyCombatMapUnit.FriendlyName;
            
            //BattleStrings
            if (enemyCombatMapUnit.Stats.CurrentHp <= 0)
            {
                stateOutput += DataOvlRef.StringReferences.GetString(DataOvlReference.BattleStrings._KILLED_BANG_N);
                return HitState.Dead;
            }
            if (enemyCombatMapUnit.Stats.CurrentHp < 24)
            {
                stateOutput += " fleeing!";
                return HitState.Fleeing;
            }
            if (enemyCombatMapUnit.Stats.CurrentHp < nCriticalThreshold)
            {
                stateOutput += DataOvlRef.StringReferences.GetString(DataOvlReference.BattleStrings._CRITICAL_BANG_N);
                return HitState.CriticallyWounded;
            }
            if (enemyCombatMapUnit.Stats.CurrentHp < nHeavyThreshold)
            {
                stateOutput += DataOvlRef.StringReferences.GetString(DataOvlReference.BattleStrings.HEAVILY_WOUNDED_BANG_N);
                return HitState.HeavilyWounded;
            }
            if (enemyCombatMapUnit.Stats.CurrentHp < nLightThreshold)
            {
                stateOutput += DataOvlRef.StringReferences.GetString(DataOvlReference.BattleStrings._LIGHTLY_WOUNDED_BANG_N);
                return HitState.LightlyWounded;
            }
            stateOutput += DataOvlRef.StringReferences.GetString(DataOvlReference.BattleStrings._BARELY_WOUNDED_BANG_N);
            return HitState.BarelyWounded;
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