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
    /// const int DSPbufferSize = 512;

    //public AudioLayoutStorage audioLayoutStorage;
    //public bool AudioLayoutUpdateRequired = false;


    [HideInInspector]
    public LineRenderer OscillatorLine;

    //[HideInInspector]
    //public Entity WeaponSynthEntity;

    /// 2 Differents SynthData storage : on weapon entities & here -> OPTI ? 
    /// -> Reason ? can't use data from a class instance in a job ?
    ///
    public NativeArray<SynthData> SynthsData;
    //public NativeArray<FilterDelayElements> _filterDelayElements;

    public NativeArray<KeyData> activeKeys;
    public NativeArray<int> activeKeyNumber;
    public NativeArray<int> activeSynthsIdx;

    public NativeArray<PlaybackAudioBundle> PlaybackAudioBundles;
    public NativeArray<PlaybackAudioBundleContext> PlaybackAudioBundlesContext;



    private EntityManager entityManager;

    private NativeArray<float> _audioData;
    public static JobHandle _Audiojobhandle;
    private int _sampleRate;

    public static AudioRingBuffer<KeysBuffer> audioRingBuffer;

    private const int NumChannels = 1; // Mono audio

    const float DeltaTime = 258f / 48000f;

    private void Start()
    {
        entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;

        AudioLayoutStorageHolder.audioLayoutStorage.PlaybackContextResetRequired = new NativeQueue<int>(Allocator.Persistent);

        //Debug.Log(AudioSettings.outputSampleRate);
        /// TEST
        //SynthsData[0] = entityManager.GetComponentData<SynthData>(WeaponSynthEntity);
        //for (int i = 0; i < SynthsData.Length; i++)
        //{
        //    SynthsData[i] = entityManager.GetComponentData<SynthData>(WeaponSynthEntity);
        //}

    }

    private void Awake()
    {
        _sampleRate = AudioSettings.outputSampleRate;
        _audioData = new NativeArray<float>(512, Allocator.Persistent);
        //_filterDelayElements = new NativeArray<FilterDelayElements>(1, Allocator.Persistent);
        //audiojobCompleted = false; 
        int ringBufferCapacity = 4;
        //audioLayoutStorage = new AudioLayoutStorage();
        //AudioLayoutStorageHolder.audioLayoutStorage = new AudioLayoutStorage();
        audioRingBuffer = new AudioRingBuffer<KeysBuffer>(ringBufferCapacity);
        audioRingBuffer.InitializeBuffer(ringBufferCapacity);

        //activeKeys = new NativeArray<KeyData>(12, Allocator.Persistent);
        //activeKeyNumber = new NativeArray<int>(1, Allocator.Persistent);

        ///// TEST AUDIO BUNDLE
        //SynthsData = new NativeArray<SynthData>(1, Allocator.Persistent);

        //PlaybackAudioBundles = new NativeArray<PlaybackAudioBundle>(1, Allocator.Persistent);
        //PlaybackAudioBundlesContext = new NativeArray<PlaybackAudioBundleContext>(1, Allocator.Persistent);
        //for (int i = 0; i < PlaybackAudioBundles.Length; i++)
        //{
        //    PlaybackAudioBundle audiobundle = PlaybackAudioBundles[i];

        //    audiobundle.PlaybackKeys = new NativeArray<PlaybackKey>(6, Allocator.Persistent);
        //    for (int y = 0; y < 6; y++)
        //    {
        //        audiobundle.PlaybackKeys[y] = new PlaybackKey { frequency = MusicUtils.getNearestKey(80) * (y + 1), time = y * 1f, lenght = 0.5f };
        //    }
        //    audiobundle.IsLooping = true;
        //    audiobundle.PlaybackDuration = 7f;
        //    PlaybackAudioBundles[i] = audiobundle;

        //}


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

        /// To reset audio playbacks and keep them in sync with the main thread
        while (AudioLayoutStorageHolder.audioLayoutStorage.PlaybackContextResetRequired.Count > 0)
        {

            int i = AudioLayoutStorageHolder.audioLayoutStorage.PlaybackContextResetRequired.Dequeue();
            // backward way to find the index of the playbackcontext to reset from the playback index
            int y = 1;
            for (; y < activeSynthsIdx.Length; y++)
            {
                if (activeSynthsIdx[y] == i)
                    break;
            }
            PlaybackAudioBundlesContext[y-1] = new PlaybackAudioBundleContext { PlaybackKeyStartIndex = 0, PlaybackTime = 0 };
            activeKeyNumber[y] = 0;
        }

        /// Jobify ?
        if (AudioLayoutStorageHolder.audioLayoutStorage.UpdateRequirement)
        {
            if(AudioLayoutStorageHolder.audioLayoutStorage.AddSynthUpdateRequirement)
            {
                var newSynthsData = new NativeArray<SynthData>(SynthsData.Length+1, Allocator.Persistent);
                var newPlaybackBundle = new NativeArray<PlaybackAudioBundle>(PlaybackAudioBundles.Length+1, Allocator.Persistent);
                var newfilterDelayElements = new NativeArray<FilterDelayElements>((PlaybackAudioBundles.Length + 2) * 12, Allocator.Persistent);

                for (int i = 0; i < SynthsData.Length; i++)
                {
                    newSynthsData[i] = SynthsData[i];
                    newPlaybackBundle[i] = PlaybackAudioBundles[i];
                }
                newSynthsData[SynthsData.Length] = AudioLayoutStorageHolder.audioLayoutStorage.ReadAddSynth();
                SynthsData.Dispose();
                PlaybackAudioBundles.Dispose();
                AudioLayoutStorageHolder.audioLayoutStorage.filterDelayElements.Dispose();
                SynthsData = newSynthsData;
                PlaybackAudioBundles = newPlaybackBundle;
                AudioLayoutStorageHolder.audioLayoutStorage.filterDelayElements = newfilterDelayElements;

            }
            if(AudioLayoutStorageHolder.audioLayoutStorage.SelectSynthUpdateRequirement)
            {
                var newActiveSynthsIdx = new NativeArray<int>(activeSynthsIdx.Length, Allocator.Persistent);
                activeSynthsIdx.CopyTo(newActiveSynthsIdx);
                newActiveSynthsIdx[0] = AudioLayoutStorageHolder.audioLayoutStorage.ReadSelectSynth();
                activeSynthsIdx.Dispose();
                activeSynthsIdx = newActiveSynthsIdx;
            }
            if(AudioLayoutStorageHolder.audioLayoutStorage.ModifySynthUpdateRequirement)
            {
                SynthsData[activeSynthsIdx[0]] = AudioLayoutStorageHolder.audioLayoutStorage.ReadModifySynth();
            }
            if (AudioLayoutStorageHolder.audioLayoutStorage.PlaybackUpdateRequirement)
            {

                var newPlaybackBundle = new NativeArray<PlaybackAudioBundle>(PlaybackAudioBundles.Length, Allocator.Persistent);
                PlaybackAudioBundles.CopyTo(newPlaybackBundle);
                newPlaybackBundle[AudioLayoutStorageHolder.audioLayoutStorage.synthPlaybackIdx] = AudioLayoutStorageHolder.audioLayoutStorage.ReadPlayback();
                PlaybackAudioBundles.Dispose();
                PlaybackAudioBundles = newPlaybackBundle;
            }
            if(AudioLayoutStorageHolder.audioLayoutStorage.ActivationUpdateRequirement)
            {
                var activation = AudioLayoutStorageHolder.audioLayoutStorage.ReadActivation();

                /// expand the arrays for Playbackkeys to audio
                if (activation.Item2 == true)
                {
                    //Debug.Log("play");

                    /// native array of FilterDelayElements ?
                    /// extend the array here ?

                    var newActiveKeys = new NativeArray<KeyData>((activeKeyNumber.Length + 1) *12, Allocator.Persistent);
                    var newActiveKeyNumber = new NativeArray<int>(activeKeyNumber.Length+1, Allocator.Persistent);
                    var newActiveSynthsIdx = new NativeArray<int>(activeSynthsIdx.Length+1, Allocator.Persistent);
                    var newActivePlaybackContext = new NativeArray<PlaybackAudioBundleContext>(PlaybackAudioBundlesContext.Length+1, Allocator.Persistent);
                    for (int i = 0; i < activeKeyNumber.Length; i++)
                    {
                        for (int y = 0; y < 12; y++)
                        {
                            newActiveKeys[(i * 12) + y] = activeKeys[(i * 12) + y];
                        }
                        newActiveKeyNumber[i] = activeKeyNumber[i];
                        newActiveSynthsIdx[i] = activeSynthsIdx[i];
                    }
                    for (int i = 0; i < PlaybackAudioBundlesContext.Length; i++)
                    {
                        newActivePlaybackContext[i] = PlaybackAudioBundlesContext[i];
                    }

                    newActiveSynthsIdx[newActiveSynthsIdx.Length-1] = activation.Item1;
                    activeKeys.Dispose();
                    activeKeyNumber.Dispose();
                    activeSynthsIdx.Dispose();
                    PlaybackAudioBundlesContext.Dispose();
                    activeKeys = newActiveKeys;
                    activeKeyNumber = newActiveKeyNumber;
                    activeSynthsIdx = newActiveSynthsIdx;
                    PlaybackAudioBundlesContext = newActivePlaybackContext;

                }
                /// Collapse the arrays for Playbackkeys to audio
                else
                {
                    var newActiveKeys = new NativeArray<KeyData>((activeKeyNumber.Length-1) * 12, Allocator.Persistent);
                    var newActiveKeyNumber = new NativeArray<int>(activeKeyNumber.Length-1, Allocator.Persistent);
                    var newActiveSynthsIdx = new NativeArray<int>(activeSynthsIdx.Length-1, Allocator.Persistent);
                    var newActivePlaybackContext = new NativeArray<PlaybackAudioBundleContext>(PlaybackAudioBundlesContext.Length-1, Allocator.Persistent);

                    //Debug.Log("collapse");

                    /// 1 synth is the active one and permanent
                    newActiveKeyNumber[0] = activeKeyNumber[0];
                    newActiveSynthsIdx[0] = activeSynthsIdx[0];
                    for (int i = 0; i < 12; i++)
                    {
                        newActiveKeys[i]= activeKeys[i];
                    }

                    int indexProgress = 1;
                    for (; indexProgress < newActiveKeyNumber.Length; indexProgress++)
                    {
                        if (AudioLayoutStorageHolder.audioLayoutStorage.synthActivationIdx == activeSynthsIdx[indexProgress])
                            break;
                        newActiveKeyNumber[indexProgress] = activeKeyNumber[indexProgress];
                        newActiveSynthsIdx[indexProgress] = activeSynthsIdx[indexProgress];
                        newActivePlaybackContext[indexProgress-1] = PlaybackAudioBundlesContext[indexProgress-1];
                        for (int y = 0; y < 12; y++)
                        {
                            newActiveKeys[(indexProgress * 12)+y] = activeKeys[(indexProgress * 12) + y];
                        }
                    }
                    for (; indexProgress < newActiveKeyNumber.Length; indexProgress++)
                    {
                        newActiveKeyNumber[indexProgress] = activeKeyNumber[indexProgress+1];
                        newActiveSynthsIdx[indexProgress] = activeSynthsIdx[indexProgress+1];
                        newActivePlaybackContext[indexProgress-1] = PlaybackAudioBundlesContext[indexProgress-1 + 1];
                        for (int y = 0; y < 12; y++)
                        {
                            newActiveKeys[(indexProgress * 12) + y] = activeKeys[((indexProgress+1) * 12) + y];
                        }
                    }


                    activeKeys.Dispose();
                    activeKeyNumber.Dispose();
                    activeSynthsIdx.Dispose();
                    PlaybackAudioBundlesContext.Dispose();
                    activeKeys = newActiveKeys;
                    activeKeyNumber = newActiveKeyNumber;
                    activeSynthsIdx = newActiveSynthsIdx;
                    PlaybackAudioBundlesContext = newActivePlaybackContext;
                }
            }


            AudioLayoutStorageHolder.audioLayoutStorage.UpdateRequirement = false;
        }

     
        NativeList<short> ActiveplaybackKeysNumberList = new NativeList<short>(Allocator.Temp);
        NativeList<float> ActiveplaybackKeysFzList = new NativeList<float>(Allocator.Temp);

        ///get the number of total keys. create a Narray of that size. Fill values
        int totalNumberOfPlaybackKeys = 0;
        for (int i = 0; i < PlaybackAudioBundlesContext.Length; i++)
        {
            int playbackAudioBundlesIdx = activeSynthsIdx[i+1];
            int playbackIndex = 0;

            //playbackIndex += (i > 0) ? PlaybackAudioBundles[activeSynthsIdx[i]].PlaybackKeys.Length : 0;
            playbackIndex += PlaybackAudioBundlesContext[i].PlaybackKeyStartIndex;

            while (playbackIndex < PlaybackAudioBundles[playbackAudioBundlesIdx].PlaybackKeys.Length
                && PlaybackAudioBundles[playbackAudioBundlesIdx].PlaybackKeys[playbackIndex].time < PlaybackAudioBundlesContext[i].PlaybackTime
                && PlaybackAudioBundles[playbackAudioBundlesIdx].PlaybackKeys[playbackIndex].time + PlaybackAudioBundles[playbackAudioBundlesIdx].PlaybackKeys[playbackIndex].lenght > PlaybackAudioBundlesContext[i].PlaybackTime)
            {
                ActiveplaybackKeysFzList.Add(MusicUtils.DirectionToFrequency(PlaybackAudioBundles[playbackAudioBundlesIdx].PlaybackKeys[playbackIndex].dir));
                playbackIndex++;
            }

            //playbackIndex -= (i > 0) ? PlaybackAudioBundles[activeSynthsIdx[i]].PlaybackKeys.Length : 0;
            playbackIndex -= PlaybackAudioBundlesContext[i].PlaybackKeyStartIndex;

            ActiveplaybackKeysNumberList.Add((short)playbackIndex);
            totalNumberOfPlaybackKeys += playbackIndex;

            //not needed anymore ?
            //if(PlaybackAudioBundles[playbackAudioBundlesIdx].IsPlaying)
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

        //Debug.LogError(PlayerkeysBuffer.keyFrenquecies[0]);

        /// gather all the new playback notes in a native array
        TotalKeysBuffer.keyFrenquecies = new NativeArray<float>(PlayerkeysBuffer.KeyNumber[0] + totalNumberOfPlaybackKeys, Allocator.TempJob);
        TotalKeysBuffer.KeyNumber = new NativeArray<short>(activeSynthsIdx.Length, Allocator.TempJob);

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

        /// Check for player keys that oath to be released and new keys for playerKeys to be copyied to ActiveKeys. OPTI ?
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
                        activeKeys[y] = new KeyData { frequency = activeKeys[y].frequency, delta = 0f, OCS1phase = activeKeys[y].OCS1phase, OCS2phase = activeKeys[y].OCS2phase, amplitudeAtRelease = 0,filterDelayElements = activeKeys[y].filterDelayElements};
                    }
                }
                /// the buffer no longuer contains the frequency. release.
                else
                {
                    ///key already releasing
                    if (activeKeys[y].amplitudeAtRelease != 0)
                        continue;

                    float delta = activeKeys[y].delta;

                    int synthidx = activeSynthsIdx[0];

                    float releaseAmplitude;
                    if (delta < SynthsData[synthidx].ADSR.Attack)
                        releaseAmplitude = (delta / SynthsData[synthidx].ADSR.Attack) * SynthsData[synthidx].amplitude;
                    else if (delta < SynthsData[synthidx].ADSR.Attack + SynthsData[synthidx].ADSR.Decay)
                        releaseAmplitude = SynthsData[synthidx].amplitude - (((delta - SynthsData[synthidx].ADSR.Attack) / SynthsData[synthidx].ADSR.Decay) * (1 - SynthsData[synthidx].ADSR.Sustain) * SynthsData[synthidx].amplitude);
                    else
                        releaseAmplitude = SynthsData[synthidx].ADSR.Sustain * SynthsData[synthidx].amplitude;

                    activeKeys[y] = new KeyData { 
                        frequency = activeKeys[y].frequency, 
                        delta = 0,  
                        OCS1phase = activeKeys[y].OCS1phase, 
                        OCS2phase = activeKeys[y].OCS2phase, 
                        amplitudeAtRelease = releaseAmplitude + 0.0001f /*make sure the key is considered released*/,
                        CutoffAmountAtRelease = delta < SynthsData[synthidx].filterADSR.Attack? 
                        (delta / SynthsData[synthidx].filterADSR.Attack)://* SynthsData[synthidx].filterEnvelopeAmount: 
                        (((1 - (delta - SynthsData[synthidx].filterADSR.Attack) / SynthsData[synthidx].filterADSR.Decay)) * (1 - SynthsData[synthidx].filterADSR.Sustain) + SynthsData[synthidx].filterADSR.Sustain),// * SynthsData[synthidx].filterEnvelopeAmount,
                        filterDelayElements = activeKeys[y].filterDelayElements
                    };

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
        int plabackSliceStartIndex = 0;
        for (int i = 0; i < PlaybackAudioBundlesContext.Length; i++)
        {
            int z = (i + 1);
            plabackSliceStartIndex += TotalKeysBuffer.KeyNumber[i];
            NativeSlice<float> keysBufferSlice = new NativeSlice<float>(TotalKeysBuffer.keyFrenquecies, plabackSliceStartIndex, TotalKeysBuffer.KeyNumber[z]);
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
                        ActiveKeysSlice[y] = new KeyData { frequency = ActiveKeysSlice[y].frequency, delta = 0f, OCS1phase = ActiveKeysSlice[y].OCS1phase, OCS2phase = ActiveKeysSlice[y].OCS2phase, amplitudeAtRelease = 0,CutoffAmountAtRelease = ActiveKeysSlice[y].CutoffAmountAtRelease};
                    }
                }
                /// the buffer no longuer contains the frequency. release.
                else
                {
                    //Debug.Log(ActiveKeysSlice[y].amplitudeAtRelease);
                    ///key already releasing
                    if (ActiveKeysSlice[y].amplitudeAtRelease != 0)
                        continue;

                    float delta = ActiveKeysSlice[y].delta;

                    int synthidx = activeSynthsIdx[z];

                    float releaseAmplitude;
                    if (delta < SynthsData[synthidx].ADSR.Attack)
                        releaseAmplitude = (delta / SynthsData[synthidx].ADSR.Attack) * SynthsData[synthidx].amplitude;
                    else if (delta < SynthsData[synthidx].ADSR.Attack + SynthsData[synthidx].ADSR.Decay)
                        releaseAmplitude = SynthsData[synthidx].amplitude - (((delta - SynthsData[synthidx].ADSR.Attack) / SynthsData[synthidx].ADSR.Decay) * (1 - SynthsData[synthidx].ADSR.Sustain) * SynthsData[synthidx].amplitude);
                    else
                        releaseAmplitude = SynthsData[synthidx].ADSR.Sustain * SynthsData[synthidx].amplitude;

                    ActiveKeysSlice[y] = new KeyData { 
                        frequency = ActiveKeysSlice[y].frequency, 
                        delta = 0, 
                        OCS1phase = ActiveKeysSlice[y].OCS1phase, 
                        OCS2phase = ActiveKeysSlice[y].OCS2phase, 
                        amplitudeAtRelease = releaseAmplitude + 0.00001f, /*make sure the key is considered released*/
                        CutoffAmountAtRelease = delta < SynthsData[synthidx].filterADSR.Attack ?
                        (delta / SynthsData[synthidx].filterADSR.Attack) :
                        (((1 - (delta - SynthsData[synthidx].filterADSR.Attack) / SynthsData[synthidx].filterADSR.Decay)) * (1 - SynthsData[synthidx].filterADSR.Sustain) + SynthsData[synthidx].filterADSR.Sustain),
                        filterDelayElements = activeKeys[y].filterDelayElements
                    };

                    PlaybackAudioBundlesContext[i] = new PlaybackAudioBundleContext { 
                        PlaybackKeyStartIndex = PlaybackAudioBundlesContext[i].PlaybackKeyStartIndex +1,
                        PlaybackTime = PlaybackAudioBundlesContext[i].PlaybackTime
                    };
                }
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

        //Debug.Log(TotalActiveKeyNumber);
        //Debug.Log(SynthsData[0].Osc2SinSawSquareFactor);

        ///activeKeys elemets cropped, rdy to be processed
        NativeArray<float> _JobFrequencies = new NativeArray<float>(TotalActiveKeyNumber, Allocator.TempJob);
        ///OSC1 + OSC2 phases array
        NativeArray<float> _JobPhases = new NativeArray<float>(TotalActiveKeyNumber*2, Allocator.TempJob);
        NativeArray<float> _JobDeltas = new NativeArray<float>(TotalActiveKeyNumber, Allocator.TempJob);
        NativeArray<SynthData> _JobSynths = new NativeArray<SynthData>(activeSynthsIdx.Length, Allocator.TempJob);

        //Debug.Log(activeSynthsIdx.Length);

        int ActiveKeysStartIdx = 0;
        for (int i = 0; i < activeSynthsIdx.Length; i++)
        {
            _JobSynths[i] = SynthsData[activeSynthsIdx[i]];

            //Debug.LogError(activeKeyNumber[i]);
            for (int y = 0; y < activeKeyNumber[i]; y++)
            {
                _JobFrequencies[ActiveKeysStartIdx+y] = activeKeys[(i * 12)+y].frequency;
                _JobPhases[ActiveKeysStartIdx + y] = activeKeys[(i * 12) + y].OCS1phase;
                _JobPhases[TotalActiveKeyNumber + ActiveKeysStartIdx + y] = activeKeys[(i * 12) + y].OCS2phase;
                _JobDeltas[ActiveKeysStartIdx + y] = activeKeys[(i * 12) + y].delta;
            }
            ActiveKeysStartIdx += activeKeyNumber[i];
        }

        AudioJob audioJob = new AudioJob(
            _sampleRate,
            _JobSynths,
            activeKeys,
            _JobFrequencies,
            _JobPhases,
            _JobDeltas,
            AudioLayoutStorageHolder.audioLayoutStorage.filterDelayElements,
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



}


/// <summary>
///  OPTI : Minimize memory allocation on the audio thread ?
/// </summary>
[BurstCompile]
public struct AudioJob : IJob
{


    private float _sampleRate;

    [ReadOnly]
    [DeallocateOnJobCompletion]
    private NativeArray<SynthData> _JobSynths;

    private NativeArray<KeyData> _KeyData;


    [ReadOnly]
    [DeallocateOnJobCompletion]
    private NativeArray<float> _JobFrequencies;
    [DeallocateOnJobCompletion]
    private NativeArray<float> _JobPhases;
    [DeallocateOnJobCompletion]
    private NativeArray<float> _JobDeltas;


    //[WriteOnly]
    private NativeArray<FilterDelayElements> _filterDelayElements;
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
        NativeArray<FilterDelayElements> filterDelayElements,
        NativeArray<float> audioData,
        NativeArray<int> activeKeynum
       )
    {
        _JobSynths = synthsdata;
        _KeyData = KeyData;
        _sampleRate = sampleRate;
        _JobFrequencies = JobFrequencies;
        _JobPhases = JobPhases;
        _JobDeltas = JobDeltas;
        _filterDelayElements = filterDelayElements;
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

        /// Anti pop test
        /// Figured that poping happens with GarbageCollector.CollectIncremental in the profiler
        {
            //if (_activeKeynum.Length > 0)
            //{
            //    // Calculate the increment of the phase based on frequency and sample rate
            //    float phaseIncrement = 400f / _sampleRate;

            //    for (int test = 0; test < 512; test += ChannelsNum)
            //    {
            //        float phase = _JobPhases[0];

            //        float testvalue = MusicUtils.Sin(phase) * 0.35f;
            //        _JobPhases[0] = (_JobPhases[0] + phaseIncrement) % 1;


            //        /// populate all channels with the values
            //        for (int channel = 0; channel < ChannelsNum; channel++)
            //        {
            //            _audioData[test + channel] = testvalue;
            //        }

            //    }
            //    _KeyData[0] = new KeyData
            //    {
            //        frequency = 0,
            //        OCS1phase = _JobPhases[0],
            //        OCS2phase = _JobPhases[0],
            //        delta = 0,
            //        amplitudeAtRelease = 0,
            //        CutoffAmountAtRelease = 0,
            //        filterDelayElements = _filterDelayElements[0]
            //    };

            //}

            //return;
        }

        NativeArray<ADSRlayouts> KeysADSRlayouts = new NativeArray<ADSRlayouts>(_JobFrequencies.Length, Allocator.Temp);

        /// Subdivision = times the filter coefficients recalculate within the DSPbufferSize : 8 = 1 filter update per 64sample(512/8)
        /// CONSTANTS
        int filterBlockReprocessSubdivision = (int)Mathf.Pow(2,3);
        int filterBlockReprocessSize = 512 / filterBlockReprocessSubdivision;
        /// store Filter coeficie,nts instead ?
        //NativeArray<float> filterCutoffFactors = new NativeArray<float>(_JobFrequencies.Length * filterBlockReprocessSubdivision, Allocator.Temp);
        NativeArray<FilterCoefficients> filterCoefficients = new NativeArray<FilterCoefficients>(_JobFrequencies.Length * filterBlockReprocessSubdivision, Allocator.Temp);

        int activeKeyStartidx = 0;
        for (int i = 0; i < _activeKeynum.Length; i++)
        {
            ADSREnvelope ADSR = _JobSynths[i].ADSR;

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
                    attackSamples = _JobDeltas[cropedKeyIdx] < ADSR.Attack ? 
                        Mathf.Min(Mathf.CeilToInt(Mathf.Abs(_JobDeltas[cropedKeyIdx] - ADSR.Attack) * (_sampleRate)), samplePool) : 0;
                    samplePool -= attackSamples;
                    decaySamples = samplePool > 0 && _JobDeltas[cropedKeyIdx] < (ADSR.Attack + ADSR.Decay) ? 
                        Mathf.Min(Mathf.CeilToInt(Mathf.Abs(Mathf.Max(_JobDeltas[cropedKeyIdx], ADSR.Attack) - (ADSR.Attack + ADSR.Decay)) * (_sampleRate)), samplePool) : 0;
                    samplePool -= decaySamples;
                    sustainSamples = samplePool;
                }
                else
                {
                    releaseSamples = Mathf.Min(Mathf.CeilToInt((_JobDeltas[cropedKeyIdx] + ADSR.Release) * (_sampleRate)), samplePool);
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
        /// Filter cuoff factors processing
        /// OPTI ! : REUSE new FilterCoefficients instead of recreating it every time
        activeKeyStartidx = 0;
        for (int i = 0; i < _activeKeynum.Length; i++)
        {
            ADSREnvelope ADSR = _JobSynths[i].filterADSR;

            /// Build the <filterBlockReprocessSubdivision> number of Filter parrameters according to the filterADSR and filterType
            switch (_JobSynths[i].filterType)
            {
                /// lowpass filter
                case 0:
                    for (int y = 0; y < _activeKeynum[i]; y++)
                    {

                        BuildLowpassBiquadFilter(i, y, activeKeyStartidx,filterBlockReprocessSubdivision, deltaIncrement,ADSR, filterCoefficients);
                        //    int cropedKeyIdx = activeKeyStartidx + y;
                        //    float synthCutoff = Mathf.Exp(_JobSynths[i].filter.Cutoff * 5 - 5) * _JobSynths[i].filter.Cutoff;

                        //    for (int z = 0; z < filterBlockReprocessSubdivision; z++)
                        //    {

                        //        if (_KeyData[(i * 12) + y].amplitudeAtRelease == 0)
                        //        {

                        //            float delta = Mathf.Min(_JobDeltas[cropedKeyIdx] + (z * (deltaIncrement * (512 / filterBlockReprocessSubdivision))), ADSR.Attack + ADSR.Decay);

                        //            float cutoff = delta < ADSR.Attack ?
                        //                synthCutoff + (_JobSynths[i].filterEnvelopeAmount * (delta / ADSR.Attack)) :
                        //                synthCutoff + (_JobSynths[i].filterEnvelopeAmount * (((1 - (delta - ADSR.Attack) / ADSR.Decay)) * (1 - ADSR.Sustain) + ADSR.Sustain));
                        //            filterCoefficients[(cropedKeyIdx * filterBlockReprocessSubdivision) + z] = new FilterCoefficients().BuildLowpassCoefficients(cutoff, _JobSynths[i].filter.Resonance);
                        //        }
                        //        else
                        //        {
                        //            float delta = Mathf.Max(_JobDeltas[cropedKeyIdx] - (z * (deltaIncrement * (512 / filterBlockReprocessSubdivision))), -ADSR.Release);
                        //            float cutoff = synthCutoff + ((1 - (-delta / ADSR.Release)) * _KeyData[i * 12 + y].CutoffAmountAtRelease) * _JobSynths[i].filterEnvelopeAmount;
                        //            filterCoefficients[(cropedKeyIdx * filterBlockReprocessSubdivision) + z] = new FilterCoefficients().BuildLowpassCoefficients(cutoff, _JobSynths[i].filter.Resonance);

                        //        }
                        //    }
                    }
                    activeKeyStartidx += _activeKeynum[i];
                    break;
                case 1:
                    for (int y = 0; y < _activeKeynum[i]; y++)
                    {
                        BuildHighpassBiquadFilter(i, y, activeKeyStartidx, filterBlockReprocessSubdivision, deltaIncrement, ADSR, filterCoefficients);
                    }
                    activeKeyStartidx += _activeKeynum[i];
                    break;
                case 2:
                    for (int y = 0; y < _activeKeynum[i]; y++)
                    {
                        BuildBandpassBiquadFilter(i, y, activeKeyStartidx, filterBlockReprocessSubdivision, deltaIncrement, ADSR, filterCoefficients);
                    }
                    activeKeyStartidx += _activeKeynum[i];
                    break;

            }
        }




        ///phases for each key playing
        NativeArray<float> OSC1phaseIncrement = new NativeArray<float>(_JobFrequencies.Length + 1, Allocator.Temp);
        NativeArray<float> OSC2phaseIncrement = new NativeArray<float>(_JobFrequencies.Length + 1, Allocator.Temp);

        //Debug.Log(_JobDeltas[0]);

        float value;
        activeKeyStartidx = 0;
        for (int i = 0; i < _activeKeynum.Length; i++)
        {
            ADSREnvelope ADSR = _JobSynths[i].ADSR;

            for (int y = activeKeyStartidx; y < activeKeyStartidx + _activeKeynum[i]; y++)
            {
                int fullKeyIdx = i * 12 + (y - activeKeyStartidx);
                _filterDelayElements[fullKeyIdx] = _KeyData[fullKeyIdx].filterDelayElements;

                OSC1phaseIncrement[y] = (_JobFrequencies[y]*Mathf.Pow(2f,_JobSynths[i].Osc1Fine/ 1200f)) / _sampleRate;
                OSC2phaseIncrement[y] = (_JobFrequencies[y]*Mathf.Pow(2f, _JobSynths[i].Osc2Fine / 1200f) * Mathf.Pow(2.0f, _JobSynths[i].Osc2Semi / 12.0f)) / _sampleRate;
                int sampleStage = 0;

                /// shrink the the potential iteration of opti
                int attackFilterBlockNum = Mathf.Min(filterBlockReprocessSubdivision, KeysADSRlayouts[y].AttackSamples);
                int decayFilterBlockNum = Mathf.Min(filterBlockReprocessSubdivision, KeysADSRlayouts[y].DecaySamples);
                int sustainFilterBlockNum = Mathf.Min(filterBlockReprocessSubdivision, KeysADSRlayouts[y].SustainSamples);
                int releaseFilterBlockNum = Mathf.Min(filterBlockReprocessSubdivision, KeysADSRlayouts[y].ReleaseSamples);

                /// Filter ATTACK + ATTACK
                for (int z = 0; z < attackFilterBlockNum; z++)
                {
                    ///reprocess the filter coefficients for each block
                    //FilterCoefficients filterCoefficients = new FilterCoefficients(filterCutoffFactors[(y * filterBlockReprocessSubdivision) + 0], _JobSynths[i].filter.Resonance);
                    int attackSamples = Mathf.Min(KeysADSRlayouts[y].AttackSamples, filterBlockReprocessSize * (z + 1));
                    ///ATTACK
                    /// VERIFY SAMPEL STAGE
                    for (; sampleStage < attackSamples; sampleStage += ChannelsNum)
                    {
                        float OSC1phase = _JobPhases[y];
                        float OSC2phase = _JobPhases[_JobFrequencies.Length + y];

                        float effectiveAmplitude = (_JobDeltas[y] / ADSR.Attack) * _JobSynths[i].amplitude;
                        _JobDeltas[y] += deltaIncrement;

                        value = ((MusicUtils.Sin(OSC1phase) * _JobSynths[i].Osc1SinSawSquareFactor.x) + (MusicUtils.Saw(OSC1phase) * _JobSynths[i].Osc1SinSawSquareFactor.y) + (MusicUtils.Square(OSC1phase, _JobSynths[i].Osc1PW) * _JobSynths[i].Osc1SinSawSquareFactor.z)) * effectiveAmplitude;
                        value += ((MusicUtils.Sin(OSC2phase) * _JobSynths[i].Osc2SinSawSquareFactor.x) + (MusicUtils.Saw(OSC2phase) * _JobSynths[i].Osc2SinSawSquareFactor.y) + (MusicUtils.Square(OSC2phase, _JobSynths[i].Osc2PW) * _JobSynths[i].Osc2SinSawSquareFactor.z)) * effectiveAmplitude;
                        _JobPhases[y] = (_JobPhases[y] + OSC1phaseIncrement[y]) % 1;
                        _JobPhases[_JobFrequencies.Length + y] = (_JobPhases[_JobFrequencies.Length + y] + OSC2phaseIncrement[y]) % 1;

                        (float filteredValue, FilterDelayElements newDelayElements) = ApplyBiquadFilter(value, filterCoefficients[(y * filterBlockReprocessSubdivision) + 0], _filterDelayElements[fullKeyIdx]);
                        _filterDelayElements[fullKeyIdx] = newDelayElements;

                        /// populate all channels with the values
                        for (int channel = 0; channel < ChannelsNum; channel++)
                        {
                            _audioData[sampleStage + channel] += filteredValue;
                        }
                    }

                }
                /// Filter DECAY + DECAY
                for (int z = 0; z < decayFilterBlockNum; z++)
                {
                    ///reprocess the filter coefficients for each block
                    //FilterCoefficients filterCoefficients = new FilterCoefficients(filterCutoffFactors[(y * filterBlockReprocessSubdivision) +0], _JobSynths[i].filter.Resonance);
                    int decaySamples = Mathf.Min(KeysADSRlayouts[y].DecaySamples, filterBlockReprocessSize * (z + 1));
                    ///DECAY
                    for (; sampleStage < decaySamples; sampleStage += ChannelsNum)
                    {
                        float OSC1phase = _JobPhases[y];
                        float OSC2phase = _JobPhases[_JobFrequencies.Length + y];

                        float effectiveAmplitude = _JobSynths[i].amplitude - ((((_JobDeltas[y] - ADSR.Attack) / ADSR.Decay) * (1 - ADSR.Sustain)) * _JobSynths[i].amplitude);
                        _JobDeltas[y] += deltaIncrement;

                        value = ((MusicUtils.Sin(OSC1phase) * _JobSynths[i].Osc1SinSawSquareFactor.x) + (MusicUtils.Saw(OSC1phase) * _JobSynths[i].Osc1SinSawSquareFactor.y) + (MusicUtils.Square(OSC1phase, _JobSynths[i].Osc1PW) * _JobSynths[i].Osc1SinSawSquareFactor.z)) * effectiveAmplitude;
                        value += ((MusicUtils.Sin(OSC2phase) * _JobSynths[i].Osc2SinSawSquareFactor.x) + (MusicUtils.Saw(OSC2phase) * _JobSynths[i].Osc2SinSawSquareFactor.y) + (MusicUtils.Square(OSC2phase, _JobSynths[i].Osc2PW) * _JobSynths[i].Osc2SinSawSquareFactor.z)) * effectiveAmplitude;
                        _JobPhases[y] = (_JobPhases[y] + OSC1phaseIncrement[y]) % 1;
                        _JobPhases[_JobFrequencies.Length + y] = (_JobPhases[_JobFrequencies.Length + y] + OSC2phaseIncrement[y]) % 1;

                        (float filteredValue, FilterDelayElements newDelayElements) = ApplyBiquadFilter(value, filterCoefficients[(y * filterBlockReprocessSubdivision) + 0], _filterDelayElements[fullKeyIdx]);
                        _filterDelayElements[fullKeyIdx] = newDelayElements;

                        /// populate all channels with the values
                        for (int channel = 0; channel < ChannelsNum; channel++)
                        {
                            _audioData[sampleStage + channel] += filteredValue;
                        }

                    }

                }
                /// Filter SUSTAIN + SUSTAIN
                for (int z = 0; z < sustainFilterBlockNum; z++)
                {
                    ///reprocess the filter coefficients for each block
                    //FilterCoefficients filterCoefficients = new FilterCoefficients(filterCutoffFactors[(y * filterBlockReprocessSubdivision) + 0], _JobSynths[i].filter.Resonance);
                    int sustainSamples = Mathf.Min(KeysADSRlayouts[y].SustainSamples, filterBlockReprocessSize * (z + 1));
                    ///SUSTAIN
                    for (; sampleStage < sustainSamples; sampleStage += ChannelsNum)
                    {
                        float OSC1phase = _JobPhases[y];
                        float OSC2phase = _JobPhases[_JobFrequencies.Length + y];

                        float effectiveAmplitude = ADSR.Sustain * _JobSynths[i].amplitude;
                        //_JobDeltas[y] = ADSR.Attack + ADSR.Decay;
                        _JobDeltas[y] += deltaIncrement;

                        value = ((MusicUtils.Sin(OSC1phase) * _JobSynths[i].Osc1SinSawSquareFactor.x) + (MusicUtils.Saw(OSC1phase) * _JobSynths[i].Osc1SinSawSquareFactor.y) + (MusicUtils.Square(OSC1phase, _JobSynths[i].Osc1PW) * _JobSynths[i].Osc1SinSawSquareFactor.z)) * effectiveAmplitude;
                        value += ((MusicUtils.Sin(OSC2phase) * _JobSynths[i].Osc2SinSawSquareFactor.x) + (MusicUtils.Saw(OSC2phase) * _JobSynths[i].Osc2SinSawSquareFactor.y) + (MusicUtils.Square(OSC2phase, _JobSynths[i].Osc2PW) * _JobSynths[i].Osc2SinSawSquareFactor.z)) * effectiveAmplitude;
                        _JobPhases[y] = (_JobPhases[y] + OSC1phaseIncrement[y]) % 1;
                        _JobPhases[_JobFrequencies.Length + y] = (_JobPhases[_JobFrequencies.Length + y] + OSC2phaseIncrement[y]) % 1;

                        (float filteredValue, FilterDelayElements newDelayElements) = ApplyBiquadFilter(value, filterCoefficients[(y * filterBlockReprocessSubdivision) + 0], _filterDelayElements[fullKeyIdx]);
                        _filterDelayElements[fullKeyIdx] = newDelayElements;

                        /// populate all channels with the values
                        for (int channel = 0; channel < ChannelsNum; channel++)
                        {
                            _audioData[sampleStage + channel] += filteredValue;
                        }

                    }

                }
                /// Filter RELEASE + RELEASE
                for (int z = 0; z < releaseFilterBlockNum; z++)
                {
                    ///reprocess the filter coefficients for each block
                    //FilterCoefficients filterCoefficients = new FilterCoefficients(filterCutoffFactors[(y * filterBlockReprocessSubdivision) +0], _JobSynths[i].filter.Resonance);
                    int releaseSamples = Mathf.Min(KeysADSRlayouts[y].ReleaseSamples, filterBlockReprocessSize * (z + 1));
                    ///RELEASE
                    for (; sampleStage < releaseSamples; sampleStage += ChannelsNum)
                    {

                        float OSC1phase = _JobPhases[y];
                        float OSC2phase = _JobPhases[_JobFrequencies.Length + y];

                        ///3.6 is an arbitrary Exponential factor for the amplitude to be as close as possible to 0 at the end 
                        float factor = -_JobDeltas[y] / ADSR.Release;
                        float effectiveAmplitude = Mathf.Exp(-1.6f * factor)*(1- factor) * _KeyData[(i * 12) + (y - activeKeyStartidx)].amplitudeAtRelease;
                        /// the delta is counting is inverted during release
                        _JobDeltas[y] -= deltaIncrement;

                        value = ((MusicUtils.Sin(OSC1phase) * _JobSynths[i].Osc1SinSawSquareFactor.x) + (MusicUtils.Saw(OSC1phase) * _JobSynths[i].Osc1SinSawSquareFactor.y) + (MusicUtils.Square(OSC1phase, _JobSynths[i].Osc1PW) * _JobSynths[i].Osc1SinSawSquareFactor.z)) * effectiveAmplitude;
                        value += ((MusicUtils.Sin(OSC2phase) * _JobSynths[i].Osc2SinSawSquareFactor.x) + (MusicUtils.Saw(OSC2phase) * _JobSynths[i].Osc2SinSawSquareFactor.y) + (MusicUtils.Square(OSC2phase, _JobSynths[i].Osc2PW) * _JobSynths[i].Osc2SinSawSquareFactor.z)) * effectiveAmplitude;
                        _JobPhases[y] = (_JobPhases[y] + OSC1phaseIncrement[y]) % 1;
                        _JobPhases[_JobFrequencies.Length + y] = (_JobPhases[_JobFrequencies.Length + y] + OSC2phaseIncrement[y]) % 1;

                        (float filteredValue, FilterDelayElements newDelayElements) = ApplyBiquadFilter(value, filterCoefficients[(y * filterBlockReprocessSubdivision) + 0], _filterDelayElements[fullKeyIdx]);
                        _filterDelayElements[fullKeyIdx] = newDelayElements;

                        /// populate all channels with the values
                        for (int channel = 0; channel < ChannelsNum; channel++)
                        {
                            _audioData[sampleStage + channel] += filteredValue;
                        }

                    }

                }


            }


            activeKeyStartidx += _activeKeynum[i];
        }

        ///Nullify keys that finished playing from the activeKeys array and collapse it to keep it continuous
        activeKeyStartidx = 0;
        for (int i = 0; i < _JobSynths.Length; i++)
        {
            int newkeysNum = 0;
            ADSREnvelope ADSR = _JobSynths[i].ADSR;
            int remainingActiveKeys = _activeKeynum[i];

            for (int y = 0; y < _activeKeynum[i]; y++)
            {
                int fullKeyIdx = (i * 12) + y - (_activeKeynum[i] - remainingActiveKeys);
                int cropedKeyIdx = activeKeyStartidx + y ;

                if (_JobDeltas[cropedKeyIdx] > -ADSR.Release)
                {
                    _KeyData[fullKeyIdx] = new KeyData { 
                        frequency = _JobFrequencies[cropedKeyIdx], 
                        OCS1phase = _JobPhases[cropedKeyIdx], 
                        OCS2phase = _JobPhases[_JobFrequencies.Length+ cropedKeyIdx], 
                        delta = _JobDeltas[cropedKeyIdx], 
                        amplitudeAtRelease = _KeyData[fullKeyIdx].amplitudeAtRelease,
                        CutoffAmountAtRelease = _KeyData[fullKeyIdx].CutoffAmountAtRelease,
                        filterDelayElements = _filterDelayElements[fullKeyIdx]
                    };
                    newkeysNum++;
                }
                else
                {
                    //int z = (i * 12) + y; ??
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


    /// <summary>
    /// PUT FILTER RELATED FUNCTION IN A SEPERATED FILE
    /// </summary>
    public (float,FilterDelayElements) ApplyBiquadFilter(float input,FilterCoefficients coefficients,FilterDelayElements delayElements)
    {
        float output = coefficients.b0 * input + coefficients.b1 * delayElements.x0 + coefficients.b2 * delayElements.x1 - coefficients.a1 * delayElements.y0 - coefficients.a2 * delayElements.y1;

        delayElements.x1 = delayElements.x0;
        delayElements.x0 = input;

        delayElements.y1 = delayElements.y0;
        delayElements.y0 = output;



        return (output, delayElements);
    }
    public void BuildLowpassBiquadFilter(int i,int y,int activeKeyStartidx,int filterBlockReprocessSubdivision, float deltaIncrement, ADSREnvelope ADSR, NativeArray<FilterCoefficients> filterCoefficients)
    {
        int cropedKeyIdx = activeKeyStartidx + y;
        float synthCutoff = Mathf.Exp(_JobSynths[i].filter.Cutoff * 5 - 5) * _JobSynths[i].filter.Cutoff;

        for (int z = 0; z < filterBlockReprocessSubdivision; z++)
        {

            if (_KeyData[(i * 12) + y].amplitudeAtRelease == 0)
            {

                float delta = Mathf.Min(_JobDeltas[cropedKeyIdx] + (z * (deltaIncrement * (512 / filterBlockReprocessSubdivision))), ADSR.Attack + ADSR.Decay);

                float cutoff = delta < ADSR.Attack ?
                    synthCutoff + (_JobSynths[i].filterEnvelopeAmount * (delta / ADSR.Attack)) :
                    synthCutoff + (_JobSynths[i].filterEnvelopeAmount * (((1 - (delta - ADSR.Attack) / ADSR.Decay)) * (1 - ADSR.Sustain) + ADSR.Sustain));
                filterCoefficients[(cropedKeyIdx * filterBlockReprocessSubdivision) + z] = new FilterCoefficients().BuildLowpassCoefficients(cutoff, _JobSynths[i].filter.Resonance);
            }
            else
            {
                float delta = Mathf.Max(_JobDeltas[cropedKeyIdx] - (z * (deltaIncrement * (512 / filterBlockReprocessSubdivision))), -ADSR.Release);
                float cutoff = synthCutoff + ((1 - (-delta / ADSR.Release)) * _KeyData[i * 12 + y].CutoffAmountAtRelease) * _JobSynths[i].filterEnvelopeAmount;
                filterCoefficients[(cropedKeyIdx * filterBlockReprocessSubdivision) + z] = new FilterCoefficients().BuildLowpassCoefficients(cutoff, _JobSynths[i].filter.Resonance);

            }
        }
        
    }
    public void BuildHighpassBiquadFilter(int i, int y, int activeKeyStartidx, int filterBlockReprocessSubdivision, float deltaIncrement, ADSREnvelope ADSR, NativeArray<FilterCoefficients> filterCoefficients)
    {
        int cropedKeyIdx = activeKeyStartidx + y;
        float synthCutoff = Mathf.Exp(_JobSynths[i].filter.Cutoff * 5 - 5) * _JobSynths[i].filter.Cutoff;

        for (int z = 0; z < filterBlockReprocessSubdivision; z++)
        {

            if (_KeyData[(i * 12) + y].amplitudeAtRelease == 0)
            {

                float delta = Mathf.Min(_JobDeltas[cropedKeyIdx] + (z * (deltaIncrement * (512 / filterBlockReprocessSubdivision))), ADSR.Attack + ADSR.Decay);

                float cutoff = delta < ADSR.Attack ?
                    synthCutoff + (_JobSynths[i].filterEnvelopeAmount * (delta / ADSR.Attack)) :
                    synthCutoff + (_JobSynths[i].filterEnvelopeAmount * (((1 - (delta - ADSR.Attack) / ADSR.Decay)) * (1 - ADSR.Sustain) + ADSR.Sustain));
                filterCoefficients[(cropedKeyIdx * filterBlockReprocessSubdivision) + z] = new FilterCoefficients().BuildHighpassCoefficients(cutoff, _JobSynths[i].filter.Resonance);
            }
            else
            {
                float delta = Mathf.Max(_JobDeltas[cropedKeyIdx] - (z * (deltaIncrement * (512 / filterBlockReprocessSubdivision))), -ADSR.Release);
                float cutoff = synthCutoff + ((1 - (-delta / ADSR.Release)) * _KeyData[i * 12 + y].CutoffAmountAtRelease) * _JobSynths[i].filterEnvelopeAmount;
                filterCoefficients[(cropedKeyIdx * filterBlockReprocessSubdivision) + z] = new FilterCoefficients().BuildHighpassCoefficients(cutoff, _JobSynths[i].filter.Resonance);

            }
        }

    }
    public void BuildBandpassBiquadFilter(int i, int y, int activeKeyStartidx, int filterBlockReprocessSubdivision, float deltaIncrement, ADSREnvelope ADSR, NativeArray<FilterCoefficients> filterCoefficients)
    {
        int cropedKeyIdx = activeKeyStartidx + y;
        float synthCutoff = Mathf.Exp(_JobSynths[i].filter.Cutoff * 5 - 5) * _JobSynths[i].filter.Cutoff;

        for (int z = 0; z < filterBlockReprocessSubdivision; z++)
        {

            if (_KeyData[(i * 12) + y].amplitudeAtRelease == 0)
            {

                float delta = Mathf.Min(_JobDeltas[cropedKeyIdx] + (z * (deltaIncrement * (512 / filterBlockReprocessSubdivision))), ADSR.Attack + ADSR.Decay);

                float cutoff = delta < ADSR.Attack ?
                    synthCutoff + (_JobSynths[i].filterEnvelopeAmount * (delta / ADSR.Attack)) :
                    synthCutoff + (_JobSynths[i].filterEnvelopeAmount * (((1 - (delta - ADSR.Attack) / ADSR.Decay)) * (1 - ADSR.Sustain) + ADSR.Sustain));
                filterCoefficients[(cropedKeyIdx * filterBlockReprocessSubdivision) + z] = new FilterCoefficients().BuildBandpassCoefficients(cutoff, _JobSynths[i].filter.Resonance);
            }
            else
            {
                float delta = Mathf.Max(_JobDeltas[cropedKeyIdx] - (z * (deltaIncrement * (512 / filterBlockReprocessSubdivision))), -ADSR.Release);
                float cutoff = synthCutoff + ((1 - (-delta / ADSR.Release)) * _KeyData[i * 12 + y].CutoffAmountAtRelease) * _JobSynths[i].filterEnvelopeAmount;
                filterCoefficients[(cropedKeyIdx * filterBlockReprocessSubdivision) + z] = new FilterCoefficients().BuildBandpassCoefficients(cutoff, _JobSynths[i].filter.Resonance);

            }
        }

    }

}
