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
using UnityEngine.Rendering;

/*
 Break on window dimention change --> TO FIX
 */

public struct FullEquipmentIdx
{    
    /// <summary>
    /// equipment index 
    /// </summary>
    public ushort absoluteIdx;
    /// <summary>
    /// equipment index relative to category : synth/drumMachine/...
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
    private OscillatorUI oscillatorUI;
    [SerializeField]
    private ADSRUI volumeAdsrUI;
    [SerializeField]
    private ADSRUI filterAdsrUI;
    [SerializeField]
    private FilterUI filterUI;
    [SerializeField]
    private UnissonUI unissonUI;
    [SerializeField]
    private VoicesUI voicesUI;

    [SerializeField]
    private UIPlaybacksHolder UIplaybacksHolder;
    public GameObject MusicSheetGB;
    public GameObject MusicTrackGB;

    [SerializeField]
    private GameObject KeyboardShaderGB;
    [SerializeField]
    private GameObject DrumPadShaderGB;
    [SerializeField]
    private DrumPadVFX drumPadVFX;

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
    public GameObject SynthToolBar;

    [HideInInspector]
    public ushort NumOfEquipments = 0;
    public short NumOfDMachines = 0;
    public short NumOfSynths = 0;
    /// Need local activeSynthIdx to modify previous activeSynth upon change
    int activeUISynthIdx = -1;
    /// Same
    int activeUIDrumMachineIdx = -1;
    RectTransform SynthUIadd_rect;
    /// index ; isPlaying ; is recording
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

        //NumOfSynths = SynthToolBar.GetComponentsInChildren<Button>().Length-1;
        SynthUIadd_rect = SynthToolBar.transform.GetChild(SynthToolBar.transform.childCount-1).GetComponent<RectTransform>();


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
            Slider slider = SynthToolBar.transform.GetChild(activeUIEquipment[i].Item1).gameObject.GetComponentInChildren<Slider>();
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
        SynthToolBar.transform.GetChild(index).GetChild(0).GetComponent<Image>().color = Color.black;
        SynthToolBar.transform.GetChild(index).GetChild(2).GetChild(0).gameObject.SetActive(true);
    }

    public void _SelectSynthUI(ushort equipmentIndex)
    {
        //var synthIdx = EquipmentIdxToSynthDataIdx[equipmentIndex];
        SynthEditPanel.SetActive(true);
        short synthDataIdx = EquipmentIdxToSynthDataIdx[equipmentIndex];
        SynthData selectedSynthData = AudioLayoutStorageHolder.audioLayoutStorage.AuxillarySynthsData[synthDataIdx];
        UpdateSynthUI(
            in selectedSynthData,
            entityManager.GetComponentData<WeaponData>(AudioManager.AuxillaryEquipmentEntities[equipmentIndex]).weaponType
            );

        if (!audioManager.ControlledEquipment_query.IsEmpty)
        {
            Entity weapon_entity = audioManager.ControlledEquipment_query.GetSingletonEntity();
            /// prevent synth select if already selected or curently recording
            if (equipmentIndex == activeUISynthIdx || entityManager.HasComponent<SynthPlaybackRecordingData>(weapon_entity))
            { return;}
                

        }

        KeyboardShaderGB.SetActive(true);
        /// activate Rec button GB for new synth and deactivate for the old one
        if (activeUISynthIdx != -1)
        {
            SynthToolBar.transform.GetChild(activeUISynthIdx).GetChild(2).GetChild(0).gameObject.SetActive(false);
            SynthToolBar.transform.GetChild(activeUISynthIdx).GetChild(0).GetComponent<Image>().color = Color.white;
            SynthToolBar.transform.GetChild(activeUISynthIdx).GetComponent<EquipmentUIelement>()._CancelRecord();
            MusicSheetGB.SetActive(false);
        }
        else if (activeUIDrumMachineIdx != -1)
        {
            SynthToolBar.transform.GetChild(activeUIDrumMachineIdx).GetChild(2).GetChild(0).gameObject.SetActive(false);
            SynthToolBar.transform.GetChild(activeUIDrumMachineIdx).GetChild(0).GetComponent<Image>().color = Color.white;
            DrumPadShaderGB.SetActive(false);
        }
        /// change synthUI color to distinguish
        SynthToolBar.transform.GetChild(equipmentIndex).GetChild(0).GetComponent<Image>().color = Color.black;
        SynthToolBar.transform.GetChild(equipmentIndex).GetChild(2).GetChild(0).gameObject.SetActive(true);

        AudioLayoutStorage.activeSynthIdx = synthDataIdx;

        activeUISynthIdx = equipmentIndex;
        activeUIDrumMachineIdx = -1;

        audioManager.SelectSynth(equipmentIndex, synthDataIdx);

    }
    public void _SelectMachineDrumUI(int index)
    {
        DrumPadShaderGB.SetActive(true);
        SynthEditPanel.SetActive(false);
        /// activate Rec button GB for new synth and deactivate for the old one
        if (activeUISynthIdx != -1)
        {
            SynthToolBar.transform.GetChild(activeUISynthIdx).GetChild(2).GetChild(0).gameObject.SetActive(false);
            SynthToolBar.transform.GetChild(activeUISynthIdx).GetChild(0).GetComponent<Image>().color = Color.white;
            SynthToolBar.transform.GetChild(AudioLayoutStorage.activeSynthIdx).GetComponent<EquipmentUIelement>()._CancelRecord();
            KeyboardShaderGB.SetActive(false);
        }
        else if (activeUIDrumMachineIdx != -1)
        {
            SynthToolBar.transform.GetChild(activeUIDrumMachineIdx).GetChild(2).GetChild(0).gameObject.SetActive(false);
            SynthToolBar.transform.GetChild(activeUIDrumMachineIdx).GetChild(0).GetComponent<Image>().color = Color.white;
        }
        AudioLayoutStorage.activeSynthIdx = -1;
        /// change synthUI color to distinguish
        SynthToolBar.transform.GetChild(index).GetChild(0).GetComponent<Image>().color = Color.black;
        /// Dont activate record button yet bc no Dmachine playback system yet -> TO
        //SynthToolBar.transform.GetChild(index).GetChild(2).GetChild(0).gameObject.SetActive(true);
        activeUISynthIdx = -1;
        activeUIDrumMachineIdx = index;

        audioManager.SelectDrumMachine(index);
    }
    public void _AddSynthUI(WeaponClass weaponClass, WeaponType weaponType)
    {
        var synthUI = SynthToolBar.transform.GetChild(NumOfEquipments).gameObject;
        synthUI.SetActive(true);
        synthUI.GetComponent<EquipmentUIelement>().thisEquipmentIdx = NumOfEquipments;
        synthUI.GetComponentInChildren<TextMeshProUGUI>().text = "Synth " + (NumOfSynths+1);

        NumOfSynths++;
        NumOfEquipments++;
        SynthToolBar.transform.GetChild(NumOfEquipments-1).GetComponent<EquipmentUIelement>().thisEquipmentCategory = EquipmentCategory.Weapon;
        EquipmentIdxToSynthDataIdx.Add((short)(NumOfSynths-1));

        SynthData newSynthData = SynthData.CreateDefault(weaponType);

        audioManager.AddSynth(NumOfEquipments,newSynthData, weaponClass,weaponType);

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
        var synthUI = SynthToolBar.transform.GetChild(NumOfEquipments).gameObject;
        synthUI.SetActive(true);
        synthUI.GetComponent<EquipmentUIelement>().thisEquipmentIdx = NumOfEquipments;
        synthUI.GetComponentInChildren<TextMeshProUGUI>().text = "Drum Machine " + (NumOfDMachines + 1);

        NumOfDMachines++;
        NumOfEquipments++;
        SynthToolBar.transform.GetChild(NumOfEquipments-1).GetComponent<EquipmentUIelement>().thisEquipmentCategory = EquipmentCategory.DrumMachine;
        /// add invalid index for padding
        EquipmentIdxToSynthDataIdx.Add(-1);

        //SynthData newSynthData = SynthData.CreateDefault(weaponType);

        audioManager.AddDrumMachine(NumOfEquipments);

        //if (NumOfEquipments == 1)
        //{
        //    _SelectMachineDrumUI(0);

        //    //UpdateSynthUI(in newSynthData, weaponType);

        //    //SynthEditPanel.SetActive(true);
        //}

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
    public void _ResetPlayback(int synthIdx)
    {
        /// OPTI
        /// If the the Current playback of the synth is playing, stop it
        //for (int i = 1; i < AudioManager.audioGenerator.activeSynthsIdx.Length; i++)
        //{
        //    if (AudioManager.audioGenerator.activeSynthsIdx[i] == activeSynthIdx)
        //    {
        //        _StopPlayback(activeSynthIdx);
        //    }
        //}

        var slider = SynthToolBar.transform.GetChild(synthIdx).gameObject.GetComponentInChildren<Slider>();
        ///change slider background color
        slider.gameObject.transform.GetChild(0).GetComponent<Image>().color = Color.grey;

        MusicSheetGB.SetActive(true);

        /// dont dispose as the memory responsability is passed to the playbackContainer
        ///AudioLayoutStorageHolder.audioLayoutStorage.ActiveMusicSheet._Dispose();
        AudioLayoutStorageHolder.audioLayoutStorage.ActiveMusicSheet = MusicSheetData.CreateDefault();
    }
    public void _RecordPlayback(ushort equipmentIdx,int startingBeat)
    {

        ushort synthIdx = (ushort)EquipmentIdxToSynthDataIdx[equipmentIdx];

        var slider = SynthToolBar.transform.GetChild(equipmentIdx).gameObject.GetComponentInChildren<Slider>();
        //var newColor = new Color { a = 1, r = (207 / 256), g = (207 / 256), b = (207 / 256) };
        slider.value = 0;
        /// PUT RECORDING LENGHT FIX
        slider.maxValue = audioManager.TEMPplaybackDuration;
        ///change slider background color
        slider.gameObject.transform.GetChild(0).GetComponent<Image>().color = Color.white;

        var synthInfo = activeUIEquipment.FirstOrDefault(item => item.Item1 == (short)equipmentIdx);
        bool synthAlreadyActive = !synthInfo.Equals(default((short, bool, bool)));

        if (synthAlreadyActive)
        {
            /// both playing and recording in mesure
            if (synthInfo.Item3 == true) slider.gameObject.transform.GetChild(1).GetChild(0).GetComponent<Image>().color = Color.yellow;

        }
        else
        {
            ///change slider color
            slider.gameObject.transform.GetChild(1).GetChild(0).GetComponent<Image>().color = Color.red;
            activeUIEquipment.Add(((short)equipmentIdx, true,false));
        }


        var ecb = audioManager.beginSimulationECBSystem.CreateCommandBuffer();

        Entity weapon_entity = audioManager.ControlledEquipment_query.GetSingletonEntity();

        /// If the the Current playback of the synth is playing, stop it
        //for (int i = 1; i < AudioManager.audioGenerator.activeSynthsIdx.Length; i++)
        //{
        //    if (AudioManager.audioGenerator.activeSynthsIdx[i] == activeSynthIdx)
        //    {
        //        Debug.Log("here");
        //        _StopPlayback(activeSynthIdx);
        //    }
        //}

        ///// Deactivate Play button GB
        //SynthToolBar.transform.GetChild(activeSynthIdx).GetChild(2).GetChild(1).gameObject.SetActive(false);
        ///// Activate Stop button GB
        //SynthToolBar.transform.GetChild(activeSynthIdx).GetChild(2).GetChild(2).gameObject.SetActive(true);


        if (!entityManager.HasComponent<SynthPlaybackRecordingData>(weapon_entity))
        {
            //Debug.Log(equipmentIdx);
            var playbackRecordingData = new SynthPlaybackRecordingData {
                duration = audioManager.TEMPplaybackDuration,
                fEquipmentIdx = new FullEquipmentIdx { absoluteIdx = equipmentIdx, relativeIdx = synthIdx } ,
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


    /// <summary>
    /// OPTI : PASS ECB INSTEAD OF CREATING ONE EVERY TIME
    /// </summary>
    /// <param name="PBidx"></param>
    public void _ActivatePlayback(int2 PBidx)
    {
        ushort relativeEquipmentIdx = (ushort)EquipmentIdxToSynthDataIdx[PBidx.x];

        /// unessesary if same playback recording being reset ?
        AudioLayoutStorageHolder.audioLayoutStorage.WritePlayback(
            UIplaybacksHolder.synthFullBundleLists[relativeEquipmentIdx][PBidx.y].playbackAudioBundle,
            relativeEquipmentIdx);

        var slider = SynthToolBar.transform.GetChild(PBidx.x).gameObject.GetComponentInChildren<Slider>();
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
            activeUIEquipment.Add(((short)PBidx.x,false,true));
        }

        Entity weapon_entity = AudioManager.AuxillaryEquipmentEntities[PBidx.x];

        var ecb = audioManager.beginSimulationECBSystem.CreateCommandBuffer();

        if (entityManager.HasComponent<PlaybackData>(weapon_entity))
        {
            AudioLayoutStorageHolder.audioLayoutStorage.PlaybackContextResetRequired.Enqueue(relativeEquipmentIdx);
            ecb.SetComponent<PlaybackData>(weapon_entity, new PlaybackData { PlaybackIndex = relativeEquipmentIdx });
            entityManager.GetBuffer<PlaybackSustainedKeyBufferData>(weapon_entity).Clear();
            entityManager.GetBuffer<PlaybackReleasedKeyBufferData>(weapon_entity).Clear();
        }
        else
        {

            PlaybackData playbackData = new PlaybackData { PlaybackIndex = relativeEquipmentIdx };

            ecb.AddComponent<PlaybackData>(weapon_entity, playbackData);

            AudioLayoutStorageHolder.audioLayoutStorage.WriteActivation(relativeEquipmentIdx);
        }


    }
    /// <summary>
    /// OPTI : PASS ECB INSTEAD OF CREATING ONE EVERY TIME
    /// </summary>
    public void _StopPlayback(int equipmentIdx)
    {
        ushort synthIdx = (ushort)EquipmentIdxToSynthDataIdx[equipmentIdx];

        ///prevent activation if no playback playing
        //var weaponIdx = equipmentIdx + 1;
        Entity weapon_entity = AudioManager.AuxillaryEquipmentEntities[equipmentIdx];

        var ecb = audioManager.beginSimulationECBSystem.CreateCommandBuffer();

        _SetSynthUItoSleep(equipmentIdx);

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
        //SynthToolBar.transform.GetChild(equipmentIdx).GetChild(2).GetChild(0).gameObject.SetActive(true);

        /// reset rotation to default
        LocalTransform newLocalTransform = entityManager.GetComponentData<LocalTransform>(weapon_entity);
        newLocalTransform.Rotation = Quaternion.Euler(0, 0, newLocalTransform.Position.x < 0 ? -45 : 225);
        ecb.SetComponent<LocalTransform>(weapon_entity, newLocalTransform);

        ecb.RemoveComponent<PlaybackData>(weapon_entity);


        /// Activate Play button GB
        //SynthToolBar.transform.GetChild(equipmentIdx).GetChild(2).GetChild(1).gameObject.SetActive(true);
        /// Deactivate Stop button GB
        //SynthToolBar.transform.GetChild(equipmentIdx).GetChild(2).GetChild(2).gameObject.SetActive(false);

        //if (entityManager.HasBuffer<PlaybackSustainedKeyBufferData>(weapon_entity))
        //{
        //    ecb.RemoveComponent<PlaybackSustainedKeyBufferData>(weapon_entity);
        //    ecb.RemoveComponent<PlaybackReleasedKeyBufferData>(weapon_entity);
        //}

   
        
    }

    public void _SetSynthUItoSleep(int equipmentIdx)
    {
        var slider = SynthToolBar.transform.GetChild(equipmentIdx).gameObject.GetComponentInChildren<Slider>();
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
            switch(requests[i].Item1)
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
