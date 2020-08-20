using System;
using System.Collections.Generic;
using System.Diagnostics;
using Ultima5Redux.Data;
using Ultima5Redux.DayNightMoon;
using Ultima5Redux.Maps;
using Ultima5Redux.PlayerCharacters;
using Ultima5Redux.SeaFaringVessel;

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
        private readonly TileReferences _tileRefs;
        private readonly MapCharacterAnimationStates _smallMapAnimationStates;
        private readonly MapCharacterAnimationStates _overworldAnimationState;
        private readonly MapCharacterAnimationStates _underworldAnimationState;
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
                    default:
                        throw new ArgumentOutOfRangeException();
                }
                throw new Ultima5ReduxException("Asked for a CurrentAnimationState that doesn't exist:" + _currentMapType.ToString());
            }
        }
        private readonly MapCharacterStates _charStates;
        
        private LargeMap.Maps _currentMapType;

        /// <summary>
        /// Constructs the collection of all Map Characters in overworld, underworld and current towne
        /// </summary>
        /// <param name="tileRefs"></param>
        /// <param name="npcRefs"></param>
        /// <param name="animationStatesDataChunk"></param>
        /// <param name="overworldAnimationStatesDataChunk"></param>
        /// <param name="underworldAnimationStatesDataChunk"></param>
        /// <param name="charStatesDataChunk"></param>
        /// <param name="nonPlayerCharacterMovementLists"></param>
        /// <param name="nonPlayerCharacterMovementOffsets"></param>
        public MapCharacters(TileReferences tileRefs, NonPlayerCharacterReferences npcRefs, 
            DataChunk animationStatesDataChunk, DataChunk overworldAnimationStatesDataChunk, DataChunk underworldAnimationStatesDataChunk, DataChunk charStatesDataChunk,
            DataChunk nonPlayerCharacterMovementLists, DataChunk nonPlayerCharacterMovementOffsets)
        {
            _tileRefs = tileRefs;
            _smallMapAnimationStates = new MapCharacterAnimationStates(animationStatesDataChunk, tileRefs);
            _overworldAnimationState = new MapCharacterAnimationStates(overworldAnimationStatesDataChunk, tileRefs);
            _underworldAnimationState = new MapCharacterAnimationStates(underworldAnimationStatesDataChunk, tileRefs);

            _charStates = new MapCharacterStates(charStatesDataChunk, tileRefs);
            Movements = new NonPlayerCharacterMovements(nonPlayerCharacterMovementLists, nonPlayerCharacterMovementOffsets);
            this.NPCRefs = npcRefs;

            // we always load the over and underworld from disk immediately, no need to reload as we will track it in memory 
            // going forward
            _overworldAnimationState.Load(MapCharacterAnimationStates.MapCharacterAnimationStatesFiles.BRIT_OOL, true);
            _underworldAnimationState.Load(MapCharacterAnimationStates.MapCharacterAnimationStatesFiles.UNDER_OOL, true );
        }

        /// <summary>
        /// Sets the current map that the MapCharacters represents
        /// </summary>
        /// <param name="singleMapReference">the actual map location</param>
        /// <param name="mapType">Is it a small map, overworld or underworld</param>
        /// <param name="timeOfDay">current time of day</param>
        /// <param name="playerCharacterRecords">all the player records</param>
        /// <param name="bLoadFromDisk">should we load the data from disk or start fresh?</param>
        /// <exception cref="ArgumentOutOfRangeException"></exception>
        public void SetCurrentMapType(SmallMapReferences.SingleMapReference singleMapReference, LargeMap.Maps mapType, TimeOfDay timeOfDay, 
            PlayerCharacterRecords playerCharacterRecords, bool bLoadFromDisk)
        {
            _currentMapType = mapType;

            // I may need make an additional save of state before wiping these MapCharacters out
            Characters.Clear();

            switch (mapType)
            {
                case LargeMap.Maps.Small:
                    LoadSmallMap(singleMapReference, timeOfDay, playerCharacterRecords, bLoadFromDisk);
                    return;
                case LargeMap.Maps.Overworld:
                    // we don't reload them because the over and underworld are only loaded at boot time
                    break;
                case LargeMap.Maps.Underworld:
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(mapType), mapType, null);
            }
            //bool bIsLargeMap = mapType != LargeMap.Maps.Small;

            // if it is a small map then we don't need to process
            //if (!bIsLargeMap) return;
            
            // map type is large (small already returned)
            // set the default animation states
            for (int i = 0; i < MAX_MAP_CHARACTERS; i++)
            {
                MapCharacterAnimationState charAnimState = CurrentAnimationState.GetCharacterState(i);

                Characters.Add(new MapCharacter(null, charAnimState, null, null, timeOfDay, playerCharacterRecords));
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

        /// <summary>
        /// Get all of the sea faring vessels on the current map
        /// </summary>
        /// <returns></returns>
        public List<SeaFaringVessel.SeaFaringVessel> GetAllSeaFaringVessels()
        {
            List<SeaFaringVessel.SeaFaringVessel> vessels = new List<SeaFaringVessel.SeaFaringVessel>();

            foreach (MapCharacter character in Characters)
            {
                if (!character.AnimationState.Tile1Ref.IsBoardable) continue;
                
                if (_tileRefs.IsFrigate(character.AnimationState.Tile1Ref.Index))
                {
                    Frigate frigate = new Frigate(character.CharacterState.TheCharacterPosition, 
                        SeaFaringVesselReference.GetDirectionBySprite(_tileRefs, character.AnimationState.Tile1Ref.Index));
                    vessels.Add(frigate);
                }
                else if (_tileRefs.IsSkiff(character.AnimationState.Tile1Ref.Index))
                {
                    Skiff skiff = new Skiff(character.CharacterState.TheCharacterPosition, 
                        SeaFaringVesselReference.GetDirectionBySprite(_tileRefs, character.AnimationState.Tile1Ref.Index));
                    vessels.Add(skiff);
                }
            }

            return vessels;
        }
        
    }
}