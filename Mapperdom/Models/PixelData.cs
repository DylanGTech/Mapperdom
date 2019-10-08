using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mapperdom.Models
{
    public struct PixelData : ICloneable
    {
        public byte OwnerId;
        public byte OccupierId;


        public byte Attributes;

        /*
         * 0 - IsOcean
         * 1 - IsGained
         * 2 - TerrainDensity
         * 3 - TerrainDensity
         * 4 - PopulationDensity
         * 5 - PopulationDensity
         * 6 - RESERVED
         * 7 - RESERVED
         */


        public PixelData(byte ownerId, byte occupierId, byte attributes)
        {
            OwnerId = ownerId;
            OccupierId = occupierId;
            Attributes = attributes;
        }


        public bool IsOcean
        {
            get { return Convert.ToBoolean(Attributes & (1 << 0)); }
            set { if (IsOcean != value) Attributes ^= (1 << 0); }
        }

        public bool IsGained
        {
            get { return Convert.ToBoolean(Attributes & (1 << 1)); }
            set { if (IsGained != value) Attributes ^= (1 << 1); }
        }

        public byte TerrainDensity
        {
            get { return Convert.ToByte((Attributes & (1 << 3)) * 2 + Attributes & (1 << 2)); }
            set
            {
                if (value > 3) throw new ArgumentOutOfRangeException("Terrain Density must range from 0-3");

                if ((value & (1 << 3)) == (Attributes & (1 << 3))) Attributes ^= (1 << 3);
                if ((value & (1 << 2)) == (Attributes & (1 << 2))) Attributes ^= (1 << 2);

            }
        }

        public byte PopulationDensity
        {
            get { return Convert.ToByte((Attributes & (1 << 5)) * 2 + Attributes & (1 << 4)); }
            set
            {
                if (value > 3) throw new ArgumentOutOfRangeException("Population Density must range from 0-3");

                if ((value & (1 << 5)) == (Attributes & (1 << 5))) Attributes ^= (1 << 5);
                if ((value & (1 << 4)) == (Attributes & (1 << 4))) Attributes ^= (1 << 4);

            }
        }

        public object Clone()
        {
            return new PixelData()
            {
                OwnerId = OwnerId,
                OccupierId = OccupierId,
                Attributes = Attributes
            };
        }
    }
}
