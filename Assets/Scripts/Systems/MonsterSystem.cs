
using Unity.Entities;
using Unity.Transforms;
using UnityEngine;

[UpdateInGroup(typeof(GameSimulationSystemGroup), OrderFirst = true)]
public partial struct MonsterSystem : ISystem
{


    public void OnCreate(ref SystemState state)
    {

        state.RequireForUpdate<PlayerData>();
    }


    public void OnUpdate(ref SystemState state)
    {
        EntityCommandBuffer ecb = SystemAPI.GetSingleton<BeginSimulationEntityCommandBufferSystem.Singleton>().CreateCommandBuffer(state.WorldUnmanaged);
        //MOUVEMENT
        var player_trans = SystemAPI.GetComponent<LocalTransform>(SystemAPI.GetSingletonEntity<PlayerData>());

        foreach (var (monster_data, trans, body) in SystemAPI.Query<RefRO<MonsterData>, RefRO<LocalTransform>, RefRW<PhyBodyData>>())
        {
            FlowfieldCellData agentCell = FlowfieldGridStorage.GetCellFromPosition(trans.ValueRO.Position);
            Vector2 moveDirection = agentCell.InLineOfSight ? new Vector2(player_trans.Position.x - trans.ValueRO.Position.x, player_trans.Position.y - trans.ValueRO.Position.y).normalized : agentCell.Direction;
            //Debug.Log(moveDirection);
            body.ValueRW.Force += moveDirection * 0.03f * SystemAPI.Time.DeltaTime;

            //Vector2 moveDirection = new float2(player_trans.Position.x - trans.ValueRO.Position.x, player_trans.Position.y - trans.ValueRO.Position.y);
            ////body.ValueRW.Velocity = monster_data.ValueRO.Speed * moveDirection.normalized * SystemAPI.Time.DeltaTime *4f;
            //body.ValueRW.Force += monster_data.ValueRO.Speed * moveDirection.normalized * 0.001f;

        }


    }


}