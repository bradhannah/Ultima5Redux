using System.IO;

//using System.Drawing;

namespace Ultima5Redux.Maps
{
    public class GenericTileset
    {
        
        byte[] _tilesetByteArray;
        public GenericTileset(string fileNameAndPath)
        {
            _tilesetByteArray = File.ReadAllBytes(fileNameAndPath);

            Initialize(fileNameAndPath);
        }

        private void WriteBitmapRgb(int r, int g, int b)
        {

        }
        
        private void Initialize(string fileNameAndPath)
        {
            foreach (byte singleByte in _tilesetByteArray)
            {
                switch (singleByte)
                {
                    case 0://white
                        WriteBitmapRgb(0, 0, 0);
                        break;
                    case 1://dark blue
                        WriteBitmapRgb(160, 0, 0);
                        break;
                    case 2://dark green
                        WriteBitmapRgb(0, 160, 0);
                        break;
                    case 3://blue green
                        WriteBitmapRgb(160, 160, 0);
                        break;
                    case 4://maroon
                        WriteBitmapRgb(0, 0, 160);
                        break;
                    case 5://dark purple
                        WriteBitmapRgb(160, 0, 160);
                        break;
                    case 6://brown
                        WriteBitmapRgb(0, 80, 160);
                        break;
                    case 7://light grey
                        WriteBitmapRgb(160, 160, 160);
                        break;
                    case 8://dark grey
                        WriteBitmapRgb(80, 80, 80);
                        break;
                    case 9://blue
                        WriteBitmapRgb(255, 0, 0);
                        break;
                    case 10://light green
                        WriteBitmapRgb(80, 255, 80);
                        break;
                    case 11://light blue
                        WriteBitmapRgb(255, 255, 80);
                        break;
                    case 12://light red
                        WriteBitmapRgb(80, 80, 255);
                        break;
                    case 13://light purple
                        WriteBitmapRgb(255, 80, 255);
                        break;
                    case 14://yellow
                        WriteBitmapRgb(80, 255, 255);
                        break;
                    case 15://black
                        WriteBitmapRgb(255, 255, 255);
                        break;
                    default:
                        WriteBitmapRgb(255, 255, 255);
                        //printf("Got code %d that I wasn't expecting\n", pic);

                        break;
                }
            }
        }

    }
}
