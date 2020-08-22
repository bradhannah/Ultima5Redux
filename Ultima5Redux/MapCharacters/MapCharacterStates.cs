using System;
using System.Collections.Generic;
using Ultima5Redux.Data;
using Ultima5Redux.Maps;

namespace Ultima5Redux.MapCharacters
{
    /// <summary>
    /// Describes all character states on a single map, be it overworld, underworld or a small map
    /// only a single collection of MapCharacterStates can be saved at a time, so when switching between
    /// small and large maps, the state will be lost
    /// </summary>
    public class MapCharacterStates
    {
        private const int MAX_CHARACTER_STATES = 0x20;

        private readonly List<MapCharacterState> _characterStates = new List<MapCharacterState>(MAX_CHARACTER_STATES);

        public MapCharacterState GetCharacterState(int nIndex)
        {
            return _characterStates[nIndex];
        }

        public MapCharacterState GetCharacterStateByPosition(Point2D xy, int nFloor)
        {
            foreach (MapCharacterState characterState in _characterStates)
            {
                if (characterState.TheCharacterPosition.X == xy.X && characterState.TheCharacterPosition.Y == xy.Y && characterState.TheCharacterPosition.Floor == nFloor)
                    return characterState;
            }
            return null;
        }

        public MapCharacterStates(DataChunk charStatesDataChunk, TileReferences tileReferences)
        {
            DataChunk dataChunk = charStatesDataChunk;

            List<UInt16> characterStateBytes = dataChunk.GetChunkAsUint16List();

            for (int nIndex = 0; nIndex < MAX_CHARACTER_STATES; nIndex++)
            {
                _characterStates.Add(new MapCharacterState(tileReferences, 
                    characterStateBytes.GetRange(nIndex * MapCharacterAnimationState.NBYTES, MapCharacterAnimationState.NBYTES).ToArray(), nIndex));
            }
        }

        public void EmptyState(int nIndex)
        {
            _characterStates[nIndex] = new MapCharacterState();
        }


    }
}
