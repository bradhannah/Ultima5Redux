﻿using System.Collections.Generic;
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

    }
}