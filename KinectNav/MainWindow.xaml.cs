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

using Microsoft.Kinect;
using System.Windows.Media.Media3D;
using HelixToolkit.Wpf;

namespace KinectNav
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    /// 
    public partial class MainWindow : Window
    {
        private const int mountHeight = 1; // m

        KinectSensor _sensor;
        MultiSourceFrameReader _reader;
        private CoordinateMapper coordinateMapper = null;
        private Point3DCollection points = new Point3DCollection();

        private bool frozen = false;

        public MainWindow()
        {
            InitializeComponent();
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
            if (_reader != null){ _reader.Dispose();}
            if (_sensor != null){_sensor.Close();}
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
                            Point3D temp = new Point3D(point.X, point.Y, point.Z);
                            points.Add(temp);
                        }
                    }

                    if (frozen == false)
                    {
                        var modelGroup = new Model3DGroup();
                        var meshBuilder = new MeshBuilder(false, false);

                        meshBuilder.AddBox(new Rect3D(0, 0, 0, 0.005, 0.005, 0.005));

                        var greenMaterial = MaterialHelper.CreateMaterial(Colors.Green);
                        var redMaterial = MaterialHelper.CreateMaterial(Colors.Red);
                        var blueMaterial = MaterialHelper.CreateMaterial(Colors.Blue);
                        var insideMaterial = MaterialHelper.CreateMaterial(Colors.Yellow);

                        var children = modelGroup.Children;
                        children.Clear();
                        var mat = redMaterial;

                        MeshGeometry3D geometryMesh = new MeshGeometry3D();

                        for (int i = 0; i < points.Count; i = i + 1)
                        {
                            if (points[i].Y < - mountHeight + 0.2)
                            {
                                meshBuilder.AddBox(new Rect3D(points[i].X, points[i].Y, points[i].Z, 0.005, 0.005, 0.005));
                            }
                            else
                            {
                                //mat = blueMaterial;
                            }

                           // children.Add(new GeometryModel3D { Geometry = mesh, Transform = new TranslateTransform3D(points[i].X, points[i].Y, points[i].Z), Material = mat, BackMaterial = insideMaterial });
                        }

                        children.Add(new GeometryModel3D { Geometry = meshBuilder.ToMesh(true), Material = redMaterial, BackMaterial = insideMaterial });
                        modelGroup.Children = children;
                        MOD.Content = modelGroup;

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

        private void AddCubeToMesh(MeshGeometry3D mesh, Point3D center, double size)
        {
            if (mesh != null)
            {
                int offset = mesh.Positions.Count;

                mesh.Positions.Add(new Point3D(center.X - size, center.Y + size, center.Z - size));
                mesh.Positions.Add(new Point3D(center.X + size, center.Y + size, center.Z - size));
                mesh.Positions.Add(new Point3D(center.X + size, center.Y + size, center.Z + size));
                mesh.Positions.Add(new Point3D(center.X - size, center.Y + size, center.Z + size));
                mesh.Positions.Add(new Point3D(center.X - size, center.Y - size, center.Z - size));
                mesh.Positions.Add(new Point3D(center.X + size, center.Y - size, center.Z - size));
                mesh.Positions.Add(new Point3D(center.X + size, center.Y - size, center.Z + size));
                mesh.Positions.Add(new Point3D(center.X - size, center.Y - size, center.Z + size));

                mesh.TriangleIndices.Add(offset + 3);
                mesh.TriangleIndices.Add(offset + 2);
                mesh.TriangleIndices.Add(offset + 6);

                mesh.TriangleIndices.Add(offset + 3);
                mesh.TriangleIndices.Add(offset + 6);
                mesh.TriangleIndices.Add(offset + 7);

                mesh.TriangleIndices.Add(offset + 2);
                mesh.TriangleIndices.Add(offset + 1);
                mesh.TriangleIndices.Add(offset + 5);

                mesh.TriangleIndices.Add(offset + 2);
                mesh.TriangleIndices.Add(offset + 5);
                mesh.TriangleIndices.Add(offset + 6);

                mesh.TriangleIndices.Add(offset + 1);
                mesh.TriangleIndices.Add(offset + 0);
                mesh.TriangleIndices.Add(offset + 4);

                mesh.TriangleIndices.Add(offset + 1);
                mesh.TriangleIndices.Add(offset + 4);
                mesh.TriangleIndices.Add(offset + 5);

                mesh.TriangleIndices.Add(offset + 0);
                mesh.TriangleIndices.Add(offset + 3);
                mesh.TriangleIndices.Add(offset + 7);

                mesh.TriangleIndices.Add(offset + 0);
                mesh.TriangleIndices.Add(offset + 7);
                mesh.TriangleIndices.Add(offset + 4);

                mesh.TriangleIndices.Add(offset + 7);
                mesh.TriangleIndices.Add(offset + 6);
                mesh.TriangleIndices.Add(offset + 5);

                mesh.TriangleIndices.Add(offset + 7);
                mesh.TriangleIndices.Add(offset + 5);
                mesh.TriangleIndices.Add(offset + 4);

                mesh.TriangleIndices.Add(offset + 2);
                mesh.TriangleIndices.Add(offset + 3);
                mesh.TriangleIndices.Add(offset + 0);

                mesh.TriangleIndices.Add(offset + 2);
                mesh.TriangleIndices.Add(offset + 0);
                mesh.TriangleIndices.Add(offset + 1);
            }
        }
    }
}
