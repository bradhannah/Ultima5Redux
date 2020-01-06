using System;
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

        public List<MapCharacter> Characters = new List<MapCharacter>(MAX_MAP_CHARACTERS);

        // load the animationstates once from disk, don't worry about again until you are saving to disk
        // load the mapcharacterstates once from disk, don't worry abut again until you are saving to disk

        /// <summary>
        /// static references to all NPCs in the world
        /// </summary>
        private NonPlayerCharacterReferences npcRefs { get; }
        private NonPlayerCharacterMovements movements { get; }

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
                throw new Exception("Asked for a CurrentAnimationState that doesn't exist:" + currentMapType.ToString());
            }
        }
        private MapCharacterStates charStates;
        
        private LargeMap.Maps currentMapType;


        public MapCharacters(TileReferences tileRefs, NonPlayerCharacterReferences npcRefs, 
            DataChunk animationStatesDataChunk, DataChunk overworldAnimationStatesDataChunk, DataChunk underworldAnimationStatesDataChunk, DataChunk charStatesDataChunk,
            DataChunk nonPlayerCharacterMovementLists, DataChunk NonPlayerCharacterMovementOffsets)
        {
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

        public void SetCurrentMapType(SmallMapReferences.SingleMapReference singleMapReference, LargeMap.Maps largeMap, bool bLoadFromDisk)
        {
            List<NonPlayerCharacterReference> npcCurrentMapRefs = null;
            
            currentMapType = largeMap;
            
            switch (largeMap)
            {
                case LargeMap.Maps.Small:
                    npcCurrentMapRefs = npcRefs.GetNonPlayerCharactersByLocation(singleMapReference.MapLocation);
                    smallMapAnimationStates.Load(MapCharacterAnimationStates.MapCharacterAnimationStatesFiles.SAVED_GAM, bLoadFromDisk);
                    break;
                case LargeMap.Maps.Overworld:
                    // we don't reload them because the over and underworld are only loaded at boot time
                    break;
                case LargeMap.Maps.Underworld:
                    break;
            }
            bool bIsLargeMap = largeMap != LargeMap.Maps.Small;

            for (int i = 0; i < MAX_MAP_CHARACTERS; i++)
            {
                // the animations are out of order - so we use this reference to track it down
                NonPlayerCharacterReference npcRef = bIsLargeMap ? null: npcCurrentMapRefs[i];
                MapCharacterState mapCharState = bIsLargeMap ? null : charStates.GetCharacterState(i);

                MapCharacterAnimationState charAnimState = null;
                if (bIsLargeMap)
                {
                    charAnimState = CurrentAnimationState.GetCharacterState(i); 
                }
                else
                {
                    if (CurrentAnimationState.HasAnyAnimationStates())
                    {
                        charAnimState = CurrentAnimationState.GetCharacterState(mapCharState.CharacterAnimationStateIndex);
                    }
                }

                NonPlayerCharacterMovement charMovement = bIsLargeMap ? null : movements.GetMovement(i);
                Characters.Add(new MapCharacter(npcRef, charAnimState, mapCharState, charMovement));

                //Debug.Assert(charAnimState.X == mapCharState.X);
                //Debug.Assert(charAnimState.Y == mapCharState.Y);
                //Debug.Assert(charAnimState.Floor == mapCharState.Floor);
            }
        }

        public MapCharacter GetMapCharacterByLocation(SmallMapReferences.SingleMapReference.Location location, Point2D xy, int nFloor)
        {
            foreach (MapCharacter character in Characters)
            {
                if (character.CurrentMapPosition == xy && character.CurrentFloor == nFloor && character.NPCRef.MapLocation == location)
                {
                    return character;
                }
            }
            return null;
        }
    }
}