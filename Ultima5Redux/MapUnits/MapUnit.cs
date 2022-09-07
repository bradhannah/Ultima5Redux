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
        [DataMember(Name = "ScheduleIndex")] private int _scheduleIndex = -1;

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

        [DataMember] public bool ArrivedAtLocation { get; private set; }

        [DataMember] public NonPlayerCharacterState NPCState { get; protected set; }

        [IgnoreDataMember] private readonly MapUnitPosition _savedMapUnitPosition = new();

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

        [IgnoreDataMember]
        protected internal abstract Dictionary<Point2D.Direction, string> DirectionToTileNameBoarded { get; }

        [IgnoreDataMember]
        protected internal virtual Dictionary<Point2D.Direction, string> FourDirectionToTileNameBoarded =>
            DirectionToTileNameBoarded;

        private double _dTimeBetweenAnimation = 0.25f;

        // public double TimeOfLastUpdate { get; set; }
        private DateTime _lastAnimationUpdate;

        private int _nCurrentAnimationIndex = 0;

        protected virtual bool OverrideAiType { get; } = false;

        protected virtual NonPlayerCharacterSchedule.AiType OverridenAiType { get; } =
            NonPlayerCharacterSchedule.AiType.Fixed;

        // public void NewFrameUpdate(double currentTime, double minAnimationTime,
        //     double maxAnimationTime, bool bNonRandomTime = false)
        // {
        //     _dTimeBetweenAnimation =
        //         bNonRandomTime ? minAnimationTime : GetRandomNumber(minAnimationTime, maxAnimationTime);
        //
        //     TimeOfLastUpdate = currentTime; //Time.time;
        // }

        // private static double GetRandomNumber(double minimum, double maximum)
        // {
        //     return Utils.Ran.NextDouble() * (maximum - minimum) + minimum;
        // }

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

            // testing - not sure why I left this out in the first place
            NPCState = npcState;

            // set the characters position 
            MapUnitPosition = mapUnitPosition;
        }

        internal virtual void CompleteNextMove(VirtualMap virtualMap, TimeOfDay timeOfDay, AStar aStar)
        {
            if (virtualMap.CurrentSingleMapReference == null)
                throw new Ultima5ReduxException("No single map is set in virtual map");

            // if you are doing the horse wander and are next to a hitching post then we clear
            // the movement queue so it doesn't keep wandering
            if (OverrideAiType && OverridenAiType == NonPlayerCharacterSchedule.AiType.HorseWander)
            {
                if (virtualMap.IsTileWithinFourDirections(MapUnitPosition.XY,
                        (int)TileReference.SpriteIndex.HitchingPost))
                {
                    Movement.ClearMovements();
                }

                // the horse doesn't wander in the overworld (yet?!)
                if (virtualMap.IsLargeMap) return;
            }

            // if there is no next available movement then we gotta recalculate and see if they should move
            if (!Movement.IsNextCommandAvailable())
                CalculateNextPath(virtualMap, timeOfDay, virtualMap.CurrentSingleMapReference.Floor, aStar);

            // if this NPC has a command in the buffer, so let's execute!
            if (!Movement.IsNextCommandAvailable()) return;

            // it's possible that CalculateNextPath came up empty for a variety of reasons, and that's okay
            // peek and see what we have before we pop it off
            MapUnitMovement.MovementCommandDirection direction = Movement.GetNextMovementCommandDirection(true);
            Point2D adjustedPos = MapUnitMovement.GetAdjustedPos(MapUnitPosition.XY, direction);

            // need to evaluate if I can even move to the next tile before actually popping out of the queue
            bool bIsNpcOnSpace = virtualMap.IsMapUnitOccupiedTile(adjustedPos);
            if (virtualMap.GetTileReference(adjustedPos).IsNPCCapableSpace && !bIsNpcOnSpace)
            {
                // pop the direction from the queue
                _ = Movement.GetNextMovementCommandDirection();
                Move(adjustedPos, MapUnitPosition.Floor, timeOfDay);
                MovementAttempts = 0;
            }
            else
            {
                MovementAttempts++;
            }

            if (ForcedWandering > 0)
            {
                WanderWithinN(virtualMap, timeOfDay, 32, true);
                ForcedWandering--;
                return;
            }

            // if we have tried a few times and failed then we will recalculate
            // could have been a fixed NPC, stubborn Avatar or whatever
            if (MovementAttempts <= 2) return;

            // a little clunky - but basically if a the NPC can't move then it picks a random direction to move (as long as it's legal)
            // and moves that single tile, which will then ultimately follow up with a recalculated route, hopefully breaking and deadlocks with other
            // NPCs
            Debug.WriteLine(NPCRef?.FriendlyName ??
                            $"NOT_NPC got stuck after {MovementAttempts.ToString()} so we are going to find a new direction for them");

            Movement.ClearMovements();

            // we are sick of waiting and will force a wander for a random number of turns to try to let the little
            // dummies figure it out on their own
            Random ran = new();
            int nTimes = ran.Next(0, 2) + 1;
            WanderWithinN(virtualMap, timeOfDay, 32, true);

            ForcedWandering = nTimes;
            MovementAttempts = 0;
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

        /// <summary>
        ///     Checks if an NPC is on a stair or ladder, and if it goes in the correct direction then it returns true indicating
        ///     they can teleport
        /// </summary>
        /// <param name="virtualMap"></param>
        /// <param name="currentTileRef">the tile they are currently on</param>
        /// <param name="ladderOrStairDirection"></param>
        /// <param name="xy"></param>
        /// <returns></returns>
        private bool CheckNpcAndKlimb(VirtualMap virtualMap, TileReference currentTileRef,
            VirtualMap.LadderOrStairDirection ladderOrStairDirection, Point2D xy)
        {
            // is player on a ladder or staircase going in the direction they intend to go?
            bool bIsOnStairCaseOrLadder = GameReferences.SpriteTileReferences.IsStaircase(currentTileRef.Index) ||
                                          GameReferences.SpriteTileReferences.IsLadder(currentTileRef.Index);

            if (!bIsOnStairCaseOrLadder) return false;

            // are they destined to go up or down it?
            if (GameReferences.SpriteTileReferences.IsStaircase(currentTileRef.Index))
            {
                if (virtualMap.IsStairGoingUp(xy))
                    return ladderOrStairDirection == VirtualMap.LadderOrStairDirection.Up;
                return ladderOrStairDirection == VirtualMap.LadderOrStairDirection.Down;
            }

            if (GameReferences.SpriteTileReferences.IsLadderUp(currentTileRef.Index))
                return ladderOrStairDirection == VirtualMap.LadderOrStairDirection.Up;
            return ladderOrStairDirection == VirtualMap.LadderOrStairDirection.Down;
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

        private void Move(MapUnitPosition mapUnitPosition, TimeOfDay tod, bool bIsLargeMap)
        {
            Move(mapUnitPosition);
            if (!bIsLargeMap) UpdateScheduleTracking(tod);
        }

        private void Move(Point2D xy, int nFloor, TimeOfDay tod)
        {
            Move(xy, nFloor);
            UpdateScheduleTracking(tod);
        }

        private void UpdateAnimationIndex()
        {
            if (KeyTileReference.TotalAnimationFrames <= 1) return;

            TimeSpan ts = DateTime.Now.Subtract(_lastAnimationUpdate);
            if (ts.TotalSeconds > _dTimeBetweenAnimation)
            {
                _lastAnimationUpdate = DateTime.Now;
                _nCurrentAnimationIndex = Utils.Ran.Next() % KeyTileReference.TotalAnimationFrames;
            }
        }

        public virtual bool CanBeExited(VirtualMap virtualMap)
        {
            return true;
        }

        // ReSharper disable once UnusedMember.Global
        public virtual string GetDebugDescription(TimeOfDay timeOfDay) =>
            $"MapUnit {KeyTileReference.Description} {MapUnitPosition} Scheduled to be at:  <b>Movement Attempts</b>: {MovementAttempts} {Movement}";

        // ReSharper disable once MemberCanBeProtected.Global

        public virtual TileReference GetNonBoardedTileReference()
        {
            if (DirectionToTileName == null) return KeyTileReference;

            UpdateAnimationIndex();
            if (!DirectionToTileName.ContainsKey(Direction))
                throw new Ultima5ReduxException(
                    $"Tried to get NonBoardedTileReference with direction {Direction} on tile {KeyTileReference.Description}");
            return GameReferences.SpriteTileReferences.GetTileReferenceByName(DirectionToTileName[Direction]);
        }

        public TileReference GetAnimatedTileReference()
        {
            // some things are not animated, so we just use the KeyTileReference every time
            if (!KeyTileReference.IsPartOfAnimation) return KeyTileReference;
            if (KeyTileReference.TotalAnimationFrames < 2) return KeyTileReference;

            UpdateAnimationIndex();
            return GameReferences.SpriteTileReferences.GetTileReference(
                KeyTileReference.Index + _nCurrentAnimationIndex);
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


        public TileReference GetBoardedTileReference()
        {
            Dictionary<Point2D.Direction, string> tileNameDictionary =
                (UseFourDirections ? FourDirectionToTileNameBoarded : DirectionToTileNameBoarded);
            if (tileNameDictionary == null) return KeyTileReference;
            return GameReferences.SpriteTileReferences.GetTileReferenceByName(tileNameDictionary[Direction]);
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

        protected virtual bool CanMoveToDumb(VirtualMap virtualMap, Point2D mapUnitPosition)
        {
            return false;
        }

        /// <summary>
        ///     calculates and stores new path for NPC
        ///     Placed outside into the VirtualMap since it will need information from the active map, VMap and the MapUnit itself
        /// </summary>
        protected void CalculateNextPath(VirtualMap virtualMap, TimeOfDay timeOfDay, int nMapCurrentFloor, AStar aStar)
        {
            // added some safety to save potential exceptions
            // if there is no NPC reference (currently only horses) then we just assign their intended position
            // as their current position 
            MapUnitPosition npcXy = NPCRef == null
                ? MapUnitPosition
                : NPCRef.Schedule.GetCharacterDefaultPositionByTime(timeOfDay);

            bool bIsDead = NPCState?.IsDead ?? false;
            // a little hacky - if they are dead then we just place them at 0,0 which is understood to be the 
            // location for NPCs that aren't present on the map
            if (bIsDead && (npcXy.X != 0 || npcXy.Y != 0))
            {
                npcXy.X = 0;
                npcXy.Y = 0;
                Move(npcXy, timeOfDay, false);
            }

            // the NPC is a non-NPC, so we keep looking
            if (npcXy.X == 0 && npcXy.Y == 0) return;

            // if the NPC is destined for the floor you are on, but are on a different floor, then they need to find a ladder or staircase

            // if the NPC is destined for a different floor then we watch to see if they are on stairs on a ladder
            bool bDifferentFloor = npcXy.Floor != MapUnitPosition.Floor;

            NonPlayerCharacterSchedule.AiType aiType =
                OverrideAiType ? OverridenAiType : NPCRef.Schedule.GetCharacterAiTypeByTime(timeOfDay);

            // if it's a different floor - but NOT for horses
            if (bDifferentFloor && aiType != NonPlayerCharacterSchedule.AiType.HorseWander)
            {
                // if the NPC is supposed to be on a different floor then the floor we are currently on
                // and we they are already on that other floor - AND they are supposed be on our current floor
                if (nMapCurrentFloor != MapUnitPosition.Floor && nMapCurrentFloor != npcXy.Floor) return;

                if (nMapCurrentFloor == npcXy.Floor) // destined for the current floor
                {
                    // we already know they aren't on this floor, so that is safe to assume
                    // so we find the closest and best ladder or stairs for them, make sure they are not occupied and send them down
                    MapUnitPosition npcPrevXy = NPCRef.Schedule.GetCharacterPreviousPositionByTime(timeOfDay);
                    VirtualMap.LadderOrStairDirection ladderOrStairDirection = nMapCurrentFloor > npcPrevXy.Floor
                        ? VirtualMap.LadderOrStairDirection.Down
                        : VirtualMap.LadderOrStairDirection.Up;

                    List<Point2D> stairsAndLadderLocations =
                        virtualMap.GetBestStairsAndLadderLocation(ladderOrStairDirection, npcXy.XY);

                    // let's make sure we have a path we can travel
                    if (stairsAndLadderLocations.Count <= 0)
                    {
                        Debug.WriteLine(
                            $"{NPCRef.FriendlyName} can't find a damn ladder or staircase at {timeOfDay.FormattedTime}");

                        // there is a rare situation (Gardner in Serpents hold) where he needs to go down, but only has access to an up ladder
                        stairsAndLadderLocations = virtualMap.GetBestStairsAndLadderLocation(
                            ladderOrStairDirection == VirtualMap.LadderOrStairDirection.Down
                                ? VirtualMap.LadderOrStairDirection.Up
                                : VirtualMap.LadderOrStairDirection.Down, npcXy.XY);
                        Debug.WriteLine(
                            $"{NPCRef.FriendlyName} couldn't find a ladder or stair going {ladderOrStairDirection.ToString()} {timeOfDay.FormattedTime}");
                        if (stairsAndLadderLocations.Count <= 0)
                            throw new Ultima5ReduxException(NPCRef.FriendlyName +
                                                            " can't find a damn ladder or staircase at " +
                                                            timeOfDay.FormattedTime);
                    }

                    // sloppy, but fine for now
                    MapUnitPosition mapUnitPosition = new()
                    {
                        XY = stairsAndLadderLocations[0], Floor = nMapCurrentFloor
                    };
                    Move(mapUnitPosition, timeOfDay, false);
                    return;
                }
                else // map character is destined for a higher or lower floor
                {
                    TileReference currentTileReference = virtualMap.GetTileReference(MapUnitPosition.XY);

                    // if we are going to the next floor, then look for something that goes up, otherwise look for something that goes down
                    VirtualMap.LadderOrStairDirection ladderOrStairDirection = npcXy.Floor > MapUnitPosition.Floor
                        ? VirtualMap.LadderOrStairDirection.Up
                        : VirtualMap.LadderOrStairDirection.Down;
                    bool bNpcShouldKlimb = CheckNpcAndKlimb(virtualMap, currentTileReference, ladderOrStairDirection,
                        MapUnitPosition.XY);
                    if (bNpcShouldKlimb)
                    {
                        // teleport them and return immediately
                        MoveNpcToDefaultScheduledPosition(timeOfDay);
                        Debug.WriteLine($"{NPCRef.FriendlyName} just went to a different floor");
                        return;
                    }

                    // we now need to build a path to the best choice of ladder or stair
                    // the list returned will be prioritized based on vicinity
                    List<Point2D> stairsAndLadderLocations =
                        virtualMap.getBestStairsAndLadderLocationBasedOnCurrentPosition(ladderOrStairDirection,
                            npcXy.XY, MapUnitPosition.XY);
                    foreach (Point2D xy in stairsAndLadderLocations)
                    {
                        bool bPathBuilt = BuildPath(this, xy, aStar);
                        // if a path was successfully built, then we have no need to build another path since this is the "best" path
                        if (bPathBuilt) return;
                    }

                    Debug.WriteLine(
                        $"Tried to build a path for {NPCRef.FriendlyName} to {npcXy} but it failed, keep an eye on it...");
                    return;
                }
            }

            // is the character is in their prescribed location?
            if (MapUnitPosition == npcXy)
                // test all the possibilities, special calculations for all of them
                switch (aiType)
                {
                    case NonPlayerCharacterSchedule.AiType.Fixed:
                        // do nothing, they are where they are supposed to be 
                        break;
                    case NonPlayerCharacterSchedule.AiType.DrudgeWorthThing:
                    case NonPlayerCharacterSchedule.AiType.Wander:
                        // choose a tile within N tiles that is not blocked, and build a single path
                        WanderWithinN(virtualMap, timeOfDay, 2);
                        break;
                    case NonPlayerCharacterSchedule.AiType.BigWander:
                        // choose a tile within N tiles that is not blocked, and build a single path
                        WanderWithinN(virtualMap, timeOfDay, 4);
                        break;
                    case NonPlayerCharacterSchedule.AiType.ChildRunAway:
                        break;
                    case NonPlayerCharacterSchedule.AiType.MerchantThing:
                        // don't think they move....?
                        break;
                    case NonPlayerCharacterSchedule.AiType.ExtortOrAttackOrFollow:
                        // set location of Avatar as way point, but only set the first movement from the list if within N of Avatar
                        break;
                    case NonPlayerCharacterSchedule.AiType.HorseWander:
                        WanderWithinN(virtualMap, timeOfDay, 4);
                        break;
                    default:
                        throw new Ultima5ReduxException(
                            $"An unexpected movement AI was encountered: {aiType} for NPC: {NPCRef.Name}");
                }
            else // character not in correct position
                switch (aiType)
                {
                    // Horses don't move if they are touching a hitching post
                    case NonPlayerCharacterSchedule.AiType.HorseWander:
                        if (!virtualMap.IsTileWithinFourDirections(npcXy.XY,
                                (int)TileReference.SpriteIndex.HitchingPost))
                        {
                            WanderWithinN(virtualMap, timeOfDay, 4);
                        }

                        break;
                    case NonPlayerCharacterSchedule.AiType.MerchantThing:
                    case NonPlayerCharacterSchedule.AiType.Fixed:
                        // move to the correct position
                        BuildPath(this, npcXy.XY, aStar);
                        break;
                    case NonPlayerCharacterSchedule.AiType.DrudgeWorthThing:
                    case NonPlayerCharacterSchedule.AiType.Wander:
                    case NonPlayerCharacterSchedule.AiType.BigWander:
                        // different wanders have different different radius'
                        int nWanderTiles = aiType == NonPlayerCharacterSchedule.AiType.Wander ? 2 : 4;
                        // we check to see how many moves it would take to get to their destination, if it takes
                        // more than the allotted amount then we first build a path to the destination
                        // note: because you are technically within X tiles doesn't mean you can access it
                        int nMoves = virtualMap.GetTotalMovesToLocation(MapUnitPosition.XY, npcXy.XY,
                            Map.WalkableType.StandardWalking);
                        // 
                        if (nMoves <= nWanderTiles)
                            WanderWithinN(virtualMap, timeOfDay, nWanderTiles);
                        else
                            // move to the correct position
                            BuildPath(this, npcXy.XY, aStar);
                        break;
                    case NonPlayerCharacterSchedule.AiType.ChildRunAway:
                        // if the avatar is close by then move away from him, otherwise return to original path, one move at a time
                        break;
                    case NonPlayerCharacterSchedule.AiType.ExtortOrAttackOrFollow:
                        // set location of Avatar as way point, but only set the first movement from the list if within N of Avatar
                        break;
                    default:
                        throw new Ultima5ReduxException(
                            $"An unexpected movement AI was encountered: {aiType} for NPC: {NPCRef.Name}");
                }
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

        /// <summary>
        ///     Moves the NPC to the appropriate floor and location based on the their expected location and position
        /// </summary>
        protected void MoveNpcToDefaultScheduledPosition(TimeOfDay timeOfDay)
        {
            MapUnitPosition npcXy = NPCRef.Schedule.GetCharacterDefaultPositionByTime(timeOfDay);

            // the NPC is a non-NPC, so we keep looking
            if (npcXy.X == 0 && npcXy.Y == 0) return;

            Move(npcXy, timeOfDay, false);
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

        protected void UpdateScheduleTracking(TimeOfDay tod)
        {
            if (MapUnitPosition == NPCRef.Schedule.GetCharacterDefaultPositionByTime(tod)) ArrivedAtLocation = true;

            int nCurrentScheduleIndex = NPCRef.Schedule.GetScheduleIndex(tod);
            // it's the first time, so we don't reset the ArrivedAtLocation flag 
            if (_scheduleIndex == -1)
            {
                _scheduleIndex = nCurrentScheduleIndex;
            }
            else if (_scheduleIndex != nCurrentScheduleIndex)
            {
                _scheduleIndex = nCurrentScheduleIndex;
                ArrivedAtLocation = false;
            }
        }


        /// <summary>
        ///     Points a character in a random position within a certain number of tiles to their scheduled position
        /// </summary>
        /// <param name="virtualMap"></param>
        /// <param name="timeOfDay"></param>
        /// <param name="nMaxDistance">max distance character should be from their scheduled position</param>
        /// <param name="bForceWander">force a wander? if not forced then there is a chance they will not move anywhere</param>
        /// <returns>the direction they should move</returns>
        protected void WanderWithinN(VirtualMap virtualMap, TimeOfDay timeOfDay, int nMaxDistance,
            bool bForceWander = false)
        {
            Random ran = new();

            // 50% of the time we won't even try to move at all
            int nRan = ran.Next(2);
            if (nRan == 0 && !bForceWander) return;

            MapUnitPosition mapUnitPosition = MapUnitPosition;
            MapUnitPosition scheduledPosition = NPCRef != null
                ? NPCRef.Schedule.GetCharacterDefaultPositionByTime(timeOfDay)
                :
                // if there is no NPCRef, we may still wander - such as a horse 
                TheSmallMapCharacterState.TheMapUnitPosition;

            // i could get the size dynamically, but that's a waste of CPU cycles
            Point2D adjustedPosition = virtualMap.GetWanderCharacterPosition(mapUnitPosition.XY, scheduledPosition.XY,
                nMaxDistance, out MapUnitMovement.MovementCommandDirection direction);

            // check to see if the random direction is within the correct distance
            if (direction != MapUnitMovement.MovementCommandDirection.None &&
                !scheduledPosition.XY.IsWithinN(adjustedPosition, nMaxDistance))
                throw new Ultima5ReduxException(
                    "GetWanderCharacterPosition has told us to go outside of our expected maximum area");
            // can we even travel onto the tile?
            if (!virtualMap.IsTileFreeToTravel(adjustedPosition, true))
            {
                if (direction != MapUnitMovement.MovementCommandDirection.None)
                    throw new Ultima5ReduxException("Was sent to a tile, but it isn't in free in WanderWithinN");
                // something else is on the tile, so we don't move
                return;
            }

            // add the single instruction to the queue
            Movement.AddNewMovementInstruction(new MovementCommand(direction, 1));
        }
    }
}