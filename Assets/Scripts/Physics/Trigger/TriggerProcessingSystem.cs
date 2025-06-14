
using System;
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

[UpdateInGroup(typeof(FixedStepGameSimulationSystemGroup))]
[UpdateAfter(typeof(TriggerStateSystem))]
public partial struct TriggerProcessingSystem : ISystem
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

        Entity triggerEventEntity = state.EntityManager.CreateEntity();
        state.EntityManager.AddBuffer<TriggerEvent>(triggerEventEntity);
    }

    public void OnUpdate(ref SystemState state)
    {
        EntityCommandBuffer ecb = SystemAPI.GetSingleton<BeginSimulationEntityCommandBufferSystem.Singleton>().CreateCommandBuffer(state.WorldUnmanaged);

        Entity triggerEventEntity = SystemAPI.GetSingletonEntity<TriggerEvent>();
        DynamicBuffer<TriggerEvent> triggerBuffer = SystemAPI.GetBuffer<TriggerEvent>(triggerEventEntity);

        // Access the ActiveTriggers map
        var activeTriggers = TriggerStateSystem.TriggerMap;

        foreach (var trigger in triggerBuffer)
        {
            Entity emitterEntity = trigger.EmitterEntity;
            Entity reciverEntity = trigger.ReciverEntity;

            // Check if this collision has already triggered
            if (activeTriggers.ContainsKey(emitterEntity) && activeTriggers[emitterEntity] == reciverEntity)
            {
                //Debug.Log("skip");
                continue; // Skip if already processed
            }
            // Mark this collision as processed
            activeTriggers[emitterEntity] = reciverEntity;

            var triggerType = state.EntityManager.GetComponentData<TriggerData>(emitterEntity).triggerType;

            var uiManager = UIManager.Instance;
            /// OPTI : switch on non contiguous enum(flag) : no jump table (bad)
            switch (triggerType)
            {
                case TriggerType.DamageEffect:
                    ProjectileInstanceData newProjectileData = state.EntityManager.GetComponentData<ProjectileInstanceData>(emitterEntity);
                    if (!state.EntityManager.HasComponent<HealthData>(reciverEntity))
                    {
                        newProjectileData.remainingLifeTime = 0;
                        ecb.SetComponent<ProjectileInstanceData>(emitterEntity, newProjectileData);
                        continue;
                    }
                    var damageBuffer = SystemAPI.GetBuffer<GlobalDamageEvent>(damageEventEntity);
                    damageBuffer.Add(new GlobalDamageEvent { 
                        Target = reciverEntity,
                        DamageValue = SystemAPI.GetComponent<ProjectileInstanceData>(emitterEntity).damage
                    });
                    newProjectileData.penetrationCapacity--;
                    newProjectileData.remainingLifeTime = newProjectileData.remainingLifeTime * Mathf.Min(newProjectileData.penetrationCapacity,1);
                    ecb.SetComponent<ProjectileInstanceData>(emitterEntity, newProjectileData);
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

        // Clear buffer after processing
        triggerBuffer.Clear();
    }


}