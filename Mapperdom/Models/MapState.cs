using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mapperdom.Models
{
    public struct MapState
    {

        [JsonIgnore]
        public PixelData[,] Pixels;

        public Dictionary<UnorderedBytePair, sbyte> Fronts;
        public Dictionary<byte, Nation> Nations;
        public Dictionary<byte, WarSide> Sides;

        public string DialogText;
        public bool IsTreatyMode;
        public Rectangle DialogRectangle;
    }
}
