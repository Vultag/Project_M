using System.Collections;
using System.Collections.Generic;
using System.Numerics;
using MusicNamespace;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using Plane = UnityEngine.Plane;
using Vector2 = UnityEngine.Vector2;
using Vector3 = UnityEngine.Vector3;
using Vector4 = UnityEngine.Vector4;

public class RaysToShader : MonoBehaviour
{

    [SerializeField]
    private Material SignalMaterial;

    [SerializeField]
    private AudioManager audioManager;
    [SerializeField]
    private AudioGenerator audioGenerator;

    private EntityManager entityManager;


    private ComputeBuffer SignalBuffer;
    private SignalData[] Signals;
    private int SignalCount;
    //private ComputeBuffer PlaybackSignalBuffer;
    //private SignalData[] PlaybackSignals;
    //private int PlaybackSignalCount;


    public EntityQuery ActiveWeapon_query;


    struct SignalData
    {
        public float3 SinSawSquareFactor;
        public float2 direction;
        public float frequency;
        public float amplitude;
    };

    // Start is called before the first frame update
    void Start()
    {
        // Initialize the signals array with arbitrary max item number
        int maxSignalCount = 25;
        Signals = new SignalData[maxSignalCount];
        // Create a compute buffer to hold the signal data
        SignalBuffer = new ComputeBuffer(maxSignalCount, sizeof(float) * 7);
        SignalMaterial.SetBuffer("_SignalBuffer", SignalBuffer);

        var world = World.DefaultGameObjectInjectionWorld;
        entityManager = world.EntityManager;

       
    }
    private void LateUpdate()
    {
        //redondant ?
        Entity player_entity = audioManager.Player_query.GetSingletonEntity();
        Entity activeWeapon_entity = audioManager.ActiveWeapon_query.GetSingletonEntity();
        float3 playerPos = new float3(entityManager.GetComponentData<LocalToWorld>(player_entity).Value.Translation());

        DynamicBuffer<SustainedKeyBufferData> SkeyBuffer = entityManager.GetBuffer<SustainedKeyBufferData>(activeWeapon_entity);
        DynamicBuffer<ReleasedKeyBufferData> RkeyBuffer = entityManager.GetBuffer<ReleasedKeyBufferData>(activeWeapon_entity);

        Vector3 mousePosition = Camera.main.ScreenToWorldPoint(PlayerSystem.mousePos);

        // Update the signal count 
        SignalCount = SkeyBuffer.Length+RkeyBuffer.Length;
        int i = 0;
        // Populate the signals array with random values for demonstration
        for (; i < SkeyBuffer.Length; i++)
        {
            Signals[i].SinSawSquareFactor = new float3(audioGenerator.SynthsData[AudioLayoutStorage.activeSynthIdx].SinFactor, audioGenerator.SynthsData[AudioLayoutStorage.activeSynthIdx].SawFactor, audioGenerator.SynthsData[AudioLayoutStorage.activeSynthIdx].SquareFactor);
            Signals[i].direction = (float2)SkeyBuffer[i].EffectiveDirLenght;
            Signals[i].frequency = MusicUtils.DirectionToFrequency(SkeyBuffer[i].EffectiveDirLenght);
            Signals[i].amplitude = SkeyBuffer[i].currentAmplitude;
        }
        for (int y = i; y < RkeyBuffer.Length+i; y++)
        {
            Signals[y].SinSawSquareFactor = new float3(audioGenerator.SynthsData[AudioLayoutStorage.activeSynthIdx].SinFactor, audioGenerator.SynthsData[AudioLayoutStorage.activeSynthIdx].SawFactor, audioGenerator.SynthsData[AudioLayoutStorage.activeSynthIdx].SquareFactor);
            Signals[y].direction = (float2)RkeyBuffer[y - i].EffectiveDirLenght;
            Signals[y].frequency = MusicUtils.DirectionToFrequency(RkeyBuffer[y-i].EffectiveDirLenght);
            Signals[y].amplitude = RkeyBuffer[y-i].currentAmplitude;
        }

        // Update the compute buffer with the new signal data
        SignalBuffer.SetData(Signals, 0, 0, SignalCount);

        // Update the time parameter -> to keep waveforms in sync ?
        float time = Time.time;

        SignalMaterial.SetVector("_WeaponPos", new Vector4(playerPos.x, playerPos.y, 0, 0));
        SignalMaterial.SetFloat("_SignalCount", SignalCount);

    }



    void OnDrawGizmos()
    {

        //passShaderData();

    }
    private void OnDestroy()
    {
        // Release the compute buffer when done
        if (SignalBuffer != null)
        {
            SignalBuffer.Release();
        }
    }

    //void passShaderData()
    //{
    //    //Debug.Log("mousePosition");

    //    if (Mouse.current != null)
    //    {
    //        //Vector3 mousePosition = PlayerSystem.mousePos;

    //        Vector2 mousePosition = Mouse.current.position.ReadValue();
    //        //// Normalize mouse position to the range [0, 1]
    //        //mousePosition.x = ((mousePosition.x/ (float)Screen.width)-0.5f)*2f;
    //        //mousePosition.y = ((-mousePosition.y/ (float)Screen.height)+0.5f)*2f;

    //        //SceneView sceneView = SceneView.lastActiveSceneView;

    //        //Vector2 weaponPosition = sceneView.camera.WorldToViewportPoint(transform.position);
    //        ////weaponPosition.x = weaponPosition.x * Screen.width;
    //        ////weaponPosition.y = weaponPosition.y * Screen.height;
    //        //weaponPosition.x = (weaponPosition.x - 0.5f) * 2f;
    //        //weaponPosition.y = (weaponPosition.y - 0.5f) * 2f;

    //        //test
    //        UnityEngine.Ray ray = HandleUtility.GUIPointToWorldRay(mousePosition);
    //        UnityEngine.Plane plane = new Plane(Vector3.forward, Vector3.zero);
    //        plane.Raycast(ray, out float distance);
    //        Vector3 worldMousePosition = ray.GetPoint(distance);

    //        //mousePosition =  Camera.main.ViewportToWorldPoint(mousePosition);
    //        Vector2 weaponPosition = transform.position;

    //        //Gizmos.DrawLine(Vector3.zero, worldMousePosition);
    //        //Debug.Log(new Vector2(Screen.width, Screen.height));

    //        // Pass the normalized mouse position to the shader
    //        signalMaterial.SetVector("_MousePos", new Vector4(worldMousePosition.x, worldMousePosition.y, 0, 0));
    //        signalMaterial.SetVector("_WeaponPos", new Vector4(weaponPosition.x, weaponPosition.y, 0, 0));
    //        signalMaterial.SetVector("_EditorRes", new Vector4((float)Screen.width, (float)Screen.height, 0, 0));
    //    }

    //}


}
