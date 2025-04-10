using System.Collections;
using System.Collections.Generic;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;

/// <summary>
/// Jobify ?
/// </summary>

[BurstCompile]
//[UpdateInGroup(typeof(GameSimulationSystemGroup))]
//[UpdateBefore(typeof(PhysicsRenderingSystem))]
public partial struct HealthSystem : ISystem
{
    private Entity damageEventEntity;

    public void OnCreate(ref SystemState state)
    {
        // Check if the GlobalDamageEvent entity already exists
        if (SystemAPI.HasSingleton<GlobalDamageEvent>())
        {
            // If it exists, get the singleton entity
            damageEventEntity = SystemAPI.GetSingletonEntity<GlobalDamageEvent>();
        }
        else
        {
            // If not, create the entity and add the GlobalDamageEvent buffer
            damageEventEntity = state.EntityManager.CreateEntity();
            state.EntityManager.AddBuffer<GlobalDamageEvent>(damageEventEntity); // Add the buffer to the new entity
        }

    }
    public void OnUpdate(ref SystemState state)
    {
        var ecbSingleton = SystemAPI.GetSingleton<BeginFixedStepSimulationEntityCommandBufferSystem.Singleton>();
        var ecb = ecbSingleton.CreateCommandBuffer(state.WorldUnmanaged);

        var globalDamageBuffer = SystemAPI.GetBuffer<GlobalDamageEvent>(damageEventEntity);

        /// would have excess memory for each duplicate
        var entityDamageMap = new NativeHashMap<Entity,float>(globalDamageBuffer.Length, Allocator.Temp);

        /// Process damage to a map before applying damage as group on each entity
        foreach (var damageEvent in globalDamageBuffer) 
        {
            if (!entityDamageMap.TryAdd(damageEvent.Target, damageEvent.DamageValue))
            {
                entityDamageMap[damageEvent.Target] += damageEvent.DamageValue;
            }
        }

        foreach (var damageGroup in entityDamageMap)
        {
            var health = state.EntityManager.GetComponentData<HealthData>(damageGroup.Key);
            health.HP -= damageGroup.Value;
            if (health.HP > 0)
            {
                ecb.SetComponent(damageGroup.Key, health);
            }
            else
            {
                //Debug.Log("called");
                /// Assumes every entity with health is also physic
                PhysicsCalls.DestroyPhysicsEntity(ecb, damageGroup.Key);
            }
        }


        entityDamageMap.Dispose();
        // Clear damage events after processing
        globalDamageBuffer.Clear();
    }
    
}
