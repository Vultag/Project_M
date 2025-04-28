using Unity.Burst;
using Unity.Entities;
using Unity.Transforms;
using UnityEngine;
using UnityEngine.EventSystems;


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

        foreach (var (projectileData,body,entity) in SystemAPI.Query<RefRW<ProjectileInstanceData>,RefRW<PhyBodyData>>().WithEntityAccess())
        {
            //body.ValueRW.Velocity = body.ValueRO.Velocity.normalized * projectileData.ValueRO.speed;
            projectileData.ValueRW.remainingLifeTime -= SystemAPI.Time.DeltaTime;
            if(projectileData.ValueRO.remainingLifeTime < 0)
            {
                PhysicsCalls.DestroyPhysicsEntity(ecb,entity);
            }
        }
    }

}