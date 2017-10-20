using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GTA;
using GTA.Math;
using GTA.Native;

namespace JetThrustPhysics.MaterialStuff.Helpers
{
    public class ShapeTestResult
    {
        public bool DidHit { get; private set; }
        public int HitEntity { get; private set; }
        public Vector3 HitPosition { get; private set; }
        public Vector3 HitNormal { get; private set; }
        public materials HitMaterial { get; private set; }

        public ShapeTestResult(bool didHit, int hitEntity, Vector3 hitPosition, Vector3 hitNormal, materials hitMaterial)
        {
            DidHit = didHit;
            HitEntity = hitEntity;
            HitPosition = hitPosition;
            HitNormal = hitNormal;
            HitMaterial = hitMaterial;
        }
    }

    public static class ShapeTestEx
    {
        public unsafe static ShapeTestResult RunShapeTest(Vector3 start, Vector3 end, Entity ignoreEntity, IntersectOptions options)
        {
            var shapeTest = Function.Call<int>(Hash._CAST_RAY_POINT_TO_POINT,
                start.X, start.Y, start.Z, end.X, end.Y, end.Z, (int)options, ignoreEntity, 7);

            bool didHit;

            int result, handle;

            float[] hitPosition = new float[6], hitNormal = new float[6];

            int material;

            fixed (float* position = hitPosition)
            fixed (float* normal = hitNormal)
            {
                result = Function.Call<int>((Hash)0x65287525D951F6BE, shapeTest, &didHit, position, normal, &material, &handle);
            }

            return new ShapeTestResult(didHit, handle, new Vector3(hitPosition[0], hitPosition[2], hitPosition[4]),
                new Vector3(hitNormal[0], hitNormal[2], hitNormal[4]), (materials)material);
        }
    }
}


