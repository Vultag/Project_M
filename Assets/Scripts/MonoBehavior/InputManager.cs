using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static UnityEngine.InputSystem.InputAction;
using UnityEngine.InputSystem;
using Unity.Entities;
using MusicNamespace;
using System;
using Unity.Collections;

/// <summary>
/// For ECS world
/// </summary>
public struct CentralizedInputData : IComponentData
{
    public Vector2 playerMouvements;
    public bool shootJustPressed;
    public bool shootJustReleased;

    public bool space;

    public bool R;

    public bool one;
    public bool two;
    public bool three;
    public bool four;
    public bool five;
    public bool six;
    public bool seven;
}
/// <summary>
/// For Mono world
/// </summary>
//public struct Inputs
//{
//    public bool M1Pressed;
//    public bool M1JustPressed;
//    public bool M1JustReleased;
//}


public class InputManager : MonoBehaviour
{
    public static PlayerControls playerControls;
    [SerializeField]
    MetronomeEffectSpawner metronomeEffectSpawner;

    public static Vector2 mousePos;
    //public static Vector2 mouseDelta;
    // public static Vector2 playerMouvement;

    public static float BeatProximityThreshold = 0.2f;
    /// To detect offbeat keys
    bool BeatNotYetPlayed = false;
    float BeatProximity;
    public static bool CanPressKey;
    int CanPressKeySwitchState = 0;

    //private Inputs inputs;
    private CentralizedInputData CentralizedInputData;

    private EntityQuery CentralizedInputDataQuery;


    void Start()
    {
        playerControls = new PlayerControls();
        playerControls.PlayerMap.Enable();

        //inputs = new Inputs();
        CentralizedInputData = new CentralizedInputData();

        playerControls.PlayerMap.Shoot.performed += OnPlayerShoot;
        playerControls.PlayerMap.Shoot.canceled += OnPlayerShoot;

        var world = World.DefaultGameObjectInjectionWorld;
        var em = world.EntityManager;
        CentralizedInputDataQuery = em.CreateEntityQuery(typeof(CentralizedInputData));
        em.CreateEntity(em.CreateArchetype(typeof(CentralizedInputData)));
        //em.AddComponent<CentralizedInputData>(CentralizedInputEntity);

    }
    //void OnEnable()
    //{
    //    Application.onBeforeRender += FrameInputsCleanup;
    //}
    //void OnDisable()
    //{
    //    Application.onBeforeRender -= FrameInputsCleanup;
    //}


    void Update()
    {


        mousePos = playerControls.PlayerMap.MousePos.ReadValue<Vector2>();
        CentralizedInputData.playerMouvements = playerControls.PlayerMap.Mouvements.ReadValue<Vector2>();

        //var newinputs = new Inputs
        //{
        //    M1JustPressed = inputs.M1JustPressed,
        //    M1JustReleased = inputs.M1JustReleased,
        //    M1Pressed = playerControls.PlayerMap.Shoot.IsPressed(),
        //};


        //Debug.Log(mousePos);

        float normalizedProximity = ((float)MusicUtils.time % (60f / (MusicUtils.BPM * 4))) / (60f / (MusicUtils.BPM * 4));
        float BeatProximity = 1 - Mathf.Abs((normalizedProximity - 0.5f) * 2);

        //Debug.DrawLine(Vector3.zero, new Vector3(0, normalizedProximity * 10, 0));

        //WeaponSystem.OnBeat = BeatProximity < BeatProximityThreshold ? true:false;
        //PlaybackRecordSystem.OnBeat = BeatProximity < BeatProximityThreshold ? true : false;


        if (BeatProximity<BeatProximityThreshold && BeatNotYetPlayed)
        {
            ActivateKey();
            CanPressKey = false;
            BeatNotYetPlayed = false;
            metronomeEffectSpawner.SpawnValidKeySpite();
        }
        /// Could malfunction if the framerate drops low enough ? (bypass state change)
        /// reset the ability press a key (offseted by the BeatProximityThreshold)
        float switchOffset = (2 - BeatProximityThreshold);
        if (Mathf.FloorToInt(((MusicUtils.time* (MusicUtils.BPM/60f) * 4) + switchOffset) %2f) != CanPressKeySwitchState)
        {
            //Debug.Log("switch:     "+ (float)MusicUtils.time);
            CanPressKey = true;
            CanPressKeySwitchState = 1-CanPressKeySwitchState;
        }

        // Push to ECS
        var world = World.DefaultGameObjectInjectionWorld;
        var em = world.EntityManager;

        //Debug.Log(CentralizedInputData.shootJustPressed);

        em.SetComponentData(CentralizedInputDataQuery.GetSingletonEntity(), CentralizedInputData);
        /// clear for next frame
        CentralizedInputData = new CentralizedInputData();

    }


    /// Seperate the callback from the key press/release ? (if the key gets released from game logic)
    private void OnPlayerShoot(CallbackContext context)
    {
        ///OPTI -> Activate 1 PlayPressed for all here and switch it at the end of the frame ?
        bool IsShooting = playerControls.PlayerMap.Shoot.IsPressed();

        /// Here For test -> move away in ifs
        float normalizedProximity = ((float)MusicUtils.time % (60f / (MusicUtils.BPM * 4))) / (60f / (MusicUtils.BPM * 4));
        BeatProximity = 1 - Mathf.Abs((normalizedProximity - 0.5f) * 2);

        //bool hasAlreadyShot = 

        //Debug.Log(IsShooting);
        if (IsShooting)
        {
            if (CanPressKey)
            {
                if (BeatProximity <= BeatProximityThreshold)
                {
                    ActivateKey();
                    //inputs.M1JustPressed = true;
                    metronomeEffectSpawner.SpawnValidKeySpite();
                }
                else
                {
                    BeatNotYetPlayed = true;
                }
            }
            else
                BeatNotYetPlayed = true;
        }
        else
        {
            if (BeatNotYetPlayed)
            {
                CanPressKey = true;
                BeatNotYetPlayed = false;
                metronomeEffectSpawner.SpawnInvalidKeySpite();
            }
            DeactivateKey();
            //if (inputs.M1Pressed)
            //{
            //    DeactivateKey();
            //    inputs.M1JustReleased = true;
            //}
        }
    }



    private void ActivateKey()
    {
        CanPressKey = false;
        CentralizedInputData.shootJustPressed = true;
        CentralizedInputData.shootJustReleased = false;
        //WeaponSystem.KeyJustPressed = true;
        //PlaybackRecordSystem.KeyJustPressed = true;
        //WeaponSystem.KeyJustReleased = false;
        //PlaybackRecordSystem.KeyJustReleased = false;

        //MachineDrumSystem.PadJustPressed = true;
        //MachineDrumSystem.PadJustReleased = false;
    }
    private void DeactivateKey()
    {
        CentralizedInputData.shootJustPressed = false;
        CentralizedInputData.shootJustReleased = true;
        //WeaponSystem.KeyJustPressed = false;
        //PlaybackRecordSystem.KeyJustPressed = false;
        //WeaponSystem.KeyJustReleased = true;
        //PlaybackRecordSystem.KeyJustReleased = true;

        //MachineDrumSystem.PadJustReleased = true;
        //MachineDrumSystem.PadJustPressed = false;
    }

}
