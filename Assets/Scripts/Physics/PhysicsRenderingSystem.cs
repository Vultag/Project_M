using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;


[UpdateInGroup(typeof(GameSimulationSystemGroup), OrderLast = true)]
public partial struct PhysicsRenderingSystem : ISystem
{

    private Vector2 gravity;

    public void OnCreate(ref SystemState state)
    {
        gravity = new Vector2(0f, -9.81f);

        //var Player_query = state.EntityManager.CreateEntityQuery(typeof(PlayerData));
        //Entity player_entity = Player_query.GetSingletonEntity();
        //Entity start_weapon = state.EntityManager.GetComponentData<PlayerData>(player_entity).MainCanon;

        //var ecbSingleton = SystemAPI.GetSingleton<BeginSimulationEntityCommandBufferSystem.Singleton>();
        //var ECB = ecbSingleton.CreateCommandBuffer(state.WorldUnmanaged);
        //var playerEquipmentSpriteBuffer = state.EntityManager.GetBuffer<Child>(start_weapon);

        //ECB.AddComponent<Disabled>(playerEquipmentSpriteBuffer[0].Value);
        //ECB.AddComponent<Disabled>(playerEquipmentSpriteBuffer[1].Value);
        //Debug.LogError(playerEquipmentSpriteBuffer[0].Value.Index);

    }


    public void OnUpdate(ref SystemState state)
    {

        Camera cam = CameraSingleton.Instance.MainCamera;

        foreach (var trans in SystemAPI.Query<RefRO<LocalTransform>>().WithAll<PlayerData>().WithAll<PhyBodyData>())
        {
            //Vector3 newCamXYpos = math.lerp(cam.transform.position, trans.ValueRO.Position, 8f * (1f / 60f));
            /// smoothing to the player cause visible stutter at high speed
            Vector3 newCamXYpos = trans.ValueRO.Position;
            cam.transform.position = new Vector3(newCamXYpos.x, newCamXYpos.y, cam.transform.position.z);

        }


        foreach (var (shape, trans) in SystemAPI.Query<RefRW<CircleShapeData>, RefRW<LocalTransform>>().WithAny<PhyBodyData>())
        {
            float fixedDeltaTime = 1f / 60f; // Assuming Fixed Timestep = (60Hz physics update)
            float alpha = ((float)SystemAPI.Time.ElapsedTime% fixedDeltaTime) / fixedDeltaTime;
            alpha = math.saturate(alpha); // Ensure alpha is clamped between 0-1
            //Debug.Log(alpha);
            trans.ValueRW.Position = new float3(math.lerp(shape.ValueRO.PreviousPosition, shape.ValueRO.Position, alpha), trans.ValueRO.Position.z);
            trans.ValueRW.Rotation = math.slerp(shape.ValueRO.PreviousRotation, shape.ValueRO.Rotation, alpha);


        }

    }


}
