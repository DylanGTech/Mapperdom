using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mapperdom.Models
{
    public class MapState
    {
        public byte?[,] OwnershipData { get; set; } //Determines which nation officially owns a pixel (including puppet states)
        public byte?[,] OccupationData { get; set; } //Determines which nation occupies a pixel (excluding puppet states)
        public bool[,] NewCapturesData { get; set; }

        public Nation TalkingNation; //DO NOT Serialize. This is just a reference

        public Dictionary<byte, Nation> Nations { get; set; }

        public Dictionary<byte, WarSide> Sides { get; set; }

        public MapState(byte?[,] ownershipData, byte?[,] occupationData, bool[,] newCapturesData, Dictionary<byte, Nation> nations, Dictionary<byte, WarSide> sides)
        {
            OwnershipData = ownershipData;
            OccupationData = occupationData;
            NewCapturesData = newCapturesData;
            Nations = nations;
            Sides = sides;
        }
    }
}
