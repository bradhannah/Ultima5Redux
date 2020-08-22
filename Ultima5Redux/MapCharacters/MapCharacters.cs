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
        private readonly TimeOfDay _timeOfDay;
        private readonly PlayerCharacterRecords _playerCharacterRecords;
        private readonly MapCharacterAnimationStates _smallMapAnimationStates;
        private readonly MapCharacterAnimationStates _overworldAnimationState;
        private readonly MapCharacterAnimationStates _underworldAnimationState;
        //public List<NonPlayerCharacterReference> NonPlayerCharacterReferencesList { get; private set; }
        private MapCharacterAnimationStates _currentMapCharacterAnimationStates;


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
        /// <param name="timeOfDay"></param>
        /// <param name="playerCharacterRecords"></param>
        /// <param name="initialMap">The initial map you are beginning on. It's important to know because there is only
        /// one CharacterState loaded in the save file at load time</param>
        /// <param name="currentSmallMap">The particular map (if small map) that you are loading</param>
        public MapCharacters(TileReferences tileRefs, NonPlayerCharacterReferences npcRefs, 
            DataChunk animationStatesDataChunk, DataChunk overworldAnimationStatesDataChunk, 
            DataChunk underworldAnimationStatesDataChunk, DataChunk charStatesDataChunk,
            DataChunk nonPlayerCharacterMovementLists, DataChunk nonPlayerCharacterMovementOffsets, 
            TimeOfDay timeOfDay, PlayerCharacterRecords playerCharacterRecords,
            LargeMap.Maps initialMap, SmallMapReferences.SingleMapReference.Location currentSmallMap = SmallMapReferences.SingleMapReference.Location.Britannia_Underworld)
        {
            // let's make sure they are using the correct combination
            Debug.Assert((initialMap == LargeMap.Maps.Small &&
                          currentSmallMap != SmallMapReferences.SingleMapReference.Location.Britannia_Underworld)
                         || initialMap != LargeMap.Maps.Small);

            _currentMapType = initialMap;
            _tileRefs = tileRefs;
            _timeOfDay = timeOfDay;
            _playerCharacterRecords = playerCharacterRecords;
            
            // small, overworld and underworld always have saved Animation states so we load them in at the beginning
            _smallMapAnimationStates = new MapCharacterAnimationStates(animationStatesDataChunk, tileRefs);

            // we always load the over and underworld from disk immediately, no need to reload as we will track it in memory 
            // going forward
            _overworldAnimationState = new MapCharacterAnimationStates(overworldAnimationStatesDataChunk, tileRefs);
            _overworldAnimationState.Load(MapCharacterAnimationStates.MapCharacterAnimationStatesFiles.BRIT_OOL, true);

            _underworldAnimationState = new MapCharacterAnimationStates(underworldAnimationStatesDataChunk, tileRefs);
            _underworldAnimationState.Load(MapCharacterAnimationStates.MapCharacterAnimationStatesFiles.UNDER_OOL, true );

            // map character states pertain to whichever map was loaded from disk
            _charStates = new MapCharacterStates(charStatesDataChunk, tileRefs);
            
            // movements pertain to whichever map was loaded from disk
            Movements = new NonPlayerCharacterMovements(nonPlayerCharacterMovementLists, nonPlayerCharacterMovementOffsets);
            
            NPCRefs = npcRefs;

            switch (initialMap)
            {
                case LargeMap.Maps.Small:
                    LoadSmallMap(currentSmallMap, true);
                    break;
                case LargeMap.Maps.Overworld:
                case LargeMap.Maps.Underworld:
                    LoadLargeMap(initialMap, true);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(initialMap), initialMap, null);
            }
        }


        private void SetCurrentMapType(SmallMapReferences.SingleMapReference.Location location, LargeMap.Maps mapType,
            bool bLoadFromDisk)
        {
            _currentMapType = mapType;

            // I may need make an additional save of state before wiping these MapCharacters out
            Characters.Clear();

            switch (mapType)
            {
                case LargeMap.Maps.Small:
                    LoadSmallMap(location, bLoadFromDisk);
                    return;
                case LargeMap.Maps.Overworld:
                case LargeMap.Maps.Underworld:
                    // we don't reload them because the over and underworld are only loaded at boot time
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(mapType), mapType, null);
            }
            

        }

        /// <summary>
        /// Sets the current map that the MapCharacters represents
        /// </summary>
        /// <param name="location"></param>
        /// <param name="mapType">Is it a small map, overworld or underworld</param>
        /// <param name="bLoadFromDisk">should we load the data from disk or start fresh?</param>
        /// <exception cref="ArgumentOutOfRangeException"></exception>
        public void SetCurrentMapType(SmallMapReferences.SingleMapReference.Location location, LargeMap.Maps mapType) 
        {
            SetCurrentMapType(location, mapType, false);
        }

        public MapCharacter GetMapCharacterByLocation(SmallMapReferences.SingleMapReference.Location location, Point2D xy, int nFloor)
        {
            foreach (MapCharacter character in Characters)
            {
                // sometimes characters are null because they don't exist - and that is OK
                if (!character.IsActive) continue;

                if (character.CurrentCharacterPosition.XY == xy && 
                    character.CurrentCharacterPosition.Floor == nFloor && character.NPCRef.MapLocation == location)
                {
                    return character;
                }
            }
            return null;
        }

        /// <summary>
        /// Called when switching to a new large map from a dungeon, small map or other large map
        /// Will load last known state from memory (originally disk) and recalculate some values
        /// such as movement as required.
        /// </summary>
        /// <param name="map"></param>
        /// <param name="bInitialLoad"></param>
        /// <exception cref="ArgumentOutOfRangeException"></exception>
        private void LoadLargeMap(LargeMap.Maps map, bool bInitialLoad) 
        {
            Debug.Assert(map != LargeMap.Maps.Small);
            
            Characters.Clear();
            
            // the over and underworld animation states are already loaded and can stick around
            switch (map)
            {
                case LargeMap.Maps.Overworld:
                    _currentMapCharacterAnimationStates = _overworldAnimationState;
                    break;
                case LargeMap.Maps.Underworld:
                    _currentMapCharacterAnimationStates = _underworldAnimationState;
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(map), map, null);
            }
            
            // populate each of the map characters individually
            for (int i = 0; i < MAX_MAP_CHARACTERS; i++)
            {
                // the party is always at zero
                if (i == 0)
                {
                    Characters.Add(new MapCharacter());
                    continue;
                }
                
                // if this is not the initial load of the map then we can trust character states and
                // movements that are already loaded into memory
                NonPlayerCharacterMovement charMovement = Movements.GetMovement(i);
                if (!bInitialLoad)
                {
                    charMovement.ClearMovements();
                }

                // we have retrieved the _currentMapCharacterAnimationStates based on the map type,
                // now just get the existing animation state which persists on disk for under, over and small maps
                MapCharacterAnimationState charAnimState = _currentMapCharacterAnimationStates.GetCharacterState(i);
                
                // todo: will need to give it a position to start at, probably based on animation state
                MapCharacterState mapCharState;
                
                // if it is the initial load then the loaded _charStates is the correct source of state
                // otherwise we need to a create a brand new character state
                if (bInitialLoad)
                {
                    mapCharState = _charStates.GetCharacterState(i);
                }
                else
                {
                    //if (charAnimState.)
                    mapCharState = new MapCharacterState(_tileRefs, null, i, _timeOfDay);
                }

                // add the new character to our list of characters currently on the map
                Characters.Add(new MapCharacter(null, charAnimState, mapCharState, 
                    charMovement, _timeOfDay, _playerCharacterRecords));
            }
        }

        /// <summary>
        /// Resets the current map to a default state - typically no monsters and NPCs in there default positions
        /// </summary>
        private void LoadSmallMap(SmallMapReferences.SingleMapReference.Location location, bool bLoadFromDisk)
        {
            if (bLoadFromDisk)
            {
                Debug.WriteLine("Loading character positions from disk...");
                // we are loading the small animation from disk
                // this is only done if you save the game and reload within a towne
                _smallMapAnimationStates.Load(MapCharacterAnimationStates.MapCharacterAnimationStatesFiles.SAVED_GAM, true);
            }
            else
            {
                Debug.WriteLine("Loading default character positions...");
            }

            // populate each of the map characters individually
            for (int i = 0; i < MAX_MAP_CHARACTERS; i++)
            {
                if (i == 0)
                {
                    Characters.Add(new MapCharacter());
                    continue;
                }
                
                List<NonPlayerCharacterReference> npcCurrentMapRefs = NPCRefs.GetNonPlayerCharactersByLocation(location);
                NonPlayerCharacterReference npcRef = npcCurrentMapRefs[i];
                MapCharacterState mapCharState;
                MapCharacterAnimationState charAnimState = null;
                NonPlayerCharacterMovement charMovement = Movements.GetMovement(i);

                if (bLoadFromDisk)
                {
                    // character states are only loaded when forced from disk and only on small maps
                    mapCharState = _charStates.GetCharacterState(i);

                    if (CurrentAnimationState.HasAnyAnimationStates())
                    {
                        charAnimState =
                            CurrentAnimationState.GetCharacterState(mapCharState.CharacterAnimationStateIndex);
                    }
                }
                else
                {
                    // we keep the object because we may be required to save this to disk - but since we are leaving the map there is no need to save their movements
                    charMovement.ClearMovements();
                    mapCharState = new MapCharacterState(_tileRefs, npcRef, i, _timeOfDay);
                }

                Characters.Add(new MapCharacter(npcRef, charAnimState, mapCharState, charMovement, _timeOfDay, _playerCharacterRecords));
            }
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