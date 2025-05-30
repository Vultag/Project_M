
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Transforms;
using UnityEngine;

[BurstCompile]
public partial struct EnergySystem : ISystem
{

    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<EquipmentEnergyData>();

    }
    public void OnUpdate(ref SystemState state)
    {

        foreach (var energy in SystemAPI.Query<RefRW<EquipmentEnergyData>>())
        {
            energy.ValueRW.energyLevel = Mathf.Min(energy.ValueRO.maxEnergy, energy.ValueRO.energyLevel + energy.ValueRO.energyRecoveryRate * SystemAPI.Time.DeltaTime );
        }
    }

}
