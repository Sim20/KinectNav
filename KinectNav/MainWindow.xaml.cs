using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Color = System.Windows.Media.Color;
using Colors = System.Windows.Media.Colors;
using System.Windows.Shapes;

using Microsoft.Kinect;
using Media3D = System.Windows.Media.Media3D;
using System.Diagnostics;

using System.Threading;
using HelixToolkit.Wpf.SharpDX;
using HelixToolkit.Wpf.SharpDX.Core;
using SharpDX;


namespace KinectNav
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    /// 
    public partial class MainWindow : Window
    {
        private string drawmode = "None";

        private const float InferredZPositionClamp = 0.1f;
        private const float robotHeight = 1.5f;

        const int mapSizeX = 100;
        const int mapSizeZ = 100;

        const int mapZoom = 20;

        private bool updateFloor = true;



        Plane groundPlane = new Plane();

        public Color DirectionalLightColor { get; private set; }

        KinectSensor _sensor;
        MultiSourceFrameReader _reader;
        private BodyFrameReader bodyFrameReader = null;
        private CoordinateMapper coordinateMapper = null;

        private List<Tuple<JointType, JointType>> bones;
        private Body[] bodies = null;

        //2DMAP
        MapTile[,] maptiles;

        MeshGeometry3D meshGeometry = new MeshGeometry3D();
        MeshGeometry3D meshGeometryMapGreen = new MeshGeometry3D();
        MeshGeometry3D meshGeometryMapRed = new MeshGeometry3D();

        Dictionary<int, Media3D.Point3D> points = new Dictionary<int, Media3D.Point3D>(); // All depth points

        List<int> groundPointsIndexes = new List<int>();
        List<int> obstPointsIndexes= new List<int>();
        List<int> PointsIndexes = new List<int>();

        public MainWindow()
        {
            //InitializeComponent();
            //Title = "Simple Demo";
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            Thread kinectThread = new Thread(kinectT);
            kinectThread.Start();

            model1.Material = PhongMaterials.Red;

            //Light setup
            light1.Color = SharpDX.Color.White;
            light1.Direction = new Vector3(0, 0, 5);

            //2D MAP
            maptiles = new MapTile[mapSizeX, mapSizeZ];

            for (int i = 0; i < mapSizeX; i++)
            {
                for (int k = 0; k < mapSizeZ; k++)
                {
                    maptiles[i, k] = new MapTile(i, k);
                }
            }

            mapModelRed.Material = PhongMaterials.Red;
            mapModelGreen.Material = PhongMaterials.Green;
        }

        private void kinectT()
        {
            _sensor = KinectSensor.GetDefault();
            if (_sensor != null)
            {
                _sensor.Open();
                //_reader = _sensor.OpenMultiSourceFrameReader(FrameSourceTypes.Color | FrameSourceTypes.Depth | FrameSourceTypes.Infrared | FrameSourceTypes.Body);
                _reader = _sensor.OpenMultiSourceFrameReader(FrameSourceTypes.Depth | FrameSourceTypes.Body);
                _reader.MultiSourceFrameArrived += Reader_MultiSourceFrameArrived;

                coordinateMapper = _sensor.CoordinateMapper;

                // SKELETON DETECTION ->

                // get the depth (display) extents
                FrameDescription frameDescription = _sensor.DepthFrameSource.FrameDescription;
                this.bodyFrameReader = _sensor.BodyFrameSource.OpenReader();

                // a bone defined as a line between two joints
                this.bones = new List<Tuple<JointType, JointType>>();

                // a bone defined as a line between two joints
                this.bones = new List<Tuple<JointType, JointType>>();

                // Torso
                this.bones.Add(new Tuple<JointType, JointType>(JointType.Head, JointType.Neck));
                this.bones.Add(new Tuple<JointType, JointType>(JointType.Neck, JointType.SpineShoulder));
                this.bones.Add(new Tuple<JointType, JointType>(JointType.SpineShoulder, JointType.SpineMid));
                this.bones.Add(new Tuple<JointType, JointType>(JointType.SpineMid, JointType.SpineBase));
                this.bones.Add(new Tuple<JointType, JointType>(JointType.SpineShoulder, JointType.ShoulderRight));
                this.bones.Add(new Tuple<JointType, JointType>(JointType.SpineShoulder, JointType.ShoulderLeft));
                this.bones.Add(new Tuple<JointType, JointType>(JointType.SpineBase, JointType.HipRight));
                this.bones.Add(new Tuple<JointType, JointType>(JointType.SpineBase, JointType.HipLeft));

                // Right Arm
                this.bones.Add(new Tuple<JointType, JointType>(JointType.ShoulderRight, JointType.ElbowRight));
                this.bones.Add(new Tuple<JointType, JointType>(JointType.ElbowRight, JointType.WristRight));
                this.bones.Add(new Tuple<JointType, JointType>(JointType.WristRight, JointType.HandRight));
                this.bones.Add(new Tuple<JointType, JointType>(JointType.HandRight, JointType.HandTipRight));
                this.bones.Add(new Tuple<JointType, JointType>(JointType.WristRight, JointType.ThumbRight));

                // Left Arm
                this.bones.Add(new Tuple<JointType, JointType>(JointType.ShoulderLeft, JointType.ElbowLeft));
                this.bones.Add(new Tuple<JointType, JointType>(JointType.ElbowLeft, JointType.WristLeft));
                this.bones.Add(new Tuple<JointType, JointType>(JointType.WristLeft, JointType.HandLeft));
                this.bones.Add(new Tuple<JointType, JointType>(JointType.HandLeft, JointType.HandTipLeft));
                this.bones.Add(new Tuple<JointType, JointType>(JointType.WristLeft, JointType.ThumbLeft));

                // Right Leg
                this.bones.Add(new Tuple<JointType, JointType>(JointType.HipRight, JointType.KneeRight));
                this.bones.Add(new Tuple<JointType, JointType>(JointType.KneeRight, JointType.AnkleRight));
                this.bones.Add(new Tuple<JointType, JointType>(JointType.AnkleRight, JointType.FootRight));

                // Left Leg
                this.bones.Add(new Tuple<JointType, JointType>(JointType.HipLeft, JointType.KneeLeft));
                this.bones.Add(new Tuple<JointType, JointType>(JointType.KneeLeft, JointType.AnkleLeft));
                this.bones.Add(new Tuple<JointType, JointType>(JointType.AnkleLeft, JointType.FootLeft));
            }
        }

        private void Window_Closed(object sender, EventArgs e)
        {
            if (_reader != null) { _reader.Dispose(); }
            if (_sensor != null) { _sensor.Close(); }
        }

        private void Reader_MultiSourceFrameArrived(object sender, MultiSourceFrameArrivedEventArgs e)
        {
            DepthFrame depthFrame = null;
            BodyFrame bodyFrame = null;

            MultiSourceFrame multiSourceFrame = e.FrameReference.AcquireFrame();

            if (multiSourceFrame == null)
            {
                return;
            }

            try
            {
                depthFrame = multiSourceFrame.DepthFrameReference.AcquireFrame();
                bodyFrame = multiSourceFrame.BodyFrameReference.AcquireFrame();

                if ((depthFrame == null) || (bodyFrame == null))
                {
                    return;
                }

                // Do something with the frame...
                //camera.Source = ToBitmap(frame);

                processDepthFrame(depthFrame);
                processBodyFrame(bodyFrame);
            }

            finally
            {
                if (depthFrame != null)
                {
                    depthFrame.Dispose();
                }

                if (bodyFrame != null)
                {
                    bodyFrame.Dispose();
                }
            }
        }

        private void processDepthFrame(DepthFrame depthFrame)
        {
            var depthWidth = depthFrame.FrameDescription.Width;
            var depthHeight = depthFrame.FrameDescription.Height;
            var depthData = new ushort[depthWidth * depthHeight];
            var camerapoints = new CameraSpacePoint[depthData.Length];

            depthFrame.CopyFrameDataToArray(depthData);
            coordinateMapper.MapDepthFrameToCameraSpace(depthData, camerapoints);

            points.Clear();
            PointsIndexes.Clear();
            obstPointsIndexes.Clear();

            int max = 5;

            for (int i = 0; i < camerapoints.Length; i++)
            {
                var point = camerapoints[i];

                if (point.X < max && point.X > -max && point.Y < max && point.Y > -max && point.X < max && point.Z > -max)
                {
                    points.Add(i, new Media3D.Point3D(point.X, point.Y, point.Z));
                    PointsIndexes.Add(i);
                }

                i++;
            }
            Stopwatch watch = System.Diagnostics.Stopwatch.StartNew();

            if (updateFloor)
            {
                groundPlane = updateGround();
                updateFloor = false;
            }

            watch.Stop();

            //delete points under ground and over robot height

            double yLimit = groundPlane.Normal.Y;

            foreach (var p in points)
            {
                double temp = groundPlane.Normal.X * p.Value.X + groundPlane.Normal.Y * p.Value.Y + groundPlane.Normal.Z * p.Value.Z + groundPlane.D + 0.1;

                if (temp < 0 && temp + robotHeight > 0)
                {
                    obstPointsIndexes.Add(p.Key);
                }
            }

            switch (drawmode)
            {
                case "points":
                    drawPoints(PointsIndexes);
                    break;
                case "obstPoints":
                    drawPoints(obstPointsIndexes);
                    break;
            }

            drawMap(obstPointsIndexes);

            Dispatcher.Invoke(() => { Title = watch.ElapsedMilliseconds.ToString(); });
        }

        private Plane updateGround()
        {
            //RANSAC
            int iter = 100;
            double threshDist = 0.05;

            int iterations = 0;
            int bestCount = 0;

            Dictionary<int, Media3D.Point3D> bestSupport = new Dictionary<int, Media3D.Point3D>();

            Plane bestPlane = new Plane();
            Random rnd = new Random();

            groundPointsIndexes.Clear();

            foreach (var point in points)
            {
                if (point.Value.Y < 0)
                {
                    groundPointsIndexes.Add(point.Key);
                }
            }

            while (iterations < iter)
            {
                Media3D.Point3D p = points[groundPointsIndexes.ElementAt(rnd.Next(0, groundPointsIndexes.Count()))];
                Vector3 point1 = new Vector3((float)p.X, (float)p.Y, (float)p.Z);

                p = points[groundPointsIndexes.ElementAt(rnd.Next(0, groundPointsIndexes.Count()))];
                Vector3 point2 = new Vector3((float)p.X, (float)p.Y, (float)p.Z);

                p = points[groundPointsIndexes.ElementAt(rnd.Next(0, groundPointsIndexes.Count()))];
                Vector3 point3 = new Vector3((float)p.X, (float)p.Y, (float)p.Z);

                Plane plane = new Plane(point1, point2, point3);

                int count = 0;

                foreach (var index in groundPointsIndexes)
                {
                    if (Math.Abs(ComputeDistance(points[index], plane)) < threshDist)
                    {
                        count++;
                    }
                }

                if (count > bestCount)
                {
                    bestPlane = plane;
                    bestCount = count;
                }

                iterations++;
            }

            return bestPlane;
        }


        private void processBodyFrame(BodyFrame bodyFrame)
        {
            if (bodyFrame != null)
            {
                if (this.bodies == null)
                {
                    this.bodies = new Body[bodyFrame.BodyCount];
                }

                bodyFrame.GetAndRefreshBodyData(this.bodies);

                MeshBuilder meshBuilder = new MeshBuilder();

                foreach (Body body in this.bodies)
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

                            meshBuilder.AddBox(new Vector3((float)position.X, (float)position.Y, (float)position.Z), 0.05, 0.05, 0.05, BoxFaces.Top);

                        }
                    }
                }

                meshGeometry = meshBuilder.ToMeshGeometry3D();
                meshGeometry.Colors = new Color4Collection(meshGeometry.TextureCoordinates.Select(x => x.ToColor4()));

                Dispatcher.Invoke(() => { skeletonModel.Geometry = meshGeometry; });
                Dispatcher.Invoke(() => { skeletonModel.Material = PhongMaterials.Yellow; });
            }
        }

        private float ComputeDistance(Media3D.Point3D point, Plane plane)
        {
            Vector3 vector = new Vector3((float)point.X, (float)point.Y, (float)point.Z);

            return Vector3.Dot(plane.Normal, vector) + plane.D;
        }

        private void drawPoints(List<int> p)
        {
            MeshBuilder meshBuilder = new MeshBuilder();

            foreach (int index in p)
            {
                meshBuilder.AddBox(new Vector3((float)points[index].X, (float)points[index].Y, (float)points[index].Z), 0.005, 0.005, 0.005, BoxFaces.All);
            }


            meshGeometry = meshBuilder.ToMeshGeometry3D();
            meshGeometry.Colors = new Color4Collection(meshGeometry.TextureCoordinates.Select(x => x.ToColor4()));

            Dispatcher.Invoke(() =>
            {
                model1.Geometry = meshGeometry;
            });
        }

        private void drawMap(List<int> p)
        {
            MeshBuilder meshBuilderRed = new MeshBuilder();
            MeshBuilder meshBuilderGreen = new MeshBuilder();

            for (int x = 0; x < maptiles.GetLength(0); x++)
            {
                for (int z = 0; z < maptiles.GetLength(1); z++)
                {
                    maptiles[x, z].Color = "green";
                }
            }

            foreach (var index in p)
            {
                Vector3 pos = new Vector3((float)points[index].X, (float)points[index].Y, (float)points[index].Z);

                Vector2 tilepos = new Vector2(Convert.ToInt32(pos.X * mapZoom + mapSizeX/2), Convert.ToInt32(pos.Z* mapZoom));

                if (tilepos.X >= 0 && tilepos.X < mapSizeX && tilepos.Y >= 0 && tilepos.Y < mapSizeZ)
                {
                    maptiles[(int)tilepos.X, (int)tilepos.Y].Color = "red";
                }
            }

            for (int x = 0; x < mapSizeX; x++)
            {
                for (int z = 0; z < mapSizeZ; z++)
                {
                    if (x < z + mapSizeZ / 2 && x > -z + mapSizeZ/2 && z > 10)
                    {
                        Vector3 vect = new Vector3((float)x / 10 - 5, -1, (float)z / 10 - 5);

                        if (maptiles[x, z].Color == "red")
                        {
                            
                            meshBuilderRed.AddBox(vect, 0.08, 0.08, 0.08, BoxFaces.All);
                        }
                        else
                        {
                            meshBuilderGreen.AddBox(vect, 0.08, 0.08, 0.08, BoxFaces.All);
                        }
                    }
                    
                }
            }

            meshGeometryMapRed = meshBuilderRed.ToMeshGeometry3D();
            meshGeometryMapGreen = meshBuilderGreen.ToMeshGeometry3D();

            Dispatcher.Invoke(() =>
            {
                mapModelRed.Geometry = meshGeometryMapRed;
                mapModelGreen.Geometry = meshGeometryMapGreen;
            });
        }


        private ImageSource ToBitmap(DepthFrame frame)
        {
            var format = PixelFormats.Bgr32;
            int width = frame.FrameDescription.Width;
            int height = frame.FrameDescription.Height;

            ushort minDepth = frame.DepthMinReliableDistance;
            ushort maxDepth = frame.DepthMaxReliableDistance;

            ushort[] depthData = new ushort[width * height];
            byte[] pixelData = new byte[width * height * (PixelFormats.Bgr32.BitsPerPixel + 7) / 8];

            frame.CopyFrameDataToArray(depthData);

            int colorIndex = 0;
            for (int depthIndex = 0; depthIndex < depthData.Length; depthIndex++)
            {
                ushort depth = depthData[depthIndex];
                byte intensity = (byte)(depth >= minDepth && depth <= maxDepth ? depth / 18 : 0);

                pixelData[colorIndex++] = intensity; // Blue
                //pixelData[colorIndex++] = intensity; // Green
                //pixelData[colorIndex++] = intensity; // Red
                colorIndex++;
                colorIndex++;
                colorIndex++;
            }

            int stride = width * format.BitsPerPixel / 8;

            return BitmapSource.Create(width, height, 96, 96, format, null, pixelData, stride);

        }



        private void header_rawDepth_Click(object sender, RoutedEventArgs e)
        {
            drawmode = "points";
        }

        private void header_collisionOoints_Click(object sender, RoutedEventArgs e)
        {
            drawmode = "obstPoints";
        }

        private void header_showNone_Click(object sender, RoutedEventArgs e)
        {
            drawmode = "none";
        }

        private void btn_UpdateGroundPlane_Click(object sender, RoutedEventArgs e)
        {
            updateFloor = true;
        }
    }
}
