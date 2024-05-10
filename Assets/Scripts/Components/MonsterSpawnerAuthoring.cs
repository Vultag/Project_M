using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using UnityEngine;

public struct MonsterSpawnerData : IComponentData
{
    public bool active;
    public float SpawnRate;
    public float SpawnRangeRadius;
    public Entity MonsterPrefab;
    public float RespawnTimer;
}

public class MonsterSpawnerAuthoring : MonoBehaviour
{

    public bool active;
    public float SpawnRate;
    public float SpawnRangeRadius;
    public GameObject MonsterPrefab;

    class MonsterSpawnerBaker : Baker<MonsterSpawnerAuthoring>
    {
        public override void Bake(MonsterSpawnerAuthoring authoring)
        {

            Entity entity = GetEntity(TransformUsageFlags.None);

            AddComponent(entity, new MonsterSpawnerData
            {

                active = authoring.active,
                SpawnRate = authoring.SpawnRate,
                SpawnRangeRadius = authoring.SpawnRangeRadius,
                MonsterPrefab = GetEntity(authoring.MonsterPrefab, TransformUsageFlags.None),
                RespawnTimer = authoring.SpawnRate

            });
        }
    }
}