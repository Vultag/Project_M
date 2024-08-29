using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using Unity.Entities.UniversalDelegates;
using Unity.Transforms;
using UnityEngine;

public partial struct PlaybackSystem : ISystem
{


    void OnCreate(ref SystemState state)
    {

    }


    void OnUpdate(ref SystemState state)
    {

        EntityCommandBuffer ecb = SystemAPI.GetSingleton<BeginSimulationEntityCommandBufferSystem.Singleton>().CreateCommandBuffer(state.WorldUnmanaged);

        ComponentLookup<CircleShapeData> ShapeComponentLookup = SystemAPI.GetComponentLookup<CircleShapeData>(isReadOnly: true);

        NativeQueue<Vector2> KeyDirQueue = new NativeQueue<Vector2>(Allocator.Temp);

        foreach (var (playback_data, synth_data, Wtrans) in SystemAPI.Query<RefRW<PlaybackData>, RefRO<SynthData>, RefRO<LocalToWorld>>())
        {

            /// Damage processing 
            
            //AudioLayoutStorageHolder.audioLayoutStorage.PlaybackAudioBundles[playback_data.ValueRO.PlaybackIndex]



            int playbackKeyIndex = playback_data.ValueRW.PlaybackKeyIndex;

            //Debug.Log(AudioLayoutStorageHolder.audioLayoutStorage.PlaybackAudioBundles.Length);

            while (playback_data.ValueRO.PlaybackKeyIndex < AudioLayoutStorageHolder.audioLayoutStorage.PlaybackAudioBundles[playback_data.ValueRO.PlaybackIndex].PlaybackKeys.Length
                && AudioLayoutStorageHolder.audioLayoutStorage.PlaybackAudioBundles[playback_data.ValueRO.PlaybackIndex].PlaybackKeys[playbackKeyIndex].time < playback_data.ValueRO.PlaybackTime)
            {
                if(AudioLayoutStorageHolder.audioLayoutStorage.PlaybackAudioBundles[playback_data.ValueRO.PlaybackIndex].PlaybackKeys[playbackKeyIndex].time + AudioLayoutStorageHolder.audioLayoutStorage.PlaybackAudioBundles[playback_data.ValueRO.PlaybackIndex].PlaybackKeys[playbackKeyIndex].lenght < playback_data.ValueRO.PlaybackTime)
                {
                    //if(playbackKeyIndex < AudioLayoutStorageHolder.audioLayoutStorage.PlaybackAudioBundles[playback_data.ValueRO.PlaybackIndex].PlaybackKeys.Length)
                    {
                        playbackKeyIndex++;
                        playback_data.ValueRW.PlaybackKeyIndex = playbackKeyIndex;
                       
                    }
                    //else
                    //    break;
                }
                else
                {
                    KeyDirQueue.Enqueue(AudioLayoutStorageHolder.audioLayoutStorage.PlaybackAudioBundles[playback_data.ValueRO.PlaybackIndex].PlaybackKeys[playbackKeyIndex].dir);
                    playbackKeyIndex++;
                }
                if (playbackKeyIndex >= AudioLayoutStorageHolder.audioLayoutStorage.PlaybackAudioBundles[playback_data.ValueRO.PlaybackIndex].PlaybackKeys.Length)
                    break;
                //if (playbackKeyIndex < AudioLayoutStorageHolder.audioLayoutStorage.PlaybackAudioBundles[playback_data.ValueRO.PlaybackIndex].PlaybackKeys.Length)
                //{
                //    break;
                //}

            }



            //playbackKeyIndex -= playback_data.ValueRO.PlaybackKeyIndex;
            //playback_data.ValueRW.PlaybackKeyIndex = playbackKeyIndex;

            if (playback_data.ValueRW.PlaybackTime + SystemAPI.Time.DeltaTime > AudioLayoutStorageHolder.audioLayoutStorage.PlaybackAudioBundles[playback_data.ValueRO.PlaybackIndex].PlaybackDuration)
            {
                if(AudioLayoutStorageHolder.audioLayoutStorage.PlaybackAudioBundles[playback_data.ValueRO.PlaybackIndex].IsLooping)
                {
                    playback_data.ValueRW.PlaybackTime = (playback_data.ValueRW.PlaybackTime + SystemAPI.Time.DeltaTime) - AudioLayoutStorageHolder.audioLayoutStorage.PlaybackAudioBundles[playback_data.ValueRO.PlaybackIndex].PlaybackDuration;
                    playback_data.ValueRW.PlaybackKeyIndex = 0;
                }
                else
                {
                    throw new InvalidOperationException("One shot playback not implemented yet");
                }
            }
            else
                playback_data.ValueRW.PlaybackTime += SystemAPI.Time.DeltaTime;

            while(KeyDirQueue.Count>0)
            {
                //if(AudioLayoutStorageHolder.audioLayoutStorage.PlaybackAudioBundles[playback_data.ValueRO.PlaybackIndex].PlaybackKeys[playbackKeyIndex].time)
                //{

                //}
                Vector2 keyDir = KeyDirQueue.Dequeue();
                RayCastHit Hit = PhysicsCalls.RaycastNode(new Ray { Origin = new Vector2(Wtrans.ValueRO.Position.x, Wtrans.ValueRO.Position.y), DirLength = keyDir }, PhysicsUtilities.CollisionLayer.MonsterLayer, ShapeComponentLookup);

                if (Hit.entity != Entity.Null)
                {
                    //Debug.Log(Hit.distance);
                    ///Debug.DrawLine(Wtrans.ValueRO.Position, new Vector2(Wtrans.ValueRO.Position.x, Wtrans.ValueRO.Position.y) + SkeyBuffer[i].Direction, Color.green, SystemAPI.Time.DeltaTime);

                    //Debug.DrawLine(Wtrans.ValueRO.Position, SystemAPI.GetComponent<CircleShapeData>(Hit.entity).Position, Color.red, SystemAPI.Time.DeltaTime);
                    Debug.DrawLine(Wtrans.ValueRO.Position, new Vector2(Wtrans.ValueRO.Position.x, Wtrans.ValueRO.Position.y) + (keyDir.normalized * Hit.distance), Color.red, SystemAPI.Time.DeltaTime);


                    MonsterData newMonsterData = SystemAPI.GetComponent<MonsterData>(Hit.entity);
                    newMonsterData.Health -= 100f * SystemAPI.Time.DeltaTime;

                    if (newMonsterData.Health > 0)
                    {
                        //Debug.Log(newMonsterData.Health);
                        SystemAPI.SetComponent<MonsterData>(Hit.entity, newMonsterData);
                    }
                    else
                    {
                        //Debug.Log("ded");
                        PhysicsCalls.DestroyPhysicsEntity(ecb, Hit.entity);
                    }

                }
                else
                {
                    Debug.DrawLine(Wtrans.ValueRO.Position, new Vector2(Wtrans.ValueRO.Position.x, Wtrans.ValueRO.Position.y) + keyDir, Color.green, SystemAPI.Time.DeltaTime);

                }

            }
        }

        /// way to remove if playback is not on loop

    }
}
