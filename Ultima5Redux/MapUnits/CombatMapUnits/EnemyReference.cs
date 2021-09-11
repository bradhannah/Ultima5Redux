using System;
using System.Collections.Generic;
using Ultima5Redux.Data;
using Ultima5Redux.Maps;
using Ultima5Redux.PlayerCharacters;
using Ultima5Redux.PlayerCharacters.CombatItems;

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

        public bool IsNpc => KeyTileReference.IsNPC;
        
        public TileReference KeyTileReference { get; }

        public DefaultEnemyStats TheDefaultEnemyStats { get; }

        public string AllCapsPluralName { get; private set; }
        public string MixedCaseSingularName { get; private set; }

        public override string ToString()
        {
            return AllCapsPluralName + "-" + MixedCaseSingularName;
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
            Bludgeons = 0, PossessCharm, Undead, DivideOnHit, Immortal, PoisonAtRange, StealsFood, NoCorpse, RangedMagic,
            Teleport, DisappearsOnDeath, Invisibility, GatesInDaemon, Poison, InfectWithPlague
        }

        private List<int> _enemyExp = new List<int>()
        {
            3, // Wizard1/WIZARDS
            4,// Bard1/BARD
            6,// Fighter1/FIGHTER
            0,// Avatar1/x
            3,// TownsPerson1/VILLAGER
            3,// Merchant1/MERCHANT
            3,// Jester1/JESTER
            3,// BardPlaying1/BARD
            0,// PersonStocks1/PIRATES
            0,// WallPrisoner1/x
            2,// Child1/CHILD
            2,// Begger1/BEGGAR
            25,// Guard1/GUARDS
            0, // Apparation1/x
            0,// Blackthorn1/BLACKTHORN
            0,// LordBritish1/LORD BRITISH
            8,// Seahorse1/SEA HORSES
            13,// Squid1/SQUIDS
            18,// SeaSerpent1/SEA SERPENTS
            6,// Shark1/SHARKS
            3,// Rat1/GIANT RATS
            2,// Bat1/BATS
            3,// Spider1/SPIDERS
            6,// Ghost1/GHOSTS
            3,// Slime1/SLIME
            3,// Gremlin1/GREMLINS
            8,// Mimic1/MIMICS
            11,// Reaper1/REAPERS
            6,// Gazer1/GAZERS
            0,// Shard/x
            11,// StoneGargoyle1/GARGOYLE
            2,// InsectSwarm1/INSECTS
            3,// Orc1/ORCS
            6,// Skeleton1/SKELETONS
            3,// Snake1/SNAKES
            8,// Ettin1/ETTINS
            6,// Headless1/HEADLESSES
            11,// Wisp1/WISPS
            13,// Daemon1/DAEMONS
            25,// Dragon1/DRAGONS
            21,// SandTrap1/SAND TRAPS
            4,// Troll1/TROLLS
            0,// PoisonField/x
            0,// Whirpool1/x
            6,// MongBat1/MONGBATS
            11,// Corpser1/CORPSERS
            2,// RotWork1/ROTWORMS
            25// ShadowLord1/SHADOW LORD
        };

        private readonly Dictionary<EnemyAbility, bool> _enemyAbilities = new Dictionary<EnemyAbility, bool>();  

        public int AttackRange { get; }
        public CombatItem.MissileType TheMissileType { get; }
        public int FriendIndex { get; }

        public int Experience => _enemyExp[_monsterIndex]; 
        
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
                TheMissileType = CombatItem.MissileType.None; 
            else
                TheMissileType = (CombatItem.MissileType)dataOvlReference.GetDataChunk(DataOvlReference.DataChunkName.ENEMY_RANGE_THING)
                    .GetByte(nMonsterIndex);
            
            // the friend index technically starts at the Mage, and skips 4 animations frames per enemy
            //const int StartFriendIndex = 320;
            FriendIndex =
                dataOvlReference.GetDataChunk(DataOvlReference.DataChunkName.ENEMY_FRIENDS).GetByte(nMonsterIndex);
            
            _nThing = dataOvlReference.GetDataChunk(DataOvlReference.DataChunkName.ENEMY_THING).GetByte(nMonsterIndex);

            TheDefaultEnemyStats = new DefaultEnemyStats
            {
                Strength = GetStat(DefaultStats.Strength, dataOvlReference, nMonsterIndex),
                Dexterity = GetStat(DefaultStats.Dexterity, dataOvlReference, nMonsterIndex),
                Intelligence = GetStat(DefaultStats.Intelligence, dataOvlReference, nMonsterIndex),
                Armour = GetStat(DefaultStats.Armour, dataOvlReference, nMonsterIndex),
                Damage = GetStat(DefaultStats.Damage, dataOvlReference, nMonsterIndex),
                HitPoints = 40,//GetStat(DefaultStats.Hitpoints, dataOvlReference, nMonsterIndex),
                MaxPerMap = GetStat(DefaultStats.MaxPerMap, dataOvlReference, nMonsterIndex),
                TreasureNumber = GetStat(DefaultStats.Treasure, dataOvlReference, nMonsterIndex)
            };

            AllCapsPluralName = dataOvlReference.StringReferences.GetString((DataOvlReference.EnemyOutOfCombatNamesUpper)nMonsterIndex);
            
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
                MixedCaseSingularName = dataOvlReference.StringReferences.GetString((DataOvlReference.EnemyIndividualNamesMixed)nMixedCaseIndex);
            }

            int nKeySpriteIndex = N_FIRST_SPRITE + (nMonsterIndex * N_FRAMES_PER_SPRITE);
            KeyTileReference = tileReferences.GetTileReferenceOfKeyIndex(nKeySpriteIndex);
        }

        private int GetStat(DefaultStats stat, DataOvlReference dataOvlReference, int nMonsterIndex)
        {
            const int TotalBytesPerRecord = 8;
            return dataOvlReference.GetDataChunk(DataOvlReference.DataChunkName.ENEMY_STATS).GetByte(nMonsterIndex * TotalBytesPerRecord + (int) stat);
        }

        public bool IsWaterEnemy => KeyTileReference.Index >= 384 && KeyTileReference.Index < 400;
    }
}