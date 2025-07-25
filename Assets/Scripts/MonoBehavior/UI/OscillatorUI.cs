using System;
using System.Numerics;
using TMPro;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.UI;
using Quaternion = UnityEngine.Quaternion;
using Random = UnityEngine.Random;
using Vector3 = UnityEngine.Vector3;


public class OscillatorUI : MonoBehaviour, IKnobController
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
    [SerializeField]
    private Transform OCS1pwKnob;
    [SerializeField]
    private Transform OCS2pwKnob;

    public UIManager uiManager;

    private EntityManager entityManager;

    UIManager IKnobController.uiManager => uiManager;

    void Start()
    {
        //uiManager = FindFirstObjectByType<UIManager>().GetComponent<UIManager>();
        OCS1wavetable.value = 0;
        OCS2wavetable.value = 0;
        entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;
    }

    public void UpdateUI(SynthData synthData)
    {
        float OCS1mixValue = synthData.Osc1SinSawSquareFactor.x + synthData.Osc1SinSawSquareFactor.y + synthData.Osc1SinSawSquareFactor.z;
        OCS1wavetable.SetValueWithoutNotify(Mathf.CeilToInt(synthData.Osc1SinSawSquareFactor.y) + Mathf.CeilToInt(synthData.Osc1SinSawSquareFactor.z)*2);
        OCS2wavetable.SetValueWithoutNotify(Mathf.CeilToInt(synthData.Osc2SinSawSquareFactor.y) + Mathf.CeilToInt(synthData.Osc2SinSawSquareFactor.z) * 2);
        MixKnob.rotation = Quaternion.Euler(0, 0, (OCS1mixValue-0.5f)*2f*145f);
        OCS1fineKnob.rotation = Quaternion.Euler(0, 0, (synthData.Osc1Fine/30)* 145f);
        OCS2fineKnob.rotation = Quaternion.Euler(0, 0, (synthData.Osc2Fine / 30) * 145f);
        OCSsemiKnob.rotation = Quaternion.Euler(0, 0, 145f - (synthData.Osc2Semi / 36) * 290f);

        /// Osc using square waveform
        if(synthData.Osc1SinSawSquareFactor.z>0)
        {
            OCS1pwKnob.transform.parent.gameObject.SetActive(true);
        }
        else
        {
            OCS1pwKnob.transform.parent.gameObject.SetActive(false);
        }
        if (synthData.Osc2SinSawSquareFactor.z > 0)
        {
            OCS2pwKnob.transform.parent.gameObject.SetActive(true);
        }
        else
        {
            OCS2pwKnob.transform.parent.gameObject.SetActive(false);
        }


        OCS1pwKnob.rotation = synthData.Osc1PW>=0.25f?Quaternion.Euler(0, 0, (synthData.Osc1PW - 0.25f) * 4f * -145f): Quaternion.Euler(0, 0, (synthData.Osc1PW - 0.12f) * (1/0.13f) * 145f);
        OCS2pwKnob.rotation = synthData.Osc2PW >= 0.25f ? Quaternion.Euler(0, 0, (synthData.Osc2PW - 0.25f) * 4f * -145f) : Quaternion.Euler(0, 0, (synthData.Osc2PW - 0.12f) * (1 / 0.13f) * 145f);

        MixKnob.GetComponent<KnobMono>().displayedValue = string.Format("{0}% - {1}%", Mathf.RoundToInt(OCS1mixValue * 100), 100 - Mathf.RoundToInt(OCS1mixValue * 100));
        OCS1fineKnob.GetComponent<KnobMono>().displayedValue = string.Format("{0}{1}", synthData.Osc1Fine, " cents");
        OCS2fineKnob.GetComponent<KnobMono>().displayedValue = string.Format("{0}{1}", synthData.Osc2Fine, " cents");
        OCSsemiKnob.GetComponent<KnobMono>().displayedValue = string.Format("{0}{1}",   synthData.Osc2Semi," semi");
        OCS1pwKnob.GetComponent<KnobMono>().displayedValue = string.Format("{0:0.00}", synthData.Osc1PW);
        OCS2pwKnob.GetComponent<KnobMono>().displayedValue = string.Format("{0:0.00}", synthData.Osc2PW);
    }

    /// UpdateUIfeatures()

    /// <summary>
    /// Currenty only deal with sin, saw and square tables
    /// </summary>
    public void UIocs1WavetableChange(TMP_Dropdown dropdown)
    {
        SynthData newsynth = AudioLayoutStorageHolder.audioLayoutStorage.AuxillarySynthsData[AudioLayoutStorage.activeSynthIdx];
        float dropdownItemValue = newsynth.Osc1SinSawSquareFactor.x + newsynth.Osc1SinSawSquareFactor.y + newsynth.Osc1SinSawSquareFactor.z;
        switch (dropdown.value)
        {
            case 0:
                newsynth.Osc1SinSawSquareFactor = new float3(dropdownItemValue,0,0);

                OCS1pwKnob.transform.parent.gameObject.SetActive(false);

                OCS1wavetable.value = 0;
                break;
            case 1:
                newsynth.Osc1SinSawSquareFactor = new float3(0, dropdownItemValue, 0);

                OCS1pwKnob.transform.parent.gameObject.SetActive(false);

                OCS1wavetable.value = 1;
                break;
            case 2:
                newsynth.Osc1SinSawSquareFactor = new float3(0, 0, dropdownItemValue);

                OCS1pwKnob.transform.parent.gameObject.SetActive(true);

                OCS1wavetable.value = 2;
                break;

        }
        AudioLayoutStorageHolder.audioLayoutStorage.WriteModifySynth(newsynth);
    }
    public void UIocs2WavetableChange(TMP_Dropdown dropdown)
    {
        SynthData newsynth = AudioLayoutStorageHolder.audioLayoutStorage.AuxillarySynthsData[AudioLayoutStorage.activeSynthIdx];
        float dropdownItemValue = newsynth.Osc2SinSawSquareFactor.x + newsynth.Osc2SinSawSquareFactor.y + newsynth.Osc2SinSawSquareFactor.z;

        switch (dropdown.value)
        {
            case 0:
                newsynth.Osc2SinSawSquareFactor = new float3(dropdownItemValue, 0, 0);

                OCS2pwKnob.transform.parent.gameObject.SetActive(false);

                OCS2wavetable.value = 0;
                break;
            case 1:
                newsynth.Osc2SinSawSquareFactor = new float3(0, dropdownItemValue, 0);

                OCS2pwKnob.transform.parent.gameObject.SetActive(false);

                OCS2wavetable.value = 1;
                break;
            case 2:
                newsynth.Osc2SinSawSquareFactor = new float3(0, 0, dropdownItemValue);
              
                OCS2pwKnob.transform.parent.gameObject.SetActive(true);

                OCS2wavetable.value = 2;
                break;

        }
        AudioLayoutStorageHolder.audioLayoutStorage.WriteModifySynth(newsynth);
    }

    public String UIknobChange(KnobChangeType knobChangeType, float newRot)
    {
        SynthData newsynth = AudioLayoutStorageHolder.audioLayoutStorage.AuxillarySynthsData[AudioLayoutStorage.activeSynthIdx];
        float factor = (newRot + 145) / 290;
        float increment = 0;
        String displayedValue = "";

        switch (knobChangeType)
        {
            case KnobChangeType.OCSmix:
                //Debug.Log((-Mathf.Sign(-newsynth.Osc1SinSawSquareFactor.x) + 1) * 0.5f);
                newsynth.Osc1SinSawSquareFactor = SynthData.OCSvalueMap[OCS1wavetable.value] * factor;
                newsynth.Osc2SinSawSquareFactor = SynthData.OCSvalueMap[OCS2wavetable.value] * (1 - factor);
                displayedValue = string.Format("{0}% - {1}%",Mathf.RoundToInt(((Vector3)newsynth.Osc1SinSawSquareFactor * 100).magnitude),Mathf.RoundToInt(((Vector3)newsynth.Osc2SinSawSquareFactor).magnitude * 100));
                break;
            case KnobChangeType.OCS1fine:
                increment = Mathf.Round((factor - 0.5f) * 2f * 30);
                newsynth.Osc1Fine = increment;
                newsynth.Osc2Fine = -increment;
                OCS2fineKnob.rotation = Quaternion.Euler(0, 0, -newRot);
                displayedValue = string.Format("{0}{1}", increment.ToString(), " cents");
                break;
            case KnobChangeType.OCS2fine:
                increment = Mathf.Round((factor - 0.5f) * 2f * 30);
                newsynth.Osc1Fine = -increment;
                newsynth.Osc2Fine = increment;
                OCS1fineKnob.rotation = Quaternion.Euler(0, 0, -newRot);
                displayedValue = string.Format("{0}{1}", increment.ToString(), " cents");
                break;
            case KnobChangeType.OCS2semi:
                increment = Mathf.Round(MathF.Abs((factor* 36)-36f));
                newsynth.Osc2Semi = increment;
                displayedValue = string.Format("{0}{1}", increment.ToString(), " semi");
                break;
            case KnobChangeType.OCS1PW:
                newsynth.Osc1PW = factor<0.5f? (1-factor) * 0.5f:Mathf.Lerp(0.12f,0.25f, (1 - factor) * 2);
                displayedValue = string.Format("{0:0.00}", newsynth.Osc1PW);
                break;
            case KnobChangeType.OCS2PW:
                newsynth.Osc2PW = factor < 0.5f ? (1 - factor) * 0.5f : Mathf.Lerp(0.12f, 0.25f, (1 - factor) * 2);
                displayedValue = string.Format("{0:0.00}", newsynth.Osc2PW);
                break;
        }

        //Debug.LogError(AudioLayoutStorageHolder.audioLayoutStorage.SynthsData[0].Osc1SinSawSquareFactor);
        AudioLayoutStorageHolder.audioLayoutStorage.WriteModifySynth(newsynth);

        return displayedValue;
    }

    public void ReviewActivatedFeatures(bool[] activatedFeatures)
    {
        OCS2wavetable.gameObject.SetActive(activatedFeatures[0]);
        MixKnob.parent.gameObject.SetActive(activatedFeatures[0]);
        OCSsemiKnob.parent.gameObject.SetActive(activatedFeatures[1]);
        OCS1fineKnob.parent.gameObject.SetActive(activatedFeatures[2]);
        OCS2fineKnob.parent.gameObject.SetActive(activatedFeatures[2]);
    }

    public void ActivateSecondOSC()
    {
        SynthData newsynth = AudioLayoutStorageHolder.audioLayoutStorage.AuxillarySynthsData[AudioLayoutStorage.activeSynthIdx];
        float dropdownItemValue = newsynth.Osc2SinSawSquareFactor.x + newsynth.Osc2SinSawSquareFactor.y + newsynth.Osc2SinSawSquareFactor.z;

        var newWavetrable = Random.Range(0, 3);
        var newMixFactor = Random.Range(0.25f, 0.75f);

        switch (newWavetrable)
        {
            case 0:
                newsynth.Osc2SinSawSquareFactor = new float3(dropdownItemValue, 0, 0);

                OCS2pwKnob.transform.parent.gameObject.SetActive(false);

                OCS2wavetable.value = 0;
                break;
            case 1:
                newsynth.Osc2SinSawSquareFactor = new float3(0, dropdownItemValue, 0);

                OCS2pwKnob.transform.parent.gameObject.SetActive(false);

                OCS2wavetable.value = 1;
                break;
            case 2:
                newsynth.Osc2SinSawSquareFactor = new float3(0, 0, dropdownItemValue);

                OCS2pwKnob.transform.parent.gameObject.SetActive(true);

                OCS2wavetable.value = 2;
                break;

        }
        newsynth.Osc1SinSawSquareFactor = SynthData.OCSvalueMap[OCS1wavetable.value] * newMixFactor;
        newsynth.Osc2SinSawSquareFactor = SynthData.OCSvalueMap[OCS2wavetable.value] * (1 - newMixFactor);
        MixKnob.rotation = Quaternion.Euler(0, 0, (newMixFactor - 0.5f) * 2f * 145f);

        AudioLayoutStorageHolder.audioLayoutStorage.WriteModifySynth(newsynth);

        OCS2wavetable.gameObject.SetActive(true);
        MixKnob.parent.gameObject.SetActive(true);
    }
    public void ActivateDetune()
    {
        SynthData newsynth = AudioLayoutStorageHolder.audioLayoutStorage.AuxillarySynthsData[AudioLayoutStorage.activeSynthIdx];
        
        float increment = Mathf.Round((Random.Range(0.2f, 0.8f) - 0.5f) * 2f * 30);
        newsynth.Osc1Fine = increment;
        newsynth.Osc2Fine = -increment;
        OCS1fineKnob.rotation = Quaternion.Euler(0, 0, (increment / 30) * 145f);
        OCS2fineKnob.rotation = Quaternion.Euler(0, 0, (-increment / 30) * 145f);
           

        AudioLayoutStorageHolder.audioLayoutStorage.WriteModifySynth(newsynth);

        OCS1fineKnob.parent.gameObject.SetActive(true);
        OCS2fineKnob.parent.gameObject.SetActive(true);
    }
    public void ActivateSemiTones()
    {
        SynthData newsynth = AudioLayoutStorageHolder.audioLayoutStorage.AuxillarySynthsData[AudioLayoutStorage.activeSynthIdx];

        float increment = Mathf.Round(MathF.Abs((Random.Range(0.2f, 0.8f) * 36) - 36f));
        newsynth.Osc2Semi = increment;
        OCSsemiKnob.rotation = Quaternion.Euler(0, 0, 145f - (increment / 36) * 290f);


        AudioLayoutStorageHolder.audioLayoutStorage.WriteModifySynth(newsynth);

        OCSsemiKnob.parent.gameObject.SetActive(true);
    }

}
