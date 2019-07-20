using Windows.UI.Xaml.Media;

namespace Mapperdom
{
    public class WarSide
    {
        public string Name;
        public System.Drawing.Color MainColor;

        public System.Drawing.Color PuppetColor => Nation.ColorFromHsl(MainColor.GetHue(),
            MainColor.GetSaturation() + 0.10f, MainColor.GetBrightness() + .20f);

        public System.Drawing.Color OccupiedColor => Nation.ColorFromHsl(MainColor.GetHue(),
            MainColor.GetSaturation() + 0.10f, MainColor.GetBrightness() + .30f);

        public System.Drawing.Color GainColor => Nation.ColorFromHsl(MainColor.GetHue(),
            MainColor.GetSaturation() - 0.10f, MainColor.GetBrightness() + .40f);

        public Brush MainBrush =>
            new SolidColorBrush(Windows.UI.Color.FromArgb(MainColor.A, MainColor.R, MainColor.G, MainColor.B));

        public Brush PuppetBrush =>
            new SolidColorBrush(Windows.UI.Color.FromArgb(PuppetColor.A, PuppetColor.R, PuppetColor.G, PuppetColor.B));

        public Brush OccupiedBrush =>
            new SolidColorBrush(Windows.UI.Color.FromArgb(OccupiedColor.A, OccupiedColor.R, OccupiedColor.G,
                OccupiedColor.B));

        public Brush GainBrush =>
            new SolidColorBrush(Windows.UI.Color.FromArgb(GainColor.A, GainColor.R, GainColor.G, GainColor.B));

        public WarSide(string name, System.Drawing.Color mainColor)
        {
            Name = name;
            MainColor = mainColor;
        }
    }
}
