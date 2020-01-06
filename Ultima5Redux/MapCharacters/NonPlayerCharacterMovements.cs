using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ultima5Redux
{
    public class NonPlayerCharacterMovements
    {
        private const int MAX_PLAYERS = 0x020;

        private List<NonPlayerCharacterMovement> movementList = new List<NonPlayerCharacterMovement>(MAX_PLAYERS);
        private DataChunk movementInstructionDataChunk;
        private DataChunk movementOffsetDataChunk;


        public NonPlayerCharacterMovements(DataChunk movementInstructionDataChunk, DataChunk movementOffsetDataChunk)
        {
            this.movementInstructionDataChunk = movementInstructionDataChunk;
            this.movementOffsetDataChunk = movementOffsetDataChunk;
            for (int i = 0; i < MAX_PLAYERS; i++)
            {
                movementList.Add(new NonPlayerCharacterMovement(i, movementInstructionDataChunk, movementOffsetDataChunk));
            }
        }

        public NonPlayerCharacterMovement GetMovement(int nIndex)
        {
            return movementList[nIndex];
        }
        //        if (NPCMovement.IsNextCommandAvailable())
        //        {

        //        }
        //        else
        //        {
        //            // there is no special movement instructions - so they are where they are expected to be
        //            MoveNPCToDefaultScheduledPosition();
        //}


        //NPCMovement = new NonPlayerCharacterMo vement(dialogIndex, gameStateRef.NonPlayerCharacterMovementLists, gameStateRef.NonPlayerCharacterMovementOffsets);
    }
}
