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
using Image = UnityEngine.UI.Image;
using Slider = UnityEngine.UI.Slider;

/*
 Break on window dimention change --> TO FIX
 */


public class UIManager : MonoBehaviour
{


    [SerializeField]
    private AudioManager audioManager;

    [SerializeField]
    SliderMono simplexSlider;

    private EntityManager entityManager;
    //private EntityQuery Player_query;

    public static PlayerControls playerControls;

    private Canvas canvas;
    private NativeArray<AABB> UIsurface;

    [SerializeField]
    private int MaxSynthNum;
    [SerializeField]
    private GameObject SynthToolBar;
    int NumOfSynths = 1;
    int activeSynthIdx = 0;
    RectTransform SynthUIadd_rect;
    /// index, IsRecording
    List<(short,bool)> activeSliders;


    private void Awake()
    {
        playerControls = new PlayerControls();
    }

    void Start()
    {

        var world = World.DefaultGameObjectInjectionWorld;
        entityManager = world.EntityManager;
        //Player_query = entityManager.CreateEntityQuery(typeof(PlayerData));

        activeSliders = new List<(short, bool)>();

        playerControls.Enable();
        canvas = this.gameObject.transform.parent.GetComponent<Canvas>();

        ConstructUIsurface();
        //NumOfSynths = SynthToolBar.GetComponentsInChildren<Button>().Length-1;
        SynthUIadd_rect = SynthToolBar.transform.GetChild(SynthToolBar.transform.childCount-1).GetComponent<RectTransform>();

    }


    void Update()
    {
        var mousePos = Mouse.current.position.ReadValue();

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
        for (int i = 0; i < activeSliders.Count; i++)
        {
            Slider slider = SynthToolBar.transform.GetChild(activeSliders[i].Item1).gameObject.GetComponentInChildren<Slider>();
            slider.value += Time.deltaTime;
            if(slider.value + Time.deltaTime> slider.maxValue)
            {
                /// Recording
                if(activeSliders[i].Item2)
                {
                    slider.value = 0;
                    activeSliders.RemoveAt(i);
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
        /// UPDATE SLIDERS

    }

    public void _SelectSynthUI(int index)
    {
        if (index == activeSynthIdx)
            return;

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

        simplexSlider.UpdateSlider(synthData.SinFactor, synthData.SquareFactor,synthData.SawFactor);

    }

    public void _RecordPlayback()
    {


        var slider = SynthToolBar.transform.GetChild(activeSynthIdx).gameObject.GetComponentInChildren<Slider>();
        //var newColor = new Color { a = 1, r = (207 / 256), g = (207 / 256), b = (207 / 256) };
        slider.value = 0;
        /// PUT RECORDING LENGHT FIX
        slider.maxValue = 2f;
        ///change slider background color
        slider.gameObject.transform.GetChild(0).GetComponent<Image>().color = Color.white;
        ///change slider color
        slider.gameObject.transform.GetChild(1).GetChild(0).GetComponent<Image>().color = Color.red;
        activeSliders.Add(((short)activeSynthIdx,true));


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

        /// Deactivate Rec button GB
        SynthToolBar.transform.GetChild(activeSynthIdx).GetChild(2).GetChild(0).gameObject.SetActive(false);
        /// Deactivate Play button GB
        SynthToolBar.transform.GetChild(activeSynthIdx).GetChild(2).GetChild(1).gameObject.SetActive(false);
        /// Activate Stop button GB
        SynthToolBar.transform.GetChild(activeSynthIdx).GetChild(2).GetChild(2).gameObject.SetActive(true);


        if (!entityManager.HasComponent<PlaybackRecordingData>(weapon_entity))
        {
            var playbackRecordingData = new PlaybackRecordingData{
                duration = 2,
                synthIndex = activeSynthIdx,
                time = 0
            };
            ecb.AddComponent<PlaybackRecordingData>(weapon_entity, playbackRecordingData);
            ecb.AddBuffer<PlaybackRecordingKeysBuffer>(weapon_entity);
            PlaybackRecordSystem.ClickPressed = false;
            PlaybackRecordSystem.ClickReleased = false;
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
        slider.maxValue = 2f;
        ///change slider color
        slider.gameObject.transform.GetChild(1).GetChild(0).GetComponent<Image>().color = Color.green;
        activeSliders.Add(((short)synthIdx, false));

        AudioManager.audioGenerator.audioLayoutStorage.WriteActivation(synthIdx, true);


    }
    public void _StopPlayback(int synthIdx)
    {
        ///prevent activation if no playback playing

        /// Activate Play button GB
        SynthToolBar.transform.GetChild(synthIdx).GetChild(2).GetChild(1).gameObject.SetActive(true);
        /// Deactivate Stop button GB
        SynthToolBar.transform.GetChild(synthIdx).GetChild(2).GetChild(2).gameObject.SetActive(false);

        var slider = SynthToolBar.transform.GetChild(synthIdx).gameObject.GetComponentInChildren<Slider>();
        slider.value = 0;

        Entity weapon_entity = audioManager.ActiveWeapon_query.GetSingletonEntity();
        /// The playback is curently recording 
        if (entityManager.HasComponent<PlaybackRecordingData>(weapon_entity))
        {
            //Debug.Log("stop on record");

            /// PUT RECORDING LENGHT FIX
            slider.maxValue = 2f;
            ///change slider color
            slider.gameObject.transform.GetChild(1).GetChild(0).GetComponent<Image>().color = Color.green;
            for (int i = 0; i < activeSliders.Count; i++)
            {
                if (activeSliders[i].Item1 == synthIdx)
                {
                    activeSliders.RemoveAt(i);
                    break;
                }
            }


            var ecb = audioManager.endSimulationECBSystem.CreateCommandBuffer();

            PlaybackRecordingData runningPlaybackData = new PlaybackRecordingData();
            runningPlaybackData = entityManager.GetComponentData<PlaybackRecordingData>(weapon_entity);
            var keyBuffer = entityManager.GetBuffer<PlaybackRecordingKeysBuffer>(weapon_entity);

            var playbackKeys = new NativeArray<PlaybackKey>(keyBuffer.Length, Allocator.Persistent);
            playbackKeys.CopyFrom(keyBuffer.AsNativeArray().Reinterpret<PlaybackKey>());

            AudioManager.audioGenerator.audioLayoutStorage.WritePlayback(new PlaybackAudioBundle
            {
                IsLooping = true,
                //IsPlaying = false,
                PlaybackDuration = runningPlaybackData.time,
                PlaybackKeys = playbackKeys
            },synthIdx);


            ecb.RemoveComponent<PlaybackRecordingKeysBuffer>(weapon_entity);
            ecb.RemoveComponent<PlaybackRecordingData>(weapon_entity);
        }
        /// The playback is running
        else
        {
            //Debug.Log("stop on playback");

            for (int i = 0; i < activeSliders.Count; i++)
            {
                if (activeSliders[i].Item1 == synthIdx)
                {
                    activeSliders.RemoveAt(i);
                    break;
                }
            }

            /// Activate Rec button GB
            SynthToolBar.transform.GetChild(activeSynthIdx).GetChild(2).GetChild(0).gameObject.SetActive(true);

            AudioManager.audioGenerator.audioLayoutStorage.WriteActivation(synthIdx, false);

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
