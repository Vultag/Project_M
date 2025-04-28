using System.Collections;
using System.Collections.Generic;
using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

[UpdateInGroup(typeof(GameSimulationSystemGroup))]
[BurstCompile]
public partial struct ProcessEffectsSystem : ISystem
{
    public void OnCreate(ref SystemState state)
    {

        /// Create effects manager entities
        Entity stunManager = state.EntityManager.CreateEntity();
        state.EntityManager.AddBuffer<StunEffectData>(stunManager);
        state.EntityManager.SetName(stunManager, $"{typeof(StunEffectData).Name}_Manager");
        ///...
    }

    public void OnUpdate(ref SystemState state)
    {

        Entity stunManager = SystemAPI.GetSingletonEntity<StunEffectData>();
        var stunBuffer = SystemAPI.GetBuffer<StunEffectData>(stunManager);

        EntityManager em = state.EntityManager;

        float deltaTime = SystemAPI.Time.DeltaTime;

        for (int i = 0; i < stunBuffer.Length; i++)
        {
            var effect = stunBuffer[i];
            effect.Duration -= deltaTime;
            stunBuffer[i] = effect;

            if (effect.Duration <= 0)
            {
                /// PREVENT OTHER SPEED MODIFYER EFFECT. REWORK ?
                //Debug.Log("remove");
                /// The entity was deleted before effect expiration
                if (!state.EntityManager.Exists(effect.TargetEntity))
                {
                    stunBuffer.RemoveAt(i);
                    i--;
                    continue;
                }
                var newMosterData = em.GetComponentData<MonsterData>(effect.TargetEntity);
                newMosterData.Speed = 0.5f;
                em.SetComponentData(effect.TargetEntity, newMosterData);
                stunBuffer.RemoveAt(i);
                i--;
            }
        }

        /// PROCESS MORE EFFECTS

        #region Effect process

        #endregion


        #region Effect cleanup

        #endregion



    }
}
