using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml.Media;

namespace Mapperdom
{
    public class WarSide
    {
        public string Name;
        public System.Drawing.Color MainColor;

        public System.Drawing.Color PuppetColor
        {
            get
            {
                return Nation.ColorFromHSL(MainColor.GetHue(), MainColor.GetSaturation() + 0.10f, MainColor.GetBrightness() + .20f);
            }
        }

        public System.Drawing.Color OccuppiedColor
        {
            get
            {
                return Nation.ColorFromHSL(MainColor.GetHue(), MainColor.GetSaturation() + 0.10f, MainColor.GetBrightness() + .30f);
            }
        }

        public System.Drawing.Color GainColor
        {
            get
            {
                return Nation.ColorFromHSL(MainColor.GetHue(), MainColor.GetSaturation() - 0.10f, MainColor.GetBrightness() + .40f);
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

        public WarSide(string name, System.Drawing.Color mainColor)
        {
            Name = name;
            MainColor = mainColor;
        }
    }
}
