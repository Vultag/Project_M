using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;


public class OscillatorUI : MonoBehaviour
{
    [SerializeField]
    private TMP_Dropdown OCS1wavetable;
    [SerializeField]
    private TMP_Dropdown OCS2wavetable;
    [SerializeField]
    private Transform MixKnob;
    [SerializeField]
    private Transform OCS1fineKnob;
    [SerializeField]
    private Transform OCS2fineKnob;
    [SerializeField]
    private Transform OCSsemiKnob;

    public UIManager uiManager;

    private float3 OSC1wavetable;
    private float3 OSC2wavetable;

    private EntityManager entityManager;

    void Start()
    {
        //uiManager = FindFirstObjectByType<UIManager>().GetComponent<UIManager>();
        OSC1wavetable = new float3(1,0,0);
        OSC2wavetable = new float3(1, 0, 0);
        entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;
    }

    public void UpdateUI(SynthData synthData)
    {
        float OCS1mixValue = synthData.Osc1SinSawSquareFactor.x + synthData.Osc1SinSawSquareFactor.y + synthData.Osc1SinSawSquareFactor.z;
        OCS1wavetable.value = Mathf.CeilToInt(synthData.Osc1SinSawSquareFactor.y) + Mathf.CeilToInt(synthData.Osc1SinSawSquareFactor.z)*2;
        OCS2wavetable.value = Mathf.CeilToInt(synthData.Osc2SinSawSquareFactor.y) + Mathf.CeilToInt(synthData.Osc2SinSawSquareFactor.z) * 2;
        MixKnob.rotation = Quaternion.Euler(0, 0, (OCS1mixValue-0.5f)*2f*145f);
        OCS1fineKnob.rotation = Quaternion.Euler(0, 0, (synthData.Osc1Fine/30)* 145f);
        OCS2fineKnob.rotation = Quaternion.Euler(0, 0, (synthData.Osc2Fine / 30) * 145f);
        OCSsemiKnob.rotation = Quaternion.Euler(0, 0, (synthData.Osc2Fine / 36) * 145f);
    }

    /// <summary>
    /// Currenty only deal with sin, saw and square tables
    /// </summary>
    public void UIocs1WavetableChange(TMP_Dropdown dropdown)
    {
        SynthData newsynth = AudioLayoutStorageHolder.audioLayoutStorage.SynthsData[AudioLayoutStorage.activeSynthIdx];
        float dropdownItemValue = newsynth.Osc1SinSawSquareFactor.x + newsynth.Osc1SinSawSquareFactor.y + newsynth.Osc1SinSawSquareFactor.z;

        switch (dropdown.value)
        {
            case 0:
                newsynth.Osc1SinSawSquareFactor = new float3(dropdownItemValue,0,0);
                OSC1wavetable = new float3(1, 0, 0);
                break;
            case 1:
                newsynth.Osc1SinSawSquareFactor = new float3(0, dropdownItemValue, 0);
                OSC1wavetable = new float3(0, 1, 0);
                break;
            case 2:
                newsynth.Osc1SinSawSquareFactor = new float3(0, 0, dropdownItemValue);
                OSC1wavetable = new float3(0, 0, 1);
                break;

        }
        AudioLayoutStorageHolder.audioLayoutStorage.WriteModifySynth(newsynth);
    }
    public void UIocs2WavetableChange(TMP_Dropdown dropdown)
    {
        SynthData newsynth = AudioLayoutStorageHolder.audioLayoutStorage.SynthsData[AudioLayoutStorage.activeSynthIdx];
        float dropdownItemValue = newsynth.Osc2SinSawSquareFactor.x + newsynth.Osc2SinSawSquareFactor.y + newsynth.Osc2SinSawSquareFactor.z;

        switch (dropdown.value)
        {
            case 0:
                newsynth.Osc2SinSawSquareFactor = new float3(dropdownItemValue, 0, 0);
                OSC2wavetable = new float3(1, 0, 0);
                break;
            case 1:
                newsynth.Osc2SinSawSquareFactor = new float3(0, dropdownItemValue, 0);
                OSC2wavetable = new float3(0, 1, 0);
                break;
            case 2:
                newsynth.Osc2SinSawSquareFactor = new float3(0, 0, dropdownItemValue);
                OSC2wavetable = new float3(0, 0, 1);
                break;

        }
        AudioLayoutStorageHolder.audioLayoutStorage.WriteModifySynth(newsynth);
    }

    public String UIknobChange(KnobChangeType knobChangeType, float newRot)
    {
        SynthData newsynth = AudioLayoutStorageHolder.audioLayoutStorage.SynthsData[AudioLayoutStorage.activeSynthIdx];
        float factor = (newRot + 145) / 290;
        float increment = 0;
        String displayedValue = "";

        switch (knobChangeType)
        {
            case KnobChangeType.OCSmix:
                //Debug.Log((-Mathf.Sign(-newsynth.Osc1SinSawSquareFactor.x) + 1) * 0.5f);
                newsynth.Osc1SinSawSquareFactor = OSC1wavetable * factor;
                newsynth.Osc2SinSawSquareFactor = OSC2wavetable * (1 - factor);
                displayedValue = new String(Mathf.RoundToInt(((Vector3)newsynth.Osc1SinSawSquareFactor*100).magnitude).ToString() + "% - " + (Mathf.RoundToInt(((Vector3)newsynth.Osc2SinSawSquareFactor).magnitude * 100)).ToString()+ "%");
                break;
            case KnobChangeType.OCS1fine:
                increment = Mathf.Round((factor - 0.5f) * 2f * 30);
                newsynth.Osc1Fine = increment;
                newsynth.Osc2Fine = -increment;
                OCS2fineKnob.rotation = Quaternion.Euler(0, 0, -newRot);
                displayedValue = new String(increment.ToString() + " cents");
                break;
            case KnobChangeType.OCS2fine:
                increment = Mathf.Round((factor - 0.5f) * 2f * 30);
                newsynth.Osc1Fine = -increment;
                newsynth.Osc2Fine = increment;
                OCS1fineKnob.rotation = Quaternion.Euler(0, 0, -newRot);
                displayedValue = new String(increment.ToString() + " cents");
                break;
            case KnobChangeType.OCS2semi:
                increment = Mathf.Round(MathF.Abs((factor* 36)-36f));
                newsynth.Osc2Semi = increment;
                displayedValue = new String(increment.ToString() + " semi");

                break;
        }

        //Debug.LogError(AudioLayoutStorageHolder.audioLayoutStorage.SynthsData[0].Osc1SinSawSquareFactor);
        AudioLayoutStorageHolder.audioLayoutStorage.WriteModifySynth(newsynth);

        return displayedValue;
    }

}
