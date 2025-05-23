
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

public partial struct PlaybackSystem : ISystem
{

    /// Used to test keys upon removal to see if they had time to be addded;
    float PreviousFrameDelta;

    /// REDONDANT ?
    private Entity damageEventEntity;
    private EntityQuery damageEventEntityQuery;

    private EntityQuery StunManagerEntityQuery;

    public void OnCreate(ref SystemState state)
    {
        // Check if the GlobalDamageEvent entity already exists
        if (SystemAPI.HasSingleton<GlobalDamageEvent>())
        {
            // If it exists, get the singleton entity
            damageEventEntity = SystemAPI.GetSingletonEntity<GlobalDamageEvent>();
        }
        else
        {
            // If not, create the entity and add the GlobalDamageEvent buffer
            damageEventEntity = state.EntityManager.CreateEntity();
            state.EntityManager.AddBuffer<GlobalDamageEvent>(damageEventEntity); // Add the buffer to the new entity
        }
        damageEventEntityQuery = state.EntityManager.CreateEntityQuery(typeof(GlobalDamageEvent));
        StunManagerEntityQuery = state.EntityManager.CreateEntityQuery(typeof(StunEffectData));

        state.RequireForUpdate<ControledWeaponTag>();

    }


    public void OnUpdate(ref SystemState state)
    {

        EntityCommandBuffer ecb = SystemAPI.GetSingleton<BeginSimulationEntityCommandBufferSystem.Singleton>().CreateCommandBuffer(state.WorldUnmanaged);

        //ComponentLookup<CircleShapeData> ShapeComponentLookup = SystemAPI.GetComponentLookup<CircleShapeData>(isReadOnly: true);
        var ShapeComponentLookup = SystemAPI.GetComponentLookup<ShapeData>(true);
        var circleShapeLookUp = SystemAPI.GetComponentLookup<CircleShapeData>(true);
        var boxShapeLookUp = SystemAPI.GetComponentLookup<BoxShapeData>(true);

        var MainWeapon = SystemAPI.GetSingletonEntity<ControledWeaponTag>();
        //Entity ProjectilePrefab = state.EntityManager.GetComponentData<WeaponAmmoData>(MainWeapon).ProjectilePrefab;
        Entity ProjectilePrefab = SystemAPI.GetSingleton<AmmoPrefabHolder>().ProjectilePrefab;

        foreach (var (playback_data,rayData, Wtrans,trans, entity) in SystemAPI.Query<RefRW<PlaybackData>, RefRW<RayData>, RefRO<LocalToWorld>,RefRW<LocalTransform>>().WithEntityAccess())
        {

            var ActiveSynth = AudioLayoutStorageHolder.audioLayoutStorage.AuxillarySynthsData[playback_data.ValueRO.FullPlaybackIndex.x];

            DynamicBuffer<PlaybackSustainedKeyBufferData> SkeyBuffer = SystemAPI.GetBuffer<PlaybackSustainedKeyBufferData>(entity);
            DynamicBuffer<PlaybackReleasedKeyBufferData> RkeyBuffer = SystemAPI.GetBuffer<PlaybackReleasedKeyBufferData>(entity);

            int playbackKeyIndex = playback_data.ValueRO.PlaybackElementIndex;

            var parentEntity = SystemAPI.GetComponent<Parent>(entity).Value;
            var parentTransform = SystemAPI.GetComponent<LocalToWorld>(parentEntity);
            //Vector2 rotatedDirLenght = math.mul(math.inverse(parentTransform.Rotation), new float3(mouseDirection.x, mouseDirection.y, 0)).xy;

            /// OPTI ?
            while (playbackKeyIndex < AudioLayoutStorageHolder.audioLayoutStorage.PlaybackAudioBundles[playback_data.ValueRO.FullPlaybackIndex.x].PlaybackKeys.Length
                && AudioLayoutStorageHolder.audioLayoutStorage.PlaybackAudioBundles[playback_data.ValueRO.FullPlaybackIndex.x].PlaybackKeys[playbackKeyIndex].time < playback_data.ValueRO.PlaybackTime)
            {
                PlaybackKey playbackkey = AudioLayoutStorageHolder.audioLayoutStorage.PlaybackAudioBundles[playback_data.ValueRO.FullPlaybackIndex.x].PlaybackKeys[playbackKeyIndex];
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
                                EffectiveDirLenght = math.mul(math.inverse(parentTransform.Rotation), new float3(SkeyBuffer[SkeyBuffer.Length - 1].EffectiveDirLenght, 0)).xy,
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
                    playback_data.ValueRW.PlaybackElementIndex = playbackKeyIndex;
                }
                else
                {
                    playbackKeyIndex++;
                }
                if (playbackKeyIndex >= AudioLayoutStorageHolder.audioLayoutStorage.PlaybackAudioBundles[playback_data.ValueRO.FullPlaybackIndex.x].PlaybackKeys.Length)
                    break;
      

            }

            for (int i = playback_data.ValueRO.KeysPlayed; i < playbackKeyIndex; i++)
            {
                PlaybackKey playbackKey = AudioLayoutStorageHolder.audioLayoutStorage.PlaybackAudioBundles[playback_data.ValueRO.FullPlaybackIndex.x].PlaybackKeys[i];

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

            if (playback_data.ValueRW.PlaybackTime + SystemAPI.Time.DeltaTime > AudioLayoutStorageHolder.audioLayoutStorage.PlaybackAudioBundles[playback_data.ValueRO.FullPlaybackIndex.x].PlaybackDuration)
            {
                AudioLayoutStorageHolder.audioLayoutStorage.PlaybackContextResetRequired.Enqueue(playback_data.ValueRO.FullPlaybackIndex.x);

                playback_data.ValueRW.PlaybackTime = (playback_data.ValueRO.PlaybackTime + SystemAPI.Time.DeltaTime) - AudioLayoutStorageHolder.audioLayoutStorage.PlaybackAudioBundles[playback_data.ValueRO.FullPlaybackIndex.x].PlaybackDuration;
                playback_data.ValueRW.PlaybackElementIndex = 0;
                playback_data.ValueRW.KeysPlayed = 0;
                SkeyBuffer.Clear();
                RkeyBuffer.Clear();
            }
            else
                playback_data.ValueRW.PlaybackTime += SystemAPI.Time.DeltaTime;
            PreviousFrameDelta = SystemAPI.Time.DeltaTime;



            /// Damage processing + Delta/amplitude incrementing
            for (int i = 0; i < SkeyBuffer.Length; i++)
            {
                //Vector2 dirLenght = ActiveSynth.Legato ? direction : SkeyBuffer[i].DirLenght;
                Vector2 dirLenght = PhysicsUtilities.Rotatelerp(SkeyBuffer[i].StartDirLenght, SkeyBuffer[i].DirLenght, -Mathf.Log(ActiveSynth.Portomento / 3) * 0.01f + 0.03f);
                //Vector2 localDirLenght = math.mul(math.inverse(parentTransform.Rotation), new float3(dirLenght, 0)).xy;
                /// set rotation of entity
                {
                    trans.ValueRW.Rotation = Quaternion.Euler(0, 0, Mathf.Atan2(-SkeyBuffer[i].DirLenght.y, -SkeyBuffer[i].DirLenght.x) * Mathf.Rad2Deg);
                }

                Vector2 raycastDirlenght = math.mul(parentTransform.Rotation, new float3(dirLenght.x, dirLenght.y, 0)).xy;

                RayCastHit Hit = PhysicsCalls.RaycastNode(new Ray { 
                    Origin = new Vector2(Wtrans.ValueRO.Position.x, Wtrans.ValueRO.Position.y), 
                    DirLength = raycastDirlenght }, 
                    PhysicsUtilities.CollisionLayer.MonsterLayer,
                    ShapeComponentLookup, circleShapeLookUp, boxShapeLookUp);

                float newDelta = SkeyBuffer[i].Delta + SystemAPI.Time.DeltaTime;
                Filter newFilter = new Filter(0, 0);

                newFilter.Cutoff = newDelta < ActiveSynth.filterADSR.Attack ?
                 ActiveSynth.filter.Cutoff + (ActiveSynth.filterEnvelopeAmount * (newDelta / ActiveSynth.filterADSR.Attack)) :
                 ActiveSynth.filter.Cutoff + (ActiveSynth.filterEnvelopeAmount * (1 - (Mathf.Min(ActiveSynth.filterADSR.Attack + ActiveSynth.filterADSR.Decay, newDelta) - ActiveSynth.filterADSR.Attack) / ActiveSynth.filterADSR.Decay) * (1 - ActiveSynth.filterADSR.Sustain) + (ActiveSynth.filterADSR.Sustain * ActiveSynth.filterEnvelopeAmount));


                if (Hit.entity != Entity.Null)
                {
                    //Debug.Log(Hit.distance);
                    // hit line
                    Debug.DrawLine(Wtrans.ValueRO.Position, new Vector2(Wtrans.ValueRO.Position.x, Wtrans.ValueRO.Position.y) + (raycastDirlenght.normalized * Hit.distance), Color.red, SystemAPI.Time.DeltaTime);

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

                    SystemAPI.GetBuffer<GlobalDamageEvent>(damageEventEntity).Add(new GlobalDamageEvent
                    {
                        Target = Hit.entity,
                        DamageValue = 25f * SystemAPI.Time.DeltaTime
                    });
                    //MonsterData newMonsterData = SystemAPI.GetComponent<MonsterData>(Hit.entity);
                    //newMonsterData.hp -= 3f * SystemAPI.Time.DeltaTime;

                    //if (newMonsterData.Health > 0)
                    //{
                    //    //Debug.Log(newMonsterData.Health);
                    //    SystemAPI.SetComponent<MonsterData>(Hit.entity, newMonsterData);
                    //}
                    //else
                    //{
                    //    //Debug.Log("ded");
                    //    PhysicsCalls.DestroyPhysicsEntity(ecb, Hit.entity);
                    //}

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

                    Debug.DrawLine(Wtrans.ValueRO.Position, new Vector2(Wtrans.ValueRO.Position.x, Wtrans.ValueRO.Position.y) + raycastDirlenght, Color.white, SystemAPI.Time.DeltaTime);

                }

            }
            for (int i = 0; i < RkeyBuffer.Length; i++)
            {

                //Vector2 localDirLenght = math.mul(math.inverse(parentTransform.Rotation), new float3(RkeyBuffer[i].DirLenght, 0)).xy;

                Vector2 raycastDirlenght = math.mul(parentTransform.Rotation, new float3(RkeyBuffer[i].DirLenght.x, RkeyBuffer[i].DirLenght.y, 0)).xy;

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

                RayCastHit Hit = PhysicsCalls.RaycastNode(new Ray { 
                    Origin = new Vector2(Wtrans.ValueRO.Position.x, Wtrans.ValueRO.Position.y), 
                    DirLength = raycastDirlenght}, 
                    PhysicsUtilities.CollisionLayer.MonsterLayer, 
                    ShapeComponentLookup,circleShapeLookUp,boxShapeLookUp);

                if (Hit.entity != Entity.Null)
                {
                    //Debug.Log(Hit.distance);
                    // hit line
                    Debug.DrawLine(Wtrans.ValueRO.Position, new Vector2(Wtrans.ValueRO.Position.x, Wtrans.ValueRO.Position.y) + (raycastDirlenght.normalized * Hit.distance), Color.red, SystemAPI.Time.DeltaTime);

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

                    SystemAPI.GetBuffer<GlobalDamageEvent>(damageEventEntity).Add(new GlobalDamageEvent
                    {
                        Target = Hit.entity,
                        DamageValue = 25f * SystemAPI.Time.DeltaTime
                    });
                    //MonsterData newMonsterData = SystemAPI.GetComponent<MonsterData>(Hit.entity);
                    //newMonsterData.Health -= 3f * SystemAPI.Time.DeltaTime;

                    //if (newMonsterData.Health > 0)
                    //{
                    //    //Debug.Log(newMonsterData.Health);
                    //    SystemAPI.SetComponent<MonsterData>(Hit.entity, newMonsterData);
                    //}
                    //else
                    //{
                    //    //Debug.Log("ded");
                    //    PhysicsCalls.DestroyPhysicsEntity(ecb, Hit.entity);
                    //}

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

                    Debug.DrawLine(Wtrans.ValueRO.Position, new Vector2(Wtrans.ValueRO.Position.x, Wtrans.ValueRO.Position.y) + raycastDirlenght, Color.white, SystemAPI.Time.DeltaTime);

                }
            }

        }

        foreach (var (playback_data, projectileData, Wtrans, trans, entity) in SystemAPI.Query<RefRW<PlaybackData>, RefRW<WeaponAmmoData>, RefRO<LocalToWorld>, RefRW<LocalTransform>>().WithEntityAccess())
        {

            var ActiveSynth = AudioLayoutStorageHolder.audioLayoutStorage.AuxillarySynthsData[playback_data.ValueRO.FullPlaybackIndex.x];

            DynamicBuffer<PlaybackSustainedKeyBufferData> SkeyBuffer = SystemAPI.GetBuffer<PlaybackSustainedKeyBufferData>(entity);
            DynamicBuffer<PlaybackReleasedKeyBufferData> RkeyBuffer = SystemAPI.GetBuffer<PlaybackReleasedKeyBufferData>(entity);

            int playbackKeyIndex = playback_data.ValueRW.PlaybackElementIndex;

            var parentEntity = SystemAPI.GetComponent<Parent>(entity).Value;
            var parentTransform = SystemAPI.GetComponent<LocalToWorld>(parentEntity);
            //Vector2 rotatedDirLenght = math.mul(math.inverse(parentTransform.Rotation), new float3(mouseDirection.x, mouseDirection.y, 0)).xy;

            /// OPTI ?
            while (playbackKeyIndex < AudioLayoutStorageHolder.audioLayoutStorage.PlaybackAudioBundles[playback_data.ValueRO.FullPlaybackIndex.x].PlaybackKeys.Length
                && AudioLayoutStorageHolder.audioLayoutStorage.PlaybackAudioBundles[playback_data.ValueRO.FullPlaybackIndex.x].PlaybackKeys[playbackKeyIndex].time < playback_data.ValueRO.PlaybackTime)
            {
                PlaybackKey playbackkey = AudioLayoutStorageHolder.audioLayoutStorage.PlaybackAudioBundles[playback_data.ValueRO.FullPlaybackIndex.x].PlaybackKeys[playbackKeyIndex];
                /// went past the latest active key
                if (playbackkey.time + playbackkey.lenght < playback_data.ValueRO.PlaybackTime)
                {
                    /// Make sure the key had time to activate if released
                    if (playbackkey.time < playback_data.ValueRO.PlaybackTime - PreviousFrameDelta)
                    {
                        if (!playbackkey.dragged)
                            RkeyBuffer.Add(new PlaybackReleasedKeyBufferData
                            {
                                DirLenght = SkeyBuffer[SkeyBuffer.Length - 1].DirLenght,
                                EffectiveDirLenght = math.mul(math.inverse(parentTransform.Rotation), new float3(SkeyBuffer[SkeyBuffer.Length - 1].EffectiveDirLenght, 0)).xy,
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
                    playback_data.ValueRW.PlaybackElementIndex = playbackKeyIndex;
                }
                else
                {
                    playbackKeyIndex++;
                }
                if (playbackKeyIndex >= AudioLayoutStorageHolder.audioLayoutStorage.PlaybackAudioBundles[playback_data.ValueRO.FullPlaybackIndex.x].PlaybackKeys.Length)
                    break;


            }

            for (int i = playback_data.ValueRO.KeysPlayed; i < playbackKeyIndex; i++)
            {
                PlaybackKey playbackKey = AudioLayoutStorageHolder.audioLayoutStorage.PlaybackAudioBundles[playback_data.ValueRO.FullPlaybackIndex.x].PlaybackKeys[i];
                Vector2 dirLenght = math.mul(parentTransform.Rotation, new float3(playbackKey.dir, 0)).xy;

                //Debug.DrawRay(new float3(Wtrans.ValueRO.Position.x, Wtrans.ValueRO.Position.y, -3), new Vector3(dirLenght.x,dirLenght.y,0),Color.red);

                /// weapon rotation
                trans.ValueRW.Rotation = Quaternion.Euler(0, 0, Mathf.Atan2(-playbackKey.dir.y, -playbackKey.dir.x) * Mathf.Rad2Deg);

                /// Projectile instanciate
                {
                    var projectileInstance = ecb.Instantiate(ProjectilePrefab);
                    ecb.SetComponent<Ocs1SinSawSquareFactorOverride>(projectileInstance, new Ocs1SinSawSquareFactorOverride { Value = ActiveSynth.Osc1SinSawSquareFactor });
                    ecb.SetComponent<Ocs2SinSawSquareFactorOverride>(projectileInstance, new Ocs2SinSawSquareFactorOverride { Value = ActiveSynth.Osc2SinSawSquareFactor });
                  
                    var projectileLTW = LocalTransform.FromPosition(new float3(Wtrans.ValueRO.Position.x, Wtrans.ValueRO.Position.y, -3));
                    ecb.SetComponent<LocalTransform>(projectileInstance, projectileLTW);

                    ecb.SetComponent<PhyBodyData>(projectileInstance, new PhyBodyData
                    {
                        AngularDamp = 0,
                        LinearDamp = 0,
                        Velocity = dirLenght.normalized * projectileData.ValueRO.Speed
                    });
                   ecb.AddComponent<ProjectileInstanceData>(projectileInstance, new ProjectileInstanceData
                    {
                       damage = projectileData.ValueRO.Damage,
                       remainingLifeTime = projectileData.ValueRO.LifeTime,
                       speed = projectileData.ValueRO.Speed,
                       penetrationCapacity = 3
                   });
                }

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


            if (playback_data.ValueRW.PlaybackTime + SystemAPI.Time.DeltaTime > AudioLayoutStorageHolder.audioLayoutStorage.PlaybackAudioBundles[playback_data.ValueRO.FullPlaybackIndex.x].PlaybackDuration)
            {
                AudioLayoutStorageHolder.audioLayoutStorage.PlaybackContextResetRequired.Enqueue(playback_data.ValueRO.FullPlaybackIndex.x);

                playback_data.ValueRW.PlaybackTime = (playback_data.ValueRO.PlaybackTime + SystemAPI.Time.DeltaTime) - AudioLayoutStorageHolder.audioLayoutStorage.PlaybackAudioBundles[playback_data.ValueRO.FullPlaybackIndex.x].PlaybackDuration;
                playback_data.ValueRW.PlaybackElementIndex = 0;
                playback_data.ValueRW.KeysPlayed = 0;
                SkeyBuffer.Clear();
                RkeyBuffer.Clear();
                
            }
            else
                playback_data.ValueRW.PlaybackTime += SystemAPI.Time.DeltaTime;
            PreviousFrameDelta = SystemAPI.Time.DeltaTime;



            /// Delta/amplitude incrementing
            for (int i = 0; i < SkeyBuffer.Length; i++)
            {
                //Vector2 dirLenght = ActiveSynth.Legato ? direction : SkeyBuffer[i].DirLenght;
                Vector2 dirLenght = PhysicsUtilities.Rotatelerp(SkeyBuffer[i].StartDirLenght, SkeyBuffer[i].DirLenght, -Mathf.Log(ActiveSynth.Portomento / 3) * 0.01f + 0.03f);
                //Vector2 localDirLenght = math.mul(math.inverse(parentTransform.Rotation), new float3(dirLenght, 0)).xy;
                ///// set rotation of entity
                //{
                //    trans.ValueRW.Rotation = Quaternion.Euler(0, 0, Mathf.Atan2(-SkeyBuffer[i].DirLenght.y, -SkeyBuffer[i].DirLenght.x) * Mathf.Rad2Deg);
                //}

                float newDelta = SkeyBuffer[i].Delta + SystemAPI.Time.DeltaTime;
                Filter newFilter = new Filter(0, 0);

                newFilter.Cutoff = newDelta < ActiveSynth.filterADSR.Attack ?
                 ActiveSynth.filter.Cutoff + (ActiveSynth.filterEnvelopeAmount * (newDelta / ActiveSynth.filterADSR.Attack)) :
                 ActiveSynth.filter.Cutoff + (ActiveSynth.filterEnvelopeAmount * (1 - (Mathf.Min(ActiveSynth.filterADSR.Attack + ActiveSynth.filterADSR.Decay, newDelta) - ActiveSynth.filterADSR.Attack) / ActiveSynth.filterADSR.Decay) * (1 - ActiveSynth.filterADSR.Sustain) + (ActiveSynth.filterADSR.Sustain * ActiveSynth.filterEnvelopeAmount));

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


            }
            for (int i = 0; i < RkeyBuffer.Length; i++)
            {

                //Vector2 localDirLenght = math.mul(math.inverse(parentTransform.Rotation), new float3(RkeyBuffer[i].DirLenght, 0)).xy;

                Vector2 raycastDirlenght = math.mul(parentTransform.Rotation, new float3(RkeyBuffer[i].DirLenght.x, RkeyBuffer[i].DirLenght.y, 0)).xy;

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
                
            }

        }


        /// OPTI ? allocation each frame
        NativeList<(ushort, float)> drumMachineRequests = new NativeList<(ushort, float)>(3, Allocator.Temp);

        foreach (var (playback_data, Wtrans, trans, drumMachineData, entity) in SystemAPI.Query<RefRW<PlaybackData>, RefRO<LocalToWorld>, RefRW<LocalTransform>, RefRO<DrumMachineData>>().WithEntityAccess().WithAll<DrumMachineData>())
        {
            int playbackPadIndex = playback_data.ValueRO.PlaybackElementIndex;

            var parentEntity = SystemAPI.GetComponent<Parent>(entity).Value;
            var parentTransform = SystemAPI.GetComponent<LocalToWorld>(parentEntity);

            MachineDrumPlaybackAudioBundle playbackAudioBundle = AudioManager.Instance.uiPlaybacksHolder.machineDrumFullBundleLists[playback_data.ValueRO.FullPlaybackIndex.x][playback_data.ValueRO.FullPlaybackIndex.y].playbackAudioBundle;

            /// OPTI ?
            while (playbackPadIndex < playbackAudioBundle.PlaybackPads.Length
                && playbackAudioBundle.PlaybackPads[playbackPadIndex].time < playback_data.ValueRO.PlaybackTime)
            {
                PlaybackPad playbackPad = playbackAudioBundle.PlaybackPads[playbackPadIndex];
                /// went past the latest active key
                
                MachineDrumSystem.PlayDrumMachine(state.EntityManager, ecb, damageEventEntityQuery,StunManagerEntityQuery, 
                    drumMachineRequests,playbackPad.instrumentIdx, parentTransform.Position.xy);

                playbackPadIndex++;
                    playback_data.ValueRW.PlaybackElementIndex = playbackPadIndex;
                
                if (playbackPadIndex >= playbackAudioBundle.PlaybackPads.Length)
                    break;
            }
            if (playback_data.ValueRW.PlaybackTime + SystemAPI.Time.DeltaTime > playbackAudioBundle.PlaybackDuration)
            {
                playback_data.ValueRW.PlaybackTime = (playback_data.ValueRO.PlaybackTime + SystemAPI.Time.DeltaTime) - playbackAudioBundle.PlaybackDuration;
                playback_data.ValueRW.PlaybackElementIndex = 0;
                playback_data.ValueRW.KeysPlayed = 0;
            }
            else
                playback_data.ValueRW.PlaybackTime += SystemAPI.Time.DeltaTime;
            PreviousFrameDelta = SystemAPI.Time.DeltaTime;

        }

        AudioManager.Instance.PlayRequestedDMachineSounds(drumMachineRequests);
        UIManager.Instance.PlayRequestedDMachineEffects(drumMachineRequests);

        drumMachineRequests.Dispose();



    }
}
