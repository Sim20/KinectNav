using System;
using System.Windows;
using HelixToolkit.Wpf.SharpDX;
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
            mapModelYellow.Material = PhongMaterials.Yellow;
            skeletonModel.Material = PhongMaterials.Yellow;
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
