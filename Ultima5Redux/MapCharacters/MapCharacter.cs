using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ultima5Redux
{
    public class MapCharacter
    {
        internal NonPlayerCharacterMovement Movement { get; }
        public NonPlayerCharacterReference NPCRef { get; }
        internal MapCharacterAnimationState AnimationState { get; }
        internal MapCharacterState CharacterState { get; }

        public CharacterPosition CurrentCharacterPosition = new CharacterPosition();


        public bool IsActive
        { 
            get
            {
                if (CharacterState != null)
                {
                    if (CharacterState.CharacterAnimationStateIndex == 0) return false;
                    return CharacterState.Active;
                }
                return false;
            }
        }

        #region Public Methods
     
        #endregion

        public MapCharacter()
        {
            NPCRef = null;
            AnimationState = null;
            CharacterState = null;
            Movement = null;
        }

        public MapCharacter(NonPlayerCharacterReference npcRef, MapCharacterAnimationState mapCharacterAnimationState, MapCharacterState mapCharacterState,
            NonPlayerCharacterMovement nonPlayerCharacterMovement, TimeOfDay timeOfDay)
        {
            NPCRef = npcRef;
            AnimationState = mapCharacterAnimationState;
            CharacterState = mapCharacterState;
            Movement = nonPlayerCharacterMovement;

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

        /// <summary>
        /// calculates and stores new path for NPC
        /// </summary>
        internal void CalculateNextPath()
        {
            
        }

    }
}
