using System;
using System.Linq;
using MusicNamespace;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Entities.UniversalDelegates;
using Unity.Jobs;
using UnityEditor;
using UnityEngine;

public class AudioGenerator : MonoBehaviour
{
    //const int DSPbufferSize = 512;

    [HideInInspector]
    public LineRenderer OscillatorLine;

    [HideInInspector]
    public Entity WeaponSynthEntity;
    private NativeArray<KeyData> activeKeys;
    /* number of keys currently playing for [0]active synth, [1+]playbacks */
    private NativeArray<int> activeKeyNumber;

    private NativeArray<PlaybackAudioBundle> PlaybackAudioBundles;
    private NativeArray<PlaybackAudioBundleContext> PlaybackAudioBundlesContext;


    private NativeArray<SynthData> SynthsData;
    

    private EntityManager entityManager;
     
    //private float amplitude;
    //private float frequency;
    //private float SinFactor;
    //private float SawFactor;
    //private float SquareFactor;
    //private bool audiojobCompleted;

    private NativeArray<float> _audioData;
    public static JobHandle _Audiojobhandle;
    private int _sampleRate;


    public static AudioRingBuffer<KeysBuffer> audioRingBuffer;

    private const int NumChannels = 1; // Mono audio

    const float DeltaTime = 512f / 48000f;

    private void Start()
    {
        entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;
        //Debug.Log(AudioSettings.outputSampleRate);
        /// TEST
        for (int i = 0; i < SynthsData.Length; i++)
        {
            SynthsData[i] = entityManager.GetComponentData<SynthData>(WeaponSynthEntity);
        }

    }

    private void Awake()
    {
        _sampleRate = AudioSettings.outputSampleRate;
        _audioData = new NativeArray<float>(512, Allocator.Persistent);
        //audiojobCompleted = false; 
        int ringBufferCapacity = 4;
        audioRingBuffer = new AudioRingBuffer<KeysBuffer>(ringBufferCapacity);
        audioRingBuffer.InitializeBuffer(ringBufferCapacity);

        activeKeys = new NativeArray<KeyData>(24, Allocator.Persistent);
        activeKeyNumber = new NativeArray<int>(2, Allocator.Persistent);

        /// TEST AUDIO BUNDLE
        SynthsData = new NativeArray<SynthData>(2, Allocator.Persistent);

        PlaybackAudioBundles = new NativeArray<PlaybackAudioBundle>(1, Allocator.Persistent);
        PlaybackAudioBundlesContext = new NativeArray<PlaybackAudioBundleContext>(1, Allocator.Persistent);
        for (int i = 0; i < PlaybackAudioBundles.Length; i++)
        {
            PlaybackAudioBundle audiobundle = PlaybackAudioBundles[i];

            audiobundle.PlaybackKeys = new NativeArray<PlaybackKey>(6, Allocator.Persistent);
            for (int y = 0; y < 6; y++)
            {
                audiobundle.PlaybackKeys[y] = new PlaybackKey { frequency = MusicUtils.getNearestKey(80) * (y + 1), time = y * 1f, lenght = 0.5f };
            }
            audiobundle.IsLooping = true;
            audiobundle.PlaybackDuration = 7f;
            PlaybackAudioBundles[i] = audiobundle;

        }


    }


    private void Update()
    {

        /*UPDATE UI DISPLAY*/
        //if (audiojobCompleted)
        //{
        //    //reasign the number of points
        //    int points = _audioData.Length / 32;
        //    OscillatorLine.positionCount = points;
        //    float xStart = 0;
        //    float xFinish = 2 * Mathf.PI*2;


        //    for (int currentPoint = 0; currentPoint < points; currentPoint++)
        //    {

        //        float progress = (float)currentPoint / (points - 1);
        //        float x = Mathf.Lerp(xStart, xFinish, progress);
        //        float y = ((MusicUtils.Sin(x)*SinFactor) + (MusicUtils.Saw(x)*SawFactor) + (MusicUtils.Square(x)*SquareFactor)) * amplitude * 6f;
        //        OscillatorLine.SetPosition(currentPoint, new Vector3(x, y, 0));

        //    }
        //    audiojobCompleted = false;
        //}


        //Debug.Break();
    }

    // Callback method for audio processing
    unsafe
    private void OnAudioFilterRead(float[] data, int channels)
    {

        NativeList<short> ActiveplaybackKeysNumberList = new NativeList<short>(Allocator.Temp);
        NativeList<float> ActiveplaybackKeysFzList = new NativeList<float>(Allocator.Temp);

        ///get the number of total keys. create a Narray of that size. Fill values
        int totalNumberOfPlaybackKeys = 0;
        for (int i = 0; i < PlaybackAudioBundles.Length; i++)
        {

            int playbackIndex = 0;

            playbackIndex += (i > 0) ? PlaybackAudioBundles[i - 1].PlaybackKeys.Length : 0;
            playbackIndex += PlaybackAudioBundlesContext[i].PlaybackKeyStartIndex;

            while (playbackIndex < PlaybackAudioBundles[i].PlaybackKeys.Length
                && PlaybackAudioBundles[i].PlaybackKeys[playbackIndex].time < PlaybackAudioBundlesContext[i].PlaybackTime
                && PlaybackAudioBundles[i].PlaybackKeys[playbackIndex].time + PlaybackAudioBundles[i].PlaybackKeys[playbackIndex].lenght > PlaybackAudioBundlesContext[i].PlaybackTime)
            {
                ActiveplaybackKeysFzList.Add(PlaybackAudioBundles[i].PlaybackKeys[playbackIndex].frequency);
                playbackIndex++;
            }

            playbackIndex -= (i > 0) ? PlaybackAudioBundles[i - 1].PlaybackKeys.Length : 0;
            playbackIndex -= PlaybackAudioBundlesContext[i].PlaybackKeyStartIndex;

            ActiveplaybackKeysNumberList.Add((short)playbackIndex);
            totalNumberOfPlaybackKeys += playbackIndex;

            PlaybackAudioBundlesContext[i] = new PlaybackAudioBundleContext { PlaybackKeyStartIndex = PlaybackAudioBundlesContext[i].PlaybackKeyStartIndex, PlaybackTime = PlaybackAudioBundlesContext[i].PlaybackTime + DeltaTime };
        }

        KeysBuffer PlayerkeysBuffer;
        KeysBuffer TotalKeysBuffer;

        ///Read from the ringBuffer to get the playerKeys
        if (audioRingBuffer.IsEmpty)
        {
            /// Buffer is empty. Recycle the latest available data.
            PlayerkeysBuffer = audioRingBuffer.RecycleLastElement();
        }
        else
        {
            PlayerkeysBuffer = audioRingBuffer.Read();
        }

        /// gather all the new playback notes in a native array
        TotalKeysBuffer.keyFrenquecies = new NativeArray<float>(PlayerkeysBuffer.KeyNumber[0] + totalNumberOfPlaybackKeys, Allocator.TempJob);
        TotalKeysBuffer.KeyNumber = new NativeArray<short>(1 + PlaybackAudioBundles.Length, Allocator.TempJob);

        ///fill the native array with the data
        TotalKeysBuffer.KeyNumber[0] = PlayerkeysBuffer.KeyNumber[0];
        for (int i = 1; i < TotalKeysBuffer.KeyNumber.Length; i++)
        {
            TotalKeysBuffer.KeyNumber[i] = ActiveplaybackKeysNumberList[i - 1];
        }

        for (int y = 0; y < TotalKeysBuffer.KeyNumber[0]; y++)
        {
            TotalKeysBuffer.keyFrenquecies[y] = PlayerkeysBuffer.keyFrenquecies[y];
        }
        for (int y = 0; y < ActiveplaybackKeysFzList.Length; y++)
        {
            TotalKeysBuffer.keyFrenquecies[TotalKeysBuffer.KeyNumber[0] + y] = ActiveplaybackKeysFzList[y];
        }

        int TotalActiveKeyNumber = 0;

        /// Check for player keys that oath to be released and new keys for playerKeys to be copyied to ActiveKeys
        {

            NativeSlice<float> keysBufferSlice = new NativeSlice<float>(TotalKeysBuffer.keyFrenquecies, 0, TotalKeysBuffer.KeyNumber[0]);
            /* FIX IF KEY CAPACITY IS NOT 12 */
            NativeSlice<KeyData> ActiveKeysSlice = new NativeSlice<KeyData>(activeKeys, 0, activeKeyNumber[0]);

            for (int y = 0; y < activeKeyNumber[0]; y++)
            {
                /// Check for keys that oath to be released 
                if (keysBufferSlice.Contains(activeKeys[y].frequency))
                {
                    ///Key already being released, check if repressed
                    if (activeKeys[y].amplitudeAtRelease != 0)
                    {
                        activeKeys[y] = new KeyData { frequency = activeKeys[y].frequency, delta = 0f, phase = activeKeys[y].phase, amplitudeAtRelease = 0 };
                    }
                }
                /// the buffer no longuer contains the frequency. release.
                else
                {
                    ///key already releasing
                    if (activeKeys[y].amplitudeAtRelease != 0)
                        continue;

                    float releaseAmplitude;
                    if (activeKeys[y].delta < SynthsData[0].ADSR.Attack)
                        releaseAmplitude = (activeKeys[y].delta / SynthsData[0].ADSR.Attack) * SynthsData[0].amplitude;
                    else if (activeKeys[y].delta < SynthsData[0].ADSR.Attack + SynthsData[0].ADSR.Decay)
                        releaseAmplitude = SynthsData[0].amplitude - (((activeKeys[y].delta - SynthsData[0].ADSR.Attack) / SynthsData[0].ADSR.Decay) * (1 - SynthsData[0].ADSR.Sustain) * SynthsData[0].amplitude);
                    else
                        releaseAmplitude = SynthsData[0].ADSR.Sustain * SynthsData[0].amplitude;

                    activeKeys[y] = new KeyData { frequency = activeKeys[y].frequency, delta = SynthsData[0].ADSR.Attack + SynthsData[0].ADSR.Decay, phase = activeKeys[y].phase, amplitudeAtRelease = releaseAmplitude + 0.0001f /*make sure the key is considered released*/ };

                }
            }
            int overwriteKeysNum = 0;
            for (int y = 0; y < TotalKeysBuffer.KeyNumber[0]; y++)
            {
                /// The key doesnt exist -> activate it
                if (!ActiveKeysSlice.Any(ActiveKeysSlice => ActiveKeysSlice.frequency == keysBufferSlice[y]))
                {
                    if (activeKeyNumber[0] < 12)
                    {
                        activeKeys[activeKeyNumber[0]] = new KeyData { frequency = keysBufferSlice[y] };
                        activeKeyNumber[0]++;
                    }
                    /// the number of keys played simultaneously has reached its limit : start overwriting the oldest ones.
                    else
                    {
                        Debug.Log("overwrite");
                        activeKeys[overwriteKeysNum] = new KeyData { frequency = keysBufferSlice[y] };
                        overwriteKeysNum++;
                    }
                }
            }
            TotalActiveKeyNumber += activeKeyNumber[0];
        }


        /// Check for player keys that oath to be released and new keys for each PlaybackAudioBundles to be copyied to ActiveKeys
        for (int i = 0; i < PlaybackAudioBundles.Length; i++)
        {
            int z = (i + 1);
            NativeSlice<float> keysBufferSlice = new NativeSlice<float>(TotalKeysBuffer.keyFrenquecies, TotalKeysBuffer.KeyNumber[i], TotalKeysBuffer.KeyNumber[z]);
            /* FIX IF KEY CAPACITY IS NOT 12 */
            NativeSlice<KeyData> ActiveKeysSlice = new NativeSlice<KeyData>(activeKeys, z * 12, activeKeyNumber[z]);

            /// Check for player keys that oath to be released 
            for (int y = 0; y < activeKeyNumber[z]; y++)
            {
                //native slice here for each buffer parts

                /// Check for keys that oath to be released 
                if (keysBufferSlice.Contains(ActiveKeysSlice[y].frequency))
                {
                    ///Key already being released, check if repressed
                    if (ActiveKeysSlice[y].amplitudeAtRelease != 0)
                    {
                        ActiveKeysSlice[y] = new KeyData { frequency = ActiveKeysSlice[y].frequency, delta = 0f, phase = ActiveKeysSlice[y].phase, amplitudeAtRelease = 0 };
                    }
                }
                /// the buffer no longuer contains the frequency. release.
                else
                {
                    //Debug.Log(ActiveKeysSlice[y].amplitudeAtRelease);
                    ///key already releasing
                    if (ActiveKeysSlice[y].amplitudeAtRelease != 0)
                        continue;

                    float releaseAmplitude;
                    if (ActiveKeysSlice[y].delta < SynthsData[z].ADSR.Attack)
                        releaseAmplitude = (ActiveKeysSlice[y].delta / SynthsData[z].ADSR.Attack) * SynthsData[z].amplitude;
                    else if (ActiveKeysSlice[y].delta < SynthsData[z].ADSR.Attack + SynthsData[z].ADSR.Decay)
                        releaseAmplitude = SynthsData[z].amplitude - (((ActiveKeysSlice[y].delta - SynthsData[z].ADSR.Attack) / SynthsData[z].ADSR.Decay) * (1 - SynthsData[z].ADSR.Sustain) * SynthsData[z].amplitude);
                    else
                        releaseAmplitude = SynthsData[z].ADSR.Sustain * SynthsData[z].amplitude;

                    ActiveKeysSlice[y] = new KeyData { frequency = ActiveKeysSlice[y].frequency, delta = SynthsData[z].ADSR.Attack + SynthsData[z].ADSR.Decay, phase = ActiveKeysSlice[y].phase, amplitudeAtRelease = releaseAmplitude + 0.00001f /*make sure the key is considered released*/ };

                    PlaybackAudioBundlesContext[i] = new PlaybackAudioBundleContext { PlaybackKeyStartIndex = PlaybackAudioBundlesContext[i].PlaybackKeyStartIndex +1, PlaybackTime = PlaybackAudioBundlesContext[i].PlaybackTime};
                }
            }
            /// the playback will finish within this DSPBufferSize -> loop or ?...
            if (PlaybackAudioBundlesContext[i].PlaybackTime + DeltaTime > PlaybackAudioBundles[i].PlaybackDuration)
            {
                PlaybackAudioBundlesContext[i] = new PlaybackAudioBundleContext { PlaybackKeyStartIndex = 0, PlaybackTime = PlaybackAudioBundlesContext[i].PlaybackTime + DeltaTime - PlaybackAudioBundles[i].PlaybackDuration };
            }
            /// Check if there are new note played and add it to the activeKeys array
            int overwriteKeysNum = 0;
            for (int y = 0; y < TotalKeysBuffer.KeyNumber[z]; y++)
            {
                /// The key doesnt exist -> activate it
                if (!ActiveKeysSlice.Any(ActiveKeysSlice => ActiveKeysSlice.frequency == keysBufferSlice[y]))
                {
                    if (activeKeyNumber[z] < 12)
                    {
                        activeKeys[(z * 12)+activeKeyNumber[z]] = new KeyData { frequency = keysBufferSlice[y] };
                        activeKeyNumber[z]++;
                    }
                    /// the number of keys played simultaneously has reached its limit : start overwriting the oldest ones.
                    else
                    {
                        Debug.Log("overwrite");
                        ///TO DO
                        //activeKeys[overwriteKeysNum] = new KeyData { frequency = keysBufferSlice[y] };
                        //overwriteKeysNum++;
                    }
                }
            }
            TotalActiveKeyNumber += activeKeyNumber[z];
        }


        if (TotalActiveKeyNumber < 1)
            return;


        ///activeKeys elemets cropped, rdy to be processed
        NativeArray<float> _JobFrequencies = new NativeArray<float>(TotalActiveKeyNumber, Allocator.TempJob);
        NativeArray<float> _JobPhases = new NativeArray<float>(TotalActiveKeyNumber, Allocator.TempJob);
        NativeArray<float> _JobDeltas = new NativeArray<float>(TotalActiveKeyNumber, Allocator.TempJob);

        int ActiveKeysStartIdx = 0;
        for (int i = 0; i < SynthsData.Length; i++)
        {
            for (int y = 0; y < activeKeyNumber[i]; y++)
            {
                _JobFrequencies[ActiveKeysStartIdx+y] = activeKeys[(i * 12)+y].frequency;
                _JobPhases[ActiveKeysStartIdx + y] = activeKeys[(i * 12) + y].phase;
                _JobDeltas[ActiveKeysStartIdx + y] = activeKeys[(i * 12) + y].delta;
            }
            ActiveKeysStartIdx += activeKeyNumber[i];
        }

        AudioJob audioJob = new AudioJob(
            _sampleRate,
            SynthsData,
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

    [ReadOnly]
    private NativeArray<SynthData> _synthsdata;

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
    //[WriteOnly]
    private NativeArray<int> _activeKeynum;

    public AudioJob(
       float sampleRate,
       NativeArray<SynthData> synthsdata,
       NativeArray<KeyData> KeyData,


        NativeArray<float> JobFrequencies,
        NativeArray<float> JobPhases,
        NativeArray<float> JobDeltas,
        NativeArray<float> audioData,
        NativeArray<int> activeKeynum
       )
    {
        _synthsdata = synthsdata;
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


        NativeArray<ADSRlayouts> KeysADSRlayouts = new NativeArray<ADSRlayouts>(_JobFrequencies.Length, Allocator.Temp);

        int activeKeyStartidx = 0;
        for (int i = 0; i < _synthsdata.Length; i++)
        {
            ADSREnvelope ADSR = _synthsdata[i].ADSR;

            for (int y = 0; y < _activeKeynum[i]; y++)
            {
                int cropedKeyIdx = activeKeyStartidx + y;

                int samplePool = 512;
                int attackSamples = 0;
                int decaySamples = 0;
                int sustainSamples = 0;
                int releaseSamples = 0;

                if (_KeyData[(i * 12) + y].amplitudeAtRelease == 0)
                {
                    attackSamples = _JobDeltas[cropedKeyIdx] < ADSR.Attack ? Mathf.Min(Mathf.CeilToInt(Mathf.Abs(_JobDeltas[cropedKeyIdx] - ADSR.Attack) * (_sampleRate)), samplePool) : 0;
                    samplePool -= attackSamples;
                    decaySamples = samplePool > 0 && _JobDeltas[cropedKeyIdx] < (ADSR.Attack + ADSR.Decay) ? Mathf.Min(Mathf.CeilToInt(Mathf.Abs(Mathf.Max(_JobDeltas[cropedKeyIdx], ADSR.Attack) - (ADSR.Attack + ADSR.Decay)) * (_sampleRate)), samplePool) : 0;
                    samplePool -= decaySamples;
                    sustainSamples = samplePool;
                }
                else
                {
                    releaseSamples = Mathf.Min(Mathf.CeilToInt(Mathf.Abs(_JobDeltas[cropedKeyIdx] - (ADSR.Attack + ADSR.Decay + ADSR.Release)) * (_sampleRate)), samplePool);
                }

                KeysADSRlayouts[cropedKeyIdx] = new ADSRlayouts
                {
                    AttackSamples = attackSamples,
                    DecaySamples = decaySamples + attackSamples,
                    SustainSamples = sustainSamples + decaySamples + attackSamples,
                    ReleaseSamples = releaseSamples

                };
            }
            activeKeyStartidx += _activeKeynum[i];
        }

        ///phases for each key playing
        NativeArray<float> phaseIncrement = new NativeArray<float>(_JobFrequencies.Length + 1, Allocator.Temp);

        float value;
        activeKeyStartidx = 0;
        for (int i = 0; i < _synthsdata.Length; i++)
        {
            ADSREnvelope ADSR = _synthsdata[i].ADSR;

            for (int y = activeKeyStartidx; y < activeKeyStartidx + _activeKeynum[i]; y++)
            {

                phaseIncrement[y] = _JobFrequencies[y] / _sampleRate;
                int sampleStage = 0;

                ///ATTACK
                for (; sampleStage < KeysADSRlayouts[y].AttackSamples; sampleStage += ChannelsNum)
                {
                    float phase = _JobPhases[y];

                    float effectiveAmplitude = (_JobDeltas[y] / ADSR.Attack) * _synthsdata[i].amplitude;
                    _JobDeltas[y] += deltaIncrement;

                    value = ((MusicUtils.Sin(phase) * _synthsdata[i].SinFactor) + (MusicUtils.Saw(phase) * _synthsdata[i].SawFactor) + (MusicUtils.Square(phase) * _synthsdata[i].SquareFactor)) * effectiveAmplitude;
                    _JobPhases[y] = (_JobPhases[y] + phaseIncrement[y]) % 1;

                    /// populate all channels with the values
                    for (int channel = 0; channel < ChannelsNum; channel++)
                    {
                        _audioData[sampleStage + channel] += value;
                    }

                }
                ///DECAY
                for (; sampleStage < KeysADSRlayouts[y].DecaySamples; sampleStage += ChannelsNum)
                {
                    float phase = _JobPhases[y];

                    float effectiveAmplitude = _synthsdata[i].amplitude - ((((_JobDeltas[y] - ADSR.Attack) / ADSR.Decay) * (1 - ADSR.Sustain)) * _synthsdata[i].amplitude);

                    _JobDeltas[y] += deltaIncrement;

                    value = ((MusicUtils.Sin(phase) * _synthsdata[i].SinFactor) + (MusicUtils.Saw(phase) * _synthsdata[i].SawFactor) + (MusicUtils.Square(phase) * _synthsdata[i].SquareFactor)) * effectiveAmplitude;
                    _JobPhases[y] = (_JobPhases[y] + phaseIncrement[y]) % 1;

                    /// populate all channels with the values
                    for (int channel = 0; channel < ChannelsNum; channel++)
                    {
                        _audioData[sampleStage + channel] += value;
                    }

                }
                ///SUSTAIN
                for (; sampleStage < KeysADSRlayouts[y].SustainSamples; sampleStage += ChannelsNum)
                {
                    float phase = _JobPhases[y];

                    float effectiveAmplitude = ADSR.Sustain * _synthsdata[i].amplitude;

                    _JobDeltas[y] = ADSR.Attack + ADSR.Decay;

                    value = ((MusicUtils.Sin(phase) * _synthsdata[i].SinFactor) + (MusicUtils.Saw(phase) * _synthsdata[i].SawFactor) + (MusicUtils.Square(phase) * _synthsdata[i].SquareFactor)) * effectiveAmplitude;
                    _JobPhases[y] = (_JobPhases[y] + phaseIncrement[y]) % 1;

                    /// populate all channels with the values
                    for (int channel = 0; channel < ChannelsNum; channel++)
                    {
                        _audioData[sampleStage + channel] += value;
                    }

                }
                ///RELEASE
                for (; sampleStage < KeysADSRlayouts[y].ReleaseSamples; sampleStage += ChannelsNum)
                {

                    float phase = _JobPhases[y];

                    float effectiveAmplitude = (1 - ((_JobDeltas[y] - (ADSR.Attack + ADSR.Decay)) / ADSR.Release)) * _KeyData[(i*12)+(y - activeKeyStartidx)].amplitudeAtRelease;
                    _JobDeltas[y] += deltaIncrement;

                    value = ((MusicUtils.Sin(phase) * _synthsdata[i].SinFactor) + (MusicUtils.Saw(phase) * _synthsdata[i].SawFactor) + (MusicUtils.Square(phase) * _synthsdata[i].SquareFactor)) * effectiveAmplitude;
                    _JobPhases[y] = (_JobPhases[y] + phaseIncrement[y]) % 1;

                    /// populate all channels with the values
                    for (int channel = 0; channel < ChannelsNum; channel++)
                    {
                        _audioData[sampleStage + channel] += value;
                    }

                }
            }
            activeKeyStartidx += _activeKeynum[i];
        }

        ///Nullify keys that finished playing from the activeKeys array and collapse it to keep it continuous
        activeKeyStartidx = 0;
        for (int i = 0; i < _synthsdata.Length; i++)
        {
            int newkeysNum = 0;
            ADSREnvelope ADSR = _synthsdata[i].ADSR;
            int remainingActiveKeys = _activeKeynum[i];

            for (int y = 0; y < _activeKeynum[i]; y++)
            {
                int fullKeyIdx = (i * 12) + y - (_activeKeynum[i] - remainingActiveKeys);
                int cropedKeyIdx = activeKeyStartidx + y ;

                if (_JobDeltas[cropedKeyIdx] < ADSR.Attack + ADSR.Decay + ADSR.Release)
                {
                    _KeyData[fullKeyIdx] = new KeyData { frequency = _JobFrequencies[cropedKeyIdx], phase = _JobPhases[cropedKeyIdx], delta = _JobDeltas[cropedKeyIdx], amplitudeAtRelease = _KeyData[fullKeyIdx].amplitudeAtRelease };
                    newkeysNum++;
                }
                else
                {
                    int z = fullKeyIdx;
                    for (; z < fullKeyIdx + (_activeKeynum[i]- y); z++)
                    {
                        _KeyData[z] = _KeyData[z+1];
                    }
                    _KeyData[z] = new KeyData { };
                    remainingActiveKeys--;
                }
            }
            activeKeyStartidx += _activeKeynum[i];
            _activeKeynum[i] = newkeysNum;
        }
    }

}
