using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.Serialization;
using Newtonsoft.Json;
using Ultima5Redux.References;
using Ultima5Redux.References.Maps;
using Ultima5Redux.References.MapUnits.NonPlayerCharacters;

namespace Ultima5Redux.MapUnits.NonPlayerCharacters
{
    [DataContract]
    public class NonPlayerCharacterStates
    {
        [DataMember(Name = "NPCMap")]
        private readonly Dictionary<SmallMapReferences.SingleMapReference.Location, List<NonPlayerCharacterState>>
            _npcMap = new();

        internal NonPlayerCharacterStates(ImportedGameState importedGameState)
        {
            Debug.Assert(importedGameState.NpcIsDeadArray.Length == importedGameState.NpcIsMetArray.Length);

            int nLocations = importedGameState.NpcIsMetArray[0].Length;

            int nNpcsPerLocation = importedGameState.NpcIsMetArray[0].Length;

            for (int locationIndex = 1; locationIndex < nLocations + 1; locationIndex++)
            {
                var location =
                    (SmallMapReferences.SingleMapReference.Location)locationIndex;
                _npcMap.Add(location, new List<NonPlayerCharacterState>(nNpcsPerLocation));

                for (int npcIndex = 0; npcIndex < nNpcsPerLocation; npcIndex++)
                {
                    NonPlayerCharacterState npcState =
                        new(
                            GameReferences.Instance.NpcRefs.GetNonPlayerCharactersByLocation(location)[npcIndex])
                        {
                            IsDead = importedGameState.NpcIsDeadArray[locationIndex - 1][npcIndex],
                            HasMetAvatar = importedGameState.NpcIsMetArray[locationIndex - 1][npcIndex]
                        };

                    _npcMap[location].Add(npcState);
                }
            }
        }

        [JsonConstructor]
        private NonPlayerCharacterStates()
        {
        }

        public NonPlayerCharacterState GetStateByLocationAndIndex(
            SmallMapReferences.SingleMapReference.Location location, int nIndex) =>
            _npcMap[location][nIndex];

        [SuppressMessage("ReSharper", "UnusedMember.Global")]
        public NonPlayerCharacterState GetStateByNpcRef(NonPlayerCharacterReference npcRef) =>
            _npcMap[npcRef.MapLocation][npcRef.DialogIndex];
    }
}