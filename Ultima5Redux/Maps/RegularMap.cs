using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.Serialization;
using Newtonsoft.Json;
using Ultima5Redux.MapUnits;
using Ultima5Redux.MapUnits.CombatMapUnits;
using Ultima5Redux.MapUnits.NonPlayerCharacters;
using Ultima5Redux.MapUnits.SeaFaringVessels;
using Ultima5Redux.MapUnits.TurnResults;
using Ultima5Redux.MapUnits.TurnResults.SpecificTurnResults;
using Ultima5Redux.PlayerCharacters.Inventory;
using Ultima5Redux.References;
using Ultima5Redux.References.Maps;
using Ultima5Redux.References.MapUnits.NonPlayerCharacters;

namespace Ultima5Redux.Maps
{
    public abstract class RegularMap : Map
    {
        //[DataMember] public SmallMapReferences.SingleMapReference.Location MapLocation { get; private set; }

        

        [IgnoreDataMember]
        protected sealed override Dictionary<Point2D, TileOverrideReference> XYOverrides =>
            _xyOverrides ??= GameReferences.Instance.TileOverrideRefs.GetTileXYOverrides(CurrentSingleMapReference);

        private Dictionary<Point2D, TileOverrideReference> _xyOverrides;

        public void MoveAvatar(Point2D newPosition)
        {
            CurrentAvatarPosition =
                new MapUnitPosition(newPosition.X, newPosition.Y, CurrentAvatarPosition.Floor);
        }

        /// <summary>
        ///     Safe method to board a MapUnit and removing it from the world
        /// </summary>
        /// <param name="mapUnit"></param>
        internal void BoardAndCleanFromWorld(MapUnit mapUnit)
        {
            // board the unit
            GetAvatarMapUnit().BoardMapUnit(mapUnit);
            // clean it from the world so it no longer appears
            ClearAndSetEmptyMapUnits(mapUnit);
        }

        /// <summary>
        ///     Create a list of the free spaces surrounding around the Avatar suitable for something to be generated onto
        ///     Uses all 8 directions
        /// </summary>
        /// <returns></returns>
        private List<Point2D> GetFreeSpacesSurroundingAvatar()
        {
            List<Point2D> freeSpacesAroundAvatar = new();

            for (int x = -1; x <= 1; x++)
            {
                for (int y = -1; y <= 1; y++)
                {
                    Point2D pointToCheck = new(Math.Max(CurrentPosition.X + x, 0),
                        Math.Max(CurrentPosition.Y + y, 0));
                    if (!IsMapUnitOccupiedTile(pointToCheck) && GetTileReference(pointToCheck).IsWalking_Passable)
                        freeSpacesAroundAvatar.Add(pointToCheck);
                }
            }

            return freeSpacesAroundAvatar;
        }

        [IgnoreDataMember]
        public override MapUnitPosition CurrentPosition
        {
            get => CurrentAvatarPosition;
            set => CurrentAvatarPosition = value;
        }


        /// <summary>
        ///     Gets a map unit if it's on the current tile
        /// </summary>
        /// <returns>true if there is a map unit of on the tile</returns>
        public bool IsMapUnitOccupiedTile() => IsMapUnitOccupiedTile(CurrentPosition.XY);

        internal bool IsTileFreeToTravelForAvatar(in Point2D xy, bool bNoStaircases = false) =>
            IsTileFreeToTravel(xy, bNoStaircases, GetAvatarMapUnit().CurrentAvatarState);

        public bool IsLandNearbyForAvatar() =>
            IsLandNearby(CurrentPosition.XY, false, GetAvatarMapUnit().CurrentAvatarState);

        /// <summary>
        ///     Creates a horse MapUnit in the surrounding tiles of the Avatar - if one exists
        /// </summary>
        /// <returns>the new horse or null if there was no where to put it</returns>
        public Horse CreateHorseAroundAvatar(TurnResults turnResults)
        {
            List<Point2D> freeSpacesAroundAvatar = GetFreeSpacesSurroundingAvatar();
            if (freeSpacesAroundAvatar.Count <= 0) return null;

            Random ran = new();
            Point2D chosenLocation = freeSpacesAroundAvatar[ran.Next() % freeSpacesAroundAvatar.Count];
            Horse horse = CreateHorse(
                new MapUnitPosition(chosenLocation.X, chosenLocation.Y, CurrentPosition.Floor), TheMapType,
                out int nIndex);

            if (nIndex == -1 || horse == null) return null;

            turnResults.PushTurnResult(new BasicResult(TurnResult.TurnResultType.PoofHorse));

            return horse;
        }

        public Avatar GetAvatarMapUnit()
        {
            if (CurrentMapUnits == null)
                throw new Ultima5ReduxException("Tried to get Avatar but CurrentMapUnits is null");
            if (CurrentMapUnits.TheAvatar == null)
                throw new Ultima5ReduxException("Tried to get Avatar but CurrentMapUnits.TheAvatar is null");

            return CurrentMapUnits.TheAvatar;
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
        protected MapUnit CreateNewMapUnit(MapUnitMovement mapUnitMovement, bool bInitialLoad,
            SmallMapReferences.SingleMapReference.Location location, NonPlayerCharacterState npcState,
            MapUnitPosition mapUnitPosition, TileReference tileReference,
            SmallMapCharacterState smallMapCharacterState = null)
        {
            MapUnit newUnit;

            if (tileReference == null || tileReference.Index == 256)
            {
                Debug.WriteLine("An empty map unit was created with no tile reference");
                newUnit = new EmptyMapUnit();
            }
            else if (smallMapCharacterState != null && npcState != null && smallMapCharacterState.Active &&
                     npcState.NPCRef.NormalNPC)
            {
                newUnit = new NonPlayerCharacter(smallMapCharacterState, mapUnitMovement, bInitialLoad, location,
                    mapUnitPosition, npcState);
                // special condition where people who were previously freed are not dead, but also do not appear 
                if (newUnit.OverrideAiType && !GameStateReference.State.TheGameOverrides.PreferenceFreedPeopleDontDie)
                {
                    NonPlayerCharacterSchedule.AiType aiType =
                        newUnit.GetCurrentAiType(GameStateReference.State.TheTimeOfDay);
                    if (aiType == NonPlayerCharacterSchedule.AiType.FollowAroundAndBeAnnoyingThenNeverSeeAgain)
                        newUnit = new EmptyMapUnit();
                }
            }
            else if (GameReferences.Instance.SpriteTileReferences.IsFrigate(tileReference.Index))
            {
                newUnit = new Frigate(mapUnitMovement, location, tileReference.GetDirection(), npcState,
                    mapUnitPosition);
            }
            else if (GameReferences.Instance.SpriteTileReferences.IsSkiff(tileReference.Index))
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
                    or TileReference.SpriteIndex.Rat_KeyIndex
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
            else if (GameReferences.Instance.SpriteTileReferences.IsMonster(tileReference.Index))
            {
                Debug.Assert(GameReferences.Instance.EnemyRefs != null);
                newUnit = new Enemy(mapUnitMovement, GameReferences.Instance.EnemyRefs.GetEnemyReference(tileReference),
                    location, npcState, mapUnitPosition);
            }
            // this is where we will create monsters too
            else
            {
                Debug.WriteLine("An empty map unit was created with " + tileReference.Name);
                newUnit = new EmptyMapUnit();
            }

            // force to use extended sprites if they exist
            newUnit.UseFourDirections = UseExtendedSprites;

            return newUnit;
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
        ///     Gets a tile reference from the tile the avatar currently resides on
        /// </summary>
        /// <returns></returns>
        public TileReference GetTileReferenceOnCurrentTile() => GetTileReference(CurrentPosition.XY);

        /// <summary>
        ///     Makes the Avatar exit the current MapUnit they are occupying
        /// </summary>
        /// <returns>The MapUnit object they were occupying - you need to re-add it the map after</returns>
        public MapUnit XitCurrentMapUnit(out string retStr)
        {
            retStr = GameReferences.Instance.DataOvlRef.StringReferences
                .GetString(DataOvlReference.KeypressCommandsStrings.XIT)
                .TrimEnd();

            if (!GetAvatarMapUnit().IsAvatarOnBoardedThing)
            {
                retStr += " " + GameReferences.Instance.DataOvlRef.StringReferences
                    .GetString(DataOvlReference.KeypressCommandsStrings.WHAT_Q).Trim();
                return null;
            }

            if (!GetAvatarMapUnit().CurrentBoardedMapUnit.CanBeExited(this))
            {
                retStr += "\n" + GameReferences.Instance.DataOvlRef.StringReferences
                    .GetString(DataOvlReference.SleepTransportStrings.N_NO_LAND_NEARBY_BANG_N).Trim();
                return null;
            }

            MapUnit unboardedMapUnit = GetAvatarMapUnit().UnboardedAvatar();
            Debug.Assert(unboardedMapUnit != null);

            // set the current positions to the equal the Avatar's as he exits the vehicle 
            unboardedMapUnit.MapLocation = MapLocation;
            unboardedMapUnit.MapUnitPosition = CurrentAvatarPosition;
            unboardedMapUnit.Direction = GetAvatarMapUnit().Direction;
            unboardedMapUnit.KeyTileReference = unboardedMapUnit.GetNonBoardedTileReference();

            AddNewMapUnit(TheMapType, unboardedMapUnit);
            retStr += " " + unboardedMapUnit.BoardXitName;

            // if the Avatar is on a frigate then we will check for Skiffs and exit on a skiff instead
            if (unboardedMapUnit is not Frigate avatarFrigate) return unboardedMapUnit;

            Debug.Assert(avatarFrigate != null, nameof(avatarFrigate) + " != null");

            // if we have skiffs, AND do not have land close by then we deploy a skiff
            if (avatarFrigate.SkiffsAboard <= 0 || IsLandNearby(GetAvatarMapUnit().CurrentAvatarState))
                return unboardedMapUnit;

            MakeAndBoardSkiff();
            avatarFrigate.SkiffsAboard--;

            return unboardedMapUnit;
        }

        /// <summary>
        ///     Gets a suitable random position when wandering
        /// </summary>
        /// <param name="characterPosition">position of character</param>
        /// <param name="scheduledPosition">scheduled position of the character</param>
        /// <param name="nMaxDistance">max number of tiles the wander can be from the scheduled position</param>
        /// <param name="direction">OUT - the direction that the character should travel</param>
        /// <returns></returns>
        internal Point2D GetWanderCharacterPosition(Point2D characterPosition, Point2D scheduledPosition,
            int nMaxDistance, out MapUnitMovement.MovementCommandDirection direction)
        {
            Random ran = new();
            List<MapUnitMovement.MovementCommandDirection> possibleDirections =
                GetPossibleDirectionsList(characterPosition, scheduledPosition, nMaxDistance, true);

            // if no directions are returned then we tell them not to move
            if (possibleDirections.Count == 0)
            {
                direction = MapUnitMovement.MovementCommandDirection.None;

                return characterPosition.Copy();
            }

            direction = possibleDirections[ran.Next() % possibleDirections.Count];

            Point2D adjustedPosition = MapUnitMovement.GetAdjustedPos(characterPosition, direction);

            return adjustedPosition;
        }

        /// <summary>
        ///     Gets possible directions that are accessible from a particular point
        /// </summary>
        /// <param name="characterPosition">the current position of the character</param>
        /// <param name="scheduledPosition">the place they are supposed to be</param>
        /// <param name="nMaxDistance">max distance they can travel from that position</param>
        /// <param name="bNoStaircases"></param>
        /// <returns></returns>
        private List<MapUnitMovement.MovementCommandDirection> GetPossibleDirectionsList(in Point2D characterPosition,
            in Point2D scheduledPosition, int nMaxDistance, bool bNoStaircases)
        {
            List<MapUnitMovement.MovementCommandDirection> directionList = new();

            // gets an adjusted position OR returns null if the position is not valid

            foreach (MapUnitMovement.MovementCommandDirection direction in Enum.GetValues(
                         typeof(MapUnitMovement.MovementCommandDirection)))
            {
                // we may be asked to avoid including .None in the list
                if (direction == MapUnitMovement.MovementCommandDirection.None) continue;

                Point2D adjustedPos = GetPositionIfUserCanMove(direction, characterPosition, bNoStaircases,
                    scheduledPosition, nMaxDistance);
                // if adjustedPos == null then the particular direction was not allowed for one reason or another
                if (adjustedPos != null) directionList.Add(direction);
            }

            return directionList;
        }

        private Point2D GetPositionIfUserCanMove(MapUnitMovement.MovementCommandDirection direction,
            Point2D characterPosition, bool bNoStaircases, Point2D scheduledPosition, int nMaxDistance)
        {
            Point2D adjustedPosition = MapUnitMovement.GetAdjustedPos(characterPosition, direction);

            // always include none
            if (direction == MapUnitMovement.MovementCommandDirection.None) return adjustedPosition;

            if (adjustedPosition.X < 0 || adjustedPosition.X >= TheMap.Length || adjustedPosition.Y < 0 ||
                adjustedPosition.Y >= TheMap[0].Length) return null;

            // is the tile free to travel to? even if it is, is it within N tiles of the scheduled tile?
            if (IsTileFreeToTravelForAvatar(adjustedPosition, bNoStaircases) &&
                scheduledPosition.IsWithinN(adjustedPosition, nMaxDistance)) return adjustedPosition;

            return null;
        }

        public MoonstoneNonAttackingUnit CreateMoonstoneNonAttackingUnit(Point2D xy, Moonstone moonstone,
            SmallMapReferences.SingleMapReference singleMapReference)
        {
            int nIndex = FindNextFreeMapUnitIndex(TheMapType);
            if (nIndex == -1) return null;

            MapUnitPosition mapUnitPosition = new(xy.X, xy.Y, singleMapReference.Floor);
            var moonstoneNonAttackingUnit =
                new MoonstoneNonAttackingUnit(moonstone, mapUnitPosition);

            // set position of frigate in the world
            CurrentMapUnits.AddMapUnit(moonstoneNonAttackingUnit);
            return moonstoneNonAttackingUnit;
        }

        public Horse CreateHorse(MapUnitPosition mapUnitPosition, Maps map, out int nIndex)
        {
            nIndex = FindNextFreeMapUnitIndex(TheMapType);
            if (nIndex == -1) return null;

            Horse horse = new(
                new MapUnitMovement(nIndex),
                //importedMovements.GetMovement(nIndex), 
                MapLocation, Point2D.Direction.Right,
                null, mapUnitPosition)
            {
                MapUnitPosition = mapUnitPosition
            };

            // set position of frigate in the world
            AddNewMapUnit(map, horse, nIndex);
            return horse;
        }

        /// <summary>
        ///     Creates a skiff and places it on the map
        /// </summary>
        /// <param name="xy"></param>
        /// <param name="direction"></param>
        /// <param name="nIndex"></param>
        /// <returns></returns>
        internal Skiff CreateSkiff(Point2D xy, Point2D.Direction direction, out int nIndex)
        {
            nIndex = FindNextFreeMapUnitIndex(TheMapType);
            if (nIndex == -1) return null;

            Skiff skiff = new(
                new MapUnitMovement(nIndex),
                //importedMovements.GetMovement(nIndex),
                SmallMapReferences.SingleMapReference.Location.Britannia_Underworld, direction, null,
                new MapUnitPosition(xy.X, xy.Y, 0));

            AddNewMapUnit(Maps.Overworld, skiff, nIndex);
            return skiff;
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
            nIndex = FindNextFreeMapUnitIndex(TheMapType);

            if (nIndex == -1) return null;

            MagicCarpet magicCarpet = new(MapLocation, direction, null, new MapUnitPosition(xy.X, xy.Y, 0));

            AddNewMapUnit(TheMapType, magicCarpet, nIndex);
            return magicCarpet;
        }


        [IgnoreDataMember]
        public int TotalMapUnitsOnMap => CurrentMapUnits.AllMapUnits.Count(m => m is not EmptyMapUnit);

        //[IgnoreDataMember] protected Avatar MasterAvatarMapUnit { get; set; }
        //[IgnoreDataMember] protected readonly ImportedGameState importedGameState;

        //[IgnoreDataMember] protected readonly MapUnitMovements importedMovements;


        internal void DamageShip(Point2D.Direction windDirection, TurnResults turnResults)
        {
            Avatar avatar = GetAvatarMapUnit();

            int nDamage = Utils.Ran.Next(5, 15);

            Debug.Assert(avatar.CurrentBoardedMapUnit is Frigate);
            if (avatar.CurrentBoardedMapUnit is not Frigate frigate)
                throw new Ultima5ReduxException("Tried to get Avatar's frigate, but it returned  null");

            // if the wind is blowing the same direction then we double the damage
            if (avatar.Direction == windDirection) nDamage *= 2;
            // decrement the damage from the frigate
            frigate.Hitpoints -= nDamage;

            turnResults.PushOutputToConsole(
                GameReferences.Instance.DataOvlRef.StringReferences.GetString(DataOvlReference.WorldStrings
                    .BREAKING_UP), false);
            // if we hit zero hitpoints then the ship is destroyed and a skiff is boarded
            if (frigate.Hitpoints <= 0)
            {
                turnResults.PushTurnResult(new BasicResult(TurnResult.TurnResultType.ActionMoveShipDestroyed));
                // destroy the ship and leave board the Avatar onto a skiff
                turnResults.PushOutputToConsole(GameReferences.Instance.DataOvlRef.StringReferences.GetString(
                    DataOvlReference.WorldStrings2
                        .SHIP_SUNK_BANG_N), false);
                turnResults.PushOutputToConsole(GameReferences.Instance.DataOvlRef.StringReferences
                    .GetString(DataOvlReference.WorldStrings2.ABANDON_SHIP_BANG_N).TrimEnd(), false);

                MapUnit newFrigate =
                    XitCurrentMapUnit(out string _);
                ClearAndSetEmptyMapUnits(newFrigate);
                MakeAndBoardSkiff();
            }
            else
            {
                if (frigate.Hitpoints <= 10)
                    turnResults.PushOutputToConsole(GameReferences.Instance.DataOvlRef.StringReferences.GetString(
                        DataOvlReference.WorldStrings
                            .HULL_WEAK), false);

                turnResults.PushTurnResult(new BasicResult(TurnResult.TurnResultType.ActionMoveShipBreakingUp));
            }
        }

        /// <summary>
        ///     The single source of truth for the Avatar's current position within the current map
        /// </summary>
        [IgnoreDataMember]
        internal MapUnitPosition CurrentAvatarPosition
        {
            get => GetAvatarMapUnit().MapUnitPosition;
            set => GetAvatarMapUnit().MapUnitPosition = value;
        }

        [JsonConstructor] protected RegularMap()
        {
        }

        protected RegularMap(SmallMapReferences.SingleMapReference.Location location, int mapFloor) : base(location,
            mapFloor)
        {
            MapLocation = location;
            MapFloor = mapFloor;
        }
    }
}