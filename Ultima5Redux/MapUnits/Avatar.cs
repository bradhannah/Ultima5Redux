using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Ultima5Redux.DayNightMoon;
using Ultima5Redux.Maps;
using Ultima5Redux.MapUnits.SeaFaringVessels;
using Ultima5Redux.References;
using Ultima5Redux.References.Maps;

namespace Ultima5Redux.MapUnits
{
    [DataContract] public sealed class Avatar : MapUnit
    {
        [JsonConverter(typeof(StringEnumConverter))]
        public enum AvatarState { Regular, Carpet, Horse, Frigate, Skiff, Hidden }

        [DataMember] internal AvatarState CurrentAvatarState { get; private set; }

        [DataMember(Name = "UseExtendedSprites")]
        private readonly bool _bUseExtendedSprites;

        [DataMember] private Point2D.Direction PreviousDirection { get; set; } = Point2D.Direction.None;

        [DataMember] public override AvatarState BoardedAvatarState => AvatarState.Regular;

        [DataMember]
        public override Point2D.Direction Direction
        {
            get => _currentDirection;
            set
            {
                _currentDirection = value;
                if (!IsAvatarOnBoardedThing) return;

                // when we load from disk, there is possible missing map unit
                if (CurrentBoardedMapUnit == null)
                {
                    BoardMapUnitFromAvatarState(CurrentAvatarState);
                }

                CurrentBoardedMapUnit.Direction = value;
            }
        }

        /// <summary>
        ///     Describes if there are only left right sprites
        /// </summary>
        [IgnoreDataMember] private readonly Dictionary<AvatarState, bool> _onlyLeftRight = new()
        {
            { AvatarState.Carpet, false },
            { AvatarState.Frigate, false },
            { AvatarState.Hidden, false },
            { AvatarState.Horse, false },
            { AvatarState.Skiff, false },
            { AvatarState.Regular, false }
        };

        [IgnoreDataMember] public override string BoardXitName => "You can't board the Avatar you silly goose!";

        [IgnoreDataMember] public override string FriendlyName => "Avatar";

        [IgnoreDataMember] public override bool IsActive => true;

        [IgnoreDataMember] public override bool IsAttackable => false;

        [IgnoreDataMember]
        public bool AreSailsHoisted =>
            IsAvatarOnBoardedThing && CurrentBoardedMapUnit is Frigate { SailsHoisted: true };

        /// <summary>
        ///     Is the Avatar currently boarded onto a thing
        /// </summary>
        [IgnoreDataMember]
        public bool IsAvatarOnBoardedThing =>
            CurrentAvatarState != AvatarState.Regular && CurrentAvatarState != AvatarState.Hidden;

        [IgnoreDataMember]
        public override TileReference KeyTileReference
        {
            get =>
                IsAvatarOnBoardedThing
                    ? GameReferences.Instance.SpriteTileReferences.GetTileReferenceByName(
                        DirectionToTileNameBoarded[Direction])
                    : GameReferences.Instance.SpriteTileReferences.GetTileReferenceByName(
                        DirectionToTileName[Direction]);
            set
            {
                CurrentAvatarState = CalculateAvatarState(value);
                base.KeyTileReference = value;
            }
        }

        /// <summary>
        ///     The current MapUnit (if any) that the Avatar is occupying. It is expected that it is NOT in the active
        ///     the current MapUnits object
        /// </summary>
        [IgnoreDataMember]
        public MapUnit CurrentBoardedMapUnit { get; private set; }

        [IgnoreDataMember]
        protected internal override Dictionary<Point2D.Direction, string> DirectionToTileNameBoarded
        {
            get
            {
                if (!IsAvatarOnBoardedThing) return DirectionToTileNameBasicAvatar;
                return UseFourDirections
                    ? CurrentBoardedMapUnit.FourDirectionToTileNameBoarded
                    : CurrentBoardedMapUnit.DirectionToTileNameBoarded;
            }
        }

        [IgnoreDataMember]
        protected override Dictionary<Point2D.Direction, string> DirectionToTileName =>
            DirectionToTileNameBasicAvatar;

        private Point2D.Direction _currentDirection;

        private Dictionary<Point2D.Direction, string> DirectionToTileNameBasicAvatar { get; } =
            new()
            {
                { Point2D.Direction.None, "Avatar1" },
                { Point2D.Direction.Left, "Avatar1" },
                { Point2D.Direction.Down, "Avatar1" },
                { Point2D.Direction.Right, "Avatar1" },
                { Point2D.Direction.Up, "Avatar1" }
            };

        public override bool UseFourDirections
        {
            get => !_onlyLeftRight[CurrentAvatarState];
            set
            {
                // generally ignored for now - don't love this 
            }
        }

        [JsonConstructor] private Avatar()
        {
        }

        private Avatar(SmallMapReferences.SingleMapReference.Location location, MapUnitMovement movement,
            MapUnitPosition mapUnitPosition, TileReference tileReference, bool bUseExtendedSprites)
        {
            _bUseExtendedSprites = bUseExtendedSprites;

            KeyTileReference = tileReference;
            MapUnitPosition = mapUnitPosition;

            BoardMapUnitFromAvatarState(CurrentAvatarState);

            MapLocation = location;

            Movement = movement;
        }

        [OnDeserialized] private void PostDeserialize(StreamingContext context)
        {
            BoardMapUnitFromAvatarState(CurrentAvatarState);
        }

        internal override void CompleteNextNonCombatMove(RegularMap regularMap, TimeOfDay timeOfDay)
        {
            // by default the thing doesn't move on it's own
        }

        /// <summary>
        ///     Show the Avatar that isn't boarded on top of anything
        /// </summary>
        internal MapUnit UnboardedAvatar()
        {
            KeyTileReference = GetNonBoardedTileReference();
            CurrentAvatarState = AvatarState.Regular;
            MapUnit previouslyBoardedMapUnit = CurrentBoardedMapUnit;
            CurrentBoardedMapUnit.IsOccupiedByAvatar = false;
            CurrentBoardedMapUnit = null;
            return previouslyBoardedMapUnit;
        }

        private static AvatarState CalculateAvatarState(TileReference tileReference)
        {
            if (tileReference.Name is "BasicAvatar" or "Avatar1") return AvatarState.Regular;
            if (tileReference.Name.StartsWith("Ship")) return AvatarState.Frigate;
            if (tileReference.Name.StartsWith("Skiff")) return AvatarState.Skiff;
            if (tileReference.Name.StartsWith("RidingMagicCarpet") || tileReference.Name.StartsWith("Carpet"))
                return AvatarState.Carpet;
            if (tileReference.Name.StartsWith("RidingHorse")) return AvatarState.Horse;
            if (tileReference.Name.StartsWith("Horse")) return AvatarState.Horse;
            throw new Ultima5ReduxException(
                $"Asked to calculate AvatarState of {tileReference.Name} but you can't do that, it's not a thing!");
        }

        private void BoardMapUnitFromAvatarState(AvatarState avatarState)
        {
            // we copy the Avatar map unit state as a starting point
            MapUnitMovement emptyMapUnitMovement = new(0, null, null);

            switch (avatarState)
            {
                case AvatarState.Regular:
                    break;
                case AvatarState.Carpet:
                    MagicCarpet carpet = new(MapLocation, Direction, null, MapUnitPosition);
                    BoardMapUnit(carpet);
                    break;
                case AvatarState.Horse:
                    Horse horse = new(emptyMapUnitMovement, MapLocation, Direction, null, MapUnitPosition);
                    BoardMapUnit(horse);
                    break;
                case AvatarState.Frigate:
                    // bajh: this is incorrect - we need to figure out the correct number of skiffs when we board it and create it
                    Frigate frigate = new(emptyMapUnitMovement, MapLocation, Direction, null,
                        MapUnitPosition)
                    {
                        // must decide how many skiffs are there and assign them
                        //frigate
                        SkiffsAboard = 1
                    };
                    BoardMapUnit(frigate);
                    break;
                case AvatarState.Skiff:
                    Skiff skiff = new(emptyMapUnitMovement, MapLocation, Direction, null, MapUnitPosition);
                    BoardMapUnit(skiff);
                    break;
                case AvatarState.Hidden:
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(avatarState), avatarState, null);
            }
        }

        private TileReference GetCurrentTileReference() =>
            CurrentAvatarState is AvatarState.Regular or AvatarState.Hidden
                ? GetNonBoardedTileReference()
                : CurrentBoardedMapUnit.GetBoardedTileReference();

        /// <summary>
        ///     Creates an Avatar MapUnit at the default small map position
        ///     Note: this should never need to be called from a LargeMap since the values persist on disk
        /// </summary>
        /// <param name="location"></param>
        /// <param name="movement"></param>
        /// <param name="mapUnitPosition"></param>
        /// <param name="tileReference"></param>
        /// <param name="bUseExtendedSprites"></param>
        /// <returns></returns>
        public static Avatar CreateAvatar(SmallMapReferences.SingleMapReference.Location location,
            MapUnitMovement movement, MapUnitPosition mapUnitPosition, TileReference tileReference,
            bool bUseExtendedSprites)
        {
            Avatar theAvatar = new(location, movement, mapUnitPosition, tileReference, bUseExtendedSprites);

            return theAvatar;
        }

        public void BoardMapUnit(MapUnit mapUnit)
        {
            if (mapUnit == null)
                throw new Ultima5ReduxException("Tried to Board a null mapunit");
            // note: since the Avatar does not control all MapUnits, we only add it our internal tracker
            // but do not release it from the world - that must be done outside this method
            KeyTileReference = mapUnit.KeyTileReference;
            CurrentAvatarState = mapUnit.BoardedAvatarState;
            CurrentBoardedMapUnit = mapUnit;
            CurrentBoardedMapUnit.IsOccupiedByAvatar = true;

            mapUnit.UseFourDirections = _bUseExtendedSprites;

            if (mapUnit is not Frigate) return;

            // if we are going onto a frigate, then we want to make sure the Avatar can start rowing
            // in the direction that it's already facing
            PreviousDirection = mapUnit.Direction;
            Direction = mapUnit.Direction;
        }

        /// <summary>
        ///     Attempt to move the Avatar in a given direction
        ///     It takes into account if the Avatar has boarded a vehicle (horse, skiff etc)
        /// </summary>
        /// <param name="direction">the direction </param>
        /// <returns>true if Avatar moved, false if they only changed direction</returns>
        public bool Move(Point2D.Direction direction)
        {
            bool bChangeTile = UseFourDirections ||
                               direction is Point2D.Direction.Left or Point2D.Direction.Right;
            // if there are only left and right sprites then we don't switch directions unless they actually
            // go left or right, otherwise we maintain direction - UNLESS we have forced extended sprites on
            // for the vehicle

            // we only track changes in tile if we are changing the direction of the sprite, otherwise we don't track
            // it and don't care - this makes sure carpets and horses don't change direction when going up
            // and down
            if (bChangeTile)
            {
                PreviousDirection = Direction;
                Direction = direction;
            }

            if (CurrentBoardedMapUnit != null) CurrentBoardedMapUnit.Direction = Direction;

            // did the Avatar change direction?
            bool bDirectionChanged = PreviousDirection != Direction;

            // set the new sprite to reflect the new direction
            if (bChangeTile) KeyTileReference = GetCurrentTileReference();

            // return false if the direction changed AND your on a Frigate
            // because you will just change direction
            return !(bDirectionChanged && CurrentAvatarState == AvatarState.Frigate);
        }
    }
}