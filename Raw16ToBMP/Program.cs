using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Raw16ToBMP
{
    static class BitmapWriter
    {
        static public Dictionary<byte, Color> GetEGAPalette2()
        { 
            Dictionary<byte, Color> palette = new Dictionary<byte, Color>();
            palette.Add(0, Color.Black);
            palette.Add(0xFF, Color.FromArgb(0xFF, 0xFF, 0xFF)); // bright white

            palette.Add(64, Color.Red);
            palette.Add(12, Color.FromArgb(0xFF, 0x55, 0x55)); // bright red


            palette.Add(1, Color.Blue);
            palette.Add(2, Color.Green);
            palette.Add(3, Color.Cyan);
            palette.Add(5, Color.Magenta);
            palette.Add(6, Color.Brown);
            palette.Add(7, Color.LightGray);
            palette.Add(8, Color.DarkGray);
            palette.Add(9, Color.FromArgb(0x55, 0x55, 0xFF)); // bright blue
            palette.Add(10, Color.FromArgb(0x55, 0xFF, 0x55)); // bright green
            palette.Add(11, Color.FromArgb(0x55, 0xFF, 0x55)); // bright cyan
            palette.Add(13, Color.FromArgb(0xFF, 0xFF, 0x55)); // bright magenta
            palette.Add(14, Color.FromArgb(0xFF, 0x55, 0xFF)); // bright yellow

            return palette;
        }

    static public Dictionary<byte, Color> GetEGAPalette()
        {
            Dictionary<byte, Color> palette=new Dictionary<byte, Color>();
            palette.Add(0, Color.Black);
            palette.Add(1, Color.Blue);
            palette.Add(2, Color.Green);
            palette.Add(3, Color.Cyan);
            palette.Add(4, Color.Red);
            palette.Add(5, Color.Magenta);
            palette.Add(6, Color.Brown);
            palette.Add(7, Color.LightGray);
            palette.Add(8, Color.DarkGray);
            //palette.Add(56, Color.DarkGray);
            //palette.Add(57, Color.FromArgb(0x55, 0x55, 0xFF)); // bright blue
            //palette.Add(58, Color.FromArgb(0x55, 0xFF, 0x55)); // bright green
            //palette.Add(59, Color.FromArgb(0x55, 0xFF, 0x55)); // bright cyan
            //palette.Add(60, Color.FromArgb(0xFF, 0x55, 0x55)); // bright red
            //palette.Add(61, Color.FromArgb(0xFF, 0xFF, 0x55)); // bright magenta
            //palette.Add(62, Color.FromArgb(0xFF, 0x55, 0xFF)); // bright yellow
            //palette.Add(63, Color.FromArgb(0xFF, 0xFF, 0xFF)); // bright white
            palette.Add(9, Color.FromArgb(0x55, 0x55, 0xFF)); // bright blue
            palette.Add(10, Color.FromArgb(0x55, 0xFF, 0x55)); // bright green
            palette.Add(11, Color.FromArgb(0x55, 0xFF, 0x55)); // bright cyan
            palette.Add(12, Color.FromArgb(0xFF, 0x55, 0x55)); // bright red
            palette.Add(13, Color.FromArgb(0xFF, 0xFF, 0x55)); // bright magenta
            palette.Add(14, Color.FromArgb(0xFF, 0x55, 0xFF)); // bright yellow
            palette.Add(15, Color.FromArgb(0xFF, 0xFF, 0xFF)); // bright white

            return palette;
        }



        public static int Write16BitmapFile(string filename, int width, int height, byte[] imageData)
        {
        

            using (var stream = new MemoryStream(imageData))
            using (var bmp = new Bitmap(width, height, PixelFormat.Format24bppRgb))
            // .Format8bppIndexed))// Format32bppArgb))
            {
                BitmapData bmpData = bmp.LockBits(new Rectangle(0, 0,
                                                                bmp.Width,
                                                                bmp.Height), 
                                                                ImageLockMode.ReadWrite,
                                                  //ImageLockMode.WriteOnly,
                                                  bmp.PixelFormat);

                //IntPtr pNative = bmpData.Scan0;
                //Marshal.Copy(imageData, 0, pNative, imageData.Length-1);

                Dictionary<byte, Color> egaPalette = GetEGAPalette();

                bmp.UnlockBits(bmpData);

                int index = 0;
                for (int y = 0; y < height; y++)
                {
                    for (int x = 0; x < width; x += 2)
                    {
                        Color pixel1 = egaPalette[(byte)(imageData[index] >> 4 & 0xF)];
                        Color pixel2 = egaPalette[(byte)(imageData[index] & 0xF)];

                        bmp.SetPixel(x, y, pixel1);
                        bmp.SetPixel(x+1, y, pixel2);

                        index++;
                    }
                }

                //bmp.UnlockBits(bmpData);

                bmp.Save(filename);
            }

            return 1;
        }

        public static int WriteBitmapFile(string filename, int width, int height, byte[] imageData)
        {
            byte[] newData = new byte[imageData.Length];

            for (int x = 0; x < imageData.Length; x += 4)
            {
                byte[] pixel = new byte[4];
                Array.Copy(imageData, x, pixel, 0, 4);

                byte r = pixel[0];
                byte g = pixel[1];
                byte b = pixel[2];
                byte a = pixel[3];

                byte[] newPixel = new byte[] { b, g, r, a };

                Array.Copy(newPixel, 0, newData, x, 4);
            }

            imageData = newData;

            using (var stream = new MemoryStream(imageData))
            using (var bmp = new Bitmap(width, height, PixelFormat.Format32bppArgb))
            {
                BitmapData bmpData = bmp.LockBits(new Rectangle(0, 0,
                                                                bmp.Width,
                                                                bmp.Height),
                                                  ImageLockMode.WriteOnly,
                                                  bmp.PixelFormat);

                IntPtr pNative = bmpData.Scan0;
                Marshal.Copy(imageData, 0, pNative, imageData.Length);

                bmp.UnlockBits(bmpData);

                bmp.Save(filename);
            }

            return 1;
        }
    }


    class Program
    {
 
        static void Main(string[] args)
        {
            const int INDEX_SIZE = 0x32;
            byte[] fileArray = File.ReadAllBytes("C:\\games\\ultima_5\\temp\\dec_res\\create.16.uncomp");

            byte[] positionArray = new byte[INDEX_SIZE];
            for (int i = 0; i < positionArray.Length; i++)
            {
                positionArray[i] = fileArray[i];
            }

            int nForcedWidth = 168 * 2;
            int nForcedHeigh = 31;
            byte[] flameGraphic = new byte[nForcedWidth * nForcedHeigh];//positionArray[2] * positionArray[3]];

            int byteIndex = 0;
            for (int bmpIndex = 0; byteIndex < nForcedWidth * nForcedHeigh; bmpIndex+=2, byteIndex +=1)
            {
                flameGraphic[byteIndex] = fileArray[byteIndex + INDEX_SIZE];
            }


            BitmapWriter.Write16BitmapFile("C:\\games\\ultima_5\\temp\\dec_res\\create.16.bmp", nForcedWidth, nForcedHeigh, flameGraphic);
                //positionArray[2], positionArray[3], fileArray);
        }
    }
}
