using System.Collections;
using System.Collections.Generic;
using TMPro;
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
    [SerializeField]
    private Transform AsliderFieldGB;
    [SerializeField]
    private Transform DsliderFieldGB;
    [SerializeField]
    private Transform SsliderFieldGB;
    [SerializeField]
    private Transform RsliderFieldGB;

    public List<Sprite> weaponSprites;
    public UnityEngine.UI.Image weaponImage;
    public GameObject weaponTextGB;

    const float ADSRmaxValue = 4f;
    [HideInInspector]
    public float2[] ThisADSRLimits = new float2[4]; 
    
    void Start()
    {
        //var test = new ADSRlimits
        //{
        //    AttackLimits = new float2(0f,0.25f),
        //    DecayLimits = new float2(0.25f, 0.5f),
        //    Sustainimits = new float2(0.5f, 0.75f),
        //    ReleaseLimits = new float2(0.75f, 1f),
        //};
        // UpdateUI(AudioLayoutStorageHolder.audioLayoutStorage.SynthsData[0], test);
    }

    public void UpdateUI(SynthData synthData, WeaponType weaponType)
    {
        ADSRlimits adsrLimits = ADSRlimits.NoLimits();
        switch (weaponType)
        {
            case WeaponType.Raygun:
                adsrLimits = ADSRlimits.RaygunLimits();
                weaponImage.sprite = weaponSprites[0];
                weaponTextGB.GetComponent<TextMeshProUGUI>().text = "Raygun";
                break;
            case WeaponType.Canon:
                adsrLimits = ADSRlimits.CanonLimits();
                weaponImage.sprite = weaponSprites[1];
                weaponTextGB.GetComponent<TextMeshProUGUI>().text = "Canon";
                break;
        }

        ThisADSRLimits[0] = adsrLimits.AttackLimits;
        ThisADSRLimits[1] = adsrLimits.DecayLimits;
        ThisADSRLimits[2] = adsrLimits.Sustainimits;
        ThisADSRLimits[3] = adsrLimits.ReleaseLimits;

        //Debug.Log(synthData.ADSR.Attack);
        float halfBackgroundLimits = 23;

        if (ADSRChangeType == ADSRChangeType.VolumeADSR)
        {
            float backgroundLimits = 25f * 2;
            //Debug.Log(synthData.ADSR.Attack);
            //Debug.Log(synthData.ADSR.Decay);
            //Debug.Log(synthData.ADSR.Sustain);
            //Debug.Log(synthData.ADSR.Release);

            Aslider.localPosition = new Vector3(Aslider.localPosition.x, (synthData.ADSR.Attack * 0.25f - 0.5f) *  backgroundLimits, Aslider.localPosition.z);
            Dslider.localPosition = new Vector3(Dslider.localPosition.x, (synthData.ADSR.Decay * 0.25f - 0.5f) * backgroundLimits, Dslider.localPosition.z);
            Sslider.localPosition = new Vector3(Sslider.localPosition.x, (synthData.ADSR.Sustain - 0.5f) *  backgroundLimits, Sslider.localPosition.z);
            Rslider.localPosition = new Vector3(Rslider.localPosition.x, (synthData.ADSR.Release * 0.25f - 0.5f) * backgroundLimits, Rslider.localPosition.z);
            Aslider.GetComponent<VerticalSliderMono>().displayedValue = string.Format("{0:0.0}", synthData.ADSR.Attack);
            Dslider.GetComponent<VerticalSliderMono>().displayedValue = string.Format("{0:0.0}", synthData.ADSR.Decay);
            Sslider.GetComponent<VerticalSliderMono>().displayedValue = string.Format("{0:0.0}", synthData.ADSR.Sustain);
            Rslider.GetComponent<VerticalSliderMono>().displayedValue = string.Format("{0:0.0}", synthData.ADSR.Release);

            float Fsize = (adsrLimits.AttackLimits.y - adsrLimits.AttackLimits.x) * backgroundLimits;
            AsliderFieldGB.GetComponent<RectTransform>().sizeDelta = new Vector2(3.2f, Fsize);
            AsliderFieldGB.localPosition = new Vector3(Aslider.localPosition.x,-25f+adsrLimits.AttackLimits.x* backgroundLimits + Fsize*0.5f, Aslider.localPosition.z);
            Fsize = (adsrLimits.DecayLimits.y - adsrLimits.DecayLimits.x) * backgroundLimits;
            DsliderFieldGB.GetComponent<RectTransform>().sizeDelta = new Vector2(3.2f, Fsize);
            DsliderFieldGB.localPosition = new Vector3(Dslider.localPosition.x, -25f + adsrLimits.DecayLimits.x * backgroundLimits + Fsize * 0.5f, Dslider.localPosition.z);
            Fsize = (adsrLimits.Sustainimits.y - adsrLimits.Sustainimits.x) * backgroundLimits;
            SsliderFieldGB.GetComponent<RectTransform>().sizeDelta = new Vector2(3.2f, Fsize);
            SsliderFieldGB.localPosition = new Vector3(Sslider.localPosition.x, -25f + adsrLimits.Sustainimits.x * backgroundLimits + Fsize * 0.5f, Sslider.localPosition.z);
            Fsize = (adsrLimits.ReleaseLimits.y - adsrLimits.ReleaseLimits.x) * backgroundLimits;
            RsliderFieldGB.GetComponent<RectTransform>().sizeDelta = new Vector2(3.2f, Fsize);
            RsliderFieldGB.localPosition = new Vector3(Rslider.localPosition.x, -25f + adsrLimits.ReleaseLimits.x * backgroundLimits + Fsize * 0.5f, Rslider.localPosition.z);

            //Canvas.ForceUpdateCanvases();
        }
        else if(ADSRChangeType == ADSRChangeType.FilterADSR)
        {
            Aslider.localPosition = new Vector3(Aslider.localPosition.x, (synthData.filterADSR.Attack * 0.25f - 0.5f) * 2f * halfBackgroundLimits, Aslider.localPosition.z);
            Dslider.localPosition = new Vector3(Dslider.localPosition.x, (synthData.filterADSR.Decay * 0.25f - 0.5f) * 2f * halfBackgroundLimits, Dslider.localPosition.z);
            Sslider.localPosition = new Vector3(Sslider.localPosition.x, (synthData.filterADSR.Sustain - 0.5f) * 2f * halfBackgroundLimits, Sslider.localPosition.z);
            Rslider.localPosition = new Vector3(Rslider.localPosition.x, (synthData.filterADSR.Release * 0.25f - 0.5f) * 2f * halfBackgroundLimits, Rslider.localPosition.z);
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
        SynthData newsynth = AudioLayoutStorageHolder.audioLayoutStorage.AuxillarySynthsData[AudioLayoutStorage.activeSynthIdx];
        value = value * ADSRmaxValue;

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
                        /// important to make it !=0
                        value += 0.000001f;
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
