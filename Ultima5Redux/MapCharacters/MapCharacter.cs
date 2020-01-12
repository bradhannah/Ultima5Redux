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

        //public Point2D CurrentMapPosition { get; private set; } = new Point2D(0, 0);
        //public int CurrentFloor { get; private set; } = 0;

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

        public void Move(Point2D xy, int nFloor)
        {
            CurrentCharacterPosition.XY = xy;
            CurrentCharacterPosition.Floor = nFloor;
        }

        public void Move(CharacterPosition characterPosition)//Point2D xy, int nFloor)
        {
            CurrentCharacterPosition = characterPosition;
        }

        //TileReferences tileRefs, NonPlayerCharacterReferences npcRefs, DataChunk animationStatesDataChunk, DataChunk charStatesDataChunk,
        //DataChunk nonPlayerCharacterMovementLists, DataChunk NonPlayerCharacterMovementOffsets

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

            //Movement = new NonPlayerCharacterMovement(Reference.DialogIndex,)
        }

        /// <summary>
        /// Moves the NPC to the appropriate floor and location based on the their expected location and position
        /// </summary>
        internal void MoveNPCToDefaultScheduledPosition(TimeOfDay timeOfDay)
        {
            // todo: need to determine actual schedule time
            // we could even just ask the NPC where they should be at the time given
            //int nIndex = 1;
            //Point2D npcXy = NPCRef.Schedule.GetHardCoord(nIndex);

            CharacterPosition npcXy = NPCRef.Schedule.GetCharacterDefaultPositionByTime(timeOfDay);

            // the NPC is a non-NPC, so we keep looking
            if (npcXy.X == 0 && npcXy.Y == 0) return;

            Move(npcXy);// npcXy, NPCRef.Schedule.GetFloor(nIndex));
        }

    }
}
