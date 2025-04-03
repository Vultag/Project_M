using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;

// Singleton component for accessing ActiveTriggers
/// Not possible -> must be blitable
//public struct ActiveTriggerSingleton : IComponentData
//{
//    public NativeParallelHashMap<Entity, Entity> TriggerMap;
//}

/// <summary>
/// Fuse with TriggerProcessingSystem?
/// </summary>
[UpdateInGroup(typeof(FixedStepGameSimulationSystemGroup))]
[UpdateAfter(typeof(PhyResolutionSystem))]
[BurstCompile]
public partial class TriggerStateSystem : SystemBase
{
    public static NativeParallelHashMap<Entity, Entity> TriggerMap;

    protected override void OnCreate()
    {
        TriggerMap = new NativeParallelHashMap<Entity, Entity>(128, Allocator.Persistent);
    }

    protected override void OnUpdate()
    {
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
        var keys = TriggerMap.GetKeyArray(Allocator.Temp);
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
            TriggerMap.Remove(key);
        }   

        // Dispose temp memory
        keys.Dispose();
        activeEntities.Dispose();
        keysToRemove.Dispose();
    }

    protected override void OnDestroy()
    {
        TriggerMap.Dispose();
    }
}