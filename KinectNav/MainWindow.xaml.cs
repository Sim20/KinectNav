using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Color = System.Windows.Media.Color;
using Colors = System.Windows.Media.Colors;

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
        private const float InferredZPositionClamp = 0.1f;
        private const int mountHeight = 1; // m
        private const float robotHeight = 1.5f;

        public Color DirectionalLightColor { get; private set; }

        KinectSensor _sensor;
        MultiSourceFrameReader _reader;
        private BodyFrameReader bodyFrameReader = null;
        private CoordinateMapper coordinateMapper = null;

        private List<Tuple<JointType, JointType>> bones;
        private Body[] bodies = null;

        MeshGeometry3D meshGeometry = new MeshGeometry3D();
        //private Media3D.Point3DCollection points = new Media3D.Point3DCollection();

        Dictionary<int, Media3D.Point3D> points = new Dictionary<int, Media3D.Point3D>();
        Dictionary<int, Media3D.Point3D> groundPoints = new Dictionary<int, Media3D.Point3D>();

        public MainWindow()
        {
            //InitializeComponent();
            //Title = "Simple Demo";
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            Thread kinectThread = new Thread(kinect);
            kinectThread.Start();

            //Light setup
            light1.Color = SharpDX.Color.White;
            light1.Direction = new Vector3(0, 0, 5);
        }

        private void kinect()
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
                //processBodyFrame(bodyFrame);

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
            groundPoints.Clear();

            int max = 5;

            for (int i = 0; i < camerapoints.Length; i++)
            {
                var point = camerapoints[i];

                if (point.X < max && point.X > -max && point.Y < max && point.Y > -max && point.X < max && point.Z > -max)
                {
                    points.Add(i, new Media3D.Point3D(point.X, point.Y, point.Z));

                    if (point.Y < -mountHeight + 0.2)
                    {
                        groundPoints.Add(i, new Media3D.Point3D(point.X, point.Y, point.Z));
                    }
                }

                i++; // !!!! TEMP !!!!
            }

            //RANSAC
            int iter = 100;
            double threshDist = 0.05;

            int iterations = 0;
            int bestCount = 0;

            Dictionary<int, Media3D.Point3D> bestSupport = new Dictionary<int, Media3D.Point3D>();

            Plane bestPlane = new Plane();
            Random rnd = new Random();

            while (iterations < iter)
            {
                Media3D.Point3D p = groundPoints.ElementAt(rnd.Next(0, groundPoints.Count())).Value;
                Vector3 point1 = new Vector3((float)p.X, (float)p.Y, (float)p.Z);

                p = groundPoints.ElementAt(rnd.Next(0, groundPoints.Count())).Value;
                Vector3 point2 = new Vector3((float)p.X, (float)p.Y, (float)p.Z);

                p = groundPoints.ElementAt(rnd.Next(0, groundPoints.Count())).Value;
                Vector3 point3 = new Vector3((float)p.X, (float)p.Y, (float)p.Z);

                Plane plane = new Plane(point1, point2, point3);

                Dictionary<int, Media3D.Point3D> pts = new Dictionary<int, Media3D.Point3D>();
                int count = 0;

                foreach (var point in groundPoints)
                {
                    if (Math.Abs(ComputeDistance(point.Value, plane)) < threshDist)
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

            //delete points under ground

            double yLimit = bestPlane.Normal.Y;
            if (yLimit > 0)
            {
                yLimit = -yLimit + 0.1;
            }
            else
            {
                yLimit = yLimit + 0.1;
            }

            foreach (var point in points.Where(t => t.Value.Y < yLimit || t.Value.Y > robotHeight + yLimit).ToList())
            {
                points.Remove(point.Key);
            }

            Stopwatch watch = System.Diagnostics.Stopwatch.StartNew();

            drawPoints(points);

            watch.Stop();

            Dispatcher.Invoke(() => { Title = watch.ElapsedMilliseconds.ToString(); });
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

                            meshBuilder.AddBox(new Vector3((float)position.X, (float)position.Y, (float)position.Z), 0.05, 0.05, 0.05, BoxFaces.All);

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

        private void drawPoints(Dictionary<int, Media3D.Point3D> points)
        {
            MeshBuilder meshBuilder = new MeshBuilder();

            foreach (var point in points)
            {
                meshBuilder.AddBox(new Vector3((float)point.Value.X, (float)point.Value.Y, (float)point.Value.Z), 0.005, 0.005, 0.005, BoxFaces.All);
            }

            meshGeometry = meshBuilder.ToMeshGeometry3D();
            meshGeometry.Colors = new Color4Collection(meshGeometry.TextureCoordinates.Select(x => x.ToColor4()));

            Dispatcher.Invoke(() => { model1.Geometry = meshGeometry; });
            Dispatcher.Invoke(() => { model1.Material = PhongMaterials.Red; });
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
    }
}
