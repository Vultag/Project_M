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
    SliderMono simplexSlider;

    public Transform toolTip;

    private EntityManager entityManager;
    //private EntityQuery Player_query;

    [HideInInspector]
    public Canvas canvas;
    private NativeArray<AABB> UIsurface;

    [SerializeField]
    private int MaxSynthNum;
    //[SerializeField]
    public GameObject SynthToolBar;
    int NumOfSynths = 1;
    int activeSynthIdx = 0;
    RectTransform SynthUIadd_rect;
    /// index, IsRecording
    List<(short,bool)> synthsInfo;

    private Vector2 PreviousMousePos;


    private void Awake()
    {

    }

    void Start()
    {

        var world = World.DefaultGameObjectInjectionWorld;
        entityManager = world.EntityManager;
        //Player_query = entityManager.CreateEntityQuery(typeof(PlayerData));

        synthsInfo = new List<(short, bool)>();

        canvas = this.gameObject.transform.parent.GetComponent<Canvas>();

        ConstructUIsurface();
        //NumOfSynths = SynthToolBar.GetComponentsInChildren<Button>().Length-1;
        SynthUIadd_rect = SynthToolBar.transform.GetChild(SynthToolBar.transform.childCount-1).GetComponent<RectTransform>();


    }


    void Update()
    {

        var mousePos = InputManager.mousePos;
        //InputManager.mouseDelta = mousePos - PreviousMousePos;
        //Debug.Log(mouseDelta);
        PreviousMousePos = mousePos;

        UIInputSystem.MouseOverUI = false;
        for (int i = 0;i < UIsurface.Length;i++)
        {
            if(PhysicsUtilities.PointInsideShape(mousePos, UIsurface[i]))
            {
                UIInputSystem.MouseOverUI = true;
                break;
            }
        }
        /// OPTI
        for (int i = 0; i < synthsInfo.Count; i++)
        {
            Slider slider = SynthToolBar.transform.GetChild(synthsInfo[i].Item1).gameObject.GetComponentInChildren<Slider>();
            slider.value += Time.deltaTime;
            if(slider.value + Time.deltaTime> slider.maxValue)
            {
                /// Recording
                if(synthsInfo[i].Item2)
                {
                    slider.value = 0;
                    synthsInfo.RemoveAt(i);
                    /// activate Play and Rec button GB
                    SynthToolBar.transform.GetChild(activeSynthIdx).GetChild(2).GetChild(1).gameObject.SetActive(true);
                    SynthToolBar.transform.GetChild(activeSynthIdx).GetChild(2).GetChild(0).gameObject.SetActive(true);
                    /// Deactivate Stop button GB
                    SynthToolBar.transform.GetChild(activeSynthIdx).GetChild(2).GetChild(2).gameObject.SetActive(false);
                }
                /// Playing
                else
                {
                    slider.value = (slider.value + Time.deltaTime - slider.maxValue);
                }
            }
        }

    }

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

    public void _SelectSynthUI(int index)
    {
        if (index == activeSynthIdx)
            return;

        AudioLayoutStorage.activeSynthIdx = index;

        Entity weapon_entity = audioManager.ActiveWeapon_query.GetSingletonEntity();

        if (entityManager.HasComponent<PlaybackRecordingData>(weapon_entity))
            _StopPlayback(activeSynthIdx);

        /// activate Rec button GB for new synth and deactivate for the old one
        SynthToolBar.transform.GetChild(index).GetChild(2).GetChild(0).gameObject.SetActive(true);
        SynthToolBar.transform.GetChild(activeSynthIdx).GetChild(2).GetChild(0).gameObject.SetActive(false);

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
        _StopPlayback(synthIdx);

        var slider = SynthToolBar.transform.GetChild(activeSynthIdx).gameObject.GetComponentInChildren<Slider>();
        ///change slider background color
        slider.gameObject.transform.GetChild(0).GetComponent<Image>().color = Color.grey;

        AudioLayoutStorageHolder.audioLayoutStorage.ActiveMusicSheet.ElementsInMesure.Dispose();
        AudioLayoutStorageHolder.audioLayoutStorage.ActiveMusicSheet.NoteElements.Dispose();
        AudioLayoutStorageHolder.audioLayoutStorage.ActiveMusicSheet.NotesSpriteIdx.Dispose();
        AudioLayoutStorageHolder.audioLayoutStorage.ActiveMusicSheet.NotesHeight.Dispose();
        AudioLayoutStorageHolder.audioLayoutStorage.ActiveMusicSheet = MusicSheetData.CreateDefault();
    }

    public void _RecordPlayback(int startingBeat)
    {

        var slider = SynthToolBar.transform.GetChild(activeSynthIdx).gameObject.GetComponentInChildren<Slider>();
        //var newColor = new Color { a = 1, r = (207 / 256), g = (207 / 256), b = (207 / 256) };
        slider.value = 0;
        /// PUT RECORDING LENGHT FIX
        slider.maxValue = audioManager.TEMPplaybackDuration;
        ///change slider background color
        slider.gameObject.transform.GetChild(0).GetComponent<Image>().color = Color.white;
        ///change slider color
        slider.gameObject.transform.GetChild(1).GetChild(0).GetComponent<Image>().color = Color.red;
        synthsInfo.Add(((short)activeSynthIdx,true));


        var ecb = audioManager.endSimulationECBSystem.CreateCommandBuffer();

        Entity weapon_entity = audioManager.ActiveWeapon_query.GetSingletonEntity();

        /// If the the Current playback of the synth is playing, stop it
        for (int i = 1; i < AudioManager.audioGenerator.activeSynthsIdx.Length; i++)
        {
            if (AudioManager.audioGenerator.activeSynthsIdx[i] == activeSynthIdx)
            {
                _StopPlayback(activeSynthIdx);
            }
        }

        ///// Deactivate Rec button GB
        //SynthToolBar.transform.GetChild(activeSynthIdx).GetChild(2).GetChild(0).gameObject.SetActive(false);
        ///// Deactivate Play button GB
        //SynthToolBar.transform.GetChild(activeSynthIdx).GetChild(2).GetChild(1).gameObject.SetActive(false);
        ///// Activate Stop button GB
        //SynthToolBar.transform.GetChild(activeSynthIdx).GetChild(2).GetChild(2).gameObject.SetActive(true);


        if (!entityManager.HasComponent<PlaybackRecordingData>(weapon_entity))
        {
            var playbackRecordingData = new PlaybackRecordingData {
                duration = audioManager.TEMPplaybackDuration,
                synthIndex = activeSynthIdx,
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
            entityManager.GetBuffer<PlaybackSustainedKeyBufferData>(weapon_entity).Clear();
            entityManager.GetBuffer<PlaybackReleasedKeyBufferData>(weapon_entity).Clear();
        }
    }
    public void _ActivatePlayback(int synthIdx)
    {

        /// Prevent play on empty playback
        if (AudioManager.audioGenerator.PlaybackAudioBundles[synthIdx].PlaybackDuration == 0)
            return;

        /// Prevent from having a synth with more than 1 playback
        for (int i = 1; i < AudioManager.audioGenerator.activeSynthsIdx.Length; i++)
        {
            if (AudioManager.audioGenerator.activeSynthsIdx[i] == synthIdx)
            {
                return;
            }
        }


        /// Deactivate Play button GB
        SynthToolBar.transform.GetChild(synthIdx).GetChild(2).GetChild(1).gameObject.SetActive(false);
        /// Activate Stop button GB
        SynthToolBar.transform.GetChild(synthIdx).GetChild(2).GetChild(2).gameObject.SetActive(true);

        var slider = SynthToolBar.transform.GetChild(synthIdx).gameObject.GetComponentInChildren<Slider>();
        slider.value = 0;
        /// PUT RECORDING LENGHT FIX
        //slider.maxValue = audioManager.TEMPplaybackDuration;
        ///change slider color
        slider.gameObject.transform.GetChild(1).GetChild(0).GetComponent<Image>().color = Color.green;
        synthsInfo.Add(((short)synthIdx, false));

        AudioLayoutStorageHolder.audioLayoutStorage.WriteActivation(synthIdx, true);

        Entity weapon_entity = WeaponSystem.WeaponEntities[synthIdx];

        var ecb = audioManager.endSimulationECBSystem.CreateCommandBuffer();

        PlaybackData playbackData = new PlaybackData { PlaybackIndex = synthIdx};

        ecb.AddComponent<PlaybackData>(weapon_entity, playbackData);

        if (!entityManager.HasBuffer<PlaybackSustainedKeyBufferData>(weapon_entity))
        {
            ecb.AddBuffer<PlaybackSustainedKeyBufferData>(weapon_entity);
            ecb.AddBuffer<PlaybackReleasedKeyBufferData>(weapon_entity);
        }

    }
    public void _StopPlayback(int synthIdx)
    {
        ///prevent activation if no playback playing

        Entity weapon_entity = WeaponSystem.WeaponEntities[synthIdx];

        var ecb = audioManager.endSimulationECBSystem.CreateCommandBuffer();

        /// The playback is curently recording 
        if (entityManager.HasComponent<PlaybackRecordingData>(weapon_entity))
        {
            Debug.LogError("stop on record. DISABLED UNTIL REASSESSMENT");
            return;
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
        /// The playback is running
        else
        {
            //Debug.Log("stop on playback");

            for (int i = 0; i < synthsInfo.Count; i++)
            {
                if (synthsInfo[i].Item1 == synthIdx)
                {
                    synthsInfo.RemoveAt(i);
                    break;
                }
            }

            /// Activate Rec button GB
            SynthToolBar.transform.GetChild(activeSynthIdx).GetChild(2).GetChild(0).gameObject.SetActive(true);

            AudioLayoutStorageHolder.audioLayoutStorage.WriteActivation(synthIdx, false);

            ecb.RemoveComponent<PlaybackData>(weapon_entity);

        }


        /// Activate Play button GB
        SynthToolBar.transform.GetChild(synthIdx).GetChild(2).GetChild(1).gameObject.SetActive(true);
        /// Deactivate Stop button GB
        SynthToolBar.transform.GetChild(synthIdx).GetChild(2).GetChild(2).gameObject.SetActive(false);

        var slider = SynthToolBar.transform.GetChild(synthIdx).gameObject.GetComponentInChildren<Slider>();
        slider.value = 0;

        if (entityManager.HasBuffer<PlaybackSustainedKeyBufferData>(weapon_entity))
        {
            ecb.RemoveComponent<PlaybackSustainedKeyBufferData>(weapon_entity);
            ecb.RemoveComponent<PlaybackReleasedKeyBufferData>(weapon_entity);
        }

   
     
    }


    void ConstructUIsurface()
    {
        var UIrects = this.gameObject.GetComponentsInChildren<RectTransform>();
        float screenRatioX = canvas.pixelRect.size.x / 800;
        float screenRatioY = canvas.pixelRect.size.y / 450;

        UIsurface = new NativeArray<AABB>(UIrects.Length, Allocator.Persistent);
        for (int i = 0; i < UIsurface.Length; i++)
        {
            UIsurface[i] = new AABB
            {
                LowerBound = RectTransformUtility.WorldToScreenPoint(Camera.main, UIrects[i].position) - new Vector2((UIrects[i].rect.width * screenRatioX) / 2, ((UIrects[i].rect.height * screenRatioY) / 2)),
                UpperBound = RectTransformUtility.WorldToScreenPoint(Camera.main, UIrects[i].position) + new Vector2((UIrects[i].rect.width * screenRatioX) / 2, (UIrects[i].rect.height * screenRatioY) / 2),
            };
        }
    }
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
