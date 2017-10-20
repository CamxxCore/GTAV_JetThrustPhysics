using System;
using GTA.Math;

namespace JetBlast.Utility
{
    public static class MathUtility
    {
        public static double RadToDeg(double rad)
        {
            return rad * 180.0 / Math.PI;
        }

        public static Vector3 InverseTransformDirection(Quaternion rotation, Vector3 vector)
        {
            return Quaternion.Invert(rotation) * vector;
        }

        public static Vector3 DirectionToRotation(Vector3 direction)
        {
            direction.Normalize();

            var x = Math.Atan2(direction.Z, Math.Sqrt(direction.Y * direction.Y + direction.X * direction.X));
            var y = 0;
            var z = -Math.Atan2(direction.X, direction.Y);

            return new Vector3
            {
                X = (float)RadToDeg(x),
                Y = (float)RadToDeg(y),
                Z = (float)RadToDeg(z)
            };
        }

        public static Vector3 Inverse(this Vector3 v)
        {
            return v * -1.0f;
        }
    }
}
