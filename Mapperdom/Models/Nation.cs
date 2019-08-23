using Mapperdom.Helpers;
using System;

namespace Mapperdom.Models
{
    public class Nation : Observable, ICloneable
    {
        private string _name;
        public string Name
        {
            get
            {
                return _name;
            }
            set
            {
                Set(ref _name, value);
            }
        }

        private System.Drawing.Color _mainColor;
        public System.Drawing.Color MainColor
        {
            get
            {
                return _mainColor;
            }
            set
            {
                Set(ref _mainColor, value);
            }
        }

        private Nation _master;
        public Nation Master
        {
            get
            {
                return _master;
            }
            set
            {
                Set(ref _master, value);
            }
        }

        private byte? _warSide;
        public byte? WarSide
        {
            get
            {
                return _warSide;
            }
            set
            {
                Set(ref _warSide, value);
            }
        }


        public Nation(string name, Nation master = null)
        {
            Name = name;
            Master = master;
            MainColor = System.Drawing.Color.FromArgb(0x0000B33C);
        }

        public static System.Drawing.Color ColorFromHsl(float hue, float saturation, float brightness)
        {
            if (hue > 360f) hue = 360f;
            else if (hue < 0) hue = 0f;
            if (saturation > 1f) saturation = 1f;
            else if (saturation < 0) saturation = 0;
            if (brightness > 1f) brightness = 1f;
            else if (brightness < 0) brightness = 0;

            double c = brightness * saturation;
            double x = c * (1 - Math.Abs(hue / 60) % 2 - 1);
            double m = brightness - c;

            double r = 0;
            double g = 0;
            double b = 0;

            switch ((int)(hue / 60))
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

            return System.Drawing.Color.FromArgb((byte)((r + m) * 255), (byte)((g + m) * 255),
                (byte)((b + m) * 255));
        }

        public object Clone()
        {
            return new Nation(Name, Master);
        }
    }
}
