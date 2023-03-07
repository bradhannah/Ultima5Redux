using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.Serialization;
using Newtonsoft.Json;
using Ultima5Redux.MapUnits;
using Ultima5Redux.MapUnits.CombatMapUnits;
using Ultima5Redux.MapUnits.SeaFaringVessels;
using Ultima5Redux.MapUnits.TurnResults;
using Ultima5Redux.PlayerCharacters.Inventory;
using Ultima5Redux.References;
using Ultima5Redux.References.Maps;
using Ultima5Redux.References.MapUnits.NonPlayerCharacters;

namespace Ultima5Redux.Maps
{
    [DataContract]
    public sealed class LargeMap : RegularMap
    {
        [DataMember(Name = "BottomRightExtent")]
        private Point2D _bottomRightExtent;

        [DataMember(Name = "TopLeftExtent")] private Point2D _topLeftExtent;

        [DataMember] public LargeMapLocationReferences.LargeMapType TheLargeLheLargeMapType { get; private set; }

        [IgnoreDataMember] public override bool IsRepeatingMap => true;

        [IgnoreDataMember] public override int NumOfXTiles => LargeMapLocationReferences.X_TILES;
        [IgnoreDataMember] public override int NumOfYTiles => LargeMapLocationReferences.Y_TILES;

        [IgnoreDataMember]
        [SuppressMessage("ReSharper", "UnusedMember.Global")]
        public bool ShowOuterSmallMapTiles => false;

        [IgnoreDataMember] public override byte[][] TheMap { get; protected set; }

        private SmallMapReferences.SingleMapReference _currentSingleMapReference;


        public override SmallMapReferences.SingleMapReference CurrentSingleMapReference =>
            _currentSingleMapReference ??=
                SmallMapReferences.SingleMapReference.GetLargeMapSingleInstance(TheLargeLheLargeMapType);

        public override Maps TheMapType => TheLargeLheLargeMapType == LargeMapLocationReferences.LargeMapType.Overworld
            ? Maps.Overworld
            : Maps.Underworld;


        [JsonConstructor]
        private LargeMap()
        {
        }

        /// <summary>
        ///     Build a large map. There are essentially two choices - Overworld and Underworld
        /// </summary>
        /// <param name="lheLargeMapType"></param>
        public LargeMap(LargeMapLocationReferences.LargeMapType lheLargeMapType) : base(
            SmallMapReferences.SingleMapReference.Location.Britannia_Underworld, (int)lheLargeMapType)
        {
            TheLargeLheLargeMapType = lheLargeMapType;

            BuildMap(lheLargeMapType);
        }

        [OnDeserialized]
        private void PostDeserialize(StreamingContext context)
        {
            BuildMap(TheLargeLheLargeMapType);
        }

        internal override WalkableType GetWalkableTypeByMapUnit(MapUnit mapUnit) =>
            mapUnit switch
            {
                Enemy enemy => enemy.EnemyReference.IsWaterEnemy
                    ? WalkableType.CombatWater
                    : WalkableType.StandardWalking,
                CombatPlayer => WalkableType.StandardWalking,
                _ => WalkableType.StandardWalking
            };

        internal override void ProcessTileEffectsForMapUnit(TurnResults turnResults, MapUnit mapUnit)
        {
            // TBD
        }

        /// <summary>
        ///     Creates a Frigate and places it in on the map
        /// </summary>
        /// <param name="xy"></param>
        /// <param name="direction"></param>
        /// <param name="nIndex"></param>
        /// <param name="nSkiffsAboard"></param>
        /// <returns></returns>
        [SuppressMessage("ReSharper", "OutParameterValueIsAlwaysDiscarded.Global")]
        internal Frigate CreateFrigate(Point2D xy, Point2D.Direction direction, out int nIndex, int nSkiffsAboard)
        {
            nIndex = FindNextFreeMapUnitIndex();

            if (nIndex == -1) return null;

            Frigate frigate = new(new MapUnitMovement(nIndex),
                SmallMapReferences.SingleMapReference.Location.Britannia_Underworld, direction, null,
                new MapUnitPosition(xy.X, xy.Y, 0))
            {
                // set position of frigate in the world
                SkiffsAboard = nSkiffsAboard,
                KeyTileReference =
                    GameReferences.Instance.SpriteTileReferences.GetTileReferenceByName("ShipNoSailsLeft")
            };

            AddNewMapUnit(frigate, nIndex);
            return frigate;
        }

        /// <summary>
        ///     Decides if any enemies needed to be spawned or de-spawned
        /// </summary>
        internal void GenerateAndCleanupEnemies(int oneInXOddsOfNewMonster, int nTurn)
        {
            ClearEnemiesIfFarAway();

            if (TotalMapUnitsOnMap >= MAX_MAP_CHARACTERS) return;
            if (oneInXOddsOfNewMonster > 0 && Utils.OneInXOdds(oneInXOddsOfNewMonster))
                // make a random monster
                CreateRandomMonster(nTurn);
        }

        internal void InitializeFromLegacy(SearchItems searchItems, ImportedGameState importedGameState)
        {
            GenerateMapUnitsForLargeMapForLegacyImport(TheLargeLheLargeMapType, true, searchItems, importedGameState);
        }


        internal bool IsAllowedToBuryMoongate()
        {
            // don't bury one on top of the other
            if (GameStateReference.State.TheMoongates.IsMoonstoneBuried(CurrentPosition.XYZ)) return false;

            // we check the current terrain and make sure it's buriable
            TileReference tileRef = GetTileReferenceOnCurrentTile();
            return GameReferences.Instance.SpriteTileReferences.IsMoonstoneBuriable(tileRef.Index);
        }

        // ReSharper disable once UnusedMember.Global
        internal void PrintMap()
        {
            PrintMapSection(TheMap, 0, 0, 160, 80);
        }

        internal Moonstone SearchAndExposeMoonstone(in Point2D xy)
        {
            // check for moonstones
            // moonstone check
            if (!GameStateReference.State.TheMoongates.IsMoonstoneBuried(xy, TheMapType)) return null;

            MoonPhaseReferences.MoonPhases moonPhase =
                GameStateReference.State.TheMoongates.GetMoonPhaseByPosition(xy, TheMapType);
            Moonstone moonstone =
                GameStateReference.State.PlayerInventory.TheMoonstones.Items[moonPhase];

            CreateMoonstoneNonAttackingUnit(xy, moonstone, _currentSingleMapReference);

            GameStateReference.State.TheMoongates.SetMoonstoneBuried((int)moonPhase, false);

            return moonstone;
        }


        private void BuildMap(LargeMapLocationReferences.LargeMapType largeMapType)
        {
            TheMap = LargeMapLocationReferences.GetMap(largeMapType);
        }

        private void ClearEnemiesIfFarAway()
        {
            const float fMaxDiagonalDistance = 22;
            MapUnitPosition avatarPosition = CurrentAvatarPosition;

            int nMaxXy = TheMapType is Maps.Overworld or Maps.Underworld
                ? LargeMapLocationReferences.X_TILES
                : 32;

            List<Enemy> enemiesToClear = null;
            foreach (Enemy enemy in CurrentMapUnits.Enemies)
            {
                if (enemy.MapUnitPosition.XY.DistanceBetweenWithWrapAround(avatarPosition.XY, nMaxXy) <=
                    fMaxDiagonalDistance)
                    continue;

                enemiesToClear ??= new List<Enemy>();
                // delete the mapunit
                enemiesToClear.Add(enemy);
            }

            enemiesToClear?.ForEach(ClearMapUnit);
        }

        /// <summary>
        /// </summary>
        /// <returns>true if a monster was created</returns>
        /// <remarks>see gameSpawnCreature in xu4 for similar method</remarks>
        // ReSharper disable once UnusedMethodReturnValue.Local
        private bool CreateRandomMonster(int nTurn)
        {
            const int maxTries = 10;
            const int nDistanceAway = 7;

            // find a position or give up
            int dX = nDistanceAway;
            for (int i = 0; i < maxTries; i++)
            {
                int dY = Utils.Ran.Next() % nDistanceAway;

                // this logic borrowed from Xu4 to create some randomness
                if (Utils.OneInXOdds(2)) dX = -dX;
                if (Utils.OneInXOdds(2)) dY = -dY;
                if (Utils.OneInXOdds(2)) Utils.SwapInts(ref dX, ref dY);

                Point2D tilePosition = new((CurrentPosition.X + dX) % NumOfXTiles,
                    (CurrentPosition.Y + dY) % NumOfYTiles);
                tilePosition.AdjustXAndYToMax(NumOfXTiles);

                if (IsTileOccupied(tilePosition)) continue;

                // it's not occupied so we can create a monster
                EnemyReference enemyRef =
                    GameReferences.Instance.EnemyRefs.GetRandomEnemyReferenceByEraAndTile(nTurn,
                        GetTileReference(tilePosition));
                if (enemyRef == null) continue;

                // add the new character to our list of characters currently on the map
                Enemy _ = CreateEnemy(tilePosition, enemyRef, CurrentSingleMapReference,
                    out int _);

                return true;
            }

            return false;
        }

        /// <summary>
        ///     Will load last known state from memory (originally disk) and recalculate some values
        ///     such as movement as required.
        ///     Called only once on load - the state of the large map will persist in and out of small maps
        /// </summary>
        /// <param name="largeMapType"></param>
        /// <param name="bInitialLoad"></param>
        /// <param name="searchItems"></param>
        /// <param name="importedGameState"></param>
        /// <exception cref="ArgumentOutOfRangeException"></exception>
        private void GenerateMapUnitsForLargeMapForLegacyImport(LargeMapLocationReferences.LargeMapType largeMapType,
            bool bInitialLoad, SearchItems searchItems, ImportedGameState importedGameState)
        {
            // the over and underworld animation states are already loaded and can stick around
            MapUnitStates currentMapUnitStates = largeMapType switch
            {
                LargeMapLocationReferences.LargeMapType.Overworld => bInitialLoad
                    ? importedGameState.OverworldMapUnitStates
                    : new MapUnitStates(),
                LargeMapLocationReferences.LargeMapType.Underworld => bInitialLoad
                    ? importedGameState.UnderworldMapUnitStates
                    : new MapUnitStates(),
                _ => throw new ArgumentOutOfRangeException(nameof(largeMapType), largeMapType, null)
            };

            // populate each of the map characters individually
            for (int i = 0; i < MAX_LEGACY_MAP_CHARACTERS; i++)
            {
                // if this is not the initial load of the map then we can trust character states and
                // movements that are already loaded into memory
                var mapUnitMovement =
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
                        mapUnitPosition, tileReference, UseExtendedSprites);

                    CurrentMapUnits.AddMapUnit(theAvatar);
                    continue;
                }

                MapUnit newUnit = CreateNewMapUnit(mapUnitMovement, bInitialLoad,
                    SmallMapReferences.SingleMapReference.Location.Britannia_Underworld, null, mapUnitPosition,
                    tileReference);
                // add the new character to our list of characters currently on the map
                CurrentMapUnits.AddMapUnit(newUnit);
            }

            int nFloor = largeMapType == LargeMapLocationReferences.LargeMapType.Underworld ? -1 : 0;
            Dictionary<Point2D, List<SearchItem>> searchItemsInMap = searchItems.GetUnDiscoveredSearchItemsByLocation(
                SmallMapReferences.SingleMapReference.Location.Britannia_Underworld, nFloor);
            // ReSharper disable once ForeachCanBePartlyConvertedToQueryUsingAnotherGetEnumerator
            foreach (KeyValuePair<Point2D, List<SearchItem>> kvp in searchItemsInMap)
            {
                // at this point we are cycling through the positions
                MapUnitPosition mapUnitPosition = new(kvp.Key.X, kvp.Key.Y, nFloor);
                var discoverableLoot =
                    new DiscoverableLoot(SmallMapReferences.SingleMapReference.Location.Britannia_Underworld,
                        mapUnitPosition, kvp.Value);
                CurrentMapUnits.AddMapUnit(discoverableLoot);
            }

            int nTotalMapUnits = CurrentMapUnits.AllMapUnits.Count;
            for (int i = 0; i < MAX_MAP_CHARACTERS - nTotalMapUnits; i++)
            {
                var emptyMapUnit = new EmptyMapUnit();
                // add the new character to our list of characters currently on the map
                CurrentMapUnits.AddMapUnit(emptyMapUnit);
            }
        }

        [SuppressMessage("ReSharper", "UnusedMember.Global")]
        public static Maps GetLargeMapTypeToMapType(LargeMapLocationReferences.LargeMapType largeMapType) =>
            largeMapType switch
            {
                LargeMapLocationReferences.LargeMapType.Overworld => Maps.Overworld,
                LargeMapLocationReferences.LargeMapType.Underworld => Maps.Underworld,
                _ => throw new ArgumentOutOfRangeException(nameof(largeMapType), largeMapType, null)
            };

        public static LargeMapLocationReferences.LargeMapType GetMapTypeToLargeMapType(Maps map) =>
            // ReSharper disable once SwitchExpressionHandlesSomeKnownEnumValuesWithExceptionInDefault
            map switch
            {
                Maps.Overworld => LargeMapLocationReferences.LargeMapType.Overworld,
                Maps.Underworld => LargeMapLocationReferences.LargeMapType.Underworld,
                _ => throw new ArgumentOutOfRangeException(nameof(map), map, null)
            };

        public override void RecalculateVisibleTiles(in Point2D initialFloodFillPosition)
        {
            if (XRayMode)
            {
                Utils.Set2DArrayAllToValue(VisibleOnMap, true);
                return;
            }

            TouchedOuterBorder = false;

            AvatarXyPos = initialFloodFillPosition;

            _topLeftExtent = new Point2D(AvatarXyPos.X - VISIBLE_IN_EACH_DIRECTION_OF_AVATAR,
                AvatarXyPos.Y - VISIBLE_IN_EACH_DIRECTION_OF_AVATAR);
            _bottomRightExtent = new Point2D(AvatarXyPos.X + VISIBLE_IN_EACH_DIRECTION_OF_AVATAR,
                AvatarXyPos.Y + VISIBLE_IN_EACH_DIRECTION_OF_AVATAR);

            RefreshTestForVisibility(1);
            SetMaxVisibleArea(AvatarXyPos, TOTAL_VISIBLE_TILES);
            FloodFillMap(AvatarXyPos.X, AvatarXyPos.Y, true);
            RecalculatedHash = Utils.Ran.Next();
        }

        [SuppressMessage("ReSharper", "MemberCanBePrivate.Global")]
        public TileReference GetTileReference(in Point2D xy, bool bIgnoreMoongate = false)
        {
            // if it's a large map and there should be a moongate and it's nighttime then it's a moongate!
            // bajh: March 22, 2020 - we are going to try to always include the Moongate, and let the game decide what it wants to do with it
            if (!bIgnoreMoongate &&
                GameStateReference.State.TheMoongates.IsMoonstoneBuried(new Point3D(xy.X, xy.Y,
                    TheLargeLheLargeMapType == LargeMapLocationReferences.LargeMapType.Overworld ? 0 : 0xFF)))
                return GameReferences.Instance.SpriteTileReferences.GetTileReferenceByName("Moongate") ??
                       throw new Ultima5ReduxException("Supposed to get a moongate override: " + xy);

            return base.GetTileReference(xy);
        }

        protected override float GetAStarWeight(in Point2D xy) => 1;
    }
}