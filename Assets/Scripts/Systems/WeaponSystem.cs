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
using Unity.Rendering;

public partial class WeaponSystem : SystemBase
{

    /// Useless ? active synth index always 0 ?
    //public static short activeSynthEntityindex;

    /// Moved to input manager
    //public float BeatCooldown;
    //private float BeatProximity;
    //private float BeatProximityThreshold;

    EntityCommandBuffer ECB;

    //temp -> put on component
    public static MusicUtils.MusicalMode mode;

    public static bool KeyJustPressed;
    public static bool KeyJustReleased;
    private int PlayedKeyIndex;
    public Vector2 mousepos;
    private bool IsShooting;

    //private bool IsRecording;
    //private NativeList<PlaybackKey> PlayKeys;

    private Vector2 mouseDirection;
    public static Vector2 GideReferenceDirection;
    /// 0 = legato inactive;
    private float activeLegatoFz;

    //private MaterialPropertyBlock _propertyBlock;

    private Entity damageEventEntity;

    public EntityQuery ControlledEquipment_query;

    protected override void OnCreate()
    {
        /// should work but AudioLayoutStorage.activeSynthIdx is changed before ActiveSynthTag removal
        RequireForUpdate<PlayerData>();
        AudioLayoutStorage.activeSynthIdx = -1;

        damageEventEntity = EntityManager.CreateEntityQuery(typeof(GlobalDamageEvent)).GetSingletonEntity();
    }

    protected override void OnStartRunning()
    {
        var Player_query = EntityManager.CreateEntityQuery(typeof(PlayerData));
        ControlledEquipment_query = EntityManager.CreateEntityQuery(typeof(ControledWeaponTag));
        Entity player_entity = Player_query.GetSingletonEntity();
        Entity start_weapon = EntityManager.GetComponentData<PlayerData>(player_entity).MainCanon;

        var ECB = World.GetOrCreateSystemManaged<BeginSimulationEntityCommandBufferSystem>().CreateCommandBuffer();

        /// hide Weapon and DMachine on main slot at start
        var playerWeaponSpriteEntity = EntityManager.GetBuffer<Child>(start_weapon)[0].Value;
        var playerDMachineSpriteEntity = EntityManager.GetBuffer<Child>(start_weapon)[1].Value;
        MaterialMeshInfo newWeaponMaterialMeshInfo = EntityManager.GetComponentData<MaterialMeshInfo>(playerWeaponSpriteEntity);
        newWeaponMaterialMeshInfo.Mesh = 0;
        ECB.SetComponent<MaterialMeshInfo>(playerWeaponSpriteEntity, newWeaponMaterialMeshInfo);
        MaterialMeshInfo newDMachineMaterialMeshInfo = EntityManager.GetComponentData<MaterialMeshInfo>(playerDMachineSpriteEntity);
        newDMachineMaterialMeshInfo.Mesh = 0;
        ECB.SetComponent<MaterialMeshInfo>(playerDMachineSpriteEntity, newDMachineMaterialMeshInfo);
    }


    protected override void OnUpdate()
    {
        var activeSynthIdx = AudioLayoutStorage.activeSynthIdx;
        if (activeSynthIdx == -1)
            return;

        var ShapeComponentLookup = SystemAPI.GetComponentLookup<ShapeData>(true);
        var circleShapeLookUp = SystemAPI.GetComponentLookup<CircleShapeData>(true);
        var boxShapeLookUp = SystemAPI.GetComponentLookup<BoxShapeData>(true);

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
        var mainWeaponEntity = ControlledEquipment_query.GetSingletonEntity();
        DynamicBuffer<SustainedKeyBufferData> SkeyBuffer = SystemAPI.GetBuffer<SustainedKeyBufferData>(mainWeaponEntity);
        DynamicBuffer<ReleasedKeyBufferData> RkeyBuffer = SystemAPI.GetBuffer<ReleasedKeyBufferData>(mainWeaponEntity);

        //TO MODIFY
        //var ActiveSynth = SystemAPI.GetComponent<SynthData>(WeaponEntities[AudioLayoutStorage.activeSynthIdx]);
        var ActiveSynth = AudioLayoutStorageHolder.audioLayoutStorage.AuxillarySynthsData[activeSynthIdx];

        var MainWeapon = SystemAPI.GetSingletonEntity<ControledWeaponTag>();
        var Wtrans = EntityManager.GetComponentData<LocalToWorld>(MainWeapon);
        var trans = EntityManager.GetComponentData<LocalTransform>(MainWeapon);
        Entity ProjectilePrefab = EntityManager.GetComponentData<WeaponData>(MainWeapon).ProjectilePrefab;

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

        bool CanPlayKey = (BeatProximity < InputManager.BeatProximityThreshold && InputManager.CanPressKey);

        /// Move to Synth system all together ?
        /// foreach not necessary as 1 weapon controlled at a time ?
        /// Ray class weapon logic
        foreach (var (rayData,entity) in SystemAPI.Query<RefRO<RayData>>().WithAll<ActiveSynthTag>().WithEntityAccess())
        {
            //Debug.Log(BeatProximity);
            //Debug.DrawLine(new Vector3(5, 0, 0), new Vector3(5, BeatProximity * 10, 0), Color.red);
            //Debug.DrawLine(new Vector3(Wtrans.ValueRO.Position.x, Wtrans.ValueRO.Position.y, 0), new Vector3(5, BeatProximity * 10, 0), Color.red);

            var parentEntity = SystemAPI.GetComponent<Parent>(entity).Value;
            var parentTransform = SystemAPI.GetComponent<LocalToWorld>(parentEntity);

            mouseDirection = mousepos - new Vector2(Wtrans.Position.x, Wtrans.Position.y);

            var localMouseDirection = math.mul(math.inverse(parentTransform.Rotation), new float3(mouseDirection.x, mouseDirection.y, 0)).xy;
            float localCurrentFz = MusicUtils.DirectionToFrequency(localMouseDirection);

            //Debug.LogError(localCurrentFz);

            if (!UIInput.MouseOverUI && localCurrentFz > 0)
            {
                if (activeLegatoFz == 0)
                {
                    if (!IsShooting)
                    {
                        var newLocalRot = Quaternion.Euler(0, 0, Mathf.Atan2(-mouseDirection.y, -mouseDirection.x) * Mathf.Rad2Deg);
                        ///local to world set
                        trans.Rotation = math.mul(math.inverse(parentTransform.Rotation), newLocalRot);
                    }
                }
                else
                {

                    Vector2 worldMouseDir = mousepos - new Vector2(Wtrans.Position.x, Wtrans.Position.y);
             
                    var LocalRot = Quaternion.Euler(0, 0, Mathf.Atan2(-worldMouseDir.y, -worldMouseDir.x) * Mathf.Rad2Deg);
                    //var localMouseDirection = math.mul(math.inverse(parentTransform.Rotation), new float3(mouseDirection.x, mouseDirection.y, 0)).xy;
                    ///local to world set
                    //trans.Rotation = math.mul(math.inverse(parentTransform.Rotation), LocalRot);
      
                    //if (localCurrentFz != activeLegatoFz)
                    //{ 
                    //    //if (BeatProximity < InputManager.BeatProximityThreshold)
                    //    //{ mouseDirection = worldMouseDir; }
                    //}
                    //else
                    //{
                    //    mouseDirection = worldMouseDir;
                    //}
                }
                ECB.SetComponent<LocalTransform>(MainWeapon, trans);
            }


            Vector2 weaponDirLenght = math.mul(math.inverse(parentTransform.Rotation), new float3(mouseDirection.x, mouseDirection.y, 0)).xy;
            float weaponFz = MusicUtils.DirectionToFrequency(weaponDirLenght);

            if (KeyJustPressed && !UIInput.MouseOverUI && localCurrentFz>0)
            {
                //Debug.LogError("test");
                //InputManager.BeatNotYetPlayed = false;
                //var localMouseDirection = math.mul(math.inverse(parentTransform.Rotation), new float3(mouseDirection.x, mouseDirection.y, 0)).xy;
                float radian = PhysicsUtilities.DirectionToRadians(weaponDirLenght);
                int note = MusicUtils.radiansToNoteIndex(radian);
                   
                // 0 = not exist : 1 = exist in Rkeybuffer
                short noteExist = 0;
                int i;

                for (i = 0; i < RkeyBuffer.Length; i++)
                {
                    int bufferNote = MusicUtils.radiansToNoteIndex(PhysicsUtilities.DirectionToRadians(RkeyBuffer[i].DirLenght));
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
                        activeLegatoFz = weaponFz;
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
               
                KeyJustPressed = false;
            }


            if (KeyJustReleased)
            {
                //if (weaponData.ValueRO.weaponClass == WeaponClass.Ray)
                {
                    if (SkeyBuffer.Length != 0)
                    {
                        //Debug.Log("rere");
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
                }
                activeLegatoFz = 0;
                IsShooting = false;
                KeyJustReleased = false;
            }


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
            {
                for (int i = 0; i < SkeyBuffer.Length; i++)
                {
                    Vector2 targetDirLenght = SkeyBuffer[i].TargetDirLenght;
                    float dirFrequency = MusicUtils.DirectionToFrequency(targetDirLenght);
                    if (activeLegatoFz > 0 && localCurrentFz>0)
                    {
                        //Debug.Log(activeLegatoFz);
                        //Debug.Log("test");
                        if (activeLegatoFz == weaponFz)
                        {
                            targetDirLenght = weaponDirLenght;
                            keysBuffer.keyFrenquecies[i] = weaponFz;
                        }
                        else if(CanPlayKey)
                        {
                            targetDirLenght = weaponDirLenght;
                            keysBuffer.keyFrenquecies[i] = weaponFz;
                            //Debug.Log("test");
                            InputManager.CanPressKey = false;
                            activeLegatoFz = weaponFz;
                            GideReferenceDirection = weaponDirLenght;
                        }
                        else
                        {
                            keysBuffer.keyFrenquecies[i] = dirFrequency;
                            //activeLegatoFz = dirFrequency;
                        }
                    }
                    else
                    {
                        keysBuffer.keyFrenquecies[i] = dirFrequency;
                        //activeLegatoFz = dirFrequency;
                    }

                    Vector2 dirLenght = PhysicsUtilities.Rotatelerp(SkeyBuffer[i].DirLenght, targetDirLenght, -Mathf.Log(ActiveSynth.Portomento / 3) * 0.01f + 0.03f);

                    Vector2 raycastDirlenght = math.mul(parentTransform.Rotation, new float3(dirLenght.x, dirLenght.y, 0)).xy;

                    RayCastHit Hit = PhysicsCalls.RaycastNode(new Ray
                    {
                        Origin = new Vector2(Wtrans.Position.x, Wtrans.Position.y),
                        DirLength = raycastDirlenght
                    },
                        PhysicsUtilities.CollisionLayer.MonsterLayer,
                        ShapeComponentLookup,circleShapeLookUp,boxShapeLookUp);

                    float newDelta = SkeyBuffer[i].Delta + SystemAPI.Time.DeltaTime;
                    float newCurrentAmplitude;
                    Filter newFilter = new Filter(0, 0);

                    newCurrentAmplitude = newDelta < ActiveSynth.ADSR.Attack ?
                        newDelta / ActiveSynth.ADSR.Attack :
                        Mathf.Max(ActiveSynth.ADSR.Sustain, 1 - ((newDelta - ActiveSynth.ADSR.Attack) / ActiveSynth.ADSR.Decay));

                    newFilter.Cutoff = newDelta < ActiveSynth.filterADSR.Attack ?
                        ActiveSynth.filter.Cutoff + (ActiveSynth.filterEnvelopeAmount * (newDelta / ActiveSynth.filterADSR.Attack)) :
                        ActiveSynth.filter.Cutoff + (ActiveSynth.filterEnvelopeAmount * (1 - (Mathf.Min(ActiveSynth.filterADSR.Attack + ActiveSynth.filterADSR.Decay, newDelta) - ActiveSynth.filterADSR.Attack) / ActiveSynth.filterADSR.Decay) * (1 - ActiveSynth.filterADSR.Sustain) + (ActiveSynth.filterADSR.Sustain * ActiveSynth.filterEnvelopeAmount));


                    if (Hit.entity != Entity.Null)
                    {
                        //Debug.Log(Hit.distance);
                        // hit line
                        Debug.DrawLine(Wtrans.Position, new Vector2(Wtrans.Position.x, Wtrans.Position.y) + (raycastDirlenght.normalized * Hit.distance), Color.red, SystemAPI.Time.DeltaTime);

                        SkeyBuffer[i] = new SustainedKeyBufferData
                        {
                            Delta = newDelta,
                            TargetDirLenght = targetDirLenght,
                            DirLenght = dirLenght,
                            EffectiveDirLenght = dirLenght * (Hit.distance / dirLenght.magnitude),
                            Phase = SkeyBuffer[i].Phase,
                            currentAmplitude = newCurrentAmplitude,
                            filter = newFilter
                        };

                        SystemAPI.GetBuffer<GlobalDamageEvent>(damageEventEntity).Add(new GlobalDamageEvent
                        {
                            Target = Hit.entity,
                            DamageValue = 25f * SystemAPI.Time.DeltaTime
                        });

                        //Debug.Log("rayhit : " + Hit.entity);

                    }
                    else
                    {

                        SkeyBuffer[i] = new SustainedKeyBufferData
                        {
                            Delta = SkeyBuffer[i].Delta + SystemAPI.Time.DeltaTime,
                            TargetDirLenght = targetDirLenght,
                            DirLenght = dirLenght,
                            EffectiveDirLenght = dirLenght,
                            Phase = SkeyBuffer[i].Phase,
                            currentAmplitude = newCurrentAmplitude,
                            filter = newFilter
                        };
                        //Debug.Log(Mathf.Max(ActiveSynth.ADSR.Sustain, 1 - ((newDelta - ActiveSynth.ADSR.Attack) / ActiveSynth.ADSR.Decay)));

                        Debug.DrawLine(Wtrans.Position, new Vector2(Wtrans.Position.x, Wtrans.Position.y) + raycastDirlenght, Color.white, SystemAPI.Time.DeltaTime);

                    }

                }
                for (int i = 0; i < RkeyBuffer.Length; i++)
                {

                    float newDelta = RkeyBuffer[i].Delta + SystemAPI.Time.DeltaTime;
                    //float Cutoff = ActiveSynth.filter.Cutoff / (Mathf.Exp(ActiveSynth.filter.Cutoff * 5 - 5) * ActiveSynth.filter.Cutoff);

                    Filter newFilter = new Filter
                    {
                        Cutoff = ActiveSynth.filter.Cutoff + ((RkeyBuffer[i].cutoffEnvelopeAtRelease) * (1 - Mathf.Min(ActiveSynth.filterADSR.Release, newDelta) / ActiveSynth.filterADSR.Release)),
                        Resonance = 0
                    };

                    if (newDelta > ActiveSynth.ADSR.Release)
                    {
                        RkeyBuffer.RemoveAt(i);
                        i--;
                        continue;
                    }

                    Vector2 raycastDirlenght = math.mul(parentTransform.Rotation, new float3(RkeyBuffer[i].DirLenght.x, RkeyBuffer[i].DirLenght.y, 0)).xy;

                    RayCastHit Hit = PhysicsCalls.RaycastNode(new Ray
                    {
                        Origin = new Vector2(Wtrans.Position.x, Wtrans.Position.y),
                        DirLength = raycastDirlenght
                    },
                        PhysicsUtilities.CollisionLayer.MonsterLayer,
                        ShapeComponentLookup, circleShapeLookUp, boxShapeLookUp);

                    if (Hit.entity != Entity.Null)
                    {
                        //Debug.Log(Hit.distance);
                        // hit line
                        Debug.DrawLine(Wtrans.Position, new Vector2(Wtrans.Position.x, Wtrans.Position.y) + (raycastDirlenght.normalized * Hit.distance), Color.red, SystemAPI.Time.DeltaTime);

                        float amplitudefactor = RkeyBuffer[i].amplitudeAtRelease * newDelta / ActiveSynth.ADSR.Release;
                        RkeyBuffer[i] = new ReleasedKeyBufferData
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

                        SystemAPI.GetBuffer<GlobalDamageEvent>(damageEventEntity).Add(new GlobalDamageEvent
                        {
                            Target = Hit.entity,
                            DamageValue = 9f * SystemAPI.Time.DeltaTime
                        });

                    }
                    else
                    {

                        RkeyBuffer[i] = new ReleasedKeyBufferData
                        {
                            Delta = newDelta,
                            DirLenght = RkeyBuffer[i].DirLenght,
                            EffectiveDirLenght = RkeyBuffer[i].DirLenght,
                            Phase = RkeyBuffer[i].Phase,
                            currentAmplitude = RkeyBuffer[i].amplitudeAtRelease * Mathf.Exp(-4.6f * RkeyBuffer[i].amplitudeAtRelease * newDelta / ActiveSynth.ADSR.Release),
                            //currentAmplitude = RkeyBuffer[i].amplitudeAtRelease * (1 - newDelta / ActiveSynth.ADSR.Release),
                            amplitudeAtRelease = RkeyBuffer[i].amplitudeAtRelease,
                            filter = newFilter,
                            cutoffEnvelopeAtRelease = RkeyBuffer[i].cutoffEnvelopeAtRelease,
                        };
                        //Debug.Log(RkeyBuffer[i].currentAmplitude);

                        Debug.DrawLine(Wtrans.Position, new Vector2(Wtrans.Position.x, Wtrans.Position.y) + raycastDirlenght, new Color(1, 1, 1, 1), SystemAPI.Time.DeltaTime);

                        //Debug.Log(RkeyBuffer[i].filter.Cutoff);
                    }



                }
            }

        

            /// Write to the audioRingBuffer to be played on the audio thread
            if (!AudioGenerator.audioRingBuffer.IsFull)
                AudioGenerator.audioRingBuffer.Write(keysBuffer);

            //Debug.LogError(AudioGenerator.audioRingBuffer.Read().KeyNumber[0]);

        }

        /// Projectile class weapon logic
        foreach (var (projectileData, entity) in SystemAPI.Query<RefRO<ProjectileData>>().WithAll<ActiveSynthTag>().WithEntityAccess())
        {
            var parentEntity = SystemAPI.GetComponent<Parent>(entity).Value;
            var parentTransform = SystemAPI.GetComponent<LocalToWorld>(parentEntity);

            mouseDirection = mousepos - new Vector2(Wtrans.Position.x, Wtrans.Position.y);
            var localMouseDirection = math.mul(math.inverse(parentTransform.Rotation), new float3(mouseDirection.x, mouseDirection.y, 0)).xy;
            float localCurrentFz = MusicUtils.DirectionToFrequency(localMouseDirection);

            Vector2 weaponDirLenght = math.mul(math.inverse(parentTransform.Rotation), new float3(mouseDirection.x, mouseDirection.y, 0)).xy;

            if (!UIInput.MouseOverUI)
            {
                var newLocalRot = Quaternion.Euler(0, 0, Mathf.Atan2(-mouseDirection.y, -mouseDirection.x) * Mathf.Rad2Deg);
                ///local to world set
                trans.Rotation = math.mul(math.inverse(parentTransform.Rotation), newLocalRot);
                ECB.SetComponent<LocalTransform>(MainWeapon, trans);
            }
            if (KeyJustPressed && !UIInput.MouseOverUI && localCurrentFz>0)
            {
                /// Projectile instanciate
                { 
                    var projectileInstance = ECB.Instantiate(ProjectilePrefab);
                    ECB.SetComponent<Ocs1SinSawSquareFactorOverride>(projectileInstance, new Ocs1SinSawSquareFactorOverride { Value = ActiveSynth.Osc1SinSawSquareFactor });
                    ECB.SetComponent<Ocs2SinSawSquareFactorOverride>(projectileInstance, new Ocs2SinSawSquareFactorOverride { Value = ActiveSynth.Osc2SinSawSquareFactor });
                    /// do default + right trans ?
                    ECB.SetComponent<ShapeData>(projectileInstance, new ShapeData
                    {
                        Position = Wtrans.Position.xy,
                        PreviousPosition = trans.Position.xy,
                        Rotation = 0,
                        collisionLayer = PhysicsUtilities.CollisionLayer.ProjectileLayer,
                        HasDynamics = false,
                        IsTrigger = true,
                    });
                    ECB.SetComponent<PhyBodyData>(projectileInstance, new PhyBodyData
                    {
                        AngularDamp = 0,
                        LinearDamp = 0,
                        Velocity = mouseDirection.normalized * projectileData.ValueRO.Speed,
                    });
                    ECB.AddComponent<ProjectileInstanceData>(projectileInstance, new ProjectileInstanceData
                    {
                        damage = projectileData.ValueRO.Damage,
                        remainingLifeTime = projectileData.ValueRO.LifeTime,
                        speed = projectileData.ValueRO.Speed,
                        penetrationCapacity = 3
                    });
                }
            

                float randian = Mathf.Abs(PhysicsUtilities.DirectionToRadians(weaponDirLenght));
                int note = MusicUtils.radiansToNoteIndex(randian);

                // 0 = not exist : 1 = exist in Rkeybuffer
                short noteExist = 0;
                int i;

                for (i = 0; i < RkeyBuffer.Length; i++)
                {
                    int bufferNote = MusicUtils.radiansToNoteIndex(PhysicsUtilities.DirectionToRadians(RkeyBuffer[i].DirLenght));
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
                    float newDelta = 0;
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

                KeyJustPressed = false;
            }
            if (KeyJustReleased)
            {
                if (SkeyBuffer.Length != 0)
                {
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
                activeLegatoFz = 0;
                IsShooting = false;
                KeyJustReleased = false;
            }

            KeysBuffer keysBuffer = new KeysBuffer
            {
                keyFrenquecies = new NativeArray<float>(12, Allocator.Temp),
                KeyNumber = new NativeArray<short>(1, Allocator.Temp)
            };
            keysBuffer.KeyNumber[0] = (short)(SkeyBuffer.Length);

            /// + Delta/amplitude/filtering incrementing 
            /// + audioBufferData filling
            /// 
            {
                for (int i = 0; i < SkeyBuffer.Length; i++)
                {
                    Vector2 targetDirLenght = SkeyBuffer[i].TargetDirLenght;
                    if (activeLegatoFz > 0)
                    {
                        targetDirLenght = weaponDirLenght;
                        keysBuffer.keyFrenquecies[i] = activeLegatoFz;
                    }
                    else
                        keysBuffer.keyFrenquecies[i] = MusicUtils.DirectionToFrequency(targetDirLenght);

                    Vector2 dirLenght = PhysicsUtilities.Rotatelerp(SkeyBuffer[i].DirLenght, targetDirLenght, -Mathf.Log(ActiveSynth.Portomento / 3) * 0.01f + 0.03f);

                    float newDelta = SkeyBuffer[i].Delta + SystemAPI.Time.DeltaTime;
                    float newCurrentAmplitude;
                    Filter newFilter = new Filter(0, 0);

                    newCurrentAmplitude = newDelta < ActiveSynth.ADSR.Attack ?
                        newDelta / ActiveSynth.ADSR.Attack :
                        Mathf.Max(ActiveSynth.ADSR.Sustain, 1 - ((newDelta - ActiveSynth.ADSR.Attack) / ActiveSynth.ADSR.Decay));

                    newFilter.Cutoff = newDelta < ActiveSynth.filterADSR.Attack ?
                        ActiveSynth.filter.Cutoff + (ActiveSynth.filterEnvelopeAmount * (newDelta / ActiveSynth.filterADSR.Attack)) :
                        ActiveSynth.filter.Cutoff + (ActiveSynth.filterEnvelopeAmount * (1 - (Mathf.Min(ActiveSynth.filterADSR.Attack + ActiveSynth.filterADSR.Decay, newDelta) - ActiveSynth.filterADSR.Attack) / ActiveSynth.filterADSR.Decay) * (1 - ActiveSynth.filterADSR.Sustain) + (ActiveSynth.filterADSR.Sustain * ActiveSynth.filterEnvelopeAmount));
                    
                    SkeyBuffer[i] = new SustainedKeyBufferData
                    {
                        Delta = SkeyBuffer[i].Delta + SystemAPI.Time.DeltaTime,
                        TargetDirLenght = targetDirLenght,
                        DirLenght = dirLenght,
                        EffectiveDirLenght = dirLenght,
                        Phase = SkeyBuffer[i].Phase,
                        currentAmplitude = newCurrentAmplitude,
                        filter = newFilter
                    };

                }
                for (int i = 0; i < RkeyBuffer.Length; i++)
                {

                    float newDelta = RkeyBuffer[i].Delta + SystemAPI.Time.DeltaTime;
                    //float Cutoff = ActiveSynth.filter.Cutoff / (Mathf.Exp(ActiveSynth.filter.Cutoff * 5 - 5) * ActiveSynth.filter.Cutoff);

                    Filter newFilter = new Filter
                    {
                        Cutoff = ActiveSynth.filter.Cutoff + ((RkeyBuffer[i].cutoffEnvelopeAtRelease) * (1 - Mathf.Min(ActiveSynth.filterADSR.Release, newDelta) / ActiveSynth.filterADSR.Release)),
                        Resonance = 0
                    };

                    if (newDelta > ActiveSynth.ADSR.Release)
                    {
                        RkeyBuffer.RemoveAt(i);
                        i--;
                        continue;
                    }

                    RkeyBuffer[i] = new ReleasedKeyBufferData
                    {
                        Delta = newDelta,
                        DirLenght = RkeyBuffer[i].DirLenght,
                        EffectiveDirLenght = RkeyBuffer[i].DirLenght,
                        Phase = RkeyBuffer[i].Phase,
                        currentAmplitude = RkeyBuffer[i].amplitudeAtRelease * Mathf.Exp(-4.6f * RkeyBuffer[i].amplitudeAtRelease * newDelta / ActiveSynth.ADSR.Release),
                        //currentAmplitude = RkeyBuffer[i].amplitudeAtRelease * (1 - newDelta / ActiveSynth.ADSR.Release),
                        amplitudeAtRelease = RkeyBuffer[i].amplitudeAtRelease,
                        filter = newFilter,
                        cutoffEnvelopeAtRelease = RkeyBuffer[i].cutoffEnvelopeAtRelease,
                    };

                }
            }

            /// Write to the audioRingBuffer to be played on the audio thread
            if (!AudioGenerator.audioRingBuffer.IsFull)
                AudioGenerator.audioRingBuffer.Write(keysBuffer);
        }

    }

}