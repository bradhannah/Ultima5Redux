using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.Serialization;
using Ultima5Redux.Maps;
using Ultima5Redux.References;

namespace Ultima5Redux.MapUnits.NonPlayerCharacters
{
    [DataContract] public class NonPlayerCharacterStates
    {
        private readonly Dictionary<SmallMapReferences.SingleMapReference.Location, List<NonPlayerCharacterState>>
            _npcMap = new Dictionary<SmallMapReferences.SingleMapReference.Location, List<NonPlayerCharacterState>>();

        internal NonPlayerCharacterStates(ImportedGameState importedGameState)
        {
            Debug.Assert(importedGameState.NPCIsDeadArray.Length == importedGameState.NPCIsMetArray.Length);

            int nLocations = importedGameState.NPCIsMetArray[0].Length;

            int nNpcsPerLocation = importedGameState.NPCIsMetArray[0].Length;

            for (int locationIndex = 1; locationIndex < nLocations + 1; locationIndex++)
            {
                SmallMapReferences.SingleMapReference.Location location =
                    (SmallMapReferences.SingleMapReference.Location)locationIndex;
                _npcMap.Add(location, new List<NonPlayerCharacterState>(nNpcsPerLocation));

                for (int npcIndex = 0; npcIndex < nNpcsPerLocation; npcIndex++)
                {
                    NonPlayerCharacterState npcState =
                        new NonPlayerCharacterState(
                            GameReferences.NpcRefs.GetNonPlayerCharactersByLocation(location)[npcIndex])
                        {
                            IsDead = importedGameState.NPCIsDeadArray[locationIndex - 1][npcIndex],
                            HasMetAvatar = importedGameState.NPCIsMetArray[locationIndex - 1][npcIndex]
                        };

                    _npcMap[location].Add(npcState);
                }
            }
        }

        public NonPlayerCharacterState GetStateByLocationAndIndex(
            SmallMapReferences.SingleMapReference.Location location, int nIndex) => _npcMap[location][nIndex];

        public NonPlayerCharacterState GetStateByNPCRef(NonPlayerCharacterReference npcRef) =>
            _npcMap[npcRef.MapLocation][npcRef.DialogIndex];
    }
}