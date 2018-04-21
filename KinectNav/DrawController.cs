using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using Microsoft.Kinect;

using HelixToolkit.Wpf.SharpDX;
using HelixToolkit.Wpf.SharpDX.Core;
using SharpDX;

namespace KinectNav
{
    static class DrawController
    {
        public static string DrawMode { get; set; }

        static MeshGeometry3D meshGeometry = new MeshGeometry3D();
        static MeshGeometry3D meshGeometryMapGreen = new MeshGeometry3D();
        static MeshGeometry3D meshGeometryMapRed = new MeshGeometry3D();

        private const float InferredZPositionClamp = 0.1f;

        static DrawController()
        {

        }

        public static void DrawPoints()
        {
            MeshBuilder meshBuilder = new MeshBuilder();
            List<int> p = new List<int>();

            switch (DrawMode)
            {
                case "points":
                    p = DepthData.AllPointsIndexes;
                    break;
                case "obstPoints":
                    p = DepthData.ObstPointsIndexes;
                    break;
            }

            foreach (int index in p)
            {
                meshBuilder.AddBox(new Vector3((float)DepthData.allPoints[index].X, (float)DepthData.allPoints[index].Y, (float)DepthData.allPoints[index].Z), 0.005, 0.005, 0.005, BoxFaces.All);
            }

            meshGeometry = meshBuilder.ToMeshGeometry3D();
            meshGeometry.Colors = new Color4Collection(meshGeometry.TextureCoordinates.Select(x => x.ToColor4()));

            MainWindow.main.Dispatcher.Invoke(() =>
            {
                MainWindow.main.model1.Geometry = meshGeometry;
            });
        }

        public static void DrawMap()
        {
            MeshBuilder meshBuilderRed = new MeshBuilder();
            MeshBuilder meshBuilderGreen = new MeshBuilder();

            int mapSizeX = _2DMap.mapSizeX;
            int mapSizeZ = _2DMap.mapSizeZ;

            for (int x = 0; x < _2DMap.maptiles.GetLength(0); x++)
            {
                for (int z = 0; z < _2DMap.maptiles.GetLength(1); z++)
                {
                    _2DMap.maptiles[x, z].Color = "none";
                }
            }

            foreach (var index in DepthData.GroundPointsIndexes)
            {
                CreateTile(index, "green");
            }

            foreach (var index in DepthData.ObstPointsIndexes)
            {
                CreateTile(index, "red");
            }

            for (int x = 0; x < mapSizeX; x++)
            {
                for (int z = 0; z < mapSizeZ; z++)
                {
                    if (x < z + mapSizeZ / 2 && x > -z + mapSizeZ / 2 && z > 10)
                    {
                        Vector3 vect = new Vector3((float)x / 10 - 5, -1, (float)z / 10 - 5);

                        if (_2DMap.maptiles[x, z].Color == "red")
                        {
                            meshBuilderRed.AddBox(vect, 0.08, 0.08, 0.08, BoxFaces.All);
                        }
                        else if (_2DMap.maptiles[x, z].Color == "green")
                        {
                            meshBuilderGreen.AddBox(vect, 0.08, 0.08, 0.08, BoxFaces.All);
                        }
                    }

                }
            }

            meshGeometryMapRed = meshBuilderRed.ToMeshGeometry3D();
            meshGeometryMapGreen = meshBuilderGreen.ToMeshGeometry3D();

            MainWindow.main.Dispatcher.Invoke(() =>
            {
                MainWindow.main.mapModelRed.Geometry = meshGeometryMapRed;
                MainWindow.main.mapModelGreen.Geometry = meshGeometryMapGreen;
            });
        }

        private static void CreateTile(int index, string str)
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

        public static void DrawBody()
        {
            MeshBuilder meshBuilder = new MeshBuilder();

            foreach (Body body in BodyData.bodies)
            {
                if (body.IsTracked)
                {
                    IReadOnlyDictionary<JointType, Joint> joints = body.Joints;

                    foreach (JointType joint in joints.Keys)
                    {
                        // sometimes the depth(Z) of an inferred joint may show as negative
                        // clamp down to 0.1f to prevent coordinatemapper from returning (-Infinity, -Infinity)

                        CameraSpacePoint position = joints[joint].Position;

                        if (position.Z < 0)
                        {
                            position.Z = InferredZPositionClamp;
                        }

                        TrackingState trackingState = joints[joint].TrackingState;

                        meshBuilder.AddBox(new Vector3((float)position.X, (float)position.Y, (float)position.Z), 0.07, 0.07, 0.07, BoxFaces.Top);

                    }
                }
            }

            meshGeometry = meshBuilder.ToMeshGeometry3D();
            meshGeometry.Colors = new Color4Collection(meshGeometry.TextureCoordinates.Select(x => x.ToColor4()));

            MainWindow.main.Dispatcher.Invoke(() =>
            {
                MainWindow.main.skeletonModel.Geometry = meshGeometry;
            });

        }
    }
}
