using System;
using System.Collections.Generic;
using TMPro;
using Unity.Entities;
using UnityEngine;
using UnityEngine.UI;
using Random = UnityEngine.Random;

public enum SynthUpgrade
{
    /// Oscillator
    SecondOscillator,
        SecondOscillatorSemiTune,
        SecondOscillatorFineTune,
    /// Filter
    Filter,
        FilterResonance,
        FilterEnveloppe,
    /// Unisson
    Unisson,
        UnissonSpread,
    /// Voices
    Voices,

}

public class EquipmentUpgradeManager : MonoBehaviour
{

    public static ushort totalNumOfSynthUpgrades;
    public static ushort totalNumOfDMinstruments;

    public GameObject XpProgressGB;
    public GameObject XpPanelGB;
    [SerializeField]
    private TextMeshProUGUI EqupimentUpgradeCountUItext;
    [SerializeField]
    private TextMeshProUGUI LvLUpgradeCountUItext;

    [HideInInspector]
    public short numOfAvailableUpgrades;
    [HideInInspector]
    public short numOfLvlUp = 0;

    public static SynthUpgrade[] BaseSynthUpgradeOption = new SynthUpgrade[]
    {
        SynthUpgrade.SecondOscillator,
        SynthUpgrade.Filter,
        SynthUpgrade.Unisson,
        SynthUpgrade.Voices,
    };


    public List<bool[]> synthsActivatedFeatures;
    public List<List<SynthUpgrade>> synthEquipmentsUpgradeOptions;


    public List<MachineDrumContent> drumMachineUpgradeOptions;

    //public MachineDrumContent drumMachineEquipmentsUpgradeOptions;
    //public const MachineDrumContent AlldrumMachineEquipmentsUpgradeOptions =
    //    MachineDrumContent.SnareDrum |
    //    MachineDrumContent.BaseDrum |
    //    MachineDrumContent.HighHat;

    float uiXp;
    ushort uiLvl;
    EntityManager entityManager;
    Entity XpEntity;
    EntityQuery energyDataQuery;

    private void Start()
    {
        totalNumOfSynthUpgrades = (ushort)(Enum.GetValues(typeof(SynthUpgrade)).Length);
        totalNumOfDMinstruments = (ushort)(Enum.GetValues(typeof(MachineDrumContent)).Length);

        synthsActivatedFeatures = new List<bool[]>();
        synthEquipmentsUpgradeOptions = new List<List<SynthUpgrade>>();

        ////drumMachineActivatedFeatures = new List<bool[]>();
        ///drumMachineEquipmentsUpgradeOptions = new List<MachineDrumContent>(totalNumOfMachineDrumInstruments);
        //drumMachineEquipmentsUpgradeOptions = AlldrumMachineEquipmentsUpgradeOptions;
        for (int i = 0; i < totalNumOfDMinstruments; i++)
        {
            drumMachineUpgradeOptions.Add((MachineDrumContent)i);
        }

        entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;
        var query = entityManager.CreateEntityQuery(typeof(XpData));
        XpEntity = query.GetSingletonEntity();
    }

    private void LateUpdate()
    {
        var xpData = entityManager.GetComponentData<XpData>(XpEntity);
        if (uiXp != xpData.currentXP)
        {
            UpdateXpUI(xpData.currentXP,xpData.XPtillNextLVL);
        }
        if (uiLvl != xpData.LVL)
        {
            numOfLvlUp++;
            UpdateLvLUI(xpData.LVL);
        }
    }

    public short upgradeEquipment(EquipmentCategory equipmentCategory, ushort absoluteEquipmentIdx, ushort relativeEquipmentIdx)
    {
        var uiManager = UIManager.Instance;
        switch (equipmentCategory)
        {
            case EquipmentCategory.Weapon:

                List<SynthUpgrade> synthEquipmentUpgradeOptions = synthEquipmentsUpgradeOptions[relativeEquipmentIdx];
                /// nothing to further upgrade
                if (synthEquipmentUpgradeOptions.Count < 1)
                {
                    return 99;
                }
                bool[] synthActivatedFeatues = synthsActivatedFeatures[relativeEquipmentIdx];

                var synthUpgrade = PopRandomUnordered(synthEquipmentUpgradeOptions, (short)Random.Range(0, synthEquipmentUpgradeOptions.Count));
                switch (synthUpgrade)
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

                var newDrumMachineData = entityManager.GetComponentData<DrumMachineData>(AudioManager.AuxillaryEquipmentEntities[absoluteEquipmentIdx]);
                if (drumMachineUpgradeOptions.Count < 1 | newDrumMachineData.InstrumentsInAddOrder.Length >=6)
                {
                    return 99;
                }
                var drumMachineupgrade = PopRandomUnordered(drumMachineUpgradeOptions, (short)Random.Range(0, drumMachineUpgradeOptions.Count));
                newDrumMachineData.InstrumentsInAddOrder.Add((byte)drumMachineupgrade);
                /// spagetios
                uiManager.drumPadVFX.transform.parent.GetChild(0).GetComponent<ToDrumPadShader>().UpdateInstrumentCount(newDrumMachineData.InstrumentsInAddOrder.Length);
                entityManager.SetComponentData<DrumMachineData>(AudioManager.AuxillaryEquipmentEntities[absoluteEquipmentIdx], newDrumMachineData);


                break;
        }
        var remainingUpgradeNum = --numOfAvailableUpgrades;

        UpdateUpgradeCounters();
        return remainingUpgradeNum;
    }
    public SynthUpgrade PopRandomUnordered(List<SynthUpgrade> upgradePool, short idx)
    {
        SynthUpgrade value = upgradePool[idx];
        int last = upgradePool.Count - 1;
        upgradePool[idx] = upgradePool[last]; // move last element into the hole
        upgradePool.RemoveAt(last);      // O(1) remove at end
        return value;
    }
    public MachineDrumContent PopRandomUnordered(List<MachineDrumContent> upgradePool, short idx)
    {
        MachineDrumContent value = upgradePool[idx];
        int last = upgradePool.Count - 1;
        upgradePool[idx] = upgradePool[last]; // move last element into the hole
        upgradePool.RemoveAt(last);      // O(1) remove at end
        return value;
    }

    private void UpdateXpUI(float xp, float xpTillNextLVL)
    {
        XpProgressGB.transform.GetChild(1).GetComponent<Slider>().value = xp / xpTillNextLVL;
        uiXp = xp;
    }
    private void UpdateLvLUI(ushort lvl)
    {
        XpProgressGB.transform.GetChild(0).GetComponent<TextMeshProUGUI>().text = "Level : " + lvl;
        XpPanelGB.SetActive(true);
        uiLvl = lvl;
        UpdateUpgradeCounters();
    }
    public void UpdateUpgradeCounters()
    {
        LvLUpgradeCountUItext.gameObject.transform.parent.gameObject.SetActive((numOfAvailableUpgrades + numOfLvlUp) > 0);
        EqupimentUpgradeCountUItext.text = numOfAvailableUpgrades.ToString();
        LvLUpgradeCountUItext.text = numOfLvlUp.ToString();
    }

}
