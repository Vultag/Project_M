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


    public Action<String, String> OnUpdateMode;
    public float modeSwitchBaseCD;
    private float modeSwitchCD;




    protected override void OnCreate()
    {
        input_actions = new PlayerControls();
        modeSwitchBaseCD = 7;
        modeSwitchCD = modeSwitchBaseCD;
    }
    protected override void OnStartRunning()
    {

        input_actions.Enable();
        //physics
        foreach (var (shape, trans) in SystemAPI.Query<RefRW<CircleShapeData>, RefRO<LocalTransform>>())
        {

            shape.ValueRW.Position = new Vector2(trans.ValueRO.Position.x, trans.ValueRO.Position.y);

        }

        OnUpdateMode(WeaponSystem.mode.ToString(), "C4");

    }
    protected override void OnUpdate()
    {

        var moveDirection = input_actions.ActionMap.Mouvements.ReadValue<Vector2>();

        ///default mouv
        //foreach (var (player_data, player_trans) in SystemAPI.Query<RefRO<PlayerData>, RefRW<LocalTransform>>())
        //{

        //    //LocalTransform player_trans = SystemAPI.GetComponent<LocalTransform>(entity);
        //    //var new_pos = player_trans.Position.x + player_data.ValueRO.mouv_speed * moveDirection;
        //    player_trans.ValueRW.Position += new float3(player_data.ValueRO.mouv_speed * moveDirection * SystemAPI.Time.DeltaTime, 0);



        ///TO DO NEXT : DYNAMIC PHYSICS

        //}
        ///test physics move
        ///SET IN A INPUT EVENT 
        foreach (var (player_data, player_phy) in SystemAPI.Query<RefRO<PlayerData>, RefRW<PhyBodyData>>())
        {


            //float tempMaxSpeed = player_data.ValueRO.mouv_speed*1.5f;

            //player_phy.ValueRW.Velocity += (player_data.ValueRO.mouv_speed*0.1f) * moveDirection * SystemAPI.Time.DeltaTime;

            player_phy.ValueRW.Velocity = player_data.ValueRO.mouv_speed * moveDirection * SystemAPI.Time.DeltaTime;


        }

        modeSwitchCD -= SystemAPI.Time.DeltaTime;

        if (modeSwitchCD < 0)
        {
            WeaponSystem.mode = (MusicUtils.MusicalMode)Mathf.Abs((int)WeaponSystem.mode - UnityEngine.Random.Range(1,5));
            OnUpdateMode(WeaponSystem.mode.ToString(), "C4");
            modeSwitchCD = modeSwitchBaseCD;
        }




    }
    protected override void OnStopRunning()
    {
        //input_actions.ActionMap.Tempo.performed -= updateTempo;
        input_actions.Disable();
    }


}