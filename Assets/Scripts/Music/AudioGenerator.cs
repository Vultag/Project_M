using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;
using Unity.Jobs;
using Unity.Burst;
using Unity.Mathematics;

public class AudioGenerator : MonoBehaviour
{
    //[SerializeField, Range(0, 1)] private float amplitude = 0.5f;
    //[SerializeField] private float frequency = 261.62f; // middle C
    [HideInInspector]
    public Entity WeaponSynthEntity;

    private EntityManager entityManager;

    private float amplitude;
    private float frequency;


    private NativeArray<float> _audioData;
    private NativeArray<double> _audioPhase;
    private JobHandle _jobhandle;
    private int _sampleRate;



    private const int NumChannels = 1; // Mono audio

    private void Start()
    {
        entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;

    }

    private void Awake()
    {
        _sampleRate = AudioSettings.outputSampleRate;
        _audioData = new NativeArray<float>(2048,Allocator.Persistent);
        _audioPhase = new NativeArray<double>(1, Allocator.Persistent);
    }


    private void Update()
    {

    }

    // Callback method for audio processing
    unsafe
    private void OnAudioFilterRead(float[] data, int channels)
    {

        amplitude = entityManager.GetComponentData<SynthData>(WeaponSynthEntity).amplitude;
        frequency = entityManager.GetComponentData<SynthData>(WeaponSynthEntity).frequency;

        AudioJob audioJob = new AudioJob(
            amplitude,
            frequency,
            _sampleRate,
            _audioPhase,
            _audioData
            );
        _jobhandle = audioJob.Schedule();
        _jobhandle.Complete();

        _audioData.CopyTo(data);


    }

    private void OnDestroy()
    {
        _audioData.Dispose();
        _audioPhase.Dispose();
    }


}

[BurstCompile]
public struct AudioJob : IJob
{

    private float _amplitude;
    private float _frequency;
    private float _sampleRate;
    //private double _phase;
    //?[DeallocateOnJobCompletion]
    private NativeArray<double> _audioPhase;
    //?[DeallocateOnJobCompletion]
    private NativeArray<float> _audioData;

    public AudioJob(
       float amplitude,
       float frequency,
       float sampleRate,
       //float phase,
        NativeArray<double> audioPhase,
        NativeArray<float> audioData
       )
    {
        _amplitude = amplitude;
        _frequency = frequency;
        _sampleRate = sampleRate;
        _audioPhase = audioPhase;
        _audioData = audioData;
    }

    public void Execute()
    {

        //_audioPhase[0] = 0.5;

        double phaseIncrement = _frequency / _sampleRate;

        for (int sample = 0; sample < _audioData.Length; sample += 1)
        {
            // get value of phase on a sine wave
            float value = Mathf.Sin((float)_audioPhase[0] * 2 * Mathf.PI) * _amplitude;

            // increment _phase value for next iteration
            _audioPhase[0] = (_audioPhase[0] + phaseIncrement) % 1;

            //_phase = 1f;

            // populate all channels with the values
            for (int channel = 0; channel < 1; channel++)
            {

                //audiobuffer.Insert(sample, value);
                _audioData[sample + channel] = value;

            }
        }






        //double phaseIncrement = _frequency / _sampleRate;

        //for (int sample = 0; sample < _audioData.Length; sample += 2)
        //{
        //    // get value of phase on a sine wave
        //    float value = Mathf.Sin((float)_phase * 2 * Mathf.PI) * _amplitude;

        //    // increment _phase value for next iteration
        //    //_phase = (_phase + phaseIncrement) % 1;
        //    _phase = 0.5;

        //    // populate all channels with the values
        //    for (int channel = 0; channel < 2; channel++)
        //    {
        //        _audioData[sample + channel] = value;
        //    }
        //}


    }

}
