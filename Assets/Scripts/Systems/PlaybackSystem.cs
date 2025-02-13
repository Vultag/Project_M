using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using NUnit.Framework.Internal.Filters;
using Unity.Collections;
using Unity.Entities;
using Unity.Entities.UniversalDelegates;
using Unity.Transforms;
using UnityEngine;

public partial struct PlaybackSystem : ISystem
{

    /// Used to test keys upon removal to see if they had time to be addded;
    float PreviousFrameDelta;

    void OnCreate(ref SystemState state)
    {

    }


    void OnUpdate(ref SystemState state)
    {

        EntityCommandBuffer ecb = SystemAPI.GetSingleton<BeginSimulationEntityCommandBufferSystem.Singleton>().CreateCommandBuffer(state.WorldUnmanaged);

        ComponentLookup<CircleShapeData> ShapeComponentLookup = SystemAPI.GetComponentLookup<CircleShapeData>(isReadOnly: true);

        NativeQueue<Vector2> KeyDirQueue = new NativeQueue<Vector2>(Allocator.Temp);

        foreach (var (playback_data, Wtrans, entity) in SystemAPI.Query<RefRW<PlaybackData>, RefRO<LocalToWorld>>().WithEntityAccess().WithAll<SynthData>())
        {

            var ActiveSynth = AudioLayoutStorageHolder.audioLayoutStorage.SynthsData[playback_data.ValueRO.SynthIndex];

            DynamicBuffer<PlaybackSustainedKeyBufferData> SkeyBuffer = SystemAPI.GetBuffer<PlaybackSustainedKeyBufferData>(entity);
            DynamicBuffer<PlaybackReleasedKeyBufferData> RkeyBuffer = SystemAPI.GetBuffer<PlaybackReleasedKeyBufferData>(entity);

            int playbackKeyIndex = playback_data.ValueRW.PlaybackKeyIndex;

            /// OPTI ?
            while (playbackKeyIndex < AudioLayoutStorageHolder.audioLayoutStorage.PlaybackAudioBundles[playback_data.ValueRO.SynthIndex].PlaybackKeys.Length
                && AudioLayoutStorageHolder.audioLayoutStorage.PlaybackAudioBundles[playback_data.ValueRO.SynthIndex].PlaybackKeys[playbackKeyIndex].time < playback_data.ValueRO.PlaybackTime)
            {
                PlaybackKey playbackkey = AudioLayoutStorageHolder.audioLayoutStorage.PlaybackAudioBundles[playback_data.ValueRO.SynthIndex].PlaybackKeys[playbackKeyIndex];
                /// went past the latest active key
                if (playbackkey.time + playbackkey.lenght < playback_data.ValueRO.PlaybackTime)
                {
                    /// Make sure the key had time to activate if released
                    if (playbackkey.time< playback_data.ValueRO.PlaybackTime- PreviousFrameDelta)
                    {
                        if (!playbackkey.dragged)
                            RkeyBuffer.Add(new PlaybackReleasedKeyBufferData
                            {
                                DirLenght = SkeyBuffer[SkeyBuffer.Length - 1].DirLenght,
                                EffectiveDirLenght = SkeyBuffer[SkeyBuffer.Length - 1].EffectiveDirLenght,
                                Delta = 0,
                                Phase = SkeyBuffer[SkeyBuffer.Length - 1].Phase,
                                currentAmplitude = SkeyBuffer[SkeyBuffer.Length - 1].currentAmplitude,
                                amplitudeAtRelease = SkeyBuffer[SkeyBuffer.Length - 1].currentAmplitude,
                                filter = SkeyBuffer[SkeyBuffer.Length - 1].filter,
                                cutoffEnvelopeAtRelease = SkeyBuffer[SkeyBuffer.Length - 1].filter.Cutoff - ActiveSynth.filter.Cutoff
                            });
                        SkeyBuffer.RemoveAt(SkeyBuffer.Length - 1);
                    }
                    else
                        playback_data.ValueRW.KeysPlayed++;
                    playbackKeyIndex++;
                    playback_data.ValueRW.PlaybackKeyIndex = playbackKeyIndex;
                }
                else
                {
                    playbackKeyIndex++;
                }
                if (playbackKeyIndex >= AudioLayoutStorageHolder.audioLayoutStorage.PlaybackAudioBundles[playback_data.ValueRO.SynthIndex].PlaybackKeys.Length)
                    break;
      

            }

            for (int i = playback_data.ValueRO.KeysPlayed; i < playbackKeyIndex; i++)
            {
                PlaybackKey playbackKey = AudioLayoutStorageHolder.audioLayoutStorage.PlaybackAudioBundles[playback_data.ValueRO.SynthIndex].PlaybackKeys[i];

                /// Check if the the legato glide over a released key
                /// REMOVED AS RAYS DONT CURRENTLY LEAVE RELEASEKEY IN LEGATO
                //if (playbackKey.keyCutIdx < short.MaxValue)
                //{
                //    //Debug.Log(RkeyBuffer.Length);
                //    //Debug.LogError(playbackKey.keyCutIdx);
                //    RkeyBuffer.RemoveAt(playbackKey.keyCutIdx);
                //}
                Vector2 dirLenght = playbackKey.dir;

                SkeyBuffer.Add(new PlaybackSustainedKeyBufferData 
                { 
                    DirLenght = dirLenght,
                    StartDirLenght = playbackKey.startDir,
                    EffectiveDirLenght = dirLenght, 
                    Phase = 0, 
                });
                playback_data.ValueRW.KeysPlayed++;
                //Debug.Log(RkeyBuffer.Length);
            }

            if (playback_data.ValueRW.PlaybackTime + SystemAPI.Time.DeltaTime > AudioLayoutStorageHolder.audioLayoutStorage.PlaybackAudioBundles[playback_data.ValueRO.SynthIndex].PlaybackDuration)
            {
                //Debug.Log("stop PB");
                //// HERE 
                //AudioLayoutStorageHolder.audioLayoutStorage.WriteActivation(playback_data.ValueRO.SynthIndex, false);
                //ecb.RemoveComponent<PlaybackData>(entity);

               // if (AudioLayoutStorageHolder.audioLayoutStorage.PlaybackAudioBundles[playback_data.ValueRO.PlaybackIndex].IsLooping)
                {

                    //Debug.Log("restart");
                    /// moved to belt
                    //AudioLayoutStorageHolder.audioLayoutStorage.PlaybackContextResetRequired.Enqueue(playback_data.ValueRO.SynthIndex);
                    
                    playback_data.ValueRW.PlaybackTime = (playback_data.ValueRO.PlaybackTime + SystemAPI.Time.DeltaTime) - AudioLayoutStorageHolder.audioLayoutStorage.PlaybackAudioBundles[playback_data.ValueRO.SynthIndex].PlaybackDuration;
                    playback_data.ValueRW.PlaybackKeyIndex = 0;
                    playback_data.ValueRW.KeysPlayed = 0;
                    SkeyBuffer.Clear();
                    RkeyBuffer.Clear();
                }
            }
            else
                playback_data.ValueRW.PlaybackTime += SystemAPI.Time.DeltaTime;
            PreviousFrameDelta = SystemAPI.Time.DeltaTime;



            /// Damage processing + Delta/amplitude incrementing
            for (int i = 0; i < SkeyBuffer.Length; i++)
            {

                //Vector2 dirLenght = ActiveSynth.Legato ? direction : SkeyBuffer[i].DirLenght;
                Vector2 dirLenght = PhysicsUtilities.Rotatelerp(SkeyBuffer[i].StartDirLenght, SkeyBuffer[i].DirLenght, -Mathf.Log(ActiveSynth.Portomento / 3) * 0.01f + 0.03f);
              
                RayCastHit Hit = PhysicsCalls.RaycastNode(new Ray { Origin = new Vector2(Wtrans.ValueRO.Position.x, Wtrans.ValueRO.Position.y), DirLength = SkeyBuffer[i].DirLenght }, PhysicsUtilities.CollisionLayer.MonsterLayer, ShapeComponentLookup);

                float newDelta = SkeyBuffer[i].Delta + SystemAPI.Time.DeltaTime;
                Filter newFilter = new Filter(0, 0);

                newFilter.Cutoff = newDelta < ActiveSynth.filterADSR.Attack ?
                 ActiveSynth.filter.Cutoff + (ActiveSynth.filterEnvelopeAmount * (newDelta / ActiveSynth.filterADSR.Attack)) :
                 ActiveSynth.filter.Cutoff + (ActiveSynth.filterEnvelopeAmount * (1 - (Mathf.Min(ActiveSynth.filterADSR.Attack + ActiveSynth.filterADSR.Decay, newDelta) - ActiveSynth.filterADSR.Attack) / ActiveSynth.filterADSR.Decay) * (1 - ActiveSynth.filterADSR.Sustain) + (ActiveSynth.filterADSR.Sustain * ActiveSynth.filterEnvelopeAmount));


                if (Hit.entity != Entity.Null)
                {
                    //Debug.Log(Hit.distance);
                    // hit line
                    //Debug.DrawLine(Wtrans.ValueRO.Position, new Vector2(Wtrans.ValueRO.Position.x, Wtrans.ValueRO.Position.y) + (SkeyBuffer[i].DirLenght.normalized * Hit.distance), Color.white, SystemAPI.Time.DeltaTime);

                    SkeyBuffer[i] = new PlaybackSustainedKeyBufferData
                    {
                        Delta = newDelta,
                        DirLenght = SkeyBuffer[i].DirLenght,
                        StartDirLenght = dirLenght,
                        EffectiveDirLenght = dirLenght * (Hit.distance / SkeyBuffer[i].DirLenght.magnitude),
                        Phase = SkeyBuffer[i].Phase,
                        currentAmplitude = newDelta < ActiveSynth.ADSR.Attack ?
                        newDelta / ActiveSynth.ADSR.Attack :
                        Mathf.Max(ActiveSynth.ADSR.Sustain, 1 - ((newDelta - ActiveSynth.ADSR.Attack) / ActiveSynth.ADSR.Decay)),
                        filter = newFilter,
                    };

                    MonsterData newMonsterData = SystemAPI.GetComponent<MonsterData>(Hit.entity);
                    newMonsterData.Health -= 3f * SystemAPI.Time.DeltaTime;

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
                    //Debug.Log(1);
                    SkeyBuffer[i] = new PlaybackSustainedKeyBufferData
                    {
                        Delta = SkeyBuffer[i].Delta + SystemAPI.Time.DeltaTime,
                        DirLenght = SkeyBuffer[i].DirLenght,
                        StartDirLenght = dirLenght,
                        EffectiveDirLenght = dirLenght,
                        Phase = SkeyBuffer[i].Phase,
                        currentAmplitude = newDelta < ActiveSynth.ADSR.Attack ? 
                        newDelta / ActiveSynth.ADSR.Attack : 
                        Mathf.Max(ActiveSynth.ADSR.Sustain, 1 - ((newDelta - ActiveSynth.ADSR.Attack) / ActiveSynth.ADSR.Decay)),
                        filter = newFilter,
                    };

                    //Debug.DrawLine(Wtrans.ValueRO.Position, new Vector2(Wtrans.ValueRO.Position.x, Wtrans.ValueRO.Position.y) + dirLenght, Color.white, SystemAPI.Time.DeltaTime);

                }

            }
            for (int i = 0; i < RkeyBuffer.Length; i++)
            {

                float newDelta = RkeyBuffer[i].Delta + SystemAPI.Time.DeltaTime;
                if (newDelta > ActiveSynth.ADSR.Release)
                {
                    RkeyBuffer.RemoveAt(i);
                    i--;
                    continue;
                }

                Filter newFilter = new Filter
                {
                    Cutoff = ActiveSynth.filter.Cutoff + ((RkeyBuffer[i].cutoffEnvelopeAtRelease) * (1 - Mathf.Min(ActiveSynth.filterADSR.Release, newDelta) / ActiveSynth.filterADSR.Release)),
                    Resonance = 0
                };

                RayCastHit Hit = PhysicsCalls.RaycastNode(new Ray { Origin = new Vector2(Wtrans.ValueRO.Position.x, Wtrans.ValueRO.Position.y), DirLength = RkeyBuffer[i].DirLenght }, PhysicsUtilities.CollisionLayer.MonsterLayer, ShapeComponentLookup);

                if (Hit.entity != Entity.Null)
                {
                    //Debug.Log(Hit.distance);
                    // hit line
                    //Debug.DrawLine(Wtrans.ValueRO.Position, new Vector2(Wtrans.ValueRO.Position.x, Wtrans.ValueRO.Position.y) + (RkeyBuffer[i].DirLenght.normalized * Hit.distance), Color.red, SystemAPI.Time.DeltaTime);

                    float amplitudefactor = RkeyBuffer[i].amplitudeAtRelease * newDelta / ActiveSynth.ADSR.Release;
                    RkeyBuffer[i] = new PlaybackReleasedKeyBufferData
                    {
                        Delta = newDelta,
                        DirLenght = RkeyBuffer[i].DirLenght,
                        EffectiveDirLenght = RkeyBuffer[i].DirLenght * (Hit.distance / RkeyBuffer[i].DirLenght.magnitude),
                        Phase = RkeyBuffer[i].Phase,
                        currentAmplitude = RkeyBuffer[i].amplitudeAtRelease * Mathf.Exp(-1.6f * amplitudefactor) * (1 - amplitudefactor),
                        amplitudeAtRelease = RkeyBuffer[i].amplitudeAtRelease,
                        filter = newFilter,
                        cutoffEnvelopeAtRelease = RkeyBuffer[i].cutoffEnvelopeAtRelease,
                    };

                    MonsterData newMonsterData = SystemAPI.GetComponent<MonsterData>(Hit.entity);
                    newMonsterData.Health -= 3f * SystemAPI.Time.DeltaTime;

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
                   
                    RkeyBuffer[i] = new PlaybackReleasedKeyBufferData
                    {
                        Delta = newDelta,
                        DirLenght = RkeyBuffer[i].DirLenght,
                        EffectiveDirLenght = RkeyBuffer[i].DirLenght,
                        Phase = RkeyBuffer[i].Phase,
                        currentAmplitude = RkeyBuffer[i].amplitudeAtRelease * Mathf.Exp(-4.6f * RkeyBuffer[i].amplitudeAtRelease * newDelta / ActiveSynth.ADSR.Release),
                        amplitudeAtRelease = RkeyBuffer[i].amplitudeAtRelease,
                        filter = newFilter,
                        cutoffEnvelopeAtRelease = RkeyBuffer[i].cutoffEnvelopeAtRelease,
                    };

                    //Debug.DrawLine(Wtrans.ValueRO.Position, new Vector2(Wtrans.ValueRO.Position.x, Wtrans.ValueRO.Position.y) + RkeyBuffer[i].DirLenght, Color.red, SystemAPI.Time.DeltaTime);

                }
            }

        }


        /// way to remove if playback is not on loop

    }
}
