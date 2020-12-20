using Ultima5Redux.Data;
using Ultima5Redux.Maps;

namespace Ultima5Redux.MapUnits.CombatMapUnits
{
    public class EnemyReference
    {
        private readonly TileReferences _tileReferences;
        private readonly int _monsterIndex;
        internal const int N_FIRST_SPRITE = 320;
        internal const int N_FRAMES_PER_SPRITE = 4;

        public string GroupName { get; }
        public TileReference KeyTileReference { get; }
        
        public EnemyReference(DataOvlReference dataOvlReference, TileReferences tileReferences, int nMonsterIndex)
        {
            _tileReferences = tileReferences;
            _monsterIndex = nMonsterIndex;
            
            GroupName = dataOvlReference.StringReferences.GetString((DataOvlReference.EnemyOutOfCombatNamesUpper)nMonsterIndex);
            int nKeySpriteIndex = N_FIRST_SPRITE + (nMonsterIndex * N_FRAMES_PER_SPRITE);
            KeyTileReference = tileReferences.GetTileReferenceOfKeyIndex(nKeySpriteIndex);
        }

    }
}