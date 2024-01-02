using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.Serialization;
using Newtonsoft.Json;
using Ultima5Redux.MapUnits;
using Ultima5Redux.MapUnits.CombatMapUnits;
using Ultima5Redux.MapUnits.NonPlayerCharacters;
using Ultima5Redux.MapUnits.SeaFaringVessels;
using Ultima5Redux.PlayerCharacters;
using Ultima5Redux.References;
using Ultima5Redux.References.Maps;
using Ultima5Redux.References.MapUnits.NonPlayerCharacters;

// ReSharper disable UnusedMember.Global
// ReSharper disable IdentifierTypo
namespace Ultima5Redux.Maps {
    [DataContract] public partial class VirtualMap {
        [DataMember] public int OneInXOddsOfNewMonster { get; set; } = 16;

        /// <summary>
        ///     The latest Map references including position and location. This is used primarily for
        ///     CurrentMap lookup and saving and loading from disk
        /// </summary>
        [DataMember]
        public SavedMapRefs SavedMapRefs { get; private set; }

        /// <summary>
        ///     Contains all Maps. The single source of truth
        /// </summary>
        [DataMember]
        public MapHolder TheMapHolder { get; private set; }

        [DataMember] public SearchItems TheSearchItems { get; private set; }

        /// <summary>
        ///     Used when entering combat to save your previous position
        /// </summary>
        [IgnoreDataMember]
        private SavedMapRefs PreTeleportMapSavedMapRefs { get; set; }


        /// <summary>
        ///     The abstracted Map object for the current map
        ///     Returns large or small depending on what is active
        /// </summary>
        [IgnoreDataMember]
        public Map CurrentMap {
            get {
                SavedMapRefs savedMapRefs = SavedMapRefs;
                if (savedMapRefs == null) return null;
                SmallMapReferences.SingleMapReference singleMapReference = savedMapRefs.GetSingleMapReference();
                if (singleMapReference == null)
                    throw new Ultima5ReduxException("Tried to get CurrentMap but it was false");

                if (singleMapReference.IsDungeon) return TheMapHolder.TheDungeonMap;

                switch (savedMapRefs.Location) {
                    case SmallMapReferences.SingleMapReference.Location.Combat_resting_shrine
                        when savedMapRefs.MapType == Map.Maps.Combat:
                        return TheMapHolder.TheCombatMap;
                    case SmallMapReferences.SingleMapReference.Location.Combat_resting_shrine:
                        if (savedMapRefs.Floor is >= 0 and < 4)
                            return TheMapHolder.CutSceneMap;
                        return TheMapHolder.IntroSceneMap;
                    //throw
                    //new Ultima5ReduxException("Resting and Shrines have not been implemented yet"),
                    case SmallMapReferences.SingleMapReference.Location.Britannia_Underworld:
                        return savedMapRefs.Floor == 0 ? TheMapHolder.OverworldMap : TheMapHolder.UnderworldMap;
                    default:
                        return TheMapHolder.GetSmallMap(singleMapReference);
                }
            }
        }

        /// <summary>
        ///     Construct the VirtualMap (requires initialization still)
        ///     This is only called on legacy game initiation - future 'new' loads will use deserialization
        /// </summary>
        /// <param name="initialMap"></param>
        /// <param name="currentSmallMapReference"></param>
        /// <param name="bUseExtendedSprites"></param>
        /// <param name="importedGameState"></param>
        /// <param name="theSearchItems"></param>
        internal VirtualMap(
            Map.Maps initialMap,
            // ReSharper disable once UnusedParameter.Local
            SmallMapReferences.SingleMapReference currentSmallMapReference, bool bUseExtendedSprites,
            ImportedGameState importedGameState, SearchItems theSearchItems) {
            TheSearchItems = theSearchItems;
            TheMapHolder = new MapHolder();

            TheMapHolder.OverworldMap.InitializeFromLegacy(theSearchItems, importedGameState);
            TheMapHolder.UnderworldMap.InitializeFromLegacy(theSearchItems, importedGameState);

            switch (initialMap) {
                case Map.Maps.Small:
                    LoadSmallMap(currentSmallMapReference, null, !importedGameState.IsInitialSaveFile,
                        importedGameState);
                    if (CurrentMap is not SmallMap)
                        throw new Ultima5ReduxException("Tried to load Small Map initially but wasn't set correctly");
                    break;
                case Map.Maps.Overworld:
                case Map.Maps.Underworld:
                    LoadLargeMap(LargeMap.GetMapTypeToLargeMapType(initialMap));
                    break;
                case Map.Maps.Combat:
                    throw new Ultima5ReduxException("Can't load a Combat Map on the initialization of a virtual map");
                case Map.Maps.Dungeon:
                    throw new Ultima5ReduxException(
                        "Can't load a Dungeon Map on the initialization of a virtual map (yet?)");
                default:
                    throw new ArgumentOutOfRangeException(nameof(initialMap), initialMap, null);
            }
        }

        [JsonConstructor] private VirtualMap() {
        }

        [OnDeserialized] private void PostDeserialize(StreamingContext context) {
            TheSearchItems = new SearchItems();
            TheSearchItems.Initialize();
        }


        private void ClearAllFlagsBeforeMapLoad() {
            // this likely means it's our first load and there is nothing to clear
            if (SavedMapRefs == null) return;
            switch (CurrentMap) {
                case SmallMap smallMap:
                    smallMap.ClearSmallMapFlags();
                    break;
                case CombatMap:
                    GameStateReference.State.CharacterRecords.ClearCombatStatuses();
                    break;
            }
        }

        private int GetCalculatedSpriteIndexByTile(TileReference tileReference, in Point2D tilePosInMap,
            bool bIsAvatarTile, bool bIsMapUnitOccupiedTile, MapUnit mapUnit, out bool bDrawCharacterOnTile) {
            int nSprite = tileReference.Index;
            bool bIsMirror = TileReferences.IsUnbrokenMirror(nSprite);
            bDrawCharacterOnTile = false;

            if (bIsMirror) {
                // if the avatar is south of the mirror then show his image
                Point2D expectedAvatarPos = new(tilePosInMap.X, tilePosInMap.Y + 1);
                if (expectedAvatarPos == CurrentMap.CurrentPosition.XY)
                    return GameReferences.Instance.SpriteTileReferences.GetTileNumberByName("MirrorAvatar");
            }

            // is the sprite a Chair? if so, we need to figure out if someone is sitting on it
            bool bIsChair = TileReferences.IsChair(nSprite);
            // bh: i should clean this up so that it doesn't need to call all this - since it's being called in GetCorrectSprite
            bool bIsLadder = TileReferences.IsLadder(nSprite);
            // is it the human sleeping side of the bed?
            bool bIsHeadOfBed = TileReferences.IsHeadOfBed(nSprite);
            // we need to check these before they get "corrected"
            // is it the stocks
            bool bIsStocks = TileReferences.IsStocks(nSprite);
            bool bIsManacles = TileReferences.IsManacles(nSprite); // is it shackles/manacles

            // this is unfortunate since I would prefer the GetCorrectSprite took care of all of this
            bool bIsFoodNearby = TileReferences.IsChair(nSprite) && CurrentMap.IsFoodNearby(tilePosInMap);

            bool bIsStaircase = TileReferences.IsStaircase(nSprite); // is it a staircase

            int nNewSpriteIndex;

            if (bIsStaircase && CurrentMap is SmallMap smallMap)
                nNewSpriteIndex = smallMap.GetStairsSprite(tilePosInMap).Index;
            else {
                // kinda hacky - but check if it's a minstrel because when they are on a chair, they play!
                if (bIsMapUnitOccupiedTile && mapUnit is NonPlayerCharacter { IsMinstrel: true } npc
                                           && nSprite == (int)TileReference.SpriteIndex.ChairBackBack &&
                                           !bIsFoodNearby) {
                    nNewSpriteIndex = npc.AlternateSittingTileReference.Index;
                }
                else {
                    nNewSpriteIndex = GameReferences.Instance.SpriteTileReferences.GetCorrectSprite(nSprite,
                        bIsMapUnitOccupiedTile,
                        bIsAvatarTile, bIsFoodNearby, GameStateReference.State.TheTimeOfDay.IsDayLight);
                }
            }

            if (nNewSpriteIndex == -2) nNewSpriteIndex = CurrentMap.GuessTile(tilePosInMap);

            bDrawCharacterOnTile = !bIsChair && !bIsLadder && !bIsHeadOfBed && !bIsStocks && !bIsManacles &&
                                   bIsMapUnitOccupiedTile;

            return nNewSpriteIndex;
        }


        /// <summary>
        ///     Loads a combat map as the current map
        ///     Saves the previous map state, for post combat
        /// </summary>
        /// <param name="singleCombatMapReference"></param>
        /// <param name="entryDirection"></param>
        /// <param name="records"></param>
        /// <param name="primaryEnemyReference"></param>
        /// <param name="nPrimaryEnemies"></param>
        /// <param name="secondaryEnemyReference"></param>
        /// <param name="nSecondaryEnemies"></param>
        /// <param name="npcRef"></param>
        private void LoadCombatMap(SingleCombatMapReference singleCombatMapReference,
            SingleCombatMapReference.EntryDirection entryDirection, PlayerCharacterRecords records,
            EnemyReference primaryEnemyReference, int nPrimaryEnemies, EnemyReference secondaryEnemyReference,
            int nSecondaryEnemies, NonPlayerCharacterReference npcRef) {
            if (npcRef != null)
                Debug.Assert(nPrimaryEnemies == 1 && nSecondaryEnemies == 0,
                    "when assigning an NPC, you must have single enemy");

            SavePreTeleportMapRefs();

            TheMapHolder.TheCombatMap = new CombatMap(singleCombatMapReference);

            SavedMapRefs.SetBySingleCombatMapReference(singleCombatMapReference);

            // we only want to push the exposed items and override map if we are on a small or large map 
            // not if we are going combat to combat map (think Debug)
            if (CurrentMap is not CombatMap combatMap)
                throw new Ultima5ReduxException("Loading combat map, but it didn't flip to combat map");

            combatMap.CreateParty(entryDirection, records);

            combatMap.CreateEnemies(singleCombatMapReference, primaryEnemyReference, nPrimaryEnemies,
                secondaryEnemyReference, nSecondaryEnemies, npcRef);

            combatMap.InitializeInitiativeQueue();
        }

        private void SavePreTeleportMapRefs() {
            if (PreTeleportMapSavedMapRefs != null && CurrentMap is CombatMap or CutSceneMap) return;
            Debug.Assert(CurrentMap is RegularMap,
                "You can't load a combat map when you are already in a combat map");
            // save the existing map details to return to
            // we also grab the latest XYZ and save it since we don't keep track of the 
            // the XY coords expect at load and save
            PreTeleportMapSavedMapRefs = SavedMapRefs.Copy();
            PreTeleportMapSavedMapRefs.MapUnitPosition.Floor = CurrentMap.CurrentPosition.Floor;
            PreTeleportMapSavedMapRefs.MapUnitPosition.X = CurrentMap.CurrentPosition.X;
            PreTeleportMapSavedMapRefs.MapUnitPosition.Y = CurrentMap.CurrentPosition.Y;
        }


        public bool ContainsSearchableMapUnits(in Point2D xy) {
            // moonstone check
            List<MapUnit> mapUnits = CurrentMap.GetMapUnitsOnTile(xy);
            TileReference tileReference = CurrentMap.GetTileReference(xy);

            bool bIsSearchableMapUnit = mapUnits.Any(m => m is Chest or DeadBody or BloodSpatter or DiscoverableLoot) ||
                                        tileReference.HasSearchReplacement;
            bool bIsMoonstoneBuried =
                GameStateReference.State.TheMoongates.IsMoonstoneBuried(xy, CurrentMap.TheMapType);

            return (CurrentMap is not LargeMap && bIsMoonstoneBuried) || bIsSearchableMapUnit;
        }

        /// <summary>
        ///     Creates a new frigate at a dock of a given location
        /// </summary>
        /// <param name="location"></param>
        // ReSharper disable once UnusedMethodReturnValue.Global
        public Frigate CreateFrigateAtDock(SmallMapReferences.SingleMapReference.Location location) =>
            TheMapHolder.OverworldMap.CreateFrigate(LargeMapLocationReferences.GetLocationOfDock(location),
                Point2D.Direction.Right, out _, 1);


        /// <summary>
        ///     Creates a new skiff and places it at a given location
        /// </summary>
        /// <param name="location"></param>
        /// <returns></returns>
        // ReSharper disable once UnusedMethodReturnValue.Global
        public Skiff CreateSkiffAtDock(SmallMapReferences.SingleMapReference.Location location) =>
            TheMapHolder.OverworldMap.CreateSkiff(LargeMapLocationReferences.GetLocationOfDock(location),
                Point2D.Direction.Right, out _);

        public Point2D GetCameraCenter() =>
            CurrentMap is CombatMap
                ? new Point2D(CurrentMap.NumOfXTiles / 2, CurrentMap.NumOfYTiles / 2)
                : CurrentMap.CurrentPosition.XY;


        /// <summary>
        ///     Gets the Avatar's current position in 3D spaces
        /// </summary>
        /// <returns></returns>
        public Point3D GetCurrent3DPosition() {
            if (CurrentMap is SmallMap smallMap)
                return new Point3D(CurrentMap.CurrentPosition.X, CurrentMap.CurrentPosition.Y, smallMap.MapFloor);

            return new Point3D(CurrentMap.CurrentPosition.X, CurrentMap.CurrentPosition.Y,
                CurrentMap.TheMapType == Map.Maps.Overworld ? 0 : 0xFF);
        }

        // ReSharper disable once MemberCanBePrivate.Global
        public SeaFaringVessel GetSeaFaringVesselAtDock(SmallMapReferences.SingleMapReference.Location location) {
            // 0 = Jhelom
            // 1 = Minoc
            // 2 = East Brittany
            // 3 = Buccaneer's Den

            var seaFaringVessel =
                TheMapHolder.OverworldMap.GetSpecificMapUnitByLocation<SeaFaringVessel>(
                    LargeMapLocationReferences.GetLocationOfDock(location), 0,
                    true);
            return seaFaringVessel;
        }

        public TileStack GetTileStack(Point2D xy, bool bSkipMapUnit) {
            var tileStack = new TileStack(xy);
            Map currentMap = CurrentMap;

            // this checks to see if you are on the outer bounds of a small map, and if the flood fill touched it
            // if it has touched it then we draw the outer tiles
            if (currentMap is SmallMap smallMap && !SmallMap.IsInBounds(xy) && smallMap.TouchedOuterBorder) {
                TileReference outerTileReference = GameReferences.Instance.SpriteTileReferences.GetTileReference(
                    smallMap.GetOutOfBoundsSprite(xy));
                tileStack.PushTileReference(outerTileReference);
                return tileStack;
            }

            // if the position of the tile is no longer inside the bounds of the visibility
            // or has become invisible, then destroy the voxels and return right away
            bool bOutsideOfVisibilityArray = !currentMap.IsInsideBounds(xy);
            if (bOutsideOfVisibilityArray || !currentMap.VisibleOnMap[xy.X][xy.Y]) {
                if (!bOutsideOfVisibilityArray)
                    tileStack.PushTileReference(GameReferences.Instance.SpriteTileReferences.GetTileReference(255));
                return tileStack;
            }

            // get the reference as per the original game data
            TileReference origTileReference = currentMap.GetTileReference(xy);
            // get topmost active map units on the tile
            MapUnit topMostMapUnit = bSkipMapUnit ? null : currentMap.GetTopVisibleMapUnit(xy, false);

            bool bIsAvatarTile = currentMap is not CombatMap && xy == currentMap.CurrentPosition?.XY;
            bool bIsMapUnitOccupiedTile = topMostMapUnit != null;

            // if there is an alternate flat sprite (for example, trees have grass)
            if (origTileReference.HasAlternateFlatSprite ||
                currentMap.IsXyOverride(xy, TileOverrideReference.TileType.Flat)) {
                TileReference flatTileReference =
                    GameReferences.Instance.SpriteTileReferences.GetTileReference(
                        currentMap.GetAlternateFlatSprite(xy));
                tileStack.PushTileReference(flatTileReference);
            }

            // we always push the original tile reference 
            int nCalculatedIndex = GetCalculatedSpriteIndexByTile(origTileReference, xy, bIsAvatarTile,
                bIsMapUnitOccupiedTile, topMostMapUnit, out bool bDrawCharacterOnTile);
            TileReference calculatedTileReference =
                GameReferences.Instance.SpriteTileReferences.GetAnimatedTileReference(nCalculatedIndex);

            // there are times we will not draw a calculated reference - such as when an NPC is on a door 
            // which indicates it is open, and therefor hidden
            bool bSkipCalculatedTileReference =
                (TileReferences.IsDoor(calculatedTileReference.Index) && bIsMapUnitOccupiedTile)
                || calculatedTileReference.DontDraw;

            if (!bSkipCalculatedTileReference) tileStack.PushTileReference(calculatedTileReference);

            // next we have a more complicated operation. We need to figure out which mapunits are on the map
            // and which ones we should show. There are often cases where we need to combine them such
            // as walking on top of a horse

            // if the GetCalculatedSpriteIndexByTile routine has told us not to draw the map unit,
            // then we skip it all together
            if (!bDrawCharacterOnTile) return tileStack;
            // if there are no map units, then we skip this bit all together
            if (topMostMapUnit == null) return tileStack;

            // we need to determine WHICH MapUnit to draw at this point
            // it could be the original, or boarded or even a different one all together?
            TileReference mapUnitTileReference = topMostMapUnit.GetAnimatedTileReference();

            Avatar avatarMapUnit = null;
            if (bIsAvatarTile)
                avatarMapUnit =
                    currentMap is RegularMap regularMap ? regularMap.GetAvatarMapUnit() : null;

            switch (topMostMapUnit) {
                case Horse when bIsAvatarTile: // if we are on a horse, let's show the mounted tile
                case Avatar { IsAvatarOnBoardedThing: true }: // we always show the Avatar on the thing he is boarded on
                    tileStack.PushTileReference(topMostMapUnit.GetBoardedTileReference(), true);
                    break;
                case MagicCarpet when bIsAvatarTile: {
                    tileStack.PushTileReference(mapUnitTileReference, true);
                    if (avatarMapUnit == null)
                        throw new Ultima5ReduxException(
                            "Avatar map unit was null, when expected on top of magic carpet");
                    tileStack.PushTileReference(avatarMapUnit.GetAnimatedTileReference(), true);
                    break;
                }
                default:
                    tileStack.PushTileReference(mapUnitTileReference, true);
                    break;
            }

            return tileStack;
        }

        /// <summary>
        ///     Determines if a specific Dock is occupied by a Sea Faring Vessel
        /// </summary>
        /// <returns></returns>
        public bool IsShipOccupyingDock(SmallMapReferences.SingleMapReference.Location location) =>
            GetSeaFaringVesselAtDock(location) != null;


        [SuppressMessage("ReSharper", "MemberCanBePrivate.Global")]
        public void LoadCombatMap(SingleCombatMapReference singleCombatMapReference,
            SingleCombatMapReference.EntryDirection entryDirection, PlayerCharacterRecords records,
            EnemyReference primaryEnemyReference = null, int nPrimaryEnemies = 0,
            EnemyReference secondaryEnemyReference = null, int nSecondaryEnemies = 0) {
            LoadCombatMap(singleCombatMapReference, entryDirection, records, primaryEnemyReference, nPrimaryEnemies,
                secondaryEnemyReference, nSecondaryEnemies, null);
        }

        /// <summary>
        ///     Specifically for loading combat maps when fighting NPCs instead of the typical Enemy
        /// </summary>
        /// <param name="singleCombatMapReference"></param>
        /// <param name="entryDirection"></param>
        /// <param name="records"></param>
        /// <param name="primaryEnemyReference"></param>
        /// <param name="npcRef"></param>
        [SuppressMessage("ReSharper", "MemberCanBePrivate.Global")]
        public void LoadCombatMap(SingleCombatMapReference singleCombatMapReference,
            SingleCombatMapReference.EntryDirection entryDirection, PlayerCharacterRecords records,
            EnemyReference primaryEnemyReference, NonPlayerCharacterReference npcRef) {
            LoadCombatMap(singleCombatMapReference, entryDirection, records, primaryEnemyReference, 1, null, 0, npcRef);
        }

        /// <summary>
        ///     Loads a combat map, but aut
        /// </summary>
        /// <param name="singleCombatMapReference"></param>
        /// <param name="entryDirection"></param>
        /// <param name="records"></param>
        /// <param name="enemyReference"></param>
        [SuppressMessage("ReSharper", "MemberCanBePrivate.Global")]
        public void LoadCombatMapWithCalculation(SingleCombatMapReference singleCombatMapReference,
            SingleCombatMapReference.EntryDirection entryDirection, PlayerCharacterRecords records,
            EnemyReference enemyReference) {
            const int nPrimaryEnemies = 1;
            int nSecondaryEnemies = 1;

            if (enemyReference.IsNpc) nSecondaryEnemies = 0;

            EnemyReference secondaryEnemyReference =
                GameReferences.Instance.EnemyRefs.GetFriendReference(enemyReference);

            LoadCombatMap(singleCombatMapReference, entryDirection, records, enemyReference, nPrimaryEnemies,
                secondaryEnemyReference, nSecondaryEnemies);
        }


        public void LoadDungeonMap(SingleDungeonMapFloorReference singleDungeonMapFloorReference,
            Point2D startingPosition) {
            // if you are somehow transported between two different small map locations, then the guards
            // forget about your transgressions
            ClearAllFlagsBeforeMapLoad();

            SavedMapRefs ??= new SavedMapRefs();
            SavedMapRefs.SetBySingleDungeonMapFloorReference(singleDungeonMapFloorReference, startingPosition);

            TheMapHolder.TheDungeonMap = new DungeonMap(singleDungeonMapFloorReference);

            if (startingPosition != null) CurrentMap.CurrentPosition.XY = startingPosition;
        }

        public void LoadCutOrIntroScene(SingleCutOrIntroSceneMapReference singleCutOrIntroSceneMapReference,
            Point2D startingPosition) {
            ClearAllFlagsBeforeMapLoad();

            SavedMapRefs ??= new SavedMapRefs();
            SavePreTeleportMapRefs();
            SavedMapRefs.SetBySingleCutOrIntroSceneMapReference(singleCutOrIntroSceneMapReference);

            if (singleCutOrIntroSceneMapReference.IsCutsceneMap) {
                if (singleCutOrIntroSceneMapReference.TheCutOrIntroSceneMapType == SingleCutOrIntroSceneMapReference.CutOrIntroSceneMapType.ShrineOfVirtueInterior) {
                    ShrineReference shrineReference =
                        GameReferences.Instance.ShrineReferences.GetShrineReferenceByPosition(
                            PreTeleportMapSavedMapRefs.MapUnitPosition.XY);

                    if (shrineReference == null) {
                        throw new Ultima5ReduxException(
                            $"Unexpected ShrineReference was null at position {SavedMapRefs.MapUnitPosition.XY}");
                    }

                    TheMapHolder.CutSceneMap = new CutSceneMap(singleCutOrIntroSceneMapReference, shrineReference);
                }
                else {
                    TheMapHolder.CutSceneMap = new CutSceneMap(singleCutOrIntroSceneMapReference);
                }
            }
            else if (singleCutOrIntroSceneMapReference.TheCutOrIntroSceneMapType == SingleCutOrIntroSceneMapReference.CutOrIntroSceneMapType.ShrineOfTheCodexInterior) {
                TheMapHolder.CutSceneMap = new CutSceneMap(singleCutOrIntroSceneMapReference);
            }
            else {
                TheMapHolder.IntroSceneMap = new IntroSceneMap(singleCutOrIntroSceneMapReference);
            }

            if (startingPosition != null) CurrentMap.CurrentPosition.XY = startingPosition;
        }

        /// <summary>
        ///     Loads a large map -either overworld or underworld
        /// </summary>
        /// <param name="largeMapType"></param>
        /// <param name="playerPosition"></param>
        [SuppressMessage("ReSharper", "MemberCanBePrivate.Global")]
        public void LoadLargeMap(LargeMapLocationReferences.LargeMapType largeMapType, Point2D playerPosition = null) {
            // if you are somehow transported between two different small map locations, then the guards
            // forget about your transgressions
            ClearAllFlagsBeforeMapLoad();

            MapUnit existingBoardedMapUnit = CurrentMap?.CurrentMapUnits?.TheAvatar?.CurrentBoardedMapUnit;
            SavedMapRefs ??= new SavedMapRefs();
            SavedMapRefs.SetByLargeMapType(largeMapType, playerPosition);

            // we need to set the actual map to the correct coordinates - up to this point we have only changed the map
            CurrentMap.CurrentPosition.Floor =
                largeMapType == LargeMapLocationReferences.LargeMapType.Overworld ? 0 : -1;
            if (playerPosition != null) CurrentMap.CurrentPosition.XY = playerPosition;
            if (existingBoardedMapUnit != null)
                CurrentMap.CurrentMapUnits.TheAvatar.BoardMapUnit(existingBoardedMapUnit);
        }

        /// <summary>
        ///     Used in initial legacy loads, as well as pretty much any other small map load
        /// </summary>
        /// <param name="singleMapReference"></param>
        /// <param name="xy"></param>
        /// <param name="bLoadFromDisk"></param>
        /// <param name="importedGameState"></param>
        /// <exception cref="Ultima5ReduxException"></exception>
        public void LoadSmallMap(SmallMapReferences.SingleMapReference singleMapReference, Point2D xy = null,
            bool bLoadFromDisk = false, ImportedGameState importedGameState = null) {
            // if you are somehow transported between two different small map locations, then the guards
            // forget about your transgressions
            ClearAllFlagsBeforeMapLoad();

            MapUnit existingBoardedMapUnit = CurrentMap?.CurrentMapUnits?.TheAvatar?.CurrentBoardedMapUnit;

            // setting this will make everything think we have a new map
            // CurrentMap will now point to the new map as well
            SavedMapRefs ??= new SavedMapRefs();
            SavedMapRefs.SetBySingleMapReference(singleMapReference, xy);

            // safety check
            if (CurrentMap is not SmallMap smallMap)
                throw new Ultima5ReduxException("CurrentMap did not switch to SmallMap on load");

            smallMap.InitializeFromLegacy(TheMapHolder.SmallMaps, singleMapReference.MapLocation,
                importedGameState, bLoadFromDisk, TheSearchItems);

            // smallMap.CurrentPosition.Floor = singleMapReference.Floor;
            smallMap.CurrentPosition = xy == null
                ? new MapUnitPosition(0, 0, singleMapReference.Floor)
                : new MapUnitPosition(xy.X, xy.Y, singleMapReference.Floor);

            if (existingBoardedMapUnit != null)
                smallMap.GetAvatarMapUnit().BoardMapUnit(existingBoardedMapUnit);

            smallMap.HandleSpecialCasesForSmallMapLoad();
        }


        public void ReturnToPreviousMapAfterCombat() {
            switch (PreTeleportMapSavedMapRefs.MapType) {
                case Map.Maps.Small:
                case Map.Maps.Overworld:
                case Map.Maps.Underworld:
                    SavedMapRefs = PreTeleportMapSavedMapRefs.Copy();
                    break;
                case Map.Maps.Dungeon:
                    break;
                case Map.Maps.Combat:
                default:
                    throw new Ultima5ReduxException(
                        "Attempting to return to previous map after combat with an unsupported map type: " +
                        PreTeleportMapSavedMapRefs.MapType);
            }
        }


        /// <summary>
        ///     Use the stairs and change floors, loading a new map
        /// </summary>
        /// <param name="xy">the position of the stairs, ladder or trapdoor</param>
        /// <param name="bForceDown">force a downward stairs</param>
        public void UseStairs(Point2D xy, bool bForceDown = false) {
            if (CurrentMap is not SmallMap smallMap)
                throw new Ultima5ReduxException("Cannot UseStairs unless you are in a small map");

            bool bStairGoUp = smallMap.IsStairGoingUp(out _) && !bForceDown;

            if (CurrentMap.CurrentSingleMapReference == null)
                throw new Ultima5ReduxException("No single map is set in virtual map");

            LoadSmallMap(GameReferences.Instance.SmallMapRef.GetSingleMapByLocation(
                CurrentMap.CurrentSingleMapReference.MapLocation,
                smallMap.MapFloor + (bStairGoUp ? 1 : -1)), xy.Copy());
        }
    }
}