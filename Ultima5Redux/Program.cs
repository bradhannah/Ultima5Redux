using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;

namespace Ultima5Redux
{
    class Program
    {
 

        static void Main(string[] args)
        {
            World world = new World("C:\\games\\ultima_5");

            //world.overworldMap.PrintMap();
            //world.underworldMap.PrintMap();

            Console.ReadKey();
        }
    }
}
