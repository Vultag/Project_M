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
    private EntityQuery Player_query;

    public Entity activeWeaponSynth;
    private short activeWeaponIDX;

    public AudioGenerator AudioGenerator;

    //[SerializeField]
    //private GameObject AudioGeneratorPrefab;
    [SerializeField]
    private LineRenderer OscillatorLine;
    [SerializeField]
    private SliderMono waveformSlider;
    //private List<GameObject> AudioSources;

    //EntityQuery _query;
    void Start()
    {

        var world = World.DefaultGameObjectInjectionWorld;
        entityManager = world.EntityManager;
        Player_query = entityManager.CreateEntityQuery(typeof(PlayerData));

        Entity player_entity = Player_query.GetSingletonEntity();
        Entity start_weapon = entityManager.GetComponentData<PlayerData>(player_entity).ActiveCanon;
        AudioGenerator.activeKeys = new NativeArray<KeyData>(12, Allocator.Persistent);
        AudioGenerator.activeKeyNumber = new NativeArray<int>(1, Allocator.Persistent);
        AudioGenerator.SynthsData = new NativeArray<SynthData>(1, Allocator.Persistent);
        AudioGenerator.SynthsData[0] = entityManager.GetComponentData<SynthData>(start_weapon);
        WeaponSystem.WeaponEntities[0] = entityManager.GetComponentData<PlayerData>(player_entity).ActiveCanon;


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

    public void SelectSynth(int index, int NumOfSynths)
    {
        if (index == activeWeaponIDX)
            return;

        Entity player_entity = Player_query.GetSingletonEntity();

        /// set active weapon synth
        //WeaponSystem.activeSynthEntityindex = (short)index;

        //Debug.Log(index);
        //Debug.Log(NumOfSynths);

        /// The active Synth being at the start of the arrays of data, it requires rebuilding of all synth datas to put it first

        NativeArray<SynthData> newSynthsData = new NativeArray<SynthData>(NumOfSynths, Allocator.Persistent);
        NativeArray<KeyData> newActiveKeys = new NativeArray<KeyData>(NumOfSynths * 12, Allocator.Persistent);
        NativeArray<int> newActiveKeyNumber = new NativeArray<int>(NumOfSynths, Allocator.Persistent);
        newSynthsData.CopyFrom(AudioGenerator.SynthsData);
        newActiveKeys.CopyFrom(AudioGenerator.activeKeys);
        newActiveKeyNumber.CopyFrom(AudioGenerator.activeKeyNumber);

        if (index!=0)
        {
            
            /// Fill the active weapoon part
            newSynthsData[0] = AudioGenerator.SynthsData[index];
            for (int i = 0; i < 12; i++)
            {
                newActiveKeys[i] = AudioGenerator.activeKeys[i + (index * 12)];
            }
            newActiveKeyNumber[0] = AudioGenerator.activeKeyNumber[index];
            /// Fill the permutation with the synth of UI 0
            newSynthsData[index] = AudioGenerator.SynthsData[activeWeaponIDX];
            /// Remplace in the storing of the synths data with the previously playing synth
            /// Conflicting with the playback key processing -> cant carry on active keys -> rework playback synths ?
            if(activeWeaponIDX!=0)
                newSynthsData[activeWeaponIDX] = AudioGenerator.SynthsData[0];

        }
        else
        {
            /// Fill the active weapoon part
            newSynthsData[0] = AudioGenerator.SynthsData[activeWeaponIDX];
            for (int i = 0; i < 12; i++)
            {
                newActiveKeys[i] = AudioGenerator.activeKeys[i + (activeWeaponIDX * 12)];
            }
            newActiveKeyNumber[0] = AudioGenerator.activeKeyNumber[activeWeaponIDX];
            /// Fill the slot it remplace in the storing of the synths data
            /// Conflicting with the playback key processing -> cant carry on active keys -> rework playback synths ?
            newSynthsData[activeWeaponIDX] = AudioGenerator.SynthsData[0];

        }

        //for (int i = 0; i < 12; i++)
        //{
        //    newActiveKeys[i] = AudioGenerator.activeKeys[i + (index * 12)];
        //}
        //newActiveKeyNumber[0] = AudioGenerator.activeKeyNumber[index];





        AudioGenerator.SynthsData.Dispose();
        AudioGenerator.activeKeys.Dispose();
        AudioGenerator.activeKeyNumber.Dispose();
        AudioGenerator.SynthsData = newSynthsData;
        AudioGenerator.activeKeys = newActiveKeys;
        AudioGenerator.activeKeyNumber = newActiveKeyNumber;
        //Debug.Log(AudioGenerator.activeKeys.Length);

        activeWeaponIDX = (short)index;

    }

    public void AddSynth(int NumOfSynths)
    {

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
        entityManager.AddComponentData(new_weapon, new Parent { Value = player_entity });
        // Add the LocalToWorld component to the child entity to ensure its position is calculated correctly
        entityManager.AddComponentData(new_weapon, new LocalToWorld { Value = float4x4.identity });


        NativeArray<SynthData> newSynthsData = new NativeArray<SynthData>(NumOfSynths, Allocator.Persistent);
        NativeArray<KeyData> newActiveKeys = new NativeArray<KeyData>(NumOfSynths * 12, Allocator.Persistent);
        NativeArray<int> newActiveKeyNumber = new NativeArray<int>(NumOfSynths, Allocator.Persistent);

        for (int i = 0; i < AudioGenerator.SynthsData.Length; i++)
        {
            newSynthsData[i] = AudioGenerator.SynthsData[i];
        }
        newSynthsData[NumOfSynths - 1] = entityManager.GetComponentData<SynthData>(new_weapon);
        for (int i = 0; i < AudioGenerator.activeKeys.Length; i++)
        {
            newActiveKeys[i] = AudioGenerator.activeKeys[i];
        }
        for (int i = 0; i < AudioGenerator.activeKeyNumber.Length; i++)
        {
            newActiveKeyNumber[i] = AudioGenerator.activeKeyNumber[i];
        }

        AudioGenerator.SynthsData.Dispose();
        AudioGenerator.activeKeys.Dispose();
        AudioGenerator.activeKeyNumber.Dispose();
        AudioGenerator.SynthsData = newSynthsData;
        AudioGenerator.activeKeys = newActiveKeys;
        AudioGenerator.activeKeyNumber = newActiveKeyNumber;

    }





}
