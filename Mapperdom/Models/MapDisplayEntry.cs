using Mapperdom;
using Mapperdom.Helpers;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Windows.UI.Xaml.Media;

namespace Mapperdom.Models
{
    public class MapDisplayEntry : Observable
    {

        private Nation _nation;
        public Nation Nation
        {
            get { return _nation; }
            set
            {
                Set(ref _nation, value);
            }
        }

        private string _sideName;
        public string SideName
        {
            get { return _sideName; }
            set
            {
                Set(ref _sideName, value);
            }
        }

        private Brush _mainBrush;
        public Brush MainBrush
        {
            get { return _mainBrush; }
            set
            {
                Set(ref _mainBrush, value);
            }
        }
        private Brush _puppetBrush;
        public Brush PuppetBrush
        {
            get { return _puppetBrush; }
            set
            {
                Set(ref _puppetBrush, value);
            }
        }
        private Brush _occupiedBrush;
        public Brush OccupiedBrush
        {
            get { return _occupiedBrush; }
            set
            {
                Set(ref _occupiedBrush, value);
            }
        }
        private Brush _gainBrush;
        public Brush GainBrush
        {
            get { return _gainBrush; }
            set
            {
                Set(ref _gainBrush, value);
            }
        }

        public MapDisplayEntry(Nation nation, WarSide ws)
        {
            this.Nation = nation;
            if (ws != null)
            {
                SideName = ws.Name;
                MainBrush = ws.MainBrush;
                PuppetBrush = ws.PuppetBrush;
                OccupiedBrush = ws.OccupiedBrush;
                GainBrush = ws.GainBrush;
            }
            else
            {
                Brush neutralBrush = new SolidColorBrush(Windows.UI.Color.FromArgb(0xff, 0x00, 0xB3, 0x3C));
                SideName = "Neutral";
                MainBrush = neutralBrush;
                PuppetBrush = neutralBrush;
                OccupiedBrush = neutralBrush;
                GainBrush = neutralBrush;
            }
        }

        public void Update(WarSide ws)
        {
            if (ws != null)
            {
                Set(ref _sideName, ws.Name, "SideName");
                Set(ref _mainBrush, ws.MainBrush, "MainBrush");
                Set(ref _puppetBrush, ws.PuppetBrush, "PuppetBrush");
                Set(ref _occupiedBrush, ws.OccupiedBrush, "OccupiedBrush");
                Set(ref _gainBrush, ws.GainBrush, "GainBrush");
            }
            else
            {
                Brush neutralBrush = new SolidColorBrush(Windows.UI.Color.FromArgb(0xff, 0x00, 0xB3, 0x3C));
                Set(ref _sideName, "Neutral", "SideName");
                Set(ref _mainBrush, neutralBrush, "MainBrush");
                Set(ref _puppetBrush, neutralBrush, "PuppetBrush");
                Set(ref _occupiedBrush, neutralBrush, "OccupiedBrush");
                Set(ref _gainBrush, neutralBrush, "GainBrush");
            }
        }
    }
}
