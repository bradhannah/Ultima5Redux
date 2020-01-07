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

        public Point2D CurrentMapPosition { get; private set; } = new Point2D(0, 0);
        public int CurrentFloor { get; private set; } = 0;

        public void Move(Point2D xy, int nFloor)
        {
            CurrentMapPosition = xy;
            CurrentFloor = nFloor;
        }

        //TileReferences tileRefs, NonPlayerCharacterReferences npcRefs, DataChunk animationStatesDataChunk, DataChunk charStatesDataChunk,
        //DataChunk nonPlayerCharacterMovementLists, DataChunk NonPlayerCharacterMovementOffsets

        public MapCharacter(NonPlayerCharacterReference npcRef, MapCharacterAnimationState mapCharacterAnimationState, MapCharacterState mapCharacterState,
            NonPlayerCharacterMovement nonPlayerCharacterMovement)
        {
            NPCRef = npcRef;
            AnimationState = mapCharacterAnimationState;
            CharacterState = mapCharacterState;
            Movement = nonPlayerCharacterMovement;

            CurrentFloor = CharacterState == null? 0: CharacterState.Floor;
            
            if (CharacterState == null)
            {
                if (npcRef != null)
                {
                    MoveNPCToDefaultScheduledPosition();
                }
            }
            else
            {
                CurrentMapPosition = new Point2D(CharacterState.X, CharacterState.Y);
            }

            //Movement = new NonPlayerCharacterMovement(Reference.DialogIndex,)
        }

        /// <summary>
        /// Moves the NPC to the appropriate floor and location based on the their expected location and position
        /// </summary>
        internal void MoveNPCToDefaultScheduledPosition()
        {
            // todo: need to determine actual schedule time
            // we could even just ask the NPC where they should be at the time given
            int nIndex = 1;
            Point2D npcXy = NPCRef.Schedule.GetHardCoord(nIndex);

            // the NPC is a non-NPC, so we keep looking
            if (npcXy.X == 0 && npcXy.Y == 0) return;

            Move(npcXy, NPCRef.Schedule.GetFloor(nIndex));
        }

    }
}
