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
    public class MapUnits
    {
        private const int MAX_MAP_CHARACTERS = 0x20;

        public readonly List<MapUnit> Characters = new List<MapUnit>(MAX_MAP_CHARACTERS);

        // load the MapAnimationStates once from disk, don't worry about again until you are saving to disk
        // load the SmallMapCharacterStates once from disk, don't worry abut again until you are saving to disk

        private readonly SmallMapCharacterStates _charStates;
        private LargeMap.Maps _currentMapType;
        /// <summary>
        /// static references to all NPCs in the world
        /// </summary>
        private NonPlayerCharacterReferences NPCRefs { get; }
        private NonPlayerCharacterMovements Movements { get; }
        private readonly TileReferences _tileRefs;
        private readonly TimeOfDay _timeOfDay;
        private readonly PlayerCharacterRecords _playerCharacterRecords;
        private readonly MapUnitStates _smallMapAnimationStates;
        private readonly MapUnitStates _overworldAnimationState;
        private readonly MapUnitStates _underworldAnimationState;

        private readonly DataChunk _activeDataChunk;
        private readonly DataChunk _overworldDataChunk;
        private readonly DataChunk _underworldDataChunk;
        
        //public List<NonPlayerCharacterReference> NonPlayerCharacterReferencesList { get; private set; }
        private MapUnitStates _currentMapUnitStates;


        private MapUnitStates CurrentAnimationState
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

        /// <summary>
        /// Constructs the collection of all Map Characters in overworld, underworld and current towne
        /// </summary>
        /// <param name="tileRefs">Global tile references</param>
        /// <param name="npcRefs">Global NPC references</param>
        /// <param name="activeAnimationStatesDataChunk"></param>
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
        public MapUnits(TileReferences tileRefs, NonPlayerCharacterReferences npcRefs, 
            DataChunk activeAnimationStatesDataChunk, DataChunk overworldAnimationStatesDataChunk, 
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

            _activeDataChunk = activeAnimationStatesDataChunk;
            _overworldDataChunk = overworldAnimationStatesDataChunk;
            _underworldDataChunk = underworldAnimationStatesDataChunk;

            // if the small map is being loaded, then we pull from disk and load the over and underworld from
            // saved.ool
            if (initialMap == LargeMap.Maps.Small)
            {
                // small, overworld and underworld always have saved Animation states so we load them in at the beginning
                _smallMapAnimationStates = new MapUnitStates(_activeDataChunk, tileRefs);
                _smallMapAnimationStates.Load(MapUnitStates.MapCharacterAnimationStatesFiles.SAVED_GAM, true);

                // we always load the over and underworld from disk immediately, no need to reload as we will track it in memory 
                // going forward
                _overworldAnimationState = new MapUnitStates(_overworldDataChunk, tileRefs);

                _underworldAnimationState = new MapUnitStates(_underworldDataChunk, tileRefs);
            }
            else 
            {
                // since it is a large map, the small map is empty because the state is lost as soon as you leave it
                // and the selected large map is pulled directly from the active state (saved.gam @ 0x6b8)
                _smallMapAnimationStates = null;

                if (initialMap == LargeMap.Maps.Overworld)
                {
                    _overworldAnimationState = new MapUnitStates(_activeDataChunk, tileRefs);
                    _underworldAnimationState = new MapUnitStates(_underworldDataChunk, tileRefs);
                }
                else
                {
                    _underworldAnimationState = new MapUnitStates(_activeDataChunk, tileRefs);
                    _overworldAnimationState = new MapUnitStates(_overworldDataChunk, tileRefs);
                }
            }
            _overworldAnimationState.Load(MapUnitStates.MapCharacterAnimationStatesFiles.BRIT_OOL, true);
            _underworldAnimationState.Load(MapUnitStates.MapCharacterAnimationStatesFiles.UNDER_OOL, true );

            // map character states pertain to whichever map was loaded from disk
            _charStates = new SmallMapCharacterStates(charStatesDataChunk, tileRefs);
            
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

        /// <summary>
        /// Sets the current map type
        /// Called internally to the class only since it has the bLoadFromDisk option
        /// </summary>
        /// <param name="location"></param>
        /// <param name="mapType"></param>
        /// <param name="bLoadFromDisk"></param>
        /// <exception cref="ArgumentOutOfRangeException"></exception>
        private void SetCurrentMapType(SmallMapReferences.SingleMapReference.Location location, LargeMap.Maps mapType,
            bool bLoadFromDisk)
        {
            _currentMapType = mapType;

            // I may need make an additional save of state before wiping these MapUnits out
            
            switch (mapType)
            {
                case LargeMap.Maps.Small:
                    LoadSmallMap(location, bLoadFromDisk);
                    return;
                case LargeMap.Maps.Overworld:
                case LargeMap.Maps.Underworld:
                    return;
                default:
                    throw new ArgumentOutOfRangeException(nameof(mapType), mapType, null);
            }
        }

        /// <summary>
        /// Sets the current map that the MapUnits represents
        /// </summary>
        /// <param name="location"></param>
        /// <param name="mapType">Is it a small map, overworld or underworld</param>
        /// <exception cref="ArgumentOutOfRangeException"></exception>
        public void SetCurrentMapType(SmallMapReferences.SingleMapReference.Location location, LargeMap.Maps mapType) 
        {
            SetCurrentMapType(location, mapType, false);
        }

        public MapUnit GetMapCharacterByLocation(SmallMapReferences.SingleMapReference.Location location, Point2D xy, int nFloor)
        {
            foreach (MapUnit character in Characters)
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
                    _currentMapUnitStates = _overworldAnimationState;
                    break;
                case LargeMap.Maps.Underworld:
                    _currentMapUnitStates = _underworldAnimationState;
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
                    MapUnit theAvatar = new MapUnit();
                    Characters.Add(theAvatar);
                    continue;
                }
                
                // if this is not the initial load of the map then we can trust character states and
                // movements that are already loaded into memory
                NonPlayerCharacterMovement charMovement = Movements.GetMovement(i);
                // always clear movements because they are not stored in the data for a LargeMap because
                // the monsters will recalculate every turn based on where the Avatar is 
                charMovement.ClearMovements();

                // we have retrieved the _currentMapUnitStates based on the map type,
                // now just get the existing animation state which persists on disk for under, over and small maps
                MapCharacterAnimationState charAnimState = _currentMapUnitStates.GetCharacterState(i);
                
                // A SmallMapCharacterState is not generated because LargeMaps do not track any details within the 
                // SmallMapCharacterState. Instead there will just be left overs of last execution.
                
                // todo: will need to give it a position to start at, probably based on animation state
                // SmallMapCharacterState mapCharState;
                
                // if it is the initial load then the loaded _charStates is the correct source of state
                // otherwise we need to a create a brand new character state
                // if (bInitialLoad)
                // {
                //     mapCharState = _charStates.GetCharacterState(i);
                // }
                // else
                // {
                //     //if (charAnimState.)
                //     mapCharState = new SmallMapCharacterState(_tileRefs, null, i, _timeOfDay);
                // }

                // add the new character to our list of characters currently on the map
                Characters.Add(new MapUnit(null, charAnimState, null, charMovement,
                    _timeOfDay, _playerCharacterRecords, bInitialLoad));
            }
        }

        /// <summary>
        /// Resets the current map to a default state - typically no monsters and NPCs in there default positions
        /// </summary>
        private void LoadSmallMap(SmallMapReferences.SingleMapReference.Location location, bool bInitialLoad)
        {
            // wipe all existing characters since they cannot exist beyond the load
            Characters.Clear();
            
            // are we loading from disk? This should only be done on initial game load since state is immediately 
            // lost when leaving
            if (bInitialLoad)
            {
                Debug.WriteLine("Loading character positions from disk...");
                // we are loading the small animation from disk
                // this is only done if you save the game and reload within a towne
                _smallMapAnimationStates.Load(MapUnitStates.MapCharacterAnimationStatesFiles.SAVED_GAM, true);
            }
            else
            {
                Debug.WriteLine("Loading default character positions...");
                // if we aren't currently on a small map then we need to reassign the current large map to the correct
                // datachunk because it would have otherwise been loaded from saved.gam the first time
                // Note: yes, this is all very confusing - I am confused, but hopefully the abstraction layers
                //       make it consumable
                if (_currentMapType != LargeMap.Maps.Small)
                {
                    if (_currentMapType == LargeMap.Maps.Overworld)
                    {
                        _overworldAnimationState.ReassignNewDataChunk(_overworldDataChunk);
                    }
                    else
                    {
                        _underworldAnimationState.ReassignNewDataChunk(_underworldDataChunk);
                    }
                }
            }

            // get all the NPC references for the current location
            List<NonPlayerCharacterReference> npcCurrentMapRefs = NPCRefs.GetNonPlayerCharactersByLocation(location);

            // populate each of the map characters individually
            for (int i = 0; i < MAX_MAP_CHARACTERS; i++)
            {
                // if it is the first index, then it's the Avatar - but if it's the initial load
                // then it will just load from disk, otherwise we need to create a stub
                if (i == 0 && !bInitialLoad)
                {
                    MapUnit theAvatar = MapUnit.CreateAvatar(_tileRefs, location);
                    Characters.Add(theAvatar);
                    continue;
                }
                
                // get the specific NPC reference 
                NonPlayerCharacterReference npcRef = npcCurrentMapRefs[i];
                // the smallMapCharacterState contains SmallMap NPC information (not applicable for LargeMap)
                SmallMapCharacterState smallMapCharacterState;
                MapCharacterAnimationState charAnimState = null;
                NonPlayerCharacterMovement charMovement = Movements.GetMovement(i);

                if (bInitialLoad)
                {
                    // character states are only loaded when forced from disk and only on small maps
                    smallMapCharacterState = _charStates.GetCharacterState(i);

                    if (CurrentAnimationState.HasAnyAnimationStates())
                    {
                        charAnimState =
                            CurrentAnimationState.GetCharacterState(smallMapCharacterState.CharacterAnimationStateIndex);
                    }
                }
                else
                {
                    // we keep the object because we may be required to save this to disk - but since we are
                    // leaving the map there is no need to save their movements
                    charMovement.ClearMovements();
                    // set a default SmallMapCharacterState based on the given NPC
                    smallMapCharacterState = new SmallMapCharacterState(_tileRefs, npcRef, i, _timeOfDay);
                    // initialize a default MapCharacterAnimationState 
                    charAnimState = new MapCharacterAnimationState(_tileRefs, npcRef);
                }

                Characters.Add(new MapUnit(npcRef, charAnimState, smallMapCharacterState, charMovement, 
                    _timeOfDay, _playerCharacterRecords, bInitialLoad));
            }
        }

        /// <summary>
        /// Get all of the sea faring vessels on the current map
        /// </summary>
        /// <returns></returns>
        public List<SeaFaringVessel.SeaFaringVessel> GetAllSeaFaringVessels()
        {
            List<SeaFaringVessel.SeaFaringVessel> vessels = new List<SeaFaringVessel.SeaFaringVessel>();

            foreach (MapUnit character in Characters)
            {
                if (!character.AnimationState.Tile1Ref.IsBoardable) continue;
                
                if (_tileRefs.IsFrigate(character.AnimationState.Tile1Ref.Index))
                {
                    // Frigate frigate = new Frigate(character.CharacterState.TheCharacterPosition, 
                    //     SeaFaringVesselReference.GetDirectionBySprite(_tileRefs, character.AnimationState.Tile1Ref.Index));
                    // vessels.Add(frigate);
                }
                else if (_tileRefs.IsSkiff(character.AnimationState.Tile1Ref.Index))
                {
                    // Skiff skiff = new Skiff(character.CharacterState.TheCharacterPosition, 
                    //     SeaFaringVesselReference.GetDirectionBySprite(_tileRefs, character.AnimationState.Tile1Ref.Index));
                    // vessels.Add(skiff);
                }
            }

            return vessels;
        }
        
    }
}