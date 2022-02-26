using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using Newtonsoft.Json;
using Ultima5Redux.Properties;
using Ultima5Redux.References.Maps;
using Ultima5Redux.References.PlayerCharacters.Inventory;

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

        internal EnemyReference GetRandomEnemyReferenceByEraAndTile(int nTurn, TileReference tileReference)
        {
            List<EnemyReference> possibleEnemies =
                AllEnemyReferences.Where(e => e.GetEraWeightByTurn(nTurn) > 0).ToList();

            // if 0, then no possible enemies based on era
            if (possibleEnemies.Count == 0)
                throw new Ultima5ReduxException("You should always have more than zero enemies to fight in each era");

            List<EnemyReference> enemiesThatCanGoOnTile =
                possibleEnemies.Where(e => e.CanGoOnTile(tileReference)).ToList();

            // no enemies are able to go on that tile
            if (enemiesThatCanGoOnTile.Count == 0) return null;

            return enemiesThatCanGoOnTile[Utils.GetNumberFromAndTo(0, enemiesThatCanGoOnTile.Count - 1)];
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

        [DataContract] public class AdditionalEnemyFlags
        {
            [DataMember] public bool ActivelyAttacks { get; set; }
            [DataMember] public bool CanFlyOverWater { get; set; }
            [DataMember] public bool CanPassThroughWalls { get; set; }
            [DataMember] public bool DoNotMove { get; set; }
            [DataMember] public int Experience { get; set; }
            [DataMember] public bool IsWaterEnemy { get; set; }
            [DataMember] public string Name { get; set; }
            [DataMember] public int Era1Weight { get; set; }
            [DataMember] public int Era2Weight { get; set; }
            [DataMember] public int Era3Weight { get; set; }
            [DataMember] public bool IsSandEnemy { get; set; }
            [DataMember] public CombatItemReference.MissileType LargeMapMissile { get; set; }
            [DataMember] public int LargeMapMissileRange { get; set; }
        }
    }
}