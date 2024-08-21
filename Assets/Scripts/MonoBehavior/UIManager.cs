using System.Collections;
using System.Collections.Generic;
using NUnit.Framework.Internal;
using Unity.Collections;
using Unity.Entities;
using Unity.Entities.UniversalDelegates;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

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

    private void Awake()
    {
        playerControls = new PlayerControls();
    }

    void Start()
    {

        var world = World.DefaultGameObjectInjectionWorld;
        entityManager = world.EntityManager;
        //Player_query = entityManager.CreateEntityQuery(typeof(PlayerData));



        playerControls.Enable();
        canvas = this.gameObject.transform.parent.GetComponent<Canvas>();

        ConstructUIsurface();
        NumOfSynths = SynthToolBar.GetComponentsInChildren<Image>().Length-1;
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
    }

    public void _SelectSynthUI(int index)
    {

        SynthToolBar.transform.GetChild(activeSynthIdx).GetComponent<Image>().color = Color.white;
        SynthToolBar.transform.GetChild(index).GetComponent<Image>().color = Color.black;
        activeSynthIdx = index;

        audioManager.SelectSynth(index,NumOfSynths);

        UpdateSynthUI();
    }
    public void _AddSynthUI()
    {
        SynthToolBar.transform.GetChild(NumOfSynths).gameObject.SetActive(true);
        NumOfSynths++;
        SynthUIadd_rect.position = SynthToolBar.transform.GetChild(NumOfSynths).GetComponent<RectTransform>().position;

        audioManager.AddSynth(NumOfSynths);


        //Entity player_entity = Player_query.GetSingletonEntity();
        //Entity new_weapon = entityManager.Instantiate(entityManager.GetComponentData<PlayerData>(player_entity).WeaponPrefab);
        //// Add the Parent component to the child entity to set the singleton as its parent
        //entityManager.AddComponentData(new_weapon, new Parent { Value = player_entity });
        //// Add the LocalToWorld component to the child entity to ensure its position is calculated correctly
        //entityManager.AddComponentData(new_weapon, new LocalToWorld { Value = float4x4.identity });


        //NativeArray<SynthData> newSynthsData = new NativeArray<SynthData>(NumOfSynths, Allocator.Persistent);
        //NativeArray<KeyData> newActiveKeys = new NativeArray<KeyData>(NumOfSynths*12, Allocator.Persistent);
        //NativeArray<int> newActiveKeyNumber = new NativeArray<int>(NumOfSynths, Allocator.Persistent);
        //newSynthsData.CopyFrom(AudioGenerator.SynthsData);
        //newActiveKeys.CopyFrom(AudioGenerator.activeKeys);
        //newActiveKeyNumber.CopyFrom(AudioGenerator.activeKeyNumber);
        //newSynthsData[NumOfSynths - 1] = entityManager.GetComponentData<SynthData>(new_weapon);
        //AudioGenerator.SynthsData.Dispose();
        //AudioGenerator.activeKeys.Dispose();
        //AudioGenerator.activeKeyNumber.Dispose();
        //AudioGenerator.SynthsData = newSynthsData;
        //AudioGenerator.activeKeys = newActiveKeys;
        //AudioGenerator.activeKeyNumber = newActiveKeyNumber;


        if (NumOfSynths == MaxSynthNum) { SynthUIadd_rect.gameObject.SetActive(false); }
    }

    void UpdateSynthUI()
    {
        var synthData = audioManager.AudioGenerator.SynthsData[0];

        simplexSlider.UpdateSlider(synthData.SinFactor, synthData.SquareFactor,synthData.SawFactor);

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
