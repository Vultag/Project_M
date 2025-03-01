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
using UnityEngine;
using UnityEngine.Scripting;

public struct CollisionPair
{
    public Entity EntityA;
    public Entity EntityB;
}


[UpdateInGroup(typeof(FixedStepGameSimulationSystemGroup), OrderLast = true)]
[UpdateBefore(typeof(ApplyPhysicsSystem))]
public partial struct PhyResolutionSystem : ISystem, ISystemStartStop
{

    public NativeList<CollisionPair> ColPair;


    void OnCreate(ref SystemState state)
    {
        
        TreeInsersionSystem.CirclesShapesQuery = state.GetEntityQuery(typeof(PhyBodyData), typeof(CircleShapeData));
        state.RequireAnyForUpdate(TreeInsersionSystem.CirclesShapesQuery);

    }


    void ISystemStartStop.OnStartRunning(ref SystemState state)
    {
    }



    void ISystemStartStop.OnStopRunning(ref SystemState state)
    {
    }

    void OnUpdate(ref SystemState state)
    {
        //return;
        var AABBtree = TreeInsersionSystem.AABBtree;
        if (AABBtree.nodes.Length <2) return;

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

        /// JOB BURST THIS ? OPTI
        AABBtree.GatherIntersectingNodes(ref ColPair, AABBtree.rootIndex);

        CircleCollisionResolutionJob CircleColJob = new CircleCollisionResolutionJob()
        {
            CircleShapes = SystemAPI.GetComponentLookup<CircleShapeData>(false),
            CircleBodies = SystemAPI.GetComponentLookup<PhyBodyData>(false),
            ColPair = ColPair,
        };
        //change batch size ? OPTI
        JobHandle ColJobHandle = CircleColJob.Schedule(ColPair.Length, 64);
        ColJobHandle.Complete();

        ColPair.Dispose();


    }


}


//TOO EXPENSIVE ? REWORK ? OPTI
[BurstCompile]
public partial struct CircleCollisionResolutionJob : IJobParallelFor//IJob //ijobparralel ?
{
    [ReadOnly]
    public NativeList<CollisionPair> ColPair;

    [NativeDisableParallelForRestriction]
    public ComponentLookup<PhyBodyData> CircleBodies;
    [NativeDisableParallelForRestriction]
    public ComponentLookup<CircleShapeData> CircleShapes;


    public void Execute(int i)
    {
        Entity entityA = ColPair[i].EntityA;
        Entity entityB = ColPair[i].EntityB;

        var newvelshapeA = CircleShapes.GetRefRW(entityA);
        var newvelshapeB = CircleShapes.GetRefRW(entityB);

        var newvelbodyA = CircleBodies.GetRefRW(entityA);
        var newvelbodyB = CircleBodies.GetRefRW(entityB);


        var distance = math.distance(newvelshapeA.ValueRO.Position, newvelshapeB.ValueRO.Position);
        var radii = newvelshapeA.ValueRO.radius + newvelshapeB.ValueRO.radius;
        if (distance < radii)
        {

            //do mass balance
            if (newvelbodyA.ValueRO.Mass > newvelbodyB.ValueRO.Mass)
            {
                float massdif = newvelbodyB.ValueRO.Mass / newvelbodyA.ValueRO.Mass;

                newvelshapeA.ValueRW.Position += (-(newvelshapeB.ValueRO.Position - newvelshapeA.ValueRO.Position).normalized * (radii - distance)) * (0.5f * massdif);
                newvelshapeB.ValueRW.Position += ((newvelshapeB.ValueRO.Position - newvelshapeA.ValueRO.Position).normalized * (radii - distance)) * (1f - (0.5f * massdif)); 

            }
            else
            {
                float massdif = newvelbodyA.ValueRO.Mass / newvelbodyB.ValueRO.Mass;

                newvelshapeA.ValueRW.Position += (-(newvelshapeB.ValueRO.Position - newvelshapeA.ValueRO.Position).normalized * (radii - distance)) * (1f - (0.5f * massdif));
                newvelshapeB.ValueRW.Position += ((newvelshapeB.ValueRO.Position - newvelshapeA.ValueRO.Position).normalized * (radii - distance)) * (0.5f * massdif);
            }


            ///restitution response :
            ///boncyness
            float boncy = 1;

            Vector2 relativeVel = (newvelbodyA.ValueRO.Velocity+ newvelbodyA.ValueRO.Force) - (newvelbodyB.ValueRO.Velocity+ newvelbodyB.ValueRO.Force);
            Vector2 normal = (newvelshapeB.ValueRO.Position - newvelshapeA.ValueRO.Position).normalized;

            ///ref video :
            ///https://youtu.be/vQO_hPOE-1Y?list=PLSlpr6o9vURwq3oxVZSimY8iC-cdd3kIs&t=790

            float j = -(1f + boncy) * math.dot(relativeVel, normal);
            j /= (1f / newvelbodyA.ValueRO.Mass) + (1f / newvelbodyB.ValueRO.Mass);

            newvelbodyA.ValueRW.Velocity += j/newvelbodyA.ValueRO.Mass * normal;
            newvelbodyB.ValueRW.Velocity -= j / newvelbodyB.ValueRO.Mass * normal;

        }

        
    }

}
