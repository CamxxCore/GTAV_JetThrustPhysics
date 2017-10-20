using System.Collections.Generic;
using System.Drawing;
using GTA;
using GTA.Math;
using GTA.Native;

namespace JetBlast.Utility
{
    public static class GameUtility
    {
        public static void DrawMarker(int type, Vector3 position, Vector3 direction, Vector3 rotation, Vector3 scale3D, Color color, bool animate = false, bool faceCam = false, bool rotate = false)
        {
            Function.Call(Hash.DRAW_MARKER, type, position.X, position.Y, position.Z, direction.X, direction.Y, direction.Z, rotation.X, rotation.Y, rotation.Z, scale3D.X, scale3D.Y, scale3D.Z, color.R, color.G, color.B, color.A, animate, faceCam, 2, rotate, 0, 0, 0);
        }

        public static void DrawMarker(Vector3 position)
        {
            DrawMarker(2, position, Vector3.Zero, Vector3.Zero, new Vector3(2.0f, 2.0f, 2.0f), Color.Yellow);
        }

        public static void DrawLine(Vector3 start, Vector3 end, Color color)
        {
            Function.Call(Hash.DRAW_LINE, start.X, start.Y, start.Z, end.X, end.Y, end.Z, color.R, color.G, color.B, color.A);
        }

        public static IEnumerable<Vector3> EnumTurbineOffsets(this Vehicle vehicle)
        {
            if (Function.Call<int>(Hash.GET_VEHICLE_CLASS, vehicle) != 16)
                yield return Vector3.Zero;

            int boneIndex;

            if ((boneIndex = Function.Call<int>(Hash.GET_ENTITY_BONE_INDEX_BY_NAME, vehicle.Handle, "afterburner")) != -1)
            {
                Vector3 p = Function.Call<Vector3>(Hash.GET_WORLD_POSITION_OF_ENTITY_BONE, vehicle.Handle, boneIndex);
                yield return Function.Call<Vector3>(Hash.GET_OFFSET_FROM_ENTITY_GIVEN_WORLD_COORDS, vehicle.Handle, p.X, p.Y, p.Z);
            }

            for (int i = 0; i < 6; i+=2)
            {
                if ((boneIndex = Function.Call<int>(Hash.GET_ENTITY_BONE_INDEX_BY_NAME, vehicle.Handle, $"exhaust_{i}")) != -1)
                {
                    Vector3 p = Function.Call<Vector3>(Hash.GET_WORLD_POSITION_OF_ENTITY_BONE, vehicle.Handle, boneIndex);
                    yield return Function.Call<Vector3>(Hash.GET_OFFSET_FROM_ENTITY_GIVEN_WORLD_COORDS, vehicle.Handle, p.X, p.Y, p.Z);
                }
            }
        }
    }
}
