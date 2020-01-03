using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Diagnostics;

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

        public CharacterStates(GameState state, TileReferences tileReferences)
        {
            DataChunk dataChunk = state.CharacterStatesDataChunk;

            List<byte> characterStateBytes = dataChunk.GetAsByteList();

            for (int i = 0; i < MAX_CHARACTER_STATES; i++)
            {
                characterStates.Add(new CharacterState(tileReferences, characterStateBytes.GetRange(i * CharacterState.NBYTES, CharacterState.NBYTES).ToArray()));
            }
        }

    }
}
