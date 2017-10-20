using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using GTA;
using GTA.Math;
using GTA.Native;
using JetBlast.Memory;
using JetBlast.Utility;

namespace JetBlast
{
    public class ScriptMain : Script
    {
        /// <summary>
        /// Maximum distance behind engine between which physics will be applied to entities
        /// </summary>
        private const float EngineBackThrustExtremityScalar = 47.0f;

        /// <summary>
        /// Maximum distance in-front of engine between which physics will be applied to entities
        /// </summary>
        private const float EngineThrustExtremityScalar = -4.6f;

        /// <summary>
        /// Scalar for engine thrust forces
        /// </summary>
        private const float ThrustScale = 0.80f;

        /// <summary>
        /// Scalar for engine reverse thrust forces
        /// </summary>
        private const float ReverseThrustScale = 0.34f;

        private readonly Dictionary<int, TrackedVehicleInfo> trackedVehicles = new Dictionary<int, TrackedVehicleInfo>();

        private readonly Dictionary<int, TrackedPedInfo> trackedPeds = new Dictionary<int, TrackedPedInfo>();

        public ScriptMain()
        {
            MemoryAccess.Init();
            Tick += OnTick;
        }

        private static bool DoEntityCapsuleTest(Vector3 start, Vector3 target, float radius, Entity ignore, out Entity hitEntity)
        {
            if (UserConfig.DebugMode)
                GameUtility.DrawLine(start, target, Color.DeepPink);

            var raycastResult = World.RaycastCapsule(start, target, radius, IntersectOptions.Everything, ignore);

            hitEntity = raycastResult.HitEntity;

            return raycastResult.DitHitEntity;
        }

        private static void ApplyThrustForce(Entity entity, Vector3 origin, Vector3 direction, float scale)
        {
            if (Function.Call<int>(Hash.GET_VEHICLE_CLASS, entity) == 16 || entity.HeightAboveGround > 15.0f) return;

            float entityDist = Vector3.Distance(entity.Position, origin);

            float zForce, scaleModifier;

            Vector3 rotationalForce;

            if (entity is Vehicle)
            {
                zForce = RandomEx.GetBoolean(0.50f) ? 0.0332f : 0.0318f;
                scaleModifier = 22.0f;
                rotationalForce = new Vector3(0.0f, 0.1f, 0.40f);
            }

            else if (entity is Ped)
            {
                if (((Ped)entity).IsRagdoll == false)
                    Function.Call(Hash.SET_PED_TO_RAGDOLL, entity.Handle, 800, 1500, 2, 1, 1, 0);
                zForce = 0.0034f;
                scaleModifier = 30.0f;
                rotationalForce = new Vector3(0.0f, 0.0f, 0.12f);
            }

            else
            {
                zForce = 0.000f;
                scaleModifier = 30.0f;
                rotationalForce = new Vector3(0.0f, 0.338f, 0.0f);
            }

            var force = (direction + new Vector3(0, 0, zForce)) * Math.Min(1.0f, scaleModifier / entityDist) * scale;

            entity.ApplyForce(force, rotationalForce, ForceType.MaxForceRot);
        }

        private static bool CanVehicleHaveEnginePhysics(Vehicle vehicle)
        {
            return vehicle.IsAlive &&
                   vehicle.EngineRunning &&
                   vehicle.HeightAboveGround < 24.0f;
        }

        private void StopTracking(Ped ped)
        {
            trackedPeds.Remove(ped.Handle);
        }

        private void StopTracking(Vehicle vehicle)
        {
            TrackedVehicleInfo info;

            if (!trackedVehicles.TryGetValue(vehicle.Handle, out info)) return;

            info.StopHeatHaze();

            trackedVehicles.Remove(vehicle.Handle);
        }

        private void UpdateEngineAnimTrigger(Vehicle vehicle, Vector3 position)
        {
            var peds = World.GetNearbyPeds(position, 4.0f);

            foreach (Ped ped in peds)
            {
                if (ped.Handle == Game.Player.Character.Handle) continue;

                if (Function.Call<bool>(Hash.GET_IS_TASK_ACTIVE, ped.Handle, 135))
                {
                    UI.Notify("anim scene");
                }

                if (!trackedPeds.ContainsKey(ped.Handle) && ped.IsOnFoot)
                {
                    ped.Task.ClearAllImmediately();

                    ped.AlwaysKeepTask = true;

                    ped.BlockPermanentEvents = true;

                    var sceneId = Function.Call<int>(Hash.CREATE_SYNCHRONIZED_SCENE, 0.0, 0.0, 0.0, 0.0, 0.0, 0.0, 2);
                    Function.Call(Hash.ATTACH_SYNCHRONIZED_SCENE_TO_ENTITY, sceneId, vehicle, Function.Call<int>(Hash.GET_ENTITY_BONE_INDEX_BY_NAME, vehicle.Handle, "exhaust_1"));
                    Function.Call(Hash.TASK_SYNCHRONIZED_SCENE, ped, sceneId, "MISSSOLOMON_3", "molly_death", 1.5, -8.0, 4, 0, 0x447a0000, 0);

                    trackedPeds.Add(ped.Handle, new TrackedPedInfo { AnimSceneID = sceneId });
                }

                else
                {
                    if (Function.Call<bool>(Hash.IS_ENTITY_PLAYING_ANIM, ped, "MISSSOLOMON_3", "molly_death", 3))
                    {
                        if (Function.Call<float>(Hash.GET_SYNCHRONIZED_SCENE_PHASE, trackedPeds[ped.Handle].AnimSceneID) > 0.89)
                        {
                            ped.Delete();

                            StopTracking(ped);

                            Function.Call(Hash.START_PARTICLE_FX_NON_LOOPED_ON_ENTITY, "scr_trev4_747_blood_impact", vehicle.Handle, 12.5793, 12.2, -7.094210147857666, 0.0, 0.0, 0.0, 1.0, 0, 0, 0);

                            Function.Call<int>(Hash.START_PARTICLE_FX_LOOPED_ON_ENTITY, "scr_trev4_747_exhaust_plane_misfire", vehicle.Handle, -12.6323, 8.1153, -7.0876, 0.0, 0.0, 0.0, 1.0, 0, 0, 0);

                            var soundId = Function.Call<int>(Hash.GET_SOUND_ID);

                            Function.Call(Hash.PLAY_SOUND_FROM_COORD, soundId, "Trevor_4_747_Man_Sucked_In", 938.6, -2984.1298828125, 15.47, 0, 0, 0, 0);
                        }
                    }
                }
            }
        }

        private void OnTick(object sender, EventArgs e)
        {
            foreach (var vehicle in World.GetAllVehicles())
            {
                if (Function.Call<int>(Hash.GET_VEHICLE_CLASS, vehicle) != 16) continue;

                if (!trackedVehicles.ContainsKey(vehicle.Handle) && CanVehicleHaveEnginePhysics(vehicle))
                {
                    var vehicleInfo = new TrackedVehicleInfo(vehicle);

                    vehicleInfo.StartHeatHaze();

                    trackedVehicles.Add(vehicle.Handle, vehicleInfo);
                }

                if (!CanVehicleHaveEnginePhysics(vehicle))
                {
                    StopTracking(vehicle);

                    continue;
                }

                trackedVehicles[vehicle.Handle].Update();
            }
        }

        private class TrackedVehicleInfo
        {
            private readonly VehicleSize size;
            private readonly Vector3[] offsets;
            private readonly int numEngines;

            public TrackedVehicleInfo(Vehicle v)
            {
                vehicle = v;
                size = GetVehicleSizeInternal(v);
                offsets = vehicle.EnumTurbineOffsets().ToArray();
                numEngines = offsets.Length;
            }

            public void StartHeatHaze()
            {
                if (UserConfig.HeatHazeStrength <= 0.0f) return;

                hazeFX = new LoopedParticle[offsets.Length];

                for (var f = 0; f < hazeFX.Length; f++)
                {
                    hazeFX[f] = new LoopedParticle("scr_solomon3", "scr_trev4_747_engine_heathaze");

                    if (!hazeFX[f].IsLoaded)
                    {
                        hazeFX[f].Load();
                    }

                    hazeFX[f].Start(vehicle, offsets[f] + new Vector3(0, -10.0f, 0), new Vector3(180, 0, 0), 2.0f * UserConfig.HeatHazeStrength, null);
                }
            }

            public void StopHeatHaze()
            {
                foreach (LoopedParticle p in hazeFX)
                {
                    p.Remove();
                }
            }

            public unsafe void Update()
            {
                var throttle = InteropUtility.ReadFloat(new IntPtr(vehicle.MemoryAddress) + MemoryAccess.ThrottleOffset);

                for (int i = 0; i < numEngines; i++)
                {
                    var backThrustDistance = EngineBackThrustExtremityScalar - 28.0f * (1.0f - throttle);

                    if (size == VehicleSize.Small)
                        backThrustDistance *= 0.6f;

                    else if (size == VehicleSize.Big)
                        backThrustDistance *= 1.4f;

                    Vector3 leftOffset = offsets[i];
                    Vector3 rightOffset = new Vector3(-leftOffset.X, leftOffset.Y, leftOffset.Z);
                    Vector3 forwardLeft =
                        vehicle.GetOffsetInWorldCoords(leftOffset + new Vector3(0, EngineThrustExtremityScalar, 0f));
                    Vector3 forwardRight =
                        vehicle.GetOffsetInWorldCoords(rightOffset + new Vector3(0, EngineThrustExtremityScalar, 0f));
                    Vector3 rearLeft =
                        vehicle.GetOffsetInWorldCoords(leftOffset - new Vector3(0, backThrustDistance, 0f));

                    var groundHeightLeft = World.GetGroundHeight(rearLeft);

                    Vector3 rearRight =
                        vehicle.GetOffsetInWorldCoords(rightOffset - new Vector3(0, backThrustDistance, 0f));

                    var groundHeightRight = World.GetGroundHeight(rearRight);

                    float scale = ThrustScale * UserConfig.ThrustMultiplier * throttle, bottomOffset;

                    Vector3 direction = Vector3.Normalize(rearLeft - forwardLeft);

                    if (vehicle.Acceleration < 0.0f ||
                        Marshal.ReadInt16(new IntPtr(vehicle.MemoryAddress) + MemoryAccess.GearOffset) <= 0)
                    {
                        scale = ReverseThrustScale * UserConfig.ReverseThrustMultiplier * throttle;
                        direction = -direction;
                    }

                    switch (size)
                    {
                        case VehicleSize.Big:
                            bottomOffset = 1.469f;
                            scale *= 1.32f;
                            break;
                        case VehicleSize.Med:
                            bottomOffset = 1.1f;
                            scale *= 1.0f;
                            break;
                        default:
                            bottomOffset = 1.0f;
                            scale *= 0.78f;
                            break;
                    }

                    Entity target;

                    float thrustRadius = 4.4f * UserConfig.ThrustRadius;

                    // left turbine
                    if (DoEntityCapsuleTest(forwardLeft, new Vector3(rearLeft.X, rearLeft.Y, groundHeightLeft),
                        thrustRadius, vehicle, out target))
                        ApplyThrustForce(target, forwardLeft, direction, scale);
                    // right turbine
                    if (DoEntityCapsuleTest(forwardRight, new Vector3(rearRight.X, rearRight.Y, groundHeightRight),
                        thrustRadius, vehicle, out target))
                        ApplyThrustForce(target, forwardRight, direction, scale);
                    // left side bottom
                    if (DoEntityCapsuleTest(forwardLeft - new Vector3(0, 0, bottomOffset),
                        rearLeft - new Vector3(0, 0, bottomOffset), thrustRadius, vehicle, out target))
                        ApplyThrustForce(target, forwardLeft, direction, scale);
                    // right side bottom
                    if (DoEntityCapsuleTest(forwardRight - new Vector3(0, 0, bottomOffset),
                        rearRight - new Vector3(0, 0, bottomOffset), thrustRadius, vehicle, out target))
                        ApplyThrustForce(target, forwardRight, direction, scale);

                    if (hazeFX?.Length > i)
                        hazeFX[i].SetEvolution("throttle", vehicle.Acceleration);

                    bool b = Game.IsControlPressed(0, Control.VehicleHandbrake);

                    Function.Call(Hash.SET_VEHICLE_BURNOUT, vehicle, b);

                    Function.Call(Hash.SET_VEHICLE_HANDBRAKE, vehicle, b);

                    if (UserConfig.UseThrottleExhaust && Game.IsControlJustPressed(0, Control.VehicleAccelerate))
                    {
                        Function.Call(Hash.REQUEST_NAMED_PTFX_ASSET, "scr_agencyheist");

                        Function.Call(Hash._SET_PTFX_ASSET_NEXT_CALL, "scr_agencyheist");

                        Function.Call(Hash.SET_PARTICLE_FX_NON_LOOPED_COLOUR, 1.0f, 1.0f, 1.0f);

                        Function.Call(Hash.SET_PARTICLE_FX_NON_LOOPED_ALPHA, 0.6f);

                        Function.Call(Hash.START_PARTICLE_FX_NON_LOOPED_AT_COORD, "scr_agency3a_door_hvy_trig",
                            forwardLeft.X, forwardLeft.Y, forwardLeft.Z, vehicle.Rotation.X, vehicle.Rotation.Y,
                            vehicle.Rotation.Z, 2f, 0, 0, 0);

                        Function.Call(Hash.START_PARTICLE_FX_NON_LOOPED_AT_COORD, "scr_agency3a_door_hvy_trig",
                            forwardRight.X, forwardRight.Y, forwardRight.Z, vehicle.Rotation.X, vehicle.Rotation.Y,
                            vehicle.Rotation.Z, 2f, 0, 0, 0);
                    }
                }
            }

            private static VehicleSize GetVehicleSizeInternal(Vehicle vehicle)
            {
                var size = vehicle.Model.GetDimensions().Length();
                if (size > 100.0f)
                    return VehicleSize.Big;
                return size > 40.0f ? VehicleSize.Med : VehicleSize.Small;
            }

            private readonly Vehicle vehicle;
            private LoopedParticle[] hazeFX;
        }

        private struct TrackedPedInfo
        {
            public int AnimSceneID;
        }

        private enum VehicleSize
        {
            Small,
            Med,
            Big
        }
    }
}
