using HelixToolkit.Wpf.SharpDX;
using HelixToolkit.Wpf.SharpDX.Core;
using Microsoft.Kinect;
using SharpDX;
using System.Collections.Generic;
using System.Linq;

namespace KinectNav
{
    static class DrawController
    {
        /// <summary> Hand and skeleton box model size </summary>
        static readonly float skeletonBoxSize = (float)0.05;
        static readonly float handBoxSize = (float)0.08;

        /// <summary>  Draw mode for point view UI </summary>
        public static string DrawMode { get; set; }

        /// <summary> Meshes for view models </summary>
        static MeshGeometry3D meshGeometry = new MeshGeometry3D();
        static MeshGeometry3D meshGeometryMapGreen = new MeshGeometry3D();
        static MeshGeometry3D meshGeometryMapRed = new MeshGeometry3D();
        static MeshGeometry3D meshGeometryMapYellow = new MeshGeometry3D();
        static MeshGeometry3D meshGeometrySkeletonRed = new MeshGeometry3D();
        static MeshGeometry3D meshGeometrySkeletonGreen = new MeshGeometry3D();
        static MeshGeometry3D meshGeometrySkeletonBlue = new MeshGeometry3D();

        private const float InferredZPositionClamp = 0.1f;

        /// <summary>
        /// Updates point view geometry from depth data
        /// </summary>
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

        /// <summary>
        /// Updates map tiles geometry
        /// </summary>
        public static void DrawMap()
        {
            MeshBuilder meshBuilderRed = new MeshBuilder();
            MeshBuilder meshBuilderGreen = new MeshBuilder();
            MeshBuilder meshBuilderYellow = new MeshBuilder();

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
                _2DMap.CreateTile(index, "green");
            }

            foreach (var index in DepthData.ObstPointsIndexes)
            {
                _2DMap.CreateTile(index, "red");
            }

            foreach (var point in BodyData.FootPoints)
            {
                _2DMap.DrawFootTiles(point);
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
                        else if (_2DMap.maptiles[x, z].Color == "yellow")
                        {
                            meshBuilderYellow.AddBox(vect, 0.08, 0.08, 0.08, BoxFaces.All);
                        }
                    }
                }
            }

            meshGeometryMapRed = meshBuilderRed.ToMeshGeometry3D();
            meshGeometryMapGreen = meshBuilderGreen.ToMeshGeometry3D();
            meshGeometryMapYellow = meshBuilderYellow.ToMeshGeometry3D();

            MainWindow.main.Dispatcher.Invoke(() =>
            {
                MainWindow.main.mapModelRed.Geometry = meshGeometryMapRed;
                MainWindow.main.mapModelGreen.Geometry = meshGeometryMapGreen;
                MainWindow.main.mapModelYellow.Geometry = meshGeometryMapYellow;
            });
        }

        /// <summary>
        /// Updates skeleton joints geometry
        /// </summary>
        public static void DrawJoints()
        {
            MeshBuilder meshBuilder = new MeshBuilder();
            MeshBuilder meshBulderRed = new MeshBuilder();
            MeshBuilder meshBulderBlue = new MeshBuilder();
            MeshBuilder meshBulderGreen = new MeshBuilder();

            foreach (Body body in BodyData.Bodies)
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

                        // create skeleton and hands geometry

                        if (joints[joint].JointType == JointType.HandLeft || joints[joint].JointType == JointType.HandRight)
                        {
                            var handState = body.HandLeftState;
                            if (joints[joint].JointType == JointType.HandRight)
                            {
                                handState = body.HandRightState;
                            }

                            if (handState == HandState.Closed)
                            {
                                meshBulderRed.AddBox(new Vector3((float)position.X, (float)position.Y, (float)position.Z), handBoxSize, handBoxSize, handBoxSize, BoxFaces.All);
                            }

                            if (handState == HandState.Open)
                            {
                                meshBulderGreen.AddBox(new Vector3((float)position.X, (float)position.Y, (float)position.Z), handBoxSize, handBoxSize, handBoxSize, BoxFaces.All);
                            }

                            if (handState == HandState.Lasso)
                            {
                                meshBulderBlue.AddBox(new Vector3((float)position.X, (float)position.Y, (float)position.Z), handBoxSize, handBoxSize, handBoxSize, BoxFaces.All);
                            }

                            meshGeometrySkeletonRed = meshBulderRed.ToMeshGeometry3D();
                            meshGeometrySkeletonBlue = meshBulderBlue.ToMeshGeometry3D();
                            meshGeometrySkeletonGreen = meshBulderGreen.ToMeshGeometry3D();

                            MainWindow.main.Dispatcher.Invoke(() =>
                            {
                                MainWindow.main.skeletonRed.Geometry = meshGeometrySkeletonRed;
                                MainWindow.main.skeletonBlue.Geometry = meshGeometrySkeletonBlue;
                                MainWindow.main.skeletonGreen.Geometry = meshGeometrySkeletonGreen;
                            });
                        }
                        else
                        {
                            meshBuilder.AddBox(new Vector3((float)position.X, (float)position.Y, (float)position.Z), skeletonBoxSize, skeletonBoxSize, skeletonBoxSize, BoxFaces.All);
                        }
                    }

                    // draw bones
                    foreach (var bone in BodyData.bones)
                    {
                        Joint joint0 = joints[bone.Item1];
                        Joint joint1 = joints[bone.Item1];

                        if (joint0.TrackingState == TrackingState.Tracked && joint1.TrackingState == TrackingState.Tracked)
                        {
                            meshBuilder.AddArrow(BodyData.jointPoints[bone.Item1], BodyData.jointPoints[bone.Item2], 0.01);
                        }
                    }


                }
            }

            meshGeometry = meshBuilder.ToMeshGeometry3D();

            MainWindow.main.Dispatcher.Invoke(() =>
            {
                MainWindow.main.skeletonModel.Geometry = meshGeometry;
            });
        }

        /// <summary>
        /// Updates gesture result UI
        /// </summary>
        public static void UpdateGestureResult()
        {
            MainWindow.main.Dispatcher.Invoke(() =>
            {
                ProcessGestureDetector(BodyData.gestureDetectorList[0], MainWindow.main.CC1);
                ProcessGestureDetector(BodyData.gestureDetectorList[1], MainWindow.main.CC2);
                ProcessGestureDetector(BodyData.gestureDetectorList[2], MainWindow.main.CC3);
                ProcessGestureDetector(BodyData.gestureDetectorList[3], MainWindow.main.CC4);
                ProcessGestureDetector(BodyData.gestureDetectorList[4], MainWindow.main.CC5);
                ProcessGestureDetector(BodyData.gestureDetectorList[5], MainWindow.main.CC6);
            });
        }

        private static void ProcessGestureDetector(GestureDetector gestureDetector, System.Windows.Controls.ContentControl contentControl)
        {
            if (gestureDetector.GestureResultView.IsTracked == false || gestureDetector.IsPaused == true)
            {
                contentControl.Content = "Not tracked";
            }

            else if (gestureDetector.GestureResultView.DetectedGesture == "None")
            {
                contentControl.Content = "Tracked, no gesture";
            }

            else
            {
                contentControl.Content = gestureDetector.GestureResultView.DetectedGesture;
            }
        }
    }
}
