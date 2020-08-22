using System.Diagnostics;
using Ultima5Redux.DayNightMoon;
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
        internal MapCharacterAnimationState AnimationState { get; }
        /// <summary>
        /// The location state of the character
        /// </summary>
        internal MapCharacterState CharacterState { get; }
        /// <summary>
        /// The characters current position on the map
        /// </summary>
        internal CharacterPosition CurrentCharacterPosition { get; private set; }  = new CharacterPosition();
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
        /// Builds a MpaCharacter from precreated objects - typically loaded from disk in advance
        /// </summary>
        /// <param name="npcRef"></param>
        /// <param name="mapCharacterAnimationState"></param>
        /// <param name="mapCharacterState"></param>
        /// <param name="nonPlayerCharacterMovement"></param>
        /// <param name="timeOfDay"></param>
        /// <param name="playerCharacterRecords"></param>
        public MapCharacter(NonPlayerCharacterReference npcRef, MapCharacterAnimationState mapCharacterAnimationState, MapCharacterState mapCharacterState,
            NonPlayerCharacterMovement nonPlayerCharacterMovement, TimeOfDay timeOfDay, PlayerCharacterRecords playerCharacterRecords)
        {
            NPCRef = npcRef;
            AnimationState = mapCharacterAnimationState;
            CharacterState = mapCharacterState;
            
            Movement = nonPlayerCharacterMovement;
            PlayerCharacterRecord record = null;
            if (playerCharacterRecords != null && npcRef != null)
            {
                //Debug.Assert(npcRef != null);
                record = playerCharacterRecords.GetCharacterRecordByNPC(npcRef);
            }

            IsInParty = record != null && record.PartyStatus == PlayerCharacterRecord.CharacterPartyStatus.InParty;

            CurrentCharacterPosition.Floor = CharacterState?.TheCharacterPosition.Floor ?? 0;
            
            if (CharacterState == null)
            {
                if (npcRef != null)
                {
                    MoveNPCToDefaultScheduledPosition(timeOfDay);
                }
            }
            else
            {
                // todo: temporary until I have monsters implemented
                if (npcRef == null)
                    return;
                Move(new Point2D(CharacterState.TheCharacterPosition.X, CharacterState.TheCharacterPosition.Y), CurrentCharacterPosition.Floor , timeOfDay);
                //CurrentCharacterPosition.XY = new Point2D(CharacterState.TheCharacterPosition.X, CharacterState.TheCharacterPosition.Y);
            }
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

            Move(npcXy, tod);
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

        /// <summary>
        /// Move the character to a new position
        /// </summary>
        /// <param name="characterPosition"></param>
        /// <param name="tod"></param>
        internal void Move(CharacterPosition characterPosition, TimeOfDay tod)
        {
            CurrentCharacterPosition = characterPosition;
            UpdateScheduleTracking(tod);
        }
        #endregion
    }
}
