using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mapperdom.Models
{
    public struct MapState
    {
        public byte?[,] ownershipData; //Determines which nation officially owns a pixel (including puppet states)
        public byte?[,] occupationData; //Determines which nation occupies a pixel (excluding puppet states)
        public bool[,] newCapturesData;

        public Nation TalkingNation;

        public Dictionary<byte, Nation> Nations;

        public Dictionary<byte, WarSide> Sides;
    }
}
