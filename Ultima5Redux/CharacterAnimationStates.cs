using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Diagnostics;

namespace Ultima5Redux
{

    public class CharacterAnimationStates
    {
        private const int MAX_CHARACTER_STATES = 0x20;

        private List<CharacterAnimationState> characterStates = new List<CharacterAnimationState>(MAX_CHARACTER_STATES);

        public CharacterAnimationState GetCharacterState(int nIndex)
        {
            return characterStates[nIndex];
        }

        public CharacterAnimationState GetCharacterStateByPosition(Point2D xy, int nFloor)
        {
            foreach (CharacterAnimationState characterState in characterStates)
            {
                if (characterState.X == xy.X && characterState.Y == xy.Y && characterState.Floor == nFloor)
                    return characterState;
            }
            return null;
        }

        public CharacterAnimationStates(GameState state, TileReferences tileReferences)
        {
            DataChunk dataChunk = state.CharacterAnimationStatesDataChunk;

            List<byte> characterStateBytes = dataChunk.GetAsByteList();

            for (int i = 0; i < MAX_CHARACTER_STATES; i++)
            {
                characterStates.Add(new CharacterAnimationState(tileReferences, characterStateBytes.GetRange(i * CharacterAnimationState.NBYTES, CharacterAnimationState.NBYTES).ToArray()));
            }
        }

    }
}
