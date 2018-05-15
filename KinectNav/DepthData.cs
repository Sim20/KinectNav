using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Media3D = System.Windows.Media.Media3D;

using Microsoft.Kinect;

using SharpDX;

namespace KinectNav
{
    static class DepthData
    {
        /// <summary> Points above this height are not considered obstacles </summary>
        private const float robotHeight = 1.5f;

        /// <summary> True, if user requested floor plane opdate </summary>
        public static bool UpdateFloor = true;

        /// <summary> Dictionary of all depth points in the raw depth frame </summary>
        public static Dictionary<int, Media3D.Point3D> allPoints = new Dictionary<int, Media3D.Point3D>();

        /// <summary> Lists of point indexes - all points, obstacle points, ground points </summary>
        public static List<int> AllPointsIndexes = new List<int>();
        public static List<int> ObstPointsIndexes = new List<int>();
        public static List<int> GroundPointsIndexes = new List<int>();
        public static Plane groundPlane;

        /// <summary>
        /// Updates points lists
        /// </summary>
        /// <param name="camerapoints"></param> Raw depth frame points mapped to camera space
        public static void UpdatePoints(CameraSpacePoint[] camerapoints)
        {
            allPoints.Clear();
            AllPointsIndexes.Clear();
            ObstPointsIndexes.Clear();
            GroundPointsIndexes.Clear();

            int max = 5; // x,y,z position limit

            // update allPoints and AllPointsIndexes
            for (int i = 0; i < camerapoints.Length; i = i + 2)
            {
                var point = camerapoints[i];

                if (point.X < max && point.X > -max && point.Y < max && point.Y > -max && point.X < max && point.Z > -max)
                {
                    allPoints.Add(i, new Media3D.Point3D(point.X, point.Y, point.Z));
                    AllPointsIndexes.Add(i);
                }
            }

            if (UpdateFloor)
            {
                // run RANSAC, detect floor
                UpdateGround();
                UpdateFloor = false;
            }

            // filter points under ground and over robot height, update ObstPointsIndexes and GroundPointsIndexes

            foreach (var p in allPoints)
            {
                Vector3 B = new Vector3((float)p.Value.X, (float)p.Value.Y, (float)p.Value.Z);

                double temp = (Vector3.Dot(B, groundPlane.Normal)) + groundPlane.D;

                if (groundPlane.D > 0)
                {
                    temp = -temp;
                }

                if (temp + 0.07 < 0 && temp + robotHeight > 0)
                {
                    ObstPointsIndexes.Add(p.Key);
                }

                else if (temp + 0.07 > 0)
                {
                    GroundPointsIndexes.Add(p.Key);
                }
            }
        }

        /// <summary>
        /// RANSAC, detects and updates groundPlane
        /// </summary>
        private static void UpdateGround()    
        { 
            int maxIterations = 100;
            double threshDist = 0.05;

            int iterations = 0;
            int bestCount = 0;

            List<int> RansacPointsIndexes = new List<int>();

            Plane bestPlane = new Plane();
            Random rnd = new Random();

            foreach (var point in allPoints)
            {
                if (point.Value.Y < 0 + robotHeight)
                {
                    RansacPointsIndexes.Add(point.Key);
                }
            }

            while (iterations < maxIterations)
            {
                Media3D.Point3D p = allPoints[RansacPointsIndexes.ElementAt(rnd.Next(0, RansacPointsIndexes.Count()))];
                Vector3 point1 = new Vector3((float)p.X, (float)p.Y, (float)p.Z);

                p = allPoints[RansacPointsIndexes.ElementAt(rnd.Next(0, RansacPointsIndexes.Count()))];
                Vector3 point2 = new Vector3((float)p.X, (float)p.Y, (float)p.Z);

                p = allPoints[RansacPointsIndexes.ElementAt(rnd.Next(0, RansacPointsIndexes.Count()))];
                Vector3 point3 = new Vector3((float)p.X, (float)p.Y, (float)p.Z);

                Plane plane = new Plane(point1, point2, point3);

                int count = 0;

                foreach (var index in RansacPointsIndexes)
                {
                    if (Math.Abs(ComputeDistance(allPoints[index], plane)) < threshDist)
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

            groundPlane = bestPlane;
        }

        /// <summary>
        /// Returns distance between point and plane
        /// </summary>
        private static float ComputeDistance(Media3D.Point3D point, Plane plane) 
        {
            Vector3 vector = new Vector3((float)point.X, (float)point.Y, (float)point.Z);
            return Vector3.Dot(plane.Normal, vector) + plane.D;
        }
    }
}
