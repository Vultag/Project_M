using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;

public class AudioManager : MonoBehaviour
{

    [SerializeField]
    private GameObject AudioGeneratorPrefab;
    [SerializeField]
    private LineRenderer OscillatorLine;
    [SerializeField]
    private SliderMono waveformSlider;
    private List<GameObject> AudioSources;

    EntityQuery _query;
    void Start()
    {
        var world = World.DefaultGameObjectInjectionWorld;
        var entityManager = world.EntityManager;

        AudioSources = new List<GameObject>();
        _query = entityManager.CreateEntityQuery(typeof(SynthData));
    }
    // Update is called once per frame
    void Update()
    {

        int SynthNum = _query.CalculateEntityCount();
        var SynthentityArray = _query.ToEntityArray(Allocator.Temp);

        for ( int i = SynthNum; i != AudioSources.Count;)
        {
            if (SynthNum > AudioSources.Count)
            {
                var new_audio = Instantiate(AudioGeneratorPrefab);
                AudioSources.Add(new_audio);
                new_audio.GetComponent<AudioGenerator>().WeaponSynthEntity = SynthentityArray[i-1];
                new_audio.GetComponent<AudioGenerator>().OscillatorLine = OscillatorLine;
                waveformSlider.CurrentSynth = new_audio.GetComponent<AudioGenerator>();

                i--;
            }
            else if (SynthNum < AudioSources.Count)
            {
                Destroy(AudioSources[AudioSources.Count-1]);
                i++;
            }
            else
            {
                break;
            }
        }



    }
}
