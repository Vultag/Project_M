
using MusicNamespace;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

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

    private ComputeBuffer WeaponsDataBuffer;
    [HideInInspector]
    public WeaponsData[] weaponsData;

    EntityQuery ActivePlaybackEntityQuery;


    //public EntityQuery ActiveWeapon_query;


    struct SignalData
    {
        public float3 SinSawSquareFactor;
        public float2 direction;
        public float frequency;
        public float amplitude;
        public float3 color;
        public float WeaponIdx;
    };
    public struct WeaponsData
    {
        public float2 weaponPos;
    };


    // Start is called before the first frame update
    void Start()
    {
        // Initialize the signals array with arbitrary max item number
        int maxSignalCount = 25;
        Signals = new SignalData[maxSignalCount];
        // Create a compute buffer to hold the signal data
        SignalBuffer = new ComputeBuffer(maxSignalCount, sizeof(float) * 11);
        SignalMaterial.SetBuffer("_SignalBuffer", SignalBuffer);

        weaponsData = new WeaponsData[7];
        WeaponsDataBuffer = new ComputeBuffer(7, sizeof(float) * 2);
        SignalMaterial.SetBuffer("_WeaponsDataBuffer", WeaponsDataBuffer);


        WeaponsDataBuffer.SetData(weaponsData, 0, 0, 7);

        var world = World.DefaultGameObjectInjectionWorld;
        entityManager = world.EntityManager;

        ActivePlaybackEntityQuery = entityManager.CreateEntityQuery(typeof(PlaybackData));

    }
    private void LateUpdate()
    {
        if (audioManager.Player_query.IsEmpty)
            return;
  
        //redondant ?
        Entity player_entity = audioManager.Player_query.GetSingletonEntity();
        var playerTrans = entityManager.GetComponentData<LocalToWorld>(player_entity).Value;

        SignalCount = 0;
        int c = 0;
        if (!audioManager.ActiveWeapon_query.IsEmpty)
        {
            Entity controlledWeapon_entity = audioManager.ControlledEquipment_query.GetSingletonEntity();
            Entity activeWeapon_entity = audioManager.ActiveWeapon_query.GetSingletonEntity();

            if (entityManager.HasComponent<RayData>(activeWeapon_entity))
            {
                DynamicBuffer<SustainedKeyBufferData> SkeyBuffer = entityManager.GetBuffer<SustainedKeyBufferData>(controlledWeapon_entity);
                DynamicBuffer<ReleasedKeyBufferData> RkeyBuffer = entityManager.GetBuffer<ReleasedKeyBufferData>(controlledWeapon_entity);

                SynthData activeSynthData = AudioLayoutStorageHolder.audioLayoutStorage.AuxillarySynthsData[AudioLayoutStorage.activeSynthIdx];

                // Update the signal count 
                SignalCount += SkeyBuffer.Length + RkeyBuffer.Length;
                int i = 0;
                // Populate the signals array with active weapon signals
                // OPTI : 4 statementes -> 1
                for (; i < SkeyBuffer.Length; i++)
                {
                    //Debug.Log("test");
                    //Debug.Log(AudioLayoutStorage.activeSynthIdx);
                    if (SkeyBuffer[i].Delta < 0) continue;
                    Signals[i].SinSawSquareFactor = activeSynthData.Osc1SinSawSquareFactor + activeSynthData.Osc2SinSawSquareFactor;
                    var rotatedDir = math.mul(playerTrans.Rotation(), new float3(SkeyBuffer[i].EffectiveDirLenght, 0));
                    Signals[i].direction = new float2(rotatedDir.x, rotatedDir.y);
                    Signals[i].frequency = MusicUtils.DirectionToFrequency(SkeyBuffer[i].EffectiveDirLenght);
                    Signals[i].amplitude = SkeyBuffer[i].currentAmplitude;
                    Signals[i].color = FilterUI.GetColorFromFilter(SkeyBuffer[i].filter.Cutoff, SkeyBuffer[i].filter.Resonance, activeSynthData.filterType);
                    Signals[i].WeaponIdx = 0;
                }
                int y = i;
                for (; y < RkeyBuffer.Length + i; y++)
                {
                    Signals[y].SinSawSquareFactor = activeSynthData.Osc1SinSawSquareFactor + activeSynthData.Osc2SinSawSquareFactor;
                    var rotatedDir = math.mul(playerTrans.Rotation(), new float3(RkeyBuffer[y - i].EffectiveDirLenght, 0));
                    Signals[y].direction = new float2(rotatedDir.x, rotatedDir.y);
                    Signals[y].frequency = MusicUtils.DirectionToFrequency(RkeyBuffer[y - i].EffectiveDirLenght);
                    Signals[y].amplitude = RkeyBuffer[y - i].currentAmplitude;
                    Signals[y].color = FilterUI.GetColorFromFilter(RkeyBuffer[y - i].filter.Cutoff, RkeyBuffer[y - i].filter.Resonance, activeSynthData.filterType);
                    Signals[y].WeaponIdx = 0;
                }
                c = y;
            }
         
        }
      
        // Populate the signals array with playback weapon signals
        NativeArray<Entity> PlaybackBufferEntities = ActivePlaybackEntityQuery.ToEntityArray(Allocator.Temp);
        for (int z = 0; z < PlaybackBufferEntities.Length; z++)
        {
            if (!entityManager.HasComponent<RayData>(PlaybackBufferEntities[z]))
                continue;
            DynamicBuffer<PlaybackSustainedKeyBufferData> PlaybackSkeyBuffer = entityManager.GetBuffer<PlaybackSustainedKeyBufferData>(PlaybackBufferEntities[z]);
            DynamicBuffer<PlaybackReleasedKeyBufferData> PlaybackRkeyBuffer = entityManager.GetBuffer<PlaybackReleasedKeyBufferData>(PlaybackBufferEntities[z]);

            int weaponIdx = entityManager.GetComponentData<WeaponData>(PlaybackBufferEntities[z]).WeaponIdx;

            SignalCount += PlaybackSkeyBuffer.Length + PlaybackRkeyBuffer.Length;
            SynthData PlaybackData = AudioLayoutStorageHolder.audioLayoutStorage.AuxillarySynthsData[entityManager.GetComponentData<PlaybackData>(PlaybackBufferEntities[z]).FullPlaybackIndex.x]; 
            int a = 0;
            for (; a < PlaybackSkeyBuffer.Length; a++)
            {
                Signals[c + a].SinSawSquareFactor = PlaybackData.Osc1SinSawSquareFactor + PlaybackData.Osc2SinSawSquareFactor;
                var rotatedDir = math.mul(playerTrans.Rotation(), new float3(PlaybackSkeyBuffer[a].EffectiveDirLenght, 0));
                Signals[c + a].direction = new float2(rotatedDir.x, rotatedDir.y);
                Signals[c + a].frequency = MusicUtils.DirectionToFrequency(PlaybackSkeyBuffer[a].EffectiveDirLenght);
                Signals[c + a].amplitude = PlaybackSkeyBuffer[a].currentAmplitude;
                Signals[c + a].color = FilterUI.GetColorFromFilter(PlaybackSkeyBuffer[a].filter.Cutoff, PlaybackSkeyBuffer[a].filter.Resonance, PlaybackData.filterType);
                Signals[c + a].WeaponIdx = weaponIdx;
            }
            int b = a;
            for (; b < PlaybackRkeyBuffer.Length + a; b++)
            {
                Signals[c + b].SinSawSquareFactor = PlaybackData.Osc1SinSawSquareFactor + PlaybackData.Osc2SinSawSquareFactor;
                var rotatedDir = math.mul(playerTrans.Rotation(), new float3(PlaybackRkeyBuffer[b - a].EffectiveDirLenght, 0));
                Signals[c + b].direction = new float2(rotatedDir.x, rotatedDir.y);
                Signals[c + b].frequency = MusicUtils.DirectionToFrequency(PlaybackRkeyBuffer[b - a].EffectiveDirLenght);
                Signals[c + b].amplitude = PlaybackRkeyBuffer[b - a].currentAmplitude;
                Signals[c + b].color = FilterUI.GetColorFromFilter(PlaybackRkeyBuffer[b - a].filter.Cutoff, PlaybackRkeyBuffer[b - a].filter.Resonance, PlaybackData.filterType);
                Signals[c + b].WeaponIdx = weaponIdx;
            }
            c += b;
        }

        var main_weapon_trans = math.mul(playerTrans.Rotation(), new float3(0, 0.42f,0));
        weaponsData[0].weaponPos = new float2(main_weapon_trans.x + playerTrans.Translation().x, main_weapon_trans.y + playerTrans.Translation().y);
        for (int w = 0; w < AudioManager.AuxillaryEquipmentEntities.Length; w++)
        {
            float3 weapTrans = math.mul(playerTrans.Rotation(), entityManager.GetComponentData<LocalTransform>(AudioManager.AuxillaryEquipmentEntities[w]).Position);
            weaponsData[w + 1].weaponPos = new float2(weapTrans.x + playerTrans.Translation().x, weapTrans.y + playerTrans.Translation().y);
        }



        // Update the compute buffer with the new signal data
        SignalBuffer.SetData(Signals, 0, 0, SignalCount);
        WeaponsDataBuffer.SetData(weaponsData, 0, 0, 7);

        SignalMaterial.SetFloat("_SignalCount", SignalCount);

    }

    void OnDrawGizmos()
    {

        //passShaderData();

    }
    private void OnDestroy()
    {
        SignalBuffer.Dispose();
        WeaponsDataBuffer.Dispose();
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
