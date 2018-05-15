using System;
using System.Threading;

using Microsoft.Kinect;

namespace KinectNav
{
    static class KinectController
    {
        /// <summary> Active sensor </summary>
        public static KinectSensor _sensor;

        /// <summary> Reader for depth/body index frames </summary>
        static MultiSourceFrameReader _reader;
        
        /// <summary> Kinect data processing thread </summary>
        static Thread kinectThread;

        /// <summary> Coordinate mapper to map one type of point to another </summary>
        private static CoordinateMapper coordinateMapper = null;

        /// <summary>
        /// Creates Kinect thread
        /// </summary>
        public static void Connect()
        {
            kinectThread = new Thread(KinectT);
            kinectThread.Start();
        }

        /// <summary>
        /// Kinect data processing thread
        /// </summary>
        private static void KinectT()
        {
            _sensor = KinectSensor.GetDefault();

            if (_sensor != null)
            {
                // connect to sensor, register callback
                _sensor.Open();
                _reader = _sensor.OpenMultiSourceFrameReader(FrameSourceTypes.Depth | FrameSourceTypes.Body);
                _reader.MultiSourceFrameArrived += Reader_MultiSourceFrameArrived;

                coordinateMapper = _sensor.CoordinateMapper;
            }

            else
            {
                // Connect to sensor failed
            }
        }

        /// <summary>
        /// Handles depth/color/body index frame data arriving from sensor
        /// </summary>
        /// <param name="sender">object sending the event</param>
        /// <param name="e">event arguments</param>
        private static void Reader_MultiSourceFrameArrived(object sender, MultiSourceFrameArrivedEventArgs e)
        {
            DepthFrame depthFrame = null;
            BodyFrame bodyFrame = null;

            MultiSourceFrame multiSourceFrame = e.FrameReference.AcquireFrame();

            // if the Frame has expired by the time we process this event, return.
            if (multiSourceFrame == null)
            {
                return;
            }

            // try/finally to ensure that we clean up before we exit the function.  
            try
            {
                depthFrame = multiSourceFrame.DepthFrameReference.AcquireFrame();
                bodyFrame = multiSourceFrame.BodyFrameReference.AcquireFrame();

                if ((depthFrame == null) || (bodyFrame == null))
                {
                    return;
                }

                // frame data processing

                MapDepthFrame(depthFrame);
                UpdateBodyData(bodyFrame);

                DrawController.DrawPoints();
                DrawController.DrawMap();
                DrawController.DrawJoints();
                DrawController.UpdateGestureResult();
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

        /// <summary>
        /// Handles depth data from sensor
        /// </summary>
        /// <param name="depthFrame"></param> Raw depth frame
        private static void MapDepthFrame(DepthFrame depthFrame)
        {
            // map raw depth points to camera space
            var depthWidth = depthFrame.FrameDescription.Width;
            var depthHeight = depthFrame.FrameDescription.Height;
            var depthData = new ushort[depthWidth * depthHeight];
            var camerapoints = new CameraSpacePoint[depthData.Length];

            depthFrame.CopyFrameDataToArray(depthData);
            coordinateMapper.MapDepthFrameToCameraSpace(depthData, camerapoints);

            // update depth point clouds
            DepthData.UpdatePoints(camerapoints);
        }

        /// <summary>
        /// Handels body data from sensor
        /// </summary>
        /// <param name="bodyFrame"></param> Raw body frame
        public static void UpdateBodyData(BodyFrame bodyFrame)
        {
            bool BodyDataReceived = false;

            if (bodyFrame != null)
            {
                if (BodyData.Bodies == null)
                {   
                    BodyData.Bodies = new Body[bodyFrame.BodyCount]; 
                }

                bodyFrame.GetAndRefreshBodyData(BodyData.Bodies);
                
                BodyDataReceived = true;
            }

            if (BodyDataReceived)
            {
                BodyData.UpdateBodyData();
            }
        }

        public static void Dispose()
        {
            if (_reader != null) { _reader.Dispose(); }
            if (_sensor != null) { _sensor.Close(); }
        }
    }
}
