using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Drawing;

namespace Ultima5Redux
{
    class GenericTileset
    {
        
        byte[] tilesetByteArray;
        public GenericTileset(string fileNameAndPath)
        {
            tilesetByteArray = File.ReadAllBytes(fileNameAndPath);

            Initialize(fileNameAndPath);
        }

        private void WriteBitmapRGB(int R, int G, int B)
        {

        }
        
        private void Initialize(string fileNameAndPath)
        {
            foreach (byte singleByte in tilesetByteArray)
            {
                switch (singleByte)
                {
                    case 0://white
                        WriteBitmapRGB(0, 0, 0);
                        break;
                    case 1://dark blue
                        WriteBitmapRGB(160, 0, 0);
                        break;
                    case 2://dark green
                        WriteBitmapRGB(0, 160, 0);
                        break;
                    case 3://blue green
                        WriteBitmapRGB(160, 160, 0);
                        break;
                    case 4://maroon
                        WriteBitmapRGB(0, 0, 160);
                        break;
                    case 5://dark purple
                        WriteBitmapRGB(160, 0, 160);
                        break;
                    case 6://brown
                        WriteBitmapRGB(0, 80, 160);
                        break;
                    case 7://light grey
                        WriteBitmapRGB(160, 160, 160);
                        break;
                    case 8://dark grey
                        WriteBitmapRGB(80, 80, 80);
                        break;
                    case 9://blue
                        WriteBitmapRGB(255, 0, 0);
                        break;
                    case 10://light green
                        WriteBitmapRGB(80, 255, 80);
                        break;
                    case 11://light blue
                        WriteBitmapRGB(255, 255, 80);
                        break;
                    case 12://light red
                        WriteBitmapRGB(80, 80, 255);
                        break;
                    case 13://light purple
                        WriteBitmapRGB(255, 80, 255);
                        break;
                    case 14://yellow
                        WriteBitmapRGB(80, 255, 255);
                        break;
                    case 15://black
                        WriteBitmapRGB(255, 255, 255);
                        break;
                    default:
                        WriteBitmapRGB(255, 255, 255);
                        //printf("Got code %d that I wasn't expecting\n", pic);

                        break;
                }
            }
        }

    }
}
