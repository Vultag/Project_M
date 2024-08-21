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

    private NativeArray<float> Rythms;


    protected override void OnCreate()
    {
        WeaponEntities = new NativeArray<Entity>(1,Allocator.Persistent);

        ///GET ACTIVE SYNTH ENTUITY
        //activeSynthEntity = SystemAPI.GetComponent<PlayerData>(SystemAPI.GetSingletonEntity<PlayerData>()).ActiveCanon;


        BeatCooldown = MusicUtils.BPM;
        BeatProximityThreshold = 0.08f;
        //temp -> put on component
        mode = MusicUtils.MusicalMode.Lydian;

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


        DynamicBuffer<SustainedKeyBufferData> SkeyBuffer = SystemAPI.GetBuffer<SustainedKeyBufferData>(WeaponEntities[0]);
        DynamicBuffer<ReleasedKeyBufferData> RkeyBuffer = SystemAPI.GetBuffer<ReleasedKeyBufferData>(WeaponEntities[0]);

        //TO MODIFY
        var ActiveSynth = SystemAPI.GetComponent<SynthData>(WeaponEntities[0]);

        ///BAD OPTI ?
        for (int i = 0; i < RkeyBuffer.Length; i++)
        {
            float newDelta = RkeyBuffer[i].Delta + SystemAPI.Time.DeltaTime;
            if (newDelta > ActiveSynth.ADSR.Release)
            {
                RkeyBuffer.RemoveAt(i);
            }
            else
            {
                RkeyBuffer[i] = new ReleasedKeyBufferData { Delta = newDelta, Direction = RkeyBuffer[i].Direction, Phase = RkeyBuffer[i].Phase, currentAmplitude = RkeyBuffer[i].currentAmplitude};
            }
        }
        for (int i = 0; i < SkeyBuffer.Length; i++)
        {
            SkeyBuffer[i] = new SustainedKeyBufferData { Delta = SkeyBuffer[i].Delta + SystemAPI.Time.DeltaTime, Direction = SkeyBuffer[i].Direction, Phase = SkeyBuffer[i].Phase, currentAmplitude = SkeyBuffer[i].currentAmplitude };
            //if (SkeyBuffer[i].Delta > ActiveSynth.ADSR.Attack+1.5f)
            //    Debug.Break();
        }

        /// Move to Synth system all together ?
        foreach (var (Wtrans, trans, synth) in SystemAPI.Query<RefRO<LocalToWorld>, RefRW<LocalTransform>, RefRO<SynthData>>())
        {
            ///OPTI :
            if (!IsShooting && !UIInputSystem.MouseOverUI)
            {

                mousepos = Camera.main.ScreenToWorldPoint(PlayerSystem.mousePos);
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
                        int bufferNote = MusicUtils.radiansToNote(Mathf.Abs(PhysicsUtilities.DirectionToRadians(RkeyBuffer[i].Direction)));
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
                        SkeyBuffer.Add(new SustainedKeyBufferData { Direction = direction, Delta = 0, Phase = 0 });
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
                            SkeyBuffer.Add(new SustainedKeyBufferData { Direction = direction, Delta = 0 , Phase = RkeyBuffer[i].Phase, currentAmplitude = RkeyBuffer[i].currentAmplitude });
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

                if (synth.ValueRO.ADSR.Sustain > 0 && SkeyBuffer.Length != 0)
                {
                    float newDeltaFactor;

                    if (SkeyBuffer[PlayedKeyIndex].Delta < synth.ValueRO.ADSR.Attack)
                    {
                        if (synth.ValueRO.ADSR.Attack == 0)
                            newDeltaFactor =  0f;
                        else
                            newDeltaFactor = 1-Mathf.Clamp((SkeyBuffer[PlayedKeyIndex].Delta / synth.ValueRO.ADSR.Attack), 0, 1f);
                    }
                    else
                    {
                        if (synth.ValueRO.ADSR.Decay == 0)
                            newDeltaFactor = synth.ValueRO.ADSR.Sustain;
                        else
                            newDeltaFactor = (1 - synth.ValueRO.ADSR.Sustain) * Mathf.Clamp(((SkeyBuffer[PlayedKeyIndex].Delta - synth.ValueRO.ADSR.Attack) / synth.ValueRO.ADSR.Decay), 0, 1f);//1 - Mathf.Clamp(((SkeyBuffer[PlayedKeyIndex].Delta - synth.ValueRO.ADSR.Attack) / synth.ValueRO.ADSR.Decay * synth.ValueRO.ADSR.Sustain), 0, 1f);
                    }

                    RkeyBuffer.Add(new ReleasedKeyBufferData { Direction = SkeyBuffer[PlayedKeyIndex].Direction, Delta = newDeltaFactor * synth.ValueRO.ADSR.Release, Phase = SkeyBuffer[PlayedKeyIndex].Phase, currentAmplitude = SkeyBuffer[PlayedKeyIndex].currentAmplitude });
                    SkeyBuffer.RemoveAt(PlayedKeyIndex);
                }
                IsShooting = false;
                PlayReleased = false;
            }


            /*remplace with generic shape*/
            ComponentLookup<CircleShapeData> ShapeComponentLookup = GetComponentLookup<CircleShapeData>(isReadOnly: true);

            KeysBuffer keysBuffer = new KeysBuffer { keyFrenquecies = new NativeArray<float>(12,Allocator.Temp), KeyNumber = new NativeArray<short>(1, Allocator.Temp) };
            keysBuffer.KeyNumber[0] = (short)(SkeyBuffer.Length);

            /// Damage processing + audioBufferData filling
            for (int i = 0; i < SkeyBuffer.Length; i++)
            {
                keysBuffer.keyFrenquecies[i] = MusicUtils.DirectionToFrequency(SkeyBuffer[i].Direction);

                RayCastHit Hit = PhysicsCalls.RaycastNode(new Ray { Origin = new Vector2(Wtrans.ValueRO.Position.x, Wtrans.ValueRO.Position.y),DirLength=SkeyBuffer[i].Direction }, PhysicsUtilities.CollisionLayer.MonsterLayer, ShapeComponentLookup);

                if (Hit.entity != Entity.Null)
                {
                    //Debug.Log(Hit.distance);
                    ///Debug.DrawLine(Wtrans.ValueRO.Position, new Vector2(Wtrans.ValueRO.Position.x, Wtrans.ValueRO.Position.y) + SkeyBuffer[i].Direction, Color.green, SystemAPI.Time.DeltaTime);

                    //Debug.DrawLine(Wtrans.ValueRO.Position, SystemAPI.GetComponent<CircleShapeData>(Hit.entity).Position, Color.red, SystemAPI.Time.DeltaTime);
                    Debug.DrawLine(Wtrans.ValueRO.Position, new Vector2(Wtrans.ValueRO.Position.x, Wtrans.ValueRO.Position.y) + (SkeyBuffer[i].Direction.normalized * Hit.distance), Color.red, SystemAPI.Time.DeltaTime);


                    MonsterData newMonsterData = SystemAPI.GetComponent<MonsterData>(Hit.entity);
                    newMonsterData.Health -= 100f* SystemAPI.Time.DeltaTime;

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
                    Debug.DrawLine(Wtrans.ValueRO.Position, new Vector2(Wtrans.ValueRO.Position.x, Wtrans.ValueRO.Position.y) + SkeyBuffer[i].Direction, Color.green, SystemAPI.Time.DeltaTime);

                }

            }
            for (int i = 0; i < RkeyBuffer.Length; i++)
            {
                // relased key damage here
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