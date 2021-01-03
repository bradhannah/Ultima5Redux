using System;
using Ultima5Redux.Data;
using Ultima5Redux.DayNightMoon;
using Ultima5Redux.Maps;
using Ultima5Redux.MapUnits.NonPlayerCharacters;
using Ultima5Redux.PlayerCharacters;
using Ultima5Redux.PlayerCharacters.CombatItems;

namespace Ultima5Redux.MapUnits.CombatMapUnits
{
    public abstract class CombatMapUnit : MapUnit
    {
        public abstract CharacterStats Stats { get; }

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
        
        public enum HitState { Missed, BarelyWounded, LightlyWounded, HeavilyWounded, CriticallyWounded, Fleeing, Dead }
        
        public HitState Attack(CombatMapUnit enemyCombatMapUnit, CombatItem weapon)
        {
            const int BareHandAttack = 3;
            bool bIsHit = IsHit(enemyCombatMapUnit);

            int nAttack = GetAttackDamage(enemyCombatMapUnit, weapon);
            if (!bIsHit || nAttack == 0) return HitState.Missed;

            enemyCombatMapUnit.Stats.CurrentHp -= nAttack;
            
            return GetState(enemyCombatMapUnit);
        }

        private bool IsHit(CombatMapUnit enemyCombatMapUnit)
        {
            Random ran = new Random();
            return (enemyCombatMapUnit.Stats.Dexterity + 128) >= (ran.Next() % 256);
        }
        
        // bool Creature::isHit(int hit_offset) {
        //     return (hit_offset + 128) >= xu4_random(0x100) ? true : false;
        // }

        private int GetAttackDamage(CombatMapUnit enemyCombatMapUnit, CombatItem weapon)
        {
            int nMaxDamage = weapon.AttackStat;
            nMaxDamage += Stats.Strength;
            nMaxDamage = Math.Min(nMaxDamage, 255);
            return 0;
        }

        HitState GetState(CombatMapUnit enemyCombatMapUnit)
        {
            int nCriticalThreshold = enemyCombatMapUnit.Stats.MaximumHp >> 2; /* (MaximumHp / 4) */
            int nHeavyThreshold = enemyCombatMapUnit.Stats.MaximumHp >> 1; /* (MaximumHp / 2) */
            int nLightThreshold = nCriticalThreshold + nHeavyThreshold;

            if (enemyCombatMapUnit.Stats.CurrentHp <= 0)
                return HitState.Dead;
            if (enemyCombatMapUnit.Stats.CurrentHp < 24)
                return HitState.Fleeing;
            if (enemyCombatMapUnit.Stats.CurrentHp < nCriticalThreshold)
                return HitState.CriticallyWounded;
            if (enemyCombatMapUnit.Stats.CurrentHp < nHeavyThreshold)
                return HitState.HeavilyWounded;
            if (enemyCombatMapUnit.Stats.CurrentHp < nLightThreshold)
                return HitState.LightlyWounded;
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