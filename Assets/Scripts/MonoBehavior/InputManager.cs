using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static UnityEngine.InputSystem.InputAction;
using UnityEngine.InputSystem;
using Unity.Entities;
using MusicNamespace;

public class InputManager : MonoBehaviour
{
    public static PlayerControls playerControls;
    [SerializeField]
    MetronomeEffectSpawner metronomeEffectSpawner;

    public static Vector2 mousePos;
    //public static Vector2 mouseDelta;
    public static Vector2 playerMouvement;

    float BeatProximityThreshold = 0.3f;

    void Start()
    {
        playerControls = new PlayerControls();
        playerControls.Enable();

        playerControls.ActionMap.Shoot.performed += OnPlayerShoot;
        playerControls.ActionMap.Shoot.canceled += OnPlayerShoot;
    }

    void Update()
    {

        mousePos = playerControls.ActionMap.MousePos.ReadValue<Vector2>();
        playerMouvement = playerControls.ActionMap.Mouvements.ReadValue<Vector2>();
        //Debug.Log(mousePos);

        float normalizedProximity = ((float)Time.time % (60f / (MusicUtils.BPM * 4))) / (60f / (MusicUtils.BPM * 4));
        float BeatProximity = 1 - Mathf.Abs((normalizedProximity - 0.5f) * 2);
        //Debug.DrawLine(Vector3.zero, new Vector3(0, BeatProximity * 10, 0));

        WeaponSystem.OnBeat = BeatProximity < BeatProximityThreshold ? true:false;
        PlaybackRecordSystem.OnBeat = BeatProximity < BeatProximityThreshold ? true : false;



    }


    private void OnPlayerShoot(CallbackContext context)
    {
        ///OPTI -> Activate 1 PlayPressed for all here and switch it at the end of the frame ?
        bool IsShooting = playerControls.ActionMap.Shoot.IsPressed();


        /// Here For test -> move away in ifs
        float normalizedProximity = ((float)Time.time % (60f / (MusicUtils.BPM * 4))) / (60f / (MusicUtils.BPM * 4));
        float BeatProximity = 1 - Mathf.Abs((normalizedProximity - 0.5f) * 2);


        //Debug.Log(IsShooting);
        if (IsShooting)
        {


            //Debug.Log(BeatProximity);
            //Debug.DrawLine(new Vector3(5, 0, 0), new Vector3(5, BeatProximity * 10, 0), Color.red, 0.4f);

            if (BeatProximity < BeatProximityThreshold)
            {
                WeaponSystem.PlayPressed = true;
                PlaybackRecordSystem.ClickPressed = true;
                WeaponSystem.PlayReleased = false;
                PlaybackRecordSystem.ClickReleased = false;
                metronomeEffectSpawner.SpawnValidKeySpite();
            }
            else
            {
                WeaponSystem.PlayPressed = false;
                PlaybackRecordSystem.ClickPressed = false;
                WeaponSystem.PlayReleased = true;
                PlaybackRecordSystem.ClickReleased = true;
                metronomeEffectSpawner.SpawnInvalidKeySpite();
            }

        }
        else
        {
            WeaponSystem.PlayPressed = false;
            PlaybackRecordSystem.ClickPressed = false;
            WeaponSystem.PlayReleased = true;
            PlaybackRecordSystem.ClickReleased = true;
        }



    }
}
