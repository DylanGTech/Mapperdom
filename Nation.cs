using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml.Media;
using Windows.UI;

namespace Mapperdom
{
    public class Nation
    {
        public string Name { get; private set; } = "Rogopia";
        public uint Manpower { get; private set; } = 0;
        public System.Drawing.Color MainColor { get; private set; } = ColorFromHSL(0, 0.6f, 0.5f);
        public Nation Master { get; private set; } = null;

        public Nation(string name, System.Drawing.Color mainColor, Nation master = null, uint manpower = 0)
        {
            Name = name;
            Manpower = manpower;
            MainColor = mainColor;
            Master = master;
        }



        public System.Drawing.Color PuppetColor
        {
            get
            {
                return ColorFromHSL(MainColor.GetHue(), MainColor.GetSaturation() + 0.10f, MainColor.GetBrightness() + .20f);
            }
        }

        public System.Drawing.Color OccuppiedColor
        {
            get
            {
                return ColorFromHSL(MainColor.GetHue(), MainColor.GetSaturation() + 0.10f, MainColor.GetBrightness() + .30f);
            }
        }

        public System.Drawing.Color GainColor
        {
            get
            {
                return ColorFromHSL(MainColor.GetHue(), MainColor.GetSaturation() - 0.10f, MainColor.GetBrightness() + .40f);
            }
        }


        public Windows.UI.Xaml.Media.Brush MainBrush
        {
            get
            {
                return new SolidColorBrush(Windows.UI.Color.FromArgb(MainColor.A, MainColor.R, MainColor.G, MainColor.B));
            }
        }
        public Windows.UI.Xaml.Media.Brush PuppetBrush
        {
            get
            {
                return new SolidColorBrush(Windows.UI.Color.FromArgb(PuppetColor.A, PuppetColor.R, PuppetColor.G, PuppetColor.B));
            }
        }
        public Windows.UI.Xaml.Media.Brush OccuppiedBrush
        {
            get
            {
                return new SolidColorBrush(Windows.UI.Color.FromArgb(OccuppiedColor.A, OccuppiedColor.R, OccuppiedColor.G, OccuppiedColor.B));
            }
        }
        public Windows.UI.Xaml.Media.Brush GainBrush
        {
            get
            {
                return new SolidColorBrush(Windows.UI.Color.FromArgb(GainColor.A, GainColor.R, GainColor.G, GainColor.B));
            }
        }

        public static System.Drawing.Color ColorFromHSL(float hue, float saturation, float brightness)
        {
            if (hue > 360f) hue = 260f;
            else if (hue < 0) hue = 0f;
            if (saturation > 1f) saturation = 1f;
            else if (saturation < 0) saturation = 0;
            if (brightness > 1f) brightness = 1;
            else if (saturation < 0) saturation = 0;

            double c = brightness * saturation;
            double x = c * (1 - Math.Abs(hue / 60) % 2 - 1);
            double m = brightness - c;

            double r = 0;
            double g = 0;
            double b = 0;

            switch(hue / 60)
            {
                case 0:
                    r = c;
                    g = x;
                    b = 0;
                    break;
                case 1:
                    r = x;
                    g = c;
                    b = 0;
                    break;
                case 2:
                    r = 0;
                    g = c;
                    b = x;
                    break;
                case 3:
                    r = 0;
                    g = x;
                    b = c;
                    break;
                case 4:
                    r = x;
                    g = 0;
                    b = c;
                    break;
                case 5:
                    r = c;
                    g = 0;
                    b = x;
                    break;
            }

            return System.Drawing.Color.FromArgb((byte)((r + m) * 255), (byte)((g + m) * 255), (byte)((b + m) * 255));
        }


        public static void Expand(ushort range, ref byte?[,] occupierMap, byte ownerId, Random rng, sbyte xFocus = 0, sbyte yFocus = 0)
        {

            if (xFocus > 3) xFocus = 3;
            if (xFocus < -3) xFocus = -3;

            if (yFocus > 3) yFocus = 3;
            if (yFocus < -3) yFocus = -3;

            while (range > 0)
            {
                bool[,] bounds = GetBoundaryPixels(ref occupierMap, ownerId);

                for (int y = 0; y < bounds.GetLength(1); y++)
                {
                    for (int x = 0; x < bounds.GetLength(0); x++)
                    {
                        if(occupierMap[x,y] != null && bounds[x,y] == true)
                        {
                            if (x > 0 && occupierMap[x - 1, y] != null && (byte)rng.Next(0, 255) % Math.Pow(4 + xFocus, 2) == 0)
                            {
                                occupierMap[x - 1, y] = ownerId;
                            }
                            if (x < occupierMap.GetLength(0) - 1 && occupierMap[x + 1, y] != null && (byte)rng.Next(0, 255) % Math.Pow(4 - xFocus, 2) == 0)
                            {
                                occupierMap[x + 1, y] = ownerId;
                            }

                            if (y > 0 && occupierMap[x, y - 1] != null && (byte)rng.Next(0, 255) % Math.Pow(4 + yFocus, 2) == 0)
                            {
                                occupierMap[x, y - 1] = ownerId;
                            }
                            if (y < occupierMap.GetLength(1) - 1 && occupierMap[x, y + 1] != null && (byte)rng.Next(0, 255) % Math.Pow(4 - yFocus, 2) == 0)
                            {
                                occupierMap[x, y + 1] = ownerId;
                            }
                        }

                    }
                }

                range--;
            }
        }

        private static bool[,] GetBoundaryPixels(ref byte?[,] occupierMap, byte owner)
        {
            bool[,] bounds = new bool[occupierMap.GetLength(0), occupierMap.GetLength(1)];

            for(int y = 0; y <  occupierMap.GetLength(1); y++)
            {
                for (int x = 0; x < occupierMap.GetLength(0); x++)
                {
                    if(occupierMap[x,y] != null && occupierMap[x,y].Value == owner)
                    {
                        if(x > 0 && occupierMap[x - 1,y] != owner)
                        {
                            bounds[x, y] = true;
                            continue;
                        }
                        if (x < occupierMap.GetLength(0) - 1 && occupierMap[x + 1, y] != owner)
                        {
                            bounds[x, y] = true;
                            continue;
                        }

                        if (y > 0 && occupierMap[x, y - 1] != owner)
                        {
                            bounds[x, y] = true;
                            continue;
                        }
                        if (y < occupierMap.GetLength(1) - 1 && occupierMap[x, y + 1] != owner)
                        {
                            bounds[x, y] = true;
                            continue;
                        }

                    }

                    bounds[x, y] = false;
                }
            }

            return bounds;
        }



        public static void AnnexOccupation(ref byte?[,] ownershipMap, ref byte?[,] occupierMap, byte ownerId, Random rng)
        {
            /*
            bool[,] bounds = GetBoundaryPixels(ref occupierMap, ownerId);
            uint xVal;
            uint yVal;

            do
            {
                xVal = (uint)rng.Next(0, bounds.GetLength(0));
                yVal = (uint)rng.Next(0, bounds.GetLength(1));
            }
            while (!(occupierMap[xVal, yVal] == ownerId && !bounds[xVal, yVal]));

            FillAroundPixel(ref bounds, xVal, yVal);
            */


            for (int y = 0; y < occupierMap.GetLength(1); y++)
            {
                for (int x = 0; x < occupierMap.GetLength(0); x++)
                {
                    
                    //if (bounds[x, y])
                    if(occupierMap[x,y] == ownerId)
                        ownershipMap[x, y] = ownerId;
                }
            }

                }

        private static void FillAroundPixel(ref bool[,] bounds, uint xVal, uint yVal)
        {
            if(!bounds[xVal, yVal])
            {
                bounds[xVal, yVal] = true;
                if (xVal > 0)
                    FillAroundPixel(ref bounds, xVal - 1, yVal);
                if (xVal < bounds.GetLength(0))
                    FillAroundPixel(ref bounds, xVal + 1, yVal);

                if (yVal > 0)
                    FillAroundPixel(ref bounds, xVal, yVal - 1);
                if (yVal < bounds.GetLength(1))
                    FillAroundPixel(ref bounds, xVal, yVal + 1);
            }
        }
    }
}
