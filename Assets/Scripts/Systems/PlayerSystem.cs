using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;
using MusicNamespace;
using System;
using Unity.Rendering;


[UpdateInGroup(typeof(FixedStepGameSimulationSystemGroup))]
public partial class PlayerSystem : SystemBase
{

    //MOVE AWAY INPUT INTO SINGLE FILE ?

    //PlayerControls input_actions;


    public Action<String, String> OnUpdateMode;
    public float modeSwitchBaseCD;
    private float modeSwitchCD;

    private float propellerCurrentScale;
    private float propellerScalingSpeed = 0.2f;


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

        //var Player_query = EntityManager.CreateEntityQuery(typeof(PlayerData));
        //Entity player_entity = Player_query.GetSingletonEntity();
        //Entity start_weapon = EntityManager.GetComponentData<PlayerData>(player_entity).MainCanon;

        //var ECB = World.GetOrCreateSystemManaged<BeginSimulationEntityCommandBufferSystem>().CreateCommandBuffer();

        ///// hide Weapon and DMachine on main slot at start
        //var playerWeaponSpriteEntity = EntityManager.GetBuffer<Child>(start_weapon)[0].Value;
        //var playerDMachineSpriteEntity = EntityManager.GetBuffer<Child>(start_weapon)[1].Value;
        //MaterialMeshInfo newWeaponMaterialMeshInfo = EntityManager.GetComponentData<MaterialMeshInfo>(playerWeaponSpriteEntity);
        //newWeaponMaterialMeshInfo.Mesh = 0;
        //ECB.SetComponent<MaterialMeshInfo>(playerWeaponSpriteEntity, newWeaponMaterialMeshInfo);
        //MaterialMeshInfo newDMachineMaterialMeshInfo = EntityManager.GetComponentData<MaterialMeshInfo>(playerDMachineSpriteEntity);
        //newDMachineMaterialMeshInfo.Mesh = 0;
        //ECB.SetComponent<MaterialMeshInfo>(playerDMachineSpriteEntity, newDMachineMaterialMeshInfo);

    }


    protected override void OnUpdate()
    {

        var moveDirection = InputManager.playerMouvement;

        //Camera cam = CameraSingleton.Instance.MainCamera;

        ///test physics move
        ///SET IN A INPUT EVENT ?
        foreach (var (player_data, player_phy,trans) in SystemAPI.Query<RefRW<PlayerData>, RefRW<PhyBodyData>, RefRO<LocalTransform>>())
        {
            float3 forward = trans.ValueRO.Up();
            //var newPropellerStrenght = math.lerp(0,Mathf.Sign(moveDirection.y) - player_data.ValueRO.propellerBackpedalRelativeStrenght, player_data.ValueRO.propellerChargeSpeed*);
            //var newPropellerStrenghtFactor = player_data.ValueRO.propellerStrenghtFactor;
            //newPropellerStrenghtFactor = moveDirection.y != 0 ? math.min(newPropellerStrenghtFactor+player_data.ValueRO.propellerChargeSpeed,1) : math.max(newPropellerStrenghtFactor - player_data.ValueRO.propellerChargeSpeed, 0);
            
            var PropellerStrenght = moveDirection.y > 0 ? moveDirection.y * player_data.ValueRO.propellerMaxStrenght : moveDirection.y * player_data.ValueRO.propellerBackpedalRelativeStrenght * player_data.ValueRO.propellerMaxStrenght;
          
            player_phy.ValueRW.Force += PropellerStrenght * new Vector2(forward.x, forward.y);
            player_phy.ValueRW.AngularForce += -player_data.ValueRO.rotate_speed * moveDirection.x;
            var newPropellerTrans = EntityManager.GetComponentData<LocalTransform>(player_data.ValueRO.Propeller);
            propellerCurrentScale = Mathf.Clamp01(propellerCurrentScale + propellerScalingSpeed * (math.sign(moveDirection.y) - 0.5f) * 2);
            newPropellerTrans.Scale = propellerCurrentScale;

            EntityManager.SetComponentData<LocalTransform>(player_data.ValueRO.Propeller, newPropellerTrans);

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