using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;
using Unity.Jobs;
using Unity.Burst;
using Unity.Mathematics;
using MusicNamespace;
using System.Linq;
using static UnityEngine.Rendering.DebugUI;
using System;
using UnityEditor;

public class AudioGenerator : MonoBehaviour
{
    //const int DSPbufferSize = 512;

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

    public static JobHandle _Audiojobhandle;

    private int _sampleRate;
    private NativeArray<KeyData> activeKeys;
    private NativeArray<int> activeKeyNumber;

    public static AudioRingBuffer<KeysBuffer> audioRingBuffer;


    private const int NumChannels = 1; // Mono audio

    private void Start()
    {
        entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;
        //Debug.Log(AudioSettings.outputSampleRate);

    }

    private void Awake()
    {
        _sampleRate = AudioSettings.outputSampleRate;
        _audioData = new NativeArray<float>(512, Allocator.Persistent);
        audiojobCompleted = false;
        int ringBufferCapacity = 4;
        audioRingBuffer = new AudioRingBuffer<KeysBuffer>(ringBufferCapacity);
        audioRingBuffer.InitializeBuffer(ringBufferCapacity);

        activeKeys = new NativeArray<KeyData>(12, Allocator.Persistent);
        activeKeyNumber = new NativeArray<int>(1, Allocator.Persistent);
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


            for (int currentPoint = 0; currentPoint < points; currentPoint++)
            {

                float progress = (float)currentPoint / (points - 1);
                float x = Mathf.Lerp(xStart, xFinish, progress);
                float y = ((MusicUtils.Sin(x)*SinFactor) + (MusicUtils.Saw(x)*SawFactor) + (MusicUtils.Square(x)*SquareFactor)) * amplitude * 6f;
                OscillatorLine.SetPosition(currentPoint, new Vector3(x, y, 0));

            }
            audiojobCompleted = false;
        }

        

    }

    // Callback method for audio processing
    unsafe
    private void OnAudioFilterRead(float[] data, int channels)
    {

        KeysBuffer keysBuffer;

        if (audioRingBuffer.IsEmpty)
        {
            /// Buffer is empty. Recycle the latest available data.
            keysBuffer = audioRingBuffer.RecycleLastElement();
        }
        else
        {
            keysBuffer = audioRingBuffer.Read();
        }

        var synthData = entityManager.GetComponentData<SynthData>(WeaponSynthEntity);


        for (int i = 0; i < activeKeyNumber[0]; i++)
        {

            if (keysBuffer.keyFrenquecies.Contains(activeKeys[i].frequency))
            {
                ///Key already being released, check if repressed
                if (activeKeys[i].amplitudeAtRelease != 0)
                {
                    activeKeys[i] = new KeyData { frequency = activeKeys[i].frequency, delta = 0f, phase = activeKeys[i].phase, amplitudeAtRelease = 0 };
                }
            }
            else
            {

                if (activeKeys[i].amplitudeAtRelease != 0)
                    continue;

                float releaseAmplitude;
                if (activeKeys[i].delta < synthData.ADSR.Attack)
                    releaseAmplitude = (activeKeys[i].delta / synthData.ADSR.Attack) * synthData.amplitude;
                else if (activeKeys[i].delta < synthData.ADSR.Attack + synthData.ADSR.Decay)
                    releaseAmplitude = synthData.amplitude - (((activeKeys[i].delta - synthData.ADSR.Attack) / synthData.ADSR.Decay) * (1 - synthData.ADSR.Sustain) * synthData.amplitude);
                else
                    releaseAmplitude = synthData.ADSR.Sustain * synthData.amplitude;

                activeKeys[i] = new KeyData { frequency = activeKeys[i].frequency, delta = synthData.ADSR.Attack+ synthData.ADSR.Decay, phase = activeKeys[i].phase, amplitudeAtRelease = releaseAmplitude +0.00001f /*make sure the key is considered released*/ };
            }
        }
        int overwriteKeysNum = 0;
        for (int i = 0; i < keysBuffer.KeyNumber; i++)
        {
            if (!activeKeys.Any(activeKeys=> activeKeys.frequency == keysBuffer.keyFrenquecies[i]))
            {
                if (activeKeyNumber[0] < 12)
                {
                    activeKeys[activeKeyNumber[0]] = new KeyData { frequency = keysBuffer.keyFrenquecies[i]};
                    activeKeyNumber[0]++; 
                }
                /// the number of keys played simultaneously has reached its limit : start overwriting the oldest ones.
                else
                {
                    Debug.Log("overwrite");
                    activeKeys[overwriteKeysNum] = new KeyData { frequency = keysBuffer.keyFrenquecies[i]};
                    overwriteKeysNum++;
                }
            }
        }

        if (activeKeyNumber[0] < 1)
            return;

        ///activeKeys elemets cropped, rdy to be processed
        NativeArray<float> _JobFrequencies = new NativeArray<float>(activeKeyNumber[0], Allocator.TempJob);
        NativeArray<float> _JobPhases = new NativeArray<float>(activeKeyNumber[0], Allocator.TempJob);
        NativeArray<float> _JobDeltas = new NativeArray<float>(activeKeyNumber[0], Allocator.TempJob);
        for (int i = 0; i < activeKeyNumber[0]; i++)
        {
            _JobFrequencies[i] = activeKeys[i].frequency;
            _JobPhases[i] = activeKeys[i].phase;
            _JobDeltas[i] = activeKeys[i].delta;
        }

        AudioJob audioJob = new AudioJob(
            _sampleRate,
            entityManager.GetComponentData<SynthData>(WeaponSynthEntity),
            activeKeys,
            _JobFrequencies,
            _JobPhases,
            _JobDeltas,
            _audioData,
            activeKeyNumber
            );


        _Audiojobhandle = audioJob.Schedule(_Audiojobhandle);

        _Audiojobhandle.Complete();

        _audioData.CopyTo(data);

        var sortedKeys = activeKeys.ToArray().OrderByDescending(Data => Data.frequency).ToArray();

        /// Copy the sorted data back to the activeKeys
        for (int i = 0; i < activeKeys.Length; i++)
        {
            activeKeys[i] = sortedKeys[i];
        }

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

    private NativeArray<KeyData> _KeyData;


    [ReadOnly]
    [DeallocateOnJobCompletion]
    private NativeArray<float> _JobFrequencies;
    [DeallocateOnJobCompletion]
    private NativeArray<float> _JobPhases;
    [DeallocateOnJobCompletion]
    private NativeArray<float> _JobDeltas;

    //[WriteOnly]
    private NativeArray<float> _audioData;
    [WriteOnly]
    private NativeArray<int> _activeKeynum;

    public AudioJob(
       float sampleRate,
       SynthData synthdata,
       NativeArray<KeyData> KeyData,


        NativeArray<float> JobFrequencies,
        NativeArray<float> JobPhases,
        NativeArray<float> JobDeltas,
        NativeArray<float> audioData,
        NativeArray<int> activeKeynum
       )
    {
        _synthdata = synthdata;
        _KeyData = KeyData;
        _sampleRate = sampleRate;
        _JobFrequencies = JobFrequencies;
        _JobPhases = JobPhases;
        _JobDeltas = JobDeltas;
        _audioData = audioData;
        _activeKeynum = activeKeynum;
    }

    public void Execute()
    {
        float deltaIncrement = 1 / _sampleRate;

        /// OPTI : SIMD ?
        /// I increment the audioData, so I need to reset it at the start of each DSPbuffer
        for (int i = 0; i < _audioData.Length; i++)
        {
            _audioData[i] = 0;
        }

        int ChannelsNum = 2;

        ADSREnvelope ADSR = _synthdata.ADSR;

        NativeArray<ADSRlayouts> KeysADSRlayouts = new NativeArray<ADSRlayouts>(_JobFrequencies.Length,Allocator.Temp);

        for (int i = 0; i < KeysADSRlayouts.Length; i++)
        {

            int samplePool = 512;
            int attackSamples = 0;
            int decaySamples = 0;
            int sustainSamples = 0;
            int releaseSamples = 0;

            if (_KeyData[i].amplitudeAtRelease == 0)
            {
                attackSamples = _KeyData[i].delta < ADSR.Attack ? Mathf.Min(Mathf.CeilToInt(Mathf.Abs(_KeyData[i].delta - ADSR.Attack) * (_sampleRate)), samplePool) : 0;
                samplePool -= attackSamples;
                decaySamples = samplePool > 0 && _KeyData[i].delta < (ADSR.Attack+ADSR.Decay) ? Mathf.Min(Mathf.CeilToInt(Mathf.Abs(Mathf.Max(_KeyData[i].delta,ADSR.Attack)- (ADSR.Attack + ADSR.Decay)) * (_sampleRate)), samplePool) : 0;
                samplePool -= decaySamples;
                sustainSamples = samplePool;
            }
            else
            {
                releaseSamples = Mathf.Min(Mathf.CeilToInt(Mathf.Abs(_KeyData[i].delta - (ADSR.Attack + ADSR.Decay + ADSR.Release)) * (_sampleRate)), samplePool);
            }

            KeysADSRlayouts[i] = new ADSRlayouts {
                AttackSamples = attackSamples,
                DecaySamples = decaySamples+ attackSamples,
                SustainSamples = sustainSamples+ decaySamples+ attackSamples,
                ReleaseSamples = releaseSamples

            };

        }

        ///phases for each key playing
        NativeArray<float> phaseIncrement = new NativeArray<float>(_JobFrequencies.Length+ 1,Allocator.Temp);

        float value;

        for (int i = 0; i < _JobFrequencies.Length; i++)
        {
            
            phaseIncrement[i] = _KeyData[i].frequency / _sampleRate;
            int sampleStage = 0;

            ///ATTACK
            for (; sampleStage < KeysADSRlayouts[i].AttackSamples; sampleStage += ChannelsNum)
            {
                float phase = _JobPhases[i];

                float effectiveAmplitude =  (_JobDeltas[i] / ADSR.Attack)*_synthdata.amplitude;
                _JobDeltas[i] += deltaIncrement;

                value = ((MusicUtils.Sin(phase) * _synthdata.SinFactor) + (MusicUtils.Saw(phase) * _synthdata.SawFactor) + (MusicUtils.Square(phase) * _synthdata.SquareFactor)) * effectiveAmplitude;
                _JobPhases[i] = (_JobPhases[i] + phaseIncrement[i]) % 1;

                /// populate all channels with the values
                for (int channel = 0; channel < ChannelsNum; channel++)
                {
                    _audioData[sampleStage + channel] += value;
                }

            }
            ///DECAY
            for (; sampleStage < KeysADSRlayouts[i].DecaySamples; sampleStage += ChannelsNum)
            {
                float phase = _JobPhases[i];

                float effectiveAmplitude = _synthdata.amplitude - ((((_JobDeltas[i] - ADSR.Attack) / ADSR.Decay)*(1-ADSR.Sustain))* _synthdata.amplitude);
                
                _JobDeltas[i] += deltaIncrement;

                value = ((MusicUtils.Sin(phase) * _synthdata.SinFactor) + (MusicUtils.Saw(phase) * _synthdata.SawFactor) + (MusicUtils.Square(phase) * _synthdata.SquareFactor)) * effectiveAmplitude;
                _JobPhases[i] = (_JobPhases[i] + phaseIncrement[i]) % 1;

                /// populate all channels with the values
                for (int channel = 0; channel < ChannelsNum; channel++)
                {
                    _audioData[sampleStage + channel] += value;
                }

            }
            ///SUSTAIN
            for (; sampleStage < KeysADSRlayouts[i].SustainSamples; sampleStage += ChannelsNum)
            {
                float phase = _JobPhases[i];

                float effectiveAmplitude = ADSR.Sustain * _synthdata.amplitude;

                _JobDeltas[i] = ADSR.Attack+ADSR.Decay;

                value = ((MusicUtils.Sin(phase) * _synthdata.SinFactor) + (MusicUtils.Saw(phase) * _synthdata.SawFactor) + (MusicUtils.Square(phase) * _synthdata.SquareFactor)) * effectiveAmplitude;
                _JobPhases[i] = (_JobPhases[i] + phaseIncrement[i]) % 1;

                /// populate all channels with the values
                for (int channel = 0; channel < ChannelsNum; channel++)
                {
                    _audioData[sampleStage + channel] += value;
                }

            }
            ///RELEASE
            for (; sampleStage < KeysADSRlayouts[i].ReleaseSamples; sampleStage += ChannelsNum)
            {

                float phase = _JobPhases[i];

                float effectiveAmplitude = (1 - ((_JobDeltas[i] - (ADSR.Attack + ADSR.Decay)) / ADSR.Release)) * _KeyData[i].amplitudeAtRelease; 
                _JobDeltas[i] += deltaIncrement;

                value = ((MusicUtils.Sin(phase) * _synthdata.SinFactor) + (MusicUtils.Saw(phase) * _synthdata.SawFactor) + (MusicUtils.Square(phase) * _synthdata.SquareFactor)) * effectiveAmplitude;
                _JobPhases[i] = (_JobPhases[i] + phaseIncrement[i]) % 1;

                /// populate all channels with the values
                for (int channel = 0; channel < ChannelsNum; channel++)
                {
                    _audioData[sampleStage + channel] += value;
                }

            }
        }

        int newkeysNum=0;
        for (int i = 0; i < _JobFrequencies.Length; i++)
        {
            if (_JobDeltas[i] < ADSR.Attack + ADSR.Decay + ADSR.Release)
            {
                _KeyData[i] = new KeyData { frequency = _KeyData[i].frequency, phase = _JobPhases[i], delta = _JobDeltas[i], amplitudeAtRelease = _KeyData[i].amplitudeAtRelease };
                newkeysNum++;
            }
            else
            {
                //Debug.Log("clear");
                _KeyData[i] = new KeyData { };
            }
        }
        _activeKeynum[0] = newkeysNum;

    }

}
