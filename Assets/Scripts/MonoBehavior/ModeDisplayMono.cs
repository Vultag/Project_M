using System.Collections;
using System.Collections.Generic;
using System.Data;
using TMPro;
using Unity.Entities;
using UnityEngine;
using UnityEngine.UI;

public class ModeDisplayMono : MonoBehaviour
{

    [SerializeField]
    private TextMeshPro Modetext;
    [SerializeField]
    private TextMeshPro Tonictext;

    [SerializeField]
    private Image CDimg;

    private float fillSpeed;
    private float fill;


    private void OnEnable()
    {
        //dont work -> systembase
        var UIsystem = World.DefaultGameObjectInjectionWorld.GetExistingSystemManaged<UIInputSystem>();
        var Playersystem = World.DefaultGameObjectInjectionWorld.GetExistingSystemManaged<PlayerSystem>();
        fillSpeed = Playersystem.modeSwitchBaseCD;
        Playersystem.OnUpdateMode += ChangeMode;
    }

    private void ChangeMode(string newMode, string newTonic)
    {

        Modetext.text = newMode;
        Tonictext.text = newTonic;
        fill = 0; // makes sure mono and Isystem are in sync

    }

    private void Update()
    {
        fill += Time.deltaTime/fillSpeed;
        CDimg.fillAmount = fill;
        if (fill >= 1f)
        {
            fill = 0f;
        }
    }

}
