using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mapperdom.Models
{
    public class MapState
    {

        public PixelData[,] Pixels { get; set; }


        public Nation TalkingNation; //DO NOT Serialize. This is just a reference
        public List<Nation> SelectedNations; //Same here



        public Dictionary<UnorderedBytePair, sbyte> Fronts { get; set; }
        public Dictionary<byte, Nation> Nations { get; set; }

        public Dictionary<byte, WarSide> Sides { get; set; }

        public MapState(PixelData[,] pixels, Dictionary<byte, Nation> nations, Dictionary<byte, WarSide> sides, Dictionary<UnorderedBytePair, sbyte> fronts)
        {
            Pixels = pixels;
            Nations = nations;
            Sides = sides;
            Fronts = fronts;
        }
    }
}
