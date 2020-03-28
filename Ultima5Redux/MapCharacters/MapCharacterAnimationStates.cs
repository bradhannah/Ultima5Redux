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
        private List<MapCharacterAnimationState> _characterStates = new List<MapCharacterAnimationState>(MAX_CHARACTER_STATES);

        private DataChunk _animationStatesDataChunk;
        private TileReferences _tileReferences;
        #endregion

        public MapCharacterAnimationStatesFiles MapCharacterAnimationStatesType { get; private set; }

        public MapCharacterAnimationState GetCharacterState(int nIndex)
        {
            return _characterStates[nIndex];
        }

        public bool HasAnyAnimationStates()
        {
            return _characterStates.Count > 0;
        }

        public MapCharacterAnimationState GetCharacterStateByPosition(Point2D xy, int nFloor)
        {
            foreach (MapCharacterAnimationState characterState in _characterStates)
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

            List<byte> characterStateBytes = _animationStatesDataChunk.GetAsByteList();

            for (int i = 0; i < MAX_CHARACTER_STATES; i++)
            {
                _characterStates.Add(new MapCharacterAnimationState(_tileReferences, characterStateBytes.GetRange(i * MapCharacterAnimationState.NBYTES, MapCharacterAnimationState.NBYTES).ToArray()));
            }
        }

        public MapCharacterAnimationStates(DataChunk animationStatesDataChunk,
            TileReferences tileReferences)
        {
            this._tileReferences = tileReferences;
            this._animationStatesDataChunk = animationStatesDataChunk;
        }

    }
}
