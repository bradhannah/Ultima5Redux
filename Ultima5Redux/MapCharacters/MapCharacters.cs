using System.Collections.Generic;
using System.Diagnostics;
using Ultima5Redux.Data;
using Ultima5Redux.DayNightMoon;
using Ultima5Redux.Maps;
using Ultima5Redux.PlayerCharacters;

namespace Ultima5Redux.MapCharacters
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
        private NonPlayerCharacterReferences NPCRefs { get; }
        private NonPlayerCharacterMovements Movements { get; }
        private TileReferences _tileRefs;
        private MapCharacterAnimationStates _smallMapAnimationStates;
        private MapCharacterAnimationStates _overworldAnimationState;
        private MapCharacterAnimationStates _underworldAnimationState;
        private MapCharacterAnimationStates CurrentAnimationState
        {
            get
            {
                switch (_currentMapType)
                {
                    case LargeMap.Maps.Small:
                        return _smallMapAnimationStates;
                    case LargeMap.Maps.Overworld:
                        return _overworldAnimationState;
                    case LargeMap.Maps.Underworld:
                        return _underworldAnimationState;
                }
                throw new Ultima5ReduxException("Asked for a CurrentAnimationState that doesn't exist:" + _currentMapType.ToString());
            }
        }
        private MapCharacterStates _charStates;
        
        private LargeMap.Maps _currentMapType;


        public MapCharacters(TileReferences tileRefs, NonPlayerCharacterReferences npcRefs, 
            DataChunk animationStatesDataChunk, DataChunk overworldAnimationStatesDataChunk, DataChunk underworldAnimationStatesDataChunk, DataChunk charStatesDataChunk,
            DataChunk nonPlayerCharacterMovementLists, DataChunk nonPlayerCharacterMovementOffsets)
        {
            this._tileRefs = tileRefs;
            _smallMapAnimationStates = new MapCharacterAnimationStates(animationStatesDataChunk, tileRefs);
            _overworldAnimationState = new MapCharacterAnimationStates(overworldAnimationStatesDataChunk, tileRefs);
            _underworldAnimationState = new MapCharacterAnimationStates(underworldAnimationStatesDataChunk, tileRefs);

            _charStates = new MapCharacterStates(charStatesDataChunk, tileRefs);
            Movements = new NonPlayerCharacterMovements(nonPlayerCharacterMovementLists, nonPlayerCharacterMovementOffsets);
            this.NPCRefs = npcRefs;

            // we always load the over and underworld from disk immediatley, no need to reload as we will track it in memory 
            // going forward
            _overworldAnimationState.Load(MapCharacterAnimationStates.MapCharacterAnimationStatesFiles.BRIT_OOL, true);
            _underworldAnimationState.Load(MapCharacterAnimationStates.MapCharacterAnimationStatesFiles.UNDER_OOL, true);

        }

        public void SetCurrentMapType(SmallMapReferences.SingleMapReference singleMapReference, LargeMap.Maps largeMap, TimeOfDay timeOfDay, 
            PlayerCharacterRecords playerCharacterRecords, bool bLoadFromDisk)
        {
            _currentMapType = largeMap;

            // I may need make an additional save of state before wiping these MapCharacters out
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
                    MapCharacterAnimationState charAnimState = CurrentAnimationState.GetCharacterState(i);

                    Characters.Add(new MapCharacter(null, charAnimState, null, null, timeOfDay, playerCharacterRecords));
                }
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

            npcCurrentMapRefs = NPCRefs.GetNonPlayerCharactersByLocation(singleMapReference.MapLocation);
            if (bLoadFromDisk)
            {
                Debug.WriteLine("Loading character positions from disk...");
                _smallMapAnimationStates.Load(MapCharacterAnimationStates.MapCharacterAnimationStatesFiles.SAVED_GAM, bLoadFromDisk);
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

                NonPlayerCharacterMovement charMovement = Movements.GetMovement(i);

                if (!bLoadFromDisk)
                {
                    // we keep the object because we may be required to save this to disk - but since we are leaving the map there is no need to save their movements
                    charMovement.ClearMovements();
                    mapCharState = new MapCharacterState(_tileRefs, npcRef, i, timeOfDay);
                }
                else
                {
                    // character states are only loaded when forced from disk and only on small maps
                    mapCharState = _charStates.GetCharacterState(i);

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