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
  
    public static Dictionary<byte, Color> GetEgaPalette()
        {
            Dictionary<byte, Color> palette=new Dictionary<byte, Color>();
            palette.Add(0, Color.Black);
            palette.Add(1, Color.Blue);
            palette.Add(2, Color.Green);
            palette.Add(3, Color.Cyan);
            palette.Add(4, Color.FromArgb(0xb51000));//Color.Red);
            palette.Add(5, Color.Magenta);
            palette.Add(6, Color.FromArgb(0xAF5800));//Color.Brown);
            palette.Add(7, Color.LightGray);
            palette.Add(8, Color.FromArgb(0x55, 0x55, 0x55));//Color.DarkGray);
            palette.Add(9, Color.FromArgb(0x55, 0x55, 0xFF)); // bright blue
            palette.Add(10, Color.FromArgb(0x55, 0xFF, 0x55)); // bright green
            palette.Add(11, Color.FromArgb(0x55, 0xFF, 0xFF)); // bright cyan
            palette.Add(12, Color.FromArgb(0xFF, 0x55, 0x55)); // bright red
            palette.Add(13, Color.FromArgb(0xFF, 0xFF, 0x55)); // bright magenta
            palette.Add(14, Color.FromArgb(0xFF, 0xFF, 0x55)); // bright yellow
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

                Dictionary<byte, Color> egaPalette = GetEgaPalette();

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

    //    public static int WriteBitmapFile(string filename, int width, int height, byte[] imageData)
    //    {
    //        byte[] newData = new byte[imageData.Length];

    //        for (int x = 0; x < imageData.Length; x += 4)
    //        {
    //            byte[] pixel = new byte[4];
    //            Array.Copy(imageData, x, pixel, 0, 4);

    //            byte r = pixel[0];
    //            byte g = pixel[1];
    //            byte b = pixel[2];
    //            byte a = pixel[3];

    //            byte[] newPixel = new byte[] { b, g, r, a };

    //            Array.Copy(newPixel, 0, newData, x, 4);
    //        }

    //        imageData = newData;

    //        using (var stream = new MemoryStream(imageData))
    //        using (var bmp = new Bitmap(width, height, PixelFormat.Format32bppArgb))
    //        {
    //            BitmapData bmpData = bmp.LockBits(new Rectangle(0, 0,
    //                                                            bmp.Width,
    //                                                            bmp.Height),
    //                                              ImageLockMode.WriteOnly,
    //                                              bmp.PixelFormat);

    //            IntPtr pNative = bmpData.Scan0;
    //            Marshal.Copy(imageData, 0, pNative, imageData.Length);

    //            bmp.UnlockBits(bmpData);

    //            bmp.Save(filename);
    //        }

    //        return 1;
    //    }
    }


    class Program
    {
 
        static void CreateMon0()
        {
            const int nDefaultOffset = 0x00;
            //const int nPixelWidth = 12 * 2;
            const int nPixelWidth = 12;
            //const int nBytesWidth = nPixelWidth / 2;
            const int nPixelHeight = 50;
            const int nStartIndex = 0x1D + 2;//(320 / 2) * 61 + 0x1A;
            //const int nOffsetPerFire = nBytesWidth * nPixelHeight + nDefaultOffset;

            Export("C:\\games\\ultima_5\\temp\\dec_res\\mon1.16.uncomp", "C:\\games\\ultima_5\\temp\\dec_res\\mon1.16.bmp",
                nPixelWidth, nPixelHeight, nStartIndex + nDefaultOffset);
            
            // const int indexSize = 0x32;
            // byte[] fileArray = File.ReadAllBytes("C:\\games\\ultima_5\\temp\\dec_res\\mon1.16.uncomp");
            // //byte[] fileArray = File.ReadAllBytes("C:\\games\\ultima_5\\temp\\dec_res\\create.16.uncomp");
            //
            // byte[] positionArray = new byte[indexSize];
            // for (int i = 0; i < positionArray.Length; i++)
            // {
            //     positionArray[i] = fileArray[i];
            // }
            //
            // int nForcedWidth = 12;// * 2;
            // int nForcedHeight = 50;
            // byte[] flameGraphic = new byte[nForcedWidth * nForcedHeight];//positionArray[2] * positionArray[3]];
            //
            // int byteIndex = 0;
            // for (int bmpIndex = 0; byteIndex < nForcedWidth * nForcedHeight; bmpIndex += 2, byteIndex += 1)
            // {
            //     flameGraphic[byteIndex] = fileArray[byteIndex + indexSize];
            // }
            //
            //
            // BitmapWriter.Write16BitmapFile("C:\\games\\ultima_5\\temp\\dec_res\\mon1.16.bmp", nForcedWidth, nForcedHeight, flameGraphic);
            //positionArray[2], positionArray[3], fileArray);
        }
        static void CreateScreen()
        {
            const int indexSize = 0x32;
            byte[] fileArray = File.ReadAllBytes("C:\\games\\ultima_5\\temp\\dec_res\\create.16.uncomp");
            //byte[] fileArray = File.ReadAllBytes("C:\\games\\ultima_5\\temp\\dec_res\\create.16.uncomp");

            byte[] positionArray = new byte[indexSize];
            for (int i = 0; i < positionArray.Length; i++)
            {
                positionArray[i] = fileArray[i];
            }

            int nForcedWidth = 168;// * 2;
            int nForcedHeigh = 200;
            byte[] flameGraphic = new byte[nForcedWidth * nForcedHeigh];//positionArray[2] * positionArray[3]];

            int byteIndex = 0;
            for (int bmpIndex = 0; byteIndex < nForcedWidth * nForcedHeigh; bmpIndex += 2, byteIndex += 1)
            {
                flameGraphic[byteIndex] = fileArray[byteIndex + indexSize];
            }


            BitmapWriter.Write16BitmapFile("C:\\games\\ultima_5\\temp\\dec_res\\create.16.bmp", nForcedWidth, nForcedHeigh, flameGraphic);
            //positionArray[2], positionArray[3], fileArray);
        }

        public static void StartScreen()
        {
            Export("C:\\games\\ultima_5\\temp\\dec_res\\startsc.16.uncomp", "C:\\games\\ultima_5\\temp\\dec_res\\startsc.16.bmp",
                168, 30, 0);
        }

        public static void UltimaScreen()
        {
            Export("C:\\games\\ultima_5\\temp\\dec_res\\ultima.16.uncomp", "C:\\games\\ultima_5\\temp\\dec_res\\ultima.16.bmp",
                320, 61, 0x1A);
        }

        public static void FireScreen()
        {
            const int nDefaultOffset = 0x04;
            const int nPixelWidth = 288;
            const int nBytesWidth = nPixelWidth / 2;
            const int nPixelHeight = 49;
            const int nStartIndex = (320 / 2) * 61 + 0x1A;
            const int nOffsetPerFire = nBytesWidth * nPixelHeight + nDefaultOffset;

            Export("C:\\games\\ultima_5\\temp\\dec_res\\ultima.16.uncomp", "C:\\games\\ultima_5\\temp\\dec_res\\ultima_fire.16.bmp",
                nPixelWidth, nPixelHeight, nStartIndex + nDefaultOffset);

            Export("C:\\games\\ultima_5\\temp\\dec_res\\ultima.16.uncomp", "C:\\games\\ultima_5\\temp\\dec_res\\ultima_fire2.16.bmp",
                nPixelWidth, nPixelHeight, nStartIndex + nOffsetPerFire + nDefaultOffset);

            Export("C:\\games\\ultima_5\\temp\\dec_res\\ultima.16.uncomp", "C:\\games\\ultima_5\\temp\\dec_res\\ultima_fire3.16.bmp",
                nPixelWidth, nPixelHeight, nStartIndex + (nOffsetPerFire*2) + nDefaultOffset);

            Export("C:\\games\\ultima_5\\temp\\dec_res\\ultima.16.uncomp", "C:\\games\\ultima_5\\temp\\dec_res\\ultima_fire4.16.bmp",
                nPixelWidth, nPixelHeight, nStartIndex + (nOffsetPerFire * 3) + nDefaultOffset);
        }


        public static void Export(string uncompFilename, string bmpFilename, int nGraphicWidth, int nGraphicHeight, int nOffset)
        {
            //const int INDEX_SIZE = 0x32;
            byte[] fileArray = File.ReadAllBytes(uncompFilename);

            byte[] positionArray = new byte[nOffset];
            for (int i = 0; i < positionArray.Length; i++)
            {
                positionArray[i] = fileArray[i];
            }

            int nForcedWidth = nGraphicWidth;
            int nForcedHeigh = nGraphicHeight;
            byte[] flameGraphic = new byte[nForcedWidth * nForcedHeigh];

            int byteIndex = 0;
            for (int bmpIndex = 0; bmpIndex < nForcedWidth * nForcedHeigh; bmpIndex += 2, byteIndex += 1)
            {
                flameGraphic[byteIndex] = fileArray[byteIndex + nOffset];
            }


            BitmapWriter.Write16BitmapFile(bmpFilename, nForcedWidth, nForcedHeigh, flameGraphic);
        }

        private ICompressorAlgorithm _compressorAlgorithm;

        public ICompressorAlgorithm CompressorAlgorithm
        {
            // allows us to set the algorithm at runtime, also lets us set the algorithm dynamically if we creata  factory class
            set
            {
                _compressorAlgorithm = value;
            }
        }

        //public PbvCompressor()
        //{
        //    // setting this by default but we could create a facotry class to let us set the algorithm based on arguments passed to the main method
        //    // IE. "Compress.exe zip -c blah.txt" would use the zip algorithm as opposed to the default one.
        //    CompressorAlgorithm = new PbvCompressorLZW();
        //}

        static void Main(string[] args)
        {
            CreateMon0();
            StartScreen();
            UltimaScreen();
            FireScreen();

            PbvCompressorLzw lzw = new PbvCompressorLzw();
            lzw.Decompress("C:\\games\\ultima_5\\temp\\ultima.16", "C:\\games\\ultima_5\\temp\\dec_res\\ultima.16.uncomp2");
        }
    }
}
