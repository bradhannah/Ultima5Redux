using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.Serialization;
using Newtonsoft.Json;
using Ultima5Redux.Maps;
using Ultima5Redux.MapUnits.CombatMapUnits;
using Ultima5Redux.MapUnits.NonPlayerCharacters;
using Ultima5Redux.MapUnits.SeaFaringVessels;
using Ultima5Redux.PlayerCharacters.Inventory;
using Ultima5Redux.References;
using Ultima5Redux.References.Maps;
using Ultima5Redux.References.MapUnits.NonPlayerCharacters;

namespace Ultima5Redux.MapUnits
{
    [DataContract] public class MapUnits
    {
        public const int MAX_LEGACY_MAP_CHARACTERS = 32;
        public const int MAX_MAP_CHARACTERS = 64;

        [DataMember(Name = "UseExtendedSprites")]
        private readonly bool _bUseExtendedSprites;

        [DataMember(Name = "InitialMapType")] private Map.Maps _initialMapType;

        [DataMember] private Map.Maps CurrentMapType { get; set; }

        [DataMember]
        public MapUnitCollection CombatMapMapUnitCollection { get; private set; } =
            new();

        [DataMember] public SmallMapReferences.SingleMapReference.Location CurrentLocation { get; private set; }

        [DataMember] public MapUnitCollection OverworldMapMapUnitCollection { get; protected set; } = new();

        // load the SmallMapCharacterStates once from disk, don't worry abut again until you are saving to disk
        [DataMember] public MapUnitCollection SmallMapUnitCollection { get; protected set; } = new();

        [DataMember]
        public MapUnitCollection UnderworldMapUnitCollection { get; protected set; } =
            new();

        /// <summary>
        ///     The single source of truth for the Avatar's current position within the current map
        /// </summary>
        [IgnoreDataMember]
        internal MapUnitPosition CurrentAvatarPosition
        {
            get => GetAvatarMapUnit().MapUnitPosition;
            set => GetAvatarMapUnit().MapUnitPosition = value;
        }

        [IgnoreDataMember] private readonly ImportedGameState _importedGameState;

        [IgnoreDataMember] private readonly MapUnitMovements _importedMovements;

        [IgnoreDataMember] private Avatar MasterAvatarMapUnit { get; set; }

        [IgnoreDataMember] public MapUnitCollection CurrentMapUnits => GetMapUnitCollection(CurrentMapType);

        [IgnoreDataMember]
        public int TotalMapUnitsOnMap => CurrentMapUnits.AllMapUnits.Count(m => m is not EmptyMapUnit);

        /// <summary>
        ///     Constructs the collection of all Map CurrentMapUnits in overworld, underworld and current towne
        ///     from a legacy save import
        /// </summary>
        /// <param name="initialMap">
        ///     The initial map you are beginning on. It's important to know because there is only
        ///     one TheSmallMapCharacterState loaded in the save file at load time
        /// </param>
        /// <param name="bUseExtendedSprites"></param>
        /// <param name="importedGameState"></param>
        /// <param name="searchItems"></param>
        /// <param name="currentSmallMap">The particular map (if small map) that you are loading</param>
        internal MapUnits(Map.Maps initialMap, bool bUseExtendedSprites, ImportedGameState importedGameState,
            SearchItems searchItems,
            SmallMapReferences.SingleMapReference.Location currentSmallMap =
                SmallMapReferences.SingleMapReference.Location.Britannia_Underworld)
        {
            CurrentMapType = initialMap;
            // the initial map is important because it contains the Master Avatar for duplication
            _initialMapType = initialMap;
            _bUseExtendedSprites = bUseExtendedSprites;
            _importedGameState = importedGameState;
            CurrentLocation = currentSmallMap;

            // movements pertain to whichever map was loaded from disk
            _importedMovements = importedGameState.CharacterMovements;

            // we only load the large maps once and they always exist on disk
            GenerateMapUnitsForLargeMap(Map.Maps.Overworld, true, searchItems);
            GenerateMapUnitsForLargeMap(Map.Maps.Underworld, true, searchItems);

            // if the small map is the initial map, then load it 
            // otherwise we force the correct states to either the over or underworld
            switch (initialMap)
            {
                case Map.Maps.Small:
                    LoadSmallMap(currentSmallMap, true, searchItems);
                    break;
                case Map.Maps.Overworld:
                    break;
                case Map.Maps.Underworld:
                    break;
                case Map.Maps.Combat:
                    throw new Ultima5ReduxException("You can't initialize the MapUnits with a combat map");
                default:
                    throw new ArgumentOutOfRangeException(nameof(initialMap), initialMap, null);
            }

            // We will reassign each AvatarMapUnit to the active one. This will ensure that when the Avatar
            // has boarded something, it should carry between maps
            SetCommonAvatarMapUnit();

            SetAllExtendedSprites();

            CurrentMapType = initialMap;
        }

        [JsonConstructor] private MapUnits() => _importedMovements = new MapUnitMovements();

        [OnDeserialized] private void PostDeserialized(StreamingContext context)
        {
            SetCommonAvatarMapUnit();
        }

        internal int AddCombatMapUnit(CombatMapUnit mapUnit)
        {
            int nIndex = FindNextFreeMapUnitIndex(Map.Maps.Combat);
            if (nIndex < 0) return -1;

            AddNewMapUnit(Map.Maps.Combat, mapUnit);

            return nIndex;
        }

        internal void ClearMapUnit(MapUnit mapUnit)
        {
            int nIndex = CurrentMapUnits.AllMapUnits.IndexOf(mapUnit);
            CurrentMapUnits.AllMapUnits[nIndex] = new EmptyMapUnit();
        }

        /// <summary>
        ///     Creates a new Magic Carpet and places it on the map
        /// </summary>
        /// <param name="xy"></param>
        /// <param name="direction"></param>
        /// <param name="nIndex"></param>
        /// <returns></returns>
        // ReSharper disable once UnusedMember.Local
        internal MagicCarpet CreateMagicCarpet(Point2D xy, Point2D.Direction direction, out int nIndex)
        {
            nIndex = FindNextFreeMapUnitIndex(CurrentMapType);

            if (nIndex == -1) return null;

            MagicCarpet magicCarpet = new(CurrentLocation, direction, null, new MapUnitPosition(xy.X, xy.Y, 0));

            AddNewMapUnit(CurrentMapType, magicCarpet, nIndex);
            return magicCarpet;
        }

        internal MapUnitCollection GetMapUnitCollection(Map.Maps map)
        {
            return map switch
            {
                Map.Maps.Small => SmallMapUnitCollection,
                Map.Maps.Overworld => OverworldMapMapUnitCollection,
                Map.Maps.Underworld => UnderworldMapUnitCollection,
                Map.Maps.Combat => CombatMapMapUnitCollection,
                _ => throw new InvalidEnumArgumentException(((int)map).ToString())
            };
        }

        internal void ReloadNpcData(SmallMapReferences.SingleMapReference.Location location)
        {
            for (int i = 1; i < SmallMapUnitCollection.AllMapUnits.Count; i++)
            {
                MapUnit mapUnit = SmallMapUnitCollection.AllMapUnits[i];
                if (mapUnit is not EmptyMapUnit and not DiscoverableLoot) //NonPlayerCharacter npc)
                {
                    if (mapUnit is DeadBody or BloodSpatter or Chest or Horse or MagicCarpet or ItemStack &&
                        mapUnit.NPCRef == null) continue;
                    if (mapUnit.NPCRef == null)
                        throw new Ultima5ReduxException($"Expected NPCRef for MapUnit {mapUnit.GetType()}");
                    if (mapUnit.NPCRef.DialogIndex != -1)
                    {
                        // get the specific NPC reference 
                        NonPlayerCharacterState npcState =
                            GameStateReference.State.TheNonPlayerCharacterStates.GetStateByLocationAndIndex(location,
                                mapUnit.NPCRef.DialogIndex);

                        mapUnit.NPCState = npcState;
                        // No need to refresh the SmallMapCharacterState because it is saved to the save file 
                        //new SmallMapCharacterState(npcState.NPCRef, i);
                    }
                }
            }
        }

        /// <summary>
        ///     Resets the current map to a default state - typically no monsters and NPCs in there default positions
        /// </summary>
        internal void LoadSmallMap(SmallMapReferences.SingleMapReference.Location location, bool bInitialLegacyLoad,
            SearchItems searchItems)
        {
            if (location is SmallMapReferences.SingleMapReference.Location.Combat_resting_shrine
                or SmallMapReferences.SingleMapReference.Location.Britannia_Underworld)
                throw new Ultima5ReduxException("Tried to load " + location + " into a small map");

            // wipe all existing characters since they cannot exist beyond the load
            SmallMapUnitCollection.Clear();
            CombatMapMapUnitCollection.Clear();

            // populate each of the map characters individually
            for (int i = 0; i < MAX_MAP_CHARACTERS; i++)
            {
                MapUnitMovement mapUnitMovement = _importedMovements.GetMovement(i) ?? new MapUnitMovement(i);

                // if it is the first index, then it's the Avatar - but if it's the initial load
                // then it will just load from disk, otherwise we need to create a stub
                if (i == 0 && !bInitialLegacyLoad)
                {
                    mapUnitMovement.ClearMovements();
                    // load the existing AvatarMapUnit with boarded MapUnits

                    SmallMapUnitCollection.Add(MasterAvatarMapUnit);
                    GetAvatarMapUnit().MapLocation = location;
                    continue;
                }

                // The zero position is always Avatar, this grabs them from the legacy save file 
                if (i == 0 && bInitialLegacyLoad)
                {
                    MapUnitState theAvatarMapState =
                        _importedGameState.GetMapUnitStatesByMap(Map.Maps.Small).GetCharacterState(0);
                    MapUnit theAvatar = Avatar.CreateAvatar(location, mapUnitMovement,
                        new MapUnitPosition(theAvatarMapState.X, theAvatarMapState.Y, theAvatarMapState.Floor),
                        theAvatarMapState.Tile1Ref, _bUseExtendedSprites);
                    SmallMapUnitCollection.Add(theAvatar);
                    continue;
                }

                // we have extended the max characters from 32 to 64 - BUT - we have to make sure we don't
                // try to index into the legacy array if we are index 32+
                bool bIsInExtendedNpcArea = i >= MAX_LEGACY_MAP_CHARACTERS;

                if (bIsInExtendedNpcArea)
                {
                    var emptyUnit = new EmptyMapUnit();
                    SmallMapUnitCollection.Add(emptyUnit);
                    continue;
                }

                // get the specific NPC reference 
                NonPlayerCharacterState npcState =
                    GameStateReference.State.TheNonPlayerCharacterStates.GetStateByLocationAndIndex(location, i);

                // we keep the object because we may be required to save this to disk - but since we are
                // leaving the map there is no need to save their movements
                mapUnitMovement.ClearMovements();

                // set a default SmallMapCharacterState based on the given NPC
                bool bInitialMapAndSmall = bInitialLegacyLoad && _importedGameState.InitialMap == Map.Maps.Small;

                // if it's an initial load we use the imported state, otherwise we assume it's fresh and new
                SmallMapCharacterState smallMapCharacterState = bInitialMapAndSmall
                    ? _importedGameState.SmallMapCharacterStates.GetCharacterState(i)
                    : new SmallMapCharacterState(npcState.NPCRef, i);

                MapUnit mapUnit = CreateNewMapUnit(mapUnitMovement, false, location, npcState,
                    smallMapCharacterState.TheMapUnitPosition,
                    GameReferences.SpriteTileReferences.GetTileReference(npcState.NPCRef.NPCKeySprite),
                    smallMapCharacterState);

                // I want to be able to do this - but if I do then no new map units are created...
                // I used the original game logic of limiting to 32 entities, which is probably the correct
                // thing to do
                //if (mapUnit is EmptyMapUnit) continue;

                SmallMapUnitCollection.Add(mapUnit);
            }

            //int nFloor = map == Map.Maps.Underworld ? -1 : 0;
            //int nFloor = sin
            int nFloors = GameReferences.SmallMapRef.GetNumberOfFloors(location);
            bool bHasBasement = GameReferences.SmallMapRef.HasBasement(location);
            int nTopFloor = bHasBasement ? nFloors - 2 : nFloors - 1;

            for (int nFloor = bHasBasement ? -1 : 0; nFloor <= nTopFloor; nFloor++)
            {
                Dictionary<Point2D, List<SearchItem>> searchItemsInMap =
                    searchItems.GetUnDiscoveredSearchItemsByLocation(
                        location, nFloor);
                foreach (KeyValuePair<Point2D, List<SearchItem>> kvp in searchItemsInMap)
                {
                    MapUnitPosition mapUnitPosition = new(kvp.Key.X, kvp.Key.Y, nFloor);
                    // at this point we are cycling through the positions
                    foreach (SearchItem searchItem in kvp.Value)
                    {
                        /// TEMPORARY FIX: you are only supposed to discover one discoverable loot at a time
                        List<SearchItem> searchItemInList = new() { searchItem };
                        var discoverableLoot = new DiscoverableLoot(location, mapUnitPosition, searchItemInList);
                        SmallMapUnitCollection.AddMapUnit(discoverableLoot);
                    }
                }
            }
        }

        /// <summary>
        ///     Adds a new map unit to the next position available
        /// </summary>
        /// <param name="map"></param>
        /// <param name="mapUnit"></param>
        /// <returns>true if successful, false if no room was found</returns>
        // ReSharper disable once UnusedMethodReturnValue.Local
        private bool AddNewMapUnit(Map.Maps map, MapUnit mapUnit)
        {
            int nIndex = FindNextFreeMapUnitIndex(map);
            return AddNewMapUnit(map, mapUnit, nIndex);
        }

        /// <summary>
        ///     Adds a new map unit to a given position
        /// </summary>
        /// <param name="map"></param>
        /// <param name="mapUnit"></param>
        /// <param name="nIndex"></param>
        /// <returns></returns>
        private bool AddNewMapUnit(Map.Maps map, MapUnit mapUnit, int nIndex)
        {
            if (nIndex == -1) return false;

            List<MapUnit> mapUnits = GetMapUnitCollection(map).AllMapUnits;
            Debug.Assert(nIndex < mapUnits.Count);
            mapUnits[nIndex] = mapUnit;
            mapUnit.UseFourDirections = _bUseExtendedSprites;
            return true;
        }

        /// <summary>
        ///     Creates a Frigate and places it in on the map
        /// </summary>
        /// <param name="xy"></param>
        /// <param name="direction"></param>
        /// <param name="nIndex"></param>
        /// <param name="nSkiffsAboard"></param>
        /// <returns></returns>
        private Frigate CreateFrigate(Point2D xy, Point2D.Direction direction, out int nIndex, int nSkiffsAboard)
        {
            nIndex = FindNextFreeMapUnitIndex(CurrentMapType);

            if (nIndex == -1) return null;

            Frigate frigate = new(_importedMovements.GetMovement(nIndex),
                SmallMapReferences.SingleMapReference.Location.Britannia_Underworld, direction, null,
                new MapUnitPosition(xy.X, xy.Y, 0));

            // set position of frigate in the world
            frigate.SkiffsAboard = nSkiffsAboard;
            frigate.KeyTileReference = GameReferences.SpriteTileReferences.GetTileReferenceByName("ShipNoSailsLeft");

            AddNewMapUnit(Map.Maps.Overworld, frigate, nIndex);
            return frigate;
        }

        /// <summary>
        ///     Generates a new map unit
        /// </summary>
        /// <param name="mapUnitMovement"></param>
        /// <param name="bInitialLoad"></param>
        /// <param name="location"></param>
        /// <param name="npcState"></param>
        /// <param name="tileReference"></param>
        /// <param name="smallMapCharacterState"></param>
        /// <param name="mapUnitPosition"></param>
        /// <returns></returns>
        private MapUnit CreateNewMapUnit(MapUnitMovement mapUnitMovement, bool bInitialLoad,
            SmallMapReferences.SingleMapReference.Location location, NonPlayerCharacterState npcState,
            MapUnitPosition mapUnitPosition, TileReference tileReference,
            SmallMapCharacterState smallMapCharacterState = null)
        {
            MapUnit newUnit;

            if (smallMapCharacterState != null && npcState != null && smallMapCharacterState.Active &&
                npcState.NPCRef.NormalNPC)
            {
                newUnit = new NonPlayerCharacter(smallMapCharacterState, mapUnitMovement, bInitialLoad, location,
                    mapUnitPosition, npcState);
            }
            else if (tileReference == null || tileReference.Index == 256)
            {
                Debug.WriteLine("An empty map unit was created with no tile reference");
                newUnit = new EmptyMapUnit();
            }
            else if (GameReferences.SpriteTileReferences.IsFrigate(tileReference.Index))
            {
                newUnit = new Frigate(mapUnitMovement, location, tileReference.GetDirection(), npcState,
                    mapUnitPosition);
            }
            else if (GameReferences.SpriteTileReferences.IsSkiff(tileReference.Index))
            {
                newUnit = new Skiff(mapUnitMovement, location, tileReference.GetDirection(), npcState, mapUnitPosition);
            }
            else if (TileReferences.IsMagicCarpet(tileReference.Index))
            {
                newUnit = new MagicCarpet(location, tileReference.GetDirection(), npcState, mapUnitPosition);
            }
            else if (TileReferences.IsUnmountedHorse(tileReference.Index))
            {
                newUnit = new Horse(mapUnitMovement, location, tileReference.GetDirection(), npcState, mapUnitPosition);
            }
            else if (smallMapCharacterState != null && npcState != null && smallMapCharacterState.Active)
            {
                // This is where we will do custom stuff for special NPS
                // guard or daemon or stone gargoyle or fighter or bard or townesperson or rat or bat or shadowlord
                if ((TileReference.SpriteIndex)npcState.NPCRef.NPCKeySprite is
                    TileReference.SpriteIndex.Guard_KeyIndex
                    or TileReference.SpriteIndex.Daemon1_KeyIndex
                    or TileReference.SpriteIndex.StoneGargoyle_KeyIndex
                    or TileReference.SpriteIndex.Fighter_KeyIndex
                    or TileReference.SpriteIndex.Bard_KeyIndex
                    or TileReference.SpriteIndex.TownsPerson_KeyIndex
                    or TileReference.SpriteIndex.Ray_KeyIndex
                    or TileReference.SpriteIndex.Bat_KeyIndex
                    or TileReference.SpriteIndex.ShadowLord_KeyIndex)
                {
                    newUnit = new NonPlayerCharacter(smallMapCharacterState, mapUnitMovement, bInitialLoad, location,
                        mapUnitPosition, npcState);
                }
                else
                {
                    newUnit = NonAttackingUnitFactory.Create(npcState.NPCRef.NPCKeySprite, location, mapUnitPosition);
                    newUnit.MapLocation = location;
                }
            }
            else if (GameReferences.SpriteTileReferences.IsMonster(tileReference.Index))
            {
                Debug.Assert(GameReferences.EnemyRefs != null);
                newUnit = new Enemy(mapUnitMovement, GameReferences.EnemyRefs.GetEnemyReference(tileReference),
                    location, npcState, mapUnitPosition);
            }
            // this is where we will create monsters too
            else
            {
                Debug.WriteLine("An empty map unit was created with " + tileReference.Name);
                newUnit = new EmptyMapUnit();
            }

            // force to use extended sprites if they exist
            newUnit.UseFourDirections = _bUseExtendedSprites;

            return newUnit;
        }

        /// <summary>
        ///     Creates a skiff and places it on the map
        /// </summary>
        /// <param name="xy"></param>
        /// <param name="direction"></param>
        /// <param name="nIndex"></param>
        /// <returns></returns>
        private Skiff CreateSkiff(Point2D xy, Point2D.Direction direction, out int nIndex)
        {
            nIndex = FindNextFreeMapUnitIndex(CurrentMapType);
            if (nIndex == -1) return null;

            Skiff skiff = new(_importedMovements.GetMovement(nIndex),
                SmallMapReferences.SingleMapReference.Location.Britannia_Underworld, direction, null,
                new MapUnitPosition(xy.X, xy.Y, 0));

            AddNewMapUnit(Map.Maps.Overworld, skiff, nIndex);
            return skiff;
        }

        /// <summary>
        ///     Finds the next available index in the available map unit list
        /// </summary>
        /// <param name="map"></param>
        /// <returns>>= 0 is an index, or -1 means no room found</returns>
        private int FindNextFreeMapUnitIndex(Map.Maps map)
        {
            int nIndex = 0;
            foreach (MapUnit mapUnit in GetMapUnitCollection(map).AllMapUnits)
            {
                if (mapUnit is EmptyMapUnit) return nIndex;

                nIndex++;
            }

            return -1;
        }

        /// <summary>
        ///     Will load last known state from memory (originally disk) and recalculate some values
        ///     such as movement as required.
        ///     Called only once on load - the state of the large map will persist in and out of small maps
        /// </summary>
        /// <param name="map"></param>
        /// <param name="bInitialLoad"></param>
        /// <param name="searchItems"></param>
        /// <exception cref="ArgumentOutOfRangeException"></exception>
        private void GenerateMapUnitsForLargeMap(Map.Maps map, bool bInitialLoad, SearchItems searchItems)
        {
            MapUnitCollection mapUnitCollection;
            // the over and underworld animation states are already loaded and can stick around
            MapUnitStates currentMapUnitStates;
            switch (map)
            {
                case Map.Maps.Overworld:
                    mapUnitCollection = OverworldMapMapUnitCollection;
                    currentMapUnitStates =
                        bInitialLoad ? _importedGameState.OverworldMapUnitStates : new MapUnitStates();
                    break;
                case Map.Maps.Underworld:
                    mapUnitCollection = UnderworldMapUnitCollection;
                    currentMapUnitStates =
                        bInitialLoad ? _importedGameState.UnderworldMapUnitStates : new MapUnitStates();
                    break;
                case Map.Maps.Combat:
                case Map.Maps.Small:
                    throw new Ultima5ReduxException("You asked for a Small map when loading a large one");
                default:
                    throw new ArgumentOutOfRangeException(nameof(map), map, null);
            }

            // populate each of the map characters individually
            for (int i = 0; i < MAX_LEGACY_MAP_CHARACTERS; i++)
            {
                // if this is not the initial load of the map then we can trust character states and
                // movements that are already loaded into memory
                MapUnitMovement mapUnitMovement =
                    (bInitialLoad ? _importedMovements.GetMovement(i) : new MapUnitMovement(i)) ??
                    new MapUnitMovement(i);

                // always clear movements because they are not stored in the data for a LargeMap because
                // the monsters will recalculate every turn based on where the Avatar is 
                mapUnitMovement.ClearMovements();

                // we have retrieved the _currentMapUnitStates based on the map type,
                // now just get the existing animation state which persists on disk for under, over and small maps
                MapUnitState mapUnitState = currentMapUnitStates.GetCharacterState(i);

                MapUnitPosition mapUnitPosition = new(mapUnitState.X, mapUnitState.Y, 0);
                TileReference tileReference = mapUnitState.Tile1Ref;

                // the party is always at zero
                if (i == 0)
                {
                    MapUnit theAvatar = Avatar.CreateAvatar(
                        SmallMapReferences.SingleMapReference.Location.Britannia_Underworld, mapUnitMovement,
                        mapUnitPosition, tileReference, _bUseExtendedSprites);

                    mapUnitCollection.AddMapUnit(theAvatar);
                    continue;
                }

                MapUnit newUnit = CreateNewMapUnit(mapUnitMovement, bInitialLoad,
                    SmallMapReferences.SingleMapReference.Location.Britannia_Underworld, null, mapUnitPosition,
                    tileReference);
                // add the new character to our list of characters currently on the map
                mapUnitCollection.AddMapUnit(newUnit);
            }

            int nFloor = map == Map.Maps.Underworld ? -1 : 0;
            Dictionary<Point2D, List<SearchItem>> searchItemsInMap = searchItems.GetUnDiscoveredSearchItemsByLocation(
                SmallMapReferences.SingleMapReference.Location.Britannia_Underworld, nFloor);
            foreach (KeyValuePair<Point2D, List<SearchItem>> kvp in searchItemsInMap)
            {
                // at this point we are cycling through the positions
                MapUnitPosition mapUnitPosition = new(kvp.Key.X, kvp.Key.Y, nFloor);
                var discoverableLoot =
                    new DiscoverableLoot(SmallMapReferences.SingleMapReference.Location.Britannia_Underworld,
                        mapUnitPosition, kvp.Value);
                //discoverableLoot.MapUnitPosition.XY = kvp.Value;
                mapUnitCollection.AddMapUnit(discoverableLoot);
            }
        }

        /// <summary>
        ///     Force all map units to use or not use extended sprites based on _bUseExtendedSprites field
        /// </summary>
        private void SetAllExtendedSprites()
        {
            OverworldMapMapUnitCollection.AllMapUnits.ForEach(m => m.UseFourDirections = _bUseExtendedSprites);
            UnderworldMapUnitCollection.AllMapUnits.ForEach(m => m.UseFourDirections = _bUseExtendedSprites);
            SmallMapUnitCollection?.AllMapUnits.ForEach(m => m.UseFourDirections = _bUseExtendedSprites);
        }

        private void SetCommonAvatarMapUnit()
        {
            MasterAvatarMapUnit = GetAvatarMapUnit();
            GetMapUnitCollection(Map.Maps.Overworld).AllMapUnits[0] = MasterAvatarMapUnit;
            GetMapUnitCollection(Map.Maps.Underworld).AllMapUnits[0] = MasterAvatarMapUnit;
        }

        /// <summary>
        ///     Clear a current map unit, essentially removing it from the world
        ///     Commonly used when something is boarded, and collapses into the Avatar himself
        ///     note: the MapUnit is no longer referenced - but it will often exist within the Avatar
        ///     object if they have in fact boarded it
        /// </summary>
        /// <param name="mapUnitToClear"></param>
        /// <exception cref="Ultima5ReduxException"></exception>
        public void ClearAndSetEmptyMapUnits(MapUnit mapUnitToClear)
        {
            for (int index = 0; index < CurrentMapUnits.AllMapUnits.Count; index++)
            {
                MapUnit mapUnit = CurrentMapUnits.AllMapUnits[index];

                if (mapUnit != mapUnitToClear) continue;

                CurrentMapUnits.AllMapUnits[index] = new EmptyMapUnit();
                return;
            }

            throw new Ultima5ReduxException(
                "You provided a MapUnit to clear, but it is not in the active MapUnit list");
        }

        public void ClearEnemiesIfFarAway()
        {
            const float fMaxDiagonalDistance = 22;
            MapUnitPosition avatarPosition = CurrentAvatarPosition;

            int nMaxXY = CurrentMapType is Map.Maps.Overworld or Map.Maps.Underworld
                ? LargeMapLocationReferences.XTiles
                : 32;

            List<Enemy> enemiesToClear = null;
            foreach (Enemy enemy in CurrentMapUnits.Enemies)
            {
                if (enemy.MapUnitPosition.XY.DistanceBetweenWithWrapAround(avatarPosition.XY, nMaxXY) <=
                    fMaxDiagonalDistance)
                    continue;

                enemiesToClear ??= new List<Enemy>();
                // delete the mapunit
                enemiesToClear.Add(enemy);
            }

            enemiesToClear?.ForEach(ClearMapUnit);
        }

        public Enemy CreateEnemy(Point2D xy, EnemyReference enemyReference,
            SmallMapReferences.SingleMapReference singleMapReference, out int nIndex)
        {
            Debug.Assert(CurrentMapType != Map.Maps.Combat);

            nIndex = FindNextFreeMapUnitIndex(singleMapReference.MapType);
            if (nIndex == -1) return null;

            MapUnitPosition mapUnitPosition = new(xy.X, xy.Y, singleMapReference.Floor);
            Enemy enemy = new(new MapUnitMovement(0), enemyReference, singleMapReference.MapLocation, null,
                mapUnitPosition);

            GetMapUnitCollection(singleMapReference.MapType).AddMapUnit(enemy);

            enemy.UseFourDirections = _bUseExtendedSprites;

            return enemy;
        }

        public Enemy CreateEnemyOnCombatMap(Point2D xy, EnemyReference enemyReference, out int nIndex)
        {
            Debug.Assert(CurrentMapType == Map.Maps.Combat);
            nIndex = FindNextFreeMapUnitIndex(Map.Maps.Combat);
            if (nIndex == -1) return null;

            Enemy enemy = new(_importedMovements.GetMovement(nIndex), enemyReference, CurrentLocation, null,
                new MapUnitPosition(xy.X, xy.Y, 0));

            nIndex = AddCombatMapUnit(enemy);

            return enemy;
        }

        /// <summary>
        ///     Creates a new frigate at a dock of a given location
        /// </summary>
        /// <param name="location"></param>
        // ReSharper disable once UnusedMethodReturnValue.Global
        public Frigate CreateFrigateAtDock(SmallMapReferences.SingleMapReference.Location location) =>
            CreateFrigate(VirtualMap.GetLocationOfDock(location), Point2D.Direction.Right, out _, 1);

        public Horse CreateHorse(MapUnitPosition mapUnitPosition, Map.Maps map, out int nIndex)
        {
            nIndex = FindNextFreeMapUnitIndex(CurrentMapType);
            if (nIndex == -1) return null;

            Horse horse = new(_importedMovements.GetMovement(nIndex), CurrentLocation, Point2D.Direction.Right,
                null, mapUnitPosition)
            {
                MapUnitPosition = mapUnitPosition
            };

            // set position of frigate in the world
            AddNewMapUnit(map, horse, nIndex);
            return horse;
        }

        public MoonstoneNonAttackingUnit CreateMoonstoneNonAttackingUnit(Point2D xy, Moonstone moonstone,
            SmallMapReferences.SingleMapReference singleMapReference)
        {
            int nIndex = FindNextFreeMapUnitIndex(CurrentMapType);
            if (nIndex == -1) return null;

            MapUnitPosition mapUnitPosition = new(xy.X, xy.Y, singleMapReference.Floor);
            var moonstoneNonAttackingUnit =
                new MoonstoneNonAttackingUnit(moonstone, mapUnitPosition);

            // set position of frigate in the world
            GetMapUnitCollection(singleMapReference.MapType).AddMapUnit(moonstoneNonAttackingUnit);
            return moonstoneNonAttackingUnit;
        }

        public NonAttackingUnit CreateNonAttackUnitOnCombatMap(Point2D xy, int nSprite, out int nIndex)
        {
            Debug.Assert(CurrentMapType == Map.Maps.Combat);
            nIndex = FindNextFreeMapUnitIndex(Map.Maps.Combat);
            if (nIndex == -1) return null;

            MapUnitPosition mapUnitPosition = new(xy.X, xy.Y, 0);
            NonAttackingUnit nonAttackingUnit = NonAttackingUnitFactory.Create(nSprite,
                SmallMapReferences.SingleMapReference.Location.Combat_resting_shrine, mapUnitPosition);

            if (nonAttackingUnit == null)
                throw new Ultima5ReduxException(
                    $"Tried to create NonAttackingUnitFactory: {nSprite} but was given null");

            nIndex = AddCombatMapUnit(nonAttackingUnit);

            return nonAttackingUnit;
        }

        /// <summary>
        ///     Creates a new skiff and places it at a given location
        /// </summary>
        /// <param name="location"></param>
        /// <returns></returns>
        // ReSharper disable once UnusedMethodReturnValue.Global
        public Skiff CreateSkiffAtDock(SmallMapReferences.SingleMapReference.Location location) =>
            CreateSkiff(VirtualMap.GetLocationOfDock(location), Point2D.Direction.Right, out _);

        public Avatar GetAvatarMapUnit()
        {
            if (CurrentMapUnits == null)
                throw new Ultima5ReduxException("Tried to get Avatar but CurrentMapUnits is null");
            if (CurrentMapUnits.TheAvatar == null)
                throw new Ultima5ReduxException("Tried to get Avatar but CurrentMapUnits.TheAvatar is null");

            return CurrentMapUnits.TheAvatar;
        }

        /// <summary>
        ///     Gets a particular map unit on a tile in a given location
        /// </summary>
        /// <param name="map"></param>
        /// <param name="xy"></param>
        /// <param name="nFloor"></param>
        /// <returns>MapUnit or null if non exist at location</returns>
        public List<MapUnit> GetMapUnitsByPosition(Map.Maps map, in Point2D xy, int nFloor)
        {
            List<MapUnit> mapUnits = new();

            foreach (MapUnit mapUnit in GetMapUnitCollection(map).AllMapUnits)
            {
                if (!mapUnit.IsActive) continue;

                if (mapUnit.MapUnitPosition.X == xy.X && mapUnit.MapUnitPosition.Y == xy.Y &&
                    mapUnit.MapUnitPosition.Floor == nFloor)
                {
                    int nTileIndex = mapUnit.KeyTileReference.Index;
                    if (GameStateReference.State.PlayerInventory.DoIHaveSpecialTileReferenceIndex(nTileIndex)) continue;
                    mapUnits.Add(mapUnit);
                }
            }

            return mapUnits;
        }

        public T GetSpecificMapUnitByLocation<T>(Map.Maps map, Point2D xy, int nFloor, bool bCheckBaseToo = false)
            where T : MapUnit
        {
            foreach (T mapUnit in GetMapUnitCollection(map).GetMapUnitByType<T>())
            {
                if (mapUnit == null)
                    throw new Ultima5ReduxException(
                        "Getting a specific map unit by location, but the list has a null entry");
                // sometimes characters are null because they don't exist - and that is OK
                if (!mapUnit.IsActive) continue;

                if (mapUnit.MapUnitPosition.XY == xy &&
                    mapUnit.MapUnitPosition.Floor == nFloor) //&& mapUnit.MapLocation == location)
                {
                    if (bCheckBaseToo && mapUnit.GetType().BaseType == typeof(T)) return mapUnit;
                    // the map unit is at the right position AND is the correct type
                    Debug.Assert(mapUnit != null);
                    if (mapUnit.GetType() == typeof(T)) return mapUnit;
                }
            }

            return null;
        }

        public void InitializeCombatMapReferences()
        {
            CombatMapMapUnitCollection.Clear();
            for (int i = 0; i < MAX_MAP_CHARACTERS; i++)
            {
                CombatMapMapUnitCollection.Add(new EmptyMapUnit());
            }
        }

        public bool IsTileOccupied(Point2D xy)
        {
            return CurrentMapUnits.AllActiveMapUnits.Any(m => m.MapUnitPosition.XY == xy);
        }

        public Skiff MakeAndBoardSkiff()
        {
            Skiff skiff = CreateSkiff(GetAvatarMapUnit().MapUnitPosition.XY, GetAvatarMapUnit().Direction,
                out int _);
            GetAvatarMapUnit().BoardMapUnit(skiff);
            ClearAndSetEmptyMapUnits(skiff);
            return skiff;
        }

        /// <summary>
        ///     Places an existing non attacking unit on a map
        ///     This is often used when an item stack exists in a chest OR if an enemy leaves a body or blood spatter
        /// </summary>
        /// <param name="nonAttackingUnit"></param>
        /// <param name="mapUnitPosition"></param>
        /// <param name="map"></param>
        /// <returns></returns>
        public bool PlaceNonAttackingUnit(NonAttackingUnit nonAttackingUnit, MapUnitPosition mapUnitPosition,
            Map.Maps map)
        {
            int nIndex = FindNextFreeMapUnitIndex(CurrentMapType);
            if (nIndex == -1) return false;

            nonAttackingUnit.MapUnitPosition = mapUnitPosition;

            // set position of frigate in the world
            AddNewMapUnit(map, nonAttackingUnit, nIndex);
            return true;
        }

        public bool RePlaceNonAttackingUnit(NonAttackingUnit originalNonAttackingUnit,
            NonAttackingUnit replacementNonAttackingUnit, MapUnitPosition mapUnitPosition,
            Map.Maps map)
        {
            int nIndex = 0;
            bool bFound = false;
            foreach (MapUnit mapUnit in GetMapUnitCollection(map).AllMapUnits)
            {
                if (mapUnit == originalNonAttackingUnit)
                {
                    bFound = true;
                    break;
                }

                nIndex++;
            }

            if (!bFound)
                throw new Ultima5ReduxException(
                    $"Tried to replace NonAttackingMapUnit but could find in the current MapUnit list (orgi: {originalNonAttackingUnit.FriendlyName}");

            // int nIndex = FindNextFreeMapUnitIndex(CurrentMapType);
            // if (nIndex == -1) return false;

            replacementNonAttackingUnit.MapUnitPosition = mapUnitPosition;

            // set position of frigate in the world
            AddNewMapUnit(map, replacementNonAttackingUnit, nIndex);
            return true;
        }

        /// <summary>
        ///     Sets the current map type
        ///     Called internally to the class only since it has the bLoadFromDisk option
        /// </summary>
        /// <param name="mapRef"></param>
        /// <param name="mapType"></param>
        /// <param name="searchItems"></param>
        /// <param name="bLoadFromDisk"></param>
        /// <exception cref="ArgumentOutOfRangeException"></exception>
        public void SetCurrentMapType(SmallMapReferences.SingleMapReference mapRef, Map.Maps mapType,
            SearchItems searchItems, bool bLoadFromDisk = false)
        {
            SetCurrentMapTypeNoLoad(mapRef, mapType, false);

            switch (mapType)
            {
                case Map.Maps.Small:
                    LoadSmallMap(mapRef.MapLocation, bLoadFromDisk, searchItems);
                    // will reload the search items fresh since we don't save every single small
                    // map to the save file
                    break;
                case Map.Maps.Combat:
                    return;
                case Map.Maps.Overworld:
                case Map.Maps.Underworld:
                    CombatMapMapUnitCollection.Clear();
                    // search items should only be loaded once and then never again
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(mapType), mapType, null);
            }

            GetAvatarMapUnit().MapLocation = mapRef.MapLocation;
            GetAvatarMapUnit().MapUnitPosition.Floor = mapRef.Floor;
        }

        /// <summary>
        ///     UNSAFE - this assumes the small map is already loaded into memory - it instead just switches the
        ///     current map units back over to it.
        /// </summary>
        /// <param name="mapRef"></param>
        /// <param name="mapType"></param>
        /// <exception cref="Ultima5ReduxException"></exception>
        public void SetCurrentMapTypeNoLoad(SmallMapReferences.SingleMapReference mapRef, Map.Maps mapType,
            bool bSetAvatar = true)
        {
            CurrentMapType = mapType;
            if (mapRef == null)
                throw new Ultima5ReduxException("Passed a null map ref to SetCurrentMapType");

            CurrentLocation = mapRef.MapLocation;

            // I may need make an additional save of state before wiping these MapUnits out
            GameStateReference.State.CharacterRecords.ClearCombatStatuses();

            if (bSetAvatar)
            {
                GetAvatarMapUnit().MapLocation = mapRef.MapLocation;
                GetAvatarMapUnit().MapUnitPosition.Floor = mapRef.Floor;
            }
        }

        /// <summary>
        ///     Makes the Avatar exit the current MapUnit they are occupying
        /// </summary>
        /// <returns>The MapUnit object they were occupying - you need to re-add it the map after</returns>
        public MapUnit XitCurrentMapUnit(VirtualMap virtualMap, out string retStr)
        {
            retStr = GameReferences.DataOvlRef.StringReferences.GetString(DataOvlReference.KeypressCommandsStrings.XIT)
                .TrimEnd();

            if (!virtualMap.TheMapUnits.GetAvatarMapUnit().IsAvatarOnBoardedThing)
            {
                retStr += " " + GameReferences.DataOvlRef.StringReferences
                    .GetString(DataOvlReference.KeypressCommandsStrings.WHAT_Q).Trim();
                return null;
            }

            if (!GetAvatarMapUnit().CurrentBoardedMapUnit.CanBeExited(virtualMap))
            {
                retStr += "\n" + GameReferences.DataOvlRef.StringReferences
                    .GetString(DataOvlReference.SleepTransportStrings.N_NO_LAND_NEARBY_BANG_N).Trim();
                return null;
            }

            MapUnit unboardedMapUnit = GetAvatarMapUnit().UnboardedAvatar();
            Debug.Assert(unboardedMapUnit != null);

            // set the current positions to the equal the Avatar's as he exits the vehicle 
            unboardedMapUnit.MapLocation = CurrentLocation;
            unboardedMapUnit.MapUnitPosition = CurrentAvatarPosition;
            unboardedMapUnit.Direction = GetAvatarMapUnit().Direction;
            unboardedMapUnit.KeyTileReference = unboardedMapUnit.GetNonBoardedTileReference();

            AddNewMapUnit(CurrentMapType, unboardedMapUnit);
            retStr += " " + unboardedMapUnit.BoardXitName;

            // if the Avatar is on a frigate then we will check for Skiffs and exit on a skiff instead
            if (unboardedMapUnit is not Frigate avatarFrigate) return unboardedMapUnit;

            Debug.Assert(avatarFrigate != null, nameof(avatarFrigate) + " != null");

            // if we have skiffs, AND do not have land close by then we deploy a skiff
            if (avatarFrigate.SkiffsAboard <= 0 || virtualMap.IsLandNearby()) return unboardedMapUnit;

            MakeAndBoardSkiff();
            avatarFrigate.SkiffsAboard--;

            return unboardedMapUnit;
        }
    }
}