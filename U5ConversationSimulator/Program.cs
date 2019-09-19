using System;
using Ultima5Redux;
using System.Collections;
using System.Collections.Generic;

namespace U5ConversationSimulator
{
    class Program
    {
        static void Main(string[] args)
        {
            World world = new World("C:\\games\\ultima_5_late\\bucden4");
            //Dictionary<int, TileReference> tileReference = TileReference.Load();
            world.OverworldMap.PrintMap();
            world.SmallMapRef.GetLocationName(SmallMapReference.SingleMapReference.Location.Lord_Britishs_Castle);
        }
    }
}
