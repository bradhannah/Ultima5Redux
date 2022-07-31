using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.Serialization;
using Newtonsoft.Json;
using Ultima5Redux.DayNightMoon;
using Ultima5Redux.External;
using Ultima5Redux.Maps;
using Ultima5Redux.MapUnits.NonPlayerCharacters;
using Ultima5Redux.References;
using Ultima5Redux.References.Maps;
using Ultima5Redux.References.MapUnits.NonPlayerCharacters;

namespace Ultima5Redux.MapUnits
{
    [DataContract] public abstract class MapUnit : MapUnitDetails
    {
        [DataMember(Name = "KeyTileIndex")] private int _keyTileIndex = -1;
        [DataMember(Name = "NpcRefIndex")] private int _npcRefIndex = -1;

        /// <summary>
        ///     The characters current position on the map
        /// </summary>
        [DataMember]
        public sealed override MapUnitPosition MapUnitPosition
        {
            get => _savedMapUnitPosition;
            internal set
            {
                if (value == null) throw new ArgumentNullException(nameof(value));
                _savedMapUnitPosition.X = value.X;
                _savedMapUnitPosition.Y = value.Y;
                _savedMapUnitPosition.Floor = value.Floor;

                if (TheSmallMapCharacterState == null) return;
                TheSmallMapCharacterState.TheMapUnitPosition.X = value.X;
                TheSmallMapCharacterState.TheMapUnitPosition.Y = value.Y;
                TheSmallMapCharacterState.TheMapUnitPosition.Floor = value.Floor;
            }
        }

        [DataMember] public NonPlayerCharacterState NPCState { get; protected set; }

        [IgnoreDataMember] private readonly MapUnitPosition _savedMapUnitPosition = new();

        // ReSharper disable once MemberCanBeProtected.Global
        [IgnoreDataMember]
        public virtual TileReference NonBoardedTileReference
        {
            get
            {
                if (DirectionToTileName == null) return KeyTileReference;
                if (!DirectionToTileName.ContainsKey(Direction))
                    throw new Ultima5ReduxException(
                        $"Tried to get NonBoardedTileReference with direction {Direction} on tile {this.KeyTileReference.Description}");
                return GameReferences.SpriteTileReferences.GetTileReferenceByName(DirectionToTileName[Direction]);
            }
        }
            

        [IgnoreDataMember]
        public TileReference BoardedTileReference
        {
            get
            {

                Dictionary<Point2D.Direction, string> tileNameDictionary =
                    (UseFourDirections ? FourDirectionToTileNameBoarded : DirectionToTileNameBoarded);
                if (tileNameDictionary == null) return KeyTileReference;
                return GameReferences.SpriteTileReferences.GetTileReferenceByName(tileNameDictionary[Direction]);
            }
        }

        [IgnoreDataMember] public NonPlayerCharacterReference NPCRef => NPCState?.NPCRef;

        [IgnoreDataMember]
        public virtual TileReference KeyTileReference
        {
            get => NPCRef == null
                ? GameReferences.SpriteTileReferences.GetTileReference(_keyTileIndex)
                : GameReferences.SpriteTileReferences.GetTileReference(NPCRef.NPCKeySprite);
            set => _keyTileIndex = value.Index;
        }

        [IgnoreDataMember]
        protected internal abstract Dictionary<Point2D.Direction, string> DirectionToTileName { get; }

        [IgnoreDataMember] protected internal abstract Dictionary<Point2D.Direction, string> DirectionToTileNameBoarded { get; }

        [IgnoreDataMember]
        protected internal virtual Dictionary<Point2D.Direction, string> FourDirectionToTileNameBoarded =>
            DirectionToTileNameBoarded;

        public int CurrentAnimationIndex { get; set; } = 0;
        public double TimeOfLastUpdate { get; set; }
        public double TimeBetweenAnimation { get; set; }
        
        public void NewFrameUpdate(double currentTime, double minAnimationTime,
            double maxAnimationTime, bool bNonRandomTime = false)
        {
            TimeBetweenAnimation =
                bNonRandomTime ? minAnimationTime : GetRandomNumber(minAnimationTime, maxAnimationTime);

            TimeOfLastUpdate = currentTime; //Time.time;
        }
        
        private static double GetRandomNumber(double minimum, double maximum)
        {
            return Utils.Ran.NextDouble() * (maximum - minimum) + minimum;
        }
        
        /// <summary>
        ///     empty constructor if there is nothing in the map character slot
        /// </summary>
        [JsonConstructor] protected MapUnit()
        {
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
        protected MapUnit(SmallMapCharacterState smallMapTheSmallMapCharacterState, MapUnitMovement mapUnitMovement,
            SmallMapReferences.SingleMapReference.Location location, Point2D.Direction direction,
            NonPlayerCharacterState npcState, TileReference tileReference, MapUnitPosition mapUnitPosition)
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

        /// <summary>
        ///     Gets the best next position that a map unit should dumbly move to to get to a particular point
        ///     Note: this is currently a dumb algorithm, just making sure they don't go through other units
        ///     or walls etc.
        ///     In the future this could be expand to use aStar, but some extra optimization work will need to be done
        /// </summary>
        /// <param name="virtualMap"></param>
        /// <param name="fromPosition"></param>
        /// <param name="toPosition">the position they are trying to get to</param>
        /// <param name="aStar"></param>
        /// <returns></returns>
        private Point2D GetBestNextPositionToMoveTowardsWalkablePointDumb(VirtualMap virtualMap, Point2D fromPosition,
            Point2D toPosition, AStar aStar)
        {
            double fShortestPath = 999f;
            Point2D bestMovePoint = null;

            // you want the valid wander points from the current position
            List<Point2D> wanderPoints = GetValidWanderPointsDumb(virtualMap, fromPosition);

            foreach (Point2D point in wanderPoints)
            {
                // keep track of the points we could wander to if we don't find a good path
                double fDistance = point.DistanceBetween(toPosition);
                if (fDistance < fShortestPath)
                {
                    fShortestPath = fDistance;
                    bestMovePoint = point;
                }
            }

            return bestMovePoint;
        }

        private Point2D GetValidRandomWanderPointDumb(VirtualMap virtualMap, Point2D toPosition)
        {
            List<Point2D> wanderablePoints = GetValidWanderPointsDumb(virtualMap, toPosition);

            if (wanderablePoints.Count == 0) return null;

            // wander logic - we are already the closest to the selected enemy
            int nChoices = wanderablePoints.Count;
            int nRandomChoice = Utils.Ran.Next() % nChoices;
            return wanderablePoints[nRandomChoice];
        }

        /// <summary>
        ///     Gets the valid points surrounding a map unit in which they could travel
        /// </summary>
        /// <param name="virtualMap"></param>
        /// <param name="mapUnitPosition">the position they are trying to get to</param>
        /// <returns>a list of positions that the character can walk to  </returns>
        private List<Point2D> GetValidWanderPointsDumb(VirtualMap virtualMap, Point2D mapUnitPosition)
        {
            // get the surrounding points around current active unit
            List<Point2D> surroundingPoints =
                mapUnitPosition.GetConstrainedFourDirectionSurroundingPoints(virtualMap.CurrentMap.NumOfXTiles - 1,
                    virtualMap.CurrentMap.NumOfYTiles - 1);

            List<Point2D> wanderablePoints = new();

            foreach (Point2D point in surroundingPoints)
            {
                // if it isn't walkable then we skip it
                bool bIsMapUnitOnTile = virtualMap.IsMapUnitOccupiedTile(point);
                // virtualMap.IsTileFreeToTravel(point) is for walking and stuff, not great for water creatures apparently
                if (!bIsMapUnitOnTile && CanMoveToDumb(virtualMap, point))
                    //aStar.GetWalkable(point))
                    wanderablePoints.Add(point);
            }

            return wanderablePoints;
        }

        protected virtual bool CanMoveToDumb(VirtualMap virtualMap, Point2D mapUnitPosition)
        {
            return false;
        }

        protected void ProcessNextMoveTowardsMapUnitDumb(VirtualMap virtualMap, Point2D fromPosition,
            Point2D toPosition, AStar aStar)
        {
            Point2D positionToMoveTo = null;

            // it IS a large map, so we do the less resource intense way of pathfinding
            positionToMoveTo =
                GetBestNextPositionToMoveTowardsWalkablePointDumb(virtualMap, fromPosition, toPosition, aStar);

            if (positionToMoveTo == null)
            {
                // only a 50% chance they will wander
                if (Utils.Ran.Next() % 2 == 0) return;

                positionToMoveTo = GetValidRandomWanderPointDumb(virtualMap, toPosition);
                if (positionToMoveTo == null) return;
            }

            MapUnitPosition.XY = positionToMoveTo;
        }

        internal Point2D GetValidRandomWanderPointAStar(Map map, AStar aStar)
        {
            List<Point2D> wanderablePoints = GetValidWanderPointsAStar(map, aStar);

            if (wanderablePoints.Count == 0) return null;

            // wander logic - we are already the closest to the selected enemy
            int nChoices = wanderablePoints.Count;
            int nRandomChoice = Utils.Ran.Next() % nChoices;
            return wanderablePoints[nRandomChoice];
        }

        /// <summary>
        ///     Gets the valid points surrounding a map unit in which they could travel
        /// </summary>
        /// <param name="map">Current map</param>
        /// <param name="aStar">the aStar for the the current map and character type</param>
        /// <returns>a list of positions that the character can walk to  </returns>
        internal List<Point2D> GetValidWanderPointsAStar(Map map, AStar aStar)
        {
            // get the surrounding points around current active unit
            List<Point2D> surroundingPoints =
                MapUnitPosition.XY.GetConstrainedFourDirectionSurroundingPoints(map.NumOfXTiles - 1,
                    map.NumOfYTiles - 1);

            List<Point2D> wanderablePoints = new();

            foreach (Point2D point in surroundingPoints)
            {
                // if it isn't walkable then we skip it
                if (!aStar.GetWalkable(point)) continue;
                wanderablePoints.Add(point);
            }

            return wanderablePoints;
        }

        /// <summary>
        ///     Gets the best next position that a map unit should dumbly move to to get to a particular point
        ///     Note: this is currently a dumb algorithm, just making sure they don't go through other units
        ///     or walls etc.
        ///     In the future this could be expand to use aStar, but some extra optimization work will need to be done
        /// </summary>
        /// <param name="map">Current map</param>
        /// <param name="toPosition">the position they are trying to get to</param>
        /// <param name="aStar">the aStar for the the current map and character type</param>
        /// <returns></returns>
        public Point2D GetBestNextPositionToMoveTowardsWalkablePointAStar(Map map, Point2D toPosition, AStar aStar)
        {
            double fShortestPath = 999f;
            Point2D bestMovePoint = null;

            List<Point2D> wanderPoints = GetValidWanderPointsAStar(map, aStar);

            foreach (Point2D point in wanderPoints)
            {
                // keep track of the points we could wander to if we don't find a good path
                double fDistance = point.DistanceBetween(toPosition);
                if (fDistance < fShortestPath)
                {
                    fShortestPath = fDistance;
                    bestMovePoint = point;
                }
            }

            return bestMovePoint;
        }

        /// <summary>
        ///     Move the map unit closer to the Avatar if possible\
        ///     Uses aStar so only usable on small maps
        /// </summary>
        /// <param name="map"></param>
        /// <param name="avatarPosition"></param>
        /// <param name="aStar"></param>
        protected void ProcessNextMoveTowardsAvatarAStar(Map map, Point2D avatarPosition, AStar aStar)
        {
            const int noPath = 0xFFFF;

            Map.WalkableType walkableType = map.GetWalkableTypeByMapUnit(this);

            Point2D positionToMoveTo = null;
            if (map is not LargeMap) throw new Ultima5ReduxException("Cannot do aStar move towards Avatar on LargeMap");

            // it's a small map so we can rely on the aStar to get us a decent path
            Stack<Node> theWay = aStar.FindPath(MapUnitPosition.XY, avatarPosition);

            if (theWay == null) return;

            int nMoves = theWay.Count;

            if (nMoves == noPath)
            {
                // we do a quick wander check
                // get the surrounding points around current active unit
                List<Point2D> surroundingPoints =
                    MapUnitPosition.XY.GetConstrainedFourDirectionSurroundingPointsWrapAround(map.NumOfXTiles - 1,
                        map.NumOfYTiles - 1);

                Queue<int> positions = Utils.CreateRandomizedIntegerQueue(surroundingPoints.Count);
                int nQueueEntries = positions.Count;
                for (int i = 0; i < nQueueEntries; i++)
                {
                    Point2D position = surroundingPoints[positions.Dequeue()];
                    if (!aStar.GetWalkable(position)) continue;

                    positionToMoveTo = position;
                    break;
                }
            }
            else
            {
                // we just follow the path
                positionToMoveTo = theWay.Pop().Position;
            }

            if (positionToMoveTo == null) return;

            Point2D oldPosition = MapUnitPosition.XY;

            // move to the new point
            MapUnitPosition.XY = positionToMoveTo;
            map.SetWalkableTile(positionToMoveTo, false, walkableType);

            if (map.IsAStarMap(Map.WalkableType.StandardWalking))
                map.RecalculateWalkableTile(oldPosition, Map.WalkableType.StandardWalking);
            if (map.IsAStarMap(Map.WalkableType.CombatLand))
                map.RecalculateWalkableTile(oldPosition, Map.WalkableType.CombatLand);
            if (map.IsAStarMap(Map.WalkableType.CombatWater))
                map.RecalculateWalkableTile(oldPosition, Map.WalkableType.CombatWater);
            if (map.IsAStarMap(Map.WalkableType.CombatFlyThroughWalls))
                map.RecalculateWalkableTile(oldPosition, Map.WalkableType.CombatFlyThroughWalls);
            if (map.IsAStarMap(Map.WalkableType.CombatLandAndWater))
                map.RecalculateWalkableTile(oldPosition, Map.WalkableType.CombatLandAndWater);
        }

        // ReSharper disable once UnusedMember.Global
        public virtual string GetDebugDescription(TimeOfDay timeOfDay) =>
            $"MapUnit {KeyTileReference.Description} {MapUnitPosition} Scheduled to be at:  <b>Movement Attempts</b>: {MovementAttempts} {Movement}";

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
                                                " is already at " + targetXy.X + "," + targetXy.Y);

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
                    mapUnit.Movement.AddNewMovementInstruction(new MovementCommand(prevDirection, nInARow));
                    nInARow = 1;
                }

                prevDirection = newDirection;
                prevPosition = node.Position;
            }

            if (nInARow > 0)
                mapUnit.Movement.AddNewMovementInstruction(new MovementCommand(newDirection, nInARow));
            return true;
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