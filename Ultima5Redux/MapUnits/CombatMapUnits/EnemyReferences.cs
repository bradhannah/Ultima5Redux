using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Ultima5Redux.Data;
using Ultima5Redux.Maps;
using Ultima5Redux.Properties;

namespace Ultima5Redux.MapUnits.CombatMapUnits
{
    public class EnemyReferences
    {
        private const int N_TOTAL_MONSTERS = 0x30;

        public List<EnemyReference> AllEnemyReferences { get; } = new List<EnemyReference>(N_TOTAL_MONSTERS);

        public EnemyReferences(DataOvlReference dataOvlReference, TileReferences tileReferences)
        {
            AdditionalEnemyFlagList additionalEnemyFlagList = new AdditionalEnemyFlagList();

            for (int nMonsterIndex = 0; nMonsterIndex < N_TOTAL_MONSTERS; nMonsterIndex++)
            {
                EnemyReference enemyReference = new EnemyReference(dataOvlReference, tileReferences, nMonsterIndex,
                    additionalEnemyFlagList.AllAdditionalEnemyFlags[nMonsterIndex]);
                AllEnemyReferences.Add(enemyReference);
            }

            //PrintDebugCSV();
        }

        public EnemyReference GetEnemyReference(TileReference tileReference)
        {
            int nIndex = (tileReference.Index - EnemyReference.N_FIRST_SPRITE) / EnemyReference.N_FRAMES_PER_SPRITE;
            return AllEnemyReferences[nIndex];
        }

        public EnemyReference GetEnemyReference(int nSprite)
        {
            int nIndex = (nSprite - EnemyReference.N_FIRST_SPRITE) / EnemyReference.N_FRAMES_PER_SPRITE;
            return AllEnemyReferences[nIndex];
        }

        public EnemyReference GetFriendReference(EnemyReference enemyReference)
        {
            return AllEnemyReferences[enemyReference.FriendIndex];
        }

        // ReSharper disable once UnusedMember.Local
        private void PrintDebugCsv()
        {
            Console.Write(@"Name,Plural,Singular,ClosestAttackRange,MissileType,Friend,Thing");
            //var dExampleBitfield = 0x8000;
            foreach (EnemyReference.EnemyAbility ability in Enum.GetValues(typeof(EnemyReference.EnemyAbility)))
            {
                Console.Write(@"," + ability);
            }

            Console.WriteLine();

            foreach (EnemyReference enemy in AllEnemyReferences)
            {
                Console.Write(enemy.KeyTileReference.Name + @"," + enemy.AllCapsPluralName + @"," +
                              enemy.MixedCaseSingularName + @"," + $@"0x{enemy.AttackRange:X2}");
                Console.Write(@"," + $@"{enemy.TheMissileType}");
                EnemyReference friend = AllEnemyReferences[enemy.FriendIndex];
                Console.Write(@"," + $@"{friend.AllCapsPluralName}");
                foreach (EnemyReference.EnemyAbility ability in Enum.GetValues(typeof(EnemyReference.EnemyAbility)))
                {
                    Console.Write(@"," + enemy.IsEnemyAbility(ability));
                }

                Console.WriteLine();
            }
        }

        private class AdditionalEnemyFlagList
        {
            public readonly List<AdditionalEnemyFlags> AllAdditionalEnemyFlags;

            public AdditionalEnemyFlagList()
            {
                AllAdditionalEnemyFlags =
                    JsonConvert.DeserializeObject<List<AdditionalEnemyFlags>>(Resources.AdditionalEnemyFlags);
            }
        }

        [JsonObject(MemberSerialization.OptIn)] public class AdditionalEnemyFlags
        {
            [JsonProperty] public bool ActivelyAttacks { get; set; }
            [JsonProperty] public bool CanFlyOverWater { get; set; }
            [JsonProperty] public bool CanPassThroughWalls { get; set; }
            [JsonProperty] public bool DoNotMove { get; set; }
            [JsonProperty] public int Experience { get; set; }
            [JsonProperty] public bool IsWaterEnemy { get; set; }
            [JsonProperty] public string Name { get; set; }
        }
    }
}