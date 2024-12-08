using System.Collections;
using System.Collections.Generic;
using MusicNamespace;
using Unity.Collections;
using Unity.Entities;
using Unity.Entities.UniversalDelegates;
using Unity.Transforms;
using UnityEngine;
using UnityEngine.EventSystems;

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


    protected override void OnCreate()
    {
        //audioGenerator = AudioManager.AudioGenerator;
        //KeyDatasAccumulator = new NativeList<PlaybackKey>();
    }


    protected override void OnUpdate()
    {

        /// OPTI -> Called this multiple times per frame
        mousepos = Camera.main.ScreenToWorldPoint(InputManager.mousePos);


        EntityCommandBuffer ecb = World.GetOrCreateSystemManaged<EndSimulationEntityCommandBufferSystem>().CreateCommandBuffer();

        float normalizedProximity = ((float)SystemAPI.Time.ElapsedTime % (60f / (MusicUtils.BPM * 4))) / (60f / (MusicUtils.BPM * 4));
        float BeatProximity = 1 - Mathf.Abs((normalizedProximity - 0.5f) * 2);



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
                        if (BeatProximity < 0.2f)
                        { mouseDirection = newMouseDir; }
                    }
                    else
                        mouseDirection = newMouseDir;
                }
            }






            SynthData ActiveSynth = AudioLayoutStorageHolder.audioLayoutStorage.SynthsData[recordData.ValueRO.synthIndex];

            var accumulator = SystemAPI.GetBuffer<PlaybackRecordingKeysBuffer>(entity);

            if(BeatProximity <0.2f)
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
                                    lenght = recordData.ValueRO.time - accumulator[accumulator.Length - 1].playbackRecordingKey.time,
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
                                    time = recordData.ValueRO.time,
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
                            time = recordData.ValueRO.time,
                            /// REMOVED AS RAYS DONT CURRENTLY LEAVE RELEASEKEY IN LEGATO
                            //keyCutIdx = short.MaxValue
                        }
                    });
                    recordData.ValueRW.GideReferenceDirection = mouseDirection;
                    recordData.ValueRW.activeLegatoFz = ActiveSynth.Legato ? MusicUtils.DirectionToFrequency(mouseDirection) : 0;
                    keyActive = true;
                    ClickPressed = false;
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
                        lenght = recordData.ValueRO.time - accumulator[accumulator.Length - 1].playbackRecordingKey.time,
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
                                lenght = recordData.ValueRO.time - accumulator[accumulator.Length - 1].playbackRecordingKey.time,
                                /// REMOVED AS RAYS DONT CURRENTLY LEAVE RELEASEKEY IN LEGATO
                                //keyCutIdx = accumulator[accumulator.Length - 1].playbackRecordingKey.keyCutIdx,
                            }
                        };
                        recordData.ValueRW.activeLegatoFz = 0;
                        keyActive = false;
                        ClickReleased = false;
                    }
                }
            }


            recordData.ValueRW.time += SystemAPI.Time.DeltaTime;
            if(recordData.ValueRO.time > recordData.ValueRO.duration)
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
                            lenght = recordData.ValueRO.duration - accumulator[accumulator.Length - 1].playbackRecordingKey.time
                        }
                    };
                    keyActive = false;
                    ClickReleased = false;
                }
                //Debug.Log(accumulator.Length);

                var playbackKeys = new NativeArray<PlaybackKey>(accumulator.Length,Allocator.Persistent);
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
