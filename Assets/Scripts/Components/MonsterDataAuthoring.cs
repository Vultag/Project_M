using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using UnityEngine;


public struct MonsterData : IComponentData
{
    public float Speed;
    public float Attack;
}
public class MonsterDataAuthoring : MonoBehaviour
{
    private float Speed = 0.5f;
    private float Attack = 1;

    class MonsterDataBaker : Baker<MonsterDataAuthoring>
    {
        public override void Bake(MonsterDataAuthoring authoring)
        {

            Entity entity = GetEntity(TransformUsageFlags.None);

            AddComponent(entity, new MonsterData
            {
                Speed = authoring.Speed,
                Attack = authoring.Attack

            });
            AddComponent(entity, new HealthData { HP = 10});
        }
    }
}