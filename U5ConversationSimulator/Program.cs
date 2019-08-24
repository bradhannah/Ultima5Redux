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
            List<TileReference> tileReference = TileReference.Load();

        }
    }
}
