using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.Serialization;
using Newtonsoft.Json;
using Ultima5Redux.DayNightMoon;
using Ultima5Redux.External;
using Ultima5Redux.Maps;
using Ultima5Redux.PlayerCharacters;
using Ultima5Redux.References;
using Ultima5Redux.References.Maps;
using Ultima5Redux.References.MapUnits.NonPlayerCharacters;

namespace Ultima5Redux.MapUnits.NonPlayerCharacters
{
    [DataContract] public sealed class NonPlayerCharacter : MapUnit
    {
        [DataMember(Name = "PlayerCharacterRecordIndex")]
        private int _playerCharacterRecordIndex;

        [DataMember(Name = "ScheduleIndex")] private int _scheduleIndex = -1;
        [DataMember] public bool ArrivedAtLocation { get; private set; }

        [IgnoreDataMember] public override Avatar.AvatarState BoardedAvatarState => Avatar.AvatarState.Hidden;

        [IgnoreDataMember] public override string BoardXitName => "Board them? You hardly know them!";

        [IgnoreDataMember] public override string FriendlyName => NPCRef.FriendlyName;

        /// <summary>
        ///     Is the map character currently an active character on the current map
        /// </summary>
        [IgnoreDataMember]
        public override bool IsActive
        {
            get
            {
                // if they are in our party then we don't include them in the map 
                if (IsInParty) return false;

                if (NPCState.IsDead) return false;

                // if they are in 0,0 then I am certain they are not real
                if (MapUnitPosition.X == 0 && MapUnitPosition.Y == 0) return false;

                // if there is a small map character state then we prefer to use it to determine if the 
                // unit is active
                Debug.Assert(TheSmallMapCharacterState != null);
                //if (TheSmallMapCharacterState.MapUnitAnimationStateIndex != 0) return TheSmallMapCharacterState.Active;

                return true;
            }
        }

        [IgnoreDataMember] public override bool IsAttackable => true;

        [IgnoreDataMember] public override TileReference NonBoardedTileReference => KeyTileReference;

        [IgnoreDataMember]
        protected override Dictionary<Point2D.Direction, string> DirectionToTileName { get; } = new();

        [IgnoreDataMember]
        protected override Dictionary<Point2D.Direction, string> DirectionToTileNameBoarded { get; } =
            new();

        [JsonConstructor] public NonPlayerCharacter()
        {
        }

        public NonPlayerCharacter(SmallMapCharacterState smallMapTheSmallMapCharacterState,
            MapUnitMovement mapUnitMovement, bool bLoadedFromDisk,
            SmallMapReferences.SingleMapReference.Location location, MapUnitPosition mapUnitPosition,
            NonPlayerCharacterState npcState) : base(smallMapTheSmallMapCharacterState, mapUnitMovement, location,
            Point2D.Direction.None, npcState,
            GameReferences.SpriteTileReferences.GetTileReference(npcState.NPCRef.NPCKeySprite), mapUnitPosition)
        {
            NPCState = npcState;
            bool bLargeMap = TheSmallMapCharacterState == null && NPCState.NPCRef == null;

            PlayerCharacterRecord record = null;

            // gets the player character record for an NPC if one exists
            // this is commonly used when meeting NPCs who have not yet joined your party 
            if (NPCState.NPCRef != null)
                record = GameStateReference.State.CharacterRecords.GetCharacterRecordByNPC(NPCState.NPCRef);

            _playerCharacterRecordIndex =
                GameStateReference.State.CharacterRecords.GetCharacterIndexByNPC(NPCState.NPCRef);

            // is the NPC you are loading currently in the party?
            IsInParty = record != null && record.PartyStatus == PlayerCharacterRecord.CharacterPartyStatus.InTheParty;

            // it's a large map so we follow different logic to determine the placement of the character
            if (bLargeMap)
            {
                Move(MapUnitPosition);
            }
            else
            {
                // there is no TheSmallMapCharacterState which indicates that it is a large map
                if (!bLoadedFromDisk)
                {
                    if (NPCState.NPCRef != null)
                        MoveNpcToDefaultScheduledPosition(GameStateReference.State.TheTimeOfDay);
                }
                else
                {
                    Move(MapUnitPosition);
                }
            }
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

        private void Move(Point2D xy, int nFloor, TimeOfDay tod)
        {
            base.Move(xy, nFloor);
            UpdateScheduleTracking(tod);
        }

        private void Move(MapUnitPosition mapUnitPosition, TimeOfDay tod, bool bIsLargeMap)
        {
            base.Move(mapUnitPosition);
            if (!bIsLargeMap) UpdateScheduleTracking(tod);
        }

        /// <summary>
        ///     Moves the NPC to the appropriate floor and location based on the their expected location and position
        /// </summary>
        private void MoveNpcToDefaultScheduledPosition(TimeOfDay timeOfDay)
        {
            MapUnitPosition npcXy = NPCRef.Schedule.GetCharacterDefaultPositionByTime(timeOfDay);

            // the NPC is a non-NPC, so we keep looking
            if (npcXy.X == 0 && npcXy.Y == 0) return;

            Move(npcXy, timeOfDay, false);
        }

        private void UpdateScheduleTracking(TimeOfDay tod)
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
        private void WanderWithinN(VirtualMap virtualMap, TimeOfDay timeOfDay, int nMaxDistance,
            bool bForceWander = false)
        {
            Random ran = new();

            // 50% of the time we won't even try to move at all
            int nRan = ran.Next(2);
            if (nRan == 0 && !bForceWander) return;

            MapUnitPosition mapUnitPosition = MapUnitPosition;
            MapUnitPosition scheduledPosition = NPCRef.Schedule.GetCharacterDefaultPositionByTime(timeOfDay);

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

        public override void CompleteNextMove(VirtualMap virtualMap, TimeOfDay timeOfDay, AStar aStar)
        {
            if (virtualMap.CurrentSingleMapReference == null)
                throw new Ultima5ReduxException("No single map is set in virtual map");

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
            //TileReference adjustedTile = GetTileReference(adjustedPos);
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

        // ReSharper disable once UnusedMember.Global
        public override string GetDebugDescription(TimeOfDay timeOfDay)
        {
            if (NPCRef != null)
                return "Name=" + NPCRef.FriendlyName + " " + MapUnitPosition + " Scheduled to be at: " +
                       NPCRef.Schedule.GetCharacterDefaultPositionByTime(timeOfDay) + " with AI Mode: " +
                       NPCRef.Schedule.GetCharacterAiTypeByTime(timeOfDay) + " <b>Movement Attempts</b>: " +
                       MovementAttempts + " " + Movement;

            return "MapUnit " + KeyTileReference.Description + " " + MapUnitPosition + " Scheduled to be at: " +
                   " <b>Movement Attempts</b>: " + MovementAttempts + " " + Movement;
        }

        /// <summary>
        ///     calculates and stores new path for NPC
        ///     Placed outside into the VirtualMap since it will need information from the active map, VMap and the MapUnit itself
        /// </summary>
        public void CalculateNextPath(VirtualMap virtualMap, TimeOfDay timeOfDay, int nMapCurrentFloor, AStar aStar)
        {
            MapUnitPosition npcXy = NPCRef.Schedule.GetCharacterDefaultPositionByTime(timeOfDay);

            // a little hacky - if they are dead then we just place them at 0,0 which is understood to be the 
            // location for NPCs that aren't present on the map
            if (NPCState.IsDead)
            {
                if (npcXy.X != 0 || npcXy.Y != 0)
                {
                    npcXy.X = 0;
                    npcXy.Y = 0;
                    Move(npcXy, timeOfDay, false);
                }
            }

            // the NPC is a non-NPC, so we keep looking
            if (npcXy.X == 0 && npcXy.Y == 0) return;

            // if the NPC is destined for the floor you are on, but are on a different floor, then they need to find a ladder or staircase

            // if the NPC is destined for a different floor then we watch to see if they are on stairs on a ladder
            bool bDifferentFloor = npcXy.Floor != MapUnitPosition.Floor;
            if (bDifferentFloor)
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

            NonPlayerCharacterSchedule.AiType aiType =
                NPCRef.Schedule.GetCharacterAiTypeByTime(timeOfDay);

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
                    default:
                        throw new Ultima5ReduxException(
                            $"An unexpected movement AI was encountered: {aiType} for NPC: {NPCRef.Name}");
                }
            else // character not in correct position
                switch (aiType)
                {
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
    }
}