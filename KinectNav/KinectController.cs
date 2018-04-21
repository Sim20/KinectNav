using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Kinect;

namespace KinectNav
{
    static class KinectController
    {
        static KinectSensor _sensor;
        static MultiSourceFrameReader _reader;
        static Thread kinectThread;
        static private BodyFrameReader bodyFrameReader = null;
        static private CoordinateMapper coordinateMapper = null;

        public static void Connect()
        {
            kinectThread = new Thread(kinectT);
            kinectThread.Start();
        }

        private static void kinectT()
        {
            _sensor = KinectSensor.GetDefault();

            if (_sensor != null)
            {
                _sensor.Open();
                //_reader = _sensor.OpenMultiSourceFrameReader(FrameSourceTypes.Color | FrameSourceTypes.Depth | FrameSourceTypes.Infrared | FrameSourceTypes.Body);
                _reader = _sensor.OpenMultiSourceFrameReader(FrameSourceTypes.Depth | FrameSourceTypes.Body);
                _reader.MultiSourceFrameArrived += Reader_MultiSourceFrameArrived;

                coordinateMapper = _sensor.CoordinateMapper;
            }

            else
            {
                // Connect to sensor failed
            }
        }

        private static void Reader_MultiSourceFrameArrived(object sender, MultiSourceFrameArrivedEventArgs e)
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

                MapDepthFrame(depthFrame);
                UpdateBody(bodyFrame);
                DrawController.DrawPoints();
                DrawController.DrawMap();
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

        private static void MapDepthFrame(DepthFrame depthFrame)
        {
            var depthWidth = depthFrame.FrameDescription.Width;
            var depthHeight = depthFrame.FrameDescription.Height;
            var depthData = new ushort[depthWidth * depthHeight];
            var camerapoints = new CameraSpacePoint[depthData.Length];

            depthFrame.CopyFrameDataToArray(depthData);
            coordinateMapper.MapDepthFrameToCameraSpace(depthData, camerapoints);

            DepthData.UpdatePoints(camerapoints);
        }

        public static void UpdateBody(BodyFrame bodyFrame)
        {
            if (bodyFrame != null)
            {
                if (BodyData.bodies == null)
                {
                    BodyData.bodies = new Body[bodyFrame.BodyCount];
                }

                bodyFrame.GetAndRefreshBodyData(BodyData.bodies);
            }
        }

        public static void Dispose()
        {
            if (_reader != null) { _reader.Dispose(); }
            if (_sensor != null) { _sensor.Close(); }
        }
    }
}
