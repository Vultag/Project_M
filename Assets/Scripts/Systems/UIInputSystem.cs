using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using UnityEngine;
using MusicNamespace;
using UnityEngine.InputSystem;

public partial class UIInputSystem : SystemBase
{

    public Action<int> OnUpdateTempo;

    UIControls input_actions;

    protected override void OnCreate()
    {
        input_actions = new UIControls();
    }

    protected override void OnStartRunning()
    {
        input_actions.Enable();
    }

    protected override void OnUpdate()
    {


        if (input_actions.UI.TEMPO.IsPressed())
        {
            int delta = (int)input_actions.UI.TEMPO.ReadValue<float>();
            MusicUtils.BPM += delta;
            //to remove ?
            OnUpdateTempo.Invoke(MusicUtils.BPM);
            //Debug.Log(MusicUtils.BPM);
        }

    }


    //public void updateTempo(InputAction.CallbackContext context)
    //{
    //    Debug.Log(context.action.ReadValue<float>());
    //    MusicUtils.BPM += (int)context.action.ReadValue<float>();
    //    Debug.Log(MusicUtils.BPM);

    //}

}
