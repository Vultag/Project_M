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


    //private EntityQuery CirclesShapesQuery;

    void OnCreate(ref SystemState state)
    {

        //CirclesShapesQuery = state.GetEntityQuery(typeof(PhyBodyData), typeof(CircleShapeData));
        //state.RequireAnyForUpdate(PhyResolutionSystem.CirclesShapesQuery);
        BeatCooldown = MusicUtils.BPM;
        //temp -> put on component
        mode = MusicUtils.MusicalMode.Lydian;
    }


    void OnUpdate(ref SystemState state)
    {


        BeatCooldown -= MusicUtils.BPM * SystemAPI.Time.DeltaTime;
        

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


                NativeList<Entity> MonsterHitList = PhysicsCalls.GatherOverlappingNodes(CastSphere);


                if (!MonsterHitList.IsEmpty)
                {
                    var test = SystemAPI.GetSingleton<SynthData>();//.amplitude = 0.2f;

                    test.amplitude = 0.15f;
                    float radians = PhysicsUtilities.DirectionToRadians(SystemAPI.GetComponent<CircleShapeData>(MonsterHitList[0]).Position - new Vector2(trans.ValueRO.Position.x, trans.ValueRO.Position.y));

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

                    //Debug.DrawLine(trans.ValueRO.Position, SystemAPI.GetComponent<CircleShapeData>(MonsterHitList[0]).Position, Color.yellow, 0.1f);

                    PhysicsCalls.DestroyPhysicsEntity(ECB, MonsterHitList[0]);
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

            BeatCooldown = 60;
            //Debug.Log(BeatCooldown);
        }




    }


}