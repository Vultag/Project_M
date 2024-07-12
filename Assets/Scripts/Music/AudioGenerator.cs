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
        //Debug.Log("test");

        //amplitude = entityManager.GetComponentData<SynthData>(WeaponSynthEntity).amplitude;
        //frequency = entityManager.GetComponentData<SynthData>(WeaponSynthEntity).frequency;

        //SinFactor = entityManager.GetComponentData<SynthData>(WeaponSynthEntity).SinFactor;
        //SawFactor = entityManager.GetComponentData<SynthData>(WeaponSynthEntity).SawFactor;
        //SquareFactor = entityManager.GetComponentData<SynthData>(WeaponSynthEntity).SquareFactor;

        //Debug.Log(SinFactor);
        //Debug.Log(SawFactor);
        //Debug.Log(SquareFactor);

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

        //JobHandle testjob = audioJob.Schedule(_Audiojobhandle);
        _Audiojobhandle = audioJob.Schedule(_Audiojobhandle);

        //JobHandle.CombineDependencies(_Audiojobhandle,);

        _Audiojobhandle.Complete();
        //testjob.Complete();

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
        //_audioPhase.Dispose();
    }

    void UpdateOscillatorDisplay()
    {

    }


}

[BurstCompile]
public struct AudioJob : IJob
{

    //private float _amplitude;
    //private float _frequency;

    //private float _SinFactor;
    //private float _SawFactor;
    //private float _SquareFactor;

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
       //float amplitude,
       //float frequency,
       //float SinFactor,
       //float SawFactor,
       //float SquareFactor,
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
        //_amplitude = amplitude;
        //_frequency = frequency;
        //_SinFactor = SinFactor;
        //_SawFactor = SawFactor;
        //_SquareFactor = SquareFactor;
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
        //var systemState = World.DefaultGameObjectInjectionWorld.GetExistingSystem<WeaponSystem>();

        //_audioPhase[0] = 0.5;

        int tempChannels = 2;


        ADSREnvelope ADSR = _synthdata.ADSR;

        /// need Synth ADSR
        /// - Make buffer of active keys containing [ Delta (0,1(depends on  synth parameter)) ; frequency ] (in a struct ?)
        /// ->
        //Debug.Log(_synthdata.Delta);

        //load the key buffer in the job ?
        //foreach

        //phases for each key playing
        NativeArray<float> phaseIncrement = new NativeArray<float>(_SustainedKeyBuffer.Length+_ReleasedKeyBuffer.Length+1,Allocator.Temp);

        //NativeArray<float> SenvelopeStage = new NativeArray<float>(_SustainedKeyBuffer.Length, Allocator.Temp);
        NativeArray<float> Samplitude = new NativeArray<float>(_SustainedKeyBuffer.Length, Allocator.Temp);
        NativeArray<float> Ramplitude = new NativeArray<float>(_ReleasedKeyBuffer.Length, Allocator.Temp);

        NativeArray<float> frequencies = new NativeArray<float>(_SustainedKeyBuffer.Length + _ReleasedKeyBuffer.Length + 1, Allocator.Temp);

        //NativeArray<double> phaseIncrement = new NativeArray<double>(1, Allocator.Temp);





        ///THIS 
        /// Smooth  out the amplitude changes in between the ADSR informations updates
        ///amplitudeSmoothingFactor size -> number of sample over which smoothing occurs (needs to be multiple of 4/8/32?)
        int ASFsize = 128*6;
        //int rampSamples = 128; // Number of samples to ramp amplitude changes
        // Amplitude smoothing sample count

        //phaseIncrement[0] = _synthdata.frequency / _sampleRate;
        //double phaseIncrement = _synthdata.frequency / _sampleRate;



        //Debug.Log(effectiveAmplitude);

        for (int i = 0; i < _SustainedKeyBuffer.Length; i++)
        {
            frequencies[i] = MusicUtils.noteToFrequency(MusicUtils.radiansToNote(Mathf.Abs(PhysicsUtilities.DirectionToRadians(_SustainedKeyBuffer[i].Direction))), WeaponSystem.mode);
            phaseIncrement[i] = frequencies[i] / _sampleRate;

            //SenvelopeStage[i] = _SustainedKeyBuffer[i].Delta < ADSR.Attack ? 1 : 0;
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

            //_previousAmplitude[0] += Samplitude[i];
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

            //_previousAmplitude[0] += Ramplitude[i];
            JobPhases[y] = _ReleasedKeyBuffer[i].Phase;
            _newAmplitudes[y] = Ramplitude[i];
        }

        //delete ?
        //for (int i = 0; i < _SustainedKeyBuffer.Length; i++)
        //{
        //    if (Samplitude[i] > _previousAmplitude[0])
        //        _previousAmplitude[0] = Samplitude[i];
        //}
        //for (int i = 0; i < _ReleasedKeyBuffer.Length; i++)
        //{
        //    if (Ramplitude[i] > _previousAmplitude[0])
        //        _previousAmplitude[0] = Ramplitude[i];
        //}
        //if(_SustainedKeyBuffer.Length>0)
        //{
        //    Debug.LogError(Samplitude[0]);
        //    Debug.Log(_SustainedKeyBuffer[0].currentAmplitude);
        //}

        //Debug.Log(_SustainedKeyBuffer[0].Delta);

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

                //audiobuffer.Insert(sample, value);
                _audioData[sample + channel] = value;

            }
        }

        for (int sample = ASFsize; sample < _audioData.Length; sample += tempChannels)
        {
            /// float value = 0;
            ///for each Key in KeyBuffer []
            /// {
            /// value += (sin/saw/square)
            /// * Amplitude * (Filter & Key.Delta)
            /// }
            /// _audioData[sample + channel] = value;
            /// 


            float value = 0;
            //i = 0;
            for (int i = 0; i < _SustainedKeyBuffer.Length; i++)
            {
                float phase = JobPhases[i];
                ///OPTI
                //float envelopeStage = _SustainedKeyBuffer[i].Delta < ADSR.Attack ? 1 : 0;
                //float amplitude = effectiveAmplitude * (((_SustainedKeyBuffer[i].Delta / ADSR.Attack) * envelopeStage) + ((1- Mathf.Clamp((_SustainedKeyBuffer[i].Delta - ADSR.Attack) / ADSR.Decay,0f,1-ADSR.Sustain))) * (1 - envelopeStage));
                //_previousAmplitude[0] += amplitude;
                //float amplitude = 0.5f;
                float amplitude = Samplitude[i];
                value += ((MusicUtils.Sin(phase) * _synthdata.SinFactor) + (MusicUtils.Saw(phase) * _synthdata.SawFactor) + (MusicUtils.Square(phase) * _synthdata.SquareFactor)) * amplitude;
                JobPhases[i] = (JobPhases[i] + phaseIncrement[i]) % 1;
            }
            for (int y = 0; y < _ReleasedKeyBuffer.Length; y++)
            {
                int i = y + _SustainedKeyBuffer.Length;
                float phase = JobPhases[i];
                //float amplitude = effectiveAmplitude * (1 - (_ReleasedKeyBuffer[y].Delta / ADSR.Release));
                //_previousAmplitude[0] += amplitude;

                float amplitude = Ramplitude[y];
                value += ((MusicUtils.Sin(phase) * _synthdata.SinFactor) + (MusicUtils.Saw(phase) * _synthdata.SawFactor) + (MusicUtils.Square(phase) * _synthdata.SquareFactor)) * amplitude;
                JobPhases[i] = (JobPhases[i] + phaseIncrement[i]) % 1;
            }


            ////float phase = (float)_audioPhase[i];
            ////value = MusicUtils.Sin(phase);

            ////_audioPhase[0] = (_audioPhase[0] + phaseIncrement[0]) % 1;

            ///float envelopeStage = _synthdata.Delta < ADSR.Attack ? 1 : 0;
            //Apply envelope here
            ///float amplitude = _synthdata.amplitude * (((_synthdata.Delta / ADSR.Attack) * envelopeStage) + (ADSR.Decay / _synthdata.Delta) * (1 - envelopeStage));
            // get value of phase on a sine wave
            ///float value = ((MusicUtils.Sin((float)_audioPhase[0])* _synthdata.SinFactor) + (MusicUtils.Saw((float)_audioPhase[0])* _synthdata.SawFactor) + (MusicUtils.Square((float)_audioPhase[0])* _synthdata.SquareFactor)) * amplitude;

            ///value = value*_synthdata.Attack

            //float value = Mathf.Sin((float)_audioPhase[0] * 2 * Mathf.PI) * _amplitude;

            // increment _phase value for next iteration
            //_audioPhase[0] = (_audioPhase[0] + phaseIncrement[0]) % 1;

            //_phase = 1f;


            // populate all channels with the values
            for (int channel = 0; channel < tempChannels; channel++)
            {

                //audiobuffer.Insert(sample, value);
                _audioData[sample + channel] = value;

            }
        }

        ///// Write back the phases to the buffers elements for next iteration
        //for (int i = 0; i < _SustainedKeyBuffer.Length; i++)
        //{
        //    _SustainedKeyBuffer[i] = new SustainedKeyBufferData { Delta = _SustainedKeyBuffer[i].Delta, frequency = _SustainedKeyBuffer[i].frequency, Phase = JobPhases[i] };
        //}
        //for (int y = _SustainedKeyBuffer.Length; y < _ReleasedKeyBuffer.Length + _SustainedKeyBuffer.Length; y++)
        //{
        //    int i = y - _SustainedKeyBuffer.Length;
        //    _ReleasedKeyBuffer[i] = new ReleasedKeyBufferData { Delta = _ReleasedKeyBuffer[i].Delta, frequency = _ReleasedKeyBuffer[i].frequency, Phase = JobPhases[y] };
        //}

        //_previousAmplitude[0] = ;


        ///increment the delta of each KeyBufferElement
        ///if the delta goes over 1 -> remove the element



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
