using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.Serialization;
using Ultima5Redux.DayNightMoon;
using Ultima5Redux.External;
using Ultima5Redux.Maps;
using Ultima5Redux.MapUnits.NonPlayerCharacters;
using Ultima5Redux.References;

namespace Ultima5Redux.MapUnits
{
    public abstract class MapUnit : MapUnitDetails
    {
        /// <summary>
        ///     The characters current position on the map
        /// </summary>
        [DataMember]
        public sealed override MapUnitPosition MapUnitPosition
        {
            get => _mapMapUnitPosition;
            internal set
            {
                if (value == null) throw new ArgumentNullException(nameof(value));
                // this is a bit redundant but we have a backing field and also store the XY positions
                // in the TheSmallMapCharacterState, but we have to do this because the .XY
                // of the MapUnitPosition is often edited directly
                _mapMapUnitPosition.X = value.X;
                _mapMapUnitPosition.Y = value.Y;
                _mapMapUnitPosition.Floor = value.Floor;

                if (TheSmallMapCharacterState == null) return;
                TheSmallMapCharacterState.TheMapUnitPosition.X = value.X;
                TheSmallMapCharacterState.TheMapUnitPosition.Y = value.Y;
                TheSmallMapCharacterState.TheMapUnitPosition.Floor = value.Floor;
            }
        }

        [DataMember] private int _keyTileIndex = -1;
        [DataMember] private int _npcRefIndex = -1;

        [DataMember] public NonPlayerCharacterState NPCState { get; protected set; }


        [IgnoreDataMember] protected abstract Dictionary<Point2D.Direction, string> DirectionToTileName { get; }
        [IgnoreDataMember] protected abstract Dictionary<Point2D.Direction, string> DirectionToTileNameBoarded { get; }

        [IgnoreDataMember]
        protected virtual Dictionary<Point2D.Direction, string> FourDirectionToTileNameBoarded =>
            DirectionToTileNameBoarded;

        // ReSharper disable once MemberCanBeProtected.Global
        [IgnoreDataMember]
        public virtual TileReference NonBoardedTileReference =>
            GameReferences.SpriteTileReferences.GetTileReferenceByName(DirectionToTileName[Direction]);

        [IgnoreDataMember] private readonly MapUnitPosition _mapMapUnitPosition = new MapUnitPosition();

        [IgnoreDataMember]
        public TileReference BoardedTileReference =>
            GameReferences.SpriteTileReferences.GetTileReferenceByName(UseFourDirections
                ? FourDirectionToTileNameBoarded[Direction]
                : DirectionToTileNameBoarded[Direction]);

        [IgnoreDataMember]
        public virtual TileReference KeyTileReference
        {
            get => NPCRef == null
                ? GameReferences.SpriteTileReferences.GetTileReference(_keyTileIndex)
                : GameReferences.SpriteTileReferences.GetTileReference(NPCRef.NPCKeySprite);
            set => _keyTileIndex = value.Index;
        }

        public NonPlayerCharacterReference NPCRef => NPCState?.NPCRef;


        /// <summary>
        ///     empty constructor if there is nothing in the map character slot
        /// </summary>
        protected MapUnit()
        {
            //TheMapUnitState = null;
            TheSmallMapCharacterState = null;
            Movement = null;
            Direction = Point2D.Direction.None;
        }

        /// <summary>
        ///     Builds a MpaCharacter from pre-instantiated objects - typically loaded from disk in advance
        /// </summary>
        /// <param name="smallMapTheSmallMapCharacterState"></param>
        /// <param name="mapUnitMovement"></param>
        /// <param name="location"></param>
        /// <param name="direction"></param>
        /// <param name="npcState"></param>
        /// <param name="mapUnitPosition"></param>
        /// <param name="tileReference"></param>
        protected MapUnit(
            SmallMapCharacterState smallMapTheSmallMapCharacterState,
            MapUnitMovement mapUnitMovement, SmallMapReferences.SingleMapReference.Location location,
            Point2D.Direction direction,
            NonPlayerCharacterState npcState, TileReference tileReference,
            MapUnitPosition mapUnitPosition)
        {
            MapLocation = location;
            TheSmallMapCharacterState = smallMapTheSmallMapCharacterState;
            Movement = mapUnitMovement;
            Direction = direction;

            if (npcState != null) _npcRefIndex = npcState.NPCRef?.DialogIndex ?? -1;

            Debug.Assert(Movement != null);

            _keyTileIndex = tileReference.Index;

            // set the characters position 
            MapUnitPosition = mapUnitPosition;
        }

        /// <summary>
        ///     Builds the actual path for the character to travel based on their current position and their target position
        /// </summary>
        /// <param name="mapUnit">where the character is presently</param>
        /// <param name="targetXy">where you want them to go</param>
        /// <param name="aStar"></param>
        /// <returns>returns true if a path was found, false if it wasn't</returns>
        protected static bool BuildPath(MapUnit mapUnit, Point2D targetXy, AStar aStar)
        {
            if (mapUnit.MapUnitPosition.XY == targetXy)
                throw new Ultima5ReduxException("Asked to build a path, but " + mapUnit.FriendlyName +
                                                " is already at " + targetXy.X + "," +
                                                targetXy.Y);

            // todo: need some code that checks for different floors and directs them to closest ladder or staircase instead of same floor position

            Stack<Node> nodeStack = aStar.FindPath(mapUnit.MapUnitPosition.XY, targetXy);

            MapUnitMovement.MovementCommandDirection prevDirection = MapUnitMovement.MovementCommandDirection.None;
            MapUnitMovement.MovementCommandDirection newDirection = MapUnitMovement.MovementCommandDirection.None;
            Point2D prevPosition = mapUnit.MapUnitPosition.XY;

            // temporary while I figure out why this happens
            if (nodeStack == null) return false;

            int nInARow = 0;
            // builds the movement list that is compatible with the original U5 movement instruction queue stored in the state file
            foreach (Node node in nodeStack)
            {
                //Point2D newPosition = Vector2ToPoint2D(node.Position);
                newDirection = GetCommandDirection(prevPosition, node.Position);

                // if the previous direction is the same as the current direction, then we keep track so that we can issue a single instruction
                // that has N iterations (ie. move East 5 times)
                if (prevDirection == newDirection || prevDirection == MapUnitMovement.MovementCommandDirection.None)
                {
                    nInARow++;
                }
                else
                {
                    // if the direction has changed then we add the previous direction and reset the concurrent counter
                    mapUnit.Movement.AddNewMovementInstruction(
                        new MapUnitMovement.MovementCommand(prevDirection, nInARow));
                    nInARow = 1;
                }

                prevDirection = newDirection;
                prevPosition = node.Position;
            }

            if (nInARow > 0)
                mapUnit.Movement.AddNewMovementInstruction(new MapUnitMovement.MovementCommand(newDirection, nInARow));
            return true;
        }

        private static MapUnitMovement.MovementCommandDirection GetCommandDirection(Point2D fromXy, Point2D toXy)
        {
            if (fromXy == toXy) return MapUnitMovement.MovementCommandDirection.None;
            if (fromXy.X < toXy.X) return MapUnitMovement.MovementCommandDirection.East;
            if (fromXy.Y < toXy.Y) return MapUnitMovement.MovementCommandDirection.South;
            if (fromXy.X > toXy.X) return MapUnitMovement.MovementCommandDirection.West;
            if (fromXy.Y > toXy.Y) return MapUnitMovement.MovementCommandDirection.North;
            throw new Ultima5ReduxException(
                "For some reason we couldn't determine the path of the command direction in getCommandDirection");
        }

        public virtual bool CanBeExited(VirtualMap virtualMap)
        {
            return true;
        }

        public virtual void CompleteNextMove(VirtualMap virtualMap, TimeOfDay timeOfDay, AStar aStar)
        {
            // by default the thing doesn't move on it's own
        }

        // ReSharper disable once UnusedMember.Global
        public virtual string GetDebugDescription(TimeOfDay timeOfDay)
        {
            return "MapUnit " + KeyTileReference.Description
                              + " " + MapUnitPosition + " Scheduled to be at: "
                              + " <b>Movement Attempts</b>: " + MovementAttempts + " "
                              + Movement;
        }

        /// <summary>
        ///     move the character to a new position
        /// </summary>
        /// <param name="xy"></param>
        /// <param name="nFloor"></param>
        protected void Move(Point2D xy, int nFloor)
        {
            MapUnitPosition.XY = xy;
            MapUnitPosition.Floor = nFloor;
        }

        /// <summary>
        ///     Move the character to a new position
        /// </summary>
        /// <param name="mapUnitPosition"></param>
        protected void Move(MapUnitPosition mapUnitPosition)
        {
            MapUnitPosition = mapUnitPosition;
        }
    }
}