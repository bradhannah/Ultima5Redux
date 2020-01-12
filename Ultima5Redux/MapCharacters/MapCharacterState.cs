using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;

namespace Ultima5Redux
{
    public class MapCharacterState
    {
        //public int X { get; }
        //public int Y { get; }
        //public int Floor { get; }
        public CharacterPosition TheCharacterPosition { get; } = new CharacterPosition();
        public int CharacterAnimationStateIndex { get; }
        public int NPCIndex { get; }
        TileReference TileRef { get; }
        public bool Active { get; }

        public MapCharacterState(TileReferences tileReferences, NonPlayerCharacterReference npcRef, int nCharacterAnimationStateIndex, TimeOfDay timeOfDay)
        {
            NPCIndex = npcRef.DialogIndex;
            TileRef = tileReferences.GetTileReference(npcRef.NPCKeySprite);
            CharacterAnimationStateIndex = nCharacterAnimationStateIndex;
            // if you are adding by hand then we can assume that the character is active
            TheCharacterPosition = npcRef.Schedule.GetCharacterDefaultPositionByTime(timeOfDay);

            Active = true;
        }

        public MapCharacterState(TileReferences tileReferences, UInt16[] stateUInts, int nNPCIndex)
        {
            Debug.Assert(stateUInts.Length == 0x8);
            NPCIndex = nNPCIndex;
            TheCharacterPosition.X = stateUInts[1];
            TheCharacterPosition.Y = stateUInts[2];
            TheCharacterPosition.Floor = stateUInts[3];
            TileRef = tileReferences.GetTileReference(stateUInts[4]+0x100);
            CharacterAnimationStateIndex = stateUInts[6];
            Active = stateUInts[7]>0;
        }
    }
}
