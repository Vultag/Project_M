using System.Collections;
using System.Collections.Generic;
using MusicNamespace;
using TMPro;
using Unity.Entities;
using UnityEngine;
using UnityEngine.UI;

public enum EquipmentCategory
{
    Weapon,
    DrumMachine
}

public struct BuildingInfo
{
    public EquipmentCategory equipmentCategory;
    public WeaponType weaponType;
    public WeaponClass weaponClass;
    public short buildingIdx;
    public Entity WeaponAmmoPrefab;
    //...
}


public class EquipmentUIelement : MonoBehaviour
{

    private UIManager uiManager;
    public TextMeshProUGUI startCountdown;

    /// <summary>
    /// the number of beat countdown before the playback start recording
    /// </summary>
    private short BaseBeatBeforeSynthStart = 3;
    private float ContdownFontSize = 12;
    [HideInInspector]
    public ushort thisEquipmentIdx;
    [HideInInspector]
    public ushort thisRelativeEquipmentIdx;
    [HideInInspector]
    public EquipmentCategory thisEquipmentCategory;

    [HideInInspector]
    public Entity thisEquipmentE;
    [SerializeField]
    Slider energySlider;

    public GameObject upgradeButtonGB;

    ///public BuildingInfo thisBuildingInfo;
    private bool RecordPrepairing = false;
    public bool ActivationPrepairing = false;

    /// <summary>
    /// used to delay slider update by 1 frame
    /// </summary>
    bool delayEnergyActivation = true;

    /// hapens mid way through runtime 
    void Start()
    {
        uiManager = Object.FindAnyObjectByType<UIManager>();
        thisEquipmentIdx = (ushort)this.gameObject.transform.GetSiblingIndex();
    }


    private void LateUpdate()
    {
        /// Suboptimal but need to delay by 1 frame bc entityManager's thisEquipmentE not ready yet
        if (delayEnergyActivation)
        {
            delayEnergyActivation = false;
            return;
        }
        var entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;
        EquipmentEnergyData energyData = entityManager.GetComponentData<EquipmentEnergyData>(thisEquipmentE);
        energySlider.value = energyData.energyLevel / energyData.maxEnergy;

    }

    public void _upgradeEquipment()
    {
        if (thisEquipmentCategory == EquipmentCategory.DrumMachine)
            return;
        switch (thisEquipmentCategory)
        {
            case EquipmentCategory.Weapon:

                List<SynthUpgrade> synthEquipmentUpgradeOptions  = this.transform.parent.GetComponent<EquipmentUpgradeManager>().synthEquipmentsUpgradeOptions[thisRelativeEquipmentIdx];
                /// nothing to further upgrade
                if(synthEquipmentUpgradeOptions.Count<1)
                {
                    return;
                }
                bool[] synthActivatedFeatues = this.transform.parent.GetComponent<EquipmentUpgradeManager>().synthsActivatedFeatures[thisRelativeEquipmentIdx];

                var upgrade = PopRandomUnordered(synthEquipmentUpgradeOptions, (short)Random.Range(0, synthEquipmentUpgradeOptions.Count));
                switch (upgrade)
                {
                    case SynthUpgrade.SecondOscillator:
                        uiManager.oscillatorUI.ActivateSecondOSC();
                        synthActivatedFeatues[0] = true;
                        synthEquipmentUpgradeOptions.Add(SynthUpgrade.SecondOscillatorFineTune);
                        synthEquipmentUpgradeOptions.Add(SynthUpgrade.SecondOscillatorSemiTune);
                        break;

                    case SynthUpgrade.SecondOscillatorSemiTune:
                        synthActivatedFeatues[1] = true;
                        uiManager.oscillatorUI.ActivateSemiTones();
                        break;

                    case SynthUpgrade.SecondOscillatorFineTune:
                        synthActivatedFeatues[2] = true;
                        uiManager.oscillatorUI.ActivateDetune();
                        break;

                    case SynthUpgrade.Filter:
                        synthActivatedFeatues[3] = true;
                        uiManager.filterUI.gameObject.SetActive(true);
                        uiManager.filterUI.InitiateFilter();
                        synthEquipmentUpgradeOptions.Add(SynthUpgrade.FilterResonance);
                        synthEquipmentUpgradeOptions.Add(SynthUpgrade.FilterEnveloppe);
                        break;

                    case SynthUpgrade.FilterResonance:
                        synthActivatedFeatues[4] = true;
                        uiManager.filterUI.ActivateFilterResonance();
                        break;

                    case SynthUpgrade.FilterEnveloppe:
                        synthActivatedFeatues[5] = true;
                        uiManager.filterAdsrUI.gameObject.SetActive(true);
                        uiManager.filterAdsrUI.UIADSRnewRandom();
                        uiManager.filterUI.ActivateFilterEnveloppe();
                        break;

                    case SynthUpgrade.Unisson:
                        synthActivatedFeatues[6] = true;
                        uiManager.unissonUI.gameObject.SetActive(true);
                        uiManager.unissonUI.InitiateUnison();
                        synthEquipmentUpgradeOptions.Add(SynthUpgrade.UnissonSpread);
                        break;

                    case SynthUpgrade.UnissonSpread:
                        synthActivatedFeatues[7] = true;
                        uiManager.unissonUI.ActivateUnisonSpread();
                        break;

                    case SynthUpgrade.Voices:
                        synthActivatedFeatues[8] = true;
                        uiManager.voicesUI.InitializeVoices();
                        uiManager.voicesUI.gameObject.SetActive(true);
                        break;
                }

                break;
            case EquipmentCategory.DrumMachine:
                Debug.Log("to do");
                break;
        }
        var remainingUpgradeNum = --this.transform.parent.GetComponent<EquipmentUpgradeManager>().numOfAvailableUpgrades;
        if (remainingUpgradeNum < 1)
            upgradeButtonGB.SetActive(false);
    }
    public SynthUpgrade PopRandomUnordered(List<SynthUpgrade> upgradePool, short idx)
    {
        SynthUpgrade value = upgradePool[idx];
        int last = upgradePool.Count - 1;
        upgradePool[idx] = upgradePool[last]; // move last element into the hole
        upgradePool.RemoveAt(last);      // O(1) remove at end
        return value;
    }
    public void _selectThisEquipment()
    {
        ///uiManager._SelectBuildingUI(thisEquipmentIdx, thisBuildingInfo);
        
        switch (thisEquipmentCategory)
        {
            case EquipmentCategory.Weapon:
                uiManager._SelectSynthUI(thisEquipmentIdx);
                break;
            case EquipmentCategory.DrumMachine:
                uiManager._SelectMachineDrumUI(thisEquipmentIdx);
                break;
        }
    }
    //public void _activateThisPlayback()
    //{
    //    uiManager._ActivateSynthPlayback(thisEquipmentIdx);
    //}

    public void _PrepairRecord()
    {
        if (uiManager.curentlyRecording)
            return;
        uiManager.curentlyRecording = true;
        uiManager._ResetPlayback(thisEquipmentIdx);
        /// Deactivate Rec button GB
        uiManager.equipmentToolBar.transform.GetChild(thisEquipmentIdx).GetChild(2).GetChild(0).gameObject.SetActive(false);
        ///// Deactivate Play button GB
        //uiManager.equipmentToolBar.transform.GetChild(thisSynthIdx).GetChild(2).GetChild(1).gameObject.SetActive(false);
        ///// Activate Stop button GB
        //uiManager.equipmentToolBar.transform.GetChild(thisSynthIdx).GetChild(2).GetChild(2).gameObject.SetActive(true);
        StartCoroutine(nameof(RecordCountdown), thisEquipmentIdx);
        RecordPrepairing = true;
    }
    public void _DisplayPrepairActivation()
    {
        StartCoroutine("ActivationCountdown");
    }
    public void _CancelActivation()
    {
        startCountdown.gameObject.SetActive(false);
        StopCoroutine("ActivationCountdown");
    }
    public void _CancelRecord()
    {
        startCountdown.gameObject.SetActive(false);
        StopCoroutine("RecordCountdown");
    }

    //public void _StopRecordOrPlayback() 
    //{
    //    if(RecordPrepairing)
    //    {
    //        StopCoroutine("RecordCountdown");
    //        RecordPrepairing = false;
    //        ///// Deactivate Rec button GB
    //        //uiManager.equipmentToolBar.transform.GetChild(thisSynthIdx).GetChild(2).GetChild(0).gameObject.SetActive(true);
    //        ///// Deactivate Play button GB
    //        //uiManager.equipmentToolBar.transform.GetChild(thisSynthIdx).GetChild(2).GetChild(1).gameObject.SetActive(true);
    //        ///// Activate Stop button GB
    //        //uiManager.equipmentToolBar.transform.GetChild(thisSynthIdx).GetChild(2).GetChild(2).gameObject.SetActive(false);
    //        startCountdown.gameObject.SetActive(false);
    //    }
    //    else
    //    {
    //        uiManager._StopSynthPlayback(thisSynthIdx);
    //    }
    //}

    /// <summary>
    /// cancel auto play for now
    /// </summary>
    public void _StopAutoPlay()
    {
        if (!RecordPrepairing)
            startCountdown.gameObject.SetActive(false);
        uiManager._StopAutoPlay(thisEquipmentIdx);
    }
    public void _StartAutoPlay()
    {
        if(!ActivationPrepairing)
        {
            uiManager._StartAutoPlay(thisEquipmentIdx);
            StartCoroutine(nameof(ActivationCountdown));
        }
        else
        {
            Debug.LogError("already ActivationPrepairing");
        }
    }

    IEnumerator RecordCountdown(ushort equipmentIdx)
    {
        startCountdown.gameObject.SetActive(true);
        startCountdown.color = Color.red;

        //int startingBeat = (int)(Mathf.Ceil((float)(MusicUtils.time))+ BaseBeatBeforeSynthStart);
        //float remainingTime = (1 - (float)(MusicUtils.time) % (60f / MusicUtils.BPM)) + BaseBeatBeforeSynthStart;

        int startingBeat = (int)(Mathf.Floor((float)(MusicUtils.time/4f))*4+ 4);
        float mesureProgress = (float)(MusicUtils.time) % ((60f / MusicUtils.BPM) * 4);
        //int BeatBeforeStart = BaseBeatBeforeSynthStart - (Mathf.FloorToInt(mesureProgress));
        float remainingTime = (4 - mesureProgress);// + BeatBeforeStart;

        while ((remainingTime - Time.deltaTime) > 0)
        {
            remainingTime -= Time.deltaTime;

            if (remainingTime > BaseBeatBeforeSynthStart)
            {
                startCountdown.text = "...";
            }
            else
            {
                float remainder = remainingTime % 1f;
                startCountdown.fontSize = ContdownFontSize-(1-remainder)*4;
                startCountdown.color = new Color(1,1- remainder, 1- remainder);
                startCountdown.text = Mathf.CeilToInt(remainingTime).ToString();
            }

            yield return new WaitForEndOfFrame();
        }
        //remainingTime -= Time.deltaTime;

        switch (thisEquipmentCategory)
        {
            case EquipmentCategory.Weapon:
                uiManager._RecordSynthPlayback(equipmentIdx, startingBeat);
                break;
            case EquipmentCategory.DrumMachine:
                uiManager._RecordDrumMachinePlayback(equipmentIdx, startingBeat);
                break;
        }

        startCountdown.gameObject.SetActive(false);
        RecordPrepairing = false;

        yield return null;
    }
    IEnumerator ActivationCountdown()
    {
        ActivationPrepairing = true;

        startCountdown.gameObject.SetActive(true);
        startCountdown.color = Color.green;

        //int startingBeat = (int)(Mathf.Ceil((float)(MusicUtils.time)) + BeatBeforeRecordStart);
        float mesureProgress = (float)(MusicUtils.time) % ((60f / MusicUtils.BPM)*4);
        //int BeatBeforeStart = BaseBeatBeforeSynthStart-(Mathf.FloorToInt(mesureProgress));
        float remainingTime = (4 - mesureProgress);// + BeatBeforeStart;

        while ((remainingTime - Time.deltaTime) > 0)
        {
            remainingTime -= Time.deltaTime;

            float remainder = remainingTime % 1f;
            startCountdown.fontSize = ContdownFontSize - (1 - remainder) * 4;
            startCountdown.color = new Color(1 - remainder, 1, 1 - remainder);
            startCountdown.text = Mathf.CeilToInt(remainingTime).ToString();
            

            yield return new WaitForEndOfFrame();
        }
        //remainingTime -= Time.deltaTime;

        //uiManager._ActivateSynthPlayback(PBidx);
        startCountdown.gameObject.SetActive(false);

        //Debug.Log("start");
        ActivationPrepairing = false;

        yield return null;
    }

}
