using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;

// Singleton component for accessing ActiveTriggers
public struct ActiveTriggerSingleton : IComponentData
{
    public NativeParallelHashMap<Entity, Entity> TriggerMap;
}

/// <summary>
/// Fuse with TriggerProcessingSystem?
/// </summary>
[UpdateInGroup(typeof(FixedStepGameSimulationSystemGroup))]
[UpdateAfter(typeof(PhyResolutionSystem))]
[BurstCompile]
public partial struct TriggerStateSystem : ISystem
{
    public void OnCreate(ref SystemState state)
    {
        // Create a singleton entity to store ActiveTriggers
        Entity singletonEntity = state.EntityManager.CreateEntity(typeof(ActiveTriggerSingleton));

        // Set initial value (empty hash map)
        state.EntityManager.SetComponentData(singletonEntity, new ActiveTriggerSingleton
        {
            TriggerMap = new NativeParallelHashMap<Entity, Entity>(128, Allocator.Persistent)
        });
    }

    public void OnUpdate(ref SystemState state)
    {
        // Create an ECB for the current frame
        EntityCommandBuffer ecb = SystemAPI.GetSingleton<BeginFixedStepSimulationEntityCommandBufferSystem.Singleton>().CreateCommandBuffer(state.WorldUnmanaged);

        // Get the singleton entity
        var activeTriggerEntity = SystemAPI.GetSingletonEntity<ActiveTriggerSingleton>();

        var activeTriggerData = SystemAPI.GetComponent<ActiveTriggerSingleton>(activeTriggerEntity);
        // Access the ActiveTriggers map
        var activeTriggers = activeTriggerData.TriggerMap;

        var triggerEventEntity = SystemAPI.GetSingletonEntity<TriggerEvent>();
        var triggerBuffer = SystemAPI.GetBuffer<TriggerEvent>(triggerEventEntity);

        var keysToRemove = new NativeList<Entity>(Allocator.Temp);

        // Build a hash set of active entities
        var activeEntities = new NativeParallelHashSet<Entity>(triggerBuffer.Length, Allocator.Temp);
        foreach (var trigger in triggerBuffer)
        {
            activeEntities.Add(trigger.EmitterEntity);
            //Debug.Log("add");
        }

        // Get key array directly
        var keys = activeTriggers.GetKeyArray(Allocator.Temp);
        foreach (var key in keys)
        {
            if (!activeEntities.Contains(key))
            {
                keysToRemove.Add(key);
            }
        }

        // Remove outdated triggers
        foreach (var key in keysToRemove)
        {
            activeTriggers.Remove(key);
        }   
        
        // Apply changes to the component only once at the end
        activeTriggerData.TriggerMap = activeTriggers;
        ecb.SetComponent(activeTriggerEntity, activeTriggerData);

        // Dispose temp memory
        keys.Dispose();
        activeEntities.Dispose();
        keysToRemove.Dispose();
    }

    public void OnDestroy(ref SystemState state)
    {
        // Remove the ActiveTriggers from the singleton component
        var activeTriggerEntity = SystemAPI.GetSingletonEntity<ActiveTriggerSingleton>();
        var activeTriggers = SystemAPI.GetComponent<ActiveTriggerSingleton>(activeTriggerEntity).TriggerMap;

        activeTriggers.Dispose();
    }
}