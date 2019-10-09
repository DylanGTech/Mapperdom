using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mapperdom.Models
{
    public struct UnorderedBytePair
    {
        private byte byte1;
        private byte byte2;

        //No matter what order the bytes are places in or what their value are, they will always map to the same value in a dictionary
        public override int GetHashCode()
        {
            //Smallest byte always comes first
            if(byte1 > byte2)
            {
                return (byte2 << 8) | byte1;
            }
            else
            {
                return (byte1 << 8) | byte2;
            }
        }

        public byte getSmallerByte()
        {
            return byte1 > byte2 ? byte2 : byte1;
        }
        public byte getLargerByte()
        {
            return byte1 < byte2 ? byte2 : byte1;
        }


        public UnorderedBytePair(byte byte1, byte byte2)
        {
            this.byte1 = byte1;
            this.byte2 = byte2;
        }
    }
}
