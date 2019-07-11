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
        public uint Manpower { get; private set; } = 0; //Not currently used
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
    }
}
