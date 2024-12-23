using System.Collections;
using System.Collections.Generic;
using MusicNamespace;
using Unity.Collections;
using Unity.Entities;
using Unity.Entities.UniversalDelegates;
using Unity.Transforms;
using UnityEngine;
using UnityEngine.EventSystems;
using static UnityEngine.EventSystems.EventTrigger;


/// <summary>
/// Currently otimized for 1 record at a time
/// -> REVIEW AND OPTIMIZE IF NEED SYMULTANEOUS
/// </summary>

public partial class PlaybackRecordSystem : SystemBase
{

    //public NativeList<PlaybackKey> KeyDatasAccumulator;
    private bool keyActive;
    //public NativeList<float> ActiveFrequencies;

    //public AudioGenerator audioGenerator;

    public Vector2 mousepos;
    private Vector2 mouseDirection;

    public static bool ClickPressed;
    public static bool ClickReleased;
    public static bool OnBeat;

    float PlaybackStartTime;
    float PlaybackTime;
    short MesureIdx = -1;
    short NoteIdx=1;
    short NoteIdxInMesure = -1;
    short NoteSubBeatIdx = -4;
    /// Up to 16 per mesure (4/4)
    /// 1 for starting Key
    short ActivatedNoteElements = 1;
    bool NoteElementActivated;
    ///// Up to 4 subdivision per beat (1/4)
    //short CurrentBeatSubdividedNote = 0;
    ///// the acculated beat subdivion to index the sheetNotes arrays accuratly
    //short BeatSubdividedNoteTotal = 0;

    /// to not reedit duos of subbeat the shoundt change 
    short LockedPreviousSSubBeat = 0;
    short SubBeatDistanceFromLastProcesssed = 0;
    short PressedKeyIdx=0;
    short LastElementSubBeatIdx=-1;
    /// <summary>
    /// INCORECT REDO DEFINITION
    /// Establish at what level the processing of Beat currently occurs
    /// Dynamic and tied of the density and disparity of the note played
    /// 4beat(ronde); 2beat(blanche) ; 1beat(noire) ; 0.5beat(croche) ; 0.25beat(dblCroche)
    /// </summary>
    float CurrentBeatProcessingLevel = 0.25f;
    /// The countdown untill a new silence element (CurrentBeatProcessingLevel*4)
    short KeyExpirationCountdown = 1;
    /// <summary>
    /// SubBeat excess in case of a key press -> require silences addition before adding the key to match the tempo
    /// </summary>
    short SubBeatExcess=0;

    /// <summary>
    ///  to process the element layout and restart mesure
    ///  0 to 4 (4/4)
    /// </summary>
    float accumulatedBeatWeight = 0;
    float accumulatedMesureWeight = 0;


    protected override void OnCreate()
    {
        //audioGenerator = AudioManager.AudioGenerator;
        //KeyDatasAccumulator = new NativeList<PlaybackKey>();
        RequireForUpdate<PlaybackRecordingData>();
    }

    protected override void OnStartRunning()
    {
        float startTimeOffset = 0;
        foreach (var recordData in SystemAPI.Query<RefRO<PlaybackRecordingData>>())
        {
            startTimeOffset = recordData.ValueRO.time;
        }
        PlaybackStartTime = (float)MusicUtils.time - startTimeOffset;
        NoteIdxInMesure = -1;
        PressedKeyIdx = 0;
        SubBeatExcess = 0;
        //CurrentBeatSubdividedNote = 0;
        //BeatSubdividedNoteTotal = 0;
        ActivatedNoteElements=1;

        MusicSheetData ActiveMusicSheet = AudioLayoutStorageHolder.audioLayoutStorage.ActiveMusicSheet;
        ActiveMusicSheet.NotesSpriteIdx[1] = 0;
        ActiveMusicSheet.NotesHeight[1] = 6;
        accumulatedMesureWeight = 0;
        CurrentBeatProcessingLevel = 0f;
        accumulatedBeatWeight = 0f;
        NoteIdx = 1;
        NoteSubBeatIdx = 0;

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

        PlaybackTime = (float)MusicUtils.time - PlaybackStartTime;
        //Debug.Log(PlaybackTime);
        float SclicedBeatTime = PlaybackTime * ((MusicUtils.BPM * 4)/60f);
        float normalizedProximity = (PlaybackTime % (60f / (MusicUtils.BPM * 4))) / (60f / (MusicUtils.BPM * 4));
        float SclicedBeatProximity = 1 - Mathf.Abs((normalizedProximity - 0.5f) * 2);
        //MesuerIdx = (short)Mathf.RoundToInt((SclicedBeatTime * 0.25f) * 0.25f);
        //NoteIdx = (short)Mathf.RoundToInt(SclicedBeatTime * 0.25f);

        short newMesureIdx = (short)(Mathf.FloorToInt((SclicedBeatTime * 0.25f) * 0.25f));
        short newNoteidx = (short)(Mathf.FloorToInt(SclicedBeatTime * 0.25f));
        /// CLOSEST SUB BEAT :
        short ClosestSubBeatIdx = (short)Mathf.RoundToInt(SclicedBeatTime % 4);
        short newNoteSubBeatIdx = (short)(newNoteidx * 4 + Mathf.FloorToInt(SclicedBeatTime % 4));
        //Debug.Log(newNoteSubBeatIdx);
        /// Offest due to the SubBeat proximity and SubBeat floor
        short SubBeatOffset = (short)(ClosestSubBeatIdx - newNoteSubBeatIdx);
        short FirstOrLastSubBeat = (short)(ClosestSubBeatIdx/4); /// Integer division
        //Debug.Log(newMesureIdx);

        //MesureIdx = newMesureIdx > MesureIdx ? newMesureIdx : MesureIdx;
        //NoteIdx = newNoteIdx > NoteIdx ? newNoteIdx : NoteIdx;





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
                    if (!keyActive)
                        mouseDirection = mousepos - new Vector2(Wtrans.ValueRO.Position.x, Wtrans.ValueRO.Position.y);
                }
                else
                {
                    Vector2 newMouseDir = mousepos - new Vector2(Wtrans.ValueRO.Position.x, Wtrans.ValueRO.Position.y);
                    float currentFz = MusicUtils.DirectionToFrequency(newMouseDir);
                    if (currentFz != recordData.ValueRO.activeLegatoFz)
                    {
                        if (SclicedBeatProximity < 0.2f)
                        { mouseDirection = newMouseDir; }
                    }
                    else
                        mouseDirection = newMouseDir;
                }
            }






            SynthData ActiveSynth = AudioLayoutStorageHolder.audioLayoutStorage.SynthsData[recordData.ValueRO.synthIndex];

            var accumulator = SystemAPI.GetBuffer<PlaybackRecordingKeysBuffer>(entity);

            if(SclicedBeatProximity <0.2f)
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
                        //else
                        //{
                        //    accumulator[accumulator.Length - 1] = new PlaybackRecordingKeysBuffer
                        //    {
                        //        playbackRecordingKey = new PlaybackKey
                        //        {
                        //            dir = accumulator[accumulator.Length - 1].playbackRecordingKey.dir,
                        //            startDir = accumulator[accumulator.Length - 1].playbackRecordingKey.startDir,
                        //            time = accumulator[accumulator.Length - 1].playbackRecordingKey.time,
                        //            lenght = recordData.ValueRO.time - accumulator[accumulator.Length - 1].playbackRecordingKey.time,
                        //            /// REMOVED AS RAYS DONT CURRENTLY LEAVE RELEASEKEY IN LEGATO
                        //            //keyCutIdx = accumulator[accumulator.Length - 1].playbackRecordingKey.keyCutIdx,
                        //        }
                        //    };
                        //    recordData.ValueRW.activeLegatoFz = 0;
                        //    keyActive = false;
                        //    ClickReleased = false;
                        //}
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
                    keyActive = true;
                    ClickPressed = false;

                    //Debug.Log(SubBeatOffset);


                    if (SubBeatExcess > 0)
                    {
                        NoteIdx++;
                        ActiveMusicSheet.ElementsInMesure[MesureIdx + 1]++;
                        ActiveMusicSheet.NotesHeight[NoteIdx] = 6;
                        ActiveMusicSheet.NotesSpriteIdx[NoteIdx] = 0;
                        SubBeatExcess = 0;
                        //accumulatedBeatWeight += .25f;
                        Debug.Log("subb");
                    }
                    //if(accumulatedBeatWeight >=1)
                    //{
                    //    accumulatedBeatWeight = 0;
                    //    accumulatedMesureWeight = 1;
                    //    Debug.Log("test");
                    //}
                    ///
                    if (accumulatedBeatWeight != 0f)
                    {
                        NoteIdx++;
                        ActiveMusicSheet.ElementsInMesure[MesureIdx + 1]++;

                        Debug.Log("accumulatedBeatWeight : " + accumulatedBeatWeight);
                    }
                    //else
                        //NoteIdx++;

                    //else
                    {
                
                        //ActiveMusicSheet.ElementsInMesure[MesureIdx + 1]++;
                        ActiveMusicSheet.NotesHeight[NoteIdx] = 6;
                        ActiveMusicSheet.NotesSpriteIdx[NoteIdx] = 10;
                        CurrentBeatProcessingLevel = 0f;
                        //accumulatedBeatWeight += .25f;
                        //Debug.Log("test");
                    }
                    PressedKeyIdx=(short)(NoteIdx);


                    //if (ClosestSubBeatIdx%4 !=0)
                    //{
                    //    /// UPON CLICKING FILL UP THE SPACE SINCE THE LAST KEY RELEASE
                    //    if (SubBeatExcess > 0)
                    //    {
                    //        //CurrentBeatSubdividedNote++;
                    //        //BeatSubdividedNoteTotal++;
                    //        ActiveMusicSheet.ElementsInMesure[MesureIdx+1]++;
                    //        ActiveMusicSheet.NotesHeight[NoteIdx + 1] = 6;
                    //        ActiveMusicSheet.NotesSpriteIdx[NoteIdx + 1] = 0;
                    //        SubBeatExcess = 0;
                    //        Debug.Log("1");
                    //    }
                    //    else
                    //        Debug.Log("1.5");

                    //    /// SAFETY CHECK -> PRESS KEY TWICE WITHIN BEAT
                    //    //CurrentBeatSubdividedNote++;
                    //    //BeatSubdividedNoteTotal++;
                    //    ActiveMusicSheet.ElementsInMesure[MesureIdx+1]++;
                    //    ActiveMusicSheet.NotesHeight[NoteIdx+1] = 0;
                    //    ActiveMusicSheet.NotesSpriteIdx[NoteIdx+1] = 10;
                    //    PressedKeyIdx = (short)(NoteIdx + 1);
                    //    CurrentBeatProcessingLevel = 0.25f;

                    //    //NoteIdx = (short)(newNoteIdx + FirstOrLastSubBeat);
                    //    NoteSubBeatIdx = (short)(newNoteSubBeatIdx + 1 + SubBeatOffset);
                    //    //for (int i = SubBeatIdx; i > 0; i--)
                    //    //{

                    //    //}

                    //}
                    //else
                    //{
                    //    /// COULD CAUSE PROBLEMS WHEN ACTIVATED AT THE VERY END OF THE RECORDING? (out of bounds)
                    //    ActiveMusicSheet.NotesHeight[NoteIdx + FirstOrLastSubBeat+1] = 4;
                    //    ActiveMusicSheet.NotesSpriteIdx[NoteIdx + FirstOrLastSubBeat+1] = 10;
                    //    PressedKeyIdx = (short)(NoteIdx + FirstOrLastSubBeat+1);
                    //    //NoteIdx =(short)(newNoteIdx+ FirstOrLastSubBeat);
                    //    NoteSubBeatIdx = (short)(newNoteSubBeatIdx+ SubBeatOffset);
                    //    Debug.Log("2");
                    //}
                    //CurrentBeatProcessingLevel = .25f;
                    ////NoteElementActivated = true;
                    //ActivatedNoteElements++;

                    //switch (SubBeatIdx)
                    //{
                    //    case 1:
                    //        CurrentBeatSubdividedNote += 2;
                    //        BeatSubdividedNoteTotal += 2;
                    //        ActiveMusicSheet.ElementsInMesure[MesureIdx] += 2;
                    //        ActiveMusicSheet.NotesHeight[newNoteIdx + BeatSubdividedNoteTotal-1] = 4;
                    //        ActiveMusicSheet.NotesSpriteIdx[newNoteIdx + BeatSubdividedNoteTotal-1] = 2;
                    //        ActiveMusicSheet.NotesHeight[newNoteIdx + BeatSubdividedNoteTotal] = 6;
                    //        ActiveMusicSheet.NotesSpriteIdx[newNoteIdx + BeatSubdividedNoteTotal] = 5;
                    //        break;
                    //    case 2:
                    //        CurrentBeatSubdividedNote += 1;
                    //        BeatSubdividedNoteTotal += 1;
                    //        ActiveMusicSheet.ElementsInMesure[MesureIdx] += 1;
                    //        ActiveMusicSheet.NotesHeight[newNoteIdx + BeatSubdividedNoteTotal - 1] = 4;
                    //        ActiveMusicSheet.NotesSpriteIdx[newNoteIdx + BeatSubdividedNoteTotal - 1] = 2;
                    //        ActiveMusicSheet.NotesHeight[newNoteIdx + BeatSubdividedNoteTotal] = 6;
                    //        ActiveMusicSheet.NotesSpriteIdx[newNoteIdx + BeatSubdividedNoteTotal] = 5;
                    //        break;

                    //    default:
                    //        break;
                    //}




                    //ActiveMusicSheet.ElementsInMesure[MesureIdx]++;
                    //short noteIdx = (short)(ActiveMusicSheet.ElementsInMesure[1] - 1 + 1);
                    //ActiveMusicSheet.NotesSpriteIdx[noteIdx] = 1;
                    //for (int i = noteIdx; i >= 1; i--)
                    //{
                    //    ActiveMusicSheet.NoteElements[i] = (4f / noteIdx);
                    //}
                    //ActiveMusicSheet.NotesHeight[noteIdx] = 4;
                    //ActivatedNoteElements++;

                    //Debug.Log(noteIdx);
                }
            }


            if (UIInputSystem.MouseOverUI)
            {
                if(keyActive)
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
                    keyActive = false;
                }

            }
            else
            {

                if (ClickReleased)
                {
                    if(keyActive)
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
                        keyActive = false;
                        ClickReleased = false;

                        /// for sheet
                        Debug.Log(CurrentBeatProcessingLevel);
                        short KeySubBeatLenght = (short)((CurrentBeatProcessingLevel-0.25f) * 4);
                        /// 10+0 = dblCroche ; 10+1 = Croche ; 10+3 = noire
                        //ActiveMusicSheet.NotesSpriteIdx[PressedKeyIdx] = 10 + (KeySubBeatLenght - 1);

                        //if (CurrentBeatProcessingLevel == 0.25f)
                        //{
                        //    ActiveMusicSheet.ElementsInMesure[MesureIdx + 1]++;
                        //    Debug.Log("test2");
                        //}

                        if (SubBeatExcess > 0)
                        {
                            ActiveMusicSheet.ElementsInMesure[MesureIdx + 1]++;
                            ActiveMusicSheet.NotesHeight[NoteIdx] = 6;
                            ActiveMusicSheet.NotesSpriteIdx[NoteIdx] = 0;
                            NoteIdx++;
                            SubBeatExcess = 0;
                            Debug.Log("excess release");
                        }
                        if (accumulatedBeatWeight != 0f)
                        {
                            //NoteIdx++;
                            ActiveMusicSheet.ElementsInMesure[MesureIdx + 1]++;

                            Debug.Log("extend");
                            NoteIdx++;
                            ActiveMusicSheet.NotesSpriteIdx[NoteIdx] = 0;
                            ActiveMusicSheet.NotesHeight[NoteIdx] = 6;
                        }
                        CurrentBeatProcessingLevel = 0;
                        Debug.Log("accumulatedBeatWeight : " + accumulatedBeatWeight);


                        //CurrentBeatProcessingLevel = 0;
                        //if ((CurrentBeatProcessingLevel+0.25f) != 1)
                        //{
                        //    ActiveMusicSheet.ElementsInMesure[MesureIdx + 1]++;
                        //    ActiveMusicSheet.NotesHeight[NoteIdx] = 6;
                        //    ActiveMusicSheet.NotesSpriteIdx[NoteIdx] = 0;
                        //    Debug.Log("test");
                        //    CurrentBeatProcessingLevel += 0.25f;
                        //}
                        //else
                        //    CurrentBeatProcessingLevel = 0.25f;


                        /// Offest due to the SubBeat proximity and SubBeat floor
                        //short SubBeatOffset = (short)(ClosestSubBeatIdx - newNoteSubBeatIdx);
                        //NoteSubBeatIdx = (short)(newNoteSubBeatIdx + SubBeatOffset);
                        //NoteIdx = (short)(newNoteIdx + FirstOrLastSubBeat + SubBeatOffset);

                        //short KeySubBeatLenght = (short)(CurrentBeatProcessingLevel * 4);
                        //if (KeySubBeatLenght == 3)
                        //{
                        //    KeySubBeatLenght = 2;
                        //    SubBeatExcess = 1;
                        //}
                        //else
                        //    SubBeatExcess = 0;
                        ///// 10+0 = dblCroche ; 10+1 = Croche ; 10+3 = noire
                        //ActiveMusicSheet.NotesSpriteIdx[PressedKeyIdx] = 10 + (KeySubBeatLenght - 1);
                        //CurrentBeatProcessingLevel += .25f;

                        //if (ClosestSubBeatIdx % 4 != 0)
                        //{
                        //    CurrentBeatSubdividedNote++;
                        //    BeatSubdividedNoteTotal++;
                        //    //ActiveMusicSheet.ElementsInMesure[MesureIdx + 1]++;
                        //    //CurrentBeatProcessingLevel = ClosestSubBeatIdx * 0.25f;
                        //}
                        //else
                        //{
                        //    //CurrentBeatSubdividedNote = 0;
                        //}
                        //CurrentBeatProcessingLevel = 0.25f;
                        //if (SubBeatExcess > 0)
                        //{

                        //    //ActiveMusicSheet.ElementsInMesure[MesureIdx + 1]++;
                        //    ActiveMusicSheet.NotesHeight[newNoteIdx + CurrentBeatSubdividedNote + 1] = 6;
                        //    ActiveMusicSheet.NotesSpriteIdx[newNoteIdx + CurrentBeatSubdividedNote + 1] = 0;
                        //    SubBeatExcess = 0;
                        //    Debug.Log("excess release");
                        //}

                        //NoteSubBeatIdx = (short)(newNoteSubBeatIdx + SubBeatOffset);

                        //SubBeatIdx = (short)(SubBeatIdx % 4);
                        //float KeyPressedBeatDistance = (newNoteIdx + (ClosestSubBeatIdx * 0.25f) + 1) - PressedKeyIdx;
                        ///// 1+2 = dblCroche ; 1+1 = Croche ; 1+0 = noire
                        //ActiveMusicSheet.NotesSpriteIdx[PressedKeyIdx] = 1 + Mathf.Max(3 - (KeyPressedBeatDistance) * 4, 0);
                        /// DO THE EDGE CASE OF 0.75 Beat note with excess ?
                        NoteElementActivated = false;
                        //LastElementIdx = ;
                        //LastElementSubBeatIdx = (short)(newNoteSubBeatIdx+CurrentBeatSubdividedNote);
                        PressedKeyIdx = 0;

                    }
                }
            }


            ///recordData.ValueRW.time += SystemAPI.Time.DeltaTime;
            /// SET IN PLACE OF BEAT COUNTING
            //if(PlaybackTime > recordData.ValueRO.duration)
            //{
            //    if(keyActive)
            //    {
            //        accumulator[accumulator.Length - 1] = new PlaybackRecordingKeysBuffer
            //        {
            //            playbackRecordingKey = new PlaybackKey
            //            {
            //                dir = accumulator[accumulator.Length - 1].playbackRecordingKey.dir,
            //                startDir = accumulator[accumulator.Length - 1].playbackRecordingKey.startDir,
            //                time = accumulator[accumulator.Length - 1].playbackRecordingKey.time,
            //                lenght = recordData.ValueRO.duration - accumulator[accumulator.Length - 1].playbackRecordingKey.time
            //            }
            //        };
            //        keyActive = false;
            //        ClickReleased = false;
            //    }
            //    //Debug.Log(accumulator.Length);

            //    var playbackKeys = new NativeArray<PlaybackKey>(accumulator.Length,Allocator.Persistent);
            //    playbackKeys.CopyFrom(accumulator.AsNativeArray().Reinterpret<PlaybackKey>());
            //    AudioLayoutStorageHolder.audioLayoutStorage.WritePlayback(new PlaybackAudioBundle
            //    {
            //        IsLooping = true,
            //        //IsPlaying = false,
            //        PlaybackDuration = recordData.ValueRO.duration,
            //        PlaybackKeys = playbackKeys
            //    }, recordData.ValueRO.synthIndex);
            //    //Debug.Log(AudioManager.audioGenerator.audioLayoutStorage.NewPlaybackAudioBundles.PlaybackKeys.Length);
            //    ecb.RemoveComponent<PlaybackRecordingKeysBuffer>(entity);
            //    ecb.RemoveComponent<PlaybackRecordingData>(entity);

            //}

            //if(KeyExpirationCountdown<=0)
            if (newNoteSubBeatIdx > NoteSubBeatIdx)
            {
                //Debug.LogError(accumulatedMesureWeight);
                //KeyExpirationCountdown -= 1;
                //Debug.Log(accumulatedBeatWeight);
                //Debug.Log("count");
                //Debug.LogError(accumulatedMesureWeight);
                if (PressedKeyIdx == 0)
                {
                    //if (newNoteIdx > NoteIdx)
                    //{
                    //    /// restart from a dblSoupir
                    //    ActiveMusicSheet.NotesSpriteIdx[newNoteIdx + CurrentBeatSubdividedNote + 1] = 0;
                    //    ActiveMusicSheet.NotesHeight[newNoteIdx + CurrentBeatSubdividedNote + 1] = 6;
                    //    //KeyExpirationCountdown = 4;
                    //    CurrentBeatProcessingLevel = 0.5f;
                    //    //LastElementSubBeatIdx = (newNoteSubBeatIdx);

                    //    NoteIdx = newNoteIdx;
                    //}
                    CurrentBeatProcessingLevel += .25f;
                    accumulatedBeatWeight += .25f;
                    {
                        //Debug.Log("eeeeeeeeeeee");
                        short SilenceSubBeatLenght = (short)(CurrentBeatProcessingLevel * 4);
                        //Debug.Log(SilenceSubBeatLenght);
                        if (SilenceSubBeatLenght == 3)
                        {
                            SilenceSubBeatLenght = 2;
                            SubBeatExcess = 1;
                        }
                        else
                            SubBeatExcess = 0;
                        /// Expand the silence of the element in the absence of note played
                        /// 0+0 = dblSoupir ; 0+1 = Soupir ; 0+3 = silence
                        ActiveMusicSheet.NotesSpriteIdx[NoteIdx] = 0 + (SilenceSubBeatLenght - 1);
                        ActiveMusicSheet.NotesHeight[NoteIdx] = 6;
                    }
                    //Debug.Log("CurrentBeatProcessingLevel : " + CurrentBeatProcessingLevel);
                    if (accumulatedBeatWeight >=1)
                    {
                        if(SubBeatExcess>0)
                        {
                            NoteIdx++;
                            ActiveMusicSheet.ElementsInMesure[MesureIdx + 1]++;
                            ActiveMusicSheet.NotesHeight[NoteIdx] = 6;
                            ActiveMusicSheet.NotesSpriteIdx[NoteIdx] = 0;
                            SubBeatExcess = 0;
                            Debug.Log("excess release INNN");
                        }

                        /// restart from a dblSoupir
                        NoteIdx += 1;
                        //ActiveMusicSheet.NotesSpriteIdx[NoteIdx] = 0;
                        //ActiveMusicSheet.NotesHeight[NoteIdx] = 6;
                        //KeyExpirationCountdown = 4;
                        CurrentBeatProcessingLevel = 0f;
                        //LastElementSubBeatIdx = (newNoteSubBeatIdx);
                        Debug.Log("aa");

                        accumulatedBeatWeight = 0f;
                        accumulatedMesureWeight += 1;
                        //NoteIdx = newNoteIdx;
                    }
                    //else
          
                    //if (accumulatedBeatWeight >= 1)
                    //{
                    //    accumulatedBeatWeight = 0f;
                    //    accumulatedMesureWeight += 1;
                    //    //LastElementSubBeatIdx = (newNoteSubBeatIdx);
                    //    Debug.Log("nn");

                    //    //NoteIdx = newNoteIdx;
                    //}
                }
                /// Expand the note with increment
                else
                {
                    //Debug.Log("count");

                    CurrentBeatProcessingLevel += .25f;
                    accumulatedBeatWeight += .25f;
                    //Debug.Log("CurrentBeatProcessingLevel : " + CurrentBeatProcessingLevel);
                    {
                        //Debug.Break();
                        Debug.Log("eeeeeeeeeeee");

                        short KeySubBeatLenght = (short)(CurrentBeatProcessingLevel * 4);
                        if (KeySubBeatLenght == 3)
                        {
                            //Debug.Log("need be 3");
                            ActiveMusicSheet.NotesHeight[NoteIdx] = 8;
                            KeySubBeatLenght = 2;
                            SubBeatExcess = 1;
                        }
                        else
                        {
                            ActiveMusicSheet.NotesHeight[NoteIdx] = 4;
                            SubBeatExcess = 0;
                        }
                        //Debug.Log(KeySubBeatLenght);
                        /// Expand the note as it is being maintained
                        /// 10+0 = dblCroche ; 10+1 = Croche ; 10+3 = noire
                        ActiveMusicSheet.NotesSpriteIdx[PressedKeyIdx] = 10 + Mathf.Min(3, KeySubBeatLenght - 1);
                        ActiveMusicSheet.NotesHeight[NoteIdx] = 4;
                    }
                    if (accumulatedBeatWeight >= 1)
                    {
                        if (SubBeatExcess > 0)
                        {
                            ActiveMusicSheet.NotesSpriteIdx[NoteIdx] = 10;
                            NoteIdx++;
                            ActiveMusicSheet.ElementsInMesure[MesureIdx + 1]++;
                            ActiveMusicSheet.NotesHeight[NoteIdx] = 4;
                            ActiveMusicSheet.NotesSpriteIdx[NoteIdx] = 11;
                            SubBeatExcess = 0;
                            Debug.Log("excess release INNN PLAYING");
                        }

                        CurrentBeatProcessingLevel = 0f;
                        NoteIdx++;
                        //ActiveMusicSheet.NotesSpriteIdx[NoteIdx] = 10;
                        //ActiveMusicSheet.NotesHeight[NoteIdx] = 4;
                        accumulatedBeatWeight = 0f;
                        accumulatedMesureWeight += 1;
                        PressedKeyIdx = NoteIdx;
                        Debug.Log("ooo");
                        //Debug.Break();
                    }
                    //else
            


                    //if (accumulatedBeatWeight >= 1)
                    //{

                    //    Debug.Log("rererr");
                    //    //Debug.Break();
                    //    //NoteIdx++;
                    //    //ActiveMusicSheet.NotesSpriteIdx[NoteIdx] = 10;
                    //    //ActiveMusicSheet.NotesHeight[NoteIdx] = 4;
                    //    ////NoteIdx += 1;
                    //    //CurrentBeatProcessingLevel = 0.25f;

                    //    accumulatedBeatWeight = 0f;
                    //    accumulatedMesureWeight += 1;

                    //}
                }

                //Debug.Log(newNoteIdx);
                NoteSubBeatIdx = newNoteSubBeatIdx;

                if (accumulatedMesureWeight >= 4)
                {
                    ///TEMP TEST 
                    Debug.Break();

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
                        if (keyActive)
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
                            keyActive = false;
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



     
     




        /// SubBeat increment
        //if(newNoteSubBeatIdx > NoteSubBeatIdx)
        //{
        //    if (newNoteIdx > NoteIdx)
        //    {
        //        LastElementSubBeatIdx = (newNoteSubBeatIdx);
        //        NoteIdx = newNoteIdx;
        //    }
        //    if (!NoteElementActivatedOnBeat)
        //    {
        //        if (PressedKeyIdx == 0)
        //        {
        //            /// scilence ou soupir calculs ici ?
        //            //float LastElementDistance = (newNoteIdx + (SubBeatIdx * 0.25f)) - LastElementIdx;
        //            float SilenceSubBeatLenght = (newNoteSubBeatIdx - LastElementSubBeatIdx);

        //            //float KeyPressedBeatDistance = (newNoteIdx + (SubBeatIdx * 0.25f)) - PressedKeyIdx;
        //            //ActiveMusicSheet.NotesSpriteIdx[PressedKeyIdx] = 1 + Mathf.Max(3 - (KeyPressedBeatDistance) * 4, 0);

        //            /// 4+2 = dblSoupir ; 4+1 = Soupir ; 4+0 = silence
        //            ActiveMusicSheet.NotesSpriteIdx[newNoteIdx + CurrentBeatSubdividedNote + 1] = 4 + Mathf.Max(3 - SilenceSubBeatLenght, 0);
        //            ActiveMusicSheet.NotesHeight[newNoteIdx + CurrentBeatSubdividedNote + 1] = 6;
        //            //ActivatedNoteElements++;
        //        }
        //    }
        //    else
        //    {

        //        float KeyPressedBeatDistance = (newNoteIdx + (SubBeatIdx * 0.25f)+1) - PressedKeyIdx;
        //        /// 1+2 = dblCroche ; 1+1 = Croche ; 1+0 = noire
        //        ActiveMusicSheet.NotesSpriteIdx[PressedKeyIdx] = 1 + Mathf.Max(3 - (KeyPressedBeatDistance) * 4, 0);
        //    }

        //    //else
        //    //{
        //    //    if(NoteElementActivatedOnBeat)
        //    //    {

        //    //    }
        //    //}
        //    NoteSubBeatIdx = newNoteSubBeatIdx;
        //    NoteElementActivatedOnBeat = false;
        //}



        //if (newNoteIdx > NoteIdx)
        //{
        //    //Debug.Log(newNoteIdx);
        //    /// redondant ?
        //    NoteIdx = newNoteIdx;
        //    NoteIdxInMesure++;

        //    if (!NoteElementActivatedOnBeat && PressedKeyIdx==0)
        //    {
        //        /// scilence ou soupir calculs ici ?
        //        float LastElementDistance = (newNoteIdx + (SubBeatIdx * 0.25f)) - LastElementIdx;

        //        //float KeyPressedBeatDistance = (newNoteIdx + (SubBeatIdx * 0.25f)) - PressedKeyIdx;
        //        //ActiveMusicSheet.NotesSpriteIdx[PressedKeyIdx] = 1 + Mathf.Max(3 - (KeyPressedBeatDistance) * 4, 0);

        //        ActiveMusicSheet.NotesSpriteIdx[NoteIdx+CurrentBeatSubdividedNote] = 4;
        //        ActiveMusicSheet.NotesHeight[NoteIdx + CurrentBeatSubdividedNote] = 6;
        //        ActivatedNoteElements++;
        //    }

        //    NoteElementActivatedOnBeat = false;
        //    CurrentBeatSubdividedNote = 0;

        //    //ActiveMusicSheet.ElementsInMesure[MesureIdx + 1]++;

        //    //for (int i = NoteIdx; i >= 1; i--)
        //    //{
        //    //    ActiveMusicSheet.NoteElements[i] = (4f / noteIdx);
        //    //}

        //}


    }

}
