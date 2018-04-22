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
        private const float robotHeight = 1.5f;
        public static bool UpdateFloor { get; set; }

        public static Dictionary<int, Media3D.Point3D> allPoints = new Dictionary<int, Media3D.Point3D>();

        public static List<int> AllPointsIndexes = new List<int>();
        public static List<int> ObstPointsIndexes = new List<int>();
        public static List<int> GroundPointsIndexes = new List<int>();  

        static Plane groundPlane;

        public static void UpdatePoints(CameraSpacePoint[] camerapoints)
        {
            allPoints.Clear();
            AllPointsIndexes.Clear();
            ObstPointsIndexes.Clear();
            GroundPointsIndexes.Clear();

            int max = 5;

            for (int i = 0; i < camerapoints.Length; i++)
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
                UpdateGround();
                UpdateFloor = false;
            }

            //delete points under ground and over robot height

            double yLimit = groundPlane.Normal.Y;

            foreach (var p in allPoints)
            {
                double temp = groundPlane.Normal.X * p.Value.X + groundPlane.Normal.Y * p.Value.Y + groundPlane.Normal.Z * p.Value.Z + groundPlane.D + 0.05 ;

                if (temp < 0 && temp + robotHeight > 0)
                {
                    ObstPointsIndexes.Add(p.Key);
                }

                else if (temp > 0)
                {
                    GroundPointsIndexes.Add(p.Key);
                }
            }
        }

        private static void UpdateGround()             //RANSAC
        {
            int maxIterations = 100;
            double threshDist = 0.05;

            int iterations = 0;
            int bestCount = 0;

            Dictionary<int, Media3D.Point3D> bestSupport = new Dictionary<int, Media3D.Point3D>();
            List<int> RansacPointsIndexes = new List<int>();

            Plane bestPlane = new Plane();
            Random rnd = new Random();

            RansacPointsIndexes.Clear();

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

        private static float ComputeDistance(Media3D.Point3D point, Plane plane)
        {
            Vector3 vector = new Vector3((float)point.X, (float)point.Y, (float)point.Z);

            return Vector3.Dot(plane.Normal, vector) + plane.D;
        }
    }
}
