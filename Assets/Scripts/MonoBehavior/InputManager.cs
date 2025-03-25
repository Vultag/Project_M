using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static UnityEngine.InputSystem.InputAction;
using UnityEngine.InputSystem;
using Unity.Entities;
using MusicNamespace;
using System;

public class InputManager : MonoBehaviour
{
    public static PlayerControls playerControls;
    [SerializeField]
    MetronomeEffectSpawner metronomeEffectSpawner;

    public static Vector2 mousePos;
    //public static Vector2 mouseDelta;
    public static Vector2 playerMouvement;

    public static float BeatProximityThreshold = 0.2f;
    /// To detect offbeat keys
    bool BeatNotYetPlayed = false;
    /// Keyswitch of two state to alternate the start/end press window and prevent multiple key press over the same subbeat
    /// 0 = start ; 1 = end
    ///short KeyPressingStage = 0;

    int CanPressKeySwitchState = 0;
    public static bool CanPressKey = true;
    private static bool _M1Pressed = false;
    public static bool KeyPressed => _M1Pressed; // Read-only
    private static bool _M1Released = false;
    public static bool KeyReleased => _M1Released; // Read-only


    void Start()
    {
        playerControls = new PlayerControls();
        playerControls.Enable();

        playerControls.ActionMap.Shoot.performed += OnPlayerShoot;
        playerControls.ActionMap.Shoot.canceled += OnPlayerShoot;
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
        /// testing
        //if (playerControls.ActionMap.Mouvements.ReadValue<Vector2>().x>0)
        //    Debug.Break();


        mousePos = playerControls.ActionMap.MousePos.ReadValue<Vector2>();
        playerMouvement = playerControls.ActionMap.Mouvements.ReadValue<Vector2>();
        //Debug.Log(mousePos);

        float normalizedProximity = ((float)MusicUtils.time % (60f / (MusicUtils.BPM * 4))) / (60f / (MusicUtils.BPM * 4));
        float BeatProximity = 1 - Mathf.Abs((normalizedProximity - 0.5f) * 2);
        //Debug.DrawLine(Vector3.zero, new Vector3(0, normalizedProximity * 10, 0));

        //WeaponSystem.OnBeat = BeatProximity < BeatProximityThreshold ? true:false;
        //PlaybackRecordSystem.OnBeat = BeatProximity < BeatProximityThreshold ? true : false;

        if(BeatProximity<BeatProximityThreshold && BeatNotYetPlayed)
        {
            ActivateKey();
            _M1Pressed = true;
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


    }


    /// Seperate the callback from the key press/release ? (if the key gets released from game logic)
    private void OnPlayerShoot(CallbackContext context)
    {
        ///OPTI -> Activate 1 PlayPressed for all here and switch it at the end of the frame ?
        bool IsShooting = playerControls.ActionMap.Shoot.IsPressed();


        /// Here For test -> move away in ifs
        float normalizedProximity = ((float)MusicUtils.time % (60f / (MusicUtils.BPM * 4))) / (60f / (MusicUtils.BPM * 4));
        float BeatProximity = 1 - Mathf.Abs((normalizedProximity - 0.5f) * 2);

        //bool hasAlreadyShot = 

        //Debug.Log(IsShooting);
        if (IsShooting)
        {
            if(CanPressKey)
            {
                if (BeatProximity <= BeatProximityThreshold)
                {
                    ActivateKey();
                    _M1Pressed = true;
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
            if(KeyPressed)
            {
                DeactivateKey();
                _M1Pressed = false;
                _M1Released = true;
            }
        }
    }



    private void ActivateKey()
    {
        _M1Pressed = true;
        CanPressKey = false;
        WeaponSystem.KeyJustPressed = true;
        PlaybackRecordSystem.KeyJustPressed = true;
        WeaponSystem.KeyJustReleased = false;
        PlaybackRecordSystem.KeyJustReleased = false;
    }
    private void DeactivateKey()
    {
        WeaponSystem.KeyJustPressed = false;
        PlaybackRecordSystem.KeyJustPressed = false;
        WeaponSystem.KeyJustReleased = true;
        PlaybackRecordSystem.KeyJustReleased = true;
    }



    private void FrameInputsCleanup()
    {
        _M1Pressed = false;
        _M1Released = false;
    }

}
