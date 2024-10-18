using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.UIElements;

public enum ADSRChangeType
{
    VolumeADSR,
    FilterADSR,
}

public class ADSRUI : MonoBehaviour
{
    public UIManager uiManager;

    [SerializeField]
    private ADSRChangeType ADSRChangeType;

    [SerializeField]
    private Transform Aslider;
    [SerializeField]
    private Transform Dslider;
    [SerializeField]
    private Transform Sslider;
    [SerializeField]
    private Transform Rslider;

    const float ADSRmaxValue = 4f;
    
    void Start()
    {
         UpdateUI(AudioLayoutStorageHolder.audioLayoutStorage.SynthsData[0]);
    }

    public void UpdateUI(SynthData synthData)
    {
        if(ADSRChangeType == ADSRChangeType.VolumeADSR)
        {
            Aslider.localPosition = new Vector3(Aslider.localPosition.x, (synthData.ADSR.Attack * 0.25f - 0.5f) * 2f * 23, Aslider.localPosition.z);
            Dslider.localPosition = new Vector3(Dslider.localPosition.x, (synthData.ADSR.Decay * 0.25f - 0.5f) * 2f * 23, Dslider.localPosition.z);
            Sslider.localPosition = new Vector3(Sslider.localPosition.x, (synthData.ADSR.Sustain - 0.5f) * 2f * 23, Sslider.localPosition.z);
            Rslider.localPosition = new Vector3(Rslider.localPosition.x, (synthData.ADSR.Release * 0.25f - 0.5f) * 2f * 23, Rslider.localPosition.z);
            Aslider.GetComponent<VerticalSliderMono>().displayedValue = string.Format("{0:0.0}", synthData.ADSR.Attack);
            Dslider.GetComponent<VerticalSliderMono>().displayedValue = string.Format("{0:0.0}", synthData.ADSR.Decay);
            Sslider.GetComponent<VerticalSliderMono>().displayedValue = string.Format("{0:0.0}", synthData.ADSR.Sustain);
            Rslider.GetComponent<VerticalSliderMono>().displayedValue = string.Format("{0:0.0}", synthData.ADSR.Release);
        }
        else if(ADSRChangeType == ADSRChangeType.FilterADSR)
        {
            Aslider.localPosition = new Vector3(Aslider.localPosition.x, (synthData.filterADSR.Attack * 0.25f - 0.5f) * 2f * 23, Aslider.localPosition.z);
            Dslider.localPosition = new Vector3(Dslider.localPosition.x, (synthData.filterADSR.Decay * 0.25f - 0.5f) * 2f * 23, Dslider.localPosition.z);
            Sslider.localPosition = new Vector3(Sslider.localPosition.x, (synthData.filterADSR.Sustain - 0.5f) * 2f * 23, Sslider.localPosition.z);
            Rslider.localPosition = new Vector3(Rslider.localPosition.x, (synthData.filterADSR.Release * 0.25f - 0.5f) * 2f * 23, Rslider.localPosition.z);
            Aslider.GetComponent<VerticalSliderMono>().displayedValue = string.Format("{0:0.0}", synthData.filterADSR.Attack);
            Dslider.GetComponent<VerticalSliderMono>().displayedValue = string.Format("{0:0.0}", synthData.filterADSR.Decay);
            Sslider.GetComponent<VerticalSliderMono>().displayedValue = string.Format("{0:0.0}", synthData.filterADSR.Sustain);
            Rslider.GetComponent<VerticalSliderMono>().displayedValue = string.Format("{0:0.0}", synthData.filterADSR.Release);
        }


        //Debug.Log(AudioLayoutStorageHolder.audioLayoutStorage.SynthsData[0].ADSR.Sustain);
        //Debug.Log(Aslider.localPosition.y);
    }

    public string UIADSRchange(short ADSRindex, float value)
    {
        SynthData newsynth = AudioLayoutStorageHolder.audioLayoutStorage.SynthsData[AudioLayoutStorage.activeSynthIdx];
        value = (value + 1) * 0.5f * ADSRmaxValue;

        switch (ADSRChangeType)
        {
            case ADSRChangeType.VolumeADSR:
                switch (ADSRindex)
                {
                    case 0:
                        newsynth.ADSR = new ADSREnvelope { Attack = value, Decay = newsynth.ADSR.Decay, Sustain = newsynth.ADSR.Sustain, Release = newsynth.ADSR.Release };
                        
                        break;
                    case 1:
                        newsynth.ADSR = new ADSREnvelope { Attack = newsynth.ADSR.Attack, Decay = value, Sustain = newsynth.ADSR.Sustain, Release = newsynth.ADSR.Release };
                        break;
                    case 2:
                        /// important to make it !=0 in the current audio key releasing algorithm
                        value += 0.000001f;
                        newsynth.ADSR = new ADSREnvelope { Attack = newsynth.ADSR.Attack, Decay = newsynth.ADSR.Decay, Sustain = value /= ADSRmaxValue, Release = newsynth.ADSR.Release };
                        break;
                    case 3:
                        /// important to make it !=0 in the current audio key releasing algorithm
                        value += 0.000001f;
                        newsynth.ADSR = new ADSREnvelope { Attack = newsynth.ADSR.Attack, Decay = newsynth.ADSR.Decay, Sustain = newsynth.ADSR.Sustain, Release = value };
                        break;
                }
            break;
            case ADSRChangeType.FilterADSR:
                switch (ADSRindex)
                {
                    case 0:
                        newsynth.filterADSR = new ADSREnvelope { Attack = value, Decay = newsynth.filterADSR.Decay, Sustain = newsynth.filterADSR.Sustain, Release = newsynth.filterADSR.Release };
                        break;
                    case 1:
                        newsynth.filterADSR = new ADSREnvelope { Attack = newsynth.filterADSR.Attack, Decay = value, Sustain = newsynth.filterADSR.Sustain, Release = newsynth.filterADSR.Release };
                        break;
                    case 2:
                        /// important to make it !=0 in the current audio key releasing algorithm
                        value += 0.000001f;
                        newsynth.filterADSR = new ADSREnvelope { Attack = newsynth.filterADSR.Attack, Decay = newsynth.filterADSR.Decay, Sustain = value /= ADSRmaxValue, Release = newsynth.filterADSR.Release };
                        break;
                    case 3:
                        /// important to make it !=0 in the current audio key releasing algorithm
                        value += 0.000001f;
                        newsynth.filterADSR = new ADSREnvelope { Attack = newsynth.filterADSR.Attack, Decay = newsynth.filterADSR.Decay, Sustain = newsynth.filterADSR.Sustain, Release = value };
                        break;
                }
            break;
        }
  

        //Debug.Log(newsynth.ADSR.Sustain);
        AudioLayoutStorageHolder.audioLayoutStorage.WriteModifySynth(newsynth);

        return string.Format("{0:0.0}", value);
    }


}
