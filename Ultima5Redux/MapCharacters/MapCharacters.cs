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

        private MapCharacterAnimationStates animationStates;
        private MapCharacterStates charStates;


        public MapCharacters(TileReferences tileRefs, NonPlayerCharacterReferences npcRefs, SmallMapReferences.SingleMapReference singleMapReference, LargeMap.Maps largeMap,
            DataChunk animationStatesDataChunk, DataChunk overworldAnimationStatesDataChunk, DataChunk underworldAnimationStatesDataChunk, DataChunk charStatesDataChunk,
            DataChunk nonPlayerCharacterMovementLists, DataChunk NonPlayerCharacterMovementOffsets)
        {
            animationStates = new MapCharacterAnimationStates(animationStatesDataChunk, overworldAnimationStatesDataChunk, underworldAnimationStatesDataChunk, tileRefs);
            
            charStates = new MapCharacterStates(charStatesDataChunk, tileRefs);
            movements = new NonPlayerCharacterMovements(nonPlayerCharacterMovementLists, NonPlayerCharacterMovementOffsets);
            this.npcRefs = npcRefs;

            List<NonPlayerCharacterReference> npcCurrentMapRefs = null;
            switch (largeMap)
            {
                case LargeMap.Maps.Small:
                    npcCurrentMapRefs = npcRefs.GetNonPlayerCharactersByLocation(singleMapReference.MapLocation);
                    animationStates.Load(MapCharacterAnimationStates.MapCharacterAnimationStatesFiles.SAVED_GAM);
                    break;
                case LargeMap.Maps.Overworld:
                    animationStates.Load(MapCharacterAnimationStates.MapCharacterAnimationStatesFiles.BRIT_OOL);
                    break;
                case LargeMap.Maps.Underworld:
                    animationStates.Load(MapCharacterAnimationStates.MapCharacterAnimationStatesFiles.UNDER_OOL);
                    break;
            }

            for (int i = 0; i < MAX_MAP_CHARACTERS; i++)
            {
                // the animations are out of order - so we use this reference to track it down
                NonPlayerCharacterReference npcRef = npcCurrentMapRefs != null ? npcCurrentMapRefs[i] : null;
                MapCharacterState mapCharState = charStates.GetCharacterState(i);
                MapCharacterAnimationState charAnimState = animationStates.GetCharacterState(mapCharState.CharacterAnimationStateIndex);
                //Debug.Assert(charAnimState.X == mapCharState.X);
                //Debug.Assert(charAnimState.Y == mapCharState.Y);
                //Debug.Assert(charAnimState.Floor == mapCharState.Floor);
                NonPlayerCharacterMovement charMovement = movements.GetMovement(i);
                Characters.Add(new MapCharacter(npcRef, charAnimState, mapCharState, charMovement));
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