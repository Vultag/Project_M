using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Xml.Linq;
using NUnit.Framework;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Entities.UniversalDelegates;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Scripting;

public struct CollisionPair
{
    public Entity BodyA;
    public Entity BodyB;
}


[UpdateInGroup(typeof(GameSimulationSystemGroup), OrderLast = true)]
[UpdateBefore(typeof(ApplyPhysicsSystem))]
//[UpdateInGroup(typeof(LateSimulationSystemGroup), OrderLast = true)]
//[UpdateInGroup(typeof(FixedStepSimulationSystemGroup))]

public partial struct PhyResolutionSystem : ISystem, ISystemStartStop
{

    //private EntityQuery CirclesBodiesQuery;

    public NativeList<CollisionPair> ColPair;
    //public NativeQueue<InvalidatedNode> InvalidNodes;



    void OnCreate(ref SystemState state)
    {
        
        TreeInsersionSystem.CirclesShapesQuery = state.GetEntityQuery(typeof(PhyBodyData), typeof(CircleShapeData));
        state.RequireAnyForUpdate(TreeInsersionSystem.CirclesShapesQuery);

        /* 500 is arbitrary : needs resize if invalides nodes is >500 (bad) */
        //InvalidNodes = new NativeQueue<InvalidatedNode>(Allocator.Persistent);

    }


    void ISystemStartStop.OnStartRunning(ref SystemState state)
    {

        //NativeArray<CircleShapeData> CirclesShapes = CirclesShapesQuery.ToComponentDataArray<CircleShapeData>(Allocator.Temp);

        //need to pass the result of the job to the component data : use this or do foreach loop ?
        //NativeArray<PhyBodyData> CirclesBodies = CirclesShapesQuery.ToComponentDataArray<PhyBodyData>(Allocator.Temp);



    }



    void ISystemStartStop.OnStopRunning(ref SystemState state)
    {
        //throw new NotImplementedException();
    }

    void OnUpdate(ref SystemState state)
    {

        var AABBtree = TreeInsersionSystem.AABBtree;
        //float AABBfat = TreeInsersionSystem.AABBfat;

        //Debug.Log("start frame");

        //not needed anymore ?
        //NativeArray <CircleShapeData> CirclesShapes = TreeInsersionSystem.CirclesShapesQuery.ToComponentDataArray<CircleShapeData>(Allocator.TempJob);

        //NativeArray<Vector2> CirclesNewVels = new NativeArray<Vector2>(TreeInsersionSystem.CirclesShapesQuery.CalculateEntityCount(), Allocator.TempJob);

        //NativeQueue<int> comparequeue = new NativeQueue<int>(Allocator.Temp);

        //Debug.Log("here");

        //Debug.Log(AABBtree.nodes.Length);



        //unnessesary ? undo ? -> into func
        //while (InvalidNodes.Count>0)
        //{
        //    InvalidatedNode node = InvalidNodes.Dequeue();

        //    //tie fat to velocity ?
        //    //remove and reinsert the node with the new aabb + fat
        //    AABBtree.RemoveLeaf(node.treeIndex);

        //    /*used to compare nodes for insert*/
        //    NativeQueue<int> comparequeue = new NativeQueue<int>(Allocator.Temp);
        //    AABBtree.InsertLeaf(node.bodyIndex,
        //         new AABB
        //         {
        //             UpperBound = node.box.UpperBound + new Vector2(AABBfat, AABBfat),
        //             LowerBound = node.box.LowerBound - new Vector2(AABBfat, AABBfat)
        //         },
        //         comparequeue
        //         );

        //    comparequeue.Clear();
        //    comparequeue.Dispose();
        //}








        /*
            The 500 as Internal Capacity is arbitrary.
            It will need to realocate memory if the collisions are < 500
            tweak it ?
        OPTI
        Check with this :
        Debug.Log(ColPair.Length);
        Debug.Log(AABBtree.GatherIntersectingNodes(AABBtree.rootIndex));
        var test = AABBtree.GatherIntersectingNodes(AABBtree.rootIndex);
            */
        ColPair = new NativeList<CollisionPair>(500, Allocator.TempJob);

        AABBtree.GatherIntersectingNodes(ColPair, AABBtree.rootIndex);
        //Debug.Log(ColPair.Length);

        CircleCollisionResolutionJob CircleColJob = new CircleCollisionResolutionJob()
        {
            //CirclesBodies = CirclesBodies,
            CircleShapes = SystemAPI.GetComponentLookup<CircleShapeData>(true),
            CircleBodies = SystemAPI.GetComponentLookup<PhyBodyData>(false),
            ColPair = ColPair,
            //CirclesNewVels = CirclesNewVels,

        };
        //change batch size ? OPTI
        JobHandle ColJobHandle = CircleColJob.Schedule(ColPair.Length, 64);
        ColJobHandle.Complete();

        ColPair.Dispose();




        /* OLD circle collision detection job
        CircleCollisionDetectionJob CircleColCheckJob = new CircleCollisionDetectionJob()
        {
            //CirclesBodies = CirclesBodies,
            CirclesShapes = CirclesShapes,
            CirclesNewVels = CirclesNewVels,

        };
        //change batch size ? OPTI
        JobHandle ColDetectJob =  CircleColCheckJob.Schedule(CirclesShapes.Length-1,64);
        ColDetectJob.Complete();
        */





        //Debug.Log(CirclesNewVels[0]);
        //Debug.Log(CirclesNewVels[1]);

        //pass the velocities to the phybody components for future physics resolve
        //CircleApplyForcesJob circleApplyForcesJob = new CircleApplyForcesJob()
        //{
        //    CirclesNewVels = CirclesNewVels,

        //};
        //circleApplyForcesJob.Run(CirclesShapesQuery);


        //CirclesShapes.Dispose();
        //CirclesBodies.Dispose();

    }


}

//[BurstCompile]
//public partial struct CircleApplyForcesJob : IJobEntity//IJob //ijobparralel ?
//{
//    [DeallocateOnJobCompletion]
//    [ReadOnly]
//    public NativeArray<Vector2> CirclesNewVels;

//    public void Execute([EntityIndexInQuery] int sortKey,ref PhyBodyData body)
//    {
//        body.Velocity += CirclesNewVels[sortKey];
//        //Debug.Log(body.Velocity);
//    }
//}

//TOO EXPENSIVE ? REWORK ? OPTI
[BurstCompile]
public partial struct CircleCollisionResolutionJob : IJobParallelFor//IJob //ijobparralel ?
{
    //[DeallocateOnJobCompletion]
    [ReadOnly]
    public ComponentLookup<CircleShapeData> CircleShapes;
    [ReadOnly]
    public NativeList<CollisionPair> ColPair;
    //public NativeArray<CircleShapeData> CirclesShapes;


    [NativeDisableParallelForRestriction]
    public ComponentLookup<PhyBodyData> CircleBodies;


    //[NativeDisableParallelForRestriction]
    //public NativeArray<Vector2> CirclesNewVels;

    public void Execute(int i)
    {

        //Debug.Log(CirclesShapes.Length);

        var a = CircleShapes.GetRefRO(ColPair[i].BodyA);
        var b = CircleShapes.GetRefRO(ColPair[i].BodyB);

        var newvelbodyA = CircleBodies.GetRefRW(ColPair[i].BodyA);
        var newvelbodyB = CircleBodies.GetRefRW(ColPair[i].BodyB);

        //Entity b = ColPair[i].BodyB;

        //math class ?? OPTI
        var distance = math.distance(a.ValueRO.Position, b.ValueRO.Position);
        var radii = a.ValueRO.radius + b.ValueRO.radius;
        if (distance < radii)
        {

            //CircleBodies.GetRefRW(ColPair[i].BodyA).ValueRW.Velocity

            //CirclesNewVels[] += (-(b.ValueRO.Position - a.ValueRO.Position).normalized * (radii - distance)) * 0.5f;
            //CirclesNewVels[] += ((b.ValueRO.Position - a.ValueRO.Position).normalized * (radii - distance)) * 0.5f;

            newvelbodyA.ValueRW.Velocity += (-(b.ValueRO.Position - a.ValueRO.Position).normalized * (radii - distance)) * 0.5f;
            newvelbodyB.ValueRW.Velocity += ((b.ValueRO.Position - a.ValueRO.Position).normalized * (radii - distance)) * 0.5f;

        }

        
    }

}

//retreve into a native array the resulting of the colision velocities OLD
//    [BurstCompile]
//public partial struct CircleCollisionDetectionJob : IJobParallelFor//IJob //ijobparralel ?
//{
//    [DeallocateOnJobCompletion]
//    [ReadOnly]
//    public NativeArray<CircleShapeData> CirclesShapes;
//    //[WriteOnly]
//    [NativeDisableParallelForRestriction]
//    public NativeArray<Vector2> CirclesNewVels;

//    public void Execute(int i)
//    {
//        //nested loop optimization : https://www.youtube.com/watch?v=t31yhVY-oz4

//        for (int j = 1; j < CirclesShapes.Length; j++)
//        {

//            var distance = math.distance(CirclesShapes[i].Position, CirclesShapes[j].Position);
//            var radii = CirclesShapes[i].radius + CirclesShapes[j].radius;
//            if (distance < radii)
//            {


//                CirclesNewVels[i] += (-(CirclesShapes[j].Position - CirclesShapes[i].Position).normalized * (radii - distance)) * 0.5f;
//                CirclesNewVels[j] += ((CirclesShapes[j].Position - CirclesShapes[i].Position).normalized * (radii - distance)) * 0.5f;

//            }

//        }
//    }

//}