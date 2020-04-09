using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Ultima5Redux.Data;
using Ultima5Redux.Maps;

namespace Ultima5Redux
{
    public class MapCharacterStates
    {
        private const int MAX_CHARACTER_STATES = 0x20;

        private List<MapCharacterState> _characterStates = new List<MapCharacterState>(MAX_CHARACTER_STATES);

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


    }
}
