﻿using Mapperdom.Helpers;
using System;

namespace Mapperdom.Models
{
    public class Nation : Observable, ICloneable
    {
        public bool _isSelected;

        public bool IsSelected
        {
            get
            {
                return _isSelected;
            }
            set
            {
                Set(ref _isSelected, value);
            }
        }

        private bool _isSurrendered;

        public bool IsSurrendered
        {
            get
            {
                return _isSurrendered;
            }
            set
            {
                Set(ref _isSurrendered, value);
            }
        }


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

        private int _labelPosX;
        public int LabelPosX
        {
            get
            {
                return _labelPosX;
            }
            set
            {
                Set(ref _labelPosX, value);
            }
        }

        private int _labelPosY;
        public int LabelPosY
        {
            get
            {
                return _labelPosY;
            }
            set
            {
                Set(ref _labelPosY, value);
            }
        }


        private int _labelFontSize;
        public int LabelFontSize
        {
            get
            {
                return _labelFontSize;
            }
            set
            {
                Set(ref _labelFontSize, value);
            }
        }

        public Nation(string name, System.Drawing.Color mainColor, byte? warSide = null, Nation master = null, int labelPosX = 0, int labelPosY = 0)
        {
            Name = name;
            Master = master;
            WarSide = warSide;
            MainColor = mainColor;
            LabelPosX = labelPosX;
            LabelPosY = labelPosY;
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
            return new Nation(Name, MainColor, WarSide, Master);
        }
    }
}
