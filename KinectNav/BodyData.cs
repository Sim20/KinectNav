using System;
using System.Collections.Generic;
using Media3D = System.Windows.Media.Media3D;
using Microsoft.Kinect;
using System.Windows;
using SharpDX;

namespace KinectNav
{
    static class BodyData
    {
        public static Body[] bodies { get; set; }

        private const float InferredZPositionClamp = 0.1f;

        public static List<GestureDetector> gestureDetectorList { get; }
        public static List<Tuple<JointType, JointType>> bones;

        public static List<Media3D.Point3D> FootPoints { get; }

        public static Dictionary<JointType, Vector3> jointPoints = new Dictionary<JointType, Vector3>();

        static BodyData()
        {
            FootPoints = new List<Media3D.Point3D>();
            gestureDetectorList = new List<GestureDetector>();

            int maxBodies = KinectController._sensor.BodyFrameSource.BodyCount;

            // create a gesture detector for each body (6 bodies => 6 detectors) and create content controls to display results in the UI
            for (int i = 0; i < maxBodies; ++i)
            {
                GestureResult result = new GestureResult(i, false, false, 0.0f);
                GestureDetector detector = new GestureDetector(KinectController._sensor, result);
                gestureDetectorList.Add(detector);
            }

            // SKELETON DETECTION ->

            // a bone defined as a line between two joints
            bones = new List<Tuple<JointType, JointType>>();

            // Torso
            bones.Add(new Tuple<JointType, JointType>(JointType.Head, JointType.Neck));
            bones.Add(new Tuple<JointType, JointType>(JointType.Neck, JointType.SpineShoulder));
            bones.Add(new Tuple<JointType, JointType>(JointType.SpineShoulder, JointType.SpineMid));
            bones.Add(new Tuple<JointType, JointType>(JointType.SpineMid, JointType.SpineBase));
            bones.Add(new Tuple<JointType, JointType>(JointType.SpineShoulder, JointType.ShoulderRight));
            bones.Add(new Tuple<JointType, JointType>(JointType.SpineShoulder, JointType.ShoulderLeft));
            bones.Add(new Tuple<JointType, JointType>(JointType.SpineBase, JointType.HipRight));
            bones.Add(new Tuple<JointType, JointType>(JointType.SpineBase, JointType.HipLeft));

            // Right Arm
            bones.Add(new Tuple<JointType, JointType>(JointType.ShoulderRight, JointType.ElbowRight));
            bones.Add(new Tuple<JointType, JointType>(JointType.ElbowRight, JointType.WristRight));
            bones.Add(new Tuple<JointType, JointType>(JointType.WristRight, JointType.HandRight));
            bones.Add(new Tuple<JointType, JointType>(JointType.HandRight, JointType.HandTipRight));
            bones.Add(new Tuple<JointType, JointType>(JointType.WristRight, JointType.ThumbRight));

            // Left Arm
            bones.Add(new Tuple<JointType, JointType>(JointType.ShoulderLeft, JointType.ElbowLeft));
            bones.Add(new Tuple<JointType, JointType>(JointType.ElbowLeft, JointType.WristLeft));
            bones.Add(new Tuple<JointType, JointType>(JointType.WristLeft, JointType.HandLeft));
            bones.Add(new Tuple<JointType, JointType>(JointType.HandLeft, JointType.HandTipLeft));
            bones.Add(new Tuple<JointType, JointType>(JointType.WristLeft, JointType.ThumbLeft));

            // Right Leg
            bones.Add(new Tuple<JointType, JointType>(JointType.HipRight, JointType.KneeRight));
            bones.Add(new Tuple<JointType, JointType>(JointType.KneeRight, JointType.AnkleRight));
            bones.Add(new Tuple<JointType, JointType>(JointType.AnkleRight, JointType.FootRight));

            // Left Leg
            bones.Add(new Tuple<JointType, JointType>(JointType.HipLeft, JointType.KneeLeft));
            bones.Add(new Tuple<JointType, JointType>(JointType.KneeLeft, JointType.AnkleLeft));
            bones.Add(new Tuple<JointType, JointType>(JointType.AnkleLeft, JointType.FootLeft));
        }

        public static void UpdateBodyData()
        {
            int maxBodies = KinectController._sensor.BodyFrameSource.BodyCount;

            if (bodies != null)
            {
                // loop through all bodies to see if any of the gesture detectors need to be updated
                for (int i = 0; i < maxBodies; ++i)
                {
                    Body b = bodies[i];
                    ulong trackingId = b.TrackingId;

                    // if the current body TrackingId changed, update the corresponding gesture detector with the new value
                    if (trackingId != gestureDetectorList[i].TrackingId)
                    {
                        gestureDetectorList[i].TrackingId = trackingId;

                        // if the current body is tracked, unpause its detector to get VisualGestureBuilderFrameArrived events
                        // if the current body is not tracked, pause its detector so we don't waste resources trying to get invalid gesture results
                        gestureDetectorList[i].IsPaused = trackingId == 0;
                    }
                }
            }

            foreach (Body body in bodies)
            {

                if (body.IsTracked)
                {
                    IReadOnlyDictionary<JointType, Joint> joints = body.Joints;

                    jointPoints.Clear();
                    FootPoints.Clear();

                    foreach (JointType joint in joints.Keys)
                    {
                        // sometimes the depth(Z) of an inferred joint may show as negative
                        // clamp down to 0.1f to prevent coordinatemapper from returning (-Infinity, -Infinity)

                        CameraSpacePoint position = joints[joint].Position;

                        if (position.Z < 0)
                        {
                            position.Z = InferredZPositionClamp;
                        }

                        jointPoints[joint] = new Vector3(position.X, position.Y, position.Z);

                        TrackingState trackingState = joints[joint].TrackingState;

                        if (joint == JointType.FootLeft || joint == JointType.FootRight)
                        {
                            FootPoints.Add(new Media3D.Point3D((float)position.X, (float)position.Y, (float)position.Z));
                        } 
                    }
                }
            }
        }
    }
}
