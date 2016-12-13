using System;
using System.Collections.Generic;
using System.Linq;
using GTA;
using GTA.Math;
using GTA.Native;

namespace JetThrustPhysics
{
    public class ScriptMain : Script
    {
        /// <summary>
        /// Maximum distance from engine between which physics will be applied to entities
        /// </summary>
        private const float ThrustExtremityScalar = 33.0f;

        /// <summary>
        /// Scalar for engine thrust forces
        /// </summary>
        private const float ThrustScale = 0.001f;

        /// <summary>
        /// Radius for thrust zone raycast checking
        /// </summary>
        private const float ThrustRadius = 2.566f;

        private readonly Ped Player = Game.Player.Character;

        private Dictionary<int, KnownVehicleInfo> KnownVehicles = new Dictionary<int, KnownVehicleInfo>();

        private Entity thrustForceTarget = null;

        public ScriptMain()
        {
            Tick += OnTick;
        }

        private bool GetEntityShapeTestCapsuleResult(Vector3 start, Vector3 target, float radius, Entity ignore, out Entity hitEntity)
        {
            var cast = World.RaycastCapsule(start, target, radius, IntersectOptions.Everything, ignore);

            hitEntity = cast.HitEntity;

            return cast.DitHitEntity;
        }

        private void ApplyThrustForce(Entity entity, Vector3 origin, Vector3 direction, float scale)
        {
            if (Function.Call<int>(Hash.GET_VEHICLE_CLASS, entity) == 16) return;

            float entityDist = (entity.Position - origin).Length();

            var force = (direction + new Vector3(0, 0, 0.12f) * (23.0f / entityDist)) * scale;

            if (entity is Ped && (entity as Ped).IsRagdoll == false)
                Function.Call(Hash.SET_PED_TO_RAGDOLL, entity.Handle, 800, 1500, 2, 1, 1, 0);

            entity.ApplyForce(force, new Vector3(0.0f, 0.0f, 1.0f), ForceType.MaxForceRot2);
        }

        private VehicleSize GetVehicleSize(Vehicle vehicle)
        {
            var size = vehicle.Model.GetDimensions().Length();

            if (size > 100.0f)
                return VehicleSize.Big;
            if (size > 40.0f)
                return VehicleSize.Med;
            else
                return VehicleSize.Small;
        }

        private bool IsVehicleValid(Vehicle vehicle)
        {
            return vehicle.IsAlive && vehicle.EngineRunning;
        }

        private void RemoveVehicle(Vehicle vehicle)
        {
            KnownVehicles.Remove(vehicle.Handle);
        }

        private void OnTick(object sender, EventArgs e)
        {
            foreach (var vehicle in World.GetAllVehicles())
            {
                if (Function.Call<int>(Hash.GET_VEHICLE_CLASS, vehicle) == 16)
                {
                    if (!KnownVehicles.ContainsKey(vehicle.Handle) && IsVehicleValid(vehicle))
                    {
                        var turbineOffsets = Utility.EnumTurbineOffsets(vehicle);

                        KnownVehicles.Add(vehicle.Handle, new KnownVehicleInfo()
                        {
                            Size = GetVehicleSize(vehicle),
                            Offsets = turbineOffsets.ToArray()
                        });
                    }             

                    if (!IsVehicleValid(vehicle))
                    {
                        RemoveVehicle(vehicle);
                        continue;
                    }

                    var info = KnownVehicles[vehicle.Handle];

                    for (int i = 0; i < info.Offsets.Length; i++)
                    {
                        Vector3 leftOrigin = vehicle.GetOffsetInWorldCoords(info.Offsets[i] + new Vector3(0, 1.2f, 0));

                        Vector3 rightOffset = new Vector3(-info.Offsets[i].X, info.Offsets[i].Y, info.Offsets[i].Z);

                        Vector3 rightOrigin = vehicle.GetOffsetInWorldCoords(rightOffset + new Vector3(0, 1.2f, 0));

                        Vector3 leftDestination = vehicle.GetOffsetInWorldCoords(info.Offsets[i] - new Vector3(0, ThrustExtremityScalar, 0f));

                        leftDestination.Z = World.GetGroundHeight(leftDestination);
                                
                        Vector3 rightDestination = vehicle.GetOffsetInWorldCoords(rightOffset - new Vector3(0, ThrustExtremityScalar, 0f));

                        rightDestination.Z = World.GetGroundHeight(rightDestination);

                        Vector3 direction = Vector3.Normalize(leftDestination - leftOrigin);

                        float scale = ThrustScale * vehicle.Acceleration;

                        if (scale <= 0.2f)
                            scale = 0.2f;

                        if (info.Size == VehicleSize.Big)
                            scale += 0.6f;

                        else if (info.Size == VehicleSize.Med)
                            scale += 0.3f;

                        if (GetEntityShapeTestCapsuleResult(leftOrigin, leftDestination, ThrustRadius, vehicle, out thrustForceTarget)) // left turbine
                            ApplyThrustForce(thrustForceTarget, leftOrigin, direction, scale);

                        if (GetEntityShapeTestCapsuleResult(rightOrigin, rightDestination, ThrustRadius, vehicle, out thrustForceTarget)) // right turbine
                            ApplyThrustForce(thrustForceTarget, rightOrigin, direction, scale);

                        if (GetEntityShapeTestCapsuleResult(leftOrigin - new Vector3(0, 0, 2.0f), leftDestination - new Vector3(0, 0, 2.0f), ThrustRadius, vehicle, out thrustForceTarget)) // left side bottom
                            ApplyThrustForce(thrustForceTarget, leftOrigin - new Vector3(0, 0, 2.0f), direction, scale);

                        if (GetEntityShapeTestCapsuleResult(rightOrigin - new Vector3(0, 0, 2.0f), rightDestination - new Vector3(0, 0, 2.0f), ThrustRadius, vehicle, out thrustForceTarget)) // right side bottom
                            ApplyThrustForce(thrustForceTarget, rightOrigin - new Vector3(0, 0, 2.0f), direction, scale);
                    }                            
                }
            }
        }

        protected override void Dispose(bool A_0)
        {
            base.Dispose(A_0);
        }

        struct KnownVehicleInfo
        {
            public VehicleSize Size;
            public Vector3[] Offsets;
        }

        enum VehicleSize
        {
            Small,
            Med,
            Big
        }
    }
}
