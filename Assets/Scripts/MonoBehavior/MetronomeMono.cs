using System.Collections;
using System.Collections.Generic;
using MusicNamespace;
using TMPro;
using Unity.Entities;
using UnityEngine;

public class MetronomeMono : MonoBehaviour
{
    [SerializeField]
    private TextMeshProUGUI TEMPOtext;
    [SerializeField]
    private Animator animator;
    [SerializeField]
    private AudioSource audioTick;



    private void OnEnable()
    {
        var UIsystem = World.DefaultGameObjectInjectionWorld.GetExistingSystemManaged<UIInputSystem>();
        UIsystem.OnUpdateTempo += ChangeTempo;
    }

    public void ChangeTempo(int change)
    {

        MusicUtils.BPM += change;
        TEMPOtext.text = MusicUtils.BPM.ToString();
        animator.SetFloat("BPM", MusicUtils.BPM / 60f);

    }

    public void MuteToggle()
    {
        audioTick.mute = !audioTick.mute;
    }

}
