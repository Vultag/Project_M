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


    void Start()
    {
        UI_Controls = new UIControls();
        UI_Controls.Enable();

        UI_Controls.UI.Tabulation.performed += OnTabulationPressed;
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

        //float clipTime = Mathf.Clamp01(editAnimator.GetCurrentAnimatorStateInfo(0).normalizedTime);
        //if (IsEditPanelOpen)
        //{
        //    editAnimator.Play("SidePanelActivation", 0, clipTime);
        //    editAnimator.SetFloat("speed", -1f);
        //    playbackAnimator.Play("SidePanelActivation", 0, 1-clipTime);
        //    playbackAnimator.SetFloat("speed", 1f);
        //}
        //else
        //{
        //    editAnimator.Play("SidePanelActivation", 0, clipTime);
        //    editAnimator.SetFloat("speed", 1f);
        //    playbackAnimator.Play("SidePanelActivation", 0, 1-clipTime);
        //    playbackAnimator.SetFloat("speed", -1f);
        //}



        IsEditPanelOpen = !IsEditPanelOpen;
    }



}
