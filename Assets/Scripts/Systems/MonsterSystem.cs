using System.Collections;
using System.Collections.Generic;
using System.Threading;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using static UnityEngine.EventSystems.EventTrigger;

[UpdateInGroup(typeof(GameSimulationSystemGroup), OrderFirst = true)]
public partial struct MonsterSystem : ISystem
{


    void OnCreate(ref SystemState state)
    {

    }


    void OnUpdate(ref SystemState state)
    {
        EntityCommandBuffer ecb = SystemAPI.GetSingleton<BeginSimulationEntityCommandBufferSystem.Singleton>().CreateCommandBuffer(state.WorldUnmanaged);
        //MOUVEMENT
        var player_trans = SystemAPI.GetComponent<LocalTransform>(SystemAPI.GetSingletonEntity<PlayerData>());

        foreach (var (monster_data, trans, body) in SystemAPI.Query<RefRO<MonsterData>, RefRO<LocalTransform>, RefRW<PhyBodyData>>())
        {
            Vector2 moveDirection = new float2(player_trans.Position.x - trans.ValueRO.Position.x, player_trans.Position.y - trans.ValueRO.Position.y);

            //body.ValueRW.Velocity = monster_data.ValueRO.Speed * moveDirection.normalized * SystemAPI.Time.DeltaTime *4f;
            body.ValueRW.Force += monster_data.ValueRO.Speed * moveDirection.normalized * 0.001f;

        }


    }


}