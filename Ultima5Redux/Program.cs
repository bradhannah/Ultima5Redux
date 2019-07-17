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
            World world = new World("/Users/bradhannah/Documents/Ultima_5");
            //World world = new World("C:\\games\\ultima_5\\gold");

            //world.overworldMap.PrintMap();
            //world.underworldMap.PrintMap();

            //Console.ReadKey();
        }
    }
}
