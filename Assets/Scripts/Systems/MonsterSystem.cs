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

[UpdateInGroup(typeof(GameSimulationSystemGroup), OrderFirst = true)]
public partial struct MonsterSystem : ISystem
{


    void OnCreate(ref SystemState state)
    {

    }


    void OnUpdate(ref SystemState state)
    {
        //MOUVEMENT
        var player_trans = SystemAPI.GetComponent<LocalTransform>(SystemAPI.GetSingletonEntity<PlayerData>());

        foreach (var (monster_data, trans, body) in SystemAPI.Query<RefRO<MonsterData>, RefRO<LocalTransform>, RefRW<PhyBodyData>>())
        {
            Vector3 moveDirection = player_trans.Position - trans.ValueRO.Position;

            //body.ValueRW.Force = monster_data.ValueRO.Speed * moveDirection.normalized * SystemAPI.Time.DeltaTime;


        }


    }


}