using System;
using System.Collections.Generic;
using TMPro;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.UIElements;
using Image = UnityEngine.UI.Image;
using Slider = UnityEngine.UI.Slider;
using MusicNamespace;
using UnityEngine.EventSystems;
using System.Linq;
using Unity.Transforms;
using Unity.Collections;
using Random = UnityEngine.Random;

/*
 Break on window dimention change --> TO FIX
 */

public struct FullEquipmentIdx
{    
    /// <summary>
    /// equipment equipmentIndex 
    /// </summary>
    public ushort absoluteIdx;
    /// <summary>
    /// equipment equipmentIndex relative to category : synth/drumMachine/...
    /// </summary>
    public ushort relativeIdx;
}

public class UIManager : MonoBehaviour
{

    public static UIManager Instance { get; private set; }

    [SerializeField]
    private AudioManager audioManager;

    /// only keep those ?
    [SerializeField]
    private GameObject SynthEditPanel;

    [SerializeField]
    public OscillatorUI oscillatorUI;
    [SerializeField]
    private ADSRUI volumeAdsrUI;
    [SerializeField]
    public ADSRUI filterAdsrUI;
    [SerializeField]
    public FilterUI filterUI;
    [SerializeField]
    public UnissonUI unissonUI;
    [SerializeField]
    public VoicesUI voicesUI;
    //[HideInInspector]
    //public EquipmentUpgradeManager equipmentUpgradeManager;

    public UIPlaybacksHolder UIplaybacksHolder;
    public GameObject MusicSheetGB;
    public GameObject MusicTrackGB;

    [SerializeField]
    private GameObject KeyboardShaderGB;
    [SerializeField]
    private GameObject DrumPadShaderGB;
    public DrumPadVFX drumPadVFX;

    [SerializeField]
    SliderMono simplexSlider;

    public Transform toolTip;
    [SerializeField]
    GameObject BuildingBlueprintGB;

    private EntityManager entityManager;
    //private EntityQuery Player_query;

    public Canvas canvas;
    //private NativeArray<AABB> UIsurface;

    [SerializeField]
    private int MaxEquipmentNum;
    //[SerializeField]
    public GameObject equipmentToolBar;

    [HideInInspector]
    public ushort NumOfEquipments = 0;
    public short NumOfDMachines = 0;
    public short NumOfSynths = 0;
    /// Need local activeSynthIdx to modify previous activeSynth upon change
    int activeUISynthIdx = -1;
    /// Same
    int activeUIDrumMachineIdx = -1;

    public short activeEquipmentIdx = -1;
    [HideInInspector]
    public bool curentlyRecording = false;

    RectTransform SynthUIadd_rect;
    /// equipmentIndex ; isPlaying ; is recording
    List<(short,bool,bool)> activeUIEquipment;
    //Dictionary<int> synthsInfo = new();

    List<short> EquipmentIdxToSynthDataIdx;

    private Vector2 PreviousMousePos;

    float TimeForShaderSync = 0;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject); // Ensure only one instance exists
            return;
        }

        DontDestroyOnLoad(gameObject); // Keep it across scenes
    }

    void Start()
    {

        var world = World.DefaultGameObjectInjectionWorld;
        entityManager = world.EntityManager;
        //Player_query = entityManager.CreateEntityQuery(typeof(PlayerData));

        activeUIEquipment = new List<(short, bool, bool)>();
        EquipmentIdxToSynthDataIdx = new List<short>();

        ///ConstructUIsurface();

        //NumOfSynths = equipmentToolBar.GetComponentsInChildren<Button>().Length-1;
        SynthUIadd_rect = equipmentToolBar.transform.GetChild(equipmentToolBar.transform.childCount-1).GetComponent<RectTransform>();


    }


    void Update()
    {

        TimeForShaderSync += Time.deltaTime;

        var mousePos = InputManager.mousePos;
        //InputManager.mouseDelta = mousePos - PreviousMousePos;
        //Debug.Log(mouseDelta);
        PreviousMousePos = mousePos;

        UIInput.MouseOverUI = EventSystem.current.IsPointerOverGameObject(PointerId.mousePointerId);
        //for (int i = 0;i < UIsurface.Length;i++)
        //{
        //    if(PhysicsUtilities.PointInsideShape(mousePos, UIsurface[i]))
        //    {
        //        UIInputSystem.MouseOverUI = true;
        //        break;
        //    }
        //}
        /// OPTI
        for (int i = 0; i < activeUIEquipment.Count; i++)
        {
            //Debug.Log(activeUISynths[i]);
            Slider slider = equipmentToolBar.transform.GetChild(activeUIEquipment[i].Item1).gameObject.GetComponentInChildren<Slider>();
            slider.value += Time.deltaTime;

            if (slider.value + Time.deltaTime > slider.maxValue)
            {
                slider.value = (slider.value + Time.deltaTime - slider.maxValue);
            }
        }

    }

    #region TOOLTIP

    public void UpdateDisplayToolTip(Vector2 pos, String content)
    {
        //Debug.Log(toolTip.GetChild(0).transform.GetComponent<RectTransform>().sizeDelta.x);
        toolTip.GetChild(0).GetComponent<TextMeshProUGUI>().text = content;
        // Force a layout update so we can use it's updated RectTransform right away
        LayoutRebuilder.ForceRebuildLayoutImmediate(toolTip.GetComponent<RectTransform>());

        //float sizeX = toolTip.GetChild(0).transform.GetComponent<RectTransform>().sizeDelta.x * 0.025f;
        //float sizey = toolTip.GetChild(0).transform.GetComponent<RectTransform>().sizeDelta.y * 0.025f;
        //toolTip.position = new Vector3(pos.x + sizeX, pos.y);
        toolTip.position = new Vector3(pos.x + toolTip.GetChild(0).transform.GetComponent<RectTransform>().sizeDelta.x * 0.025f, pos.y);
        //Debug.Log(toolTip.GetChild(0).transform.GetComponent<RectTransform>().sizeDelta.x * 0.025f);
    }
    public void ForceDisableTooltip()
    {
        toolTip.gameObject.SetActive(false);
    }

    #endregion

    /// <summary>
    /// For deprecated tower defence
    /// </summary>
    /// <param name="index"></param>
    /// <param name="buildingInfo"></param>
    public void _SelectBuildingUI(ushort index, BuildingInfo buildingInfo)
    {
        _SelectSynthUI(index);

        /// buildin placement blueprint GB activation
        BuildingBlueprintGB.SetActive(true);

        var buildingBlueprint = BuildingBlueprintGB.GetComponent<BuildingBlueprintUI>();
        buildingBlueprint.buildingInfo = buildingInfo;
        /// arbitrary but tailored to 0.85 scale circle sprite
        buildingBlueprint.CurrentCircle = new CircleShapeData { radius = 0.55f };

        /// change synthUI color to distinguish
        equipmentToolBar.transform.GetChild(index).GetChild(0).GetComponent<Image>().color = Color.black;
        equipmentToolBar.transform.GetChild(index).GetChild(2).GetChild(0).gameObject.SetActive(true);
    }

    #region SYNTHS / DRUM MACHINE

    public void _SelectSynthUI(ushort equipmentIndex)
    {
        if (!audioManager.ControlledEquipment_query.IsEmpty)
        {
            Entity weapon_entity = audioManager.ControlledEquipment_query.GetSingletonEntity();
            /// prevent synth select if already selected or curently recording
            if (equipmentIndex == activeEquipmentIdx || curentlyRecording)
            { return; }
        }

        //var synthIdx = EquipmentIdxToSynthDataIdx[equipmentIndex];
        SynthEditPanel.SetActive(true);
        short synthDataIdx = EquipmentIdxToSynthDataIdx[equipmentIndex];
        SynthData selectedSynthData = AudioLayoutStorageHolder.audioLayoutStorage.AuxillarySynthsData[synthDataIdx];

        UpdateSynthFeatures(synthDataIdx);

        UpdateSynthUI(
            in selectedSynthData,
            entityManager.GetComponentData<WeaponData>(AudioManager.AuxillaryEquipmentEntities[equipmentIndex]).weaponType
            );

        /// disable upgrade on previous
        if (activeEquipmentIdx!=-1) 
            equipmentToolBar.transform.GetChild(activeEquipmentIdx).GetComponent<EquipmentUIelement>().upgradeButtonGB.SetActive(false);
        /// activate on selected if available
        equipmentToolBar.transform.GetChild(equipmentIndex).GetComponent<EquipmentUIelement>().upgradeButtonGB.SetActive(equipmentToolBar.GetComponent<EquipmentUpgradeManager>().numOfAvailableUpgrades > 0);
        
        activeEquipmentIdx = (short)equipmentIndex;

        KeyboardShaderGB.SetActive(true);
        /// activate Rec button GB for new synth and deactivate for the old one
        if (activeUISynthIdx != -1)
        {
            equipmentToolBar.transform.GetChild(activeUISynthIdx).GetChild(2).GetChild(0).gameObject.SetActive(false);
            equipmentToolBar.transform.GetChild(activeUISynthIdx).GetChild(0).GetComponent<Image>().color = Color.white;
            equipmentToolBar.transform.GetChild(activeUISynthIdx).GetComponent<EquipmentUIelement>()._CancelRecord();
            MusicSheetGB.SetActive(false);
        }
        else if (activeUIDrumMachineIdx != -1)
        {
            equipmentToolBar.transform.GetChild(activeUIDrumMachineIdx).GetChild(2).GetChild(0).gameObject.SetActive(false);
            equipmentToolBar.transform.GetChild(activeUIDrumMachineIdx).GetChild(0).GetComponent<Image>().color = Color.white;
            DrumPadShaderGB.SetActive(false);
        }
        /// change synthUI color to distinguish
        equipmentToolBar.transform.GetChild(equipmentIndex).GetChild(0).GetComponent<Image>().color = Color.black;
        equipmentToolBar.transform.GetChild(equipmentIndex).GetChild(2).GetChild(0).gameObject.SetActive(true);

        AudioLayoutStorage.activeSynthIdx = synthDataIdx;

        activeUISynthIdx = equipmentIndex;
        activeUIDrumMachineIdx = -1;

        audioManager.SelectSynth(equipmentIndex, synthDataIdx);

    }
    public void _SelectMachineDrumUI(int equipmentIndex)
    {
        if (!audioManager.ControlledEquipment_query.IsEmpty)
        {
            Entity weapon_entity = audioManager.ControlledEquipment_query.GetSingletonEntity();
            /// prevent synth select if already selected or curently recording
            if (equipmentIndex == activeEquipmentIdx || curentlyRecording)
            { return; }
        }

        /// disable upgrade on previous
        if (activeEquipmentIdx != -1)
            equipmentToolBar.transform.GetChild(activeEquipmentIdx).GetComponent<EquipmentUIelement>().upgradeButtonGB.SetActive(false);
        /// activate on selected if available
        equipmentToolBar.transform.GetChild(equipmentIndex).GetComponent<EquipmentUIelement>().upgradeButtonGB.SetActive(equipmentToolBar.GetComponent<EquipmentUpgradeManager>().numOfAvailableUpgrades > 0);


        activeEquipmentIdx = (short)equipmentIndex;

        DrumPadShaderGB.SetActive(true);
        SynthEditPanel.SetActive(false);

        /// activate Rec button GB for new synth and deactivate for the old one
        if (activeUISynthIdx != -1)
        {
            equipmentToolBar.transform.GetChild(activeUISynthIdx).GetChild(2).GetChild(0).gameObject.SetActive(false);
            equipmentToolBar.transform.GetChild(activeUISynthIdx).GetChild(0).GetComponent<Image>().color = Color.white;
            equipmentToolBar.transform.GetChild(AudioLayoutStorage.activeSynthIdx).GetComponent<EquipmentUIelement>()._CancelRecord();
            KeyboardShaderGB.SetActive(false);
        }
        else if (activeUIDrumMachineIdx != -1)
        {
            equipmentToolBar.transform.GetChild(activeUIDrumMachineIdx).GetChild(2).GetChild(0).gameObject.SetActive(false);
            equipmentToolBar.transform.GetChild(activeUIDrumMachineIdx).GetChild(0).GetComponent<Image>().color = Color.white;
        }
        /// change synthUI color to distinguish
        equipmentToolBar.transform.GetChild(equipmentIndex).GetChild(0).GetComponent<Image>().color = Color.black;
        equipmentToolBar.transform.GetChild(equipmentIndex).GetChild(2).GetChild(0).gameObject.SetActive(true);

        AudioLayoutStorage.activeSynthIdx = -1;

        activeUISynthIdx = -1;
        activeUIDrumMachineIdx = equipmentIndex;

        audioManager.SelectDrumMachine(equipmentIndex);
    }
    public void _AddSynthUI(WeaponClass weaponClass, WeaponType weaponType)
    {
        var synthUI = equipmentToolBar.transform.GetChild(NumOfEquipments).gameObject;

        synthUI.GetComponent<EquipmentUIelement>().thisEquipmentIdx = NumOfEquipments;
        synthUI.GetComponent<EquipmentUIelement>().thisRelativeEquipmentIdx = (ushort)NumOfSynths;
        equipmentToolBar.GetComponent<EquipmentUpgradeManager>().synthEquipmentsUpgradeOptions.Add(EquipmentUpgradeManager.BaseSynthUpgradeOption.ToList());
        equipmentToolBar.GetComponent<EquipmentUpgradeManager>().synthsActivatedFeatures.Add(new bool[EquipmentUpgradeManager.totalNumOfSynthUpgrades]);
        synthUI.GetComponentInChildren<TextMeshProUGUI>().text = "Synth " + (NumOfSynths + 1);

        NumOfSynths++;
        NumOfEquipments++;
        equipmentToolBar.transform.GetChild(NumOfEquipments - 1).GetComponent<EquipmentUIelement>().thisEquipmentCategory = EquipmentCategory.Weapon;

        EquipmentIdxToSynthDataIdx.Add((short)(NumOfSynths - 1));

        UIplaybacksHolder.PBholders[NumOfEquipments - 1].equipmentCategory = EquipmentCategory.Weapon;

        SynthData newSynthData = SynthData.CreateDefault(weaponType);

        audioManager.AddSynth(NumOfEquipments, newSynthData, weaponClass, weaponType);

        synthUI.SetActive(true);

        //if (NumOfEquipments == 1)
        //{
        //    _SelectSynthUI(0);
        //    //UpdateSynthUI(in newSynthData, weaponType);

        //    //activeUISynthIdx = 0;
        //    //audioManager.activeWeaponIDX = 0;
        //    //SynthEditPanel.SetActive(true);
        //}

    }

    public void _AddDrumMachineUI()
    {
        var synthUI = equipmentToolBar.transform.GetChild(NumOfEquipments).gameObject;
        synthUI.SetActive(true);
        synthUI.GetComponent<EquipmentUIelement>().thisEquipmentIdx = NumOfEquipments;

        var drumMachineUpgradeOptions = equipmentToolBar.GetComponent<EquipmentUpgradeManager>().drumMachineUpgradeOptions;
        MachineDrumContent randomMachineDrumContent = drumMachineUpgradeOptions[Random.Range(0, drumMachineUpgradeOptions.Count)];
        drumMachineUpgradeOptions.Remove(randomMachineDrumContent);
        ////equipmentToolBar.GetComponent<EquipmentUpgradeManager>().drumMachineActivatedFeatures.Add(new bool[EquipmentUpgradeManager.totalNumOfDMinstruments]);
        synthUI.GetComponent<EquipmentUIelement>().thisRelativeEquipmentIdx = (ushort)NumOfDMachines;
        ///synthUI.GetComponent<EquipmentUIelement>().transform.parent.GetComponent<EquipmentUpgradeManager>().synthEquipmentsUpgradeOptions.Add(EquipmentUpgradeManager.BaseSynthUpgradeOption.ToList());

        synthUI.GetComponentInChildren<TextMeshProUGUI>().text = "Drum Machine " + (NumOfDMachines + 1);

        NumOfDMachines++;
        NumOfEquipments++;
        equipmentToolBar.transform.GetChild(NumOfEquipments - 1).GetComponent<EquipmentUIelement>().thisEquipmentCategory = EquipmentCategory.DrumMachine;
        /// add invalid equipmentIndex for padding
        EquipmentIdxToSynthDataIdx.Add(-1);

        UIplaybacksHolder.PBholders[NumOfEquipments - 1].equipmentCategory = EquipmentCategory.DrumMachine;

        //SynthData newSynthData = SynthData.CreateDefault(weaponType);

        audioManager.AddDrumMachine(NumOfEquipments, randomMachineDrumContent);

        //if (NumOfEquipments == 1)
        //{
        //    _SelectMachineDrumUI(0);

        //    //UpdateSynthUI(in newSynthData, weaponType);

        //    //SynthEditPanel.SetActive(true);
        //}

    }
    /// Update the synth edit pannel to correspond with the synth's current upgrades
    void UpdateSynthFeatures(short synthIdx)
    {
        bool[] activatedFeatures = equipmentToolBar.GetComponent<EquipmentUpgradeManager>().synthsActivatedFeatures[synthIdx];
        oscillatorUI.ReviewActivatedFeatures(activatedFeatures);
        filterUI.gameObject.SetActive(activatedFeatures[3]);
        filterUI.ReviewActivatedFeatures(activatedFeatures);
        unissonUI.gameObject.SetActive(activatedFeatures[6]);
        unissonUI.ReviewActivatedFeatures(activatedFeatures);
        voicesUI.gameObject.SetActive(activatedFeatures[8]);
        /// enable/disable upgrade button -> not nessesary for now as upgrade charges are global
        ///equipmentToolBar.transform.GetChild(UIManager.Instance.activeEquipmentIdx).GetComponent<EquipmentUIelement>().upgradeButtonGB.SetActive(equipmentToolBar.GetComponent<EquipmentUpgradeManager>().numOfAvailableUpgrades > 0);
    }
    void UpdateSynthUI(in SynthData synthData, WeaponType weaponType)
    {
        //var synthData = AudioManager.audioGenerator.SynthsData[activeUISynthIdx];
        //Debug.Log(adsrLimits.Sustainimits);
        oscillatorUI.UpdateUI(synthData);
        volumeAdsrUI.UpdateUI(synthData, weaponType);
        filterAdsrUI.UpdateUI(synthData, WeaponType.Null);
        filterUI.UpdateUI(synthData);
        unissonUI.UpdateUI(synthData);
        voicesUI.UpdateUI(synthData);

    }
    public void _ResetPlayback(int equipmentIdx)
    {
        /// OPTI
        /// If the the Current playback of the synth is playing, stop it
        //for (int i = 1; i < AudioManager.audioGenerator.activeSynthsIdx.Length; i++)
        //{
        //    if (AudioManager.audioGenerator.activeSynthsIdx[i] == activeSynthIdx)
        //    {
        //        _StopSynthPlayback(activeSynthIdx);
        //    }
        //}

        var slider = equipmentToolBar.transform.GetChild(equipmentIdx).gameObject.GetComponentInChildren<Slider>();
        ///change slider background color
        slider.gameObject.transform.GetChild(0).GetComponent<Image>().color = Color.grey;

        switch (UIplaybacksHolder.PBholders[equipmentIdx].equipmentCategory)
        {
            case EquipmentCategory.Weapon:
                MusicSheetGB.GetComponent<MusicSheetToShader>().enabled = true;
                MusicSheetGB.GetComponent<DrumPadSheetToShader>().enabled = false;
                break;
            case EquipmentCategory.DrumMachine:
                MusicSheetGB.GetComponent<MusicSheetToShader>().enabled = false;
                MusicSheetGB.GetComponent<DrumPadSheetToShader>().enabled = true;
                break;
            default:
                break;
        }
        MusicSheetGB.SetActive(true);

        /// dont dispose as the memory responsability is passed to the playbackContainer
        ///AudioLayoutStorageHolder.audioLayoutStorage.ActiveMusicSheet._Dispose();
        AudioLayoutStorageHolder.audioLayoutStorage.ActiveMusicSheet = MusicSheetData.CreateDefault();
        AudioLayoutStorageHolder.audioLayoutStorage.ActiveDrumPadSheetData = DrumPadSheetData.CreateDefault();
    }
    public void _RecordSynthPlayback(ushort equipmentIdx, int startingBeat)
    {

        ushort synthIdx = (ushort)EquipmentIdxToSynthDataIdx[equipmentIdx];

        var slider = equipmentToolBar.transform.GetChild(equipmentIdx).gameObject.GetComponentInChildren<Slider>();
        //var newColor = new Color { a = 1, r = (207 / 256), g = (207 / 256), b = (207 / 256) };
        slider.value = 0;
        /// PUT RECORDING LENGHT FIX
        slider.maxValue = audioManager.TEMPplaybackDuration;
        ///change slider background color
        slider.gameObject.transform.GetChild(0).GetComponent<Image>().color = Color.white;

        var synthInfo = activeUIEquipment.FirstOrDefault(item => item.Item1 == (short)equipmentIdx);
        bool synthAlreadyActive = !synthInfo.Equals(default((short, bool, bool)));

        MusicSheetGB.GetComponent<MusicSheetToShader>().enabled = true;
        MusicSheetGB.GetComponent<DrumPadSheetToShader>().enabled = false;
        MusicSheetGB.SetActive(true);

        if (synthAlreadyActive)
        {
            /// both playing and recording in mesure
            if (synthInfo.Item3 == true) slider.gameObject.transform.GetChild(1).GetChild(0).GetComponent<Image>().color = Color.yellow;

        }
        else
        {
            ///change slider color
            slider.gameObject.transform.GetChild(1).GetChild(0).GetComponent<Image>().color = Color.red;
            activeUIEquipment.Add(((short)equipmentIdx, true, false));
        }


        var ecb = audioManager.beginSimulationECBSystem.CreateCommandBuffer();

        Entity weapon_entity = audioManager.ControlledEquipment_query.GetSingletonEntity();

        /// If the the Current playback of the synth is playing, stop it
        //for (int i = 1; i < AudioManager.audioGenerator.activeSynthsIdx.Length; i++)
        //{
        //    if (AudioManager.audioGenerator.activeSynthsIdx[i] == activeSynthIdx)
        //    {
        //        Debug.Log("here");
        //        _StopSynthPlayback(activeSynthIdx);
        //    }
        //}

        ///// Deactivate Play button GB
        //equipmentToolBar.transform.GetChild(activeSynthIdx).GetChild(2).GetChild(1).gameObject.SetActive(false);
        ///// Activate Stop button GB
        //equipmentToolBar.transform.GetChild(activeSynthIdx).GetChild(2).GetChild(2).gameObject.SetActive(true);


        if (!entityManager.HasComponent<SynthPlaybackRecordingData>(weapon_entity))
        {
            //Debug.Log(equipmentIdx);
            var playbackRecordingData = new SynthPlaybackRecordingData
            {
                duration = audioManager.TEMPplaybackDuration,
                fEquipmentIdx = new FullEquipmentIdx { absoluteIdx = equipmentIdx, relativeIdx = synthIdx },
                startBeat = startingBeat,
                GideReferenceDirection = WeaponSystem.GideReferenceDirection
            };

            ecb.AddComponent<SynthPlaybackRecordingData>(weapon_entity, playbackRecordingData);
            ecb.AddBuffer<PlaybackRecordingKeysBuffer>(weapon_entity);

            //PlaybackRecordSystem.KeyJustPressed = false;
            //PlaybackRecordSystem.KeyJustReleased = false;
        }
        if (entityManager.HasBuffer<PlaybackSustainedKeyBufferData>(weapon_entity))
        {
            Debug.Log("has entityManager.GetBuffer<PlaybackReleasedKeyBufferData");
            //entityManager.GetBuffer<PlaybackSustainedKeyBufferData>(weapon_entity).Clear();
            //entityManager.GetBuffer<PlaybackReleasedKeyBufferData>(weapon_entity).Clear();
        }
    }

    public void _RecordDrumMachinePlayback(ushort equipmentIdx, int startingBeat)
    {
        /// assume there is only one DM for now
        ///ushort DrumMachineIdx = equipmentIdx;

        var slider = equipmentToolBar.transform.GetChild(equipmentIdx).gameObject.GetComponentInChildren<Slider>();
        //var newColor = new Color { a = 1, r = (207 / 256), g = (207 / 256), b = (207 / 256) };
        slider.value = 0;
        /// PUT RECORDING LENGHT FIX
        slider.maxValue = audioManager.TEMPplaybackDuration;
        ///change slider background color
        slider.gameObject.transform.GetChild(0).GetComponent<Image>().color = Color.white;

        MusicSheetGB.GetComponent<MusicSheetToShader>().enabled = false;
        MusicSheetGB.GetComponent<DrumPadSheetToShader>().enabled = true;
        MusicSheetGB.SetActive(true);

        var equipmentInfo = activeUIEquipment.FirstOrDefault(item => item.Item1 == (short)equipmentIdx);
        bool equipmentAlreadyActive = !equipmentInfo.Equals(default((short, bool, bool)));

        if (equipmentAlreadyActive)
        {
            /// both playing and recording in mesure
            if (equipmentInfo.Item3 == true) slider.gameObject.transform.GetChild(1).GetChild(0).GetComponent<Image>().color = Color.yellow;

        }
        else
        {
            ///change slider color
            slider.gameObject.transform.GetChild(1).GetChild(0).GetComponent<Image>().color = Color.red;
            activeUIEquipment.Add(((short)equipmentIdx, true, false));
        }


        var ecb = audioManager.beginSimulationECBSystem.CreateCommandBuffer();

        Entity machineDrum_entity = AudioManager.AuxillaryEquipmentEntities[equipmentIdx];


        if (!entityManager.HasComponent<DrumMachinePlaybackRecordingData>(machineDrum_entity))
        {
            //Debug.Log(equipmentIdx);
            var playbackRecordingData = new DrumMachinePlaybackRecordingData
            {
                duration = audioManager.TEMPplaybackDuration,
                /// assume there is only one DM for now
                fEquipmentIdx = new FullEquipmentIdx { absoluteIdx = equipmentIdx, relativeIdx = 0 },
                startBeat = startingBeat,
            };

            ecb.AddComponent<DrumMachinePlaybackRecordingData>(machineDrum_entity, playbackRecordingData);
            ecb.AddBuffer<PlaybackRecordingPadsBuffer>(machineDrum_entity);

        }
        else
        { Debug.LogError("PB?"); }
    }

    /// <summary>
    /// OPTI : PASS ECB INSTEAD OF CREATING ONE EVERY TIME
    /// </summary>
    /// <param name="PBidx"></param>
    public void _ActivateSynthPlayback(int2 PBidx)
    {
        ushort relativeEquipmentIdx = (ushort)EquipmentIdxToSynthDataIdx[PBidx.x];

        /// unessesary if same playback recording being reset ?
        AudioLayoutStorageHolder.audioLayoutStorage.WritePlayback(
            UIplaybacksHolder.synthFullBundleLists[relativeEquipmentIdx][PBidx.y].Item1.playbackAudioBundle,
            relativeEquipmentIdx);

        var slider = equipmentToolBar.transform.GetChild(PBidx.x).gameObject.GetComponentInChildren<Slider>();
        slider.value = 0;
        /// PUT RECORDING LENGHT FIX
        //slider.maxValue = audioManager.TEMPplaybackDuration;
        ///change slider background color
        slider.gameObject.transform.GetChild(0).GetComponent<Image>().color = Color.white;

        var synthInfo = activeUIEquipment.FirstOrDefault(item => item.Item1 == (short)PBidx.x);
        bool synthAlreadyActive = !synthInfo.Equals(default((short, bool, bool)));
        if (synthAlreadyActive)
        {
            /// both playing and recording in mesure
            if (synthInfo.Item2 == true) slider.gameObject.transform.GetChild(1).GetChild(0).GetComponent<Image>().color = Color.yellow;

        }
        else
        {
            ///change slider color
            slider.gameObject.transform.GetChild(1).GetChild(0).GetComponent<Image>().color = Color.green;
            activeUIEquipment.Add(((short)PBidx.x, false, true));
        }

        Entity weapon_entity = AudioManager.AuxillaryEquipmentEntities[PBidx.x];

        var ecb = audioManager.beginSimulationECBSystem.CreateCommandBuffer();

        if (entityManager.HasComponent<PlaybackData>(weapon_entity))
        {
            AudioLayoutStorageHolder.audioLayoutStorage.PlaybackContextResetRequired.Enqueue(relativeEquipmentIdx);
            ecb.SetComponent<PlaybackData>(weapon_entity, new PlaybackData { FullPlaybackIndex = new int2(relativeEquipmentIdx, PBidx.y) });
            entityManager.GetBuffer<PlaybackSustainedKeyBufferData>(weapon_entity).Clear();
            entityManager.GetBuffer<PlaybackReleasedKeyBufferData>(weapon_entity).Clear();
        }
        else
        {

            PlaybackData playbackData = new PlaybackData { FullPlaybackIndex = new int2(relativeEquipmentIdx, PBidx.y) };

            ecb.AddComponent<PlaybackData>(weapon_entity, playbackData);

            AudioLayoutStorageHolder.audioLayoutStorage.WriteActivation(relativeEquipmentIdx);
        }


    }
    /// <summary>
    /// OPTI : PASS ECB INSTEAD OF CREATING ONE EVERY TIME
    /// </summary>
    public void _ActivateDrumMachinePlayback(int2 PBidx)
    {
        /// fix for more than 1 DM
        ushort relativeEquipmentIdx = 0;

        var slider = equipmentToolBar.transform.GetChild(PBidx.x).gameObject.GetComponentInChildren<Slider>();
        slider.value = 0;
        /// PUT RECORDING LENGHT FIX
        //slider.maxValue = audioManager.TEMPplaybackDuration;
        ///change slider background color
        slider.gameObject.transform.GetChild(0).GetComponent<Image>().color = Color.white;

        var synthInfo = activeUIEquipment.FirstOrDefault(item => item.Item1 == (short)PBidx.x);
        bool synthAlreadyActive = !synthInfo.Equals(default((short, bool, bool)));
        if (synthAlreadyActive)
        {
            /// both playing and recording in mesure
            if (synthInfo.Item2 == true) slider.gameObject.transform.GetChild(1).GetChild(0).GetComponent<Image>().color = Color.yellow;

        }
        else
        {
            ///change slider color
            slider.gameObject.transform.GetChild(1).GetChild(0).GetComponent<Image>().color = Color.green;
            activeUIEquipment.Add(((short)PBidx.x, false, true));
        }

        Entity weapon_entity = AudioManager.AuxillaryEquipmentEntities[PBidx.x];

        var ecb = audioManager.beginSimulationECBSystem.CreateCommandBuffer();

        if (entityManager.HasComponent<PlaybackData>(weapon_entity))
        {
            ecb.SetComponent<PlaybackData>(weapon_entity, new PlaybackData { FullPlaybackIndex = new int2(relativeEquipmentIdx, PBidx.y) });
        }
        else
        {
            PlaybackData playbackData = new PlaybackData { FullPlaybackIndex = new int2(relativeEquipmentIdx, PBidx.y) };
            ecb.AddComponent<PlaybackData>(weapon_entity, playbackData);
        }

    }
    /// Porly optimized -> rework
    public bool _ConsumePlaybackContainer(EquipmentCategory equipmentCategory, ushort equipmentIdx, ushort PBcontainerIdx)
    {
        bool emptyedContainer = false;
        if (equipmentCategory == EquipmentCategory.Weapon)
        {
            ushort relativeEquipmentIdx = (ushort)EquipmentIdxToSynthDataIdx[equipmentIdx];
            emptyedContainer = UIplaybacksHolder._ConsumeContainerCharge(equipmentCategory, new FullEquipmentIdx { absoluteIdx = equipmentIdx, relativeIdx = relativeEquipmentIdx }, PBcontainerIdx);
        }
        else
        {
            ushort relativeEquipmentIdx = 0;
            emptyedContainer = UIplaybacksHolder._ConsumeContainerCharge(equipmentCategory, new FullEquipmentIdx { absoluteIdx = equipmentIdx, relativeIdx = relativeEquipmentIdx }, PBcontainerIdx);
        }
        if (UIplaybacksHolder.PBholders[equipmentIdx].ContainerNumber == 0)
        {
            /// Deactivate Stop button GB
            equipmentToolBar.transform.GetChild(equipmentIdx).GetChild(2).GetChild(2).gameObject.SetActive(false);
        }
        return emptyedContainer;
    }
    /// <summary>
    /// OPTI : PASS ECB INSTEAD OF CREATING ONE EVERY TIME
    /// </summary>
    public void _StopSynthPlayback(int equipmentIdx)
    {
        ushort synthIdx = (ushort)EquipmentIdxToSynthDataIdx[equipmentIdx];

        ///prevent activation if no playback playing
        //var weaponIdx = equipmentIdx + 1;
        Entity weapon_entity = AudioManager.AuxillaryEquipmentEntities[equipmentIdx];

        var ecb = audioManager.beginSimulationECBSystem.CreateCommandBuffer();

        _SetEquipmentUItoSleep(equipmentIdx);

        /// SAFETY
        bool isRunning = false;
        /// If the the equipmentIdx is playing, stop it
        for (int i = 1; i < AudioManager.audioGenerator.activeSynthsIdx.Length; i++)
        {
            if (AudioManager.audioGenerator.activeSynthsIdx[i] == synthIdx)
            {
                isRunning = true;
            }
        }
        if (!isRunning) Debug.LogError("stoped a non runing playback");
        ///

        AudioLayoutStorageHolder.audioLayoutStorage.WriteDeactivation(synthIdx);

        /// Activate Rec button GB
        //equipmentToolBar.transform.GetChild(equipmentIdx).GetChild(2).GetChild(0).gameObject.SetActive(true);

        /// reset rotation to default
        LocalTransform newLocalTransform = entityManager.GetComponentData<LocalTransform>(weapon_entity);
        newLocalTransform.Rotation = Quaternion.Euler(0, 0, newLocalTransform.Position.x < 0 ? -45 : 225);
        ecb.SetComponent<LocalTransform>(weapon_entity, newLocalTransform);

        ecb.RemoveComponent<PlaybackData>(weapon_entity);


        /// Activate Play button GB
        //equipmentToolBar.transform.GetChild(equipmentIdx).GetChild(2).GetChild(1).gameObject.SetActive(true);
        /// Deactivate Stop button GB
        //equipmentToolBar.transform.GetChild(equipmentIdx).GetChild(2).GetChild(2).gameObject.SetActive(false);

        //if (entityManager.HasBuffer<PlaybackSustainedKeyBufferData>(weapon_entity))
        //{
        //    ecb.RemoveComponent<PlaybackSustainedKeyBufferData>(weapon_entity);
        //    ecb.RemoveComponent<PlaybackReleasedKeyBufferData>(weapon_entity);
        //}



    }
    public void _StopDrumMachinePlayback(int equipmentIdx)
    {
        ///prevent activation if no playback playing
        //var weaponIdx = equipmentIdx + 1;
        Entity weapon_entity = AudioManager.AuxillaryEquipmentEntities[equipmentIdx];

        var ecb = audioManager.beginSimulationECBSystem.CreateCommandBuffer();

        _SetEquipmentUItoSleep(equipmentIdx);

        /// Activate Rec button GB
        //equipmentToolBar.transform.GetChild(equipmentIdx).GetChild(2).GetChild(0).gameObject.SetActive(true);

        ecb.RemoveComponent<PlaybackData>(weapon_entity);

    }
    public void _StartAutoPlay(ushort equipmentIdx)
    {
        /// Deactivate Play button GB
        equipmentToolBar.transform.GetChild(equipmentIdx).GetChild(2).GetChild(1).gameObject.SetActive(false);
        /// Activate Stop button GB
        equipmentToolBar.transform.GetChild(equipmentIdx).GetChild(2).GetChild(2).gameObject.SetActive(true);

        UIplaybacksHolder.PBholders[equipmentIdx].AutoPlayOn = true;
    }
    public void _StopAutoPlay(ushort equipmentIdx)
    {
        UIplaybacksHolder.PBholders[equipmentIdx].AutoPlayOn = false;
        if (UIplaybacksHolder.PBholders[equipmentIdx].ContainerNumber > 0)
        {
            /// OPTI
            if (UIplaybacksHolder.PBholders[equipmentIdx].transform.GetChild(0).GetComponent<PlaybackContainerUI>().containerCharges > 1)
            {
                /// Activate Play button GB
                equipmentToolBar.transform.GetChild(equipmentIdx).GetChild(2).GetChild(1).gameObject.SetActive(true);
            }
        }
        /// Deactivate Stop button GB
        equipmentToolBar.transform.GetChild(equipmentIdx).GetChild(2).GetChild(2).gameObject.SetActive(false);
        equipmentToolBar.transform.GetChild(equipmentIdx).GetComponent<EquipmentUIelement>().ActivationPrepairing = false;
    }

    /// <summary>
    /// OPTI : PASS ECB INSTEAD OF CREATING ONE EVERY TIME
    /// </summary>
    public void _SetEquipmentUItoSleep(int equipmentIdx)
    {
        var slider = equipmentToolBar.transform.GetChild(equipmentIdx).gameObject.GetComponentInChildren<Slider>();
        var synthInfo = activeUIEquipment.FirstOrDefault(item => item.Item1 == (short)equipmentIdx);

        if (!synthInfo.Equals(default((short, bool, bool))))
        {
            //Debug.Log("remove");
            activeUIEquipment.Remove(synthInfo);
        }

        ///change slider background color
        slider.gameObject.transform.GetChild(0).GetComponent<Image>().color = Color.grey;
        slider.value = 0;
    }

    public void PlayRequestedDMachineEffects(NativeList<(ushort, float)> requests)
    {
        for (int i = 0; i < requests.Length; i++)
        {
            /// OPTI : redondant operation across calls
            switch (requests[i].Item1)
            {
                case 0:
                    drumPadVFX.UpdateKickShader(TimeForShaderSync);
                    break;
                case 1:
                    drumPadVFX.UpdateSnareShader(requests[i].Item2, TimeForShaderSync);
                    break;
                case 2:
                    drumPadVFX.UpdateHitHatShader(TimeForShaderSync);
                    break;
            }
            //UpdateSnareShader
            //DMachineAudioSource.PlayOneShot(DMachineClips[requests[i]]);
        }
    }


    #endregion

    //void ConstructUIsurface()
    //{
    //    var UIrects = this.gameObject.GetComponentsInChildren<RectTransform>();
    //    float screenRatioX = canvas.pixelRect.size.x / 800;
    //    float screenRatioY = canvas.pixelRect.size.y / 450;

    //    UIsurface = new NativeArray<AABB>(UIrects.Length, Allocator.Persistent);
    //    for (int i = 0; i < UIsurface.Length; i++)
    //    {
    //        UIsurface[i] = new AABB
    //        {
    //            LowerBound = RectTransformUtility.WorldToScreenPoint(Camera.main, UIrects[i].position) - new Vector2((UIrects[i].rect.width * screenRatioX) / 2, ((UIrects[i].rect.height * screenRatioY) / 2)),
    //            UpperBound = RectTransformUtility.WorldToScreenPoint(Camera.main, UIrects[i].position) + new Vector2((UIrects[i].rect.width * screenRatioX) / 2, (UIrects[i].rect.height * screenRatioY) / 2),
    //        };
    //    }
    //}
    /// If I decide to bakes the rectangles into a polygon for opti.
    /*
    private NativeArray<Vector2> BakeUIsurface()
    {

    }
    private bool IsPointInPolygon(float2 point, NativeArray<Vector2> polygonVertices)
    {
        int n = polygonVertices.Length;
        bool inside = false;

        for (int i = 0, j = n - 1; i < n; j = i++)
        {
            float2 vi = polygonVertices[i];
            float2 vj = polygonVertices[j];

            if (((vi.y > point.y) != (vj.y > point.y)) &&
                (point.x < (vj.x - vi.x) * (point.y - vi.y) / (vj.y - vi.y) + vi.x))
            {
                inside = !inside;
            }
        }

        return inside;
    }
    */
}
