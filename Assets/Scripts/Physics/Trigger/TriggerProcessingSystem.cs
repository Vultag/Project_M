
using System;
using System.Linq;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;

public struct TriggerEvent : IBufferElementData
{
    public Entity EmitterEntity;
    public Entity ReciverEntity;
}
public struct TriggerPair : IEquatable<TriggerPair>
{
    public Entity EmitterEntity;
    public Entity ReciverEntity;
    public bool Equals(TriggerPair other)
    {
        return (EmitterEntity == other.EmitterEntity && ReciverEntity == other.ReciverEntity);
    }
    public override int GetHashCode()
    {
        // Order-insensitive hash (use XOR or commutative hash)
        int hashA = EmitterEntity.GetHashCode();
        int hashB = ReciverEntity.GetHashCode();
        return hashA ^ hashB;
    }
}

public partial struct TriggerProcessingSystem : ISystem
{
    private Entity damageEventEntity;

    private NativeHashSet<(Entity,Entity)> activeTriggers;
    private NativeHashSet<(Entity, Entity)> nextFrameTriggers;

    public void OnCreate(ref SystemState state)
    {
        activeTriggers = new NativeHashSet<(Entity, Entity)>(128, Allocator.Persistent);
        nextFrameTriggers = new NativeHashSet<(Entity, Entity)>(128, Allocator.Persistent);

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

        Entity triggerEventEntity = state.EntityManager.CreateEntity();
        state.EntityManager.AddBuffer<TriggerEvent>(triggerEventEntity);
    }

    public void OnUpdate(ref SystemState state)
    {
        EntityCommandBuffer ecb = SystemAPI.GetSingleton<BeginSimulationEntityCommandBufferSystem.Singleton>().CreateCommandBuffer(state.WorldUnmanaged);

        Entity triggerEventEntity = SystemAPI.GetSingletonEntity<TriggerEvent>();
        DynamicBuffer<TriggerEvent> triggerBuffer = SystemAPI.GetBuffer<TriggerEvent>(triggerEventEntity);

        for (int i = triggerBuffer.Length - 1; i >= 0; i--)
        {
            Entity emitterEntity = triggerBuffer[i].EmitterEntity;
            Entity reciverEntity = triggerBuffer[i].ReciverEntity;

            nextFrameTriggers.Add((triggerBuffer[i].EmitterEntity, triggerBuffer[i].ReciverEntity));

            // Check if this collision has already triggered
            if (activeTriggers.Contains((triggerBuffer[i].EmitterEntity, triggerBuffer[i].ReciverEntity)))
            {
                //Debug.Log("skip");
                continue; // Skip if already processed
            }

            var triggerType = state.EntityManager.GetComponentData<TriggerData>(emitterEntity).triggerType;
            var damageBuffer = SystemAPI.GetBuffer<GlobalDamageEvent>(damageEventEntity);

            var uiManager = UIManager.Instance;
            /// OPTI : switch on non contiguous enum(flag) : no jump table (bad)
            switch (triggerType)
            {
                case TriggerType.ProjectileDamageEffect:
                    ProjectileInstanceData newProjectileData = state.EntityManager.GetComponentData<ProjectileInstanceData>(emitterEntity);
                    if (!state.EntityManager.HasComponent<HealthData>(reciverEntity))
                    {
                        newProjectileData.remainingLifeTime = 0;
                        ecb.SetComponent<ProjectileInstanceData>(emitterEntity, newProjectileData);
                        continue;
                    }
                    damageBuffer.Add(new GlobalDamageEvent { 
                        Target = reciverEntity,
                        DamageValue = SystemAPI.GetComponent<ProjectileInstanceData>(emitterEntity).damage
                    });
                    newProjectileData.penetrationCapacity--;
                    newProjectileData.remainingLifeTime = newProjectileData.remainingLifeTime * Mathf.Min(newProjectileData.penetrationCapacity,1);
                    ecb.SetComponent<ProjectileInstanceData>(emitterEntity, newProjectileData);
                    break;
                case TriggerType.CollisionDamageEffect:

                    /// NOT USED FOR NOW
                    /// initially inteneded for monster damage

                    if (!state.EntityManager.HasComponent<HealthData>(reciverEntity) || !state.EntityManager.HasComponent<MonsterData>(emitterEntity))
                    {
                        continue;
                    }
                    damageBuffer.Add(new GlobalDamageEvent
                    {
                        Target = reciverEntity,
                        DamageValue = 3f
                    });
                    break;

                case TriggerType.WeaponCollectible:
                    //Debug.Log("collect weapon");
                    var weaponCollectibleData = SystemAPI.GetComponent<WeaponCollectibleData>(emitterEntity);
                    if (uiManager.NumOfEquipments <6)
                        UIManager.Instance._AddSynthUI(weaponCollectibleData.weaponClass,weaponCollectibleData.weaponType);
                    PhysicsCalls.DestroyPhysicsEntity(ecb,emitterEntity);
                    break;
                case TriggerType.DrumMachineCollectibe:
                    //Debug.Log("collect drumMachine");
                    var DrumMachinecollectibleData = SystemAPI.GetComponent<DrumMachineCollectibleData>(emitterEntity);
                    if (uiManager.NumOfEquipments < 6)
                        UIManager.Instance._AddDrumMachineUI();
                    PhysicsCalls.DestroyPhysicsEntity(ecb,emitterEntity);
                    break;
                default:
                    break;
            }


        }
        /// Swap buffers
        var temp = activeTriggers;
        activeTriggers = nextFrameTriggers;
        nextFrameTriggers = temp;
        nextFrameTriggers.Clear();
        // Clear buffer after processing
        triggerBuffer.Clear();
    }

    public void OnDestroy(ref SystemState state)
    {
        activeTriggers.Dispose();
        nextFrameTriggers.Dispose();
    }


}