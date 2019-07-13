using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ultima5Redux
{
    class GameState
    {
        private Random ran = new Random();

        public string AvatarsName { get { return "Fred"; } }

        public bool OneInXOdds(int howMany)
        {
            // if ran%howMany is zero then we beat the odds
            int nextRan = ran.Next();
            return ((nextRan % howMany) == 0);
        }



        public bool NpcHasMetAvatar(NonPlayerCharacters.NonPlayerCharacter npc)
        {
            return false;
        }

    }
}
