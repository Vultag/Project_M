using System.Collections;
using System.Collections.Generic;
using MusicNamespace;
using Unity.Collections;
using Unity.Entities;
using Unity.Entities.UniversalDelegates;
using Unity.Transforms;
using UnityEngine;

public partial class PlaybackRecordSystem : SystemBase
{

    //public NativeList<PlaybackKey> KeyDatasAccumulator;
    private bool keyActive;
    //public NativeList<float> ActiveFrequencies;

    //public AudioGenerator audioGenerator;

    public Vector2 mousepos;

    public static bool ClickPressed;
    public static bool ClickReleased;


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


        //foreach (var (recordData,keyBuffer) in SystemAPI.Query<RefRW<PlaybackRecordingData>, DynamicBuffer <SustainedKeyBufferData>> ())
        foreach (var (Wtrans, recordData, entity) in SystemAPI.Query<RefRO<LocalToWorld>,RefRW<PlaybackRecordingData>>().WithEntityAccess())
        {

            /// if mouse click
            /// prepair a playbackkey with the lenght missing
            /// if mouse release
            /// add the prepaired playbackkey to the list, set duration to time-playbackkey.time


            var accumulator = SystemAPI.GetBuffer<PlaybackRecordingKeysBuffer>(entity);

            if (UIInputSystem.MouseOverUI)
            {
                if(keyActive)
                {
                    accumulator[accumulator.Length - 1] = new PlaybackRecordingKeysBuffer{ playbackRecordingKey = new PlaybackKey
                    {
                        dir = accumulator[accumulator.Length - 1].playbackRecordingKey.dir,
                        time = accumulator[accumulator.Length - 1].playbackRecordingKey.time,
                        lenght = recordData.ValueRO.time - accumulator[accumulator.Length - 1].playbackRecordingKey.time
                    }};
                    keyActive = false;
                }

            }
            else
            {
                if (ClickPressed)
                {
                    Vector2 direction = mousepos - new Vector2(Wtrans.ValueRO.Position.x, Wtrans.ValueRO.Position.y);
                    //float randian = Mathf.Abs(PhysicsUtilities.DirectionToRadians(direction));
                    //int note = MusicUtils.radiansToNote(randian);

                    accumulator.Add(new PlaybackRecordingKeysBuffer { playbackRecordingKey = new PlaybackKey { dir = direction, time = recordData.ValueRO.time } });
                    keyActive = true;
                    ClickPressed = false;
                }

                if (ClickReleased)
                {
                    if(keyActive)
                    {
                        accumulator[accumulator.Length - 1] = new PlaybackRecordingKeysBuffer
                        {
                            playbackRecordingKey = new PlaybackKey
                            {
                                dir = accumulator[accumulator.Length - 1].playbackRecordingKey.dir,
                                time = accumulator[accumulator.Length - 1].playbackRecordingKey.time,
                                lenght = recordData.ValueRO.time - accumulator[accumulator.Length - 1].playbackRecordingKey.time
                            }
                        };
                        keyActive = false;
                        ClickReleased = false;
                    }
                }
            }


            recordData.ValueRW.time += SystemAPI.Time.DeltaTime;
            if(recordData.ValueRO.time > recordData.ValueRO.duration)
            {
                //Debug.Log("finished record");
                if(keyActive)
                {
                    accumulator[accumulator.Length - 1] = new PlaybackRecordingKeysBuffer
                    {
                        playbackRecordingKey = new PlaybackKey
                        {
                            dir = accumulator[accumulator.Length - 1].playbackRecordingKey.dir,
                            time = accumulator[accumulator.Length - 1].playbackRecordingKey.time,
                            lenght = recordData.ValueRO.duration - accumulator[accumulator.Length - 1].playbackRecordingKey.time
                        }
                    };
                    keyActive = false;
                    ClickReleased = false;
                }


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
