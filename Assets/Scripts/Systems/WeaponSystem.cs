using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using Unity.Transforms;
using UnityEngine;
using MusicNamespace;
using Unity.Mathematics;
using Unity.Entities.UniversalDelegates;

[UpdateInGroup(typeof(GameSimulationSystemGroup))]
[UpdateAfter(typeof(PhyResolutionSystem))]
public partial class WeaponSystem : SystemBase
{

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


    //private EntityQuery CirclesShapesQuery;

    protected override void OnCreate()
    {

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


        DynamicBuffer<SustainedKeyBufferData> SkeyBuffer = SystemAPI.GetBuffer<SustainedKeyBufferData>(SystemAPI.GetSingletonEntity<SynthData>());
        DynamicBuffer<ReleasedKeyBufferData> RkeyBuffer = SystemAPI.GetBuffer<ReleasedKeyBufferData>(SystemAPI.GetSingletonEntity<SynthData>());

        //TO MODIFY
        var ActiveSynth = SystemAPI.GetSingleton<SynthData>();

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
        }

        /// Move to Synth system all together ?
        foreach (var (Wtrans, trans, synth) in SystemAPI.Query<RefRO<LocalToWorld>, RefRW<LocalTransform>, RefRO<SynthData>>())
        {

            if (!IsShooting)
            {
                mousepos = Camera.main.ScreenToWorldPoint(PlayerSystem.mousePos);
                Vector2 dir = ((new Vector2(Wtrans.ValueRO.Position.x, Wtrans.ValueRO.Position.y)) - (mousepos)).normalized;

                trans.ValueRW.Rotation = Quaternion.Euler(0, 0, Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg);
            }

            if (PlayPressed)
            {
                //if (BeatProximity < BeatProximityThreshold)
                {
                    Vector2 direction = mousepos - new Vector2(Wtrans.ValueRO.Position.x, Wtrans.ValueRO.Position.y);
                    float randian = Mathf.Abs(PhysicsUtilities.DirectionToRadians(direction));
                    int note = MusicUtils.radiansToNote(randian);
                    //float frequency = MusicUtils.noteToFrequency(note, mode);
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
                            //reset delta ?
                            ///PlayedKeyIndex = i;
                            // sudden change in amplitude -> poping
                            ///SkeyBuffer[i] = new SustainedKeyBufferData { frequency = MusicUtils.noteToFrequency(note, mode), Delta = 0, Phase = SkeyBuffer[i].Phase};
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

            /// Damage processing
            for (int i = 0; i < SkeyBuffer.Length; i++)
            {

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
           

            }


        }



        #region Survivor's like weapon approche
        /*
        /// Think of something better -> ADSR ? -> waveform stacking problem 
        /// 

        if (BeatCooldown <= 0)
        {

            //float newRythm = Rythms[UnityEngine.Random.Range(0, 3)];
            float newRythm = Rythms[2];

            BeatCooldown = newRythm;
        }


        if (BeatCooldown < 3 && BeatCooldown > 0)
        {

            //Debug.Log("stop");

            var test = SystemAPI.GetSingleton<SynthData>();//.amplitude = 0.2f;

            test.amplitude = 0f;
            SystemAPI.SetSingleton<SynthData>(test);
        }
     

        if (BeatCooldown <= 0)
        {

        

            ECB = SystemAPI.GetSingleton<BeginSimulationEntityCommandBufferSystem.Singleton>().CreateCommandBuffer(state.WorldUnmanaged);
           

            foreach (var (trans, weapon) in SystemAPI.Query<RefRW<LocalTransform>, RefRO<WeaponData>>())
            {
                //Debug.Log("cast");

                CircleShapeData CastSphere = new CircleShapeData
                {
                    Position = new Vector2(trans.ValueRO.Position.x, trans.ValueRO.Position.y),
                    radius = weapon.ValueRO.tempShootRange
                };


                //NativeList<Entity> BodyHitList = PhysicsCalls.GatherOverlappingNodes(CastSphere,ComponentFilter);


                NativeList<Entity> MonsterHitList = PhysicsCalls.GatherOverlappingNodes(CastSphere,PhysicsUtilities.CollisionLayer.MonsterLayer);

                ///ICI LE SETUP POUR LE BUFFER DE NOTES

                var testbugffer = SystemAPI.GetBuffer<KeyBufferData>(SystemAPI.GetSingletonEntity<SynthData>());

                //for i in testbuffer.lenght:
                //testbugffer.ElementAt(0).test == "frequecy already playing"
                //if playing -> just reset delta of buffer element
                //if not -> add new element with frequecy/delta = 0;

                //testbugffer.Add(new KeyBufferData { test = 0 });

                //Debug.Log(testbugffer.Length);

                if (!MonsterHitList.IsEmpty)
                {

      

                    ///rempla

                    var test = SystemAPI.GetSingleton<SynthData>();//.amplitude = 0.2f;

                    test.amplitude = 0.15f;
                    float radians = Mathf.Abs(PhysicsUtilities.DirectionToRadians(SystemAPI.GetComponent<CircleShapeData>(MonsterHitList[0]).Position - new Vector2(trans.ValueRO.Position.x, trans.ValueRO.Position.y)));

                    //float key = MusicUtils.getNearestKey(angle + 200f + 32.7032f);

                    int note = MusicUtils.radiansToNote(radians);
                    float key = MusicUtils.noteToFrequency(note, mode);

                    //Debug.LogError(radians);
                    //Debug.LogError(note);
                    //Debug.LogError(key);

                    test.frequency = key;

                    SystemAPI.SetSingleton<SynthData>(test);

                    //Debug.LogError(SystemAPI.GetComponent<CircleShapeData>(MonsterHitList[0]).Position.normalized);


                    //Debug.LogError("hit");
                    //Debug.LogError(PhysicsUtilities.Proximity(TreeInsersionSystem.AABBtree.nodes[0].box, CastSphere));

                    Debug.DrawLine(trans.ValueRO.Position, SystemAPI.GetComponent<CircleShapeData>(MonsterHitList[0]).Position, Color.yellow, 0.3f);

                    MonsterData newMonsterData = SystemAPI.GetComponent<MonsterData>(MonsterHitList[0]);
                    //float Rhealth = newMonsterData.Health;
                    ///remetre
                    newMonsterData.Health -= 3f;

                    if (newMonsterData.Health > 0)
                    {
                        //Debug.Log(newMonsterData.Health);
                        SystemAPI.SetComponent<MonsterData>(MonsterHitList[0], newMonsterData);
                    }
                    else
                    {
                        //Debug.Log("ded");
                        PhysicsCalls.DestroyPhysicsEntity(ECB, MonsterHitList[0]);
                    }

                }
                else
                {
                    //Debug.Log("did not overlap");
                    var test = SystemAPI.GetSingleton<SynthData>();//.amplitude = 0.2f;

                    test.amplitude = 0f;
                    SystemAPI.SetSingleton<SynthData>(test);
                }
                MonsterHitList.Dispose();

            }


            float newRythm = Rythms[UnityEngine.Random.Range(0,3)];


            BeatCooldown = newRythm;
            //Debug.Log(BeatCooldown);
        }
        */
        #endregion



    }

}