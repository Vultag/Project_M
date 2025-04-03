using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using Unity.Entities;
using UnityEngine;

public struct EffectUtils
{
    public static void ApplyStun(Entity stunManager,EntityManager entityManager, EntityCommandBuffer ecb, Entity target,float duration)
    {
        var buffer = entityManager.GetBuffer<StunEffectData>(stunManager);

        /// check if entity already effected -> refresh
        for (int i = 0; i < buffer.Length; i++)
        {
            if (buffer[i].TargetEntity == target)
            {
                buffer[i] = new StunEffectData { TargetEntity = target, Duration = duration };
                return;
            }
        }

        var newMonsterData = entityManager.GetComponentData<MonsterData>(target);
        newMonsterData.Speed = 0;
        ecb.SetComponent<MonsterData>(target, newMonsterData);
        buffer.Add(new StunEffectData { TargetEntity = target, Duration = duration });
    }

}