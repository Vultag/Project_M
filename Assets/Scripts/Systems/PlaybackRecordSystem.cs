using System.Collections;
using System.Collections.Generic;
using MusicNamespace;
using Unity.Collections;
using Unity.Entities;
using Unity.Entities.UniversalDelegates;
using Unity.Transforms;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using static UnityEngine.EventSystems.EventTrigger;


/// <summary>
/// Currently otimized for 1 record at a time
/// -> REVIEW AND OPTIMIZE IF NEED SYMULTANEOUS
/// </summary>
[UpdateInGroup(typeof(GameSimulationSystemGroup))]
[UpdateBefore(typeof(WeaponSystem))]
public partial class PlaybackRecordSystem : SystemBase
{

    private bool AudioKeyActive;

    public Vector2 mousepos;
    private Vector2 mouseDirection;

    public static bool ClickPressed;
    public static bool ClickReleased;
    public static bool OnBeat;

    float PlaybackStartBeat;
    float PlaybackTime;
    /// <summary>
    /// mesureidx needs to be 1 behind for incrementation or infinite loop in shader
    /// </summary>
    short MesureIdx = 1;
    short NoteIdx=1;
    //short NoteIdxInMesure = -1;
    short NoteSubBeatIdx = -4;

    /// <summary>
    /// 0 = Key not pressed
    /// </summary>
    short PressedKeyIdx=0;
    /// <summary>
    /// Establish at what level the processing of Beat currently occurs
    /// Dynamic and tied of the density and disparity of the note played
    /// 4beat(ronde); 2beat(blanche) ; 1beat(noire) ; 0.5beat(croche) ; 0.25beat(dblCroche)
    /// </summary>
    float CurrentBeatProcessingLevel = 0.25f;

    /// <summary>
    ///  to process the element layout and restart mesure
    ///  0 to 4 (4/4)
    /// </summary>
    float accumulatedBeatWeight = 0;
    float accumulatedMesureWeight = 0;


    /// To know if the key passed a subbeat process stage
    bool KeyYetToBeProcessedOnce = false;
    /// To know if the previous element is a key
    bool KeyChain = false;
    /// To determine if the mesure needs expanding
    //short currentMesureSubdivision;
    /// keep track of the current note height to ease incrmentation
    float currentNoteHeight;

    float BeatProximityThreshold;

    /// not ideal ? OPTI?
    private static AudioManager audioManager;
    public static void SetAudioManager(AudioManager manager)
    {
        audioManager = manager;
    }

    protected override void OnCreate()
    {
        RequireForUpdate<PlaybackRecordingData>();        // Find AudioManager once at the start
    }

    protected override void OnStartRunning()
    {
        if (audioManager == null)
        {
            Debug.LogError("AudioManager is still null! Did SetAudioManager get called in time?");
        }

        BeatProximityThreshold = InputManager.BeatProximityThreshold;
        //float startTimeOffset = 0;
        foreach (var recordData in SystemAPI.Query<RefRO<PlaybackRecordingData>>())
        {
            PlaybackStartBeat = recordData.ValueRO.startBeat;
        }
        //NoteIdxInMesure = -1;
        PressedKeyIdx = 0;

        MusicSheetData ActiveMusicSheet = AudioLayoutStorageHolder.audioLayoutStorage.ActiveMusicSheet;
        accumulatedMesureWeight = 0;
        CurrentBeatProcessingLevel = 0f;
        accumulatedBeatWeight = 0f;
        NoteIdx = 1;
        NoteSubBeatIdx = 0;

        /// Expand the new mesure
        ActiveMusicSheet.ElementsInMesure[0 + 1] = 4;

    }


    protected override void OnUpdate()
    {

        /// OPTI -> Called this multiple times per frame
        mousepos = Camera.main.ScreenToWorldPoint(InputManager.mousePos);


        EntityCommandBuffer ecb = World.GetOrCreateSystemManaged<EndSimulationEntityCommandBufferSystem>().CreateCommandBuffer();

        MusicSheetData ActiveMusicSheet = AudioLayoutStorageHolder.audioLayoutStorage.ActiveMusicSheet;

        PlaybackTime = (float)MusicUtils.time - PlaybackStartBeat;
        //Debug.Log(PlaybackTime);
        float SclicedBeatTime = PlaybackTime * ((MusicUtils.BPM * 4)/60f);
        float normalizedProximity = (PlaybackTime % (60f / (MusicUtils.BPM * 4))) / (60f / (MusicUtils.BPM * 4));
        float SclicedBeatProximity = 1 - Mathf.Abs((normalizedProximity - 0.5f) * 2);

        short newNoteidxOffseted = (short)(Mathf.FloorToInt(Mathf.Abs(SclicedBeatTime-BeatProximityThreshold) * 0.25f));

        short newNoteSubBeatIdxOffseted = (short)(newNoteidxOffseted * 4 + Mathf.FloorToInt(Mathf.Abs(SclicedBeatTime-BeatProximityThreshold) % 4));

   
        foreach (var (Wtrans, recordData, entity) in SystemAPI.Query<RefRO<LocalToWorld>,RefRW<PlaybackRecordingData>>().WithEntityAccess())
        {

            /// if mouse click
            /// prepair a playbackkey with the lenght missing
            /// if mouse release
            /// add the prepaired playbackkey to the list, set duration to time-playbackkey.time
            /// 
            //Vector2 direction = mousepos - new Vector2(Wtrans.ValueRO.Position.x, Wtrans.ValueRO.Position.y);

            if (!UIInput.MouseOverUI)
            {
                if (recordData.ValueRO.activeLegatoFz == 0)
                {
                    if (!AudioKeyActive)
                        mouseDirection = mousepos - new Vector2(Wtrans.ValueRO.Position.x, Wtrans.ValueRO.Position.y);
                }
                else
                {
                    Vector2 newMouseDir = mousepos - new Vector2(Wtrans.ValueRO.Position.x, Wtrans.ValueRO.Position.y);
                    float currentFz = MusicUtils.DirectionToFrequency(newMouseDir);
                    if (currentFz != recordData.ValueRO.activeLegatoFz)
                    {
                        if (SclicedBeatProximity < InputManager.BeatProximityThreshold)
                        { mouseDirection = newMouseDir; }
                    }
                    else
                        mouseDirection = newMouseDir;
                }
            }






            SynthData ActiveSynth = AudioLayoutStorageHolder.audioLayoutStorage.SynthsData[recordData.ValueRO.synthIndex];

            var accumulator = SystemAPI.GetBuffer<PlaybackRecordingKeysBuffer>(entity);
        
            if (SclicedBeatProximity < InputManager.BeatProximityThreshold && InputManager.CanPressKey)
            {
           

                if (recordData.ValueRO.activeLegatoFz != 0 && !ClickReleased)
                {

                    //Vector2 currentDir = PhysicsUtilities.Rotatelerp(accumulator[accumulator.Length - 1].StartDirLenght, accumulator[accumulator.Length - 1].DirLenght, -Mathf.Log(ActiveSynth.Portomento / 3) * 0.01f + 0.03f);
                    float currentFz = MusicUtils.DirectionToFrequency(mouseDirection);
                    if (currentFz != recordData.ValueRO.activeLegatoFz)
                    {
                        //if (OnBeat)
                        {
                            /// OPTI "accumulator.Length - 1" ?
                            accumulator[accumulator.Length - 1] = new PlaybackRecordingKeysBuffer
                            {
                                playbackRecordingKey = new PlaybackKey
                                {
                                    dir = MusicUtils.CenterDirection(accumulator[accumulator.Length - 1].playbackRecordingKey.dir) * mouseDirection.magnitude,
                                    //dir = PhysicsUtilities.RadianToDirection(PhysicsUtilities.DirectionToRadians(accumulator[accumulator.Length - 1].playbackRecordingKey.dir)) * direction.magnitude,
                                    startDir = accumulator[accumulator.Length - 1].playbackRecordingKey.startDir,
                                    time = accumulator[accumulator.Length - 1].playbackRecordingKey.time,
                                    lenght = PlaybackTime - accumulator[accumulator.Length - 1].playbackRecordingKey.time,
                                    keyCutIdx = accumulator[accumulator.Length - 1].playbackRecordingKey.keyCutIdx,
                                    dragged = true,
                                }
                            };

                            /// REMOVED AS RAYS DONT CURRENTLY LEAVE RELEASEKEY IN LEGATO
                            /*
                            short RkeyIdxOnPlayback = short.MaxValue;
                            /// Check if the the legato glide over a released key
                            for (int i = 0; i < accumulator.Length-1; i++)
                            {
                                if (MusicUtils.DirectionToFrequency(accumulator[i].playbackRecordingKey.dir) == currentFz)
                                {
                                    /// check every active releasing key in the accumulator to find the keycutindex
                                    RkeyIdxOnPlayback = 0;
                                    for (int y = 0; y < i; y++)
                                    {
                                        if ((accumulator[i].playbackRecordingKey.time+accumulator[i].playbackRecordingKey.lenght)> recordData.ValueRO.time)
                                            RkeyIdxOnPlayback++;
                                    }
                                    //Debug.Log(RkeyIdxOnPlayback);
                                    accumulator[i] = new PlaybackRecordingKeysBuffer
                                    {
                                        playbackRecordingKey = new PlaybackKey
                                        {
                                            dir = accumulator[i].playbackRecordingKey.dir,
                                            startDir = accumulator[i].playbackRecordingKey.startDir,
                                            time = accumulator[i].playbackRecordingKey.time,
                                            lenght = accumulator[i].playbackRecordingKey.lenght,
                                            keyCutIdx = accumulator[i].playbackRecordingKey.keyCutIdx,
                                            dragged = true,
                                        }
                                    };
                                    break;
                                }
                            }
                            */
                            recordData.ValueRW.activeLegatoFz = currentFz;

                            accumulator.Add(new PlaybackRecordingKeysBuffer
                            {
                                playbackRecordingKey = new PlaybackKey
                                {
                                    dir = MusicUtils.CenterDirection(mouseDirection) * mouseDirection.magnitude,
                                    //dir = PhysicsUtilities.RadianToDirection(PhysicsUtilities.DirectionToRadians(direction)) * direction.magnitude,
                                    //startDir = accumulator.Length==0? direction: recordData.ValueRO.GideReferenceDirection,
                                    startDir = recordData.ValueRO.GideReferenceDirection,
                                    time = PlaybackTime,
                                    /// REMOVED AS RAYS DONT CURRENTLY LEAVE RELEASEKEY IN LEGATO
                                    //keyCutIdx = RkeyIdxOnPlayback,
                                    dragged = true
                                }
                            });
                            recordData.ValueRW.GideReferenceDirection = mouseDirection;
                            //Debug.Log("df");
                        }

                        /// SHEET

                        currentNoteHeight = MusicUtils.NoteIndexToNote(MusicUtils.radiansToNoteIndex(Mathf.Abs(PhysicsUtilities.DirectionToRadians(accumulator[accumulator.Length - 1].playbackRecordingKey.dir))));
                      
                        if (CurrentBeatProcessingLevel !=0)
                        {
                            NoteIdx++;
                            ActiveMusicSheet.ElementsInMesure[MesureIdx]++;
                        }
                        {
                            currentNoteHeight = MusicUtils.NoteIndexToNote(MusicUtils.radiansToNoteIndex(Mathf.Abs(PhysicsUtilities.DirectionToRadians(mouseDirection))));
                            SetNoteHeight(ref ActiveMusicSheet, currentNoteHeight);
                            /// note is linked
                            ActiveMusicSheet.NotesSpriteIdx[NoteIdx] = -10;
                            CurrentBeatProcessingLevel = 0f;
                        }

                        KeyChain = true;
                        PressedKeyIdx = (short)(NoteIdx);
                        KeyYetToBeProcessedOnce = true;

                    }
                }
            }

            if (ClickPressed && !UIInput.MouseOverUI)
            {
                //float randian = Mathf.Abs(PhysicsUtilities.DirectionToRadians(direction));
                //int note = MusicUtils.radiansToNote(randian);

                accumulator.Add(new PlaybackRecordingKeysBuffer
                {
                    playbackRecordingKey = new PlaybackKey
                    {
                        dir = mouseDirection,
                        //startDir = accumulator.Length==0? direction: recordData.ValueRO.GideReferenceDirection,
                        startDir = recordData.ValueRO.GideReferenceDirection,
                        time = PlaybackTime,
                        /// REMOVED AS RAYS DONT CURRENTLY LEAVE RELEASEKEY IN LEGATO
                        //keyCutIdx = short.MaxValue
                    }
                });
                recordData.ValueRW.GideReferenceDirection = mouseDirection;
                recordData.ValueRW.activeLegatoFz = ActiveSynth.Legato ? MusicUtils.DirectionToFrequency(mouseDirection) : 0;
                AudioKeyActive = true;
                ClickPressed = false;

                /// SHEET

                if (accumulatedBeatWeight > 0 && !KeyChain)
                {
                    NoteIdx++;
                    ActiveMusicSheet.ElementsInMesure[MesureIdx]++;
                }
                {
                    currentNoteHeight = MusicUtils.NoteIndexToNote(MusicUtils.radiansToNoteIndex(Mathf.Abs(PhysicsUtilities.DirectionToRadians(mouseDirection))));
                    SetNoteHeight(ref ActiveMusicSheet, currentNoteHeight);
                    ActiveMusicSheet.NotesSpriteIdx[NoteIdx] = 10;
                    CurrentBeatProcessingLevel = 0f;
                }

                KeyChain = true;
                PressedKeyIdx = (short)(NoteIdx);
                KeyYetToBeProcessedOnce = true;

            }

            if (UIInput.MouseOverUI)
            {
                if(AudioKeyActive)
                {
                    accumulator[accumulator.Length - 1] = new PlaybackRecordingKeysBuffer{ playbackRecordingKey = new PlaybackKey
                    {
                        dir = accumulator[accumulator.Length - 1].playbackRecordingKey.dir,
                        startDir = accumulator[accumulator.Length - 1].playbackRecordingKey.startDir,
                        time = accumulator[accumulator.Length - 1].playbackRecordingKey.time,
                        lenght = PlaybackTime - accumulator[accumulator.Length - 1].playbackRecordingKey.time,
                        keyCutIdx = accumulator[accumulator.Length - 1].playbackRecordingKey.keyCutIdx,
                        dragged = accumulator[accumulator.Length - 1].playbackRecordingKey.dragged,
                    }
                    };
                    AudioKeyActive = false;
                }

            }
            else
            {

                if (ClickReleased)
                {
                    if(AudioKeyActive)
                    {
                        accumulator[accumulator.Length - 1] = new PlaybackRecordingKeysBuffer
                        {
                            playbackRecordingKey = new PlaybackKey
                            {
                                dir = accumulator[accumulator.Length - 1].playbackRecordingKey.dir,
                                startDir = accumulator[accumulator.Length - 1].playbackRecordingKey.startDir,
                                time = accumulator[accumulator.Length - 1].playbackRecordingKey.time,
                                lenght = PlaybackTime - accumulator[accumulator.Length - 1].playbackRecordingKey.time,
                                /// REMOVED AS RAYS DONT CURRENTLY LEAVE RELEASEKEY IN LEGATO
                                //keyCutIdx = accumulator[accumulator.Length - 1].playbackRecordingKey.keyCutIdx,
                            }
                        };
                        recordData.ValueRW.activeLegatoFz = 0;
                        AudioKeyActive = false;
                        ClickReleased = false;

                        /// Sheet

                        //Debug.Log(CurrentBeatProcessingLevel);

                        if (!KeyYetToBeProcessedOnce && CurrentBeatProcessingLevel != 0f)
                        {
                            NoteIdx++;
                        }

                        CurrentBeatProcessingLevel = 0;
                        PressedKeyIdx = 0;

                    }
                }
            }


            if (newNoteSubBeatIdxOffseted > NoteSubBeatIdx)
            {
                
                if (PressedKeyIdx == 0)
                {
                    if(KeyYetToBeProcessedOnce)
                    {
                        accumulatedBeatWeight += 0.25f;
                        KeyYetToBeProcessedOnce = false;
                        if (accumulatedBeatWeight>=1)
                        {
                            accumulatedBeatWeight = 0f;
                            accumulatedMesureWeight += 1;
                        }
                        NoteIdx++;
                    }
                    else
                        IncrementSilence(ref ActiveMusicSheet);

                }
                /// Expand the note with increment
                else
                {
                    //Debug.Log("count");

                    KeyYetToBeProcessedOnce = false;
                    IncrementNote(ref ActiveMusicSheet);

                }
                


      

                //Debug.Log(newNoteIdx);
                NoteSubBeatIdx = newNoteSubBeatIdxOffseted;

                /// Note ideal. rework mesure expention to be more ergonomic ?
                ActiveMusicSheet.ElementsInMesure[MesureIdx] = ActiveMusicSheet.ElementsInMesure[MesureIdx] < (NoteIdx) ?
                               ActiveMusicSheet.ElementsInMesure[MesureIdx] + 1 : ActiveMusicSheet.ElementsInMesure[MesureIdx];

                if (accumulatedMesureWeight >= 4)
                {
                    ActiveMusicSheet.ElementsInMesure[MesureIdx] = ActiveMusicSheet.ElementsInMesure[MesureIdx] > (NoteIdx) ?
                                       ActiveMusicSheet.ElementsInMesure[MesureIdx] -1 : ActiveMusicSheet.ElementsInMesure[MesureIdx];
                    ///TEMP TEST 
                    //Debug.Break();

                    /// ONLY NEED TO COUNT NEWSUBBEATINDX ?
                    //MesureIdx = newMesureIdx;

                    //ActiveMusicSheet.mesureNumber++;
                    //NoteIdxInMesure = -1;
                    ///// Expand the new mesure
                    //ActiveMusicSheet.ElementsInMesure[MesureIdx + 1] = 4;
                    //ActiveMusicSheet.NoteElements[NoteIdx] = 1;
                    //ActiveMusicSheet.NoteElements[NoteIdx + 1] = 1;
                    //ActiveMusicSheet.NoteElements[NoteIdx + 2] = 1;
                    //ActiveMusicSheet.NoteElements[NoteIdx + 3] = 1;
                    //accumulatedMesureWeight = 0;

                    /// IF MESURE LENGHT REATCHED 
                    {
                        if (AudioKeyActive)
                        {
                            accumulator[accumulator.Length - 1] = new PlaybackRecordingKeysBuffer
                            {
                                playbackRecordingKey = new PlaybackKey
                                {
                                    dir = accumulator[accumulator.Length - 1].playbackRecordingKey.dir,
                                    startDir = accumulator[accumulator.Length - 1].playbackRecordingKey.startDir,
                                    time = accumulator[accumulator.Length - 1].playbackRecordingKey.time,
                                    lenght = recordData.ValueRO.duration - accumulator[accumulator.Length - 1].playbackRecordingKey.time
                                }
                            };
                            AudioKeyActive = false;
                            ClickReleased = false;
                        }
                        //Debug.Log(accumulator.Length);

                        var playbackKeys = new NativeArray<PlaybackKey>(accumulator.Length, Allocator.Persistent);
                        playbackKeys.CopyFrom(accumulator.AsNativeArray().Reinterpret<PlaybackKey>());
                        PlaybackAudioBundle newPlaybackAudioBundle = new PlaybackAudioBundle
                        {
                            IsLooping = true,
                            //IsPlaying = false,
                            PlaybackDuration = recordData.ValueRO.duration,
                            PlaybackKeys = playbackKeys
                        };
                        AudioLayoutStorageHolder.audioLayoutStorage.WritePlayback(newPlaybackAudioBundle, recordData.ValueRO.synthIndex);

                        /// carefull about disposing PlaybackAudioBundle and musicSheet -> used inside holder
                        audioManager.uiPlaybacksHolder._AddSynthPlaybackContainer(newPlaybackAudioBundle,ActiveMusicSheet,(short)recordData.ValueRO.synthIndex);

                        ecb.RemoveComponent<PlaybackRecordingKeysBuffer>(entity);
                        ecb.RemoveComponent<PlaybackRecordingData>(entity);

                        //NoteIdx = 0;
                        //ActiveMusicSheet.ElementsInMesure[MesureIdx]--;
                    }
                }

            }


        }

    }



    /// <summary>
    /// MOVE TO SEPERATE FILE
    /// </summary>

    private void UpdateSilence(ref MusicSheetData activeMusicSheet)
    {
    
        short SilenceSubBeatLenght = (short)(CurrentBeatProcessingLevel * 4);
        //Debug.Log(SilenceSubBeatLenght);
        if (SilenceSubBeatLenght == 3)
        {
            activeMusicSheet.NotesSpriteIdx[NoteIdx] = 1.5f;
        }
        else
        {
            /// Expand the silence of the element in the absence of note played
            /// 0+0 = dblSoupir ; 0+1 = Soupir ; 0+3 = silence
            int spriteIDX = 0 + (SilenceSubBeatLenght - 1);
            activeMusicSheet.NotesSpriteIdx[NoteIdx] = spriteIDX;
        }

        activeMusicSheet.NotesHeight[NoteIdx] = 6;
    
    }
    private void UpdateNote(ref MusicSheetData activeMusicSheet, float noteHeight)
    {
        
        //Debug.Break();

        short KeySubBeatLenght = (short)(CurrentBeatProcessingLevel * 4);
        if (KeySubBeatLenght == 3)
        {
            // Croche dotted
            activeMusicSheet.NotesSpriteIdx[PressedKeyIdx] = 11.5f * Mathf.Sign(activeMusicSheet.NotesSpriteIdx[NoteIdx]);//sign to preserve liaison
        }
        else
        {
            /// Expand the note as it is being maintained
            /// 10+0 = dblCroche ; 10+1 = Croche ; 10+3 = noire
            int spriteIDX = 10 + Mathf.Min(3, KeySubBeatLenght - 1);
            activeMusicSheet.NotesSpriteIdx[PressedKeyIdx] = spriteIDX * Mathf.Sign(activeMusicSheet.NotesSpriteIdx[NoteIdx]);//sign to preserve liaison;

        }

        SetNoteHeight(ref activeMusicSheet,noteHeight);
    }

    private void IncrementNote(ref MusicSheetData activeMusicSheet)
    {

        CurrentBeatProcessingLevel += .25f;
        accumulatedBeatWeight += .25f;
        UpdateNote(ref activeMusicSheet, currentNoteHeight);

        if (CurrentBeatProcessingLevel >= 1)
        {

            CurrentBeatProcessingLevel = 0f;
            NoteIdx++;
            activeMusicSheet.NotesSpriteIdx[NoteIdx] = -10;
            SetNoteHeight(ref activeMusicSheet, currentNoteHeight);
            PressedKeyIdx = NoteIdx;
        }
        if (accumulatedBeatWeight >= 1)
        {
            accumulatedBeatWeight = 0f;
            accumulatedMesureWeight += 1;
        }

    }

    private void IncrementSilence(ref MusicSheetData activeMusicSheet)
    {

        //Debug.Log("incr");
        {
            KeyChain = false;
            CurrentBeatProcessingLevel += .25f;
            accumulatedBeatWeight += .25f;
            UpdateSilence(ref activeMusicSheet);
        }
        if (accumulatedBeatWeight >= 1)
        {

            /// restart from a dblSoupir
            NoteIdx += 1;
            CurrentBeatProcessingLevel = 0f;
            accumulatedBeatWeight = 0f;
            accumulatedMesureWeight += 1;
            //Debug.Log("aa");
        }

    }

    private void SetNoteHeight(ref MusicSheetData activeMusicSheet, float height)
    {
        /// Calculate wether the key is considered sharp, flat or natural
        /// height the input is either a integer of ?.5
        /// integer = no alteration , ?.25 = sharp ?(+1).75 = flat 
        /// natural -> not implemented yet
        /// CURENTRLY SET TO ONLY SHARP -> TO DO
        activeMusicSheet.NotesHeight[NoteIdx] = height- (height-Mathf.Floor(height))*0.5f;
    }


}
