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

    //PlayerControls input_actions;


    public Action<String, String> OnUpdateMode;
    public float modeSwitchBaseCD;
    private float modeSwitchCD;






    protected override void OnCreate()
    {
        //input_actions = new PlayerControls();
        modeSwitchBaseCD = 7;
        modeSwitchCD = modeSwitchBaseCD;
    }
    protected override void OnStartRunning()
    {

        //input_actions.Enable();
        //input_actions.ActionMap.Shoot.performed += OnPlayerShoot;
        //input_actions.ActionMap.Shoot.canceled += OnPlayerShoot;
        //physics
        foreach (var (shape, trans) in SystemAPI.Query<RefRW<CircleShapeData>, RefRO<LocalTransform>>())
        {

            shape.ValueRW.Position = new Vector2(trans.ValueRO.Position.x, trans.ValueRO.Position.y);

        }

        ///OnUpdateMode(WeaponSystem.mode.ToString(), "C4");

    }


    protected override void OnUpdate()
    {

        var moveDirection = InputManager.playerMouvement;


        ///test physics move
        ///SET IN A INPUT EVENT ?
        foreach (var (player_data, player_phy) in SystemAPI.Query<RefRO<PlayerData>, RefRW<PhyBodyData>>())
        {
            player_phy.ValueRW.Force = player_data.ValueRO.mouv_speed * moveDirection * SystemAPI.Time.DeltaTime;

        }

        ///modeSwitchCD -= SystemAPI.Time.DeltaTime;

        if (modeSwitchCD < 0)
        {
            Debug.Log("mode change");
            WeaponSystem.mode = (MusicUtils.MusicalMode)Mathf.Abs((int)WeaponSystem.mode - UnityEngine.Random.Range(1,5));
            OnUpdateMode(WeaponSystem.mode.ToString(), "C4");
            modeSwitchCD = modeSwitchBaseCD;
        }

    }


    //private void OnPlayerShoot(CallbackContext context)
    //{
    //    ///OPTI -> Activate 1 PlayPressed for all here and switch it at the end of the frame ?
    //    bool IsShooting = input_actions.ActionMap.Shoot.IsPressed();
    //    //Debug.Log(UIInputSystem.MouseOverUI);
    //    WeaponSystem.PlayPressed = IsShooting;
    //    WeaponSystem.PlayReleased = !IsShooting;
    //    PlaybackRecordSystem.ClickPressed = IsShooting;
    //    PlaybackRecordSystem.ClickReleased = !IsShooting;


    //}

    //protected override void OnStopRunning()
    //{
    //    input_actions.ActionMap.Shoot.performed -= OnPlayerShoot;
    //    input_actions.ActionMap.Shoot.canceled -= OnPlayerShoot;
    //    input_actions.Disable();
    //}



}