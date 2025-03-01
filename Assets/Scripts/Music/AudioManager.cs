using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using Unity.Entities.UniversalDelegates;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;
using UnityEngine.UIElements;
using static UnityEngine.EventSystems.EventTrigger;
using Object = UnityEngine.Object;

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


    public UIPlaybacksHolder uiPlaybacksHolder;


    public EndSimulationEntityCommandBufferSystem endSimulationECBSystem;

    private void Awake()
    {
        AudioLayoutStorageHolder.audioLayoutStorage.SynthsData = new NativeArray<SynthData>(1, Allocator.Persistent);
        AudioLayoutStorageHolder.audioLayoutStorage.SynthsData[0] = SynthData.CreateDefault();
        AudioLayoutStorageHolder.audioLayoutStorage.PlaybackAudioBundles = new NativeArray<PlaybackAudioBundle>(1, Allocator.Persistent);
        //AudioLayoutStorageHolder.audioLayoutStorage.ActiveMusicSheet = MusicSheetData.CreateDefault();
        activeWeaponIDX = -1;
    }

    //EntityQuery _query;
    void Start()
    {
        TEMPplaybackDuration = 4f;

        audioGenerator = Object.FindAnyObjectByType<AudioGenerator>();
        uiPlaybacksHolder = Object.FindAnyObjectByType<UIPlaybacksHolder>();
        PlaybackRecordSystem.SetAudioManager(this);

        var world = World.DefaultGameObjectInjectionWorld;
        entityManager = world.EntityManager;
        Player_query = entityManager.CreateEntityQuery(typeof(PlayerData));
        ActiveWeapon_query = entityManager.CreateEntityQuery(typeof(ActiveSynthTag));

        endSimulationECBSystem = World.DefaultGameObjectInjectionWorld.GetOrCreateSystemManaged<EndSimulationEntityCommandBufferSystem>();

        // ONLY IF START WITH WEAPON
        Entity player_entity = Player_query.GetSingletonEntity();
        Entity start_weapon = entityManager.GetComponentData<PlayerData>(player_entity).MainCanon;
        entityManager.AddBuffer<SustainedKeyBufferData>(start_weapon);
        entityManager.AddBuffer<ReleasedKeyBufferData>(start_weapon);
        audioGenerator.activeKeys = new NativeArray<KeyData>(12, Allocator.Persistent);
        audioGenerator.activeKeyNumber = new NativeArray<int>(1, Allocator.Persistent);
        audioGenerator.SynthsData = new NativeArray<SynthData>(1, Allocator.Persistent);
        audioGenerator.activeSynthsIdx = new NativeArray<int>(1, Allocator.Persistent);
        audioGenerator.SynthsData[0] = AudioLayoutStorageHolder.audioLayoutStorage.SynthsData[0];
        audioGenerator.PlaybackAudioBundles = new NativeArray<PlaybackAudioBundle>(1, Allocator.Persistent);
        WeaponSystem.WeaponEntities[0] = start_weapon;
        endSimulationECBSystem.CreateCommandBuffer().AddComponent<ActiveSynthTag>(start_weapon);
        //AudioLayoutStorage.activeSynthIdx = 0;



        //Debug.Log(ActiveWeapon_query.CalculateEntityCount());

        // Get the EndSimulationEntityCommandBufferSystem from the World

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
        if(!ActiveWeapon_query.IsEmpty)
        {
            Entity weapon_entity = ActiveWeapon_query.GetSingletonEntity();
            ecb.RemoveComponent<ActiveSynthTag>(weapon_entity);
        }
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

        AudioLayoutStorageHolder.audioLayoutStorage.WriteAddSynth(SynthData.CreateDefault());

        ecb.AddBuffer<SustainedKeyBufferData>(new_weapon);
        ecb.AddBuffer<ReleasedKeyBufferData>(new_weapon);
        ecb.AddComponent<SynthData>(new_weapon,SynthData.CreateDefault());

        /// Add the Parent component to the child entity to set the singleton as its parent
        ecb.AddComponent(new_weapon, new Parent { Value = player_entity });
        /// Add the LocalToWorld component to the child entity to ensure its position is calculated correctly
        var newWeaponLTW = LocalTransform.FromPosition(new float3(0, 0.40f, 0));

        var newWeaponData = new WeaponData { WeaponIdx = (NumOfSynths - 1) };

        if ((NumOfSynths-1) == 1)
        {

            //audioGenerator.gameObject.SetActive(true);
            ///// Activate the RaysToShader gameObject
            //Camera.main.transform.GetChild(0).gameObject.SetActive(true);
            var start_weapon = entityManager.GetComponentData<PlayerData>(player_entity).MainCanon;
            ecb.AddComponent<ControledWeaponTag>(start_weapon);
            //ecb.AddBuffer<SustainedKeyBufferData>(start_weapon);
            //ecb.AddBuffer<ReleasedKeyBufferData>(start_weapon);
            ecb.AddComponent<SynthData>(start_weapon, SynthData.CreateDefault());
            //SelectSynth(0);
        }
        /// TEMPORARY TILL I FIGURE WEAPON "SLOTS" ON THE PLAYER
        switch (NumOfSynths-1)
            {
                case 1:
                    newWeaponLTW = LocalTransform.FromPosition(new float3(-0.5985f, 0.1386f, 0));
                    newWeaponLTW.Rotation = Quaternion.Euler(0, 0, -45);
                    break;
                case 2:
                    newWeaponLTW = LocalTransform.FromPosition(new float3(0.5985f, 0.1386f, 0));
                    newWeaponLTW.Rotation = Quaternion.Euler(0, 0, 225);
                    break;
                case 3:
                    newWeaponLTW = LocalTransform.FromPosition(new float3(-0.9569f, -0.2231f, 0));
                    newWeaponLTW.Rotation = Quaternion.Euler(0, 0, -45);
                    break;
                case 4:
                    newWeaponLTW = LocalTransform.FromPosition(new float3(0.9569f, -0.2231f, 0));
                    newWeaponLTW.Rotation = Quaternion.Euler(0, 0, 225);
                    break;
                case 5:
                    newWeaponLTW = LocalTransform.FromPosition(new float3(-1.3189f, -0.5808f, 0));
                    newWeaponLTW.Rotation = Quaternion.Euler(0, 0, -45);
                    break;
                case 6:
                    newWeaponLTW = LocalTransform.FromPosition(new float3(1.3189f, -0.5808f, 0));
                    newWeaponLTW.Rotation = Quaternion.Euler(0, 0, 225);
                    break;

            }
      
        ecb.AddComponent(new_weapon, newWeaponLTW);
        ecb.AddComponent(new_weapon, newWeaponData);

    }

    #region SYNTHPLAYBACK PANEL

 
    #endregion



}
