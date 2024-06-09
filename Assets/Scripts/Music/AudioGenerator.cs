using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;
using Unity.Jobs;
using Unity.Burst;
using Unity.Mathematics;
using MusicNamespace;

public class AudioGenerator : MonoBehaviour
{
    //[SerializeField, Range(0, 1)] private float amplitude = 0.5f;
    //[SerializeField] private float frequency = 261.62f; // middle C

    [SerializeField]
    private SliderMono WaveShapeSlider;

    [HideInInspector]
    public LineRenderer OscillatorLine;

    [HideInInspector]
    public Entity WeaponSynthEntity;

    private EntityManager entityManager;

    private float amplitude;
    private float frequency;

    private float SinFactor;
    private float SawFactor;
    private float SquareFactor;

    private bool audiojobCompleted;

    private NativeArray<float> _audioData;
    //no need to be a nativearry ?
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
        audiojobCompleted = false;
    }


    private void Update()
    {

        /*UPDATE UI DISPLAY*/
        if(audiojobCompleted)
        {
            //reasign the number of points
            int points = _audioData.Length / 32;
            OscillatorLine.positionCount = points;
            float xStart = 0;
            float xFinish = 2 * Mathf.PI*2;

            //SynthData synth = entityManager.GetComponentData<SynthData>(WeaponSynthEntity);


            for (int currentPoint = 0; currentPoint < points; currentPoint++)
            {


                //float value = Mathf.Sin((float)_audioPhase[0] * 2 * Mathf.PI) * 1f;

                float progress = (float)currentPoint / (points - 1);
                float x = Mathf.Lerp(xStart, xFinish, progress);
                float y = ((MusicUtils.Sin(x)*SinFactor) + (MusicUtils.Saw(x)*SawFactor) + (MusicUtils.Square(x)*SquareFactor)) * amplitude * 6f;
                OscillatorLine.SetPosition(currentPoint, new Vector3(x, y, 0));

                //float progress = (float)currentPoint / (points - 1);
                //float x = Mathf.Lerp(xStart, xFinish, progress);
                //float y = _audioData[currentPoint]*6f;
                //OscillatorLine.SetPosition(currentPoint, new Vector3(x, y, 0));


            }
            audiojobCompleted = false;
        }

        

    }

    // Callback method for audio processing
    unsafe
    private void OnAudioFilterRead(float[] data, int channels)
    {

        amplitude = entityManager.GetComponentData<SynthData>(WeaponSynthEntity).amplitude;
        frequency = entityManager.GetComponentData<SynthData>(WeaponSynthEntity).frequency;

        SinFactor = entityManager.GetComponentData<SynthData>(WeaponSynthEntity).SinFactor;
        SawFactor = entityManager.GetComponentData<SynthData>(WeaponSynthEntity).SawFactor;
        SquareFactor = entityManager.GetComponentData<SynthData>(WeaponSynthEntity).SquareFactor;

        //Debug.Log(SinFactor);
        //Debug.Log(SawFactor);
        //Debug.Log(SquareFactor);

        AudioJob audioJob = new AudioJob(
            amplitude,
            frequency,
            SinFactor,
            SawFactor,
            SquareFactor,
            _sampleRate,
            _audioPhase,
            _audioData
            );
        _jobhandle = audioJob.Schedule();
        _jobhandle.Complete();

        _audioData.CopyTo(data);

        audiojobCompleted = true;
        
    }

    private void OnDestroy()
    {
        _audioData.Dispose();
        _audioPhase.Dispose();
    }

    void UpdateOscillatorDisplay()
    {

    }


}

[BurstCompile]
public struct AudioJob : IJob
{

    private float _amplitude;
    private float _frequency;

    private float _SinFactor;
    private float _SawFactor;
    private float _SquareFactor;

    private float _sampleRate;
    //private double _phase;
    //?[DeallocateOnJobCompletion]
    private NativeArray<double> _audioPhase;
    //?[DeallocateOnJobCompletion]
    private NativeArray<float> _audioData;

    public AudioJob(
       float amplitude,
       float frequency,
       float SinFactor,
       float SawFactor,
       float SquareFactor,
       float sampleRate,
       //float phase,
        NativeArray<double> audioPhase,
        NativeArray<float> audioData
       )
    {
        _amplitude = amplitude;
        _frequency = frequency;
        _SinFactor = SinFactor;
        _SawFactor = SawFactor;
        _SquareFactor = SquareFactor;
        _sampleRate = sampleRate;
        _audioPhase = audioPhase;
        _audioData = audioData;
    }

    public void Execute()
    {

        //_audioPhase[0] = 0.5;

        int tempChannels = 2;

        double phaseIncrement = _frequency / _sampleRate;

        for (int sample = 0; sample < _audioData.Length; sample += tempChannels)
        {
            // get value of phase on a sine wave
            float value = ((MusicUtils.Sin((float)_audioPhase[0])*_SinFactor) + (MusicUtils.Saw((float)_audioPhase[0])*_SawFactor) + (MusicUtils.Square((float)_audioPhase[0])*_SquareFactor)) * _amplitude;
            //float value = Mathf.Sin((float)_audioPhase[0] * 2 * Mathf.PI) * _amplitude;

            // increment _phase value for next iteration
            _audioPhase[0] = (_audioPhase[0] + phaseIncrement) % 1;

            //_phase = 1f;

            // populate all channels with the values
            for (int channel = 0; channel < tempChannels; channel++)
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
