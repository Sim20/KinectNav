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
        internal static MainWindow main;

        public MainWindow()
        {
            InitializeComponent();
            main = this;
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            //Light setup
            Title = "KinectNav";
            light1.Color = SharpDX.Color.White;
            light1.Direction = new Vector3(0, 0, 5);

            KinectController.Connect();

            model1.Material = PhongMaterials.Red;
            mapModelRed.Material = PhongMaterials.Red;
            mapModelGreen.Material = PhongMaterials.Green;
        }

        private void Window_Closed(object sender, EventArgs e)
        {
            KinectController.Dispose();
        }

        private void header_rawDepth_Click(object sender, RoutedEventArgs e)
        {
            DrawController.DrawMode = "points";
        }

        private void header_collisionOoints_Click(object sender, RoutedEventArgs e)
        {
            DrawController.DrawMode = "obstPoints";
        }

        private void header_showNone_Click(object sender, RoutedEventArgs e)
        {
            DrawController.DrawMode = "none";
        }

        private void btn_UpdateGroundPlane_Click(object sender, RoutedEventArgs e)
        {
            DepthData.UpdateFloor = true;
        }
    }
}
