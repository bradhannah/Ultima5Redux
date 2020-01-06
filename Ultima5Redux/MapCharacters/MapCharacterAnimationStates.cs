using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Diagnostics;

namespace Ultima5Redux
{

    public class MapCharacterAnimationStates
    {
        #region Public Enums
        public enum MapCharacterAnimationStatesFiles { SAVED_GAM, BRIT_OOL, UNDER_OOL };
        #endregion

        #region Constants
        private const int MAX_CHARACTER_STATES = 0x20;
        #endregion

        #region Private Fields
        private List<MapCharacterAnimationState> characterStates = new List<MapCharacterAnimationState>(MAX_CHARACTER_STATES);

        private DataChunk animationStatesDataChunk;
        private TileReferences tileReferences;
        #endregion

        public MapCharacterAnimationStatesFiles MapCharacterAnimationStatesType { get; private set; }

        public MapCharacterAnimationState GetCharacterState(int nIndex)
        {
            return characterStates[nIndex];
        }

        public bool HasAnyAnimationStates()
        {
            return characterStates.Count > 0;
        }

        public MapCharacterAnimationState GetCharacterStateByPosition(Point2D xy, int nFloor)
        {
            foreach (MapCharacterAnimationState characterState in characterStates)
            {
                if (characterState.X == xy.X && characterState.Y == xy.Y && characterState.Floor == nFloor)
                    return characterState;
            }
            return null;
        }

        public void Load(MapCharacterAnimationStatesFiles mapCharacterAnimationStatesType, bool bLoadFromDisk)
        {
            MapCharacterAnimationStatesType = mapCharacterAnimationStatesType;

            if (!bLoadFromDisk) return;

            List<byte> characterStateBytes = animationStatesDataChunk.GetAsByteList();

            for (int i = 0; i < MAX_CHARACTER_STATES; i++)
            {
                characterStates.Add(new MapCharacterAnimationState(tileReferences, characterStateBytes.GetRange(i * MapCharacterAnimationState.NBYTES, MapCharacterAnimationState.NBYTES).ToArray()));
            }
        }

        public MapCharacterAnimationStates(DataChunk animationStatesDataChunk,
            TileReferences tileReferences)
        {
            this.tileReferences = tileReferences;
            this.animationStatesDataChunk = animationStatesDataChunk;
        }

    }
}
