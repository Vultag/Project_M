using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using Unity.Mathematics;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

public class FilterUI : MonoBehaviour,IKnobController
{

    public UIManager uiManager;
    [SerializeField]
    private FliterToShader filterToShader;
    [SerializeField]
    private Image FilterColorImage;

    private float filterCuoff;
    private float filterResonance;

    private EntityManager entityManager;

    UIManager IKnobController.uiManager => uiManager;

    void Start()
    {
        entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;
    }

    public String UIknobChange(KnobChangeType knobChangeType, float newRot)
    {
        SynthData newsynth = AudioLayoutStorageHolder.audioLayoutStorage.SynthsData[AudioLayoutStorage.activeSynthIdx];
        float factor = (newRot + 145) / 290;
        float increment = 0;
        String displayedValue = "";

        switch (knobChangeType)
        {
            case KnobChangeType.FilterCutoff:
                filterCuoff = 1-factor;
                //Debug.Log(filterCuoff);
                filterToShader.ModifyFilter(filterCuoff, filterResonance);
                newsynth.filter.Cutoff = filterCuoff;
                //increment = Mathf.Round((factor - 0.5f) * 2f * 30);
                //newsynth.Osc1Fine = increment;
                //newsynth.Osc2Fine = -increment;
                //displayedValue = string.Format("{0}{1}", increment.ToString(), " cents");
                break;
            case KnobChangeType.FilterRes:
                filterResonance = 1-factor;
                //Debug.Log(filterResonance);
                filterToShader.ModifyFilter(filterCuoff, filterResonance);
                newsynth.filter.Resonance = filterResonance;
                //increment = Mathf.Round((factor - 0.5f) * 2f * 30);
                //newsynth.Osc1Fine = increment;
                //newsynth.Osc2Fine = -increment;
                //OCS2fineKnob.rotation = Quaternion.Euler(0, 0, -newRot);
                //displayedValue = string.Format("{0}{1}", increment.ToString(), " cents");
                break;
            case KnobChangeType.FilterEnv:
                break;
        }

        float3 color = GetColorFromFilter(filterCuoff, filterResonance);
        FilterColorImage.color = new Color { r = color.x, g = color.y, b = color.z, a=1.0f };

        //Debug.LogError(AudioLayoutStorageHolder.audioLayoutStorage.SynthsData[0].Osc1SinSawSquareFactor);
        AudioLayoutStorageHolder.audioLayoutStorage.WriteModifySynth(newsynth);

        return displayedValue;
    }

    float3 GetColorFromFilter(float cutoff, float resonance)
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
        float t1 = (1f/6f)*1;
        float t2 = (1f / 6f) * 2;
        float t3 = (1f / 6f) * 3;
        float t4 = (1f / 6f) * 4;
        float t5 = (1f / 6f) * 5;

        // Compute blend factors
        float blend1 = math.smoothstep(0.0f, t1, cutoff);
        float blend2 = math.smoothstep(t1, t2, cutoff);
        float blend3 = math.smoothstep(t2, t3, cutoff);
        float blend4 = math.smoothstep(t3, t4, cutoff);
        float blend5 = math.smoothstep(t4, t5, cutoff);
        float blend6 = math.smoothstep(t5, 1.0f, cutoff);

        // Linearly interpolate between colors based on the blend factors
        color = Vector3.Lerp(blue, cyan, blend1);
        color = Vector3.Lerp(color, green, blend2);
        color = Vector3.Lerp(color, yellow, blend3);
        color = Vector3.Lerp(color, orange, blend4);
        color = Vector3.Lerp(color, red, blend5);
        color = Vector3.Lerp(color, white, blend6);

        // // Intensity scaling to simulate the human eye sensitivity
        // float intensity = 1.0;
        // intensity *= (wavelength > 700.0) ? 0.3 + 0.7 * (780.0 - wavelength) / (780.0 - 700.0) : 1.0;
        // intensity *= (wavelength < 420.0) ? 0.3 + 0.7 * (wavelength - 380.0) / (420.0 - 380.0) : 1.0;

        // // Apply intensity scaling
        // color *= intensity;


        return color;
    }

}
