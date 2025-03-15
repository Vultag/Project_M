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

        var entitiesToDestroy = new NativeHashSet<Entity>(globalDamageBuffer.Length, Allocator.Temp);

        foreach (var damageEvent in globalDamageBuffer) 
        {
            if (entitiesToDestroy.Contains(damageEvent.Target))
                continue; // Skip already marked entities

            var health = state.EntityManager.GetComponentData<HealthData>(damageEvent.Target);
            health.HP -= damageEvent.DamageValue;
            if (health.HP > 0)
            {
                ecb.SetComponent(damageEvent.Target, health);
            }
            else
            {
                entitiesToDestroy.Add(damageEvent.Target);
                /// Assumes every entity with health is also physic
                PhysicsCalls.DestroyPhysicsEntity(ecb, damageEvent.Target);
            }

        }

        // Clear damage events after processing
        globalDamageBuffer.Clear();
    }
    
}
