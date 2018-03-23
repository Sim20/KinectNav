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
        private Media3D.Point3DCollection points = new Media3D.Point3DCollection();
        private Media3D.Point3DCollection groundPoints = new Media3D.Point3DCollection();


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

                    foreach (CameraSpacePoint point in camerapoints)
                    {
                        int max = 50;
                        if (point.X < max && point.X > -max && point.Y < max && point.Y > -max && point.X < max && point.Z > -max)
                        {
                            points.Add(new Media3D.Point3D(point.X, point.Y, point.Z));
                            if (point.Y < -mountHeight + 0.4)
                            {
                                groundPoints.Add(new Media3D.Point3D(point.X, point.Y, point.Z));
                            }
                        }
                    }

                    Media3D.Point3DCollection validGroungPoints = new Media3D.Point3DCollection();

                    if (frozen == false)
                    {
                        drawPoints(groundPoints);
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

        private void drawPoints(Media3D.Point3DCollection points)
        {

            HelixToolkit.Wpf.SharpDX.MeshBuilder meshBuilder = new HelixToolkit.Wpf.SharpDX.MeshBuilder();

            for (int i = 0; i < points.Count; i = i + 1)
            {
                meshBuilder.AddBox(new Vector3((float)points[i].X, (float)points[i].Y, (float)points[i].Z), 0.01, 0.01, 0.01, HelixToolkit.Wpf.SharpDX.BoxFaces.All);
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
