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

    //private BeginSimulationEntityCommandBufferSystem.Singleton bsECB;
    //private EntityCommandBuffer ecb;

    void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<BeginSimulationEntityCommandBufferSystem.Singleton>();
    }

    void OnUpdate(ref SystemState state)
    {

        //var bsECB = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>();

        EntityCommandBuffer ecb = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>().CreateCommandBuffer(state.WorldUnmanaged);

        foreach (var (spawner, ltw) in SystemAPI.Query<RefRW<MonsterSpawnerData>, RefRO<LocalToWorld>>())
        {

            if (spawner.ValueRO.active == true)
            {
                //remove
                //spawner.ValueRW.RespawnTimer = 0;

                spawner.ValueRW.RespawnTimer -= SystemAPI.Time.DeltaTime;


                if (spawner.ValueRO.RespawnTimer < 0)
                {
                    //var instance = new NativeArray<Entity>(1, Allocator.Temp);
                    var monster = ecb.Instantiate(spawner.ValueRO.MonsterPrefab);

                    var direction = new Vector3(Random.Range(-1f, 1f), Random.Range(-1f, 1f), 0).normalized;

                    var new_pos = ltw.ValueRO.Position + new float3(direction * spawner.ValueRO.SpawnRangeRadius);


                    ecb.SetComponent<LocalTransform>(monster, new LocalTransform { Position = new_pos, Rotation = Quaternion.identity, Scale = 1f });
                    //if I add physics
                    //TO UPDATE :
                    //float newRadius = 0.5f;
                    //float fat = TreeInsersionSystem.AABBfat;
                    //NativeQueue<int> comparequeue = new NativeQueue<int>(Allocator.Temp);
                    ecb.SetComponent<CircleShapeData>(monster, new CircleShapeData { Position = new Vector2(new_pos.x,new_pos.y), radius = 0.5f, collisionLayer = PhysicsUtilities.CollisionLayer.MonsterLayer});
                    //var newAABB = new AABB
                    //{
                    //    UpperBound = new Vector2(new_pos.x + newRadius + fat, new_pos.y + newRadius + fat),
                    //    LowerBound = new Vector2(new_pos.x - newRadius - fat, new_pos.y - newRadius - fat)
                    //};
                    //TreeInsersionSystem.AABBtree.InsertLeaf(monster,newAABB,comparequeue);
                    //comparequeue.Dispose();

                    //spawner.ValueRW.poolEntity = ecb.Instantiate(spawner.ValueRO.MonsterPrefab);
                    //ecb.AddComponent<Disabled>(spawner.ValueRO.poolEntity);

                    //ecb.SetComponent<PhyBodyData>(monster, new PhyBodyData { Position = new Vector2(new_pos.x, new_pos.y), Mass = 1.0f });


                    spawner.ValueRW.RespawnTimer = spawner.ValueRO.SpawnRate;
                }
            }

        }
        //ecb.Playback(state.EntityManager);
        //ecb.Dispose();

    }


}