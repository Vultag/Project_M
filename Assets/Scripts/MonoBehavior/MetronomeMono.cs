using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.Entities;
using UnityEngine;

public class MetronomeMono : MonoBehaviour
{
    [SerializeField]
    private TextMeshPro TEMPOtext;
    [SerializeField]
    private Animator animator;
    [SerializeField]
    private AudioSource audioTick;



    private void OnEnable()
    {
        //dont work -> systembase
        var UIsystem = World.DefaultGameObjectInjectionWorld.GetExistingSystemManaged<UIInputSystem>();
        UIsystem.OnUpdateTempo += ChangeTempo;
    }

    private void ChangeTempo(int newtempo)
    {

        TEMPOtext.text = newtempo.ToString();
        animator.SetFloat("BPM", newtempo/60f);

    }
  

}
