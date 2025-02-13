using System;
using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using NUnit.Framework.Internal;
using TMPro;
using Unity.Collections;
using Unity.Entities;
using Unity.Entities.UniversalDelegates;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using UnityEngine.UIElements;
using static UnityEngine.EventSystems.EventTrigger;
using Image = UnityEngine.UI.Image;
using Slider = UnityEngine.UI.Slider;
using MusicNamespace;
using UnityEngine.EventSystems;
using System.Linq;
using UnityEngine.Rendering;

/*
 Break on window dimention change --> TO FIX
 */


public class UIManager : MonoBehaviour
{


    [SerializeField]
    private AudioManager audioManager;
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
    SliderMono simplexSlider;

    public Transform toolTip;

    private EntityManager entityManager;
    //private EntityQuery Player_query;

    [HideInInspector]
    public Canvas canvas;
    //private NativeArray<AABB> UIsurface;

    [SerializeField]
    private int MaxSynthNum;
    //[SerializeField]
    public GameObject SynthToolBar;
    int NumOfSynths = 1;
    int activeSynthIdx = 0;
    RectTransform SynthUIadd_rect;
    /// index ; isPlaying ; is recording
    List<(short,bool,bool)> activeUISynths;
    //Dictionary<int> synthsInfo = new();

    private Vector2 PreviousMousePos;


    private void Awake()
    {

    }

    void Start()
    {

        var world = World.DefaultGameObjectInjectionWorld;
        entityManager = world.EntityManager;
        //Player_query = entityManager.CreateEntityQuery(typeof(PlayerData));

        activeUISynths = new List<(short, bool, bool)>();

        canvas = this.gameObject.transform.parent.GetComponent<Canvas>();

        ///ConstructUIsurface();
        
        //NumOfSynths = SynthToolBar.GetComponentsInChildren<Button>().Length-1;
        SynthUIadd_rect = SynthToolBar.transform.GetChild(SynthToolBar.transform.childCount-1).GetComponent<RectTransform>();


    }


    void Update()
    {

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
        for (int i = 0; i < activeUISynths.Count; i++)
        {
            //Debug.Log(activeUISynths[i]);
            Slider slider = SynthToolBar.transform.GetChild(activeUISynths[i].Item1).gameObject.GetComponentInChildren<Slider>();
            slider.value += Time.deltaTime;
         
            //if(slider.value + Time.deltaTime> slider.maxValue)
            //{
            //    /// Recording
            //    if(synthsInfo[i].Item2)
            //    {
            //        slider.value = 0;
            //        synthsInfo.RemoveAt(i);
            //        /// activate Play and Rec button GB
            //        SynthToolBar.transform.GetChild(activeSynthIdx).GetChild(2).GetChild(1).gameObject.SetActive(true);
            //        SynthToolBar.transform.GetChild(activeSynthIdx).GetChild(2).GetChild(0).gameObject.SetActive(true);
            //        /// Deactivate Stop button GB
            //        SynthToolBar.transform.GetChild(activeSynthIdx).GetChild(2).GetChild(2).gameObject.SetActive(false);
            //    }
            //    /// Playing
            //    else
            //    {
            //        slider.value = (slider.value + Time.deltaTime - slider.maxValue);
            //    }
            //}
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


    public void _SelectSynthUI(int index)
    {
        Entity weapon_entity = audioManager.ActiveWeapon_query.GetSingletonEntity();
        /// prevent synth select if already selected or curently recording
        if (index == activeSynthIdx|| entityManager.HasComponent<PlaybackRecordingData>(weapon_entity))
            return;

        SynthToolBar.transform.GetChild(AudioLayoutStorage.activeSynthIdx).GetComponent<SynthUIelement>()._CancelRecord();
        //if(SynthToolBar.transform.GetChild(index).GetComponent<SynthUIelement>().coro)
        //{
        //    return;
        //}

        AudioLayoutStorage.activeSynthIdx = index;

        //if (entityManager.HasComponent<PlaybackRecordingData>(weapon_entity))
        //    _StopPlayback(activeSynthIdx);

        /// activate Rec button GB for new synth and deactivate for the old one
        SynthToolBar.transform.GetChild(index).GetChild(2).GetChild(0).gameObject.SetActive(true);
        SynthToolBar.transform.GetChild(activeSynthIdx).GetChild(2).GetChild(0).gameObject.SetActive(false);

        /// change synthUI color to distinguish
        SynthToolBar.transform.GetChild(activeSynthIdx).GetChild(0).GetComponent<Image>().color = Color.white;
        SynthToolBar.transform.GetChild(index).GetChild(0).GetComponent<Image>().color = Color.black;
        activeSynthIdx = index;

        audioManager.SelectSynth(index);

        UpdateSynthUI();
    }
    public void _AddSynthUI()
    {
        var synthUI = SynthToolBar.transform.GetChild(NumOfSynths).gameObject;
        synthUI.SetActive(true);
        synthUI.GetComponent<SynthUIelement>().thisSynthIdx = NumOfSynths;
        synthUI.GetComponentInChildren<TextMeshProUGUI>().text = "Synth " + (NumOfSynths+1);

        NumOfSynths++;
        SynthUIadd_rect.position = SynthToolBar.transform.GetChild(NumOfSynths).GetComponent<RectTransform>().position;

        audioManager.AddSynth(NumOfSynths);

        if (NumOfSynths == MaxSynthNum) { SynthUIadd_rect.gameObject.SetActive(false); }
    }
    void UpdateSynthUI()
    {
        var synthData = AudioManager.audioGenerator.SynthsData[activeSynthIdx];
        //Debug.Log(synthData.ADSR.Sustain);
        oscillatorUI.UpdateUI(synthData);
        volumeAdsrUI.UpdateUI(synthData);
        filterAdsrUI.UpdateUI(synthData);
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
    public void _RecordPlayback(int synthIdx,int startingBeat)
    {

        var slider = SynthToolBar.transform.GetChild(synthIdx).gameObject.GetComponentInChildren<Slider>();
        //var newColor = new Color { a = 1, r = (207 / 256), g = (207 / 256), b = (207 / 256) };
        slider.value = 0;
        /// PUT RECORDING LENGHT FIX
        slider.maxValue = audioManager.TEMPplaybackDuration;
        ///change slider background color
        slider.gameObject.transform.GetChild(0).GetComponent<Image>().color = Color.white;

        var synthInfo = activeUISynths.FirstOrDefault(item => item.Item1 == (short)synthIdx);
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
            activeUISynths.Add(((short)synthIdx, true,false));
        }


        var ecb = audioManager.endSimulationECBSystem.CreateCommandBuffer();

        Entity weapon_entity = audioManager.ActiveWeapon_query.GetSingletonEntity();

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


        if (!entityManager.HasComponent<PlaybackRecordingData>(weapon_entity))
        {
            //Debug.Log(synthIdx);
            var playbackRecordingData = new PlaybackRecordingData {
                duration = audioManager.TEMPplaybackDuration,
                synthIndex = synthIdx,
                startBeat = startingBeat,
                GideReferenceDirection = WeaponSystem.GideReferenceDirection
            };

            ecb.AddComponent<PlaybackRecordingData>(weapon_entity, playbackRecordingData);
            ecb.AddBuffer<PlaybackRecordingKeysBuffer>(weapon_entity);
          
            PlaybackRecordSystem.ClickPressed = false;
            PlaybackRecordSystem.ClickReleased = false;
        }
        if (entityManager.HasBuffer<PlaybackSustainedKeyBufferData>(weapon_entity))
        {
            Debug.Log("has entityManager.GetBuffer<PlaybackReleasedKeyBufferData");
            //entityManager.GetBuffer<PlaybackSustainedKeyBufferData>(weapon_entity).Clear();
            //entityManager.GetBuffer<PlaybackReleasedKeyBufferData>(weapon_entity).Clear();
        }
    }
    public void _ActivatePlayback(int2 PBidx)
    {

        /// Prevent play on empty playback
        //if (AudioManager.audioGenerator.PlaybackAudioBundles[synthIdx].PlaybackDuration == 0)
        //    return;

     


        AudioLayoutStorageHolder.audioLayoutStorage.WritePlayback(
            UIplaybacksHolder.synthFullBundleLists[PBidx.x][PBidx.y].playbackAudioBundle,
            PBidx.x);

        var slider = SynthToolBar.transform.GetChild(PBidx.x).gameObject.GetComponentInChildren<Slider>();
        slider.value = 0;
        /// PUT RECORDING LENGHT FIX
        //slider.maxValue = audioManager.TEMPplaybackDuration;
        ///change slider background color
        slider.gameObject.transform.GetChild(0).GetComponent<Image>().color = Color.white;

        var synthInfo = activeUISynths.FirstOrDefault(item => item.Item1 == (short)PBidx.x);
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
            activeUISynths.Add(((short)PBidx.x,false,true));
        }


        /// Prevent from having a synth with more than 1 playback
        for (int i = 1; i < AudioManager.audioGenerator.activeSynthsIdx.Length; i++)
        {
            if (AudioManager.audioGenerator.activeSynthsIdx[i] == PBidx.x)
            {
                //Debug.Log("synth aleady activated");
                return;
            }
        }

        ///// Deactivate Play button GB
        //SynthToolBar.transform.GetChild(synthIdx).GetChild(2).GetChild(1).gameObject.SetActive(false);
        ///// Activate Stop button GB
        //SynthToolBar.transform.GetChild(synthIdx).GetChild(2).GetChild(2).gameObject.SetActive(true);


        AudioLayoutStorageHolder.audioLayoutStorage.WriteActivation(PBidx.x);

        Entity weapon_entity = WeaponSystem.WeaponEntities[PBidx.x];

        var ecb = audioManager.endSimulationECBSystem.CreateCommandBuffer();

        PlaybackData playbackData = new PlaybackData { SynthIndex = PBidx.x };

        ecb.AddComponent<PlaybackData>(weapon_entity, playbackData);
        //Debug.LogWarning(0);

        if (!entityManager.HasBuffer<PlaybackSustainedKeyBufferData>(weapon_entity))
        {
            //Debug.LogWarning(1);
            ecb.AddBuffer<PlaybackSustainedKeyBufferData>(weapon_entity);
            ecb.AddBuffer<PlaybackReleasedKeyBufferData>(weapon_entity);
        }

    }
    public void _StopPlayback(int synthIdx)
    {
        ///prevent activation if no playback playing

        Entity weapon_entity = WeaponSystem.WeaponEntities[synthIdx];

        var ecb = audioManager.endSimulationECBSystem.CreateCommandBuffer();

        _SetSynthUItoSleep(synthIdx);

        /// SAFETY
        bool isRunning = false;
        /// If the the synthIdx is playing, stop it
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
        //SynthToolBar.transform.GetChild(synthIdx).GetChild(2).GetChild(0).gameObject.SetActive(true);

        ecb.RemoveComponent<PlaybackData>(weapon_entity);


        /// The playback is curently recording 
        //if (entityManager.HasComponent<PlaybackRecordingData>(weapon_entity))
        {
            ///Debug.LogError("stop on record. DISABLED UNTIL REASSESSMENT");
            //return;
            /*
            PlaybackRecordingData runningPlaybackData = new PlaybackRecordingData();
            runningPlaybackData = entityManager.GetComponentData<PlaybackRecordingData>(weapon_entity);

            slider.maxValue = (float)(MusicUtils.time)-runningPlaybackData.startBeat;
            ///change slider color
            slider.gameObject.transform.GetChild(1).GetChild(0).GetComponent<Image>().color = Color.green;
            for (int i = 0; i < synthsInfo.Count; i++)
            {
                if (synthsInfo[i].Item1 == synthIdx)
                {
                    synthsInfo.RemoveAt(i);
                    break;
                }
            }



            var keyBuffer = entityManager.GetBuffer<PlaybackRecordingKeysBuffer>(weapon_entity);

            var playbackKeys = new NativeArray<PlaybackKey>(keyBuffer.Length, Allocator.Persistent);
            playbackKeys.CopyFrom(keyBuffer.AsNativeArray().Reinterpret<PlaybackKey>());

            AudioLayoutStorageHolder.audioLayoutStorage.WritePlayback(new PlaybackAudioBundle
            {
                IsLooping = true,
                //IsPlaying = false,
                PlaybackDuration =  runningPlaybackData.time,
                PlaybackKeys = playbackKeys
            },synthIdx);


            ecb.RemoveComponent<PlaybackRecordingKeysBuffer>(weapon_entity);
            ecb.RemoveComponent<PlaybackRecordingData>(weapon_entity);
            */
        }


        /// Activate Play button GB
        //SynthToolBar.transform.GetChild(synthIdx).GetChild(2).GetChild(1).gameObject.SetActive(true);
        /// Deactivate Stop button GB
        //SynthToolBar.transform.GetChild(synthIdx).GetChild(2).GetChild(2).gameObject.SetActive(false);

        if (entityManager.HasBuffer<PlaybackSustainedKeyBufferData>(weapon_entity))
        {
            ecb.RemoveComponent<PlaybackSustainedKeyBufferData>(weapon_entity);
            ecb.RemoveComponent<PlaybackReleasedKeyBufferData>(weapon_entity);
        }

   
     
    }

    public void _SetSynthUItoSleep(int synthIdx)
    {
        var slider = SynthToolBar.transform.GetChild(synthIdx).gameObject.GetComponentInChildren<Slider>();
        var synthInfo = activeUISynths.FirstOrDefault(item => item.Item1 == (short)synthIdx);

            if (!synthInfo.Equals(default((short, bool, bool))))
            {
                //Debug.Log("remove");
                activeUISynths.Remove(synthInfo);
            }
        
        ///change slider background color
        slider.gameObject.transform.GetChild(0).GetComponent<Image>().color = Color.grey;
        slider.value = 0;
        /// reactivate rec button
        SynthToolBar.transform.GetChild(activeSynthIdx).GetChild(2).GetChild(0).gameObject.SetActive(true);
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
