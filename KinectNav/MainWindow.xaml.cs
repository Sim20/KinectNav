using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Numerics = System.Numerics;
using Color = System.Windows.Media.Color;
using Colors = System.Windows.Media.Colors;

using Microsoft.Kinect;
using Media3D = System.Windows.Media.Media3D;

using HelixToolkit.Wpf;
using HelixToolkit.Wpf.SharpDX;
using HelixToolkit.Wpf.SharpDX.Core;
using SharpDX;
using Color4 = SharpDX.Color4;


namespace KinectNav
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    /// 
    public partial class MainWindow : Window
    {
        private const int mountHeight = 1; // m

        public Color DirectionalLightColor { get; private set; }

        KinectSensor _sensor;
        MultiSourceFrameReader _reader;
        private CoordinateMapper coordinateMapper = null;

        private bool frozen = false;

        MeshGeometry3D meshGeometry = new MeshGeometry3D();
        //private Media3D.Point3DCollection points = new Media3D.Point3DCollection();

        Dictionary<int, Media3D.Point3D> points = new Dictionary<int, Media3D.Point3D>();
        Dictionary<int, Media3D.Point3D> groundPoints = new Dictionary<int, Media3D.Point3D>();


        public MainWindow()
        {
            //InitializeComponent();
            DirectionalLightColor = Colors.White;
            Title = "Simple Demo";
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            _sensor = KinectSensor.GetDefault();
            if (_sensor != null)
            {
                _sensor.Open();
                _reader = _sensor.OpenMultiSourceFrameReader(FrameSourceTypes.Color | FrameSourceTypes.Depth | FrameSourceTypes.Infrared | FrameSourceTypes.Body);
                _reader.MultiSourceFrameArrived += Reader_MultiSourceFrameArrived;
            }
        }

        private void Window_Closed(object sender, EventArgs e)
        {
            if (_reader != null) { _reader.Dispose(); }
            if (_sensor != null) { _sensor.Close(); }
        }

        void Reader_MultiSourceFrameArrived(object sender, MultiSourceFrameArrivedEventArgs e)
        {
            // Get a reference to the multi-frame
            var reference = e.FrameReference.AcquireFrame();

            // Open depth frame
            using (var frame = reference.DepthFrameReference.AcquireFrame())
            {
                if (frame != null)
                {
                    // Do something with the frame...
                    //camera.Source = ToBitmap(frame);

                    var depthWidth = frame.FrameDescription.Width;
                    var depthHeight = frame.FrameDescription.Height;
                    var depthData = new ushort[depthWidth * depthHeight];
                    var camerapoints = new CameraSpacePoint[depthData.Length];

                    frame.CopyFrameDataToArray(depthData);
                    coordinateMapper = _sensor.CoordinateMapper;
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
                    }

                    if (frozen == false)
                    {
                        //RANSAC
                        //num - the minimum number of points. For line fitting problem, num=2
                        //iter number of iterations
                        int iter = 100;
                        //threshDist threshold used to id a point that fits well
                        double threshDist = 0.04;
                        //d number of nearby points required

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

                            // PTS TO PLANE
                            Plane plane = new Plane(point1, point2, point3);

                            Dictionary<int, Media3D.Point3D> pts = new Dictionary<int, Media3D.Point3D>();
                            pts.Clear();

                            foreach (var point in groundPoints)
                            {
                                if (Math.Abs(ComputeDistance(point.Value, plane)) < threshDist)
                                {
                                    pts.Add(point.Key, point.Value);
                                }
                            }

                            if (pts.Count() > bestCount)
                            {
                                bestSupport = pts;
                                bestPlane = plane;
                                bestCount = pts.Count();
                            }
                            iterations++;
                        }

                        foreach (var point in bestSupport)
                        {
                            points.Remove(point.Key);
                        }

                        bestPlane.Normalize();
                        //grid.Geometry = LineBuilder.GenerateGrid(new Vector3(0, 1, 0), -5, 5, -5, 5);
                        drawPoints(points);

                        frozen = true;
                    }
                }
            }

            /*
            // Open infrared frame
            using (var frame = reference.InfraredFrameReference.AcquireFrame())
            {
                if (frame != null)
                {
                    // Do something with the frame...
                }
            }
            */
        }

        private float ComputeDistance(Media3D.Point3D point, Plane plane)
        {
            Vector3 vector = new Vector3((float)point.X, (float)point.Y, (float)point.Z);

            float dot = Vector3.Dot(plane.Normal, vector);
            float value = dot + plane.D;
            return value;
        }

        private void drawPoints(Dictionary<int, Media3D.Point3D> points)
        {

            HelixToolkit.Wpf.SharpDX.MeshBuilder meshBuilder = new HelixToolkit.Wpf.SharpDX.MeshBuilder();
            foreach (var point in points)
            {
                meshBuilder.AddBox(new Vector3((float)point.Value.X, (float)point.Value.Y, (float)point.Value.Z), 0.005, 0.005, 0.005, HelixToolkit.Wpf.SharpDX.BoxFaces.All);
            }

            meshGeometry = meshBuilder.ToMeshGeometry3D();
            meshGeometry.Colors = new Color4Collection(meshGeometry.TextureCoordinates.Select(x => x.ToColor4()));
            model1.Geometry = meshGeometry;
            model1.Material = PhongMaterials.White;
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
