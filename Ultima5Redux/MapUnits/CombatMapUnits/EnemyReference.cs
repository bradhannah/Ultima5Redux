using System;
using System.Collections.Generic;
using Ultima5Redux.Data;
using Ultima5Redux.Maps;
using Ultima5Redux.PlayerCharacters;

namespace Ultima5Redux.MapUnits.CombatMapUnits
{
    public class EnemyReference
    {
        public class DefaultEnemyStats
        {
            public int Strength { get; internal set; }
            public int Dexterity { get; internal set; }
            public int Intelligence { get; internal set;}
            public int Armour { get; internal set; }
            public int Damage { get; internal set; }
            public int HitPoints { get; internal set; }
            public int MaxPerMap { get; internal set; }
            public int TreasureNumber { get; internal set; }
        } 
        
        private readonly TileReferences _tileReferences;
        private readonly int _monsterIndex;
        internal const int N_FIRST_SPRITE = 320;
        internal const int N_FRAMES_PER_SPRITE = 4;

        private const int N_RAW_BYTES = 2;

        public string GroupName { get; }
        public TileReference KeyTileReference { get; }

        public DefaultEnemyStats TheDefaultEnemyStats { get; }

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
            Bludgeons = 0, PossessCharm, Undead, DivideOnHit, Immortal, PoisonAtRange, StealsFood, NoCorpse, RangedMagic,
            Teleport, DisappearsOnDeath, Invisibility, GatesInDaemon, Poison, InfectWithPlague
        }

        public enum MissileType
        {
            None = -1, Arrow = 0, CannonBall, Axe, Red, Blue, Green, Violet, Rock
        }
        
        private readonly Dictionary<EnemyAbility, bool> _enemyAbilities = new Dictionary<EnemyAbility, bool>();  

        public int AttackRange { get; }
        public MissileType TheMissileType { get; }
        public int FriendIndex { get; }
        
        internal byte _nThing;

        private enum DefaultStats
        {
            Strength = 0, Dexterity, Intelligence, Armour, Damage, Hitpoints, MaxPerMap,
            Treasure
        }

        public bool IsEnemyAbility(EnemyAbility ability)
        {
            return (_enemyAbilities.ContainsKey(ability) && _enemyAbilities[ability]);
        }
        
        public EnemyReference(DataOvlReference dataOvlReference, TileReferences tileReferences, int nMonsterIndex)
        {
            _tileReferences = tileReferences;
            _monsterIndex = nMonsterIndex;

            List<bool> enemyFlags = dataOvlReference.GetDataChunk(DataOvlReference.DataChunkName.ENEMY_FLAGS).
                GetAsBitmapBoolList(nMonsterIndex * N_RAW_BYTES, N_RAW_BYTES);
            foreach (EnemyAbility ability in Enum.GetValues(typeof(EnemyAbility)))
            {
                if (enemyFlags[(int) ability]) { _enemyAbilities.Add(ability, true); }
            }
            
            AttackRange =
                dataOvlReference.GetDataChunk(DataOvlReference.DataChunkName.ENEMY_ATTACK_RANGE).GetByte(nMonsterIndex);
            
            if (AttackRange == 1) 
                TheMissileType = MissileType.None; 
            else
                TheMissileType = (MissileType)dataOvlReference.GetDataChunk(DataOvlReference.DataChunkName.ENEMY_RANGE_THING)
                    .GetByte(nMonsterIndex);
            
            FriendIndex = dataOvlReference.GetDataChunk(DataOvlReference.DataChunkName.ENEMY_FRIENDS).GetByte(nMonsterIndex);
            
            _nThing = dataOvlReference.GetDataChunk(DataOvlReference.DataChunkName.ENEMY_THING).GetByte(nMonsterIndex);

            TheDefaultEnemyStats = new DefaultEnemyStats
            {
                Strength = GetStat(DefaultStats.Strength, dataOvlReference, nMonsterIndex),
                Dexterity = GetStat(DefaultStats.Dexterity, dataOvlReference, nMonsterIndex),
                Intelligence = GetStat(DefaultStats.Intelligence, dataOvlReference, nMonsterIndex),
                Armour = GetStat(DefaultStats.Armour, dataOvlReference, nMonsterIndex),
                Damage = GetStat(DefaultStats.Damage, dataOvlReference, nMonsterIndex),
                HitPoints = GetStat(DefaultStats.Hitpoints, dataOvlReference, nMonsterIndex),
                MaxPerMap = GetStat(DefaultStats.MaxPerMap, dataOvlReference, nMonsterIndex),
                TreasureNumber = GetStat(DefaultStats.Treasure, dataOvlReference, nMonsterIndex)
            };

            GroupName = dataOvlReference.StringReferences.GetString((DataOvlReference.EnemyOutOfCombatNamesUpper)nMonsterIndex);
            int nKeySpriteIndex = N_FIRST_SPRITE + (nMonsterIndex * N_FRAMES_PER_SPRITE);
            KeyTileReference = tileReferences.GetTileReferenceOfKeyIndex(nKeySpriteIndex);
        }

        private int GetStat(DefaultStats stat, DataOvlReference dataOvlReference, int nMonsterIndex)
        {
            const int TotalBytesPerRecord = 8;
            return dataOvlReference.GetDataChunk(DataOvlReference.DataChunkName.ENEMY_STATS).GetByte(nMonsterIndex * TotalBytesPerRecord + (int) stat);
        }
    }
}