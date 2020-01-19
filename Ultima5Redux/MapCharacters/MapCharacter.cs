using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ultima5Redux
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
        #endregion

        #region Public Properties
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

        public bool IsActive
        { 
            get
            {
                if (IsInParty) return false;
                if (CharacterState != null)
                {
                    if (CharacterState.CharacterAnimationStateIndex == 0) return false;
                    return CharacterState.Active;
                }
                return false;
            }
        }
        #endregion

        #region Constructors
        /// <summary>
        /// emtpy constructor if there is nothing in the map character slot
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
        public MapCharacter(NonPlayerCharacterReference npcRef, MapCharacterAnimationState mapCharacterAnimationState, MapCharacterState mapCharacterState,
            NonPlayerCharacterMovement nonPlayerCharacterMovement, TimeOfDay timeOfDay, PlayerCharacterRecords playerCharacterRecords)
        {
            NPCRef = npcRef;
            AnimationState = mapCharacterAnimationState;
            CharacterState = mapCharacterState;
            Movement = nonPlayerCharacterMovement;
            PlayerCharacterRecord record = playerCharacterRecords.GetCharacterRecordByNPC(npcRef);
            IsInParty = record==null?false:record.PartyStatus == PlayerCharacterRecord.CharacterPartyStatus.InParty;

            CurrentCharacterPosition.Floor = CharacterState == null? 0: CharacterState.TheCharacterPosition.Floor;
            
            if (CharacterState == null)
            {
                if (npcRef != null)
                {
                    MoveNPCToDefaultScheduledPosition(timeOfDay);
                }
            }
            else
            {
                CurrentCharacterPosition.XY = new Point2D(CharacterState.TheCharacterPosition.X, CharacterState.TheCharacterPosition.Y);
            }
        }
        #endregion

        /// <summary>
        /// Moves the NPC to the appropriate floor and location based on the their expected location and position
        /// </summary>
        internal void MoveNPCToDefaultScheduledPosition(TimeOfDay timeOfDay)
        {
            CharacterPosition npcXy = NPCRef.Schedule.GetCharacterDefaultPositionByTime(timeOfDay);

            // the NPC is a non-NPC, so we keep looking
            if (npcXy.X == 0 && npcXy.Y == 0) return;

            Move(npcXy);
        }

        /// <summary>
        /// move the character to a new position
        /// </summary>
        /// <param name="xy"></param>
        /// <param name="nFloor"></param>
        internal void Move(Point2D xy, int nFloor)
        {
            CurrentCharacterPosition.XY = xy;
            CurrentCharacterPosition.Floor = nFloor;
        }

        /// <summary>
        /// Move the character to a new positon
        /// </summary>
        /// <param name="characterPosition"></param>
        internal void Move(CharacterPosition characterPosition)
        {
            CurrentCharacterPosition = characterPosition;
        }
  
    }
}
