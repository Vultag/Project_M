using System.Collections;
using System.Collections.Generic;
using NUnit.Framework.Internal;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;
using static UnityEngine.GraphicsBuffer;
using static UnityEngine.InputSystem.InputAction;
using MusicNamespace;
using UnityEngine.Animations;
using System;


[UpdateInGroup(typeof(GameSimulationSystemGroup))]
public partial class PlayerSystem : SystemBase
{

    //MOVE AWAY INPUT INTO SINGLE FILE ?

    PlayerControls input_actions;

    //private InputAction Mouv_pressed;



    protected override void OnCreate()
    {
        input_actions = new PlayerControls();
    }
    protected override void OnStartRunning()
    {

        input_actions.Enable();
        //physics
        foreach (var (shape, trans) in SystemAPI.Query<RefRW<CircleShapeData>, RefRO<LocalTransform>>())
        {

            shape.ValueRW.Position = new Vector2(trans.ValueRO.Position.x, trans.ValueRO.Position.y);

        }

        //input_actions.ActionMap.Tempo.performed += ctx => updateTempo(ctx);

    }
    protected override void OnUpdate()
    {

        var moveDirection = input_actions.ActionMap.Mouvements.ReadValue<Vector2>();

        //default mouv
        foreach (var (player_data, player_trans) in SystemAPI.Query<RefRO<PlayerData>, RefRW<LocalTransform>>())
        {

            //LocalTransform player_trans = SystemAPI.GetComponent<LocalTransform>(entity);
            //var new_pos = player_trans.Position.x + player_data.ValueRO.mouv_speed * moveDirection;
            player_trans.ValueRW.Position += new float3(player_data.ValueRO.mouv_speed * moveDirection * SystemAPI.Time.DeltaTime, 0);


        }

        //test physics move
        //foreach (var (player_data, player_phy) in SystemAPI.Query<RefRO<PlayerData>, RefRW<PhyBodyData>>())
        //{

        //    player_phy.ValueRW.Velocity = player_data.ValueRO.mouv_speed * moveDirection * SystemAPI.Time.DeltaTime;


        //}

        //if (input_actions.ActionMap.Tempo.IsPressed())
        //{
        //    int delta = (int)input_actions.ActionMap.Tempo.ReadValue<float>();
        //    MusicUtils.BPM += delta;
        //    //to remove ?
        //    UpdateTempo.Invoke(MusicUtils.BPM);
        //    //Debug.Log(MusicUtils.BPM);
        //}


    }
    protected override void OnStopRunning()
    {
        //input_actions.ActionMap.Tempo.performed -= updateTempo;
        input_actions.Disable();
    }

    //void updateTempo(InputAction.CallbackContext context)
    //{
    //    Debug.Log(context.action.ReadValue<float>());
    //    MusicUtils.BPM += (int)context.action.ReadValue<float>();
    //    Debug.Log(MusicUtils.BPM);

    //}
    

}