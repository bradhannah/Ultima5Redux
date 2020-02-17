﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;

namespace Ultima5Redux
{
    public class MapCharacters
    {
        private const int MAX_MAP_CHARACTERS = 0x20;

        public readonly List<MapCharacter> Characters = new List<MapCharacter>(MAX_MAP_CHARACTERS);

        // load the animationstates once from disk, don't worry about again until you are saving to disk
        // load the mapcharacterstates once from disk, don't worry abut again until you are saving to disk

        /// <summary>
        /// static references to all NPCs in the world
        /// </summary>
        private NonPlayerCharacterReferences npcRefs { get; }
        private NonPlayerCharacterMovements movements { get; }
        private TileReferences tileRefs;
        private MapCharacterAnimationStates smallMapAnimationStates;
        private MapCharacterAnimationStates overworldAnimationState;
        private MapCharacterAnimationStates underworldAnimationState;
        private MapCharacterAnimationStates CurrentAnimationState
        {
            get
            {
                switch (currentMapType)
                {
                    case LargeMap.Maps.Small:
                        return smallMapAnimationStates;
                    case LargeMap.Maps.Overworld:
                        return overworldAnimationState;
                    case LargeMap.Maps.Underworld:
                        return underworldAnimationState;
                }
                throw new Ultima5ReduxException("Asked for a CurrentAnimationState that doesn't exist:" + currentMapType.ToString());
            }
        }
        private MapCharacterStates charStates;
        
        private LargeMap.Maps currentMapType;


        public MapCharacters(TileReferences tileRefs, NonPlayerCharacterReferences npcRefs, 
            DataChunk animationStatesDataChunk, DataChunk overworldAnimationStatesDataChunk, DataChunk underworldAnimationStatesDataChunk, DataChunk charStatesDataChunk,
            DataChunk nonPlayerCharacterMovementLists, DataChunk NonPlayerCharacterMovementOffsets)
        {
            this.tileRefs = tileRefs;
            smallMapAnimationStates = new MapCharacterAnimationStates(animationStatesDataChunk, tileRefs);
            overworldAnimationState = new MapCharacterAnimationStates(overworldAnimationStatesDataChunk, tileRefs);
            underworldAnimationState = new MapCharacterAnimationStates(underworldAnimationStatesDataChunk, tileRefs);

            charStates = new MapCharacterStates(charStatesDataChunk, tileRefs);
            movements = new NonPlayerCharacterMovements(nonPlayerCharacterMovementLists, NonPlayerCharacterMovementOffsets);
            this.npcRefs = npcRefs;

            // we always load the over and underworld from disk immediatley, no need to reload as we will track it in memory 
            // going forward
            overworldAnimationState.Load(MapCharacterAnimationStates.MapCharacterAnimationStatesFiles.BRIT_OOL, true);
            underworldAnimationState.Load(MapCharacterAnimationStates.MapCharacterAnimationStatesFiles.UNDER_OOL, true);

        }

        public void SetCurrentMapType(SmallMapReferences.SingleMapReference singleMapReference, LargeMap.Maps largeMap, TimeOfDay timeOfDay, 
            PlayerCharacterRecords playerCharacterRecords, bool bLoadFromDisk)
        {
            List<NonPlayerCharacterReference> npcCurrentMapRefs = null;
            
            currentMapType = largeMap;

            // I may need make an additional save of state before wiping these mapcharacters out
            Characters.Clear();

            switch (largeMap)
            {
                case LargeMap.Maps.Small:
                    LoadSmallMap(singleMapReference, timeOfDay, playerCharacterRecords, bLoadFromDisk);
                    return;
                case LargeMap.Maps.Overworld:
                    // we don't reload them because the over and underworld are only loaded at boot time
                    break;
                case LargeMap.Maps.Underworld:
                    break;
            }
            bool bIsLargeMap = largeMap != LargeMap.Maps.Small;

            if (bIsLargeMap)
            {
                for (int i = 0; i < MAX_MAP_CHARACTERS; i++)
                {
                    // the animations are out of order - so we use this reference to track it down
                    NonPlayerCharacterReference npcRef = bIsLargeMap ? null : npcCurrentMapRefs[i];

                    MapCharacterAnimationState charAnimState = null;

                    MapCharacterState mapCharState = null;
                    NonPlayerCharacterMovement charMovement = null;
                    charAnimState = CurrentAnimationState.GetCharacterState(i);

                    Characters.Add(new MapCharacter(npcRef, charAnimState, mapCharState, charMovement, timeOfDay, playerCharacterRecords));
                }
                //Debug.Assert(charAnimState.X == mapCharState.X);
                //Debug.Assert(charAnimState.Y == mapCharState.Y);
                //Debug.Assert(charAnimState.Floor == mapCharState.Floor);
            }
        }

        public MapCharacter GetMapCharacterByLocation(SmallMapReferences.SingleMapReference.Location location, Point2D xy, int nFloor)
        {
            foreach (MapCharacter character in Characters)
            {
                // sometimes characters are null because they don't exist - and that is OK
                if (!character.IsActive) continue;

                if (character.CurrentCharacterPosition.XY == xy && character.CurrentCharacterPosition.Floor == nFloor && character.NPCRef.MapLocation == location)
                {
                    return character;
                }
            }
            return null;
        }

        /// <summary>
        /// Resets the current map to a default state - typically no monsters and NPCs in there default positions
        /// </summary>
        private List<NonPlayerCharacterReference> LoadSmallMap(SmallMapReferences.SingleMapReference singleMapReference, TimeOfDay timeOfDay, 
            PlayerCharacterRecords playerCharacterRecords, bool bLoadFromDisk)
        {
            List<NonPlayerCharacterReference> npcCurrentMapRefs = null;

            npcCurrentMapRefs = npcRefs.GetNonPlayerCharactersByLocation(singleMapReference.MapLocation);
            if (bLoadFromDisk)
            {
                Debug.WriteLine("Loading character positions from disk...");
                smallMapAnimationStates.Load(MapCharacterAnimationStates.MapCharacterAnimationStatesFiles.SAVED_GAM, bLoadFromDisk);
            }
            else
            {
                Debug.WriteLine("Loading default character positions...");
            }

            for (int i = 0; i < MAX_MAP_CHARACTERS; i++)
            {
                if (i == 0)
                {
                    Characters.Add(new MapCharacter());
                    continue;
                }


                NonPlayerCharacterReference npcRef = npcCurrentMapRefs[i];
                MapCharacterState mapCharState;
                MapCharacterAnimationState charAnimState = null;

                NonPlayerCharacterMovement charMovement = movements.GetMovement(i);

                if (!bLoadFromDisk)
                {
                    // we keep the object because we may be required to save this to disk - but since we are leaving the map there is no need to save their movements
                    charMovement.ClearMovements();
                    mapCharState = new MapCharacterState(tileRefs, npcRef, i, timeOfDay);
                }
                else
                {
                    // character states are only loaded when forced from disk and only on small maps
                    mapCharState = charStates.GetCharacterState(i);

                    if (CurrentAnimationState.HasAnyAnimationStates())
                    {
                        charAnimState = CurrentAnimationState.GetCharacterState(mapCharState.CharacterAnimationStateIndex);
                    }
                }

                Characters.Add(new MapCharacter(npcRef, charAnimState, mapCharState, charMovement, timeOfDay, playerCharacterRecords));
            }
            return npcCurrentMapRefs;
        }

    }
}