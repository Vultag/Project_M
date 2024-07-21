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

    }


}
