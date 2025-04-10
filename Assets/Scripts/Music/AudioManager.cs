using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using Unity.Entities.UniversalDelegates;
using Unity.Mathematics;
using Unity.Rendering;
using Unity.Transforms;
using UnityEngine;
using UnityEngine.UIElements;
using static UnityEngine.EventSystems.EventTrigger;
using Object = UnityEngine.Object;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    private EntityManager entityManager;

    /// CHANGE PLACE ?
    public EntityQuery Player_query;
    public EntityQuery ActiveWeapon_query;
    public EntityQuery ActiveDMachine_query;
    public EntityQuery ControlledEquipment_query;

    public static NativeArray<Entity> AuxillaryEquipmentEntities;

    //public NativeArray<Entity> WeaponSynthEntities;
    //public short activeWeaponSynth;
    [HideInInspector]
    public short activeEquipmentIDX;

    public static AudioGenerator audioGenerator;
    //private AudioLayoutStorage audioLayoutStorage;
    public float TEMPplaybackDuration;

    //[SerializeField]
    //private GameObject AudioGeneratorPrefab;
    [SerializeField]
    private LineRenderer OscillatorLine;
    [SerializeField]
    private SliderMono waveformSlider;

    [SerializeField]
    private AudioSource DMachineAudioSource;
    [SerializeField]
    private AudioClip[] DMachineClips;


    public UIPlaybacksHolder uiPlaybacksHolder;


    public EndSimulationEntityCommandBufferSystem endSimulationECBSystem;

    private void Awake()
    {

        AuxillaryEquipmentEntities = new NativeArray<Entity>(0, Allocator.Persistent);

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

        AudioLayoutStorageHolder.audioLayoutStorage.AuxillarySynthsData = new NativeArray<SynthData>(1, Allocator.Persistent);
        /// Placeholder for the Audiogenerator Start ?
        AudioLayoutStorageHolder.audioLayoutStorage.AuxillarySynthsData[0] = SynthData.CreateDefault(WeaponType.Canon);
        AudioLayoutStorageHolder.audioLayoutStorage.PlaybackAudioBundles = new NativeArray<PlaybackAudioBundle>(1, Allocator.Persistent);
        //AudioLayoutStorageHolder.audioLayoutStorage.ActiveMusicSheet = MusicSheetData.CreateDefault();
        activeEquipmentIDX = -1;
    }

    IEnumerator StartCoroutine()
    {

        var world = World.DefaultGameObjectInjectionWorld;
        entityManager = world.EntityManager;
        Player_query = entityManager.CreateEntityQuery(typeof(PlayerData));

        while (Player_query.IsEmpty)
            yield return null; // Wait until the PlayerData entity exists

        // ONLY IF START WITH WEAPON
        Entity player_entity = Player_query.GetSingletonEntity();
        Entity start_weapon = entityManager.GetComponentData<PlayerData>(player_entity).MainCanon;
        entityManager.AddBuffer<SustainedKeyBufferData>(start_weapon);
        entityManager.AddBuffer<ReleasedKeyBufferData>(start_weapon);

    }

        //EntityQuery _query;
    void Start()
    {

        StartCoroutine("StartCoroutine");


        TEMPplaybackDuration = 4f;

        audioGenerator = Object.FindAnyObjectByType<AudioGenerator>();
        uiPlaybacksHolder = Object.FindAnyObjectByType<UIPlaybacksHolder>();
        //PlaybackRecordSystem.SetAudioManager(this);

        ActiveWeapon_query = entityManager.CreateEntityQuery(typeof(ActiveSynthTag));
        ActiveDMachine_query = entityManager.CreateEntityQuery(typeof(ActiveDMachineTag));
        ControlledEquipment_query = entityManager.CreateEntityQuery(typeof(ControledWeaponTag));

        endSimulationECBSystem = World.DefaultGameObjectInjectionWorld.GetOrCreateSystemManaged<EndSimulationEntityCommandBufferSystem>();

        audioGenerator.activeKeys = new NativeArray<KeyData>(12, Allocator.Persistent);
        audioGenerator.activeKeyNumber = new NativeArray<int>(1, Allocator.Persistent);
        audioGenerator.SynthsData = new NativeArray<SynthData>(1, Allocator.Persistent);
        audioGenerator.activeSynthsIdx = new NativeArray<int>(1, Allocator.Persistent);
        audioGenerator.SynthsData[0] = AudioLayoutStorageHolder.audioLayoutStorage.AuxillarySynthsData[0];
        audioGenerator.PlaybackAudioBundles = new NativeArray<PlaybackAudioBundle>(1, Allocator.Persistent);
        /// Useless ?
        //AudioManager.EquipmentEntities[0] = start_weapon;

        //endSimulationECBSystem.CreateCommandBuffer().AddComponent<ActiveSynthTag>(start_weapon);
        //AudioLayoutStorage.activeSynthIdx = 0;


        //var playerEquipmentSpriteBuffer = entityManager.GetBuffer<Child>(start_weapon);
        //var startBuffer = endSimulationECBSystem.CreateCommandBuffer();

        //startBuffer.AddComponent<Disabled>(playerEquipmentSpriteBuffer[0].Value);
        //startBuffer.AddComponent<Disabled>(playerEquipmentSpriteBuffer[1].Value);
        //Debug.Log(playerEquipmentSpriteBuffer[0].Value.Index);


        //Debug.Log(ActiveWeapon_query.CalculateEntityCount());

        // Get the EndSimulationEntityCommandBufferSystem from the World

        //AudioSources = new List<GameObject>();
        //_query = entityManager.CreateEntityQuery(typeof(SynthData));
    }
    private void OnDestroy()
    {
        AuxillaryEquipmentEntities.Dispose();
        AudioLayoutStorageHolder.audioLayoutStorage.AuxillarySynthsData.Dispose();
        AudioLayoutStorageHolder.audioLayoutStorage.PlaybackAudioBundles.Dispose();
    }


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
        if (index == activeEquipmentIDX)
            return;
        //Debug.Log(index);
        //Debug.Log(activeWeaponIDX);

        activeEquipmentIDX = (short)index;
        var controlledEquipmentE = ControlledEquipment_query.GetSingletonEntity();
        var newWeaponE = AudioManager.AuxillaryEquipmentEntities[index];

        var ecb = endSimulationECBSystem.CreateCommandBuffer();
        var newWeaponData = entityManager.GetComponentData<WeaponData>(controlledEquipmentE);
        /// child index unstable ??
        var playerWeaponSpriteEntity = newWeaponData.MainWeaponSpriteE;

        /// show Weapon on main slot
        MaterialMeshInfo newWeaponMaterialMeshInfo = entityManager.GetComponentData<MaterialMeshInfo>(playerWeaponSpriteEntity);
        newWeaponMaterialMeshInfo.Mesh = -1;
        ecb.SetComponent<MaterialMeshInfo>(playerWeaponSpriteEntity, newWeaponMaterialMeshInfo);

        //if(ControlledEquipment_query.IsEmpty)
        //{
        //    ecb.AddComponent<ControledWeaponTag>(controlledEquipmentE);

        //    if (newWeaponData.weaponClass == WeaponClass.Ray)
        //    {
        //        ecb.SetComponent<URPMaterialPropertyAtlasItemIdx>(playerWeaponSpriteEntity, new URPMaterialPropertyAtlasItemIdx { Value = 2 });
        //        //ecb.AddComponent(start_weapon, new RayData
        //        //{
        //        //    //to do
        //        //});
        //    }
        //    else if (newWeaponData.weaponClass == WeaponClass.Projectile)
        //    {
        //        ecb.SetComponent<URPMaterialPropertyAtlasItemIdx>(playerWeaponSpriteEntity, new URPMaterialPropertyAtlasItemIdx { Value = 1 });
        //        //ecb.AddComponent(start_weapon, new ProjectileData
        //        //{
        //        //    Damage = 6f,
        //        //    Speed = 1f
        //        //});
        //    }
        //    //AudioLayoutStorage.activeSynthIdx = 0;
        //    //AudioLayoutStorageHolder.audioLayoutStorage.WriteModifySynth(synthData);
        //}

        if (newWeaponData.weaponClass == WeaponClass.Ray)
        {
            /// Set sprite idx on auxillary weapon
            ecb.SetComponent<URPMaterialPropertyAtlasItemIdx>(playerWeaponSpriteEntity, new URPMaterialPropertyAtlasItemIdx { Value = 2 });

        }
        else if (newWeaponData.weaponClass == WeaponClass.Projectile)
        {
            /// Set sprite idx on auxillary weapon
            ecb.SetComponent<URPMaterialPropertyAtlasItemIdx>(playerWeaponSpriteEntity, new URPMaterialPropertyAtlasItemIdx { Value = 1 });

        }
        if (!ActiveWeapon_query.IsEmpty)
        {
            Entity weapon_entity = ActiveWeapon_query.GetSingletonEntity();
            ecb.RemoveComponent<ActiveSynthTag>(weapon_entity);
        }
        if(!ActiveDMachine_query.IsEmpty)
        {
            /// hide DMachine on main slot
            var playerDMachineSpriteEntity = newWeaponData.MainDMachineSpriteE;
            MaterialMeshInfo newDMachineMaterialMeshInfo = entityManager.GetComponentData<MaterialMeshInfo>(playerDMachineSpriteEntity);
            ///newDMachineMaterialMeshInfo.Material = -2;
            newDMachineMaterialMeshInfo.Mesh = 0;
            ecb.SetComponent<MaterialMeshInfo>(playerDMachineSpriteEntity, newDMachineMaterialMeshInfo);

            Entity DMachine_entity = ActiveDMachine_query.GetSingletonEntity();
            ecb.RemoveComponent<ActiveDMachineTag>(DMachine_entity);
        }
        ecb.AddComponent<ActiveSynthTag>(newWeaponE);

        AudioLayoutStorageHolder.audioLayoutStorage.WriteSelectSynth(index);

    }
    public void SelectDrumMachine(int index)
    {
        if (index == activeEquipmentIDX)
            return;

        activeEquipmentIDX = (short)index;
        var mainWeaponE = ControlledEquipment_query.GetSingletonEntity();
        var newDMachineE = AudioManager.AuxillaryEquipmentEntities[index];
        var ecb = endSimulationECBSystem.CreateCommandBuffer();

        /// show DMachine on main slot
        /// child index unstable ??
        var playerDMachineSpriteEntity = entityManager.GetComponentData<WeaponData>(mainWeaponE).MainDMachineSpriteE;
        MaterialMeshInfo newDMachineMaterialMeshInfo = entityManager.GetComponentData<MaterialMeshInfo>(playerDMachineSpriteEntity);
        ///newDMachineMaterialMeshInfo.Material = -2;
        newDMachineMaterialMeshInfo.Mesh = -1;
        ecb.SetComponent<MaterialMeshInfo>(playerDMachineSpriteEntity, newDMachineMaterialMeshInfo);

        if (!ActiveWeapon_query.IsEmpty)
        {
            /// hide Weapon on main slot
            var playerWeaponSpriteEntity = entityManager.GetComponentData<WeaponData>(mainWeaponE).MainWeaponSpriteE;
            MaterialMeshInfo newWeaponMaterialMeshInfo = entityManager.GetComponentData<MaterialMeshInfo>(playerWeaponSpriteEntity);
            ///newWeaponMaterialMeshInfo.Material = -1;
            newWeaponMaterialMeshInfo.Mesh = 0;
            ecb.SetComponent<MaterialMeshInfo>(playerWeaponSpriteEntity, newWeaponMaterialMeshInfo);
     
            LocalTransform newPlayerDMachineSpriteEntityTrans = entityManager.GetComponentData<LocalTransform>(mainWeaponE);
            newPlayerDMachineSpriteEntityTrans.Rotation = quaternion.identity;
            ecb.SetComponent<LocalTransform>(mainWeaponE, newPlayerDMachineSpriteEntityTrans);

            Entity weapon_entity = ActiveWeapon_query.GetSingletonEntity();
            ecb.RemoveComponent<ActiveSynthTag>(weapon_entity);
        }
        if (!ActiveDMachine_query.IsEmpty)
        {
            Entity DMachine_entity = ActiveDMachine_query.GetSingletonEntity();
            ecb.RemoveComponent<ActiveDMachineTag>(DMachine_entity);
        }
        ecb.AddComponent<ActiveDMachineTag>(newDMachineE);
    }
    /// <summary>
    ///  INSTANCIATING WITH ENTITY MANAGER -> REWORK
    /// </summary>
    public void AddSynth(int NumOfEquipments, SynthData synthData, WeaponClass weaponClass, WeaponType weaponType)
    {

        var ecb = endSimulationECBSystem.CreateCommandBuffer();

        Entity player_entity = Player_query.GetSingletonEntity();
        var start_weapon = entityManager.GetComponentData<PlayerData>(player_entity).MainCanon;
        Entity new_weapon = entityManager.Instantiate(entityManager.GetComponentData<PlayerData>(player_entity).WeaponPrefab);
        NativeArray<Entity> equipmentEntities = new NativeArray<Entity>(NumOfEquipments, Allocator.Persistent);
        for (int i = 0; i < AudioManager.AuxillaryEquipmentEntities.Length; i++)
        {
            equipmentEntities[i] = AudioManager.AuxillaryEquipmentEntities[i];
        }
        equipmentEntities[NumOfEquipments-1] = new_weapon;
        AudioManager.AuxillaryEquipmentEntities.Dispose();
        AudioManager.AuxillaryEquipmentEntities = equipmentEntities;

        //Debug.Log(synthData.ADSR.Sustain);

        ecb.AddBuffer<SustainedKeyBufferData>(new_weapon);
        ecb.AddBuffer<ReleasedKeyBufferData>(new_weapon);
        //ecb.AddComponent<SynthData>(new_weapon, synthData);

        /// Add the Parent component to the child entity to set the singleton as its parent
        ecb.AddComponent(new_weapon, new Parent { Value = player_entity });
        /// Add the LocalToWorld component to the child entity to ensure its position is calculated correctly
        var newWeaponLTW = LocalTransform.FromPosition(new float3(0, 0.40f, 0));

        //var newWeaponData = new WeaponData { WeaponIdx = (NumOfSynths - 1) };

        if (NumOfEquipments == 1)
        {
            //audioGenerator.gameObject.SetActive(true);
            ///// Activate the RaysToShader gameObject
            //Camera.main.transform.GetChild(0).gameObject.SetActive(true);
            ecb.AddComponent<ControledWeaponTag>(start_weapon);
            //ecb.AddComponent<ActiveSynthTag>(new_weapon);
            //ecb.AddBuffer<SustainedKeyBufferData>(start_weapon);
            //ecb.AddBuffer<ReleasedKeyBufferData>(start_weapon);
            //ecb.AddComponent<SynthData>(start_weapon, synthData);
            //SelectSynth(0);

            var playerWeaponSpriteEntity = entityManager.GetComponentData<WeaponData>(start_weapon).MainWeaponSpriteE;
            if (weaponClass == WeaponClass.Ray)
            {
                ecb.SetComponent<URPMaterialPropertyAtlasItemIdx>(playerWeaponSpriteEntity, new URPMaterialPropertyAtlasItemIdx { Value = 2 });
                //ecb.AddComponent(start_weapon, new RayData
                //{
                //    //to do
                //});
            }
            else if (weaponClass == WeaponClass.Projectile)
            {
                ecb.SetComponent<URPMaterialPropertyAtlasItemIdx>(playerWeaponSpriteEntity, new URPMaterialPropertyAtlasItemIdx { Value = 1 });
                //ecb.AddComponent(start_weapon, new ProjectileData
                //{
                //    Damage = 6f,
                //    Speed = 1f
                //});
            }
            AudioLayoutStorage.activeSynthIdx = 0;
            AudioLayoutStorageHolder.audioLayoutStorage.WriteModifySynth(synthData);
            AudioLayoutStorage.activeSynthIdx = -1;
        }
        else
        {
            AudioLayoutStorageHolder.audioLayoutStorage.WriteAddSynth(synthData);
        }
        /// TEMPORARY TILL I FIGURE WEAPON "SLOTS" ON THE PLAYER
        switch (NumOfEquipments)
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
        //ecb.AddComponent(new_weapon, newWeaponData);

        /// Need to fetch LinkedEntityGroup as parent/childs arnt assigned yet
        Entity newWeaponSpriteEntity = Entity.Null;
        foreach (LinkedEntityGroup linkedE in entityManager.GetBuffer<LinkedEntityGroup>(new_weapon))
        {
            if (entityManager.HasComponent<MaterialMeshInfo>(linkedE.Value))
                newWeaponSpriteEntity = linkedE.Value;
        }
        //WeaponData newWeaponData = entityManager.GetComponentData<WeaponData>(start_weapon);

        ecb.AddComponent<WeaponData>(new_weapon, new WeaponData
        {
            WeaponIdx = NumOfEquipments,
            weaponClass = weaponClass,
            weaponType = weaponType,
        });
        if (weaponClass == WeaponClass.Ray)
        {
            /// Set sprite idx on auxillary weapon
            ecb.AddComponent<URPMaterialPropertyAtlasItemIdx>(newWeaponSpriteEntity, new URPMaterialPropertyAtlasItemIdx { Value = 2 });

            ecb.AddComponent(new_weapon, new RayData
            {
                //to do
            });
        }
        else if (weaponClass == WeaponClass.Projectile)
        {
            /// Set sprite idx on auxillary weapon
            ecb.AddComponent<URPMaterialPropertyAtlasItemIdx>(newWeaponSpriteEntity, new URPMaterialPropertyAtlasItemIdx { Value = 1 });

            ecb.AddComponent(new_weapon, new ProjectileData
            {
                Damage = 6f,
                Speed = 1f,
                LifeTime = 3f,
                penetrationCapacity = 1
            });
        }

    }
    /// <summary>
    ///  INSTANCIATING WITH ENTITY MANAGER -> REWORK
    /// </summary>
    public void AddDrumMachine(int NumOfEquipments)
    {
        var ecb = endSimulationECBSystem.CreateCommandBuffer();

        Entity player_entity = Player_query.GetSingletonEntity();
        var playerData = entityManager.GetComponentData<PlayerData>(player_entity);
        Entity new_DMachine = entityManager.Instantiate(entityManager.GetComponentData<PlayerData>(player_entity).DrumMachinePrefab);
        NativeArray<Entity> equipmentEntities = new NativeArray<Entity>(NumOfEquipments, Allocator.Persistent);
        for (int i = 0; i < AudioManager.AuxillaryEquipmentEntities.Length; i++)
        {
            equipmentEntities[i] = AudioManager.AuxillaryEquipmentEntities[i];
        }
        equipmentEntities[NumOfEquipments - 1] = new_DMachine;
        AudioManager.AuxillaryEquipmentEntities.Dispose();
        AudioManager.AuxillaryEquipmentEntities = equipmentEntities;


        /// Add the Parent component to the child entity to set the singleton as its parent
        ecb.AddComponent(new_DMachine, new Parent { Value = player_entity });
        /// Add the LocalToWorld component to the child entity to ensure its position is calculated correctly
        var newDMachineLTW = LocalTransform.FromPosition(new float3(0, 0.40f, 0));

        //var newWeaponData = new WeaponData { WeaponIdx = (NumOfSynths - 1) };

        if (NumOfEquipments == 1)
        {
            ecb.AddComponent<ControledWeaponTag>(playerData.MainCanon);
        }
        /// TEMPORARY TILL I FIGURE WEAPON "SLOTS" ON THE PLAYER
        switch (NumOfEquipments)
        {
            case 1:
                newDMachineLTW = LocalTransform.FromPosition(new float3(-0.5985f, 0.1386f, 0));
                newDMachineLTW.Rotation = Quaternion.Euler(0, 0, -45);
                break;
            case 2:
                newDMachineLTW = LocalTransform.FromPosition(new float3(0.5985f, 0.1386f, 0));
                newDMachineLTW.Rotation = Quaternion.Euler(0, 0, 225);
                break;
            case 3:
                newDMachineLTW = LocalTransform.FromPosition(new float3(-0.9569f, -0.2231f, 0));
                newDMachineLTW.Rotation = Quaternion.Euler(0, 0, -45);
                break;
            case 4:
                newDMachineLTW = LocalTransform.FromPosition(new float3(0.9569f, -0.2231f, 0));
                newDMachineLTW.Rotation = Quaternion.Euler(0, 0, 225);
                break;
            case 5:
                newDMachineLTW = LocalTransform.FromPosition(new float3(-1.3189f, -0.5808f, 0));
                newDMachineLTW.Rotation = Quaternion.Euler(0, 0, -45);
                break;
            case 6:
                newDMachineLTW = LocalTransform.FromPosition(new float3(1.3189f, -0.5808f, 0));
                newDMachineLTW.Rotation = Quaternion.Euler(0, 0, 225);
                break;

        }

        ecb.AddComponent(new_DMachine, newDMachineLTW);

        ecb.AddComponent<DrumMachineData>(new_DMachine, new DrumMachineData
        {
            machineDrumContent = MachineDrumContent.SnareDrum | MachineDrumContent.BaseDrum | MachineDrumContent.HighHat,
            InstrumentAddOrder = new FixedList32Bytes<byte> { 2,1,0 }
        });
    }

    public void PlayRequestedDMachineSounds(NativeList<(ushort, float)> requests)
    {
        for (int i = 0; i < requests.Length; i++)
        {
            DMachineAudioSource.PlayOneShot(DMachineClips[requests[i].Item1]);
        }
    }

    #region SYNTHPLAYBACK PANEL


    #endregion



}
