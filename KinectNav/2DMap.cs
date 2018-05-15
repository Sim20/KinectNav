using System;
using SharpDX;

namespace KinectNav
{
    static class _2DMap
    {
        /// <summary> Map x,y size, map zoom relative to depth point data </summary>
        public const int mapSizeX = 100;
        public const int mapSizeZ = 100;
        public const int mapZoom = 20;

        public static MapTile[,] maptiles;

        /// <summary>
        /// Creates blank map
        /// </summary>
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
        /// <summary>
        /// Creates colored map tile from depth point
        /// </summary>
        /// <param name="index"></param> Depth point index
        /// <param name="str"></param> Map tile color
        public static void CreateTile(int index, string str)
        {
            int mapZoom = _2DMap.mapZoom;
            int mapSizeX = _2DMap.mapSizeX;
            int mapSizeZ = _2DMap.mapSizeZ;

            Vector3 pos = new Vector3((float)DepthData.allPoints[index].X, (float)DepthData.allPoints[index].Y, (float)DepthData.allPoints[index].Z);

            Vector2 tilepos = new Vector2(Convert.ToInt32(pos.X * mapZoom + mapSizeX / 2), Convert.ToInt32(pos.Z * mapZoom));

            if (tilepos.X >= 0 && tilepos.X < mapSizeX && tilepos.Y >= 0 && tilepos.Y < mapSizeZ)
            {
                _2DMap.maptiles[(int)tilepos.X, (int)tilepos.Y].Color = str;
            }
        }

        /// <summary>
        /// Creates 5x5 yellow map tiles around a point
        /// </summary>
        /// <param name="point"></param> Point to draw around
        public static void DrawFootTiles(System.Windows.Media.Media3D.Point3D point)
        {
            Vector3 pos = new Vector3((float)point.X, (float)point.Y, (float)point.Z);

            Vector2 tilepos = new Vector2(Convert.ToInt32(pos.X * mapZoom + mapSizeX / 2), Convert.ToInt32(pos.Z * mapZoom));

            for (int i = -2; i <= 2; i++)
            {
                for (int k = -2; k <= 2; k++)
                {
                    if (tilepos.X + i >= 0 && tilepos.X + i < mapSizeX && tilepos.Y + k >= 0 && tilepos.Y + k < mapSizeZ)
                    {
                        _2DMap.maptiles[(int)tilepos.X + i, (int)tilepos.Y + k].Color = "yellow";
                    }
                }
            }
        }
    }
}
