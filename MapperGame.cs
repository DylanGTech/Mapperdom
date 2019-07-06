using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
using System.Threading.Tasks;
using Windows.UI;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;

namespace Mapperdom
{
    public class MapperGame
    {
        private WriteableBitmap baseImage;

        //Null repreresents an unclaimable pixel (ocean)
        private byte?[,] ownershipData; //Determines which nation officially owns a pixel (including puppet states)
        private byte?[,] occupationData; //Determines which nation occupies a pixel (excluding puppet states)

        public Dictionary<byte, Nation> nations;
        private Random rng;


        public MapperGame(WriteableBitmap map, int? seed = null)
        {
            rng = seed != null ? new Random(seed.Value) : new Random();

            baseImage = map;

            byte[] imageArray = new byte[baseImage.PixelHeight * baseImage.PixelWidth * 4];

            using (Stream stream = baseImage.PixelBuffer.AsStream())
            {
                stream.Read(imageArray, 0, imageArray.Length);
            }



            ownershipData = new byte?[baseImage.PixelWidth,baseImage.PixelHeight];
            occupationData = new byte?[baseImage.PixelWidth,baseImage.PixelHeight];
            for (int y = 0; y < baseImage.PixelHeight; y++)
            {
                for (int x = 0; x < baseImage.PixelWidth; x++)
                {

                    //Ocean
                    if (imageArray[4 * (y * baseImage.PixelHeight + x)] == 0x80 //Blue
                        && imageArray[4 * (y * baseImage.PixelHeight + x) + 1] == 0x55 //Green
                        && imageArray[4 * (y * baseImage.PixelHeight + x) + 2] == 0x00) //Red
                    {
                        ownershipData[x, y] = null;
                        occupationData[x, y] = null;
                    }
                    //Land
                    else if (imageArray[4 * (y * baseImage.PixelHeight + x)] == 0x00 //Blue
                        && imageArray[4 * (y * baseImage.PixelHeight + x) + 1] == 0xB3 //Green
                        && imageArray[4 * (y * baseImage.PixelHeight + x) + 2] == 0x3C) //Red
                    {
                        ownershipData[x, y] = 0;
                        occupationData[x, y] = 0;
                    }
                    //Invalid
                    else throw new Exception(string.Format("Image composition contains invalid coloring: R:{0},G:{1},B:{2}", imageArray[4 * (y * baseImage.PixelHeight + x) + 2], imageArray[4 * (y * baseImage.PixelHeight + x) + 1], imageArray[4 * (y * baseImage.PixelHeight + x)]));

                }
            }
            nations = new Dictionary<byte, Nation>();

            //Default nation
            nations.Add(0, new Nation("Rogopia", Nation.ColorFromHSL(0f, 0.6f, 0.5f)));
            nations.Add(1, new Nation("Rebels", Nation.ColorFromHSL(240f, 0.6f, 0.5f)));


            Random r = new Random();


            for(byte b = 0; b < 31; b++)
            {
                int x1;
                int y1;
                do
                {
                    x1 = r.Next(0, occupationData.GetLength(0) - 1);
                    y1 = r.Next(0, occupationData.GetLength(1) - 1);
                }
                while (!occupationData[x1, y1].HasValue);

                occupationData[x1, y1] = 1;
            }
        }

        public ImageSource GetCurrentMap()
        {

            //Read base image
            WriteableBitmap newImage = new WriteableBitmap(baseImage.PixelWidth, baseImage.PixelHeight);
            byte[] imageArray = new byte[newImage.PixelHeight * newImage.PixelWidth * 4];

            using (Stream stream = baseImage.PixelBuffer.AsStream())
            {
                stream.Read(imageArray, 0, imageArray.Length);
            }


            bool[,] borders = new bool[baseImage.PixelWidth, baseImage.PixelHeight];
            GetBorders(ref borders);

            for (int y = 0; y < baseImage.PixelHeight; y++)
            {
                for (int x = 0; x < baseImage.PixelWidth; x++)
                {
                    //Draw borders between nations (very small for the time being)
                    if(borders[x,y])
                    {
                        imageArray[4 * (y * baseImage.PixelHeight + x)] = 0; // Blue
                        imageArray[4 * (y * baseImage.PixelHeight + x) + 1] = 0;  // Green
                        imageArray[4 * (y * baseImage.PixelHeight + x) + 2] = 0; // Red
                        //imageArray[4 * (y * baseImage.PixelHeight + x) + 3] = 0; // Opacity
                    }

                    if (ownershipData[x,y] != null)
                    {
                        Nation officialNation = nations[ownershipData[x,y].Value];
                        Nation occupierNation = nations[occupationData[x,y].Value];

                        if(officialNation.Master == null && officialNation == occupierNation) //Nation controls its own territory and is not puppetted
                        {
                            imageArray[4 * (y * baseImage.PixelHeight + x)] = officialNation.MainColor.B; // Blue
                            imageArray[4 * (y * baseImage.PixelHeight + x) + 1] = officialNation.MainColor.G;  // Green
                            imageArray[4 * (y * baseImage.PixelHeight + x) + 2] = officialNation.MainColor.R; // Red
                            //imageArray[4 * (y * baseImage.PixelHeight + x) + 3] = 0; // Opacity
                        }
                        else if(officialNation.Master == occupierNation || officialNation == occupierNation) //Nation controlls a puppet's territory
                        {
                            imageArray[4 * (y * baseImage.PixelHeight + x)] = occupierNation.PuppetColor.B; // Blue
                            imageArray[4 * (y * baseImage.PixelHeight + x) + 1] = occupierNation.PuppetColor.G;  // Green
                            imageArray[4 * (y * baseImage.PixelHeight + x) + 2] = occupierNation.PuppetColor.R; // Red
                            //imageArray[4 * (y * baseImage.PixelHeight + x) + 3] = 0; // Opacity
                        }
                        else //Nation is occupied by another nation
                        {
                            imageArray[4 * (y * baseImage.PixelHeight + x)] = occupierNation.OccuppiedColor.B; // Blue
                            imageArray[4 * (y * baseImage.PixelHeight + x) + 1] = occupierNation.OccuppiedColor.G;  // Green
                            imageArray[4 * (y * baseImage.PixelHeight + x) + 2] = occupierNation.OccuppiedColor.R; // Red
                            //imageArray[4 * (y * baseImage.PixelHeight + x) + 3] = 0; // Opacity
                        }
                    }
                }
            }

            //Write pixel array to final image
            using (Stream stream = newImage.PixelBuffer.AsStream())
            {
                stream.Write(imageArray, 0, imageArray.Length);
            }

            return newImage;
        }


        public void Advance(ushort amount, byte nationId, sbyte xFocus = 0, sbyte yFocus = 0)
        {
            Nation.Expand(amount, ref occupationData, nationId, rng, xFocus, yFocus);
        }

        public void AnnexTerritory(byte nationId)
        {
            Nation.AnnexOccupation(ref ownershipData, ref occupationData, nationId, rng);
        }

        //Get pixels with locations that border other nations (not including the sea)
        public void GetBorders(ref bool[,] borders)
        {
            for (int y = 0; y < baseImage.PixelHeight; y++)
            {
                for (int x = 0; x < baseImage.PixelWidth; x++)
                {
                    if(ownershipData[x, y].HasValue)
                    {
                        if (x > 0 && ownershipData[x - 1, y].HasValue && ownershipData[x - 1, y] != ownershipData[x, y])
                        {
                            borders[x, y] = true;
                            continue;
                        }
                        if (x < ownershipData.GetLength(0) - 1 && ownershipData[x + 1, y].HasValue && ownershipData[x + 1, y] != ownershipData[x, y])
                        {
                            borders[x, y] = true;
                            continue;
                        }

                        if (y > 0 && ownershipData[x, y - 1].HasValue && ownershipData[x, y - 1] != ownershipData[x, y])
                        {
                            borders[x, y] = true;
                            continue;
                        }
                        if (y < ownershipData.GetLength(1) - 1 && ownershipData[x, y + 1].HasValue && ownershipData[x, y + 1] != ownershipData[x, y])
                        {
                            borders[x, y] = true;
                            continue;
                        }
                    }
                    borders[x, y] = false;
;                }
            }
        }

    }
}
