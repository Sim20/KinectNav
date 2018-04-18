using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KinectNav
{
    class MapTile
    {
        private double posX;
        private double posy;
        private string color;

        public MapTile( double x, double y)
        {
            posX = x;
            posy = y;
        }

        public double X
        {
            get
            {
                return posX;
            }
        }

        public double Y
        {
            get
            {
                return posy;
            }
        }

        public string Color { get => color; set => color = value; }
    }
}
