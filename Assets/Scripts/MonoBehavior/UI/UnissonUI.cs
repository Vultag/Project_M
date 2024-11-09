using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

public class UnissonUI : MonoBehaviour, IKnobController
{

    public UIManager uiManager;
    [SerializeField]
    private Transform UnissonVoicesKnob;
    [SerializeField]
    private Transform DetuneKnob;
    [SerializeField]
    private Transform SpreadKnob;

    short unissonVoices;
    float Detune;
    float Spread;

    private EntityManager entityManager;

    UIManager IKnobController.uiManager => uiManager;

    void Start()
    {

        entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;
        unissonVoices = 1;
        Detune = 0.26f;
        Spread = 0;
    }

    public void UpdateUI(SynthData synthData)
    {
        unissonVoices = synthData.UnissonVoices;
        Detune = synthData.UnissonDetune;
        Spread = synthData.UnissonSpread;
        //filterToShader.ModifyFilter(synthData.filter.Cutoff, synthData.filter.Resonance, synthData.filterEnvelopeAmount);
        UnissonVoicesKnob.rotation = Quaternion.Euler(0, 0, (-synthData.UnissonVoices+1)* (290f*0.25f) + 145f);
        DetuneKnob.rotation = Quaternion.Euler(0, 0, ((1 - (synthData.UnissonDetune - 2) / 48f) - 0.5f) * 2f * 145f);
        SpreadKnob.rotation = Quaternion.Euler(0, 0, ((1 - synthData.UnissonSpread) - 0.5f) * 2f * 145f);
        UnissonVoicesKnob.GetComponent<KnobMono>().displayedValue = string.Format("{0:0}", synthData.UnissonVoices);
        DetuneKnob.GetComponent<KnobMono>().displayedValue = string.Format("{0}{1}", Mathf.RoundToInt(synthData.UnissonDetune).ToString(), " semi");
        SpreadKnob.GetComponent<KnobMono>().displayedValue = string.Format("{0:0.00}", synthData.UnissonSpread);
    }


    public string UIknobChange(KnobChangeType knobChangeType, float newRot)
    {
        SynthData newsynth = AudioLayoutStorageHolder.audioLayoutStorage.SynthsData[AudioLayoutStorage.activeSynthIdx];
        float factor = (newRot + 145) / 290;
        string displayedValue = "";

        switch (knobChangeType)
        {
            case KnobChangeType.UnissonVoices:
                short state = (short)Mathf.Round(4-(factor*3f));
                unissonVoices = state;
                newsynth.UnissonVoices = unissonVoices;
                displayedValue = string.Format("{0:0}", newsynth.UnissonVoices);
                break;
            case KnobChangeType.UnissonDetune:
                Detune = Mathf.Lerp(0.02f, 0.5f, 1 - factor) * 100f;
                newsynth.UnissonDetune = Detune;
                displayedValue = string.Format("{0}{1}",  Mathf.RoundToInt(newsynth.UnissonDetune).ToString(), " semi");
                break;
            case KnobChangeType.UnissonSpread:
                Spread = (1 - factor);
                newsynth.UnissonSpread = Spread;
                displayedValue = string.Format("{0:0.00}", newsynth.UnissonSpread);
                break;
        }

        //filterToShader.ModifyFilter(filterCutoff, filterResonance, filterEnvelope);
        //float3 color = GetColorFromFilter(filterCutoff, filterResonance, newsynth.filterType);
        //FilterColorImage.color = new Color { r = color.x, g = color.y, b = color.z, a = 1.0f };

        ////Debug.LogError(AudioLayoutStorageHolder.audioLayoutStorage.SynthsData[0].Osc1SinSawSquareFactor);
        AudioLayoutStorageHolder.audioLayoutStorage.WriteModifySynth(newsynth);

        return displayedValue;
    }

  
    



}
