using System;
using System.Linq;
using System.Numerics;
using MusicNamespace;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Entities.UniversalDelegates;
using Unity.Jobs;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;

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
    /// <summary>
    /// //1rst is selectedSynth 6 other for playback
    /// exclude selectedSynth
    /// </summary>
    public NativeArray<int> SynthIdxTOactiveSynthsIdxMap;

    public NativeArray<float> _OCSphaseIncrements;
    public NativeArray<FilterDelayElements> filterDelayElements;
    public NativeArray<float> SynthGlideBaseFz;

    public NativeArray<SynthPlaybackAudioBundle> PlaybackAudioBundles;
    public NativeArray<PlaybackAudioBundleContext> PlaybackAudioBundlesContext;



    private EntityManager entityManager;

    private NativeArray<float> _audioData;
    public static JobHandle _Audiojobhandle;
    private int _sampleRate;

    public static AudioRingBuffer<KeysBuffer> audioRingBuffer;

    private const int NumChannels = 1; // Mono audio

    const float DeltaTime = 258f / 48000f;

    /// Temporary -> TO DO : Synth parameter
    public static float MaxVolume = 0.2f;

    private void Start()
    {
        entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;

        AudioLayoutStorageHolder.audioLayoutStorage.PlaybackContextResetRequired = new NativeQueue<int>(Allocator.Persistent);
        AudioLayoutStorageHolder.audioLayoutStorage.PlaybackActivationUpdateRequired = new NativeQueue<int>(Allocator.Persistent);
        AudioLayoutStorageHolder.audioLayoutStorage.PlaybackDeactivationUpdateRequired = new NativeQueue<int>(Allocator.Persistent);
        AudioLayoutStorageHolder.audioLayoutStorage.PlaybackWriteUpdateRequired = new NativeQueue<(int,SynthPlaybackAudioBundle)>(Allocator.Persistent);

        //Debug.Log(AudioSettings.outputSampleRate);
        /// TEST
        //SynthsData[0] = entityManager.GetComponentData<SynthData>(WeaponSynthEntity);
        //for (int i = 0; i < SynthsData.Length; i++)
        //{
        //    SynthsData[i] = entityManager.GetComponentData<SynthData>(WeaponSynthEntity);
        //}

        //gameObject.SetActive(false);

    }

    private void Awake()
    {
        _sampleRate = AudioSettings.outputSampleRate;
        _audioData = new NativeArray<float>(512, Allocator.Persistent);
        SynthIdxTOactiveSynthsIdxMap = new NativeArray<int>(6, Allocator.Persistent);
        //SynthIdxTOactiveSynthsIdxMap[0] = 0;
        /// assiciate 99 to null for better debugging
        for (int i = 1; i < SynthIdxTOactiveSynthsIdxMap.Length; i++){SynthIdxTOactiveSynthsIdxMap[i] = 99;}

        //_filterDelayElements = new NativeArray<FilterDelayElements>(1, Allocator.Persistent);
        //audiojobCompleted = false; 
        int ringBufferCapacity = 4;
        //audioLayoutStorage = new AudioLayoutStorage();
        //AudioLayoutStorageHolder.audioLayoutStorage = new AudioLayoutStorage();
        audioRingBuffer = new AudioRingBuffer<KeysBuffer>(ringBufferCapacity);
        audioRingBuffer.InitializeBuffer(ringBufferCapacity);
        _OCSphaseIncrements = new NativeArray<float>(8, Allocator.Persistent);
        SynthGlideBaseFz = new NativeArray<float>(2, Allocator.Persistent);
        /// 4 elements per key -> 12 max keys per synth = 48 elements per synth
        /// starting active synth(48) + starting synth playback(48) = 96
        /// 

        filterDelayElements = new NativeArray<FilterDelayElements>(96, Allocator.Persistent);

        //activeKeys = new NativeArray<KeyData>(12, Allocator.Persistent);
        //activeKeyNumber = new NativeArray<int>(1, Allocator.Persistent);

        ///// TEST AUDIO BUNDLE
        //SynthsData = new NativeArray<SynthData>(1, Allocator.Persistent);

        //PlaybackAudioBundles = new NativeArray<SynthPlaybackAudioBundle>(1, Allocator.Persistent);
        //PlaybackAudioBundlesContext = new NativeArray<PlaybackAudioBundleContext>(1, Allocator.Persistent);
        //for (int i = 0; i < PlaybackAudioBundles.Length; i++)
        //{
        //    SynthPlaybackAudioBundle audiobundle = PlaybackAudioBundles[i];

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

    private void OnDestroy()
    {

        int ringBufferCapacity = 4;

        SynthsData.Dispose();
        activeKeys.Dispose();
        activeKeyNumber.Dispose();
        activeSynthsIdx.Dispose();
        _audioData.Dispose();
        SynthIdxTOactiveSynthsIdxMap.Dispose();
        _OCSphaseIncrements.Dispose();
        SynthGlideBaseFz.Dispose();
        filterDelayElements.Dispose();
        PlaybackAudioBundles.Dispose();
        PlaybackAudioBundlesContext.Dispose();

        audioRingBuffer.DisposeBuffer(ringBufferCapacity);

        AudioLayoutStorageHolder.audioLayoutStorage.PlaybackContextResetRequired.Dispose();
        AudioLayoutStorageHolder.audioLayoutStorage.PlaybackActivationUpdateRequired.Dispose();
        AudioLayoutStorageHolder.audioLayoutStorage.PlaybackDeactivationUpdateRequired.Dispose();
        AudioLayoutStorageHolder.audioLayoutStorage.PlaybackWriteUpdateRequired.Dispose();
    }


    private void Update()
    {

    }

    // Callback method for audio processing
    unsafe
    private void OnAudioFilterRead(float[] data, int channels)
    {

        /// To reset audio playbacks and keep them in sync with the main thread
        while (AudioLayoutStorageHolder.audioLayoutStorage.PlaybackContextResetRequired.Count > 0)
        {
            int SynthIdx = AudioLayoutStorageHolder.audioLayoutStorage.PlaybackContextResetRequired.Dequeue();
            int contextIndex = SynthIdxTOactiveSynthsIdxMap[SynthIdx];
            PlaybackAudioBundlesContext[contextIndex - 1] = new PlaybackAudioBundleContext { PlaybackKeyStartIndex = 0, PlaybackTime = 0 };
            activeKeyNumber[contextIndex] = 0;
        }

        /// 1 or more synth active playback data are being updated
        if (AudioLayoutStorageHolder.audioLayoutStorage.PlaybackWriteUpdateRequired.Count > 0)
        {
            var newPlaybackBundle = new NativeArray<SynthPlaybackAudioBundle>(PlaybackAudioBundles.Length, Allocator.Persistent);
            PlaybackAudioBundles.CopyTo(newPlaybackBundle);

            while (AudioLayoutStorageHolder.audioLayoutStorage.PlaybackWriteUpdateRequired.Count > 0)
            {
                (int, SynthPlaybackAudioBundle) updatedPlaybackInfo = AudioLayoutStorageHolder.audioLayoutStorage.PlaybackWriteUpdateRequired.Dequeue();
                newPlaybackBundle[updatedPlaybackInfo.Item1] = updatedPlaybackInfo.Item2;
            }
            PlaybackAudioBundles.Dispose();
            PlaybackAudioBundles = newPlaybackBundle;
        }

        /// 1 or more synth is either being stopped or activated -> adjust the context to match the new situation
        int weight = AudioLayoutStorageHolder.audioLayoutStorage.UpdatedActivePlaybackWeight;
        if (AudioLayoutStorageHolder.audioLayoutStorage.PlaybackDeactivationUpdateRequired.Count>0|| weight > 0)
        {    
            //Debug.Log(activeKeyNumber.Length);
            //Debug.Log(weight);
            var newActiveKeys = new NativeArray<KeyData>((activeKeyNumber.Length + weight) * 12, Allocator.Persistent);
            var newActiveKeyNumber = new NativeArray<int>(activeKeyNumber.Length + weight, Allocator.Persistent);
            var newActiveSynthsIdx = new NativeArray<int>(activeSynthsIdx.Length + weight, Allocator.Persistent);
            int numOfPops = AudioLayoutStorageHolder.audioLayoutStorage.PlaybackDeactivationUpdateRequired.Count;
        
            int numOfContextsBeforeAdd = activeKeyNumber.Length - numOfPops;
            /// keep in mind PlaybackAudioBundleContext are -1 compared to rest of context ?
            var newActivePlaybackContext = new NativeArray<PlaybackAudioBundleContext>(PlaybackAudioBundlesContext.Length + weight, Allocator.Persistent);
            AudioLayoutStorageHolder.audioLayoutStorage.UpdatedActivePlaybackWeight = 0;

            /// 1 synth is the active one and permanent
            newActiveKeyNumber[0] = activeKeyNumber[0];
            newActiveSynthsIdx[0] = activeSynthsIdx[0];
            for (int i = 0; i < 12; i++)
            {
                newActiveKeys[i] = activeKeys[i];
            }
            int PopingIndex = 1;
            int popedItems = 0;
            int targetIndex = PopingIndex;

            /// first, copy the n+1 contexts and pop the no longer active synth ones (minus the last section)
            while (AudioLayoutStorageHolder.audioLayoutStorage.PlaybackDeactivationUpdateRequired.Count > 0)
            {
                int deactivationIdx = AudioLayoutStorageHolder.audioLayoutStorage.PlaybackDeactivationUpdateRequired.Dequeue();
                int SynthIdxInContext = SynthIdxTOactiveSynthsIdxMap[deactivationIdx];
                //Debug.LogError(SynthIdxTOactiveSynthsIdxMap[deactivationIdx]);
                SynthIdxTOactiveSynthsIdxMap[deactivationIdx] = 99;

                for (; (PopingIndex-1) < (SynthIdxInContext- numOfPops); PopingIndex++)
                {
                    targetIndex = PopingIndex + popedItems;
                    newActiveKeyNumber[PopingIndex] = activeKeyNumber[targetIndex];
                    newActiveSynthsIdx[PopingIndex] = activeSynthsIdx[targetIndex];
                    SynthIdxTOactiveSynthsIdxMap[activeSynthsIdx[targetIndex]] = PopingIndex;
                    newActivePlaybackContext[PopingIndex - 1] = PlaybackAudioBundlesContext[targetIndex - 1];
                    for (int y = 0; y < 12; y++)
                    {
                        newActiveKeys[(PopingIndex * 12) + y] = activeKeys[(targetIndex * 12) + y];
                    }
                }
                popedItems++;
                /// set the index after the poped synth to copy the rest of the contexts
                //PopingIndex = SynthIdxInContext;
            }
            /// Copying the rest of the contexts after le last pop (all-first if no poping)
            for (; PopingIndex < numOfContextsBeforeAdd; PopingIndex++)
            {
                targetIndex = PopingIndex + popedItems;
                newActiveKeyNumber[PopingIndex] = activeKeyNumber[targetIndex];
                newActiveSynthsIdx[PopingIndex] = activeSynthsIdx[targetIndex];
                SynthIdxTOactiveSynthsIdxMap[activeSynthsIdx[targetIndex]] = PopingIndex;
                newActivePlaybackContext[PopingIndex - 1] = PlaybackAudioBundlesContext[targetIndex - 1];
                for (int y = 0; y < 12; y++)
                {
                    newActiveKeys[(PopingIndex * 12) + y] = activeKeys[(targetIndex * 12) + y];
                }
            }
            /// Last, assign the activationIdx to the expanded array element
            for (int completedAddition = 0; AudioLayoutStorageHolder.audioLayoutStorage.PlaybackActivationUpdateRequired.Count>0; completedAddition++)
            {
                int activationIdx = AudioLayoutStorageHolder.audioLayoutStorage.PlaybackActivationUpdateRequired.Dequeue();
                newActiveSynthsIdx[numOfContextsBeforeAdd+completedAddition] = activationIdx;
                //Debug.LogWarning(activationIdx);
                SynthIdxTOactiveSynthsIdxMap[activationIdx] = numOfContextsBeforeAdd + completedAddition;
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
        

        /// Jobify ?
        if (AudioLayoutStorageHolder.audioLayoutStorage.UpdateRequirement)
        {
            if (AudioLayoutStorageHolder.audioLayoutStorage.AddSynthUpdateRequirement)
            {
                var newSynthsData = new NativeArray<SynthData>(SynthsData.Length+1, Allocator.Persistent);
                var newPlaybackBundle = new NativeArray<SynthPlaybackAudioBundle>(PlaybackAudioBundles.Length+1, Allocator.Persistent);
                /// 12 = max number of notes ; 4 = max number of unisson -> 48
                var newfilterDelayElements = new NativeArray<FilterDelayElements>((PlaybackAudioBundles.Length + 2) * 48, Allocator.Persistent);
                var newSynthGlideBaseFz = new NativeArray<float>(SynthGlideBaseFz.Length+1, Allocator.Persistent);

                for (int i = 0; i < SynthsData.Length; i++)
                {
                    newSynthsData[i] = SynthsData[i];
                    newPlaybackBundle[i] = PlaybackAudioBundles[i];
                    newSynthGlideBaseFz[i] = SynthGlideBaseFz[i];
                }
                newSynthsData[SynthsData.Length] = AudioLayoutStorageHolder.audioLayoutStorage.ReadAddSynth();
                SynthsData.Dispose();
                PlaybackAudioBundles.Dispose();
                filterDelayElements.Dispose();
                SynthGlideBaseFz.Dispose();
                SynthsData = newSynthsData;
                PlaybackAudioBundles = newPlaybackBundle;
                filterDelayElements = newfilterDelayElements;
                SynthGlideBaseFz = newSynthGlideBaseFz;

            }
            if(AudioLayoutStorageHolder.audioLayoutStorage.SelectSynthUpdateRequirement)
            {
                var newActiveSynthsIdx = new NativeArray<int>(activeSynthsIdx.Length, Allocator.Persistent);
                activeSynthsIdx.CopyTo(newActiveSynthsIdx);
                //Debug.Log(newActiveSynthsIdx[0]);
                //Debug.LogError(SynthIdxTOactiveSynthsIdxMap[newActiveSynthsIdx[0]]);
                int selectedSynth = AudioLayoutStorageHolder.audioLayoutStorage.ReadSelectSynth();
                //SynthIdxTOactiveSynthsIdxMap[newActiveSynthsIdx[0]] = ;
                newActiveSynthsIdx[0] = selectedSynth;
                //SynthIdxTOactiveSynthsIdxMap[selectedSynth] = 0;
                activeSynthsIdx.Dispose();
                activeSynthsIdx = newActiveSynthsIdx;
            }
            if(AudioLayoutStorageHolder.audioLayoutStorage.ModifySynthUpdateRequirement)
            {
                SynthsData[activeSynthsIdx[0]] = AudioLayoutStorageHolder.audioLayoutStorage.ReadModifySynth();
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
            //int playbackIndex = 0;

            //playbackIndex += (i > 0) ? PlaybackAudioBundles[activeSynthsIdx[i]].PlaybackKeys.Length : 0;
            int playbackIndex = PlaybackAudioBundlesContext[i].PlaybackKeyStartIndex;
            int skipedKey = 0;

            while (playbackIndex < PlaybackAudioBundles[playbackAudioBundlesIdx].PlaybackKeys.Length
                && PlaybackAudioBundles[playbackAudioBundlesIdx].PlaybackKeys[playbackIndex].time < PlaybackAudioBundlesContext[i].PlaybackTime)
                //&& PlaybackAudioBundles[playbackAudioBundlesIdx].PlaybackKeys[playbackIndex].time + PlaybackAudioBundles[playbackAudioBundlesIdx].PlaybackKeys[playbackIndex].lenght > PlaybackAudioBundlesContext[i].PlaybackTime)
            {
                if (PlaybackAudioBundles[playbackAudioBundlesIdx].PlaybackKeys[playbackIndex].time + PlaybackAudioBundles[playbackAudioBundlesIdx].PlaybackKeys[playbackIndex].lenght > PlaybackAudioBundlesContext[i].PlaybackTime)
                {
                    ActiveplaybackKeysFzList.Add(MusicUtils.DirectionToFrequency(PlaybackAudioBundles[playbackAudioBundlesIdx].PlaybackKeys[playbackIndex].dir));
                    playbackIndex++;
                }
                else
                {
                    skipedKey++;
                    playbackIndex++;
                }

            }

            //playbackIndex -= (i > 0) ? PlaybackAudioBundles[activeSynthsIdx[i]].PlaybackKeys.Length : 0;
            playbackIndex -= PlaybackAudioBundlesContext[i].PlaybackKeyStartIndex + skipedKey;

            ActiveplaybackKeysNumberList.Add((short)playbackIndex);
            totalNumberOfPlaybackKeys += playbackIndex;

            //not needed anymore ?
            //if(PlaybackAudioBundles[playbackAudioBundlesIdx].IsPlaying)
            PlaybackAudioBundlesContext[i] = new PlaybackAudioBundleContext { 
                PlaybackKeyStartIndex = PlaybackAudioBundlesContext[i].PlaybackKeyStartIndex, 
                PlaybackTime = PlaybackAudioBundlesContext[i].PlaybackTime + DeltaTime 
            };
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
        TotalKeysBuffer.keyFrenquecies = new NativeArray<float>(PlayerkeysBuffer.KeyNumber[0] + totalNumberOfPlaybackKeys, Allocator.Temp);
        TotalKeysBuffer.KeyNumber = new NativeArray<short>(activeSynthsIdx.Length, Allocator.Temp);

        ///fill the native array with the data
        //if(TotalKeysBuffer.KeyNumber.Length>0)
        {
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
                        float newDelta = 0;
                        if(SynthsData[0].Legato)
                        {
                            float deltaFactor = 1-((SynthsData[0].ADSR.Release + activeKeys[y].delta) / SynthsData[0].ADSR.Release);
                            /// Deduce the amplitude of the releasing key
                            float amplitude = (activeKeys[y].amplitudeAtRelease / MaxVolume) * (Mathf.Exp(-1.6f * deltaFactor) * (1 - deltaFactor));
                            /// map it to the attack of the new note to keep it continuous
                            newDelta = (amplitude * SynthsData[0].ADSR.Attack);
                        }

                        activeKeys[y] = new KeyData
                        {
                            frequency = activeKeys[y].frequency,
                            GlideStartFrenquency = SynthGlideBaseFz[0],
                            delta = newDelta,
                            OCS1phaseV1 = activeKeys[y].OCS1phaseV1,
                            OCS1phaseV2 = activeKeys[y].OCS1phaseV2,
                            OCS1phaseV3 = activeKeys[y].OCS1phaseV3,
                            OCS1phaseV4 = activeKeys[y].OCS1phaseV4,
                            OCS2phaseV1 = activeKeys[y].OCS2phaseV1,
                            OCS2phaseV2 = activeKeys[y].OCS2phaseV2,
                            OCS2phaseV3 = activeKeys[y].OCS2phaseV3,
                            OCS2phaseV4 = activeKeys[y].OCS2phaseV4,
                            amplitudeAtRelease = 0,
                            filterDelayElementsV1 = activeKeys[y].filterDelayElementsV1,
                            filterDelayElementsV2 = activeKeys[y].filterDelayElementsV2,
                            filterDelayElementsV3 = activeKeys[y].filterDelayElementsV3,
                            filterDelayElementsV4 = activeKeys[y].filterDelayElementsV4,
                        };
                        SynthGlideBaseFz[0] = activeKeys[y].frequency;
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
                        GlideStartFrenquency = activeKeys[y].GlideStartFrenquency,
                        delta = 0,
                        OCS1phaseV1 = activeKeys[y].OCS1phaseV1,
                        OCS1phaseV2 = activeKeys[y].OCS1phaseV2,
                        OCS1phaseV3 = activeKeys[y].OCS1phaseV3,
                        OCS1phaseV4 = activeKeys[y].OCS1phaseV4,
                        OCS2phaseV1 = activeKeys[y].OCS2phaseV1,
                        OCS2phaseV2 = activeKeys[y].OCS2phaseV2,
                        OCS2phaseV3 = activeKeys[y].OCS2phaseV3,
                        OCS2phaseV4 = activeKeys[y].OCS2phaseV4,
                        amplitudeAtRelease = releaseAmplitude + 0.0001f /*make sure the key is considered released*/,
                        CutoffAmountAtRelease = delta < SynthsData[synthidx].filterADSR.Attack? 
                        (delta / SynthsData[synthidx].filterADSR.Attack)://* SynthsData[synthidx].filterEnvelopeAmount: 
                        (((1 - (delta - SynthsData[synthidx].filterADSR.Attack) / SynthsData[synthidx].filterADSR.Decay)) * (1 - SynthsData[synthidx].filterADSR.Sustain) + SynthsData[synthidx].filterADSR.Sustain),// * SynthsData[synthidx].filterEnvelopeAmount,
                        filterDelayElementsV1 = activeKeys[y].filterDelayElementsV1,
                        filterDelayElementsV2 = activeKeys[y].filterDelayElementsV2,
                        filterDelayElementsV3 = activeKeys[y].filterDelayElementsV3,
                        filterDelayElementsV4 = activeKeys[y].filterDelayElementsV4
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
                        //Debug.LogError(keysBufferSlice[y]);
                        activeKeys[activeKeyNumber[0]] = new KeyData {
                            frequency = keysBufferSlice[y],
                            GlideStartFrenquency = SynthGlideBaseFz[0],
                            delta = 0,
                        };
                        activeKeyNumber[0]++;
                    }
                    /// the number of keys played simultaneously has reached its limit : start overwriting the oldest ones.
                    else
                    {
                        Debug.Log("overwrite");
                        activeKeys[overwriteKeysNum] = new KeyData { frequency = keysBufferSlice[y] , GlideStartFrenquency = SynthGlideBaseFz[0]};
                        overwriteKeysNum++;
                    }
                    SynthGlideBaseFz[0] = keysBufferSlice[y];
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
                int synthidx = activeSynthsIdx[z];

                /// Check for keys that oath to be released 
                if (keysBufferSlice.Contains(ActiveKeysSlice[y].frequency))
                {
                    ///Key already being released, check if repressed
                    if (ActiveKeysSlice[y].amplitudeAtRelease != 0)
                    {

                        float newDelta = 0;
                        if (SynthsData[0].Legato)
                        {
                            float deltaFactor = 1 - ((SynthsData[synthidx].ADSR.Release + ActiveKeysSlice[y].delta) / SynthsData[synthidx].ADSR.Release);
                            /// Deduce the amplitude of the releasing key
                            float amplitude = (ActiveKeysSlice[y].amplitudeAtRelease / MaxVolume) * (Mathf.Exp(-1.6f * deltaFactor) * (1 - deltaFactor));
                            /// map it to the attack of the new note to keep it continuous
                            newDelta = (amplitude * SynthsData[synthidx].ADSR.Attack);
                        }

                        ActiveKeysSlice[y] = new KeyData { 
                            frequency = ActiveKeysSlice[y].frequency,
                            GlideStartFrenquency = SynthGlideBaseFz[z],
                            delta = newDelta, 
                            OCS1phaseV1 = ActiveKeysSlice[y].OCS1phaseV1,
                            OCS1phaseV2 = ActiveKeysSlice[y].OCS1phaseV2,
                            OCS1phaseV3 = ActiveKeysSlice[y].OCS1phaseV3,
                            OCS1phaseV4 = ActiveKeysSlice[y].OCS1phaseV4,
                            OCS2phaseV1 = ActiveKeysSlice[y].OCS2phaseV1,
                            OCS2phaseV2 = ActiveKeysSlice[y].OCS2phaseV2,
                            OCS2phaseV3 = ActiveKeysSlice[y].OCS2phaseV3,
                            OCS2phaseV4 = ActiveKeysSlice[y].OCS2phaseV4,
                            amplitudeAtRelease = 0,
                            CutoffAmountAtRelease = ActiveKeysSlice[y].CutoffAmountAtRelease,
                            filterDelayElementsV1 = ActiveKeysSlice[y].filterDelayElementsV1,
                            filterDelayElementsV2 = ActiveKeysSlice[y].filterDelayElementsV2,
                            filterDelayElementsV3 = ActiveKeysSlice[y].filterDelayElementsV3,
                            filterDelayElementsV4 = ActiveKeysSlice[y].filterDelayElementsV4
                        };
                        SynthGlideBaseFz[z] = ActiveKeysSlice[y].frequency;
                        //Debug.LogWarning(ActiveKeysSlice[y].frequency);
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

                    float releaseAmplitude;
                    if (delta < SynthsData[synthidx].ADSR.Attack)
                        releaseAmplitude = (delta / SynthsData[synthidx].ADSR.Attack) * SynthsData[synthidx].amplitude;
                    else if (delta < SynthsData[synthidx].ADSR.Attack + SynthsData[synthidx].ADSR.Decay)
                        releaseAmplitude = SynthsData[synthidx].amplitude - (((delta - SynthsData[synthidx].ADSR.Attack) / SynthsData[synthidx].ADSR.Decay) * (1 - SynthsData[synthidx].ADSR.Sustain) * SynthsData[synthidx].amplitude);
                    else
                        releaseAmplitude = SynthsData[synthidx].ADSR.Sustain * SynthsData[synthidx].amplitude;

                    ActiveKeysSlice[y] = new KeyData {
                        frequency = ActiveKeysSlice[y].frequency,
                        GlideStartFrenquency = ActiveKeysSlice[y].GlideStartFrenquency,
                        delta = 0,
                        OCS1phaseV1 = ActiveKeysSlice[y].OCS1phaseV1,
                        OCS1phaseV2 = ActiveKeysSlice[y].OCS1phaseV2,
                        OCS1phaseV3 = ActiveKeysSlice[y].OCS1phaseV3,
                        OCS1phaseV4 = ActiveKeysSlice[y].OCS1phaseV4,
                        OCS2phaseV1 = ActiveKeysSlice[y].OCS2phaseV1,
                        OCS2phaseV2 = ActiveKeysSlice[y].OCS2phaseV2,
                        OCS2phaseV3 = ActiveKeysSlice[y].OCS2phaseV3,
                        OCS2phaseV4 = ActiveKeysSlice[y].OCS2phaseV4,
                        amplitudeAtRelease = releaseAmplitude + 0.00001f, /*make sure the key is considered released*/
                        CutoffAmountAtRelease = delta < SynthsData[synthidx].filterADSR.Attack ?
                        (delta / SynthsData[synthidx].filterADSR.Attack) :
                        (((1 - (delta - SynthsData[synthidx].filterADSR.Attack) / SynthsData[synthidx].filterADSR.Decay)) * (1 - SynthsData[synthidx].filterADSR.Sustain) + SynthsData[synthidx].filterADSR.Sustain),
                        filterDelayElementsV1 = ActiveKeysSlice[y].filterDelayElementsV1,
                        filterDelayElementsV2 = ActiveKeysSlice[y].filterDelayElementsV2,
                        filterDelayElementsV3 = ActiveKeysSlice[y].filterDelayElementsV3,
                        filterDelayElementsV4 = ActiveKeysSlice[y].filterDelayElementsV4
                    };

                    PlaybackAudioBundlesContext[i] = new PlaybackAudioBundleContext { 
                        PlaybackKeyStartIndex = PlaybackAudioBundlesContext[i].PlaybackKeyStartIndex +1,
                        PlaybackTime = PlaybackAudioBundlesContext[i].PlaybackTime
                    };
                }
            }
       
            /// Check if there are new note played and add it to the activeKeys array
            //int overwriteKeysNum = 0;
            for (int y = 0; y < TotalKeysBuffer.KeyNumber[z]; y++)
            {
                /// The key doesnt exist -> activate it
                if (!ActiveKeysSlice.Any(ActiveKeysSlice => ActiveKeysSlice.frequency == keysBufferSlice[y]))
                {
                    if (activeKeyNumber[z] < 12)
                    {
                        ///safe
                        //if (z > (keysBufferSlice.Length+1)) Debug.LogError("error from here");

                        activeKeys[(z * 12)+activeKeyNumber[z]] = new KeyData { 
                            frequency = keysBufferSlice[y], 
                            GlideStartFrenquency = SynthGlideBaseFz[z] 
                        };
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
                    SynthGlideBaseFz[z] = keysBufferSlice[y];
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
        ///OSC1 + OSC2 phases array * number of max unisson (4)
        NativeArray<float> _JobPhases = new NativeArray<float>(TotalActiveKeyNumber*2*4, Allocator.TempJob);
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
                _JobDeltas[ActiveKeysStartIdx + y] = activeKeys[(i * 12) + y].delta;
                _JobPhases[(ActiveKeysStartIdx + y)*4] = activeKeys[(i * 12) + y].OCS1phaseV1;
                _JobPhases[(ActiveKeysStartIdx + y) * 4 + 1] = activeKeys[(i * 12) + y].OCS1phaseV2;
                _JobPhases[(ActiveKeysStartIdx + y) * 4 + 2] = activeKeys[(i * 12) + y].OCS1phaseV3;
                _JobPhases[(ActiveKeysStartIdx + y) * 4 + 3] = activeKeys[(i * 12) + y].OCS1phaseV4;
                _JobPhases[(TotalActiveKeyNumber + ActiveKeysStartIdx + y)*4] = activeKeys[(i * 12) + y].OCS2phaseV1;
                _JobPhases[(TotalActiveKeyNumber + ActiveKeysStartIdx + y) * 4 + 1] = activeKeys[(i * 12) + y].OCS2phaseV2;
                _JobPhases[(TotalActiveKeyNumber + ActiveKeysStartIdx + y) * 4 + 2] = activeKeys[(i * 12) + y].OCS2phaseV3;
                _JobPhases[(TotalActiveKeyNumber + ActiveKeysStartIdx + y) * 4 + 3] = activeKeys[(i * 12) + y].OCS2phaseV4;
            }
            ActiveKeysStartIdx += activeKeyNumber[i];
        }

        //AudioJob audioJob = new AudioJob(
        //    _sampleRate,
        //    _JobSynths,
        //    activeKeys,
        //    _JobFrequencies,
        //    _JobPhases,
        //    _JobDeltas,
        //    filterDelayElements,
        //    _audioData,
        //    activeKeyNumber,
        //    _OCSphaseIncrements
        //    //SynthGlideBaseFz
        //    );
        AudioJob audioJob = new AudioJob {
            _sampleRate = _sampleRate,
            _JobSynths = _JobSynths,
            _KeyData = activeKeys,
            _JobFrequencies = _JobFrequencies,
            _JobPhases = _JobPhases,
            _JobDeltas = _JobDeltas,
            _filterDelayElements = filterDelayElements,
            _audioData = _audioData,
            _activeKeynum = activeKeyNumber,
            _OCSphaseIncrements = _OCSphaseIncrements
            //SynthGlideBaseFz
            };


        _Audiojobhandle = audioJob.Schedule(_Audiojobhandle);

        _Audiojobhandle.Complete();


        //_JobFrequencies.Dispose();
        //_JobPhases.Dispose();
        //_JobDeltas.Dispose();
        //_JobSynths.Dispose();

        _audioData.CopyTo(data);


    }



}


/// <summary>
///  OPTI : Minimize memory allocation on the audio thread ?
/// </summary>
[BurstCompile]
public struct AudioJob : IJob
{


    public float _sampleRate;

    [ReadOnly]
    [DeallocateOnJobCompletion]
    public NativeArray<SynthData> _JobSynths;

    public NativeArray<KeyData> _KeyData;


    [ReadOnly]
    [DeallocateOnJobCompletion]
    public NativeArray<float> _JobFrequencies;
    [DeallocateOnJobCompletion]
    public NativeArray<float> _JobPhases;
    [DeallocateOnJobCompletion]
    public NativeArray<float> _JobDeltas;


    //[WriteOnly]
    public NativeArray<FilterDelayElements> _filterDelayElements;
    //[WriteOnly]
    public NativeArray<float> _audioData;
    //[WriteOnly]
    public NativeArray<int> _activeKeynum;
    public NativeArray<float> _OCSphaseIncrements;
    //[ReadOnly]
    //private NativeArray<float> _SynthGlideBaseFz;

    //public AudioJob(
    //   float sampleRate,
    //   NativeArray<SynthData> synthsdata,
    //   NativeArray<KeyData> KeyData,


    //    NativeArray<float> JobFrequencies,
    //    NativeArray<float> JobPhases,
    //    NativeArray<float> JobDeltas,
    //    NativeArray<FilterDelayElements> filterDelayElements,
    //    NativeArray<float> audioData,
    //    NativeArray<int> activeKeynum,
    //    NativeArray<float> OCSphaseIncrements
    //   //NativeArray<float> SynthGlideBaseFz
    //   )
    //{ }
    //{
    //    _JobSynths = synthsdata;
    //    _KeyData = KeyData;
    //    _sampleRate = sampleRate;
    //    _JobFrequencies = JobFrequencies;
    //    _JobPhases = JobPhases;
    //    _JobDeltas = JobDeltas;
    //    _filterDelayElements = filterDelayElements;
    //    _audioData = audioData;
    //    _activeKeynum = activeKeynum;
    //    _OCSphaseIncrements = OCSphaseIncrements;
    //    //_SynthGlideBaseFz = SynthGlideBaseFz;
    //}

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

        /// SHOULD NOT DO ALLOCATION INSIDE JOB !?
        NativeArray<ADSRlayouts> KeysADSRlayouts = new NativeArray<ADSRlayouts>(_JobFrequencies.Length, Allocator.Temp);

        /// Subdivision = times the filter coefficients recalculate within the DSPbufferSize : 8 = 1 filter update per 64sample(512/8)
        /// CONSTANTS
        int filterBlockReprocessSubdivision = (int)Mathf.Pow(2,3);
        int filterBlockReprocessSize = 512 / filterBlockReprocessSubdivision;
        /// SHOULD NOT DO ALLOCATION INSIDE JOB !?
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


        float value;
        float leftGain = 0; // Left channel gain
        float rightGain = 0; // Right channel gain
        activeKeyStartidx = 0;
        for (int i = 0; i < _activeKeynum.Length; i++)
        {
            ADSREnvelope ADSR = _JobSynths[i].ADSR;

            //Debug.Log(_JobSynths[i].Osc2Fine);

            for (int y = activeKeyStartidx; y < activeKeyStartidx + _activeKeynum[i]; y++)
            {
                int fullKeyIdx = i * 12 + (y - activeKeyStartidx);
                //int currentKeyIdx = (int)(y - activeKeyStartidx);
                _filterDelayElements[fullKeyIdx * 4] = _KeyData[fullKeyIdx].filterDelayElementsV1;
                _filterDelayElements[fullKeyIdx * 4 + 1] = _KeyData[fullKeyIdx].filterDelayElementsV2;
                _filterDelayElements[fullKeyIdx * 4 + 2] = _KeyData[fullKeyIdx].filterDelayElementsV3;
                _filterDelayElements[fullKeyIdx * 4 + 3] = _KeyData[fullKeyIdx].filterDelayElementsV4;

                /// shrink the the potential iteration of opti
                int attackFilterBlockNum = Mathf.Min(filterBlockReprocessSubdivision, KeysADSRlayouts[y].AttackSamples);
                int decayFilterBlockNum = Mathf.Min(filterBlockReprocessSubdivision, KeysADSRlayouts[y].DecaySamples);
                int sustainFilterBlockNum = Mathf.Min(filterBlockReprocessSubdivision, KeysADSRlayouts[y].SustainSamples);
                int releaseFilterBlockNum = Mathf.Min(filterBlockReprocessSubdivision, KeysADSRlayouts[y].ReleaseSamples);

                int sampleStage = 0;

                ///unisson here ?
                ///

                /// Create on start a native arrayu to store the 8 OSCphaseIncrements and assign it here for each key after detune calculation 
                /// to fix conflicting filter and unisson

                for (int v = 0; v < _JobSynths[i].UnissonVoices; v++)
                {
                    float voiceCenterOffset = v - (_JobSynths[i].UnissonVoices - 1) * 0.5f;
                    //float detuneAmount = Mathf.Pow(2f, (voiceCenterOffset * _JobSynths[i].UnissonDetune) / 1200);
                    float detune = Mathf.Pow(2f, (voiceCenterOffset * _JobSynths[i].UnissonDetune) / 1200f);
                    float glideProgress = (Mathf.Min(Mathf.Sign(_JobDeltas[y]) *_JobDeltas[y], _JobSynths[i].Portomento)+float.Epsilon) / (_JobSynths[i].Portomento + float.Epsilon);
                    /// frequency with glide taken into account
                    float effectiveFrequency = _JobFrequencies[y]* glideProgress + _KeyData[fullKeyIdx].GlideStartFrenquency * (1-glideProgress);
                    _OCSphaseIncrements[v] = (effectiveFrequency * Mathf.Pow(2f, _JobSynths[i].Osc1Fine / 1200f) * detune) / _sampleRate;
                    _OCSphaseIncrements[v+4] = (effectiveFrequency * Mathf.Pow(2f, _JobSynths[i].Osc2Fine / 1200f) * Mathf.Pow(2.0f, _JobSynths[i].Osc2Semi / 12.0f) * detune) / _sampleRate;
                }

                /// Filter ATTACK + ATTACK
                for (int z = 0; z < attackFilterBlockNum; z++)
                {
                    int attackSamples = Mathf.Min(KeysADSRlayouts[y].AttackSamples, filterBlockReprocessSize * (z + 1));
                    ///ATTACK
                    for (; sampleStage < attackSamples; sampleStage += ChannelsNum)
                    {

                        float effectiveAmplitude = (_JobDeltas[y] / ADSR.Attack) * _JobSynths[i].amplitude;
                        _JobDeltas[y] += deltaIncrement;

                        for (int v = 0; v < _JobSynths[i].UnissonVoices; v++)
                        {

                            float OSC1phase = _JobPhases[y * 4 + v];
                            float OSC2phase = _JobPhases[(_JobFrequencies.Length + y) * 4 + v];

                            value = 0f;
                            value += ((MusicUtils.Sin(OSC1phase) * _JobSynths[i].Osc1SinSawSquareFactor.x) + (MusicUtils.Saw(OSC1phase) * _JobSynths[i].Osc1SinSawSquareFactor.y) + (MusicUtils.Square(OSC1phase, _JobSynths[i].Osc1PW) * _JobSynths[i].Osc1SinSawSquareFactor.z)) * effectiveAmplitude;
                            value += ((MusicUtils.Sin(OSC2phase) * _JobSynths[i].Osc2SinSawSquareFactor.x) + (MusicUtils.Saw(OSC2phase) * _JobSynths[i].Osc2SinSawSquareFactor.y) + (MusicUtils.Square(OSC2phase, _JobSynths[i].Osc2PW) * _JobSynths[i].Osc2SinSawSquareFactor.z)) * effectiveAmplitude;
                            _JobPhases[y * 4 + v] = (_JobPhases[y * 4 + v] + _OCSphaseIncrements[v]) % 1;
                            _JobPhases[(_JobFrequencies.Length + y) * 4 + v] = (_JobPhases[(_JobFrequencies.Length + y) * 4 + v] + _OCSphaseIncrements[v+4]) % 1;

                            // Calculate pan for spread effect
                            /// Normalized Range from -1 to 1 ; 0 = no pan
                            float pan = (-(_JobSynths[i].UnissonVoices - 1) * 0.5f + v) * _JobSynths[i].UnissonSpread;


                            // Apply stereo panning based on spread
                            leftGain = (1f / _JobSynths[i].UnissonVoices) * (pan + 1); // Left channel gain
                            rightGain = (1f / _JobSynths[i].UnissonVoices) + (1f / _JobSynths[i].UnissonVoices - leftGain); // Right channel gain


                            (float filteredValue, FilterDelayElements newDelayElements) = ApplyBiquadFilter(value, filterCoefficients[(y * filterBlockReprocessSubdivision) + 0], _filterDelayElements[fullKeyIdx * 4 + v]);
                            _filterDelayElements[fullKeyIdx * 4 + v] = newDelayElements;

                            /// populate all channels with the values
                            _audioData[sampleStage] += filteredValue * leftGain;
                            _audioData[sampleStage + 1] += filteredValue * rightGain;

                        }

                    }

                }
                /// Filter DECAY + DECAY
                for (int z = 0; z < decayFilterBlockNum; z++)
                {
                    int decaySamples = Mathf.Min(KeysADSRlayouts[y].DecaySamples, filterBlockReprocessSize * (z + 1));
                    ///DECAY
                    for (; sampleStage < decaySamples; sampleStage += ChannelsNum)
                    {
                        float effectiveAmplitude = _JobSynths[i].amplitude - ((((_JobDeltas[y] - ADSR.Attack) / ADSR.Decay) * (1 - ADSR.Sustain)) * _JobSynths[i].amplitude);
                        _JobDeltas[y] += deltaIncrement;

                        for (int v = 0; v < _JobSynths[i].UnissonVoices; v++)
                        {

                            float OSC1phase = _JobPhases[y * 4 + v];
                            float OSC2phase = _JobPhases[(_JobFrequencies.Length + y) * 4 + v];

                            value = 0f;
                            value += ((MusicUtils.Sin(OSC1phase) * _JobSynths[i].Osc1SinSawSquareFactor.x) + (MusicUtils.Saw(OSC1phase) * _JobSynths[i].Osc1SinSawSquareFactor.y) + (MusicUtils.Square(OSC1phase, _JobSynths[i].Osc1PW) * _JobSynths[i].Osc1SinSawSquareFactor.z)) * effectiveAmplitude;
                            value += ((MusicUtils.Sin(OSC2phase) * _JobSynths[i].Osc2SinSawSquareFactor.x) + (MusicUtils.Saw(OSC2phase) * _JobSynths[i].Osc2SinSawSquareFactor.y) + (MusicUtils.Square(OSC2phase, _JobSynths[i].Osc2PW) * _JobSynths[i].Osc2SinSawSquareFactor.z)) * effectiveAmplitude;
                            _JobPhases[y * 4 + v] = (_JobPhases[y * 4 + v] + _OCSphaseIncrements[v]) % 1;
                            _JobPhases[(_JobFrequencies.Length + y) * 4 + v] = (_JobPhases[(_JobFrequencies.Length + y) * 4 + v] + _OCSphaseIncrements[v + 4]) % 1;

                            // Calculate pan for spread effect
                            /// Normalized Range from -1 to 1 ; 0 = no pan
                            float pan = (-(_JobSynths[i].UnissonVoices - 1) * 0.5f + v) * _JobSynths[i].UnissonSpread;

                            // Apply stereo panning based on spread
                            leftGain = (1f / _JobSynths[i].UnissonVoices) * (pan + 1); // Left channel gain
                            rightGain = (1f / _JobSynths[i].UnissonVoices) + (1f / _JobSynths[i].UnissonVoices - leftGain); // Right channel gain


                            (float filteredValue, FilterDelayElements newDelayElements) = ApplyBiquadFilter(value, filterCoefficients[(y * filterBlockReprocessSubdivision) + 0], _filterDelayElements[fullKeyIdx * 4 + v]);
                            _filterDelayElements[fullKeyIdx * 4 + v] = newDelayElements;

                            /// populate all channels with the values
                            _audioData[sampleStage] += filteredValue * leftGain;
                            _audioData[sampleStage + 1] += filteredValue * rightGain;

                        }

                    }

                }
                /// Filter SUSTAIN + SUSTAIN
                for (int z = 0; z < sustainFilterBlockNum; z++)
                {
                    int sustainSamples = Mathf.Min(KeysADSRlayouts[y].SustainSamples, filterBlockReprocessSize * (z + 1));
                    ///SUSTAIN
                    for (; sampleStage < sustainSamples; sampleStage += ChannelsNum)
                    {

                        float effectiveAmplitude = ADSR.Sustain * _JobSynths[i].amplitude;
                        //_JobDeltas[y] = ADSR.Attack + ADSR.Decay;
                        _JobDeltas[y] += deltaIncrement;

                        for (int v = 0; v < _JobSynths[i].UnissonVoices; v++)
                        {

                            float OSC1phase = _JobPhases[y * 4 + v];
                            float OSC2phase = _JobPhases[(_JobFrequencies.Length + y) * 4 + v];

                            value = 0f;
                            value += ((MusicUtils.Sin(OSC1phase) * _JobSynths[i].Osc1SinSawSquareFactor.x) + (MusicUtils.Saw(OSC1phase) * _JobSynths[i].Osc1SinSawSquareFactor.y) + (MusicUtils.Square(OSC1phase, _JobSynths[i].Osc1PW) * _JobSynths[i].Osc1SinSawSquareFactor.z)) * effectiveAmplitude;
                            value += ((MusicUtils.Sin(OSC2phase) * _JobSynths[i].Osc2SinSawSquareFactor.x) + (MusicUtils.Saw(OSC2phase) * _JobSynths[i].Osc2SinSawSquareFactor.y) + (MusicUtils.Square(OSC2phase, _JobSynths[i].Osc2PW) * _JobSynths[i].Osc2SinSawSquareFactor.z)) * effectiveAmplitude;
                            _JobPhases[y * 4 + v] = (_JobPhases[y * 4 + v] + _OCSphaseIncrements[v]) % 1;
                            _JobPhases[(_JobFrequencies.Length + y) * 4 + v] = (_JobPhases[(_JobFrequencies.Length + y) * 4 + v] + _OCSphaseIncrements[v + 4]) % 1;

                            // Calculate pan for spread effect
                            /// Normalized Range from -1 to 1 ; 0 = no pan
                            float pan = (-(_JobSynths[i].UnissonVoices - 1) * 0.5f + v) * _JobSynths[i].UnissonSpread;
                            //Debug.Log(pan);

                            // Apply stereo panning based on spread
                            leftGain = (1f / _JobSynths[i].UnissonVoices) * (pan + 1); // Left channel gain
                            rightGain = (1f / _JobSynths[i].UnissonVoices) + (1f / _JobSynths[i].UnissonVoices - leftGain); // Right channel gain


                            (float filteredValue, FilterDelayElements newDelayElements) = ApplyBiquadFilter(value, filterCoefficients[(y * filterBlockReprocessSubdivision) + 0], _filterDelayElements[fullKeyIdx * 4 + v]);
                            _filterDelayElements[fullKeyIdx * 4 + v] = newDelayElements;

                            /// populate all channels with the values
                            _audioData[sampleStage] += filteredValue * leftGain;
                            _audioData[sampleStage + 1] += filteredValue * rightGain;
                        }


                    }

                }
                /// Filter RELEASE + RELEASE
                for (int z = 0; z < releaseFilterBlockNum; z++)
                {
                    int releaseSamples = Mathf.Min(KeysADSRlayouts[y].ReleaseSamples, filterBlockReprocessSize * (z + 1));
                    ///RELEASE
                    for (; sampleStage < releaseSamples; sampleStage += ChannelsNum)
                    {

                        ///1.6 is an arbitrary Exponential factor for the amplitude to be as close as possible to 0 at the end 
                        float factor = -_JobDeltas[y] / ADSR.Release;
                        float effectiveAmplitude = Mathf.Exp(-1.6f * factor) * (1 - factor) * _KeyData[(i * 12) + (y - activeKeyStartidx)].amplitudeAtRelease;
                        /// the delta is counting is inverted during release
                        _JobDeltas[y] -= deltaIncrement;

                        for (int v = 0; v < _JobSynths[i].UnissonVoices; v++)
                        {

                            float OSC1phase = _JobPhases[y * 4 + v];
                            float OSC2phase = _JobPhases[(_JobFrequencies.Length + y) * 4 + v];

                            value = 0f;
                            value += ((MusicUtils.Sin(OSC1phase) * _JobSynths[i].Osc1SinSawSquareFactor.x) + (MusicUtils.Saw(OSC1phase) * _JobSynths[i].Osc1SinSawSquareFactor.y) + (MusicUtils.Square(OSC1phase, _JobSynths[i].Osc1PW) * _JobSynths[i].Osc1SinSawSquareFactor.z)) * effectiveAmplitude;
                            value += ((MusicUtils.Sin(OSC2phase) * _JobSynths[i].Osc2SinSawSquareFactor.x) + (MusicUtils.Saw(OSC2phase) * _JobSynths[i].Osc2SinSawSquareFactor.y) + (MusicUtils.Square(OSC2phase, _JobSynths[i].Osc2PW) * _JobSynths[i].Osc2SinSawSquareFactor.z)) * effectiveAmplitude;
                            _JobPhases[y * 4 + v] = (_JobPhases[y * 4 + v] + _OCSphaseIncrements[v]) % 1;
                            _JobPhases[(_JobFrequencies.Length + y) * 4 + v] = (_JobPhases[(_JobFrequencies.Length + y) * 4 + v] + _OCSphaseIncrements[v + 4]) % 1;

                            // Calculate pan for spread effect
                            /// Normalized Range from -1 to 1 ; 0 = no pan
                            float pan = (-(_JobSynths[i].UnissonVoices - 1) * 0.5f + v) * _JobSynths[i].UnissonSpread;

                            // Apply stereo panning based on spread
                            leftGain = (1f / _JobSynths[i].UnissonVoices) * (pan + 1); // Left channel gain
                            rightGain = (1f / _JobSynths[i].UnissonVoices) + (1f / _JobSynths[i].UnissonVoices - leftGain); // Right channel gain


                            (float filteredValue, FilterDelayElements newDelayElements) = ApplyBiquadFilter(value, filterCoefficients[(y * filterBlockReprocessSubdivision) + 0], _filterDelayElements[fullKeyIdx * 4 + v]);
                            _filterDelayElements[fullKeyIdx * 4 + v] = newDelayElements;

                            /// populate all channels with the values
                            _audioData[sampleStage] += filteredValue * leftGain;
                            _audioData[sampleStage + 1] += filteredValue * rightGain;
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
                    float glideProgress = (Mathf.Min(Mathf.Sign(_JobDeltas[cropedKeyIdx]) * _JobDeltas[cropedKeyIdx], _JobSynths[i].Portomento) + float.Epsilon) / (_JobSynths[i].Portomento + float.Epsilon);
                    float effectiveFrequency = _JobFrequencies[cropedKeyIdx] * glideProgress + _KeyData[fullKeyIdx].GlideStartFrenquency * (1 - glideProgress);
                    _KeyData[fullKeyIdx] = new KeyData { 
                        frequency = _JobFrequencies[cropedKeyIdx],
                        GlideStartFrenquency = effectiveFrequency,
                        OCS1phaseV1 = _JobPhases[cropedKeyIdx*4],
                        OCS1phaseV2 = _JobPhases[cropedKeyIdx * 4 + 1],
                        OCS1phaseV3 = _JobPhases[cropedKeyIdx * 4 + 2],
                        OCS1phaseV4 = _JobPhases[cropedKeyIdx * 4 + 3],
                        OCS2phaseV1 = _JobPhases[(_JobFrequencies.Length + cropedKeyIdx) * 4],
                        OCS2phaseV2 = _JobPhases[(_JobFrequencies.Length + cropedKeyIdx) * 4 + 1],
                        OCS2phaseV3 = _JobPhases[(_JobFrequencies.Length + cropedKeyIdx) * 4 + 2],
                        OCS2phaseV4 = _JobPhases[(_JobFrequencies.Length + cropedKeyIdx) * 4 + 3],
                        delta = _JobDeltas[cropedKeyIdx], 
                        amplitudeAtRelease = _KeyData[fullKeyIdx].amplitudeAtRelease,
                        CutoffAmountAtRelease = _KeyData[fullKeyIdx].CutoffAmountAtRelease,
                        filterDelayElementsV1 = _filterDelayElements[fullKeyIdx * 4],
                        filterDelayElementsV2 = _filterDelayElements[fullKeyIdx * 4 + 1],
                        filterDelayElementsV3 = _filterDelayElements[fullKeyIdx * 4 + 2],
                        filterDelayElementsV4 = _filterDelayElements[fullKeyIdx * 4 + 3]
                    };
                    newkeysNum++;
                }
                else
                {
                    /// Very bad OPTI ?
                    //int z = (i * 12) + y; ??
                    int z = fullKeyIdx;
                    for (; z < fullKeyIdx + (_activeKeynum[i]- y); z++)
                    {
                        _KeyData[z] = _KeyData[z+1];
                        /// need to collapse the _filterDelayElements in the same way because they map to _keyData
                        _filterDelayElements[z * 4] = _filterDelayElements[z * 4 + 4];
                        _filterDelayElements[z * 4+1] = _filterDelayElements[z * 4 + 5];
                        _filterDelayElements[z * 4+2] = _filterDelayElements[z * 4 + 6];
                        _filterDelayElements[z * 4+3] = _filterDelayElements[z * 4 + 7];
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
