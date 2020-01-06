using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ultima5Redux
{
    public class MapCharacterStates
    {
        private const int MAX_CHARACTER_STATES = 0x20;

        private List<MapCharacterState> characterStates = new List<MapCharacterState>(MAX_CHARACTER_STATES);

        public MapCharacterState GetCharacterState(int nIndex)
        {
            return characterStates[nIndex];
        }

        public MapCharacterState GetCharacterStateByPosition(Point2D xy, int nFloor)
        {
            foreach (MapCharacterState characterState in characterStates)
            {
                if (characterState.X == xy.X && characterState.Y == xy.Y && characterState.Floor == nFloor)
                    return characterState;
            }
            return null;
        }

        public MapCharacterStates(DataChunk charStatesDataChunk, TileReferences tileReferences)
        {
            DataChunk dataChunk = charStatesDataChunk;

            List<UInt16> characterStateBytes = dataChunk.GetChunkAsUINT16List();

            for (int nIndex = 0; nIndex < MAX_CHARACTER_STATES; nIndex++)
            {
                characterStates.Add(new MapCharacterState(tileReferences, 
                    characterStateBytes.GetRange(nIndex * MapCharacterAnimationState.NBYTES, MapCharacterAnimationState.NBYTES).ToArray(), nIndex));
            }
        }


    }
}
