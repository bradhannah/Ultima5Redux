using System;
using System.Collections.Generic;
using Ultima5Redux.Data;
using Ultima5Redux.Maps;

namespace Ultima5Redux.MapUnits.CombatMapUnits
{
    public class EnemyReferences
    {
        private const int N_TOTAL_MONSTERS = 0x30;

        public List<EnemyReference> AllEnemyReferences { get; } = new List<EnemyReference>(N_TOTAL_MONSTERS);

        public EnemyReferences(DataOvlReference dataOvlReference, TileReferences tileReferences)
        {
            for (int nMonsterIndex = 0; nMonsterIndex < N_TOTAL_MONSTERS; nMonsterIndex++)
            {
                EnemyReference enemyReference = new EnemyReference(dataOvlReference, tileReferences, nMonsterIndex);
                AllEnemyReferences.Add(enemyReference);
            }

            PrintDebugCSV();
        }

        private void PrintDebugCSV()
        {
            Console.Write(@"Name,AttackRange,MissileType,Friend,Thing");
            //var dExampleBitfield = 0x8000;
            foreach (EnemyReference.EnemyAbility ability in Enum.GetValues(typeof(EnemyReference.EnemyAbility)))
            // for (int index = 0; index < AllEnemyReferences[0]._enemyFlags.Count; index++)
            {
                //Console.Write(@"," + $@"0x{dExampleBitfield:X4}");
                Console.Write(@"," + ability);
                //dExampleBitfield >>= 1;
                //Console.Write(@"," + index.ToString());
            }

            Console.WriteLine();
            
            foreach (EnemyReference enemy in AllEnemyReferences)
            {
                Console.Write(enemy.KeyTileReference.Name + @"/" + enemy.AllCapsPluralName + @"," + $@"0x{enemy.AttackRange:X2}");
                Console.Write(@"," + $@"{enemy.TheMissileType}");
                EnemyReference friend = AllEnemyReferences[enemy.FriendIndex];
                Console.Write(@"," + $@"{friend.AllCapsPluralName}");
                Console.Write(@"," + $@"0x{enemy._nThing:X2}");
                // Console.Write(@"," + $@"0x{enemy._nThing:X2}");
                foreach (EnemyReference.EnemyAbility ability in Enum.GetValues(typeof(EnemyReference.EnemyAbility)))
                {
                    Console.Write(@"," + enemy.IsEnemyAbility(ability));
                }
                Console.WriteLine();
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

    }
}