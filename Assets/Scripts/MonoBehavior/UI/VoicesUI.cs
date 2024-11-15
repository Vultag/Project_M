using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using UnityEngine;
using UnityEngine.UI;

public class VoicesUI : MonoBehaviour, IKnobController
{
    public UIManager uiManager;
    [SerializeField]
    private Transform PortomentoKnob;
    [SerializeField]
    private Image LegatoSwitch;
    [SerializeField]
    private Sprite SwitchUp;
    [SerializeField]
    private Sprite SwitchDown;
    //Assets/Sprites/Display/switchDown.png

    float Portomento;

    private EntityManager entityManager;

    UIManager IKnobController.uiManager => uiManager;

    void Start()
    {
        entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;
    }


    public void UpdateUI(SynthData synthData)
    {
        Portomento = synthData.Portomento;
        PortomentoKnob.rotation = Quaternion.Euler(0, 0, ((1 - synthData.Portomento/3) - 0.5f) * 2f * 145f);
        PortomentoKnob.GetComponent<KnobMono>().displayedValue = string.Format("{0}{1}", (Mathf.RoundToInt(synthData.Portomento * 100)).ToString(), " MS");
        LegatoSwitch.sprite = synthData.Legato?SwitchUp:SwitchDown;
    }

    public void SwitchActivation()
    {

        SynthData newsynth = AudioLayoutStorageHolder.audioLayoutStorage.SynthsData[AudioLayoutStorage.activeSynthIdx];
        if (LegatoSwitch.sprite == SwitchDown)
        {
            newsynth.Legato = true;
            LegatoSwitch.sprite = SwitchUp;
        }
        else
        {
            newsynth.Legato = false;
            LegatoSwitch.sprite = SwitchDown;
        }
        AudioLayoutStorageHolder.audioLayoutStorage.WriteModifySynth(newsynth);

    }

    public string UIknobChange(KnobChangeType knobChangeType, float newRot)
    {
        SynthData newsynth = AudioLayoutStorageHolder.audioLayoutStorage.SynthsData[AudioLayoutStorage.activeSynthIdx];
        float factor = (newRot + 145) / 290;
        string displayedValue = "";

        switch (knobChangeType)
        {
            case KnobChangeType.Portamento:
                Portomento = (1 - factor)*3;
                newsynth.Portomento = Portomento;
                displayedValue = string.Format("{0}{1}", (Mathf.RoundToInt(newsynth.Portomento*100)).ToString(), " MS");
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
