using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using Unity.Transforms;
using UnityEngine;
using MusicNamespace;
using Unity.Mathematics;
using Unity.Entities.UniversalDelegates;
using static UnityEngine.EventSystems.EventTrigger;
using UnityEditor.Rendering;

[UpdateInGroup(typeof(GameSimulationSystemGroup))]
//[UpdateAfter(typeof(PhyResolutionSystem))]
public partial class WeaponSystem : SystemBase
{

    public static NativeArray<Entity> WeaponEntities;

    /// Useless ? active synth index always 0 ?
    //public static short activeSynthEntityindex;

    /// Moved to input manager
    //public float BeatCooldown;
    //private float BeatProximity;
    //private float BeatProximityThreshold;

    EntityCommandBuffer ECB;

    //temp -> put on component
    public static MusicUtils.MusicalMode mode;

    public static bool PlayPressed;
    public static bool PlayReleased;
    private int PlayedKeyIndex;
    public Vector2 mousepos;
    private bool IsShooting;

    //private bool IsRecording;
    //private NativeList<PlaybackKey> PlayKeys;

    private Vector2 mouseDirection;
    private NativeArray<float> Rythms;
    public static Vector2 GideReferenceDirection;
    /// 0 = legato inactive;
    private float activeLegatoFz;

    //private MaterialPropertyBlock _propertyBlock;

    protected override void OnCreate()
    {

        WeaponEntities = new NativeArray<Entity>(1,Allocator.Persistent);

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

        RequireForUpdate<ControledWeaponTag>();

    }


    protected override void OnUpdate()
    {

        //BeatCooldown -= MusicUtils.BPM * SystemAPI.Time.DeltaTime;

        ECB = World.GetOrCreateSystemManaged<BeginSimulationEntityCommandBufferSystem>().CreateCommandBuffer();

        /// Moved to input manager
        //BeatProximityThreshold = (0.2f * 4.1f) * Mathf.Min((1.5f*MusicUtils.BPM)/60f,1);
        //float normalizedProximity = ((float)SystemAPI.Time.ElapsedTime % (60f / (MusicUtils.BPM*4))) / (60f / (MusicUtils.BPM * 4));
        //BeatProximity = 1-Mathf.Abs((normalizedProximity-0.5f)*2);

        //Debug.Log(BeatProximity);
        //Debug.DrawLine(Vector3.zero,new Vector3(0, BeatProximity*10,0));
        //Debug.Log(WeaponEntities[AudioLayoutStorage.activeSynthIdx]);

        //Debug.Break();

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
        }

        mousepos = Camera.main.ScreenToWorldPoint(InputManager.mousePos);


        float normalizedProximity = ((float)MusicUtils.time % (60f / (MusicUtils.BPM * 4))) / (60f / (MusicUtils.BPM * 4));
        float BeatProximity = 1 - Mathf.Abs((normalizedProximity - 0.5f) * 2);
        //Debug.LogWarning(BeatProximity);

        /// Move to Synth system all together ?
        foreach (var (Wtrans, trans,entity) in SystemAPI.Query<RefRO<LocalToWorld>, RefRW<LocalTransform>>().WithAll<ControledWeaponTag>().WithEntityAccess())
        {

            //Debug.Log(BeatProximity);
            //Debug.DrawLine(new Vector3(5, 0, 0), new Vector3(5, BeatProximity * 10, 0), Color.red);
            //Debug.DrawLine(new Vector3(Wtrans.ValueRO.Position.x, Wtrans.ValueRO.Position.y, 0), new Vector3(5, BeatProximity * 10, 0), Color.red);

            var parentEntity = SystemAPI.GetComponent<Parent>(entity).Value;
            var parentTransform = SystemAPI.GetComponent<LocalToWorld>(parentEntity);

            mouseDirection = mousepos - new Vector2(Wtrans.ValueRO.Position.x, Wtrans.ValueRO.Position.y);

            if (!UIInput.MouseOverUI)
            {
                if (activeLegatoFz == 0)
                {
                    if (!IsShooting)
                    {
                        var newLocalRot = Quaternion.Euler(0, 0, Mathf.Atan2(-mouseDirection.y, -mouseDirection.x) * Mathf.Rad2Deg);
                        ///local to world set
                        trans.ValueRW.Rotation = math.mul(math.inverse(parentTransform.Rotation), newLocalRot);
                    }
                }
                else
                {
                    Vector2 worldMouseDir = mousepos - new Vector2(Wtrans.ValueRO.Position.x, Wtrans.ValueRO.Position.y);
             
                    var LocalRot = Quaternion.Euler(0, 0, Mathf.Atan2(-worldMouseDir.y, -worldMouseDir.x) * Mathf.Rad2Deg);
                    var localMouseDirection = math.mul(math.inverse(parentTransform.Rotation), new float3(mouseDirection.x, mouseDirection.y, 0)).xy;
                    ///local to world set
                    trans.ValueRW.Rotation = math.mul(math.inverse(parentTransform.Rotation), LocalRot);
      
                    float currentFz = MusicUtils.DirectionToFrequency(localMouseDirection);
                    if (currentFz != activeLegatoFz)
                    { 
                        if (BeatProximity < InputManager.BeatProximityThreshold)
                        { mouseDirection = worldMouseDir; }
                    }
                    else
                    {
                        mouseDirection = worldMouseDir;
                    }
                }
            }


            Vector2 weaponDirLenght = math.mul(math.inverse(parentTransform.Rotation), new float3(mouseDirection.x, mouseDirection.y, 0)).xy;

            //weaponDirLenght = math.mul(trans.ValueRO.Rotation, new float3(mouseDirection.x, 0, mouseDirection.y)).xz;
            //weaponDirLenght = mouseDirection;
            //Vector2 weaponDirLenght = mouseDirection;
            //trans.ValueRW.Rotation = newLocalRot;

            //Debug.LogError(weaponDirLenght);

            if (BeatProximity< InputManager.BeatProximityThreshold && InputManager.CanPressKey)
            {    ///OPTI :
            
                if (activeLegatoFz != 0)
                {
                    var localMouseDirection = math.mul(math.inverse(parentTransform.Rotation), new float3(mouseDirection.x, mouseDirection.y, 0)).xy;
                    float currentFz = MusicUtils.DirectionToFrequency(localMouseDirection);
                    if (currentFz != activeLegatoFz)
                    {
                        /// If there is a playback recording I has to update before to not block it
                        InputManager.CanPressKey = false;
                        //Debug.LogWarning("set false");
                        //if (OnBeat)
                        {
                            /// Check if the the legato glide over a released key
                            for (int i = 0; i < RkeyBuffer.Length; i++)
                            {
                                if (MusicUtils.DirectionToFrequency(RkeyBuffer[i].DirLenght) == currentFz)
                                {
                                    RkeyBuffer.RemoveAt(i);
                                    break;
                                }
                            }
                            SkeyBuffer[SkeyBuffer.Length - 1] = new SustainedKeyBufferData
                            {
                                DirLenght = SkeyBuffer[SkeyBuffer.Length - 1].DirLenght,
                                EffectiveDirLenght = SkeyBuffer[SkeyBuffer.Length - 1].EffectiveDirLenght,
                                Delta = 0,
                                Phase = SkeyBuffer[SkeyBuffer.Length - 1].Phase,
                                currentAmplitude = SkeyBuffer[SkeyBuffer.Length - 1].currentAmplitude
                            };
                            GideReferenceDirection = mouseDirection;
                            activeLegatoFz = currentFz;
                        }

                    }
                }

                
            }
            if (PlayPressed && !UIInput.MouseOverUI)
            {
                /// FOR SHOOTING TIED TO BEAT
                /// Moved to input manager
                //if (BeatProximity < BeatProximityThreshold)
                {
                    //Debug.LogError("test");
                    //InputManager.BeatNotYetPlayed = false;
                    //var localMouseDirection = math.mul(math.inverse(parentTransform.Rotation), new float3(mouseDirection.x, mouseDirection.y, 0)).xy;
                    float randian = Mathf.Abs(PhysicsUtilities.DirectionToRadians(weaponDirLenght));
                    int note = MusicUtils.radiansToNoteIndex(randian);
                   
                    // 0 = not exist : 1 = exist in Rkeybuffer
                    short noteExist = 0;
                    int i;

                    for (i = 0; i < RkeyBuffer.Length; i++)
                    {
                        int bufferNote = MusicUtils.radiansToNoteIndex(Mathf.Abs(PhysicsUtilities.DirectionToRadians(RkeyBuffer[i].DirLenght)));
                        if (bufferNote == note)
                        {
                            noteExist = 1;
                            break;
                        }
                    }
                    if (noteExist == 0)
                    {
                        if (ActiveSynth.Legato)
                        {
                            activeLegatoFz = MusicUtils.DirectionToFrequency(weaponDirLenght);
                        }
                        //add to play buffer
                        //Debug.Log(weaponDirLenght);
                        //Debug.LogError(GideReferenceDirection);
                        PlayedKeyIndex = SkeyBuffer.Length;
                        SkeyBuffer.Add(new SustainedKeyBufferData
                        {
                            TargetDirLenght = weaponDirLenght,
                            DirLenght = GideReferenceDirection,
                            EffectiveDirLenght = GideReferenceDirection,
                            Delta = 0,
                            Phase = 0
                        });

                    }
                    /// Key exist in Rkeybuffer
                    else
                    {
                        //Vector2 effectiveDirLenght = GideReferenceDirection;
                        float newDelta = 0;
                        //effectiveDirLenght = GideReferenceDirection;
                        if (ActiveSynth.Legato)
                        {
                            activeLegatoFz = MusicUtils.DirectionToFrequency(weaponDirLenght);
                            float deltaFactor = 1 - ((ActiveSynth.ADSR.Release - RkeyBuffer[i].Delta) / ActiveSynth.ADSR.Release);
                            /// Deduce the amplitude of the releasing key
                            float amplitude = RkeyBuffer[i].amplitudeAtRelease * (Mathf.Exp(-1.6f * deltaFactor) * (1 - deltaFactor));
                            /// map it to the attack of the new note to keep it continuous
                            newDelta = (amplitude * ActiveSynth.ADSR.Attack);
                        }
                        //add to play buffer
                        PlayedKeyIndex = SkeyBuffer.Length;
                        SkeyBuffer.Add(new SustainedKeyBufferData
                        {
                            TargetDirLenght = weaponDirLenght,
                            DirLenght = GideReferenceDirection,
                            EffectiveDirLenght = GideReferenceDirection,
                            Delta = newDelta,
                            Phase = RkeyBuffer[i].Phase,
                            currentAmplitude = RkeyBuffer[i].currentAmplitude
                        });
                        RkeyBuffer.RemoveAt(i);

                    }
                    GideReferenceDirection = weaponDirLenght;
                    IsShooting = true;
                }
                PlayPressed = false;
            }

            if (PlayReleased)
            {
                activeLegatoFz = 0;
                if (SkeyBuffer.Length != 0)
                {
                    //Debug.LogError(SkeyBuffer[PlayedKeyIndex].currentAmplitude);
                    //Debug.LogError("newDeltaFactor * ActiveSynth.ADSR.Release");
                    RkeyBuffer.Add(new ReleasedKeyBufferData
                    {
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

            KeysBuffer keysBuffer = new KeysBuffer 
            { 
                keyFrenquecies = new NativeArray<float>(12,Allocator.Temp), 
                KeyNumber = new NativeArray<short>(1, Allocator.Temp) 
            };
            keysBuffer.KeyNumber[0] = (short)(SkeyBuffer.Length);

            /// Damage processing 
            /// + Delta/amplitude/filtering incrementing 
            /// + audioBufferData filling
            /// 
            for (int i = 0; i < SkeyBuffer.Length; i++)
            {
                //Vector2 direction = mousepos - new Vector2(Wtrans.ValueRO.Position.x, Wtrans.ValueRO.Position.y);
                Vector2 targetDirLenght = SkeyBuffer[i].TargetDirLenght;
                if (activeLegatoFz>0)
                {
                    targetDirLenght = weaponDirLenght;
                    keysBuffer.keyFrenquecies[i] = activeLegatoFz;
                }
                else
                    keysBuffer.keyFrenquecies[i] = MusicUtils.DirectionToFrequency(targetDirLenght);

                //dirLenght = ActiveSynth.Portomento > 0 ? GideReferenceDirection : dirLenght;
                //dirLenght = PhysicsUtilities.Rotatelerp(SkeyBuffer[i].DirLenght, dirLenght, -Mathf.Log(ActiveSynth.Portomento / 3) * 0.01f + 0.03f);
                //Vector2 currentDirLenght = SkeyBuffer[i].CurrentGlideDir * SkeyBuffer[i].DirLenght.magnitude;
                Vector2 dirLenght = PhysicsUtilities.Rotatelerp(SkeyBuffer[i].DirLenght, targetDirLenght, -Mathf.Log(ActiveSynth.Portomento / 3) * 0.01f + 0.03f);

                Vector2 raycastDirlenght = math.mul(parentTransform.Rotation, new float3(dirLenght.x, dirLenght.y, 0)).xy;

                RayCastHit Hit = PhysicsCalls.RaycastNode(new Ray 
                { 
                    Origin = new Vector2(Wtrans.ValueRO.Position.x, Wtrans.ValueRO.Position.y),
                    DirLength= raycastDirlenght
                }, 
                    PhysicsUtilities.CollisionLayer.MonsterLayer, 
                    ShapeComponentLookup);

                float newDelta = SkeyBuffer[i].Delta + SystemAPI.Time.DeltaTime;
                float newCurrentAmplitude;
                Filter newFilter = new Filter(0,0);
              
                newCurrentAmplitude = newDelta < ActiveSynth.ADSR.Attack ? 
                    newDelta / ActiveSynth.ADSR.Attack : 
                    Mathf.Max(ActiveSynth.ADSR.Sustain, 1 - ((newDelta - ActiveSynth.ADSR.Attack) / ActiveSynth.ADSR.Decay));
                
                newFilter.Cutoff = newDelta < ActiveSynth.filterADSR.Attack? 
                    ActiveSynth.filter.Cutoff + (ActiveSynth.filterEnvelopeAmount * (newDelta / ActiveSynth.filterADSR.Attack)):
                    ActiveSynth.filter.Cutoff + (ActiveSynth.filterEnvelopeAmount * (1 - (Mathf.Min(ActiveSynth.filterADSR.Attack + ActiveSynth.filterADSR.Decay, newDelta) - ActiveSynth.filterADSR.Attack) / ActiveSynth.filterADSR.Decay) * (1 - ActiveSynth.filterADSR.Sustain) + (ActiveSynth.filterADSR.Sustain* ActiveSynth.filterEnvelopeAmount));


                if (Hit.entity != Entity.Null)
                {
                    //Debug.Log(Hit.distance);
                    // hit line
                    Debug.DrawLine(Wtrans.ValueRO.Position, new Vector2(Wtrans.ValueRO.Position.x, Wtrans.ValueRO.Position.y) + (raycastDirlenght.normalized * Hit.distance), Color.red, SystemAPI.Time.DeltaTime);
                   
                    SkeyBuffer[i] = new SustainedKeyBufferData {
                        Delta = newDelta,
                        TargetDirLenght = targetDirLenght,
                        DirLenght = dirLenght,
                        EffectiveDirLenght = dirLenght * (Hit.distance/ dirLenght.magnitude), 
                        Phase = SkeyBuffer[i].Phase,
                        currentAmplitude = newCurrentAmplitude,
                        filter = newFilter
                    };


                    MonsterData newMonsterData = SystemAPI.GetComponent<MonsterData>(Hit.entity);
                    newMonsterData.Health -= 30f* SystemAPI.Time.DeltaTime;

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
                        TargetDirLenght = targetDirLenght,
                        DirLenght = dirLenght, 
                        EffectiveDirLenght = dirLenght, 
                        Phase = SkeyBuffer[i].Phase,
                        currentAmplitude = newCurrentAmplitude,
                        filter = newFilter
                    };
                    //Debug.Log(Mathf.Max(ActiveSynth.ADSR.Sustain, 1 - ((newDelta - ActiveSynth.ADSR.Attack) / ActiveSynth.ADSR.Decay)));
                    
                    Debug.DrawLine(Wtrans.ValueRO.Position, new Vector2(Wtrans.ValueRO.Position.x, Wtrans.ValueRO.Position.y) + raycastDirlenght, Color.white, SystemAPI.Time.DeltaTime);

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

                Vector2 raycastDirlenght = math.mul(parentTransform.Rotation, new float3(RkeyBuffer[i].DirLenght.x, RkeyBuffer[i].DirLenght.y, 0)).xy;

                RayCastHit Hit = PhysicsCalls.RaycastNode(new Ray { 
                    Origin = new Vector2(Wtrans.ValueRO.Position.x, Wtrans.ValueRO.Position.y), 
                    DirLength = raycastDirlenght }, 
                    PhysicsUtilities.CollisionLayer.MonsterLayer, 
                    ShapeComponentLookup);

                if (Hit.entity != Entity.Null)
                {
                    //Debug.Log(Hit.distance);
                    // hit line
                    Debug.DrawLine(Wtrans.ValueRO.Position, new Vector2(Wtrans.ValueRO.Position.x, Wtrans.ValueRO.Position.y) + (raycastDirlenght.normalized * Hit.distance), Color.red, SystemAPI.Time.DeltaTime);

                    float amplitudefactor = RkeyBuffer[i].amplitudeAtRelease * newDelta / ActiveSynth.ADSR.Release;
                    RkeyBuffer[i] = new ReleasedKeyBufferData
                    {
                        Delta = newDelta,
                        DirLenght = RkeyBuffer[i].DirLenght,
                        EffectiveDirLenght = RkeyBuffer[i].DirLenght * (Hit.distance / RkeyBuffer[i].DirLenght.magnitude),
                        Phase = RkeyBuffer[i].Phase,
                        currentAmplitude = RkeyBuffer[i].amplitudeAtRelease * Mathf.Exp(-1.6f * amplitudefactor) * (1- amplitudefactor),
                        amplitudeAtRelease = RkeyBuffer[i].amplitudeAtRelease,
                        filter = newFilter,
                        cutoffEnvelopeAtRelease = RkeyBuffer[i].cutoffEnvelopeAtRelease,
                    };

                    MonsterData newMonsterData = SystemAPI.GetComponent<MonsterData>(Hit.entity);
                    newMonsterData.Health -= 30f * SystemAPI.Time.DeltaTime;

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

                    Debug.DrawLine(Wtrans.ValueRO.Position, new Vector2(Wtrans.ValueRO.Position.x, Wtrans.ValueRO.Position.y) + raycastDirlenght, new Color(1,1,1,1), SystemAPI.Time.DeltaTime);

                    //Debug.Log(RkeyBuffer[i].filter.Cutoff);
                }



            }

            /// Write to the audioRingBuffer to be played on the audio thread
            if (!AudioGenerator.audioRingBuffer.IsFull)
                AudioGenerator.audioRingBuffer.Write(keysBuffer);

            //Debug.LogError(AudioGenerator.audioRingBuffer.Read().KeyNumber[0]);


        }


    }

}