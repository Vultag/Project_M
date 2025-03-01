using System;
using UnityEngine;
using UnityEngine.Rendering.Universal;
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

    UIControls UI_Controls;

    public static bool MouseOverUI;

    //private bool IsSidePanelOpen = true;
    private bool IsEditPanelOpen = true;

    private float currentZoom;
    private float zoomTarget;
    private float zoomDelta;


    void Start()
    {
        zoomTarget = Camera.main.orthographicSize;
        currentZoom = zoomTarget;
        UI_Controls = new UIControls();
        UI_Controls.Enable();

        UI_Controls.UI.Tabulation.performed += OnTabulationPressed;

        UI_Controls.UI.Zoom.performed += OnZoomTick;
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
        zoomTarget = Mathf.Max(zoomTarget - UI_Controls.UI.Zoom.ReadValue<Vector2>().y*0.01f,0);

    }

}
