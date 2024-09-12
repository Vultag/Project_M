using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using Unity.Entities.UniversalDelegates;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

public class AudioManager : MonoBehaviour
{


    private EntityManager entityManager;

    /// CHANGE PLACE ?
    public EntityQuery Player_query;
    public EntityQuery ActiveWeapon_query;

    //public NativeArray<Entity> WeaponSynthEntities;
    //public short activeWeaponSynth;
    private short activeWeaponIDX;

    public static AudioGenerator audioGenerator;
    //private AudioLayoutStorage audioLayoutStorage;
    public float TEMPplaybackDuration;

    //[SerializeField]
    //private GameObject AudioGeneratorPrefab;
    [SerializeField]
    private LineRenderer OscillatorLine;
    [SerializeField]
    private SliderMono waveformSlider;
    //private List<GameObject> AudioSources;

    public EndSimulationEntityCommandBufferSystem endSimulationECBSystem;

    //EntityQuery _query;
    void Start()
    {
        TEMPplaybackDuration = 4f;

        audioGenerator = Object.FindAnyObjectByType<AudioGenerator>();

        var world = World.DefaultGameObjectInjectionWorld;
        entityManager = world.EntityManager;
        Player_query = entityManager.CreateEntityQuery(typeof(PlayerData));
        ActiveWeapon_query = entityManager.CreateEntityQuery(typeof(ActiveSynthTag));

        Entity player_entity = Player_query.GetSingletonEntity();
        Entity start_weapon = entityManager.GetComponentData<PlayerData>(player_entity).ActiveCanon;
        audioGenerator.activeKeys = new NativeArray<KeyData>(12, Allocator.Persistent);
        audioGenerator.activeKeyNumber = new NativeArray<int>(1, Allocator.Persistent);
        audioGenerator.SynthsData = new NativeArray<SynthData>(1, Allocator.Persistent);
        audioGenerator.activeSynthsIdx = new NativeArray<int>(1, Allocator.Persistent);
        audioGenerator.SynthsData[0] = entityManager.GetComponentData<SynthData>(start_weapon);
        //audioGenerator.SynthsData[0] = SynthData.CreateDefault();
        audioGenerator.PlaybackAudioBundles = new NativeArray<PlaybackAudioBundle>(1, Allocator.Persistent);
        AudioLayoutStorageHolder.audioLayoutStorage.SynthsData = new NativeArray<SynthData>(1, Allocator.Persistent);
        AudioLayoutStorageHolder.audioLayoutStorage.PlaybackAudioBundles = new NativeArray<PlaybackAudioBundle>(1, Allocator.Persistent);
        //audioGenerator.PlaybackAudioBundlesContext = new NativeArray<PlaybackAudioBundleContext>(1, Allocator.Persistent);
        WeaponSystem.WeaponEntities[0] = start_weapon;

        //Debug.Log(ActiveWeapon_query.CalculateEntityCount());

        // Get the EndSimulationEntityCommandBufferSystem from the World
        endSimulationECBSystem = World.DefaultGameObjectInjectionWorld.GetOrCreateSystemManaged<EndSimulationEntityCommandBufferSystem>();

        endSimulationECBSystem.CreateCommandBuffer().AddComponent<ActiveSynthTag>(start_weapon);

        //AudioSources = new List<GameObject>();
        //_query = entityManager.CreateEntityQuery(typeof(SynthData));
    }
    // Update is called once per frame
    void Update()
    {

        //int SynthNum = _query.CalculateEntityCount();
        //var SynthentityArray = _query.ToEntityArray(Allocator.Temp);

        //for ( int i = SynthNum; i != AudioSources.Count;)
        //{
        //    if (SynthNum > AudioSources.Count)
        //    {
        //        var new_audio = Instantiate(AudioGeneratorPrefab);
        //        AudioSources.Add(new_audio);
        //        ///new_audio.GetComponent<AudioGenerator>().WeaponSynthEntity = SynthentityArray[i-1];
        //        new_audio.GetComponent<AudioGenerator>().OscillatorLine = OscillatorLine;
        //        waveformSlider.CurrentSynth = new_audio.GetComponent<AudioGenerator>();

        //        i--;
        //    }
        //    else if (SynthNum < AudioSources.Count)
        //    {
        //        Destroy(AudioSources[AudioSources.Count-1]);
        //        i++;
        //    }
        //    else
        //    {
        //        break;
        //    }
        //}



    }

    public void SelectSynth(int index)
    {
        if (index == activeWeaponIDX)
            return;

        activeWeaponIDX = (short)index;

        var ecb = endSimulationECBSystem.CreateCommandBuffer();
        Entity weapon_entity = ActiveWeapon_query.GetSingletonEntity();
        ecb.RemoveComponent<ActiveSynthTag>(weapon_entity);
        ecb.AddComponent<ActiveSynthTag>(WeaponSystem.WeaponEntities[activeWeaponIDX]);


        AudioLayoutStorageHolder.audioLayoutStorage.WriteSelectSynth(index);



    }

    public void AddSynth(int NumOfSynths)
    {

        var ecb = endSimulationECBSystem.CreateCommandBuffer();

        Entity player_entity = Player_query.GetSingletonEntity();
        Entity new_weapon = entityManager.Instantiate(entityManager.GetComponentData<PlayerData>(player_entity).WeaponPrefab);
        NativeArray<Entity> weaponEntities = new NativeArray<Entity>(NumOfSynths, Allocator.Persistent);
        for (int i = 0; i < WeaponSystem.WeaponEntities.Length; i++)
        {
            weaponEntities[i] = WeaponSystem.WeaponEntities[i];
        }
        weaponEntities[NumOfSynths-1] = new_weapon;
        WeaponSystem.WeaponEntities.Dispose();
        WeaponSystem.WeaponEntities = weaponEntities;

        // Add the Parent component to the child entity to set the singleton as its parent
        ecb.AddComponent(new_weapon, new Parent { Value = player_entity });
        // Add the LocalToWorld component to the child entity to ensure its position is calculated correctly
        ecb.AddComponent(new_weapon, new LocalToWorld { Value = float4x4.identity });

        AudioLayoutStorageHolder.audioLayoutStorage.WriteAddSynth(SynthData.CreateDefault());


    }





}
