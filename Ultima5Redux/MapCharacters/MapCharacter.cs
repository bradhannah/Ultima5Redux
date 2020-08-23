using System;
using System.Diagnostics;
using Ultima5Redux.DayNightMoon;
using Ultima5Redux.Maps;
using Ultima5Redux.PlayerCharacters;

namespace Ultima5Redux.MapCharacters
{
    public class MapCharacter
    {
        #region Private and Internal Properties 
        /// <summary>
        /// All the movements for the map character
        /// </summary>
        internal NonPlayerCharacterMovement Movement { get; }
        /// <summary>
        /// the state of the animations
        /// </summary>
        internal MapCharacterAnimationState AnimationState { get; private set; }

        /// <summary>
        /// The location state of the character
        /// </summary>
        internal MapCharacterState CharacterState { get; }

        private readonly CharacterPosition _characterPosition = new CharacterPosition();
        
        /// <summary>
        /// The characters current position on the map
        /// </summary>
        internal CharacterPosition CurrentCharacterPosition
        {
            get => _characterPosition;
            //return new CharacterPosition(AnimationState.X, AnimationState.Y, AnimationState.Floor);
            private set
            {
                if (value == null) throw new ArgumentNullException(nameof(value));
                // this is a bit redundant but we have a backing field and also store the XY positions
                // in the AnimationState and CharacterState
                _characterPosition.X = value.X;
                _characterPosition.Y = value.Y;
                _characterPosition.Floor = value.Floor;
                
                AnimationState.X = (byte) value.X;
                AnimationState.Y = (byte) value.Y;
                AnimationState.Floor = (byte) value.Floor;

                if (CharacterState == null) return;
                CharacterState.TheCharacterPosition.X = value.X;
                CharacterState.TheCharacterPosition.Y = value.Y;
                CharacterState.TheCharacterPosition.Floor = value.Floor;
            }
        }

        /// <summary>
        /// How many iterations will I force the character to wander?
        /// </summary>
        internal int ForcedWandering { get; set; }
        
        public bool ArrivedAtLocation { get; private set; }
        
        private int _scheduleIndex = -1;
        #endregion

        #region Public Properties
        public int MovementAttempts = 0;
        /// <summary>
        /// Reference to current NPC (if it's an NPC at all!)
        /// </summary>
        public NonPlayerCharacterReference NPCRef { get; }
        /// <summary>
        /// Is the character currently active on the map?
        /// </summary>
        
        public bool IsInParty
        {
            get; set; 
        }

        /// <summary>
        /// Is the map character currently an active character on the current map
        /// </summary>
        public bool IsActive
        { 
            get
            {
                // if they are in our party then we don't include them in the map 
                if (IsInParty) return false;

                // if they are in 0,0 then I am certain they are not real
                if (CurrentCharacterPosition.X == 0 && CurrentCharacterPosition.Y == 0) return false;

                if (CharacterState == null) return false;
                if (CharacterState.CharacterAnimationStateIndex != 0) return CharacterState.Active;
                return false;
            }
        }

        public string GetDebugDescription(TimeOfDay timeOfDay)
        { 
            string debugLookStr = ("Name=" + NPCRef.FriendlyName 
                + " " + CurrentCharacterPosition + " Scheduled to be at: "+
                NPCRef.Schedule.GetCharacterDefaultPositionByTime(timeOfDay) + " with AI Mode: "+ 
                NPCRef.Schedule.GetCharacterAiTypeByTime(timeOfDay) +
                " <b>Movement Attempts</b>: "+ MovementAttempts + " " + 
                this.Movement.ToString());
            
            return debugLookStr;
        }

        #endregion

        #region Constructors
        /// <summary>
        /// empty constructor if there is nothing in the map character slot
        /// </summary>
        public MapCharacter()
        {
            NPCRef = null;
            AnimationState = null;
            CharacterState = null;
            Movement = null;
        }

        /// <summary>
        /// Builds a MpaCharacter from pre-instantiated objects - typically loaded from disk in advance
        /// </summary>
        /// <param name="npcRef"></param>
        /// <param name="mapCharacterAnimationState"></param>
        /// <param name="mapCharacterState"></param>
        /// <param name="nonPlayerCharacterMovement"></param>
        /// <param name="timeOfDay"></param>
        /// <param name="playerCharacterRecords"></param>
        /// <param name="bLoadedFromDisk"></param>
        public MapCharacter(NonPlayerCharacterReference npcRef, MapCharacterAnimationState mapCharacterAnimationState, MapCharacterState mapCharacterState,
            NonPlayerCharacterMovement nonPlayerCharacterMovement, TimeOfDay timeOfDay, PlayerCharacterRecords playerCharacterRecords, bool bLoadedFromDisk)
        {
            NPCRef = npcRef;
            AnimationState = mapCharacterAnimationState;
            CharacterState = mapCharacterState;
            Movement = nonPlayerCharacterMovement;

            PlayerCharacterRecord record = null;

            bool bLargeMap = CharacterState == null && npcRef == null;
            
            Debug.Assert(playerCharacterRecords != null);
            Debug.Assert(AnimationState != null);
            Debug.Assert(Movement != null);
            
            // gets the player character record for an NPC if one exists
            // this is commonly used when meeting NPCs who have not yet joined your party 
            if (npcRef != null)
            {
                record = playerCharacterRecords.GetCharacterRecordByNPC(npcRef);
            }

            // is the NPC you are loading currently in the party?
            IsInParty = record != null && record.PartyStatus == PlayerCharacterRecord.CharacterPartyStatus.InParty;

            // set the characters position 
            CurrentCharacterPosition = new CharacterPosition(AnimationState.X, AnimationState.Y, AnimationState.Floor);
            
            //CurrentCharacterPosition.Floor = AnimationState.Floor;
            // CurrentCharacterPosition.Floor = CharacterState?.TheCharacterPosition.Floor ?? 0;

            // it's a large map so we follow different logic to determine the placement of the character
            if (bLargeMap)
            {
                Move(CurrentCharacterPosition, null, true);
            }
            else
            {
                // there is no CharacterState which indicates that it is a large map
                if (!bLoadedFromDisk)
                {
                    if (npcRef != null)
                    {
                        MoveNPCToDefaultScheduledPosition(timeOfDay);
                    }
                }
                else
                {
                    Move(CurrentCharacterPosition, timeOfDay, false);
                }
            }
        }

        /// <summary>
        /// Creates an Avatar MapCharacter at the default small map position
        /// Note: this should never need to be called from a LargeMap since the values persist on disk
        /// </summary>
        /// <param name="tileReferences"></param>
        /// <param name="location"></param>
        /// <returns></returns>
        public static MapCharacter CreateAvatar(TileReferences tileReferences, 
            SmallMapReferences.SingleMapReference.Location location)
        {
            MapCharacter theAvatar = new MapCharacter
            {
                AnimationState = MapCharacterAnimationState.CreateAvatar(tileReferences,
                    SmallMapReferences.GetStartingXYZByLocation(location))
            };

            return theAvatar;
        }
        #endregion

        #region Internal Methods
        /// <summary>
        /// Moves the NPC to the appropriate floor and location based on the their expected location and position
        /// </summary>
        internal void MoveNPCToDefaultScheduledPosition(TimeOfDay tod)
        {
            CharacterPosition npcXy = NPCRef.Schedule.GetCharacterDefaultPositionByTime(tod);

            // the NPC is a non-NPC, so we keep looking
            if (npcXy.X == 0 && npcXy.Y == 0) return;

            Move(npcXy, tod, false);
        }

        /// <summary>
        /// move the character to a new position
        /// </summary>
        /// <param name="xy"></param>
        /// <param name="nFloor"></param>
        /// <param name="tod"></param>
        internal void Move(Point2D xy, int nFloor, TimeOfDay tod)
        {
            CurrentCharacterPosition.XY = xy;
            CurrentCharacterPosition.Floor = nFloor;

            UpdateScheduleTracking(tod);
        }

        /// <summary>
        /// Move the character to a new position
        /// </summary>
        /// <param name="characterPosition"></param>
        /// <param name="tod"></param>
        /// <param name="bIsLargeMap"></param>
        internal void Move(CharacterPosition characterPosition, TimeOfDay tod, bool bIsLargeMap)
        {
            CurrentCharacterPosition = characterPosition;
            if (!bIsLargeMap)
            {
                UpdateScheduleTracking(tod);
            }
        }

        private void UpdateScheduleTracking(TimeOfDay tod)
        {
            if (CurrentCharacterPosition == NPCRef.Schedule.GetCharacterDefaultPositionByTime(tod))
            {
                ArrivedAtLocation = true;
            }

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

      
        #endregion
    }
}
