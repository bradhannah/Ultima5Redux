using System.Collections.Generic;
using System.Runtime.Serialization;
using Newtonsoft.Json;
using Ultima5Redux.Data;

namespace Ultima5Redux.MapUnits
{
    /// <summary>
    ///     Describes all character states on a single map, be it overworld, underworld or a small map
    ///     only a single collection of SmallMapCharacterStates can be saved at a time, so when switching between
    ///     small and large maps, the state will be lost
    /// </summary>
    [DataContract] public class SmallMapCharacterStates
    {
        private const int MAX_CHARACTER_STATES = 0x20;

        [DataMember] private List<SmallMapCharacterState> CharacterStates { get; set; } =
            new List<SmallMapCharacterState>(MAX_CHARACTER_STATES);

        [JsonConstructor] SmallMapCharacterStates()
        {
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

        public void EmptyState(int nIndex)
        {
            CharacterStates[nIndex] = new SmallMapCharacterState();
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