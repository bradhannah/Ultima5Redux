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
        public int X { get; }
        public int Y { get; }
        public int Floor { get; }
        public int CharacterAnimationStateIndex { get; }
        public int NPCIndex { get; }
        TileReference TileRef { get; }
        public bool Active { get; }

        public MapCharacterState(TileReferences tileReferences, UInt16[] stateUInts, int nNPCIndex)
        {
            Debug.Assert(stateUInts.Length == 0x8);
            NPCIndex = nNPCIndex;
            X = stateUInts[1];
            Y = stateUInts[2];
            Floor = stateUInts[3];
            TileRef = tileReferences.GetTileReference(stateUInts[4]+0x100);
            CharacterAnimationStateIndex = stateUInts[6];
            Active = stateUInts[7]>0;
        }
    }
}
