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
        private const float ThrustScale = 0.872f;

        /// <summary>
        /// Scalar for engine reverse thrust forces
        /// </summary>
        private const float ReverseThrustScale = 0.4f;

        /// <summary>
        /// Radius for thrust zone raycast checking
        /// </summary>
        private const float ThrustRadius = 4.4f;

        private Dictionary<int, KnownVehicleInfo> knownVehicles = new Dictionary<int, KnownVehicleInfo>();

        private Dictionary<int, KnownPedInfo> knownPeds = new Dictionary<int, KnownPedInfo>();

        private Entity thrustForceTarget = null;

        public ScriptMain()
        {
          //  Function.Call(Hash.REQUEST_ANIM_DICT, "MISSSOLOMON_3");
          //  Function.Call(Hash.REQUEST_NAMED_PTFX_ASSET, "scr_solomon3");
          //  Function.Call(Hash.REQUEST_NAMED_PTFX_ASSET, "scr_solomon3");
          //  Function.Call(Hash.REQUEST_SCRIPT_AUDIO_BANK, "Trv_4_747", 0);

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
            if (Function.Call<int>(Hash.GET_VEHICLE_CLASS, entity) == 16 || entity.HeightAboveGround > 15.0f) return;
        
            float entityDist = (entity.Position - origin).Length();

            var force = (direction + new Vector3(0, 0, 0.0928f) * (32.0f / entityDist)) * scale;

            if (entity is Ped && (entity as Ped).IsRagdoll == false)
                Function.Call(Hash.SET_PED_TO_RAGDOLL, entity.Handle, 800, 1500, 2, 1, 1, 0);

            entity.ApplyForce(force, new Vector3(0.0f, 0.0f, 0.388f), ForceType.MaxForceRot2);

            if (entity.Velocity.Z >= 4.8f)
                entity.Velocity = new Vector3(entity.Velocity.X, entity.Velocity.Y, 4.8f);
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
            return vehicle.IsAlive && vehicle.EngineRunning && vehicle.HeightAboveGround < 24.0f;
        }

        private void RemoveVehicle(Vehicle vehicle)
        {
            knownVehicles.Remove(vehicle.Handle);
        }

        private void RemovePed(Ped ped)
        {
            knownPeds.Remove(ped.Handle);
        }

        private bool IsPedValid(Ped ped)
        {
            return ped.IsOnFoot;
        }

        private void UpdateEngineAnimTrigger(Vehicle vehicle, Vector3 position)
        {
            var peds = World.GetNearbyPeds(position, 4.0f);

            for (int i = 0; i < peds.Length; i++)
            {
                if (peds[i].Handle == Game.Player.Character.Handle) continue;

                var ped = peds[i];

              /*  if (Function.Call<bool>(Hash.GET_IS_TASK_ACTIVE, ped.Handle, 135))
                {
                    UI.Notify("anim scene");
                }*/

                if (!knownPeds.ContainsKey(ped.Handle) && IsPedValid(ped))
                {
                    ped.Task.ClearAllImmediately();

                    ped.AlwaysKeepTask = true;

                    ped.BlockPermanentEvents = true;

                    var sceneId = Function.Call<int>(Hash.CREATE_SYNCHRONIZED_SCENE, 0.0, 0.0, 0.0, 0.0, 0.0, 0.0, 2);
                    Function.Call(Hash.ATTACH_SYNCHRONIZED_SCENE_TO_ENTITY, sceneId, vehicle, Function.Call<int>(Hash.GET_ENTITY_BONE_INDEX_BY_NAME, vehicle.Handle, "exhaust_1"));
                    Function.Call(Hash.TASK_SYNCHRONIZED_SCENE, ped, sceneId, "MISSSOLOMON_3", "molly_death", 1.5, -8.0, 4, 0, 0x447a0000, 0);

                    knownPeds.Add(ped.Handle, new KnownPedInfo() { AnimSceneID = sceneId });
                }

                else
                {
                    if (Function.Call<bool>(Hash.IS_ENTITY_PLAYING_ANIM, ped, "MISSSOLOMON_3", "molly_death", 3))
                    {
                        if (Function.Call<float>(Hash.GET_SYNCHRONIZED_SCENE_PHASE, knownPeds[ped.Handle].AnimSceneID) > 0.89)
                        {
                            ped.Delete();

                            RemovePed(ped);

                            Function.Call(Hash.START_PARTICLE_FX_NON_LOOPED_ON_ENTITY, "scr_trev4_747_blood_impact", vehicle.Handle, 12.5793, 12.2, -7.094210147857666, 0.0, 0.0, 0.0, 1.0, 0, 0, 0);

                            var fx = Function.Call<int>(Hash.START_PARTICLE_FX_LOOPED_ON_ENTITY, "scr_trev4_747_exhaust_plane_misfire", vehicle.Handle, -12.6323, 8.1153, -7.0876, 0.0, 0.0, 0.0, 1.0, 0, 0, 0);

                            var soundId = Function.Call<int>(Hash.GET_SOUND_ID);

                            Function.Call(Hash.PLAY_SOUND_FROM_COORD, soundId, "Trevor_4_747_Man_Sucked_In", 938.6, -2984.1298828125, 15.47, 0, 0, 0, 0);                                                                       
                        }
                    }
                }
            }
        }

        private unsafe void OnTick(object sender, EventArgs e)
        {
            foreach (var vehicle in World.GetAllVehicles())
            {
                if (Function.Call<int>(Hash.GET_VEHICLE_CLASS, vehicle) == 16)
                {
                    if (!knownVehicles.ContainsKey(vehicle.Handle) && IsVehicleValid(vehicle))
                    {
                        var turbineOffsets = Utility.EnumTurbineOffsets(vehicle);

                        knownVehicles.Add(vehicle.Handle, new KnownVehicleInfo()
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

                    var info = knownVehicles[vehicle.Handle];

                    for (int i = 0; i < info.Offsets.Length; i++)
                    {
                        var fThrottle = InteropExt.ReadFloat(new IntPtr(vehicle.MemoryAddress) + 0x1B70); //vehicle.Velocity.Length() / 30.0f;

                        fThrottle *= 1.0f;

                        if (fThrottle >= 1.0f) fThrottle = 1.0f;

                        else if (fThrottle < 0.25f) fThrottle = 0.25f;

                        var backtThrustDistance = EngineBackThrustExtremityScalar - (20.0f * (1.0f - fThrottle));

                        if (info.Size == VehicleSize.Small)
                            backtThrustDistance *= 0.6f;

                        else if (info.Size == VehicleSize.Big)
                            backtThrustDistance *= 1.4f;

                        Vector3 forwardLeft = vehicle.GetOffsetInWorldCoords(info.Offsets[i] + new Vector3(0, EngineThrustExtremityScalar, 0f));

                        Vector3 rightOffset = new Vector3(-info.Offsets[i].X, info.Offsets[i].Y, info.Offsets[i].Z);

                        Vector3 forwardRight = vehicle.GetOffsetInWorldCoords(rightOffset + new Vector3(0, EngineThrustExtremityScalar, 0f));

                        Vector3 rearLeft = vehicle.GetOffsetInWorldCoords(info.Offsets[i] - new Vector3(0, backtThrustDistance, 0f));

                        var groundHeightLeft = World.GetGroundHeight(rearLeft);

                        Vector3 rearRight = vehicle.GetOffsetInWorldCoords(rightOffset - new Vector3(0, backtThrustDistance, 0f));

                        var groundHeightRight = World.GetGroundHeight(rearRight);                

                        float scale = 0.0f, bottomOffset;

                        Vector3 direction = Vector3.Normalize(rearLeft - forwardLeft);

                        if (vehicle.Acceleration < 0.0f || vehicle.CurrentGear <= 0)
                        {
                            scale = ReverseThrustScale * fThrottle;
                            direction = -direction;
                        }

                        else scale = ThrustScale * fThrottle;

                        if (info.Size == VehicleSize.Big)
                        {
                            bottomOffset = 1.4f;
                            scale *= 1.64f;
                        }

                        else if (info.Size == VehicleSize.Med)
                        {
                            bottomOffset = 1.3f;
                            scale *= 1.4f;
                        }

                        else bottomOffset = 1.5f;

                        if (GetEntityShapeTestCapsuleResult(forwardLeft, new Vector3(rearLeft.X, rearLeft.Y, groundHeightLeft), ThrustRadius, vehicle, out thrustForceTarget)) // left turbine
                            ApplyThrustForce(thrustForceTarget, forwardLeft, direction, scale);

                        if (GetEntityShapeTestCapsuleResult(forwardRight, new Vector3(rearRight.X, rearRight.Y, groundHeightRight), ThrustRadius, vehicle, out thrustForceTarget)) // right turbine
                            ApplyThrustForce(thrustForceTarget, forwardRight, direction, scale);

                        if (GetEntityShapeTestCapsuleResult(forwardLeft - new Vector3(0, 0, bottomOffset),  rearLeft - new Vector3(0, 0, bottomOffset), ThrustRadius, vehicle, out thrustForceTarget)) // left side bottom
                            ApplyThrustForce(thrustForceTarget, forwardLeft, direction, scale);

                        if (GetEntityShapeTestCapsuleResult(forwardRight - new Vector3(0, 0, bottomOffset), rearRight - new Vector3(0, 0, bottomOffset), ThrustRadius, vehicle, out thrustForceTarget)) // right side bottom
                            ApplyThrustForce(thrustForceTarget, forwardRight, direction, scale);

                      /*var animTriggerPos1 = vehicle.GetOffsetInWorldCoords(info.Offsets[i] + new Vector3(0, 12.0f, 0));
                        var animTriggerPos2 = vehicle.GetOffsetInWorldCoords(rightOffset + new Vector3(0, 12.0f, 0));
                        animTriggerPos1.Z = World.GetGroundHeight(animTriggerPos1);
                        animTriggerPos2.Z = World.GetGroundHeight(animTriggerPos2);
                        UpdateEngineAnimTrigger(vehicle, animTriggerPos1);
                        UpdateEngineAnimTrigger(vehicle, animTriggerPos2);*/
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

        struct KnownPedInfo
        {
            public int AnimSceneID;
        }

        enum VehicleSize
        {
            Small,
            Med,
            Big
        }
    }
}
