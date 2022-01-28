using System.Collections.Generic;
using Newtonsoft.Json;
using Ultima5Redux.Properties;
using Ultima5Redux.References.Maps;

namespace Ultima5Redux.References.MapUnits.NonPlayerCharacters
{
    public class EnemyReferences
    {
        private const int N_TOTAL_MONSTERS = 0x30;

        public List<EnemyReference> AllEnemyReferences { get; } = new(N_TOTAL_MONSTERS);

        public EnemyReferences(DataOvlReference dataOvlReference, TileReferences tileReferences)
        {
            AdditionalEnemyFlagList additionalEnemyFlagList = new();

            for (int nMonsterIndex = 0; nMonsterIndex < N_TOTAL_MONSTERS; nMonsterIndex++)
            {
                EnemyReference enemyReference = new(dataOvlReference, tileReferences, nMonsterIndex,
                    additionalEnemyFlagList.AllAdditionalEnemyFlags[nMonsterIndex]);
                AllEnemyReferences.Add(enemyReference);
            }
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

        private sealed class AdditionalEnemyFlagList
        {
            public readonly List<AdditionalEnemyFlags> AllAdditionalEnemyFlags;

            public AdditionalEnemyFlagList()
            {
                AllAdditionalEnemyFlags =
                    JsonConvert.DeserializeObject<List<AdditionalEnemyFlags>>(Resources.AdditionalEnemyFlags);
            }
        }

        [JsonObject(MemberSerialization.OptIn)]
        public class AdditionalEnemyFlags
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