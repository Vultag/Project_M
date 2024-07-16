using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;
using Unity.Jobs;
using Unity.Burst;
using Unity.Mathematics;
using MusicNamespace;
using Unity.Entities.UniversalDelegates;
using static UnityEngine.Rendering.DebugUI;

public class AudioGenerator : MonoBehaviour
{
    //[SerializeField, Range(0, 1)] private float amplitude = 0.5f;
    //[SerializeField] private float frequency = 261.62f; // middle C

    //[SerializeField]
    //private SliderMono WaveShapeSlider;

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
    //public static NativeArray<double> _audioPhase;

    public static JobHandle _Audiojobhandle;// { get; private set; }

    private int _sampleRate;
    private NativeArray<float> _previousAmplitudes;



    //public DynamicBuffer<KeyData> KeyBuffer;




    private const int NumChannels = 1; // Mono audio

    private void Start()
    {
        entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;

    }

    private void Awake()
    {
        _sampleRate = AudioSettings.outputSampleRate;
        _audioData = new NativeArray<float>(2048,Allocator.Persistent);
        //_previousAmplitude = new NativeArray<float>(1, Allocator.Persistent);
        // max 12 keys at a time before it has to reallocate
        //_audioPhase = new NativeArray<double>(12, Allocator.Persistent);
        audiojobCompleted = false;
    }


    private void Update()
    {

        /*UPDATE UI DISPLAY*/
        if (audiojobCompleted)
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

        var SKeyBufferArray = entityManager.GetBuffer<SustainedKeyBufferData>(WeaponSynthEntity).ToNativeArray(Allocator.TempJob);
        var RKeyBufferArray = entityManager.GetBuffer<ReleasedKeyBufferData>(WeaponSynthEntity).ToNativeArray(Allocator.TempJob);


        NativeArray<float> JobPhases = new NativeArray<float>(SKeyBufferArray.Length + RKeyBufferArray.Length+1, Allocator.TempJob);
        NativeArray<float> newAmplitudes = new NativeArray<float>(SKeyBufferArray.Length + RKeyBufferArray.Length+1, Allocator.TempJob);


        AudioJob audioJob = new AudioJob(
            _sampleRate,
            entityManager.GetComponentData<SynthData>(WeaponSynthEntity),
            SKeyBufferArray,
            RKeyBufferArray,
            JobPhases,
            //_audioPhase,
            _audioData,
            newAmplitudes
            );


        _Audiojobhandle = audioJob.Schedule(_Audiojobhandle);


        _Audiojobhandle.Complete();

        _audioData.CopyTo(data);

        //get the reference again to prevent unvalidating USELESS ?
        var SKeyBuffer = entityManager.GetBuffer<SustainedKeyBufferData>(WeaponSynthEntity);
        var RKeyBuffer = entityManager.GetBuffer<ReleasedKeyBufferData>(WeaponSynthEntity);

        /// Write back the phases to the buffers elements for next iteration
        for (int i = 0; i < SKeyBuffer.Length; i++)
        {
            SKeyBuffer[i] = new SustainedKeyBufferData { Delta = SKeyBuffer[i].Delta, Direction = SKeyBuffer[i].Direction, Phase = JobPhases[i], currentAmplitude = newAmplitudes[i] };
        }
        for (int y = SKeyBuffer.Length; y < RKeyBuffer.Length + SKeyBuffer.Length; y++)
        {
            int i = y - SKeyBuffer.Length;
            RKeyBuffer[i] = new ReleasedKeyBufferData { Delta = RKeyBuffer[i].Delta, Direction = RKeyBuffer[i].Direction, Phase = JobPhases[y],currentAmplitude = newAmplitudes[y] };
        }
        JobPhases.Dispose();
        newAmplitudes.Dispose();

        audiojobCompleted = true;
        
    }

    private void OnDestroy()
    {
        _audioData.Dispose();
    }

    void UpdateOscillatorDisplay()
    {

    }


}

[BurstCompile]
public struct AudioJob : IJob
{


    private float _sampleRate;
    private SynthData _synthdata;

    [DeallocateOnJobCompletion]
    private NativeArray<SustainedKeyBufferData> _SustainedKeyBuffer;

    [DeallocateOnJobCompletion]
    private NativeArray<ReleasedKeyBufferData> _ReleasedKeyBuffer;

    //DynamicBuffer<SustainedKeyBufferData> _SustainedKeyBuffer;

    //DynamicBuffer<ReleasedKeyBufferData> _ReleasedKeyBuffer;

    //private double _phase;
    //?[DeallocateOnJobCompletion]
    private NativeArray<float> JobPhases;
    //?[DeallocateOnJobCompletion]
    private NativeArray<float> _audioData;
    [WriteOnly]
    private NativeArray<float> _newAmplitudes;

    public AudioJob(
       float sampleRate,
       SynthData synthdata,
         NativeArray<SustainedKeyBufferData> SustainedKeyBuffer,
         NativeArray<ReleasedKeyBufferData> ReleasedKeyBuffer,
        //DynamicBuffer<SustainedKeyBufferData> SustainedKeyBuffer,
        //DynamicBuffer<ReleasedKeyBufferData> ReleasedKeyBuffer,
        //float phase,
        NativeArray<float> audioPhase,
        NativeArray<float> audioData,
        NativeArray<float> newAmplitudes
       )
    {
        _synthdata = synthdata;
        _SustainedKeyBuffer = SustainedKeyBuffer;
        _ReleasedKeyBuffer = ReleasedKeyBuffer;
        _sampleRate = sampleRate;
        JobPhases = audioPhase;
        //_audioPhase = audioPhase;
        _audioData = audioData;
        _newAmplitudes = newAmplitudes;
    }

    public void Execute()
    {

        int tempChannels = 2;

        ADSREnvelope ADSR = _synthdata.ADSR;

        //phases for each key playing
        NativeArray<float> phaseIncrement = new NativeArray<float>(_SustainedKeyBuffer.Length+_ReleasedKeyBuffer.Length+1,Allocator.Temp);

        NativeArray<float> Samplitude = new NativeArray<float>(_SustainedKeyBuffer.Length, Allocator.Temp);
        NativeArray<float> Ramplitude = new NativeArray<float>(_ReleasedKeyBuffer.Length, Allocator.Temp);

        NativeArray<float> frequencies = new NativeArray<float>(_SustainedKeyBuffer.Length + _ReleasedKeyBuffer.Length + 1, Allocator.Temp);


        /// Smooth  out the amplitude changes in between the ADSR informations updates
        ///amplitudeSmoothingFactor size -> number of sample over which smoothing occurs (needs to be multiple of 4/8/32?)
        int ASFsize = 128*6;
  
        //Debug.Log(effectiveAmplitude);

        for (int i = 0; i < _SustainedKeyBuffer.Length; i++)
        {
            frequencies[i] = MusicUtils.noteToFrequency(MusicUtils.radiansToNote(Mathf.Abs(PhysicsUtilities.DirectionToRadians(_SustainedKeyBuffer[i].Direction))), WeaponSystem.mode);
            phaseIncrement[i] = frequencies[i] / _sampleRate;

            if(_SustainedKeyBuffer[i].Delta < ADSR.Attack)
            {
                if (ADSR.Attack == 0)
                    Samplitude[i] = _synthdata.amplitude * 1f;
                else
                    Samplitude[i] = _synthdata.amplitude * Mathf.Clamp((_SustainedKeyBuffer[i].Delta / ADSR.Attack),0,1f);
            }
            else
            {
                if (ADSR.Decay == 0)
                    Samplitude[i] = _synthdata.amplitude * ADSR.Sustain;
                else
                    Samplitude[i] = _synthdata.amplitude * (1 - (1- ADSR.Sustain)* Mathf.Clamp(((_SustainedKeyBuffer[i].Delta - ADSR.Attack) / ADSR.Decay), 0, 1f));
            }

            JobPhases[i] = _SustainedKeyBuffer[i].Phase;
            _newAmplitudes[i] = Samplitude[i];
        }
        for (int y = _SustainedKeyBuffer.Length; y < _ReleasedKeyBuffer.Length + _SustainedKeyBuffer.Length; y++)
        {
            int i = y - _SustainedKeyBuffer.Length;
            frequencies[y] = MusicUtils.noteToFrequency(MusicUtils.radiansToNote(Mathf.Abs(PhysicsUtilities.DirectionToRadians(_ReleasedKeyBuffer[i].Direction))), WeaponSystem.mode);
            phaseIncrement[y] = frequencies[y] / _sampleRate;

            if (ADSR.Release == 0)
                Ramplitude[i] = 0;
            else
                Ramplitude[i] = _synthdata.amplitude * (1-Mathf.Clamp(_ReleasedKeyBuffer[i].Delta / ADSR.Release, 0, 1f));

            JobPhases[y] = _ReleasedKeyBuffer[i].Phase;
            _newAmplitudes[y] = Ramplitude[i];
        }



        for (int sample = 0; sample < ASFsize; sample += tempChannels)
        {

            float ASF = (float)sample / (float)ASFsize;// Mathf.Clamp(sample / ASFsize,0,1f); ??
            float value = 0;

            //Debug.LogError(sample);
            //Debug.Log(ASFsize);
            //Debug.LogWarning(sample / ASFsize);

            for (int i = 0; i < _SustainedKeyBuffer.Length; i++)
            {
                float phase = JobPhases[i];

                float amplitude = (Samplitude[i] * ASF) + (_SustainedKeyBuffer[i].currentAmplitude * (1 - ASF)); 
                //Debug.LogError(Samplitude[i]);
                //Debug.Log(_SustainedKeyBuffer[i].currentAmplitude);
                //Debug.LogWarning(amplitude);

                value += ((MusicUtils.Sin(phase) * _synthdata.SinFactor) + (MusicUtils.Saw(phase) * _synthdata.SawFactor) + (MusicUtils.Square(phase) * _synthdata.SquareFactor)) * amplitude;
                JobPhases[i] = (JobPhases[i] + phaseIncrement[i]) % 1;
            }
            for (int y = 0; y < _ReleasedKeyBuffer.Length; y++)
            {
                int i = y + _SustainedKeyBuffer.Length;
                float phase = JobPhases[i];
  
                float amplitude = (Ramplitude[y] *ASF) + (_ReleasedKeyBuffer[y].currentAmplitude * (1-ASF));
                value += ((MusicUtils.Sin(phase) * _synthdata.SinFactor) + (MusicUtils.Saw(phase) * _synthdata.SawFactor) + (MusicUtils.Square(phase) * _synthdata.SquareFactor)) * amplitude;
                JobPhases[i] = (JobPhases[i] + phaseIncrement[i]) % 1;
            }


            // populate all channels with the values
            for (int channel = 0; channel < tempChannels; channel++)
            {
                _audioData[sample + channel] = value;
            }
        }

        for (int sample = ASFsize; sample < _audioData.Length; sample += tempChannels)
        {

            float value = 0;
            //i = 0;
            for (int i = 0; i < _SustainedKeyBuffer.Length; i++)
            {
                float phase = JobPhases[i];
        
                float amplitude = Samplitude[i];
                value += ((MusicUtils.Sin(phase) * _synthdata.SinFactor) + (MusicUtils.Saw(phase) * _synthdata.SawFactor) + (MusicUtils.Square(phase) * _synthdata.SquareFactor)) * amplitude;
                JobPhases[i] = (JobPhases[i] + phaseIncrement[i]) % 1;
            }
            for (int y = 0; y < _ReleasedKeyBuffer.Length; y++)
            {
                int i = y + _SustainedKeyBuffer.Length;
                float phase = JobPhases[i];

                float amplitude = Ramplitude[y];
                value += ((MusicUtils.Sin(phase) * _synthdata.SinFactor) + (MusicUtils.Saw(phase) * _synthdata.SawFactor) + (MusicUtils.Square(phase) * _synthdata.SquareFactor)) * amplitude;
                JobPhases[i] = (JobPhases[i] + phaseIncrement[i]) % 1;
            }


            // populate all channels with the values
            for (int channel = 0; channel < tempChannels; channel++)
            {

                //audiobuffer.Insert(sample, value);
                _audioData[sample + channel] = value;

            }
        }

    }

}
