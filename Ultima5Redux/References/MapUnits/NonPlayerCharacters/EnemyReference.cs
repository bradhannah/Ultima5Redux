﻿using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Ultima5Redux.References.Maps;
using Ultima5Redux.References.PlayerCharacters.Inventory;

namespace Ultima5Redux.References.MapUnits.NonPlayerCharacters
{
    public class EnemyReference
    {
        private enum DefaultStats
        {
            // ReSharper disable once UnusedMember.Local
            Strength = 0, Dexterity, Intelligence, Armour, Damage, Hitpoints, MaxPerMap, Treasure
        }

        // 0x8000 - Bludgeons (I)
        // 0x4000 - Possesses (Charm) (J) 
        // 0x2000 - Undead (K)
        // 0x1000 - Divide on hit (L)
        // 0x0800 - Immortal (M)
        // 0x0400 - Poison at Range (R)
        // 0x0200 - Steals Food (O)
        // 0x0100 - No Corpse (P)
        // 0x0080 - ranged - magic bolt (A)
        // 0x0040 - ranged attacks (B)
        // 0x0020 - Teleport (C)
        // 0x0010 - Disappears on Death (D)
        // 0x0008 - Invisibility (E)
        // 0x0004 - Gates in Daemons (F)
        // 0x0002 - Poisons (G)
        // 0x0001 - Infect with Plague (H) - poison melee? 
        public enum EnemyAbility
        {
            Bludgeons = 0, PossessCharm, Undead, DivideOnHit, Immortal, PoisonAtRange, StealsFood, NoCorpse,
            RangedMagic, Teleport, DisappearsOnDeath, Invisibility, GatesInDaemon, Poison, InfectWithPlague
        }

        internal const int N_FIRST_SPRITE = 320;
        internal const int N_FRAMES_PER_SPRITE = 4;

        private const int N_RAW_BYTES = 2;
        private readonly EnemyReferences.AdditionalEnemyFlags _additionalEnemyFlags;

        private readonly Dictionary<EnemyAbility, bool> _enemyAbilities = new Dictionary<EnemyAbility, bool>();

        public bool ActivelyAttacks => _additionalEnemyFlags.ActivelyAttacks;

        public string AllCapsPluralName { get; }

        public int AttackRange { get; }
        public bool CanFlyOverWater => _additionalEnemyFlags.CanFlyOverWater;
        public bool CanPassThroughWalls => _additionalEnemyFlags.CanPassThroughWalls;

        public bool DoesNotMove => _additionalEnemyFlags.DoNotMove;
        public int Experience => _additionalEnemyFlags.Experience; //_enemyExp[_monsterIndex]; 
        public int FriendIndex { get; }

        public bool IsNpc => KeyTileReference.IsNPC;

        public bool IsWaterEnemy =>
            _additionalEnemyFlags.IsWaterEnemy; //KeyTileReference.Index >= 384 && KeyTileReference.Index < 400;

        public TileReference KeyTileReference { get; }
        public string MixedCaseSingularName { get; }

        public DefaultEnemyStats TheDefaultEnemyStats { get; }
        public CombatItemReference.MissileType TheMissileType { get; }

        public EnemyReference(DataOvlReference dataOvlReference, TileReferences tileReferences, int nMonsterIndex,
            EnemyReferences.AdditionalEnemyFlags additionalEnemyFlags)
        {
            _additionalEnemyFlags = additionalEnemyFlags;

            List<bool> enemyFlags = dataOvlReference.GetDataChunk(DataOvlReference.DataChunkName.ENEMY_FLAGS)
                .GetAsBitmapBoolList(nMonsterIndex * N_RAW_BYTES, N_RAW_BYTES);
            foreach (EnemyAbility ability in Enum.GetValues(typeof(EnemyAbility)))
            {
                if (enemyFlags[(int)ability])
                {
                    _enemyAbilities.Add(ability, true);
                }
            }

            AttackRange = dataOvlReference.GetDataChunk(DataOvlReference.DataChunkName.ENEMY_ATTACK_RANGE)
                .GetByte(nMonsterIndex);

            if (AttackRange == 1)
                TheMissileType = CombatItemReference.MissileType.None;
            else
                TheMissileType = (CombatItemReference.MissileType)dataOvlReference
                    .GetDataChunk(DataOvlReference.DataChunkName.ENEMY_RANGE_THING).GetByte(nMonsterIndex);

            // the friend index technically starts at the Mage, and skips 4 animations frames per enemy
            //const int StartFriendIndex = 320;
            FriendIndex = dataOvlReference.GetDataChunk(DataOvlReference.DataChunkName.ENEMY_FRIENDS)
                .GetByte(nMonsterIndex);

            dataOvlReference.GetDataChunk(DataOvlReference.DataChunkName.ENEMY_THING).GetByte(nMonsterIndex);

            TheDefaultEnemyStats = new DefaultEnemyStats
            {
                Strength = GetStat(DefaultStats.Strength, dataOvlReference, nMonsterIndex),
                Dexterity = GetStat(DefaultStats.Dexterity, dataOvlReference, nMonsterIndex),
                Intelligence = GetStat(DefaultStats.Intelligence, dataOvlReference, nMonsterIndex),
                Armour = GetStat(DefaultStats.Armour, dataOvlReference, nMonsterIndex),
                Damage = GetStat(DefaultStats.Damage, dataOvlReference, nMonsterIndex),
                // bajh: TEMPORARY so I can test and not kill them immediately
                HitPoints = 40, //GetStat(DefaultStats.Hitpoints, dataOvlReference, nMonsterIndex),
                MaxPerMap = GetStat(DefaultStats.MaxPerMap, dataOvlReference, nMonsterIndex),
                TreasureNumber = GetStat(DefaultStats.Treasure, dataOvlReference, nMonsterIndex)
            };

            AllCapsPluralName =
                dataOvlReference.StringReferences.GetString((DataOvlReference.EnemyOutOfCombatNamesUpper)nMonsterIndex);

            // the following is a super gross hack to account for the fact that they leave 4 of singular names out of the ordered list
            int nMixedCaseIndex = nMonsterIndex;
            if (nMonsterIndex > 8) nMixedCaseIndex += -2;
            if (nMonsterIndex > 41) nMixedCaseIndex += -2;

            if (nMonsterIndex == 8 || nMonsterIndex == 9 || nMonsterIndex == 42 || nMonsterIndex == 43)
            {
                MixedCaseSingularName = "x";
            }
            else
            {
                MixedCaseSingularName =
                    dataOvlReference.StringReferences.GetString(
                        (DataOvlReference.EnemyIndividualNamesMixed)nMixedCaseIndex);
            }

            int nKeySpriteIndex = N_FIRST_SPRITE + (nMonsterIndex * N_FRAMES_PER_SPRITE);
            KeyTileReference = tileReferences.GetTileReferenceOfKeyIndex(nKeySpriteIndex);
        }

        public override string ToString()
        {
            return AllCapsPluralName + "-" + MixedCaseSingularName;
        }

        private int GetStat(DefaultStats stat, DataOvlReference dataOvlReference, int nMonsterIndex)
        {
            const int TotalBytesPerRecord = 8;
            return dataOvlReference.GetDataChunk(DataOvlReference.DataChunkName.ENEMY_STATS)
                .GetByte(nMonsterIndex * TotalBytesPerRecord + (int)stat);
        }

        public bool IsEnemyAbility(EnemyAbility ability)
        {
            return (_enemyAbilities.ContainsKey(ability) && _enemyAbilities[ability]);
        }

        [SuppressMessage("ReSharper", "UnusedAutoPropertyAccessor.Global")] public class DefaultEnemyStats
        {
            public int Armour { get; internal set; }
            public int Damage { get; internal set; }
            public int Dexterity { get; internal set; }
            public int HitPoints { get; internal set; }
            public int Intelligence { get; internal set; }
            public int MaxPerMap { get; internal set; }
            public int Strength { get; internal set; }
            public int TreasureNumber { get; internal set; }
        }
    }
}