using System.Collections.Generic;
using Ultima5Redux.Data;
using Ultima5Redux.Maps;

namespace Ultima5Redux.MapCharacters
{

    /// <summary>
    /// Map character animation states
    /// This is a generic application of it. The raw data must be passed in during construction.
    /// </summary>
    public class MapCharacterAnimationStates
    {
        #region Public Enums
        public enum MapCharacterAnimationStatesFiles { SAVED_GAM, BRIT_OOL, UNDER_OOL };
        #endregion

        #region Constants
        private const int MAX_CHARACTER_STATES = 0x20;
        #endregion

        #region Private Fields
        private readonly List<MapCharacterAnimationState> _characterStates = new List<MapCharacterAnimationState>(MAX_CHARACTER_STATES);

        private readonly DataChunk _animationStatesDataChunk;
        private readonly TileReferences _tileReferences;
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

        /// <summary>
        /// Load the character animation states
        /// </summary>
        /// <param name="mapCharacterAnimationStatesType"></param>
        /// <param name="bLoadFromDisk"></param>
        /// <param name="nOffset"></param>
        public void Load(MapCharacterAnimationStatesFiles mapCharacterAnimationStatesType, bool bLoadFromDisk, int nOffset = 0x00)
        {
            MapCharacterAnimationStatesType = mapCharacterAnimationStatesType;

            if (!bLoadFromDisk) return;

            List<byte> characterStateBytes = _animationStatesDataChunk.GetAsByteList();

            for (int i = 0; i < MAX_CHARACTER_STATES; i++)
            {
                _characterStates.Add(new MapCharacterAnimationState(_tileReferences, 
                    characterStateBytes.GetRange((i * MapCharacterAnimationState.NBYTES) + nOffset,
                        MapCharacterAnimationState.NBYTES).ToArray()));
            }
        }

        public MapCharacterAnimationStates(DataChunk animationStatesDataChunk,
            TileReferences tileReferences)
        {
            _tileReferences = tileReferences;
            _animationStatesDataChunk = animationStatesDataChunk;
        }

    }
}
