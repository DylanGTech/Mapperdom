using Mapperdom.Helpers;
using System;
using Windows.UI.Xaml.Media;

namespace Mapperdom.Models
{
    public class WarSide : Observable, ICloneable
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
                OnPropertyChanged("MainBrush");
            }
        }

        private System.Drawing.Color _puppetColor;
        public System.Drawing.Color PuppetColor
        {
            get
            {
                return _puppetColor;
            }
            set
            {
                Set(ref _puppetColor, value);
                OnPropertyChanged("PuppetBrush");
            }
        }
        private System.Drawing.Color _occupiedColor;
        public System.Drawing.Color OccupiedColor
        {
            get
            {
                return _occupiedColor;
            }
            set
            {
                Set(ref _occupiedColor, value);
                OnPropertyChanged("OccupiedBrush");
            }
        }
        private System.Drawing.Color _gainColor;
        public System.Drawing.Color GainColor
        {
            get
            {
                return _gainColor;
            }
            set
            {
                Set(ref _gainColor, value);
                OnPropertyChanged("GainBrush");
            }
        }

        public Brush MainBrush =>
            new SolidColorBrush(Windows.UI.Color.FromArgb(MainColor.A, MainColor.R, MainColor.G, MainColor.B));
      
        public Brush PuppetBrush =>
            new SolidColorBrush(Windows.UI.Color.FromArgb(PuppetColor.A, PuppetColor.R, PuppetColor.G, PuppetColor.B));

        public Brush OccupiedBrush =>
            new SolidColorBrush(Windows.UI.Color.FromArgb(OccupiedColor.A, OccupiedColor.R, OccupiedColor.G,
                OccupiedColor.B));

        public Brush GainBrush =>
            new SolidColorBrush(Windows.UI.Color.FromArgb(GainColor.A, GainColor.R, GainColor.G, GainColor.B));

        public WarSide(string name, System.Drawing.Color mainColor, System.Drawing.Color puppetColor, System.Drawing.Color occupiedColor, System.Drawing.Color gainColor)
        {
            Name = name;
            MainColor = mainColor;
            PuppetColor = puppetColor;
            OccupiedColor = occupiedColor;
            GainColor = gainColor;
        }

        public object Clone()
        {
            return new WarSide(Name, MainColor, PuppetColor, OccupiedColor, GainColor);
        }
    }
}
