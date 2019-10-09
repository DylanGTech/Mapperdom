using Mapperdom.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mapperdom.Models
{
    public class FrontEntry : Observable
    {
        private MapperGame gameReference;

        private UnorderedBytePair pair;

        private Nation _nation1;
        public Nation Nation1
        {
            get { return gameReference.Nations[pair.getSmallerByte()]; }
        }

        private Nation _nation2;
        public Nation Nation2
        {
            get { return gameReference.Nations[pair.getLargerByte()]; }
        }

        public sbyte Strength
        {
            get { return gameReference.Fronts[pair]; }
            set
            {
                gameReference.Fronts[pair] = value;
            }
        }


        public FrontEntry(UnorderedBytePair ids, MapperGame game)
        {
            gameReference = game;
            pair = ids;
        }
    }
}
