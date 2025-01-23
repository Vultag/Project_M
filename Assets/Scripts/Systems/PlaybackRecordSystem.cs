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

    //public NativeList<PlaybackKey> KeyDatasAccumulator;
    private bool AudioKeyActive;
    //public NativeList<float> ActiveFrequencies;

    //public AudioGenerator audioGenerator;

    public Vector2 mousepos;
    private Vector2 mouseDirection;

    public static bool ClickPressed;
    public static bool ClickReleased;
    public static bool OnBeat;

    float PlaybackStartBeat;
    float PlaybackTime;
    short MesureIdx = -1;
    short NoteIdx=1;
    short NoteIdxInMesure = -1;
    short NoteSubBeatIdx = -4;
    /// Up to 16 per mesure (4/4)
    /// 1 for starting Key
    short ActivatedNoteElements = 1;

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
    /// SubBeat excess in case of a key press -> require silences addition before adding the key to match the tempo
    /// </summary>

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
    short currentMesureSubdivision;


    protected override void OnCreate()
    {
        //audioGenerator = AudioManager.AudioGenerator;
        //KeyDatasAccumulator = new NativeList<PlaybackKey>();
        RequireForUpdate<PlaybackRecordingData>();
    }

    protected override void OnStartRunning()
    {
        //float startTimeOffset = 0;
        foreach (var recordData in SystemAPI.Query<RefRO<PlaybackRecordingData>>())
        {
            PlaybackStartBeat = recordData.ValueRO.startBeat;
        }
        //PlaybackStartBeat = (float)MusicUtils.time - startTimeOffset;
        NoteIdxInMesure = -1;
        PressedKeyIdx = 0;
        ActivatedNoteElements=1;

        MusicSheetData ActiveMusicSheet = AudioLayoutStorageHolder.audioLayoutStorage.ActiveMusicSheet;
        accumulatedMesureWeight = 0;
        CurrentBeatProcessingLevel = 0f;
        accumulatedBeatWeight = 0f;
        NoteIdx = 1;
        NoteSubBeatIdx = 0;
        currentMesureSubdivision = 4;

        /// Expand the new mesure
        ActiveMusicSheet.ElementsInMesure[0 + 1] = 4;
        ActiveMusicSheet.NoteElements[0] = 1;
        ActiveMusicSheet.NoteElements[0 + 1] = 1;
        ActiveMusicSheet.NoteElements[0 + 2] = 1;
        ActiveMusicSheet.NoteElements[0 + 3] = 1;

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

        short newNoteidx = (short)(Mathf.FloorToInt(SclicedBeatTime * 0.25f));

        short newNoteSubBeatIdx = (short)(newNoteidx * 4 + Mathf.FloorToInt(SclicedBeatTime % 4));

        short SnapToStartOrEnd = (short)Mathf.RoundToInt(SclicedBeatTime % 1); /// Integer division


        //foreach (var (recordData,keyBuffer) in SystemAPI.Query<RefRW<PlaybackRecordingData>, DynamicBuffer <SustainedKeyBufferData>> ())
        foreach (var (Wtrans, recordData, entity) in SystemAPI.Query<RefRO<LocalToWorld>,RefRW<PlaybackRecordingData>>().WithEntityAccess())
        {

            /// if mouse click
            /// prepair a playbackkey with the lenght missing
            /// if mouse release
            /// add the prepaired playbackkey to the list, set duration to time-playbackkey.time
            /// 
            //Vector2 direction = mousepos - new Vector2(Wtrans.ValueRO.Position.x, Wtrans.ValueRO.Position.y);

            if (!UIInputSystem.MouseOverUI)
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

                        if (SnapToStartOrEnd == 0)
                        {

                            short offset = accumulatedBeatWeight == 0 ? (short)1 : (short)0;
                            /// decrement previous note
                            if (newNoteSubBeatIdx==NoteSubBeatIdx)
                            {                 
                                NoteIdx -= offset;
                                /// Deduct the BeatProcessingLevel of the previous note
                                CurrentBeatProcessingLevel = Mathf.Ceil(ActiveMusicSheet.NotesSpriteIdx[NoteIdx] - 10) * 0.25f + 0.25f;
                                CurrentBeatProcessingLevel -= 0.25f;
                                UpdateNote(ref ActiveMusicSheet);

                                NoteIdx++;
                                ActiveMusicSheet.ElementsInMesure[MesureIdx + 1]++;
                            }
                            else
                            {
                                //Debug.LogError("BEFORE BEAT");
                                NoteIdx += (short)-(offset-1);
                                ActiveMusicSheet.ElementsInMesure[MesureIdx + 1] += (short)-(offset - 1);
                            }
                 

                            {
                                ActiveMusicSheet.NotesHeight[NoteIdx] = 6;
                                ActiveMusicSheet.NotesSpriteIdx[NoteIdx] = 10;
                                ActiveMusicSheet.NoteElements[NoteIdx] = 0.25f;
                                CurrentBeatProcessingLevel = 0f;
                            }
                            //Debug.LogError("legato A" + accumulatedBeatWeight + ":" + (float)MusicUtils.time + ":     " + PlaybackTime);

                        }
                        else
                        {
                            if (accumulatedBeatWeight != 0)
                            {
                                NoteIdx++;
                                ActiveMusicSheet.ElementsInMesure[MesureIdx + 1]++;
                            }
                            {
                                ActiveMusicSheet.NotesHeight[NoteIdx] = 6;
                                ActiveMusicSheet.NotesSpriteIdx[NoteIdx] = 10;
                                ActiveMusicSheet.NoteElements[NoteIdx] = 0.25f;
                                CurrentBeatProcessingLevel = 0f;
                            }
                            //Debug.LogError("legato B" + accumulatedBeatWeight+":" + (float)MusicUtils.time+":     "+ PlaybackTime);

                        }
                        PressedKeyIdx = (short)(NoteIdx);
                        KeyYetToBeProcessedOnce = true;
                        KeyChain = true;

                    }
                }
            }

            if (ClickPressed && !UIInputSystem.MouseOverUI)
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

                /* 
                 * subbeats :                       1  2  3  4  1
                 *                                  0  |  |  |  0
                 * PlacementSubdivisionLVL :        0  2  1  2  0
                 */
                //short keyPlacementSubdivisionLVL = 

                /// OPTI
                if (SnapToStartOrEnd == 0)
                {


                    /// VERIF ICI -> PAS SENSE ETRE UNE NOTE ?? (spriteIDX>=10)
                    if (ActiveMusicSheet.NotesSpriteIdx[NoteIdx]>= 10)
                    {
                        Debug.LogError("NOT SUPPOSED TO HAPPEN?");
                        //Debug.Break();
                    }
                    /*
                    if (accumulatedBeatWeight == 0.25f)
                    {
                        if (ActiveMusicSheet.NotesSpriteIdx[NoteIdx] == 10)
                            Debug.LogError("NOT SUPPOSED TO HAPPEN");

                        {

                            //ActiveMusicSheet.ElementsInMesure[MesureIdx + 1]++;
                            ActiveMusicSheet.NotesHeight[NoteIdx] = 6;
                            ActiveMusicSheet.NotesSpriteIdx[NoteIdx] = 10;
                            ActiveMusicSheet.NoteElements[NoteIdx] = 0.25f;
                            //CurrentBeatProcessingLevel = 0.25f;
                            //accumulatedBeatWeight += .25f;
                            //Debug.Log("test");
                            //PressedKeyIdx = (short)(NoteIdx);
                        }
                    }
                    if (accumulatedBeatWeight == 0)
                    {
                        /// dont work

                        //KeyYetToBeProcessedOnce = true;
                        NoteIdx--;
                        //ActiveMusicSheet.ElementsInMesure[MesureIdx + 1]--;

                        if (ActiveMusicSheet.NotesSpriteIdx[NoteIdx] >= 10)
                            Debug.LogError("NOT SUPPOSED TO HAPPEN ??");
                        Debug.LogError("HAPPEN :" + CurrentBeatProcessingLevel + ":" + ActiveMusicSheet.NotesSpriteIdx[NoteIdx]);
                        //PressedKeyIdx = (short)(NoteIdx);
                        //initialIndex = 0;
                        /// recalculate if chain
                        //isChain = CurrentBeatProcessingLevel == 0.25f;

                        /// Deduct the BeatProcessingLevel at the previous note before it was discarded at the beat increment
                        CurrentBeatProcessingLevel = Mathf.Ceil(ActiveMusicSheet.NotesSpriteIdx[NoteIdx]) * 0.25f + 0.25f;
                        Debug.LogError(" :" + CurrentBeatProcessingLevel);

                        if (CurrentBeatProcessingLevel==0.25f)
                        {
                            
                            ActiveMusicSheet.NotesHeight[NoteIdx] = 6;
                            ActiveMusicSheet.NotesSpriteIdx[NoteIdx] = 10;
                            ActiveMusicSheet.NoteElements[NoteIdx] = 0.25f;
                            NoteIdx++;
                            // ??
                            //PressedKeyIdx = NoteIdx;
                        }
                        else
                        {
                            short KeySubBeatLenght = (short)((CurrentBeatProcessingLevel - 0.25f) * 4);
                            if (KeySubBeatLenght == 3)
                            {
                                ActiveMusicSheet.NotesSpriteIdx[NoteIdx] = 1.5f;
                                ActiveMusicSheet.NoteElements[NoteIdx] = 0.75f;
                            }
                            else
                            {
                                int spriteIDX = Mathf.Min(3, KeySubBeatLenght - 1);
                                ActiveMusicSheet.NotesSpriteIdx[NoteIdx] = spriteIDX;
                                ActiveMusicSheet.NoteElements[NoteIdx] = spriteIDX*0.25f + 0.25f;
                            }

                            NoteIdx++;
                            ActiveMusicSheet.ElementsInMesure[MesureIdx + 1]++;
                            ActiveMusicSheet.NotesHeight[NoteIdx] = 6;
                            ActiveMusicSheet.NotesSpriteIdx[NoteIdx] = 10;
                            ActiveMusicSheet.NoteElements[NoteIdx] = 0.25f;
                            NoteIdx++;

                        }
                        CurrentBeatProcessingLevel = 0;


                    }
                    if (accumulatedBeatWeight > 0.25f)
                    {
            

                        // FIX HERE ? for 0
                        if (CurrentBeatProcessingLevel>0.25f)
                        {
                            short KeySubBeatLenght = (short)((CurrentBeatProcessingLevel - 0.25f) * 4);
                            if (KeySubBeatLenght == 3)
                            {
                                ActiveMusicSheet.NotesSpriteIdx[NoteIdx] = 1.5f;
                                ActiveMusicSheet.NoteElements[NoteIdx] = 0.75f;
                            }
                            else
                            {
                                int spriteIDX = Mathf.Min(3, KeySubBeatLenght - 1);
                                ActiveMusicSheet.NotesSpriteIdx[NoteIdx] = Mathf.Min(3, KeySubBeatLenght - 1);
                                ActiveMusicSheet.NoteElements[NoteIdx] = spriteIDX * 0.25f + 0.25f;
                            }

                            NoteIdx++;
                            ActiveMusicSheet.ElementsInMesure[MesureIdx + 1]++;
                            Debug.Log("more than dblsoupir : " + CurrentBeatProcessingLevel);

                        }
                        else
                        {
                            Debug.Log("dblsoupir");
                        }


                        {

                            //ActiveMusicSheet.ElementsInMesure[MesureIdx + 1]++;
                            ActiveMusicSheet.NotesHeight[NoteIdx] = 6;
                            ActiveMusicSheet.NotesSpriteIdx[NoteIdx] = 10;
                            ActiveMusicSheet.NoteElements[NoteIdx] = 0.25f;

                        }   

                    }
                    */


                    if (accumulatedBeatWeight > 0 && !KeyChain)
                    {
                        NoteIdx++;
                        ActiveMusicSheet.ElementsInMesure[MesureIdx + 1]++;
                    }

                    {
                        ActiveMusicSheet.NotesHeight[NoteIdx] = 6;
                        ActiveMusicSheet.NotesSpriteIdx[NoteIdx] = 10;
                        ActiveMusicSheet.NoteElements[NoteIdx] = 0.25f;
                        CurrentBeatProcessingLevel = 0f;
                    }

                    //Debug.LogError(" overrite to former element : "+ accumulatedBeatWeight);
                    //Debug.Log(" CurrentBeatProcessingLevel : " + CurrentBeatProcessingLevel);
                    //Debug.Break();

                }
                else
                {

                    if(accumulatedBeatWeight!=0)
                    {
                        if (!KeyChain)
                        {
                            UpdateSilence(ref ActiveMusicSheet);
                            NoteIdx++;
                            ActiveMusicSheet.ElementsInMesure[MesureIdx + 1]++;
                        }
                        else
                        {
                            //Debug.Log("chain");
                        }
                    }
              

                    {

                        ActiveMusicSheet.NotesHeight[NoteIdx] = 6;
                        ActiveMusicSheet.NotesSpriteIdx[NoteIdx] = 10;
                        ActiveMusicSheet.NoteElements[NoteIdx] = 0.25f;
                        CurrentBeatProcessingLevel = 0f;
                    }



                }

                KeyChain = true;
                PressedKeyIdx = (short)(NoteIdx);
                //Debug.Log("KeyToProcessOnce");
                KeyYetToBeProcessedOnce = true;

            }

            if (UIInputSystem.MouseOverUI)
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
                        
                        /// Key is being released on subBeat. do this frame's subeat proccessing here
                        if (newNoteSubBeatIdx > NoteSubBeatIdx)
                        {
                            IncrementNote(ref ActiveMusicSheet);

                            NoteSubBeatIdx = newNoteSubBeatIdx;

                            //Debug.LogWarning("test");
                        }
                    
                        if(!KeyYetToBeProcessedOnce && accumulatedBeatWeight != 0f)
                        {
                            ActiveMusicSheet.ElementsInMesure[MesureIdx + 1]++;
                            NoteIdx++;
                        }
                    
                        CurrentBeatProcessingLevel = 0;
                        PressedKeyIdx = 0;

                    }
                }
            }


            if (newNoteSubBeatIdx > NoteSubBeatIdx)
            {
                if (PressedKeyIdx == 0)
                {

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
                NoteSubBeatIdx = newNoteSubBeatIdx;

                if (accumulatedMesureWeight >= 4)
                {
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
                        AudioLayoutStorageHolder.audioLayoutStorage.WritePlayback(new PlaybackAudioBundle
                        {
                            IsLooping = true,
                            //IsPlaying = false,
                            PlaybackDuration = recordData.ValueRO.duration,
                            PlaybackKeys = playbackKeys
                        }, recordData.ValueRO.synthIndex);
                        //Debug.Log(AudioManager.audioGenerator.audioLayoutStorage.NewPlaybackAudioBundles.PlaybackKeys.Length);
                        ecb.RemoveComponent<PlaybackRecordingKeysBuffer>(entity);
                        ecb.RemoveComponent<PlaybackRecordingData>(entity);

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
    
        //Debug.Log("incr");
        short SilenceSubBeatLenght = (short)(CurrentBeatProcessingLevel * 4);
        //Debug.Log(SilenceSubBeatLenght);
        if (SilenceSubBeatLenght == 3)
        {
            activeMusicSheet.NotesSpriteIdx[NoteIdx] = 1.5f;
            activeMusicSheet.NoteElements[NoteIdx] = 0.75f;
        }
        else
        {
            /// Expand the silence of the element in the absence of note played
            /// 0+0 = dblSoupir ; 0+1 = Soupir ; 0+3 = silence
            int spriteIDX = 0 + (SilenceSubBeatLenght - 1);
            activeMusicSheet.NotesSpriteIdx[NoteIdx] = spriteIDX;
            activeMusicSheet.NoteElements[NoteIdx] = spriteIDX * 0.25f + 0.25f;
        }

        activeMusicSheet.NotesHeight[NoteIdx] = 6;
    
    }
    private void UpdateNote(ref MusicSheetData activeMusicSheet)
    {
        
        //Debug.Break();

        short KeySubBeatLenght = (short)(CurrentBeatProcessingLevel * 4);
        if (KeySubBeatLenght == 3)
        {
            // Croche dotted
            activeMusicSheet.NotesSpriteIdx[PressedKeyIdx] = 11.5f;
            activeMusicSheet.NoteElements[PressedKeyIdx] = 0.75f;
        }
        else
        {
            /// Expand the note as it is being maintained
            /// 10+0 = dblCroche ; 10+1 = Croche ; 10+3 = noire
            int spriteIDX = 10 + Mathf.Min(3, KeySubBeatLenght - 1);
            activeMusicSheet.NotesSpriteIdx[PressedKeyIdx] = spriteIDX;
            activeMusicSheet.NoteElements[PressedKeyIdx] = (spriteIDX - 10) * 0.25f + 0.25f;
        }

        //Debug.Log(KeySubBeatLenght);
        activeMusicSheet.NotesHeight[NoteIdx] = 4;
        
    }

    private void IncrementNote(ref MusicSheetData activeMusicSheet)
    {

        CurrentBeatProcessingLevel += .25f;
        accumulatedBeatWeight += .25f;
        //Debug.Log("eeeeeeeeeeee   " + accumulatedBeatWeight);
        UpdateNote(ref activeMusicSheet);


        if (accumulatedBeatWeight >= 1)
        {

            CurrentBeatProcessingLevel = 0f;
            NoteIdx++;
            accumulatedBeatWeight = 0f;
            accumulatedMesureWeight += 1;
            PressedKeyIdx = NoteIdx;
            //Debug.Log("ooo  " + accumulatedBeatWeight);
            //Debug.Break();
        }

    }

    private void IncrementSilence(ref MusicSheetData activeMusicSheet)
    {

        if (KeyYetToBeProcessedOnce)
        {
            KeyYetToBeProcessedOnce = false;
            accumulatedBeatWeight += .25f;
            if (accumulatedBeatWeight != 1f)
            {
                NoteIdx++;
                activeMusicSheet.ElementsInMesure[MesureIdx + 1]++;
                //Debug.Log("here");

            }

            //Debug.Log("KeyReleaseProcesssssss : " + accumulatedBeatWeight);
        }
        else
        {
            KeyChain = false;
            CurrentBeatProcessingLevel += .25f;
            accumulatedBeatWeight += .25f;
            UpdateSilence(ref activeMusicSheet);

        }

        //Debug.Log("tete");

        //Debug.Log("CurrentBeatProcessingLevel : " + CurrentBeatProcessingLevel);
        if (accumulatedBeatWeight >= 1)
        {

            /// restart from a dblSoupir
            NoteIdx += 1;
            CurrentBeatProcessingLevel = 0f;
            //Debug.Log("aa");

            accumulatedBeatWeight = 0f;
            accumulatedMesureWeight += 1;
        }

    }


}
