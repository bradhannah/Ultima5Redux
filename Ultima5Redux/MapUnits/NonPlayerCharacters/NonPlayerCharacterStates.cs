﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.Serialization;
using Ultima5Redux.Dialogue;
using Ultima5Redux.Maps;

namespace Ultima5Redux.MapUnits.NonPlayerCharacters
{
    [DataContract]
    public class NonPlayerCharacterStates
    {
        private readonly Dictionary<SmallMapReferences.SingleMapReference.Location, List<NonPlayerCharacterState>> _npcMap =
            new Dictionary<SmallMapReferences.SingleMapReference.Location, List<NonPlayerCharacterState>>();
        
        public NonPlayerCharacterStates(ImportedGameState importedGameState, NonPlayerCharacterReferences npcRefs)
        {
            Debug.Assert(importedGameState.NPCIsDeadArray.Length == importedGameState.NPCIsMetArray.Length);

            int nLocations = importedGameState.NPCIsMetArray[0].Length;
                //Enum.GetNames(typeof(SmallMapReferences.SingleMapReference.Location)).Length;
            
            int nNpcsPerLocation = importedGameState.NPCIsMetArray[0].Length;
            
            for (int locationIndex = 1; locationIndex < nLocations + 1; locationIndex++)
            {
                SmallMapReferences.SingleMapReference.Location location =
                    (SmallMapReferences.SingleMapReference.Location)locationIndex;
                _npcMap.Add(location, new List<NonPlayerCharacterState>(nNpcsPerLocation));
                
                // if (location == SmallMapReferences.SingleMapReference.Location.Britannia_Underworld)
                //     continue;
                
                for (int npcIndex = 0; npcIndex < nNpcsPerLocation; npcIndex++)
                {
                    NonPlayerCharacterState npcState = 
                        new NonPlayerCharacterState(npcRefs.GetNonPlayerCharactersByLocation(location)[npcIndex])
                    {
                        IsDead = importedGameState.NPCIsDeadArray[locationIndex - 1][npcIndex],
                        HasMetAvatar = importedGameState.NPCIsMetArray[locationIndex - 1][npcIndex]
                    };
                    
                    _npcMap[location].Add(npcState);
                        //[npcIndex] = npcState;
                }
            }
        }

        public NonPlayerCharacterState GetStateByNPCRef(NonPlayerCharacterReference npcRef) 
            => _npcMap[npcRef.MapLocation][npcRef.DialogIndex];

        public NonPlayerCharacterState GetStateByLocationAndIndex(
            SmallMapReferences.SingleMapReference.Location location,
            int nIndex) => _npcMap[location][nIndex];


    }
}