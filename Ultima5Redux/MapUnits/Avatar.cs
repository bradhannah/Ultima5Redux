using System;
using System.Collections.Generic;
using Ultima5Redux.Maps;
using Ultima5Redux.MapUnits.SeaFaringVessels;
using Ultima5Redux.References;

namespace Ultima5Redux.MapUnits
{
    public sealed class Avatar : MapUnit
    {
        public enum AvatarState { Regular, Carpet, Horse, Frigate, Skiff, Hidden }

        private readonly bool _bUseExtendedSprites;

        /// <summary>
        ///     Describes if there are only left right sprites
        /// </summary>
        private readonly Dictionary<AvatarState, bool> _onlyLeftRight = new Dictionary<AvatarState, bool>
        {
            { AvatarState.Carpet, true },
            { AvatarState.Frigate, false },
            { AvatarState.Hidden, false },
            { AvatarState.Horse, true },
            { AvatarState.Skiff, false },
            { AvatarState.Regular, false }
        };

        private Avatar(SmallMapReferences.SingleMapReference.Location location,
            MapUnitMovement movement, MapUnitPosition mapUnitPosition, TileReference tileReference, bool bUseExtendedSprites)
        {
            _bUseExtendedSprites = bUseExtendedSprites;
        
            KeyTileReference = tileReference;
            MapUnitPosition = mapUnitPosition;
        
            BoardMapUnitFromAvatarState(CurrentAvatarState);
        
            MapLocation = location;
        
            Movement = movement;
        }

        internal AvatarState CurrentAvatarState { get; private set; }
        private Point2D.Direction PreviousDirection { get; set; } = Point2D.Direction.None;
        public override AvatarState BoardedAvatarState => AvatarState.Regular;

        public bool AreSailsHoisted => IsAvatarOnBoardedThing && CurrentBoardedMapUnit is Frigate frigate &&
                                       frigate.SailsHoisted;

        public override bool IsActive => true;

        public override bool IsAttackable => false;

        /// <summary>
        ///     Is the Avatar currently boarded onto a thing
        /// </summary>
        public bool IsAvatarOnBoardedThing =>
            CurrentAvatarState != AvatarState.Regular && CurrentAvatarState != AvatarState.Hidden;

        public Point2D.Direction CurrentDirection { get; private set; }

        /// <summary>
        ///     The current MapUnit (if any) that the Avatar is occupying. It is expected that it is NOT in the active
        ///     the current MapUnits object
        /// </summary>
        public MapUnit CurrentBoardedMapUnit { get; private set; }

        public override string BoardXitName => "You can't board the Avatar you silly goose!";

        public override string FriendlyName => "Avatar";

        public override TileReference KeyTileReference
        {
            get =>
                IsAvatarOnBoardedThing
                    ? GameReferences.SpriteTileReferences.GetTileReferenceByName(DirectionToTileNameBoarded[CurrentDirection])
                    : GameReferences.SpriteTileReferences.GetTileReferenceByName(DirectionToTileName[CurrentDirection]);
            set
            {
                CurrentAvatarState = CalculateAvatarState(value);
                base.KeyTileReference = value;
            }
        }

        protected override Dictionary<Point2D.Direction, string> DirectionToTileName { get; }
            = new Dictionary<Point2D.Direction, string>
            {
                { Point2D.Direction.None, "BasicAvatar" },
                { Point2D.Direction.Left, "BasicAvatar" },
                { Point2D.Direction.Down, "BasicAvatar" },
                { Point2D.Direction.Right, "BasicAvatar" },
                { Point2D.Direction.Up, "BasicAvatar" }
            };

        protected override Dictionary<Point2D.Direction, string> DirectionToTileNameBoarded => DirectionToTileName;

        private AvatarState CalculateAvatarState(TileReference tileReference)
        {
            if (tileReference.Name == "BasicAvatar") return AvatarState.Regular;
            if (tileReference.Name.StartsWith("Ship")) return AvatarState.Frigate;
            if (tileReference.Name.StartsWith("Skiff")) return AvatarState.Skiff;
            if (tileReference.Name.StartsWith("RidingMagicCarpet") || tileReference.Name.StartsWith("Carpet")) return AvatarState.Carpet;
            if (tileReference.Name.StartsWith("RidingHorse")) return AvatarState.Horse;
            if (tileReference.Name.StartsWith("Horse")) return AvatarState.Horse;
            throw new Ultima5ReduxException("Asked to calculate AvatarState of " + tileReference.Name +
                                            " but you can't do that, it's not a thing!");
        }

        private TileReference GetCurrentTileReference()
        {
            if (CurrentAvatarState == AvatarState.Regular || CurrentAvatarState == AvatarState.Hidden)
                return NonBoardedTileReference;

            return CurrentBoardedMapUnit.BoardedTileReference;
        }

        /// <summary>
        ///     Attempt to move the Avatar in a given direction
        ///     It takes into account if the Avatar has boarded a vehicle (horse, skiff etc)
        /// </summary>
        /// <param name="direction">the direction </param>
        /// <returns>true if Avatar moved, false if they only changed direction</returns>
        public bool Move(Point2D.Direction direction)
        {
            bool bChangeTile = true;
            // if there are only left and right sprites then we don't switch directions unless they actually
            // go left or right, otherwise we maintain direction - UNLESS we have forced extended sprites on
            // for the vehicle
            bool bUseFourDirections = CurrentBoardedMapUnit?.UseFourDirections ?? false;
            if (_onlyLeftRight[CurrentAvatarState] && !bUseFourDirections)
                if (direction != Point2D.Direction.Left && direction != Point2D.Direction.Right)
                    bChangeTile = false;

            // we only track changes in tile if we are changing the direction of the sprite, otherwise we don't track
            // it and don't care - this makes sure carpets and horses don't change direction when going up
            // and down
            if (bChangeTile)
            {
                PreviousDirection = CurrentDirection;
                CurrentDirection = direction;
            }

            if (CurrentBoardedMapUnit != null) CurrentBoardedMapUnit.Direction = CurrentDirection;

            // did the Avatar change direction?
            bool bDirectionChanged = PreviousDirection != CurrentDirection;

            // set the new sprite to reflect the new direction
            if (bChangeTile) KeyTileReference = GetCurrentTileReference();
                //TheMapUnitState.SetTileReference(GetCurrentTileReference());

            // return false if the direction changed AND your on a Frigate
            // because you will just change direction
            return !(bDirectionChanged && CurrentAvatarState == AvatarState.Frigate);
        }

        /// <summary>
        ///     Creates an Avatar MapUnit at the default small map position
        ///     Note: this should never need to be called from a LargeMap since the values persist on disk
        /// </summary>
        /// <param name="location"></param>
        /// <param name="movement"></param>
        /// <param name="mapUnitPosition"></param>
        /// <param name="bUseExtendedSprites"></param>
        /// <returns></returns>
        public static MapUnit CreateAvatar(SmallMapReferences.SingleMapReference.Location location, 
            MapUnitMovement movement, MapUnitPosition mapUnitPosition, TileReference tileReference, 
            bool bUseExtendedSprites)
        {
            Avatar theAvatar = new Avatar(location, movement, mapUnitPosition, tileReference, bUseExtendedSprites);
        
            return theAvatar;
        }

        /// <summary>
        ///     Show the Avatar that isn't boarded on top of anything
        /// </summary>
        internal MapUnit UnboardedAvatar()
        {
            KeyTileReference = NonBoardedTileReference;
            CurrentAvatarState = AvatarState.Regular;
            MapUnit previouslyBoardedMapUnit = CurrentBoardedMapUnit;
            CurrentBoardedMapUnit.IsOccupiedByAvatar = false;
            CurrentBoardedMapUnit = null;
            return previouslyBoardedMapUnit;
        }

        private void BoardMapUnitFromAvatarState(AvatarState avatarState)
        {
            //MapUnitState vehicleState = new MapUnitState();
            // we copy the Avatar map unit state as a starting point
            //TheMapUnitState.CopyTo(vehicleState);
            MapUnitMovement emptyMapUnitMovement = new MapUnitMovement(0, null, null);
            
            switch (avatarState)
            {
                case AvatarState.Regular:
                    break;
                case AvatarState.Carpet:
                    MagicCarpet carpet = new MagicCarpet(MapLocation, CurrentDirection, null, MapUnitPosition);
                    BoardMapUnit(carpet);
                    break;
                case AvatarState.Horse:
                    Horse horse = new Horse(emptyMapUnitMovement, MapLocation, CurrentDirection, null, MapUnitPosition);
                    BoardMapUnit(horse);
                    break;
                case AvatarState.Frigate:
                    // todo: this is incorrect - we need to figure out the correct number of skiffs when we board it and create it
                    Frigate frigate = new Frigate(emptyMapUnitMovement, MapLocation, CurrentDirection, null, MapUnitPosition);
                    //frigate
                    // must decide how many skiffs are there and assign them
                    frigate.SkiffsAboard = 1; 
                        //TheMapUnitState.Depends3;
                    BoardMapUnit(frigate);
                    break;
                case AvatarState.Skiff:
                    Skiff skiff = new Skiff(emptyMapUnitMovement, MapLocation, CurrentDirection, null, MapUnitPosition);
                    BoardMapUnit(skiff);
                    break;
                case AvatarState.Hidden:
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(avatarState), avatarState, null);
            }
        }

        public void BoardMapUnit(MapUnit mapUnit)
        {
            // note: since the Avatar does not control all MapUnits, we only add it our internal tracker
            // but do not release it from the world - that must be done outside this method
            KeyTileReference = mapUnit.KeyTileReference;
            CurrentAvatarState = mapUnit.BoardedAvatarState;
            CurrentBoardedMapUnit = mapUnit;
            CurrentBoardedMapUnit.IsOccupiedByAvatar = true;

            mapUnit.UseFourDirections = _bUseExtendedSprites;

            if (!(mapUnit is Frigate)) return;

            // if we are going onto a frigate, then we want to make sure the Avatar can start rowing
            // in the direction that it's already facing
            PreviousDirection = mapUnit.Direction;
            CurrentDirection = mapUnit.Direction;
        }
    }
}