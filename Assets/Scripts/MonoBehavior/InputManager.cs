using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static UnityEngine.InputSystem.InputAction;
using UnityEngine.InputSystem;

public class InputManager : MonoBehaviour
{
    public static PlayerControls playerControls;

    public static Vector2 mousePos;
    //public static Vector2 mouseDelta;
    public static Vector2 playerMouvement;

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
    }


    private void OnPlayerShoot(CallbackContext context)
    {
        ///OPTI -> Activate 1 PlayPressed for all here and switch it at the end of the frame ?
        bool IsShooting = playerControls.ActionMap.Shoot.IsPressed();
        //Debug.Log(IsShooting);
        WeaponSystem.PlayPressed = IsShooting;
        WeaponSystem.PlayReleased = !IsShooting;
        PlaybackRecordSystem.ClickPressed = IsShooting;
        PlaybackRecordSystem.ClickReleased = !IsShooting;


    }
}
