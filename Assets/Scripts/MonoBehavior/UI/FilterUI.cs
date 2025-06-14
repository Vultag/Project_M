using System;
using System.Collections;
using System.Collections.Generic;
using System.Numerics;
using TMPro;
using Unity.Entities;
using Unity.Mathematics;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;
using static UnityEngine.Rendering.DebugUI;
using Quaternion = UnityEngine.Quaternion;
using Random = UnityEngine.Random;
using Vector3 = UnityEngine.Vector3;

public class FilterUI : MonoBehaviour,IKnobController
{

    public UIManager uiManager;
    [SerializeField]
    private FliterToShader filterToShader;
    [SerializeField]
    private Image FilterColorImage;
    [SerializeField]
    private Transform filterCutoffKnob;
    [SerializeField]
    private Transform filterResonanceKnob;
    [SerializeField]
    private Transform filterEnvelopeAmountKnob;

    private float filterCutoff;
    private float filterResonance;
    private float filterEnvelope;

    private EntityManager entityManager;

    UIManager IKnobController.uiManager => uiManager;

    void Awake()
    {
        entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;
        filterCutoff = 1;
        filterResonance = 0;
        filterEnvelope = 0;
    }

    public void UpdateUI(SynthData synthData)
    {
        filterCutoff = synthData.filter.Cutoff;
        filterResonance = synthData.filter.Resonance;
        filterEnvelope = synthData.filterEnvelopeAmount;
        filterToShader.ModifyFilter(synthData.filter.Cutoff, synthData.filter.Resonance,synthData.filterEnvelopeAmount);
        filterCutoffKnob.rotation = Quaternion.Euler(0, 0, ((1 - synthData.filter.Cutoff) - 0.5f) * 2f * 145f);
        filterResonanceKnob.rotation = Quaternion.Euler(0, 0, ((1-synthData.filter.Resonance) - 0.5f) * 2f * 145f);
        filterEnvelopeAmountKnob.rotation = Quaternion.Euler(0, 0, ((1-synthData.filterEnvelopeAmount) - 0.5f) * 2f * 145f);
        filterCutoffKnob.GetComponent<KnobMono>().displayedValue = string.Format("{0:0.0}", (1 - synthData.filter.Cutoff));
        filterResonanceKnob.GetComponent<KnobMono>().displayedValue = string.Format("{0:0.0}", (1 - synthData.filter.Resonance));
        filterCutoffKnob.GetComponent<KnobMono>().displayedValue = string.Format("{0:0.0}", (1 - synthData.filterEnvelopeAmount));
    }
    public void InitiateFilter()
    {
        SynthData newsynth = AudioLayoutStorageHolder.audioLayoutStorage.AuxillarySynthsData[AudioLayoutStorage.activeSynthIdx];

        var newFilterType = (short)Random.Range(0, 3);
        var newCutoff = Random.Range(0f, 1f);

        filterCutoff = newCutoff;
        filterResonance = 0;
        filterEnvelope = 0;
        newsynth.filter.Cutoff = filterCutoff;
        newsynth.filter.Resonance = filterResonance;
        newsynth.filterEnvelopeAmount = 0;
        newsynth.filterType = newFilterType;
        filterCutoffKnob.rotation = Quaternion.Euler(0, 0, ((1 - newsynth.filter.Cutoff) - 0.5f) * 2f * 145f);
        filterResonanceKnob.rotation = Quaternion.Euler(0, 0, ((1 - newsynth.filter.Resonance) - 0.5f) * 2f * 145f);
        filterEnvelopeAmountKnob.rotation = Quaternion.Euler(0, 0, ((1 - newsynth.filterEnvelopeAmount) - 0.5f) * 2f * 145f);
        filterToShader.ModifyFilter(filterCutoff, filterResonance, filterEnvelope);
        float3 color = GetColorFromFilter(filterCutoff, filterResonance, newsynth.filterType);
        FilterColorImage.color = new Color { r = color.x, g = color.y, b = color.z, a = 1.0f };

        filterToShader.SwitchFilterShader(newFilterType);
        AudioLayoutStorageHolder.audioLayoutStorage.WriteModifySynth(newsynth);
    }
    public void ReviewActivatedFeatures(bool[] activatedFeatues)
    {
        filterResonanceKnob.parent.gameObject.SetActive(activatedFeatues[4]);
        filterEnvelopeAmountKnob.parent.gameObject.SetActive(activatedFeatues[5]);
    }
    public void ActivateFilterResonance()
    {
        SynthData newsynth = AudioLayoutStorageHolder.audioLayoutStorage.AuxillarySynthsData[AudioLayoutStorage.activeSynthIdx];

        var newResonanceFactor = Random.Range(0f, 0.5f);

        filterResonance = newResonanceFactor;
        filterCutoff = newsynth.filter.Cutoff;
        newsynth.filter.Resonance = filterResonance;
        filterResonanceKnob.rotation = Quaternion.Euler(0, 0, ((1 - newsynth.filter.Resonance) - 0.5f) * 2f * 145f);
        filterToShader.ModifyFilter(filterCutoff, filterResonance, filterEnvelope);
        float3 color = GetColorFromFilter(filterCutoff, filterResonance, newsynth.filterType);
        FilterColorImage.color = new Color { r = color.x, g = color.y, b = color.z, a = 1.0f };

        AudioLayoutStorageHolder.audioLayoutStorage.WriteModifySynth(newsynth);
        filterResonanceKnob.parent.gameObject.SetActive(true);
    }
    public void ActivateFilterEnveloppe()
    {
        SynthData newsynth = AudioLayoutStorageHolder.audioLayoutStorage.AuxillarySynthsData[AudioLayoutStorage.activeSynthIdx];

        var newEnveloppeFactor = Random.Range(0f, 1f);

        filterEnvelope = newEnveloppeFactor;
        newsynth.filterEnvelopeAmount = filterEnvelope;
        filterEnvelopeAmountKnob.rotation = Quaternion.Euler(0, 0, ((1 - newsynth.filterEnvelopeAmount) - 0.5f) * 2f * 145f);
        filterToShader.ModifyFilter(filterCutoff, filterResonance, filterEnvelope);

        AudioLayoutStorageHolder.audioLayoutStorage.WriteModifySynth(newsynth);
        filterEnvelopeAmountKnob.parent.gameObject.SetActive(true);
    }
    public void ChangeFilterTypeUI(TMP_Dropdown dropdown)
    {
        SynthData newsynth = AudioLayoutStorageHolder.audioLayoutStorage.AuxillarySynthsData[AudioLayoutStorage.activeSynthIdx];
        switch (dropdown.value)
        {
            case 0:
                filterCutoff = 1;
                break;
            case 1:
                filterCutoff = 0;
                break;
            case 2:
                filterCutoff = 0.5f;
                break;
        }
        filterResonance = 0;
        filterEnvelope = 0;
        newsynth.filter.Cutoff = filterCutoff;
        newsynth.filter.Resonance = filterResonance;
        newsynth.filterEnvelopeAmount = 0;
        newsynth.filterType = (short)dropdown.value;
        filterCutoffKnob.rotation = Quaternion.Euler(0, 0, ((1 - newsynth.filter.Cutoff) - 0.5f) * 2f * 145f);
        filterResonanceKnob.rotation = Quaternion.Euler(0, 0, ((1 - newsynth.filter.Resonance) - 0.5f) * 2f * 145f);
        filterEnvelopeAmountKnob.rotation = Quaternion.Euler(0, 0, ((1 - newsynth.filterEnvelopeAmount) - 0.5f) * 2f * 145f);
        filterCutoffKnob.GetComponent<KnobMono>().displayedValue = string.Format("{0:0.0}", (1 - newsynth.filter.Cutoff));
        filterResonanceKnob.GetComponent<KnobMono>().displayedValue = string.Format("{0:0.0}", (1 - newsynth.filter.Resonance));
        filterCutoffKnob.GetComponent<KnobMono>().displayedValue = string.Format("{0:0.0}", (1 - newsynth.filterEnvelopeAmount));
        filterToShader.ModifyFilter(filterCutoff, filterResonance, filterEnvelope);
        float3 color = GetColorFromFilter(filterCutoff, filterResonance, newsynth.filterType);
        FilterColorImage.color = new Color { r = color.x, g = color.y, b = color.z, a = 1.0f };

        filterToShader.SwitchFilterShader((short)dropdown.value);
        AudioLayoutStorageHolder.audioLayoutStorage.WriteModifySynth(newsynth);

    }

    public String UIknobChange(KnobChangeType knobChangeType, float newRot)
    {
        SynthData newsynth = AudioLayoutStorageHolder.audioLayoutStorage.AuxillarySynthsData[AudioLayoutStorage.activeSynthIdx];
        float factor = (newRot + 145) / 290;
        float displayedValue=0;

        switch (knobChangeType)
        {
            case KnobChangeType.FilterCutoff:
                filterCutoff = 1 - factor;
                //Debug.Log(filterCuoff);
                newsynth.filter.Cutoff = filterCutoff;
                displayedValue = filterCutoff;
                break;
            case KnobChangeType.FilterRes:
                filterResonance = 1-factor;
                //Debug.Log(filterResonance);
                newsynth.filter.Resonance = filterResonance;
                displayedValue = filterResonance;
                break;
            case KnobChangeType.FilterEnv:
                filterEnvelope = 1 - factor;
                newsynth.filterEnvelopeAmount = 1-factor;
                displayedValue = filterEnvelope;
                break;
        }

        filterToShader.ModifyFilter(filterCutoff, filterResonance, filterEnvelope);
        float3 color = GetColorFromFilter(filterCutoff, filterResonance, newsynth.filterType);
        FilterColorImage.color = new Color { r = color.x, g = color.y, b = color.z, a=1.0f };

        //Debug.LogError(AudioLayoutStorageHolder.audioLayoutStorage.SynthsData[0].Osc1SinSawSquareFactor);
        AudioLayoutStorageHolder.audioLayoutStorage.WriteModifySynth(newsynth);

        return string.Format("{0:0.00}", displayedValue);
    }

    public static float3 GetColorFromFilter(float cutoff, float resonance, short filtertype)
    {
        float3 blue = new float3(0.0f, 0.0f, 1.0f);
        float3 cyan = new float3(0.0f, 1.0f, 1.0f);
        float3 green = new float3(0.0f, 1.0f, 0.0f);
        float3 yellow = new float3(1.0f, 1.0f, 0.0f);
        float3 orange = new float3(1.0f, 0.5f, 0.0f);
        float3 red = new float3(1.0f, 0.0f, 0.0f);
        float3 white = new float3(1.0f, 1.0f, 1.0f);
        float3 color = new float3(0.0f, 0.0f, 0.0f);
        //cutoff *= 600;
        //cutoff /= 6;


  
        // Define thresholds for transitions
        float t1 = (1f/5f)*1;
        float t2 = (1f / 5f) * 2;
        float t3 = (1f / 5f) * 3;
        float t4 = (1f / 5f) * 4;
        //float t5 = (1f / 6f) * 5;

        switch (filtertype)
        {
            case 0:
            {
                // Compute blend factors
                float blend1 = math.smoothstep(0.0f, t1, cutoff);
                float blend2 = math.smoothstep(t1, t2, cutoff);
                float blend3 = math.smoothstep(t2, t3, cutoff);
                float blend4 = math.smoothstep(t3, t4, cutoff);
                float blend5 = math.smoothstep(t4, (4.5f / 5), cutoff);
                float blend6 = math.smoothstep((4.5f / 5), 1.0f, cutoff);

                // Linearly interpolate between colors based on the blend factors
                color = Vector3.Lerp(blue, cyan, blend1);
                color = Vector3.Lerp(color, green, blend2);
                color = Vector3.Lerp(color, yellow, blend3);
                color = Vector3.Lerp(color, orange, blend4);
                color = Vector3.Lerp(color, red, blend5);
                color = Vector3.Lerp(color, white, blend6);
                break;
            }
            case 1:
            {
                // Compute blend factors
                float blend0 = math.smoothstep(0.0f, (0.5f / 5), cutoff);
                float blend1 = math.smoothstep((0.5f / 5), t1, cutoff);
                float blend2 = math.smoothstep(t1, t2, cutoff);
                float blend3 = math.smoothstep(t2, t3, cutoff);
                float blend4 = math.smoothstep(t3, t4, cutoff);
                float blend5 = math.smoothstep(t4, 1.0f, cutoff);

                // Linearly interpolate between colors based on the blend factors
                color = Vector3.Lerp(white, blue, blend0);
                color = Vector3.Lerp(color, cyan, blend1);

                color = Vector3.Lerp(color, green, blend2);
                color = Vector3.Lerp(color, yellow, blend3);
                color = Vector3.Lerp(color, orange, blend4);
                color = Vector3.Lerp(color, red, blend5);
                break;
            }
            case 2:
            {
                // Compute blend factors
                float blend1 = math.smoothstep(0.0f, t1, cutoff);
                float blend2 = math.smoothstep(t1, t2, cutoff);
                float blend3 = math.smoothstep(t2, t3, cutoff);
                float blend4 = math.smoothstep(t3, t4, cutoff);
                float blend5 = math.smoothstep(t4, 1.0f, cutoff);

                // Linearly interpolate between colors based on the blend factors
                color = Vector3.Lerp(blue, cyan, blend1);
                color = Vector3.Lerp(color, green, blend2);
                color = Vector3.Lerp(color, yellow, blend3);
                color = Vector3.Lerp(color, orange, blend4);
                color = Vector3.Lerp(color, red, blend5);
                break;
            }
        }


        // // Intensity scaling to simulate the human eye sensitivity
        // float intensity = 1.0;
        // intensity *= (wavelength > 700.0) ? 0.3 + 0.7 * (780.0 - wavelength) / (780.0 - 700.0) : 1.0;
        // intensity *= (wavelength < 420.0) ? 0.3 + 0.7 * (wavelength - 380.0) / (420.0 - 380.0) : 1.0;

        // // Apply intensity scaling
        // color *= intensity;


        return color;
    }

}
