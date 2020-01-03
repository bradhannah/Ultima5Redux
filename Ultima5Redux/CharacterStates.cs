using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ultima5Redux
{
    public class CharacterStates
    {
        private const int MAX_CHARACTER_STATES = 0x20;

        private List<CharacterState> characterStates = new List<CharacterState>(MAX_CHARACTER_STATES);

        public CharacterState GetCharacterState(int nIndex)
        {
            return characterStates[nIndex];
        }

        public CharacterState GetCharacterStateByPosition(Point2D xy, int nFloor)
        {
            foreach (CharacterState characterState in characterStates)
            {
                if (characterState.X == xy.X && characterState.Y == xy.Y && characterState.Floor == nFloor)
                    return characterState;
            }
            return null;
        }

        public CharacterStates(GameState state, TileReferences tileReferences)
        {
            DataChunk dataChunk = state.CharacterStatesDataChunk;

            List<UInt16> characterStateBytes = dataChunk.GetChunkAsUINT16List();

            for (int nIndex = 0; nIndex < MAX_CHARACTER_STATES; nIndex++)
            {
                characterStates.Add(new CharacterState(tileReferences, 
                    characterStateBytes.GetRange(nIndex * CharacterAnimationState.NBYTES, CharacterAnimationState.NBYTES).ToArray(), nIndex));
            }
        }


    }
}
