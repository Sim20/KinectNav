using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KinectNav
{
    static class _2DMap
    {
        public const int mapSizeX = 100;
        public const int mapSizeZ = 100;

        public const int mapZoom = 20;

        public static MapTile[,] maptiles;

        static _2DMap()
        {
            maptiles = new MapTile[mapSizeX, mapSizeZ];

            for (int i = 0; i < mapSizeX; i++)
            {
                for (int k = 0; k < mapSizeZ; k++)
                {
                    maptiles[i, k] = new MapTile(i, k);
                }
            }
        }
    }
}
