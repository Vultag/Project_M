using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using UnityEngine;


public struct MonsterData : IComponentData
{
    public float Health;
    public float Speed;
    public float Attack;
}
public class MonsterDataAuthoring : MonoBehaviour
{
    //set to private for now
    private float Health = 10;
    private float Speed = 5f;
    private float Attack = 1;

    class MonsterDataBaker : Baker<MonsterDataAuthoring>
    {
        public override void Bake(MonsterDataAuthoring authoring)
        {

            Entity entity = GetEntity(TransformUsageFlags.None);

            AddComponent(entity, new MonsterData
            {

                Health = authoring.Health,
                Speed = authoring.Speed,
                Attack = authoring.Attack

            });
        }
    }
}