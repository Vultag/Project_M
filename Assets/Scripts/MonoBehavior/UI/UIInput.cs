using System;
using UnityEngine;
using static UnityEngine.InputSystem.InputAction;

public partial class UIInput : MonoBehaviour
{

    [SerializeField]
    UIManager UImanager;
    [SerializeField]
    GameObject SynthEditPanelGB;
    [SerializeField]
    GameObject SynthPlaybackPanelGB;

    public Action<int> OnUpdateTempo;

    PlayerControls.UI_MapActions UI_Controls;

    public static bool MouseOverUI;

    //private bool IsSidePanelOpen = true;
    private bool IsEditPanelOpen = true;

    private float currentZoom;
    private float zoomTarget;
    private float zoomDelta;

    [SerializeField]
    Vector2 MinMaxZoom;

    void Start()
    {
        zoomTarget = Camera.main.orthographicSize;
        currentZoom = zoomTarget;
        UI_Controls = InputManager.playerControls.UI_Map;
        UI_Controls.Enable();

        UI_Controls.Tabulation.performed += OnTabulationPressed;

        UI_Controls.Zoom.performed += OnZoomTick;

        UI_Controls.Space.performed += OnSpacePressed;
        UI_Controls._1.performed += On1Pressed;
        UI_Controls._2.performed += On2Pressed;
        UI_Controls._3.performed += On3Pressed;
        UI_Controls._4.performed += On4Pressed;
        UI_Controls._5.performed += On5Pressed;
        UI_Controls._6.performed += On6Pressed;
        UI_Controls._7.performed += On7Pressed;
        UI_Controls.R.performed += OnRPressed;


    }


    private void Update()
    {
        ///ZOOM
        if (zoomTarget != currentZoom)
        {
            float smoothing = 3f;
            Camera.main.orthographicSize = Mathf.Lerp(currentZoom, zoomTarget, 1f - Mathf.Exp(-smoothing * zoomDelta));
            zoomDelta += Time.deltaTime;
        }
    }



    private void OnTabulationPressed(CallbackContext context)
    {
        /// interchange pannels

        /// OPTI 
        Animator editAnimator = SynthEditPanelGB.GetComponent<Animator>();
        Animator playbackAnimator = SynthPlaybackPanelGB.GetComponent<Animator>();

        UImanager.ForceDisableTooltip();
        float clipTime = Mathf.Clamp01(editAnimator.GetCurrentAnimatorStateInfo(0).normalizedTime);
        if (IsEditPanelOpen)
        {
            editAnimator.Play("SidePanelDeactivation", 0, 1-clipTime);
            playbackAnimator.Play("SidePanelActivation", 0, 1-clipTime);
        }
        else
        {
            editAnimator.Play("SidePanelActivation", 0, 1-clipTime);
            playbackAnimator.Play("SidePanelDeactivation", 0, 1-clipTime);
        }
        IsEditPanelOpen = !IsEditPanelOpen;
    }

    private void OnZoomTick(CallbackContext context)
    {
        zoomDelta = 0;
        currentZoom = Camera.main.orthographicSize;
        zoomTarget = Mathf.Clamp(Mathf.Max(zoomTarget - UI_Controls.Zoom.ReadValue<Vector2>().y*0.01f,0),MinMaxZoom.x,MinMaxZoom.y);

    }

    private void OnSpacePressed(CallbackContext context)
    {
        if (UImanager.UIplaybacksHolder.PBholders[UImanager.activeEquipmentIdx].AutoPlayOn == true | UImanager.equipmentToolBar.transform.GetChild(UImanager.activeEquipmentIdx).GetComponent<EquipmentUIelement>().ActivationPrepairing)
        {
            UImanager.equipmentToolBar.transform.GetChild(UImanager.activeEquipmentIdx).GetComponent<EquipmentUIelement>()._StopAutoPlay();
        }
        else if(UImanager.UIplaybacksHolder.PBholders[UImanager.activeEquipmentIdx].ContainerNumber>0)
        {
            UImanager.equipmentToolBar.transform.GetChild(UImanager.activeEquipmentIdx).GetComponent<EquipmentUIelement>()._StartAutoPlay();
        }
    }

    private void OnRPressed(CallbackContext context)
    {
        UImanager.equipmentToolBar.transform.GetChild(UImanager.activeEquipmentIdx).GetComponent<EquipmentUIelement>()._PrepairRecord();
    }

    private void On7Pressed(CallbackContext context)
    {
        UImanager.equipmentToolBar.transform.GetChild(6).GetComponent<EquipmentUIelement>()._selectThisEquipment();
    }

    private void On6Pressed(CallbackContext context)
    {
        UImanager.equipmentToolBar.transform.GetChild(5).GetComponent<EquipmentUIelement>()._selectThisEquipment();
    }

    private void On5Pressed(CallbackContext context)
    {
        UImanager.equipmentToolBar.transform.GetChild(4).GetComponent<EquipmentUIelement>()._selectThisEquipment();
    }

    private void On4Pressed(CallbackContext context)
    {
        UImanager.equipmentToolBar.transform.GetChild(3).GetComponent<EquipmentUIelement>()._selectThisEquipment();
    }

    private void On3Pressed(CallbackContext context)
    {
        UImanager.equipmentToolBar.transform.GetChild(2).GetComponent<EquipmentUIelement>()._selectThisEquipment();
    }

    private void On2Pressed(CallbackContext context)
    {
        UImanager.equipmentToolBar.transform.GetChild(1).GetComponent<EquipmentUIelement>()._selectThisEquipment();
    }
    private void On1Pressed(CallbackContext context)
    {
        UImanager.equipmentToolBar.transform.GetChild(0).GetComponent<EquipmentUIelement>()._selectThisEquipment();
    }

}
