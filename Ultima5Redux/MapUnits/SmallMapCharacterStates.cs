using System.Collections.Generic;
using System.Diagnostics;
using Newtonsoft.Json;
using Ultima5Redux.Data;

namespace Ultima5Redux.MapUnits
{
    /// <summary>
    ///     Describes all character states on a single map, be it overworld, underworld or a small map
    ///     only a single collection of SmallMapCharacterStates can be saved at a time, so when switching between
    ///     small and large maps, the state will be lost
    /// </summary>
    public class SmallMapCharacterStates
    {
        private const int MAX_CHARACTER_STATES = 0x20;

        private List<SmallMapCharacterState> CharacterStates { get; } =
            new List<SmallMapCharacterState>(MAX_CHARACTER_STATES);

        [JsonConstructor] private SmallMapCharacterStates()
        {
            Debug.Assert(true, "If you are deserializing then a mistake was made");
        }

        public SmallMapCharacterStates(DataChunk charStatesDataChunk)
        {
            DataChunk dataChunk = charStatesDataChunk;

            List<ushort> characterStateBytes = dataChunk.GetChunkAsUint16List();

            for (int nIndex = 0; nIndex < MAX_CHARACTER_STATES; nIndex++)
            {
                CharacterStates.Add(new SmallMapCharacterState(
                    characterStateBytes.GetRange(nIndex * MapUnitState.NBYTES, MapUnitState.NBYTES).ToArray(), nIndex));
            }
        }

        public SmallMapCharacterState GetCharacterState(int nIndex)
        {
            return CharacterStates[nIndex];
        }

        public SmallMapCharacterState GetCharacterStateByPosition(Point2D xy, int nFloor)
        {
            foreach (SmallMapCharacterState characterState in CharacterStates)
            {
                if (characterState.TheMapUnitPosition.X == xy.X && characterState.TheMapUnitPosition.Y == xy.Y &&
                    characterState.TheMapUnitPosition.Floor == nFloor)
                    return characterState;
            }

            return null;
        }
    }
}