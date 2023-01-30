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
        private const double D_TIME_BETWEEN_ANIMATION = 0.25f;
        private const float MAX_VISIBILITY = 5;
        private const int N_DISTANCE_TO_TRIGGER_GARGOYLES = 4;
        [DataMember(Name = "KeyTileIndex")] private int _keyTileIndex = -1;

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

        [DataMember] public NonPlayerCharacterState NPCState { get; protected internal set; }

        [IgnoreDataMember] private readonly MapUnitPosition _savedMapUnitPosition = new();

        [IgnoreDataMember] public NonPlayerCharacterReference NPCRef => NPCState?.NPCRef;

        [IgnoreDataMember]
        public virtual TileReference KeyTileReference
        {
            get => NPCRef == null
                ? GameReferences.Instance.SpriteTileReferences.GetTileReference(_keyTileIndex)
                : GameReferences.Instance.SpriteTileReferences.GetTileReference(NPCRef.NPCKeySprite);
            set => _keyTileIndex = value.Index;
        }

        [IgnoreDataMember]
        protected internal abstract Dictionary<Point2D.Direction, string> DirectionToTileNameBoarded { get; }

        [IgnoreDataMember]
        protected internal virtual Dictionary<Point2D.Direction, string> FourDirectionToTileNameBoarded =>
            DirectionToTileNameBoarded;

        [IgnoreDataMember] protected internal virtual bool OverrideAiType => NPCState?.OverrideAiType ?? false;

        [IgnoreDataMember] protected abstract Dictionary<Point2D.Direction, string> DirectionToTileName { get; }

        [IgnoreDataMember]
        protected virtual NonPlayerCharacterSchedule.AiType OverridenAiType =>
            NPCState?.OverridenAiType ?? NonPlayerCharacterSchedule.AiType.Fixed;

        private DateTime _lastAnimationUpdate;

        private int _nCurrentAnimationIndex;
        public virtual bool CanStackMapUnitsOnTop => false;

        [field: DataMember(Name = "NpcRefIndex")]
        public int NpcRefIndex { get; } = -1;

        // Should the tile be animated? 
        // presently, it's only for Stone Gargoyles, but could be extended to more
        // ReSharper disable once MemberCanBePrivate.Global
        public bool ShouldAnimate
        {
            get
            {
                if (NPCRef == null) return true;
                return GetCurrentAiType(GameStateReference.State.TheTimeOfDay) !=
                       NonPlayerCharacterSchedule.AiType.StoneGargoyleTrigger;
            }
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

            if (npcState != null) NpcRefIndex = npcState.NPCRef?.DialogIndex ?? -1;

            Debug.Assert(Movement != null);

            _keyTileIndex = tileReference.Index;

            // testing - not sure why I left this out in the first place
            NPCState = npcState;

            // set the characters position 
            MapUnitPosition = mapUnitPosition;
        }

        internal virtual void CompleteNextNonCombatMove(RegularMap regularMap, TimeOfDay timeOfDay)
        {
            if (regularMap.CurrentSingleMapReference == null)
                throw new Ultima5ReduxException("No single map is set in virtual map");

            // if you are doing the horse wander and are next to a hitching post then we clear
            // the movement queue so it doesn't keep wandering
            if (OverrideAiType && OverridenAiType == NonPlayerCharacterSchedule.AiType.HorseWander)
            {
                if (regularMap.IsTileWithinFourDirections(MapUnitPosition.XY,
                        (int)TileReference.SpriteIndex.HitchingPost))
                {
                    Movement.ClearMovements();
                }

                // the horse doesn't wander in the overworld (yet?!)
                if (regularMap is LargeMap) return;
            }

            /////
            // WE are going to check for aggressive NPC types for extortion of just looking for a fight! 

            // if there is no next available movement then we gotta recalculate and see if they should move
            if (regularMap is SmallMap smallMap && !Movement.IsNextCommandAvailable())
                CalculateNextPathOnSmallMap(smallMap, timeOfDay, regularMap.CurrentSingleMapReference.Floor);

            // if this NPC has a command in the buffer, so let's execute!
            if (!Movement.IsNextCommandAvailable()) return;

            // it's possible that CalculateNextPath came up empty for a variety of reasons, and that's okay
            // peek and see what we have before we pop it off
            MapUnitMovement.MovementCommandDirection direction = Movement.GetNextMovementCommandDirection(true);
            Point2D adjustedPos = MapUnitMovement.GetAdjustedPos(MapUnitPosition.XY, direction);

            // need to evaluate if I can even move to the next tile before actually popping out of the queue
            bool bIsNpcOnSpace = regularMap.IsMapUnitOccupiedTile(adjustedPos);
            if (regularMap.GetTileReference(adjustedPos).IsNPCCapableSpace && !bIsNpcOnSpace)
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
                WanderWithinN(regularMap, timeOfDay, 32, true);
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
            WanderWithinN(regularMap, timeOfDay, 32, true);

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
        ///     Builds the actual path for the character to travel based on their current position and their target position
        /// </summary>
        /// <param name="mapUnit">where the character is presently</param>
        /// <param name="targetXy">where you want them to go</param>
        /// <param name="aStar"></param>
        /// <param name="bOnlyOne">provide the first path you find, don't bother looking for more</param>
        /// <returns>returns true if a path was found, false if it wasn't</returns>
        private static bool BuildPath(MapUnitDetails mapUnit, Point2D targetXy, AStar aStar,
            bool bOnlyOne = false)
        {
            if (mapUnit.MapUnitPosition.XY == targetXy) return true;

            Debug.WriteLine(
                $"Asked to build a path, but {mapUnit.FriendlyName} is already at {targetXy.X},{targetXy.Y}");

            Stack<Node> nodeStack = aStar.FindPath(mapUnit.MapUnitPosition.XY, targetXy);

            var prevDirection = MapUnitMovement.MovementCommandDirection.None;
            var newDirection = MapUnitMovement.MovementCommandDirection.None;
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

                if (bOnlyOne) break;
            }

            if (nInARow > 0)
                mapUnit.Movement.AddNewMovementInstruction(new MovementCommand(newDirection, nInARow));
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

        /// <summary>
        ///     calculates and stores new path for NPC
        ///     Placed outside into the VirtualMap since it will need information from the active map, VMap and the MapUnit itself
        /// </summary>
        private void CalculateNextPathOnSmallMap(SmallMap smallMap, TimeOfDay timeOfDay, int nMapCurrentFloor)
        {
            // added some safety to save potential exceptions
            // if there is no NPC reference (currently only horses) then we just assign their intended position
            // as their current position 
            MapUnitPosition npcDestinationPosition = NPCRef == null
                ? MapUnitPosition
                : NPCRef.Schedule.GetCharacterDefaultPositionByTime(timeOfDay);

            bool bIsDead = NPCState?.IsDead ?? false;
            // a little hacky - if they are dead then we just place them at 0,0 which is understood to be the 
            // location for NPCs that aren't present on the map
            if (bIsDead && (npcDestinationPosition.X != 0 || npcDestinationPosition.Y != 0))
            {
                npcDestinationPosition.X = 0;
                npcDestinationPosition.Y = 0;
                Move(npcDestinationPosition, timeOfDay, false);
            }

            // the NPC is a non-NPC, so we keep looking
            if (npcDestinationPosition.X == 0 && npcDestinationPosition.Y == 0) return;

            // if the NPC is destined for the floor you are on, but are on a different floor, then they need to find a ladder or staircase

            // if the NPC is destined for a different floor then we watch to see if they are on stairs on a ladder
            bool bDifferentFloor = npcDestinationPosition.Floor != MapUnitPosition.Floor;

            if (!OverrideAiType && NPCRef == null)
                throw new Ultima5ReduxException(
                    "You MUST override the AI Type of a MapUnit if they have no corresponding NPC Reference");

            // basically if you are within a certain visible distance AND they are trying to arrest you,
            // then we will override the AI, otherwise they can stick to their normal schedules
            bool bWithinVisibilityRange =
                smallMap.CurrentAvatarPosition.XY.DistanceBetween(MapUnitPosition.XY) < MAX_VISIBILITY;
            NonPlayerCharacterSchedule.AiType aiType;
            if (smallMap.IsWantedManByThePoPo && bWithinVisibilityRange)
                aiType = NPCRef is { IsGuard: false }
                    ? NonPlayerCharacterSchedule.AiType.ChildRunAway
                    : NonPlayerCharacterSchedule.AiType.ExtortOrAttackOrFollow;
            else
                aiType = GetCurrentAiType(timeOfDay);

            // if the NPC is currently on a different floor different floor - but NOT for horses
            if (bDifferentFloor && aiType != NonPlayerCharacterSchedule.AiType.HorseWander)
            {
                // if the NPC is supposed to be on a different floor then the floor we are currently on
                // and we they are already on that other floor - AND they are supposed be on our current floor
                if (nMapCurrentFloor != MapUnitPosition.Floor &&
                    nMapCurrentFloor != npcDestinationPosition.Floor) return;

                if (nMapCurrentFloor == npcDestinationPosition.Floor) // destined for the current floor
                {
                    // we already know they aren't on this floor, so that is safe to assume
                    // so we find the closest and best ladder or stairs for them, make sure they are not occupied and send them down
                    if (NPCRef == null)
                        throw new Ultima5ReduxException("Tried to get NPC schedule, but NPCRef was null");
                    MapUnitPosition npcPrevXy = NPCRef.Schedule.GetCharacterPreviousPositionByTime(timeOfDay);
                    VirtualMap.LadderOrStairDirection ladderOrStairDirection = nMapCurrentFloor > npcPrevXy.Floor
                        ? VirtualMap.LadderOrStairDirection.Down
                        : VirtualMap.LadderOrStairDirection.Up;

                    List<Point2D> stairsAndLadderLocations =
                        smallMap.GetBestStairsAndLadderLocation(ladderOrStairDirection, npcDestinationPosition.XY);

                    // let's make sure we have a path we can travel
                    if (stairsAndLadderLocations.Count <= 0)
                    {
                        Debug.WriteLine(
                            $"{NPCRef.FriendlyName} can't find a damn ladder or staircase at {timeOfDay.FormattedTime}");

                        // there is a rare situation (Gardner in Serpents hold) where he needs to go down, but only has access to an up ladder
                        stairsAndLadderLocations = smallMap.GetBestStairsAndLadderLocation(
                            ladderOrStairDirection == VirtualMap.LadderOrStairDirection.Down
                                ? VirtualMap.LadderOrStairDirection.Up
                                : VirtualMap.LadderOrStairDirection.Down, npcDestinationPosition.XY);
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
                else // MapUnit is destined for a higher or lower floor
                {
                    TileReference currentTileReference = smallMap.GetTileReference(MapUnitPosition.XY);

                    // if we are going to the next floor, then look for something that goes up, otherwise look for something that goes down
                    VirtualMap.LadderOrStairDirection ladderOrStairDirection =
                        npcDestinationPosition.Floor > MapUnitPosition.Floor
                            ? VirtualMap.LadderOrStairDirection.Up
                            : VirtualMap.LadderOrStairDirection.Down;
                    bool bNpcShouldKlimb = CheckNpcAndKlimb(smallMap, currentTileReference, ladderOrStairDirection,
                        MapUnitPosition.XY);
                    if (bNpcShouldKlimb)
                    {
                        // teleport them and return immediately
                        MoveNpcToDefaultScheduledPosition(timeOfDay);
                        Debug.WriteLine($"{NPCRef?.FriendlyName} just went to a different floor");
                        return;
                    }

                    // we now need to build a path to the best choice of ladder or stair
                    // the list returned will be prioritized based on vicinity
                    List<Point2D> stairsAndLadderLocations =
                        smallMap.getBestStairsAndLadderLocationBasedOnCurrentPosition(ladderOrStairDirection,
                            npcDestinationPosition.XY, MapUnitPosition.XY);
                    foreach (Point2D xy in stairsAndLadderLocations)
                    {
                        bool bPathBuilt = BuildPath(this, xy,
                            CreateAStar(smallMap));
                        // if a path was successfully built, then we have no need to build another path since this is the "best" path
                        if (bPathBuilt) return;
                    }

                    Debug.WriteLine(
                        $"Tried to build a path for {NPCRef?.FriendlyName} to {npcDestinationPosition} but it failed, keep an eye on it...");
                    return;
                }
            }

            // is the character is in their prescribed location?
            if (MapUnitPosition == npcDestinationPosition)
                // test all the possibilities, special calculations for all of them
                switch (aiType)
                {
                    case NonPlayerCharacterSchedule.AiType.BlackthornGuardFixed:
                    case NonPlayerCharacterSchedule.AiType.Fixed:
                        // do nothing, they are where they are supposed to be 
                        break;
                    case NonPlayerCharacterSchedule.AiType.MerchantBuyingSellingCustom:
                    case NonPlayerCharacterSchedule.AiType.MerchantBuyingSellingWander:
                    case NonPlayerCharacterSchedule.AiType.Wander:
                        // choose a tile within N tiles that is not blocked, and build a single path
                        WanderWithinN(smallMap, timeOfDay, 2);
                        break;
                    case NonPlayerCharacterSchedule.AiType.BlackthornGuardWander:
                    case NonPlayerCharacterSchedule.AiType.BigWander:
                        // choose a tile within N tiles that is not blocked, and build a single path
                        WanderWithinN(smallMap, timeOfDay, 4);
                        break;
                    case NonPlayerCharacterSchedule.AiType.ChildRunAway:
                        RunAwayFromAvatar(smallMap,
                            //aStar, 
                            MapUnitPosition);
                        break;
                    case NonPlayerCharacterSchedule.AiType.CustomAi:
                    case NonPlayerCharacterSchedule.AiType.MerchantBuyingSelling:
                        // don't think they move....?
                        break;
                    case NonPlayerCharacterSchedule.AiType.DrudgeWorthThing:
                        GetCloserToAvatar(smallMap); //, aStar);
                        break;
                    case NonPlayerCharacterSchedule.AiType.ExtortOrAttackOrFollow:
                        // set location of Avatar as way point, but only set the first movement from the list if within N of Avatar
                        BuildPath(this, smallMap.CurrentAvatarPosition.XY,
                            CreateAStar(smallMap),
                            true);
                        break;
                    case NonPlayerCharacterSchedule.AiType.HorseWander:
                        WanderWithinN(smallMap, timeOfDay, 4);
                        break;
                    case NonPlayerCharacterSchedule.AiType.StoneGargoyleTrigger:
                        // basic behaviour is if the Avatar is close to them (2 tiles between) then they 
                        // become aggressive - maybe just switch their AI type at that point
                        MapUnitPosition avatarPosition = smallMap.CurrentAvatarPosition;
                        if (avatarPosition.XY.DistanceBetween(MapUnitPosition.XY) <= N_DISTANCE_TO_TRIGGER_GARGOYLES)
                        {
                            NPCState?.OverrideAi(NonPlayerCharacterSchedule.AiType.DrudgeWorthThing);
                        }

                        break;
                    case NonPlayerCharacterSchedule.AiType.FixedExceptAttackWhenIsWantedByThePoPo:
                        if (smallMap.IsWantedManByThePoPo)
                        {
                            //BuildPath(this, virtualMap.TheMapUnits.CurrentAvatarPosition.XY, aStar, true);
                            GetCloserToAvatar(smallMap);
                            //, aStar);
                        }

                        // else they stay where they are
                        break;
                    case NonPlayerCharacterSchedule.AiType.Begging:
                    case NonPlayerCharacterSchedule.AiType.GenericExtortingGuard:
                    case NonPlayerCharacterSchedule.AiType.HalfYourGoldExtortingGuard:
                    case NonPlayerCharacterSchedule.AiType.SmallWanderWantsToChat:
                        // let's have them try to hang out with the avatar most of the time, but not everytime
                        // for a little randomness
                        if (Utils.OneInXOdds(3))
                        {
                            WanderWithinN(smallMap, timeOfDay, 3);
                        }
                        else
                        {
                            BuildPath(this, smallMap.CurrentAvatarPosition.XY,
                                CreateAStar(smallMap),
                                true);
                        }

                        break;
                    case NonPlayerCharacterSchedule.AiType.FollowAroundAndBeAnnoyingThenNeverSeeAgain:
                        // let's have them try to hang out with the avatar most of the time, but not everytime
                        // for a little randomness
                        if (Utils.OneInXOdds(3))
                        {
                            WanderWithinN(smallMap, timeOfDay, 4);
                        }
                        else
                        {
                            BuildPath(this, smallMap.CurrentAvatarPosition.XY,
                                CreateAStar(smallMap),
                                true);
                        }

                        break;
                    default:
                        throw new Ultima5ReduxException(
                            $"An unexpected movement AI was encountered: {aiType} for NPC: {NPCRef?.Name}");
                }
            else // character not in correct position
                switch (aiType)
                {
                    // Horses don't move if they are touching a hitching post
                    case NonPlayerCharacterSchedule.AiType.HorseWander:
                        if (!smallMap.IsTileWithinFourDirections(npcDestinationPosition.XY,
                                (int)TileReference.SpriteIndex.HitchingPost))
                        {
                            WanderWithinN(smallMap, timeOfDay, 4);
                        }

                        break;
                    case NonPlayerCharacterSchedule.AiType.BlackthornGuardFixed:
                    case NonPlayerCharacterSchedule.AiType.CustomAi:
                    case NonPlayerCharacterSchedule.AiType.MerchantBuyingSelling:
                    case NonPlayerCharacterSchedule.AiType.Fixed:
                        // move to the correct position
                        BuildPath(this, npcDestinationPosition.XY,
                            CreateAStar(smallMap));
                        break;
                    case NonPlayerCharacterSchedule.AiType.BlackthornGuardWander:
                    case NonPlayerCharacterSchedule.AiType.MerchantBuyingSellingWander:
                    case NonPlayerCharacterSchedule.AiType.MerchantBuyingSellingCustom:
                    case NonPlayerCharacterSchedule.AiType.Wander:
                    case NonPlayerCharacterSchedule.AiType.BigWander:
                        // different wanders have different different radius'
                        int nWanderTiles = aiType == NonPlayerCharacterSchedule.AiType.Wander ? 2 : 4;
                        // we check to see how many moves it would take to get to their destination, if it takes
                        // more than the allotted amount then we first build a path to the destination
                        // note: because you are technically within X tiles doesn't mean you can access it
                        int nMoves = smallMap.GetTotalMovesToLocation(MapUnitPosition.XY, npcDestinationPosition.XY,
                            Map.WalkableType.StandardWalking);
                        // 
                        if (nMoves <= nWanderTiles)
                            WanderWithinN(smallMap, timeOfDay, nWanderTiles);
                        else
                            // move to the correct position
                            BuildPath(this, npcDestinationPosition.XY,
                                CreateAStar(smallMap));
                        //, aStar);
                        break;
                    case NonPlayerCharacterSchedule.AiType.ChildRunAway:
                        // if the avatar is close by then move away from him, otherwise return to original path, one move at a time
                        RunAwayFromAvatar(smallMap,
                            //aStar, 
                            MapUnitPosition);
                        break;
                    case NonPlayerCharacterSchedule.AiType.DrudgeWorthThing:
                        GetCloserToAvatar(smallMap);
                        //, aStar);
                        break;
                    case NonPlayerCharacterSchedule.AiType.ExtortOrAttackOrFollow:
                        // set location of Avatar as way point, but only set the first movement from the list if within N of Avatar
                        // check to see how close we are - if we are too far, then just go to our scheduled location
                        // we should only try to get closer, not build a whole path
                        BuildPath(this, smallMap.CurrentAvatarPosition.XY,
                            CreateAStar(smallMap),
                            true);
                        break;
                    case NonPlayerCharacterSchedule.AiType.FixedExceptAttackWhenIsWantedByThePoPo:
                        if (smallMap.IsWantedManByThePoPo)
                        {
                            GetCloserToAvatar(smallMap);
                            //, aStar);
                            //BuildPath(this, virtualMap.TheMapUnits.CurrentAvatarPosition.XY, aStar, true);
                        }
                        else // get where you are going so you can be stationary
                        {
                            BuildPath(this, npcDestinationPosition.XY,
                                CreateAStar(smallMap));
                            //, aStar);
                        }

                        break;
                    case NonPlayerCharacterSchedule.AiType.Begging:
                    case NonPlayerCharacterSchedule.AiType.GenericExtortingGuard:
                    case NonPlayerCharacterSchedule.AiType.HalfYourGoldExtortingGuard:
                    case NonPlayerCharacterSchedule.AiType.SmallWanderWantsToChat:
                    case NonPlayerCharacterSchedule.AiType.FollowAroundAndBeAnnoyingThenNeverSeeAgain:
                        WanderWithinN(smallMap, timeOfDay, 4);
                        break;
                    default:
                        throw new Ultima5ReduxException(
                            $"An unexpected movement AI was encountered: {aiType} for NPC: {NPCRef?.Name}");
                }
        }

        /// <summary>
        ///     Checks if an NPC is on a stair or ladder, and if it goes in the correct direction then it returns true indicating
        ///     they can teleport
        /// </summary>
        /// <param name="map"></param>
        /// <param name="currentTileRef">the tile they are currently on</param>
        /// <param name="ladderOrStairDirection"></param>
        /// <param name="xy"></param>
        /// <returns></returns>
        private bool CheckNpcAndKlimb(Map map, TileReference currentTileRef,
            VirtualMap.LadderOrStairDirection ladderOrStairDirection, Point2D xy)
        {
            // is player on a ladder or staircase going in the direction they intend to go?
            bool bIsOnStairCaseOrLadder =
                TileReferences.IsStaircase(currentTileRef.Index) ||
                GameReferences.Instance.SpriteTileReferences.IsLadder(currentTileRef.Index);

            if (!bIsOnStairCaseOrLadder) return false;

            // are they destined to go up or down it?
            if (TileReferences.IsStaircase(currentTileRef.Index))
            {
                if (GameReferences.Instance.SmallMapRef.DoStairsGoUp(map.MapLocation,
                        map.CurrentSingleMapReference.Floor, xy, out _))
                    return ladderOrStairDirection == VirtualMap.LadderOrStairDirection.Up;
                return ladderOrStairDirection == VirtualMap.LadderOrStairDirection.Down;
            }

            if (TileReferences.IsLadderUp(currentTileRef.Index))
                return ladderOrStairDirection == VirtualMap.LadderOrStairDirection.Up;
            return ladderOrStairDirection == VirtualMap.LadderOrStairDirection.Down;
        }

        private AStar CreateAStar(Map map) =>
            map.GetAStarMap(map.GetWalkableTypeByMapUnit(this));

        /// <summary>
        ///     Gets the best next position that a map unit should dumbly move to to get to a particular point
        ///     Note: this is currently a dumb algorithm, just making sure they don't go through other units
        ///     or walls etc.
        ///     In the future this could be expand to use aStar, but some extra optimization work will need to be done
        /// </summary>
        /// <param name="regularMap"></param>
        /// <param name="fromPosition"></param>
        /// <param name="toPosition">the position they are trying to get to</param>
        /// <returns></returns>
        private Point2D GetBestNextPositionToMoveTowardsWalkablePointDumb(RegularMap regularMap, Point2D fromPosition,
            Point2D toPosition)
        {
            double fShortestPath = 999f;
            Point2D bestMovePoint = null;

            // you want the valid wander points from the current position
            List<Point2D> wanderPoints = GetValidWanderPointsDumb(regularMap, fromPosition);

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

        private void GetCloserToAvatar(RegularMap regularMap)
        {
            IEnumerable<Point2D> possiblePositions = MapUnitPosition.XY
                .GetConstrainedFourDirectionSurroundingPointsCloserTo(
                    regularMap.CurrentAvatarPosition.XY,
                    regularMap.NumOfXTiles, regularMap.NumOfYTiles);

            AStar aStar = regularMap.GetAStarMap(regularMap.GetWalkableTypeByMapUnit(this));

            foreach (Point2D point in possiblePositions)
            {
                if (regularMap.IsTileFreeToTravelForAvatar(point, true))
                    BuildPath(this, point, aStar,
                        true);
            }
        }

        private Point2D GetValidRandomWanderPointDumb(RegularMap regularMap, Point2D toPosition)
        {
            List<Point2D> wanderablePoints = GetValidWanderPointsDumb(regularMap, toPosition);

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
        private List<Point2D> GetValidWanderPointsAStar(Map map, AStar aStar)
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
        ///     Gets the valid points surrounding a map unit in which they could travel
        /// </summary>
        /// <param name="map"></param>
        /// <param name="mapUnitPosition">the position they are trying to get to</param>
        /// <returns>a list of positions that the character can walk to  </returns>
        private List<Point2D> GetValidWanderPointsDumb(Map map, Point2D mapUnitPosition)
        {
            // get the surrounding points around current active unit
            List<Point2D> surroundingPoints =
                mapUnitPosition.GetConstrainedFourDirectionSurroundingPoints(map.NumOfXTiles - 1,
                    map.NumOfYTiles - 1);

            List<Point2D> wanderablePoints = new();

            foreach (Point2D point in surroundingPoints)
            {
                // if it isn't walkable then we skip it
                bool bIsMapUnitOnTile = map.IsMapUnitOccupiedTile(point);
                if (!bIsMapUnitOnTile && CanMoveToDumb(map, point))
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

        /// <summary>
        ///     move the character to a new position
        /// </summary>
        /// <param name="xy"></param>
        /// <param name="nFloor"></param>
        private void Move(Point2D xy, int nFloor)
        {
            MapUnitPosition.XY = xy;
            MapUnitPosition.Floor = nFloor;
        }

        private void RunAwayFromAvatar(RegularMap regularMap,
            MapUnitPosition npcDestinationPosition)
        {
            IEnumerable<Point2D> possiblePositions = npcDestinationPosition.XY
                .GetConstrainedFourDirectionSurroundingPointsFurtherAway(
                    regularMap.CurrentAvatarPosition.XY,
                    regularMap.NumOfXTiles - 1, regularMap.NumOfYTiles - 1);
            AStar aStar = CreateAStar(regularMap);
            foreach (Point2D point in possiblePositions)
            {
                // this will return ASAP if the end point is not travelable by the mapunit
                BuildPath(this, point, aStar, true);
            }
        }

        private void UpdateAnimationIndex()
        {
            if (KeyTileReference.TotalAnimationFrames <= 1) return;
            if (!ShouldAnimate)
            {
                _nCurrentAnimationIndex = 0;
                return;
            }

            TimeSpan ts = DateTime.Now.Subtract(_lastAnimationUpdate);
            if (ts.TotalSeconds > D_TIME_BETWEEN_ANIMATION)
            {
                _lastAnimationUpdate = DateTime.Now;
                _nCurrentAnimationIndex = Utils.Ran.Next() % KeyTileReference.TotalAnimationFrames;
            }
        }

        private void UpdateScheduleTracking(TimeOfDay tod)
        {
            // sometime there is no NPCRef so lets just return (like purchased horses)
            if (NPCRef == null) return;
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
        /// <param name="regularMap"></param>
        /// <param name="timeOfDay"></param>
        /// <param name="nMaxDistance">max distance character should be from their scheduled position</param>
        /// <param name="bForceWander">force a wander? if not forced then there is a chance they will not move anywhere</param>
        /// <returns>the direction they should move</returns>
        private void WanderWithinN(RegularMap regularMap, TimeOfDay timeOfDay, int nMaxDistance,
            bool bForceWander = false)
        {
            Random ran = new();

            // 50% of the time we won't even try to move at all
            int nRan = ran.Next(2);
            if (nRan == 0 && !bForceWander) return;

            MapUnitPosition mapUnitPosition = MapUnitPosition;
            MapUnitPosition scheduledPosition;
            if (NPCRef != null)
                scheduledPosition = NPCRef.Schedule.GetCharacterDefaultPositionByTime(timeOfDay);
            else if (TheSmallMapCharacterState != null)
                scheduledPosition = TheSmallMapCharacterState.TheMapUnitPosition;
            else
                // just in case neither of those exist, then they can just wander from where ever they
                // silly behaviour is better than a crash
                scheduledPosition = MapUnitPosition;

            // if there is no NPCRef, we may still wander - such as a horse 

            // i could get the size dynamically, but that's a waste of CPU cycles
            Point2D adjustedPosition = regularMap.GetWanderCharacterPosition(mapUnitPosition.XY, scheduledPosition.XY,
                nMaxDistance, out MapUnitMovement.MovementCommandDirection direction);

            // check to see if the random direction is within the correct distance
            if (direction != MapUnitMovement.MovementCommandDirection.None &&
                !scheduledPosition.XY.IsWithinN(adjustedPosition, nMaxDistance))
                throw new Ultima5ReduxException(
                    "GetWanderCharacterPosition has told us to go outside of our expected maximum area");
            // can we even travel onto the tile?
            if (!regularMap.IsTileFreeToTravelForAvatar(adjustedPosition, true))
            {
                if (direction != MapUnitMovement.MovementCommandDirection.None)
                    throw new Ultima5ReduxException("Was sent to a tile, but it isn't in free in WanderWithinN");
                // something else is on the tile, so we don't move
                return;
            }

            // add the single instruction to the queue
            Movement.AddNewMovementInstruction(new MovementCommand(direction, 1));
        }


        public virtual bool CanBeExited(RegularMap regularMap) => true;

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
            return GameReferences.Instance.SpriteTileReferences.GetTileReferenceByName(DirectionToTileName[Direction]);
        }

        public TileReference GetAnimatedTileReference()
        {
            // some things are not animated, so we just use the KeyTileReference every time
            if (!KeyTileReference.IsPartOfAnimation) return KeyTileReference;
            if (KeyTileReference.TotalAnimationFrames < 2) return KeyTileReference;

            UpdateAnimationIndex();
            return GameReferences.Instance.SpriteTileReferences.GetTileReference(
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
                UseFourDirections ? FourDirectionToTileNameBoarded : DirectionToTileNameBoarded;
            if (tileNameDictionary == null) return KeyTileReference;
            return GameReferences.Instance.SpriteTileReferences.GetTileReferenceByName(tileNameDictionary[Direction]);
        }

        public NonPlayerCharacterSchedule.AiType GetCurrentAiType(TimeOfDay tod) =>
            OverrideAiType ? OverridenAiType : NPCRef.Schedule.GetCharacterAiTypeByTime(tod);

        protected virtual bool CanMoveToDumb(Map map, Point2D mapUnitPosition) => false;

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
        /// <param name="regularMap"></param>
        protected void ProcessNextMoveTowardsAvatarAStar(RegularMap regularMap)
        {
            Map.WalkableType walkableType = regularMap.GetWalkableTypeByMapUnit(this);
            Point2D avatarPosition = regularMap.CurrentAvatarPosition.XY;

            const int noPath = 0xFFFF;

            Point2D positionToMoveTo = null;
            if (regularMap is not LargeMap)
                throw new Ultima5ReduxException("Cannot do aStar move towards Avatar on LargeMap");

            AStar aStar = regularMap.GetAStarMap(walkableType);
            // it's a small map so we can rely on the aStar to get us a decent path
            Stack<Node> theWay = aStar.FindPath(MapUnitPosition.XY, avatarPosition);

            if (theWay == null) return;

            int nMoves = theWay.Count;

            if (nMoves == noPath)
            {
                // we do a quick wander check
                // get the surrounding points around current active unit
                List<Point2D> surroundingPoints =
                    MapUnitPosition.XY.GetConstrainedFourDirectionSurroundingPointsWrapAround(
                        regularMap.NumOfXTiles - 1,
                        regularMap.NumOfYTiles - 1);

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

            // move to the new point
            MapUnitPosition.XY = positionToMoveTo;
        }

        protected void ProcessNextMoveTowardsMapUnitDumb(RegularMap regularMap, Point2D fromPosition,
            Point2D toPosition)
        {
            Point2D positionToMoveTo = null;

            // it IS a large map, so we do the less resource intense way of pathfinding
            positionToMoveTo =
                GetBestNextPositionToMoveTowardsWalkablePointDumb(regularMap, fromPosition, toPosition);

            if (positionToMoveTo == null)
            {
                // only a 50% chance they will wander
                if (Utils.Ran.Next() % 2 == 0) return;

                positionToMoveTo = GetValidRandomWanderPointDumb(regularMap, toPosition);
                if (positionToMoveTo == null) return;
            }

            MapUnitPosition.XY = positionToMoveTo;
        }
    }
}