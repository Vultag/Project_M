using System.Collections;
using System.Collections.Generic;
using MusicNamespace;
using TMPro;
using Unity.Entities;
using UnityEngine;

public class MetronomeMono : MonoBehaviour
{
    [SerializeField]
    private TextMeshProUGUI BPMtext;
    [SerializeField]
    private AudioSource audioTickOnBeat;
    [SerializeField]
    private AudioSource audioTickQuarterBeat;
    [SerializeField]
    private Material MetronomeMat;

    float localTime =0;
    float QuarterBeatTimeCount = 0;

    private void OnEnable()
    {
        var UIsystem = World.DefaultGameObjectInjectionWorld.GetExistingSystemManaged<UIInputSystem>();
        UIsystem.OnUpdateTempo += ChangeTempo;
        MetronomeMat.SetFloat("_BPMnormalized", MusicUtils.BPM / 60f);
    }
    private void Update()
    {
        MusicUtils.time += Time.deltaTime;
        localTime += Time.deltaTime;
        QuarterBeatTimeCount += Time.deltaTime;
        if (localTime > 15f/ MusicUtils.BPM)
        {
            if (QuarterBeatTimeCount > 60f / MusicUtils.BPM)
            { 
                audioTickOnBeat.Play();
                QuarterBeatTimeCount = Time.time % (60f / MusicUtils.BPM);
            }
            else
            {
                //audioTickQuarterBeat.Play();
            }
            localTime = (float)(MusicUtils.time) % (15f / MusicUtils.BPM);
        }
    }

    public void ChangeTempo(int change)
    {

        MusicUtils.BPM = Mathf.Clamp(MusicUtils.BPM+change,1,240);
        BPMtext.text = MusicUtils.BPM.ToString();

        MetronomeMat.SetFloat("_BPMnormalized", MusicUtils.BPM / 60f);
    }

    public void MuteToggle()
    {
        audioTickOnBeat.mute = !audioTickOnBeat.mute;
        audioTickQuarterBeat.mute = !audioTickQuarterBeat.mute;
    }

}
