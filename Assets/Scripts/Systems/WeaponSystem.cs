using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using Unity.Transforms;
using UnityEngine;
using MusicNamespace;

[UpdateInGroup(typeof(GameSimulationSystemGroup))]
[UpdateAfter(typeof(PhyResolutionSystem))]
public partial struct WeaponSystem : ISystem
{

    public float BeatCooldown;

    EntityCommandBuffer ECB;

    //temp -> put on component
    public static MusicUtils.MusicalMode mode;

    private NativeArray<float> Rythms;


    //private EntityQuery CirclesShapesQuery;

    void OnCreate(ref SystemState state)
    {

        //CirclesShapesQuery = state.GetEntityQuery(typeof(PhyBodyData), typeof(CircleShapeData));
        //state.RequireAnyForUpdate(PhyResolutionSystem.CirclesShapesQuery);
        BeatCooldown = MusicUtils.BPM;
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


    void OnUpdate(ref SystemState state)
    {


        BeatCooldown -= MusicUtils.BPM * SystemAPI.Time.DeltaTime;

        /// Think of something better -> ADSR ? -> waveform stacking problem 
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
           

            foreach (var (trans, weapon) in SystemAPI.Query<RefRO<LocalTransform>, RefRO<WeaponData>>())
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

            float newRythm = Rythms[Random.Range(0,3)];


            BeatCooldown = newRythm;
            //Debug.Log(BeatCooldown);
        }




    }


}