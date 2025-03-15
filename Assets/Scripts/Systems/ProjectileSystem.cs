using Unity.Burst;
using Unity.Entities;
using Unity.Transforms;
using UnityEngine;


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

        foreach (var (projectileData,entity) in SystemAPI.Query<RefRW<ProjectileInstanceData>>().WithEntityAccess())
        {
            projectileData.ValueRW.remainingLifeTime -= SystemAPI.Time.DeltaTime;
            if(projectileData.ValueRO.remainingLifeTime < 0)
            {
                PhysicsCalls.DestroyPhysicsEntity(ecb,entity);
            }
        }
    }

}