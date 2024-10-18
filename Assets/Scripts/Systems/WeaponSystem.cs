using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using Unity.Transforms;
using UnityEngine;
using MusicNamespace;
using Unity.Mathematics;
using Unity.Entities.UniversalDelegates;
using UnityEditor.Rendering;
using NUnit.Framework.Internal.Filters;

[UpdateInGroup(typeof(GameSimulationSystemGroup))]
[UpdateAfter(typeof(PhyResolutionSystem))]
public partial class WeaponSystem : SystemBase
{

    public static NativeArray<Entity> WeaponEntities;

    /// Useless ? active synth index always 0 ?
    //public static short activeSynthEntityindex;

    public float BeatCooldown;
    private float BeatProximity;
    private float BeatProximityThreshold;

    EntityCommandBuffer ECB;

    //temp -> put on component
    public static MusicUtils.MusicalMode mode;

    public static bool PlayPressed;
    public static bool PlayReleased;
    private bool PlayActive;
    private float PlayKey;
    private int PlayedKeyIndex;
    public Vector2 mousepos;
    private bool IsShooting;

    //private bool IsRecording;
    //private NativeList<PlaybackKey> PlayKeys;

    private NativeArray<float> Rythms;

    //private MaterialPropertyBlock _propertyBlock;

    protected override void OnCreate()
    {

        WeaponEntities = new NativeArray<Entity>(1,Allocator.Persistent);

        BeatCooldown = MusicUtils.BPM;
        BeatProximityThreshold = 0.08f;
        //temp -> put on component
        mode = MusicUtils.MusicalMode.Phrygian;

        ///todo
        ///remplace beatcooldown by note cooldown and make randomly 60 for noir ; 30 for croche ; 15 for double croche .... upon played
        Rythms = new NativeArray<float>(3, Allocator.Persistent)
        {
            [0] = 60,
            [1] = 30,
            [2] = 15,
            ///do more ...
        };

    }


    protected override void OnUpdate()
    {

        BeatCooldown -= MusicUtils.BPM * SystemAPI.Time.DeltaTime;

        ECB = World.GetOrCreateSystemManaged<BeginSimulationEntityCommandBufferSystem>().CreateCommandBuffer();

        BeatProximity = 0.5f - Mathf.Abs(0.5f - (BeatCooldown / MusicUtils.BPM));

        //Entity weapon_entity = AudioManager.ActiveWeapon_query.GetSingletonEntity();

        DynamicBuffer<SustainedKeyBufferData> SkeyBuffer = SystemAPI.GetBuffer<SustainedKeyBufferData>(WeaponEntities[AudioLayoutStorage.activeSynthIdx]);
        DynamicBuffer<ReleasedKeyBufferData> RkeyBuffer = SystemAPI.GetBuffer<ReleasedKeyBufferData>(WeaponEntities[AudioLayoutStorage.activeSynthIdx]);

        //TO MODIFY
        //var ActiveSynth = SystemAPI.GetComponent<SynthData>(WeaponEntities[AudioLayoutStorage.activeSynthIdx]);
        var ActiveSynth = AudioLayoutStorageHolder.audioLayoutStorage.SynthsData[AudioLayoutStorage.activeSynthIdx];
        //Debug.Log(ActiveSynth.ADSR.Sustain);

        ///BAD OPTI ?
        for (int i = 0; i < RkeyBuffer.Length; i++)
        {
            //float newDelta = RkeyBuffer[i].Delta + SystemAPI.Time.DeltaTime;
            if (RkeyBuffer[i].Delta + SystemAPI.Time.DeltaTime > ActiveSynth.ADSR.Release)
            {
                RkeyBuffer.RemoveAt(i);
            }
            else
            {
                //RkeyBuffer[i] = new ReleasedKeyBufferData { Delta = newDelta, Direction = RkeyBuffer[i].Direction, Phase = RkeyBuffer[i].Phase, currentAmplitude = 1-(newDelta / ActiveSynth.ADSR.Release) };
            }
        }
        //for (int i = 0; i < SkeyBuffer.Length; i++)
        //{
        //    float newDelta = SkeyBuffer[i].Delta + SystemAPI.Time.DeltaTime;
        //    SkeyBuffer[i] = new SustainedKeyBufferData { Delta = newDelta, Direction = SkeyBuffer[i].Direction, Phase = SkeyBuffer[i].Phase, currentAmplitude = newDelta<ActiveSynth.ADSR.Attack?newDelta/ ActiveSynth.ADSR.Attack:1f };
        //    //if (SkeyBuffer[i].Delta > ActiveSynth.ADSR.Attack+1.5f)
        //    //    Debug.Break();
        //}

        /// Move to Synth system all together ?
        foreach (var (Wtrans, trans) in SystemAPI.Query<RefRO<LocalToWorld>, RefRW<LocalTransform>>().WithAll<SynthData>())
        {
            ///OPTI :
            if (!IsShooting && !UIInputSystem.MouseOverUI)
            {

                mousepos = Camera.main.ScreenToWorldPoint(InputManager.mousePos);
                Vector2 dir = ((new Vector2(Wtrans.ValueRO.Position.x, Wtrans.ValueRO.Position.y)) - (mousepos)).normalized;

                trans.ValueRW.Rotation = Quaternion.Euler(0, 0, Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg);
            }

            if (PlayPressed && !UIInputSystem.MouseOverUI)
            {
                /// FOR SHOOTING TIED TO BEAT
                //if (BeatProximity < BeatProximityThreshold)
                {
                    Vector2 direction = mousepos - new Vector2(Wtrans.ValueRO.Position.x, Wtrans.ValueRO.Position.y);
                    float randian = Mathf.Abs(PhysicsUtilities.DirectionToRadians(direction));
                    int note = MusicUtils.radiansToNote(randian);

                    // 0 = not exist : 1 = in Skeybuffer
                    short noteExist = 0;
                    int i;

                    for (i = 0; i < RkeyBuffer.Length; i++)
                    {
                        int bufferNote = MusicUtils.radiansToNote(Mathf.Abs(PhysicsUtilities.DirectionToRadians(RkeyBuffer[i].DirLenght)));
                        if (bufferNote == note)
                        {
                            noteExist = 2;
                            break;
                        } 
                    }
                    if (noteExist==0)
                    {
                        //add to play buffer
                        PlayedKeyIndex = SkeyBuffer.Length;
                        SkeyBuffer.Add(new SustainedKeyBufferData { DirLenght = direction, EffectiveDirLenght = direction, Delta = 0, Phase = 0});
                    }
                    else
                    {
                        // can't happen ? 
                        if (noteExist == 1)
                        {
                            Debug.LogError("can't happen ?");
                        }
                        //noteExist == 2
                        else
                        {
                            //add to play buffer
                            PlayedKeyIndex = SkeyBuffer.Length;
                            SkeyBuffer.Add(new SustainedKeyBufferData { DirLenght = direction,EffectiveDirLenght = direction, Delta = 0 , Phase = RkeyBuffer[i].Phase, currentAmplitude = RkeyBuffer[i].currentAmplitude });
                            /// Set its delta to fade up to the new one ? (similar to synthorial)
                            RkeyBuffer.RemoveAt(i);
                        }
                    }

                    IsShooting = true;
                }
                PlayPressed = false;
            }
            if (PlayReleased)
            {

                if (ActiveSynth.ADSR.Sustain > 0 && SkeyBuffer.Length != 0)
                {
                    float newDeltaFactor;

                    if (SkeyBuffer[PlayedKeyIndex].Delta < ActiveSynth.ADSR.Attack)
                    {
                        if (ActiveSynth.ADSR.Attack == 0)
                            newDeltaFactor =  0f;
                        else
                            newDeltaFactor = 1-(SkeyBuffer[PlayedKeyIndex].Delta / ActiveSynth.ADSR.Attack);
                    }
                    else
                    {
                        if (ActiveSynth.ADSR.Decay == 0)
                            newDeltaFactor = 1-ActiveSynth.ADSR.Sustain;
                        else
                            newDeltaFactor = (1-ActiveSynth.ADSR.Sustain) * Mathf.Clamp(((SkeyBuffer[PlayedKeyIndex].Delta - ActiveSynth.ADSR.Attack) / ActiveSynth.ADSR.Decay), 0, 1f);
                        //Debug.Log(((SkeyBuffer[PlayedKeyIndex].Delta - ActiveSynth.ADSR.Attack) / ActiveSynth.ADSR.Decay));
                    }

                    //Debug.LogError("newDeltaFactor * ActiveSynth.ADSR.Release");
                    RkeyBuffer.Add(new ReleasedKeyBufferData { 
                        DirLenght = SkeyBuffer[PlayedKeyIndex].DirLenght, 
                        EffectiveDirLenght = SkeyBuffer[PlayedKeyIndex].EffectiveDirLenght,
                        //Delta = Mathf.Exp(4.6f*(newDeltaFactor-1)) * ActiveSynth.ADSR.Release, 
                        Delta = 0, 
                        Phase = SkeyBuffer[PlayedKeyIndex].Phase, 
                        currentAmplitude = SkeyBuffer[PlayedKeyIndex].currentAmplitude,
                        amplitudeAtRelease = SkeyBuffer[PlayedKeyIndex].currentAmplitude,
                        filter = SkeyBuffer[PlayedKeyIndex].filter,
                        cutoffEnvelopeAtRelease = SkeyBuffer[PlayedKeyIndex].filter.Cutoff - ActiveSynth.filter.Cutoff
                    });

                    SkeyBuffer.RemoveAt(PlayedKeyIndex);
                }
                IsShooting = false;
                PlayReleased = false;
            }


            /*remplace with generic shape*/
            ComponentLookup<CircleShapeData> ShapeComponentLookup = GetComponentLookup<CircleShapeData>(isReadOnly: true);

            KeysBuffer keysBuffer = new KeysBuffer { keyFrenquecies = new NativeArray<float>(12,Allocator.Temp), KeyNumber = new NativeArray<short>(1, Allocator.Temp) };
            keysBuffer.KeyNumber[0] = (short)(SkeyBuffer.Length);

            /// Damage processing + Delta/amplitude/filtering incrementing + audioBufferData filling
            for (int i = 0; i < SkeyBuffer.Length; i++)
            {
                keysBuffer.keyFrenquecies[i] = MusicUtils.DirectionToFrequency(SkeyBuffer[i].DirLenght);

                RayCastHit Hit = PhysicsCalls.RaycastNode(new Ray { Origin = new Vector2(Wtrans.ValueRO.Position.x, Wtrans.ValueRO.Position.y),DirLength=SkeyBuffer[i].DirLenght }, PhysicsUtilities.CollisionLayer.MonsterLayer, ShapeComponentLookup);

                float newDelta = SkeyBuffer[i].Delta + SystemAPI.Time.DeltaTime;
                float newCurrentAmplitude;
                Filter newFilter = new Filter(0,0);
                //float Cutoff = ActiveSynth.filter.Cutoff / (Mathf.Exp(* 5 - 5));

                newCurrentAmplitude = newDelta < ActiveSynth.ADSR.Attack ? 
                    newDelta / ActiveSynth.ADSR.Attack : 
                    Mathf.Max(ActiveSynth.ADSR.Sustain, 1 - ((newDelta - ActiveSynth.ADSR.Attack) / ActiveSynth.ADSR.Decay));
                
                newFilter.Cutoff = newDelta < ActiveSynth.filterADSR.Attack? 
                    ActiveSynth.filter.Cutoff + (ActiveSynth.filterEnvelopeAmount * (newDelta / ActiveSynth.filterADSR.Attack)):
                    ActiveSynth.filter.Cutoff + (ActiveSynth.filterEnvelopeAmount * (1 - (Mathf.Min(ActiveSynth.filterADSR.Attack + ActiveSynth.filterADSR.Decay, newDelta) - ActiveSynth.filterADSR.Attack) / ActiveSynth.filterADSR.Decay) * (1 - ActiveSynth.filterADSR.Sustain) + (ActiveSynth.filterADSR.Sustain* ActiveSynth.filterEnvelopeAmount));

                //if (newDelta < ActiveSynth.ADSR.Attack)
                //{
                //    newCurrentAmplitude = newDelta / ActiveSynth.ADSR.Attack;
                //}
                //else
                //{
                //    newCurrentAmplitude = Mathf.Max(ActiveSynth.ADSR.Sustain, 1 - ((newDelta - ActiveSynth.ADSR.Attack) / ActiveSynth.ADSR.Decay));
                // }
                //if (newDelta < ActiveSynth.filterADSR.Attack)
                //{
                //    newFilter.Cutoff = ActiveSynth.filter.Cutoff + (ActiveSynth.filterEnvelopeAmount * (newDelta / ActiveSynth.filterADSR.Attack));
                //}
                //else
                //{
                //    newFilter.Cutoff = ActiveSynth.filter.Cutoff + (ActiveSynth.filterEnvelopeAmount * (1 - (Mathf.Min(ActiveSynth.filterADSR.Attack + ActiveSynth.filterADSR.Decay, newDelta) - ActiveSynth.filterADSR.Attack) / ActiveSynth.filterADSR.Decay) * (1 - ActiveSynth.filterADSR.Sustain) + ActiveSynth.filterADSR.Sustain);
                //}



                if (Hit.entity != Entity.Null)
                {
                    //Debug.Log(Hit.distance);
                    // hit line
                    //Debug.DrawLine(Wtrans.ValueRO.Position, new Vector2(Wtrans.ValueRO.Position.x, Wtrans.ValueRO.Position.y) + (SkeyBuffer[i].DirLenght.normalized * Hit.distance), Color.white, SystemAPI.Time.DeltaTime);
                    
                    SkeyBuffer[i] = new SustainedKeyBufferData {
                        Delta = newDelta, 
                        DirLenght = SkeyBuffer[i].DirLenght,
                        EffectiveDirLenght = SkeyBuffer[i].DirLenght * (Hit.distance/ SkeyBuffer[i].DirLenght.magnitude), 
                        Phase = SkeyBuffer[i].Phase,
                        currentAmplitude = newCurrentAmplitude,
                        filter = newFilter
                    };


                    MonsterData newMonsterData = SystemAPI.GetComponent<MonsterData>(Hit.entity);
                    newMonsterData.Health -= 3f* SystemAPI.Time.DeltaTime;

                    if (newMonsterData.Health > 0)
                    {
                        //Debug.Log(newMonsterData.Health);
                        SystemAPI.SetComponent<MonsterData>(Hit.entity, newMonsterData);
                    }
                    else
                    {
                        //Debug.Log("ded");
                        PhysicsCalls.DestroyPhysicsEntity(ECB, Hit.entity);
                    }

                }
                else
                {

                    SkeyBuffer[i] = new SustainedKeyBufferData { 
                        Delta = SkeyBuffer[i].Delta + SystemAPI.Time.DeltaTime, 
                        DirLenght = SkeyBuffer[i].DirLenght, EffectiveDirLenght = SkeyBuffer[i].DirLenght, 
                        Phase = SkeyBuffer[i].Phase,
                        currentAmplitude = newCurrentAmplitude,
                        filter = newFilter
                    };
                    //Debug.Log(Mathf.Max(ActiveSynth.ADSR.Sustain, 1 - ((newDelta - ActiveSynth.ADSR.Attack) / ActiveSynth.ADSR.Decay)));
                    
                    //Debug.DrawLine(Wtrans.ValueRO.Position, new Vector2(Wtrans.ValueRO.Position.x, Wtrans.ValueRO.Position.y) + SkeyBuffer[i].DirLenght, Color.white, SystemAPI.Time.DeltaTime);

                }

            }
            for (int i = 0; i < RkeyBuffer.Length; i++)
            {

                float newDelta = RkeyBuffer[i].Delta + SystemAPI.Time.DeltaTime;
                //float Cutoff = ActiveSynth.filter.Cutoff / (Mathf.Exp(ActiveSynth.filter.Cutoff * 5 - 5) * ActiveSynth.filter.Cutoff);
                
                Filter newFilter = new Filter
                {
                    Cutoff = ActiveSynth.filter.Cutoff + ((RkeyBuffer[i].cutoffEnvelopeAtRelease) * (1- Mathf.Min(ActiveSynth.filterADSR.Release, newDelta) /ActiveSynth.filterADSR.Release)),
                    Resonance = 0
                };

                if (newDelta > ActiveSynth.ADSR.Release)
                {
                    RkeyBuffer.RemoveAt(i);
                    i--;
                    continue;
                }

                RayCastHit Hit = PhysicsCalls.RaycastNode(new Ray { Origin = new Vector2(Wtrans.ValueRO.Position.x, Wtrans.ValueRO.Position.y), DirLength = RkeyBuffer[i].DirLenght }, PhysicsUtilities.CollisionLayer.MonsterLayer, ShapeComponentLookup);

                if (Hit.entity != Entity.Null)
                {
                    //Debug.Log(Hit.distance);
                    // hit line
                    //Debug.DrawLine(Wtrans.ValueRO.Position, new Vector2(Wtrans.ValueRO.Position.x, Wtrans.ValueRO.Position.y) + (RkeyBuffer[i].DirLenght.normalized * Hit.distance), Color.red, SystemAPI.Time.DeltaTime);

                    RkeyBuffer[i] = new ReleasedKeyBufferData
                    {
                        Delta = newDelta,
                        DirLenght = RkeyBuffer[i].DirLenght,
                        EffectiveDirLenght = RkeyBuffer[i].DirLenght * (Hit.distance / RkeyBuffer[i].DirLenght.magnitude),
                        Phase = RkeyBuffer[i].Phase,
                        currentAmplitude = RkeyBuffer[i].amplitudeAtRelease * Mathf.Exp(-4.6f * RkeyBuffer[i].amplitudeAtRelease * newDelta / ActiveSynth.ADSR.Release),
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
                        PhysicsCalls.DestroyPhysicsEntity(ECB, Hit.entity);
                    }

                }
                else
                {

                    RkeyBuffer[i] = new ReleasedKeyBufferData { 
                        Delta = newDelta, DirLenght = RkeyBuffer[i].DirLenght, 
                        EffectiveDirLenght = RkeyBuffer[i].DirLenght, 
                        Phase = RkeyBuffer[i].Phase,
                        currentAmplitude = RkeyBuffer[i].amplitudeAtRelease*Mathf.Exp(-4.6f * RkeyBuffer[i].amplitudeAtRelease * newDelta / ActiveSynth.ADSR.Release),
                        //currentAmplitude = RkeyBuffer[i].amplitudeAtRelease * (1 - newDelta / ActiveSynth.ADSR.Release),
                        amplitudeAtRelease = RkeyBuffer[i].amplitudeAtRelease,
                        filter = newFilter,
                        cutoffEnvelopeAtRelease = RkeyBuffer[i].cutoffEnvelopeAtRelease,
                    };
                    //Debug.Log(RkeyBuffer[i].currentAmplitude);

                    //Debug.DrawLine(Wtrans.ValueRO.Position, new Vector2(Wtrans.ValueRO.Position.x, Wtrans.ValueRO.Position.y) + RkeyBuffer[i].DirLenght, new Color(1,1,1,1), SystemAPI.Time.DeltaTime);

                    //Debug.Log(RkeyBuffer[i].filter.Cutoff);
                }



            }

            //if (keysBuffer.keyFrenquecies[0] == 0 && keysBuffer.KeyNumber[0]!=0)
            //    Debug.Log("tetetetet");
            //if(SkeyBuffer.Length !=0)
            //    Debug.LogError(SkeyBuffer[0].Direction);
            //Debug.LogError(keysBuffer.keyFrenquecies[0]);

            /// Write to the audioRingBuffer to be played on the audio thread
            if (!AudioGenerator.audioRingBuffer.IsFull)
                AudioGenerator.audioRingBuffer.Write(keysBuffer);

            //Debug.LogError(AudioGenerator.audioRingBuffer.Read().KeyNumber[0]);


        }


    }

}