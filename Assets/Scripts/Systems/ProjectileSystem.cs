using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;
using UnityEngine.EventSystems;


[UpdateInGroup(typeof(GameSimulationSystemGroup))]
[UpdateBefore(typeof(HealthSystem))]
[BurstCompile]
public partial struct ProjectileSystem : ISystem
{

    private Entity damageEventEntity;

    public void OnCreate(ref SystemState state)
    {

    }
    public void OnUpdate(ref SystemState state)
    {
        var ecbSingleton = SystemAPI.GetSingleton<BeginSimulationEntityCommandBufferSystem.Singleton>();
        var ecb = ecbSingleton.CreateCommandBuffer(state.WorldUnmanaged);

        foreach (var (projectileData,shape,trans,entity) in SystemAPI.Query<RefRW<ProjectileInstanceData>,RefRW<ShapeData>, RefRW<LocalTransform>>().WithEntityAccess())
        {
            trans.ValueRW.Position += new float3(projectileData.ValueRO.direction * projectileData.ValueRO.speed,0);
            shape.ValueRW.Position = trans.ValueRO.Position.xy;
            //body.ValueRW.Velocity = body.ValueRO.Velocity.normalized * projectileData.ValueRO.speed;
            projectileData.ValueRW.remainingLifeTime -= SystemAPI.Time.DeltaTime;
            if (projectileData.ValueRO.remainingLifeTime < 0)
            {
                PhysicsCalls.DestroyPhysicsEntity(ecb, entity);
            }
        }
    }

}