using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;
using Random = UnityEngine.Random;



[UpdateInGroup(typeof(GameSimulationSystemGroup),OrderFirst = true)]
public partial struct MonsterSpawnerSystem : ISystem
{
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<BeginSimulationEntityCommandBufferSystem.Singleton>();
    }

    public void OnUpdate(ref SystemState state)
    {

        EntityCommandBuffer ecb = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>().CreateCommandBuffer(state.WorldUnmanaged);

        foreach (var (spawner, ltw,entity) in SystemAPI.Query<RefRW<MonsterSpawnerData>, RefRO<LocalToWorld>>().WithEntityAccess())
        {

            if (spawner.ValueRO.active == true)
            {

                spawner.ValueRW.RespawnTimer -= SystemAPI.Time.DeltaTime;


                if (spawner.ValueRO.RespawnTimer < 0)
                {

                    var monster = ecb.Instantiate(spawner.ValueRO.MonsterPrefab);

                    var direction = new Vector3(Random.Range(-1f, 1f), Random.Range(-1f, 1f), 0).normalized*3;

                    var new_pos = ltw.ValueRO.Position + new float3(direction * spawner.ValueRO.SpawnRangeRadius);


                    ecb.SetComponent<LocalTransform>(monster, new LocalTransform { Position = new_pos, Rotation = Quaternion.identity, Scale = 1f });

                    var newShapeData = state.EntityManager.GetComponentData<ShapeData>(spawner.ValueRO.MonsterPrefab);
                    newShapeData.Position = new_pos.xy;
                    newShapeData.Rotation = 0;
                    ecb.SetComponent<ShapeData>(monster, newShapeData);
                

                    spawner.ValueRW.RespawnTimer = spawner.ValueRO.SpawnRate;
                }
            }

        }

    }


}