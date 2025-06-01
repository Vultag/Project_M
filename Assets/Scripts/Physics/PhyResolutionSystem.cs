using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.Linq;
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
using UnityEditor.Search;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Scripting;
using static UnityEngine.EventSystems.EventTrigger;
using static UnityEngine.Rendering.DebugUI;
using static UnityEngine.Rendering.ProbeTouchupVolume;
using Color = UnityEngine.Color;

//public enum CollisionPairType
//{
//    CircleCirle,
//    CircleBox,
//    BoxCircle,
//    BoxBox
//}

//public struct CollisionPair
//{
//    public Entity EntityA;
//    public Entity EntityB;
//}
public struct CollisionPair : IEquatable<CollisionPair>
{
    public Entity EntityA;
    public Entity EntityB;

    public bool Equals(CollisionPair other)
    {
        return EntityA == other.EntityA && EntityB == other.EntityB;
    }

    public override int GetHashCode()
    {
        return (EntityA.GetHashCode() * 397) ^ EntityB.GetHashCode();
    }
}

//public struct CircleCircleColPair
//{
//    public Entity EntityA;
//    public Entity EntityB;
//}
//public struct CircleBoxColPair
//{
//    public Entity EntityA;
//    public Entity EntityB;
//}
//public struct BoxBoxColPair
//{
//    public Entity EntityA;
//    public Entity EntityB;
//}


public partial struct PhyResolutionSystem : ISystem//, ISystemStartStop
{

    //public NativeList<CollisionPair> ColPair;
    //public NativeList<CollisionPair> TriggerPair;


    public void OnCreate(ref SystemState state)
    {
        
        TreeInsersionSystem.CirclesShapesQuery = state.GetEntityQuery(typeof(PhyBodyData), typeof(CircleShapeData));
        state.RequireAnyForUpdate(TreeInsersionSystem.CirclesShapesQuery);

    }


    //void ISystemStartStop.OnStartRunning(ref SystemState state)
    //{
    //}



    //void ISystemStartStop.OnStopRunning(ref SystemState state)
    //{
    //}

    public void OnUpdate(ref SystemState state)
    {
        //return;
        var DynamicAABBtree = TreeInsersionSystem.DynamicBodiesAABBtree;
        var StaticAABBtree = TreeInsersionSystem.StaticBodiesAABBtree;
        if ((DynamicAABBtree.nodes.Length+ Mathf.Min(1,StaticAABBtree.nodes.Length)) < 2) return;

        /*
            The 500 as Internal Capacity is arbitrary.
            It will need to realocate memory if the collisions are < 500
            tweak it ?
            */
        /*
         * for triggers :
         * make TriggerPair it's own native list and pass it to GatherIntersectingNodes for filling ?
        */


        Entity triggerEventEntity = SystemAPI.GetSingletonEntity<TriggerEvent>();
        DynamicBuffer<TriggerEvent> triggerBuffer = SystemAPI.GetBuffer<TriggerEvent>(triggerEventEntity);

        /// Arbirary maximum of 300 Collision per FixedFrames
        NativeList<CollisionPair> ColPair = new NativeList<CollisionPair>(300, Allocator.Temp);
        NativeParallelHashSet<TriggerPair> triggerSet = new NativeParallelHashSet<TriggerPair>(100, Allocator.Temp);
        NativeList<CollisionPair> CircleCirlceColPair = new NativeList<CollisionPair>(100, Allocator.TempJob);
        NativeList<CollisionPair> CircleBoxColPair = new NativeList<CollisionPair>(100, Allocator.TempJob);
        NativeList<CollisionPair> BoxBoxColPair = new NativeList<CollisionPair>(100, Allocator.TempJob);


        /// Arbirary maximum of 60 trigger per FixedFrames
        var triggerEvents = new NativeList<TriggerEvent>(60,Allocator.TempJob);
        var processedTriggerPairs = new NativeParallelHashSet<CollisionPair>(60,Allocator.TempJob);

        /// JOB BURST THIS ? OPTI
        /// Inter Dynamic bodies col garthering
        DynamicAABBtree.GatherIntersectingNodes(ref ColPair, DynamicAABBtree.rootIndex);
        /// Static to Dynamic bodies col garthering
        StaticAABBtree.GatherIntersectingStaticNodes(ref ColPair, ref DynamicAABBtree, StaticAABBtree.rootIndex);

        var phyBodiesLookUp = SystemAPI.GetComponentLookup<PhyBodyData>(false);
        var phyShapesLookUp = SystemAPI.GetComponentLookup<ShapeData>(false);


        foreach (var pair in ColPair)
        {
            var shapeA = phyShapesLookUp.GetRefRO(pair.EntityA).ValueRO.shapeType;
            var shapeB = phyShapesLookUp.GetRefRO(pair.EntityB).ValueRO.shapeType;

            // Create dispatch key (sorted or unsorted depending on symmetry)
            /// Map pair to int and use flat switch (int) -> This gives you true jump - table dispatch, which is often faster. (int shapeCombo = ((int)shapeA << 2) | (int)shapeB;)
            switch ((shapeA, shapeB))
            {
                case (ShapeType.Circle, ShapeType.Circle):
                    CircleCirlceColPair.Add(pair);
                    break;
                case (ShapeType.Box, ShapeType.Circle):
                    CircleBoxColPair.Add(new CollisionPair { EntityA = pair.EntityB, EntityB = pair.EntityA});
                    break;
                case (ShapeType.Circle, ShapeType.Box):
                    CircleBoxColPair.Add(pair);
                    break;
                case (ShapeType.Box, ShapeType.Box):
                    BoxBoxColPair.Add(pair);
                    break;
                 /// ... Do polygone ?
            }
        }
        /// pairs dispached. Can dispose ?
        ColPair.Dispose();

        var circleShapeLookUp = SystemAPI.GetComponentLookup<CircleShapeData>(false);
        var boxShapeLookUp = SystemAPI.GetComponentLookup<BoxShapeData>(false);

        /// TO DO : 
        /// speculative contact ? : extended pairs for a more stable resolution over the iterations
        /// -> already handled by the AABB fat ?

        const short SloverIteration = 6; 
        JobHandle dep = default;

        for (int i = 0; i < SloverIteration; i++)
        {


            JobHandle CircleVsCircleJobHandle = new CircleVsCircleCollisionResolutionJob()
            {
                CircleShapes = circleShapeLookUp,
                bodies = phyBodiesLookUp,
                Shapes = phyShapesLookUp,
                ColPair = CircleCirlceColPair,
                triggerEvents = triggerEvents.AsParallelWriter(),
                SolverIteration = SloverIteration,
                processedTriggerPairs = processedTriggerPairs,
            }.Schedule(CircleCirlceColPair.Length, 64, dep);

            JobHandle CircleVsBoxJobHandle = new CircleVsBoxImpulseResolutionJob()
            {
                CircleShapes = circleShapeLookUp,
                BoxShapes = boxShapeLookUp,
                Bodies = phyBodiesLookUp,
                Shapes = phyShapesLookUp,
                ColPair = CircleBoxColPair,
                triggerEvents = triggerEvents.AsParallelWriter(),
                SolverIteration = SloverIteration,
                processedTriggerPairs = processedTriggerPairs,
            }.Schedule(CircleBoxColPair.Length, 64, CircleVsCircleJobHandle);

            JobHandle BoxVsBoxJobHandle = new BoxVsBoxImpulseResolutionJob()
            {
                BoxShapes = boxShapeLookUp,
                Bodies = phyBodiesLookUp,
                Shapes = phyShapesLookUp,
                ColPair = BoxBoxColPair,
                triggerEvents = triggerEvents.AsParallelWriter(),
                SolverIteration = SloverIteration,
                processedTriggerPairs = processedTriggerPairs,
            }.Schedule(BoxBoxColPair.Length, 64, CircleVsBoxJobHandle);

            dep = BoxVsBoxJobHandle;
        }

        dep.Complete();



        // Batch write to buffer (avoids multiple buffer resizes)
        triggerBuffer.AddRange(triggerEvents.AsArray());

        processedTriggerPairs.Dispose();
        triggerSet.Dispose();
        CircleCirlceColPair.Dispose();
        CircleBoxColPair.Dispose();
        BoxBoxColPair.Dispose();
        triggerEvents.Dispose();

    }


}


[BurstCompile]
public partial struct CircleVsCircleCollisionResolutionJob : IJobParallelFor//IJob //ijobparralel ?
{
    [ReadOnly]
    public NativeList<CollisionPair> ColPair;
    [WriteOnly]
    public NativeList<TriggerEvent>.ParallelWriter triggerEvents;

    [NativeDisableParallelForRestriction]
    public ComponentLookup<PhyBodyData> bodies;
    [ReadOnly]
    public ComponentLookup<CircleShapeData> CircleShapes;
    [NativeDisableParallelForRestriction]
    public ComponentLookup<ShapeData> Shapes;

    [ReadOnly] public short SolverIteration;

    [NativeDisableParallelForRestriction]
    public NativeParallelHashSet<CollisionPair> processedTriggerPairs;

    public void Execute(int i)
    {
        Entity entityA = ColPair[i].EntityA;
        Entity entityB = ColPair[i].EntityB;

        var circleShapeA = CircleShapes.GetRefRO(entityA);
        var circleShapeB = CircleShapes.GetRefRO(entityB);

        var shapeA = Shapes.GetRefRW(entityA);
        var shapeB = Shapes.GetRefRW(entityB);

        var posA = shapeA.ValueRO.Position;
        var posB = shapeB.ValueRO.Position;
        var delta = posB - posA;
        var distSq = math.lengthsq(delta);
        var radii = circleShapeA.ValueRO.radius + circleShapeB.ValueRO.radius;
        var radiiSq = radii * radii;

        if (!(distSq < radiiSq))
            return; 

        float dist = math.length(delta);
        Vector2 normal = delta / dist;
        float penetration = radii - dist;

        //bool TriggerEvent = newvelshapeA.ValueRO.IsTrigger | newvelshapeB.ValueRO.IsTrigger;
        bool Dynamics = shapeA.ValueRO.HasDynamics & shapeB.ValueRO.HasDynamics;

        /// Trigger event collecting
        /// SOME UNINTENDED EVENT MIGHT BE ADDED WHEN TWO TRIGGER -> FIX
        if (shapeA.ValueRO.IsTrigger)
        {
            ///(Add() returns true if the element was not already present!)
            if (!processedTriggerPairs.Add(ColPair[i]))
            {
                triggerEvents.AddNoResize(new TriggerEvent { EmitterEntity = entityA, ReciverEntity = entityB });
            }
        }
        if (shapeB.ValueRO.IsTrigger)
        {
            var col = new CollisionPair { EntityA = ColPair[i].EntityB, EntityB = ColPair[i].EntityA };
            ///(Add() returns true if the element was not already present!)
            if (!processedTriggerPairs.Add(col))
            {
                triggerEvents.AddNoResize(new TriggerEvent { EmitterEntity = entityB, ReciverEntity = entityA });
            }
        }
        /// Both entities are Dynamic, do physics calculation
        if (Dynamics)
        {

            var bodyA = bodies.GetRefRW(entityA);
            var bodyB = bodies.GetRefRW(entityB);

            float massA = bodyA.ValueRO.Mass;
            float massB = bodyB.ValueRO.Mass;

            float invMassA = bodyA.ValueRO.InvMass;
            float invMassB = bodyB.ValueRO.InvMass;

            /// position correction 
            Vector2 correction = (penetration * normal) / SolverIteration;
            float totalInverseMass = invMassA + invMassB;
            float Afactor = 0f;
            float Bfactor = 0f;
            if (totalInverseMass > 0f)
            {
                Afactor = invMassA / totalInverseMass;
                Bfactor = (1 - Afactor);
            }
            shapeA.ValueRW.Position += -correction * Afactor;
            shapeB.ValueRW.Position += correction * Bfactor;

            float restitution = Mathf.Min(bodyA.ValueRO.Restitution, bodyB.ValueRO.Restitution);

            Vector2 relativeVel = (bodyB.ValueRO.Velocity) - (bodyA.ValueRO.Velocity);

            float j = -(1f + restitution) * math.dot(relativeVel, normal);
            j /= (bodyA.ValueRO.InvMass) + (bodyB.ValueRO.InvMass);

            bodyA.ValueRW.Velocity += -j * invMassA * normal;
            bodyB.ValueRW.Velocity += j * invMassB * normal;
        }
        

        
    }

}


[BurstCompile]
public partial struct CircleVsBoxImpulseResolutionJob : IJobParallelFor
{
    [ReadOnly] public NativeList<CollisionPair> ColPair;
    [WriteOnly] public NativeList<TriggerEvent>.ParallelWriter triggerEvents;

    [ReadOnly] public ComponentLookup<CircleShapeData> CircleShapes;
    [ReadOnly] public ComponentLookup<BoxShapeData> BoxShapes;

    [NativeDisableParallelForRestriction] public ComponentLookup<ShapeData> Shapes;
    [NativeDisableParallelForRestriction] public ComponentLookup<PhyBodyData> Bodies;

    [ReadOnly] public short SolverIteration;

    [NativeDisableParallelForRestriction]
    public NativeParallelHashSet<CollisionPair> processedTriggerPairs;
    public void Execute(int index)
    {
        Entity entityA = ColPair[index].EntityA;
        Entity entityB = ColPair[index].EntityB;

        var circleEntity = entityA;
        var boxEntity = entityB;

        RefRW<ShapeData> shapeA = Shapes.GetRefRW(circleEntity);
        RefRW<ShapeData> shapeB = Shapes.GetRefRW(boxEntity);

        RefRO<CircleShapeData> circle = CircleShapes.GetRefRO(circleEntity);
        RefRO<BoxShapeData> box = BoxShapes.GetRefRO(boxEntity);

        float2 circlePos = shapeA.ValueRO.Position;
        float2 boxPos = shapeB.ValueRO.Position;

        // Compute rotation matrix from rotation angle
        float2x2 rotMatrix = AngleToMatrix(shapeB.ValueRO.Rotation* Mathf.Deg2Rad);
        float2x2 invRotMatrix = math.transpose(rotMatrix);

        float2 boxHalfExtents = box.ValueRO.dimentions * 0.5f;

        float2 rel = circlePos - boxPos;
        float2 localCircle = math.mul(invRotMatrix, rel);
        float2 closestLocal = math.clamp(localCircle, -boxHalfExtents, boxHalfExtents);
        float2 closestWorldOffset = math.mul(rotMatrix, closestLocal);
        float2 closestPoint = boxPos + closestWorldOffset;

        float2 diff = circlePos - closestPoint;
        float distSq = math.lengthsq(diff);
        float radius = circle.ValueRO.radius;

        if (distSq >= radius * radius)
            return;

        float dist = Mathf.Max(1e-6f, math.sqrt(distSq));
        float2 normal = diff / dist;
        float penetration = radius - dist;

        /// Trigger event collecting
        /// SOME UNINTENDED EVENT MIGHT BE ADDED WHEN TWO TRIGGER -> FIX
        if (shapeA.ValueRO.IsTrigger)
        {
            ///(Add() returns true if the element was not already present!)
            if (!processedTriggerPairs.Add(ColPair[index]))
            {
                triggerEvents.AddNoResize(new TriggerEvent { EmitterEntity = entityA, ReciverEntity = entityB });
            }
        }
        if (shapeB.ValueRO.IsTrigger)
        {
            var col = new CollisionPair { EntityA = ColPair[index].EntityB, EntityB = ColPair[index].EntityA };
            ///(Add() returns true if the element was not already present!)
            if (!processedTriggerPairs.Add(col))
            {
                triggerEvents.AddNoResize(new TriggerEvent { EmitterEntity = entityB, ReciverEntity = entityA });
            }
        }

        if (!shapeA.ValueRO.HasDynamics || !shapeB.ValueRO.HasDynamics)
            return;

        ///https://youtu.be/VbvdoLQQUPs

        RefRW<PhyBodyData> bodyCircle = Bodies.GetRefRW(circleEntity);
        RefRW<PhyBodyData> bodyBox = Bodies.GetRefRW(boxEntity);

        float2 velA = bodyCircle.ValueRO.Velocity;
        float2 velB = bodyBox.ValueRO.Velocity;

        float angVelA = bodyCircle.ValueRO.AngularVelocity;
        float angVelB = bodyBox.ValueRO.AngularVelocity;

        float massA = bodyCircle.ValueRO.Mass;
        float massB = bodyBox.ValueRO.Mass;

        float invMassA = bodyCircle.ValueRO.InvMass;
        float invMassB = bodyBox.ValueRO.InvMass;

        float invInertiaA = bodyCircle.ValueRO.Inertia > 0f ? 1f / bodyCircle.ValueRO.Inertia : 0f;
        float invInertiaB = bodyBox.ValueRO.Inertia > 0f ? 1f / bodyBox.ValueRO.Inertia : 0f;

        /// position correction 
        Vector2 correction = (penetration * normal) / SolverIteration;
        float totalInverseMass = invMassA + invMassB;
        float Afactor = 0f;
        float Bfactor = 0f;
        if (totalInverseMass > 0f)
        {
            Afactor = invMassA / totalInverseMass;
            Bfactor = (1 - Afactor);
        }
        shapeA.ValueRW.Position += correction * Afactor;
        shapeB.ValueRW.Position += -correction * Bfactor;
        circlePos = shapeA.ValueRO.Position;
        boxPos = shapeB.ValueRO.Position;


        float2 contactPoint = closestPoint;
        float2 rA = contactPoint - circlePos;
        float2 rB = contactPoint - boxPos;

        float2 raPerp = new float2(-rA.y, rA.x);
        float2 rbPerp = new float2(-rB.y, rB.x);

        float2 angularVelA = raPerp * angVelA; // Perpendicular vector
        float2 angularVelB = rbPerp * angVelB; // Perpendicular vector

        // Velocity at the point (linear + angular velocity contributions)
        float2 velAtPointA = velA + angularVelA;
        // Repeat similarly for entity B (box)
        // Velocity at the point for entity B
        float2 velAtPointB = velB + angularVelB;

        // Relative vel between 2 bodies
        float2 relativeVel = velAtPointA - velAtPointB;

        float relVelAlongNormal = math.dot(relativeVel, normal);

        if (relVelAlongNormal > 0)
            return;

        float raPerpDotN = math.dot(raPerp, normal);
        float rbPerpDotN = math.dot(rbPerp, normal);
        float denom = invMassA + invMassB +
            (raPerpDotN * raPerpDotN) * invInertiaA +
            (rbPerpDotN * rbPerpDotN) * invInertiaB;

        float restitution = Mathf.Min(bodyCircle.ValueRO.Restitution, bodyBox.ValueRO.Restitution);

        float impulseMag = (-(1+restitution) * relVelAlongNormal) / denom;

        Vector2 impulse = (impulseMag * normal) / SolverIteration;

        // Apply impulse to velocities (linear and angular)
        bodyCircle.ValueRW.Velocity += impulse * invMassA;
        bodyBox.ValueRW.Velocity += -impulse * invMassB;

        // cross product (2D)
        float torqueA = rA.x * impulse.y - rA.y * impulse.x;  // rA x impulse (scalar torque)
        float torqueB = rB.x * impulse.y - rB.y * impulse.x;  // rB x impulse (scalar torque)

        bodyCircle.ValueRW.AngularVelocity += torqueA * invInertiaA;  // Apply torque to circle's angular velocity
        bodyBox.ValueRW.AngularVelocity += -torqueB * invInertiaB;  // Apply torque to box's angular velocity
        
    }
    private static float2x2 AngleToMatrix(float radians)
    {
        float c = math.cos(radians);
        float s = math.sin(radians);
        return new float2x2(c, -s, s, c);
    }
}

[BurstCompile]
public partial struct BoxVsBoxImpulseResolutionJob : IJobParallelFor
{
    [ReadOnly] public NativeList<CollisionPair> ColPair;
    [WriteOnly] public NativeList<TriggerEvent>.ParallelWriter triggerEvents;

    [ReadOnly] public ComponentLookup<BoxShapeData> BoxShapes;

    [NativeDisableParallelForRestriction] public ComponentLookup<ShapeData> Shapes;
    [NativeDisableParallelForRestriction] public ComponentLookup<PhyBodyData> Bodies;

    [ReadOnly] public short SolverIteration;

    [NativeDisableParallelForRestriction]
    public NativeParallelHashSet<CollisionPair> processedTriggerPairs;
    public void Execute(int index)
    {
        Entity entityA = ColPair[index].EntityA;
        Entity entityB = ColPair[index].EntityB;

        RefRW<ShapeData> shapeA = Shapes.GetRefRW(entityA);
        RefRW<ShapeData> shapeB = Shapes.GetRefRW(entityB);

        RefRO<BoxShapeData> boxA = BoxShapes.GetRefRO(entityA);
        RefRO<BoxShapeData> boxB = BoxShapes.GetRefRO(entityB);

        RefRW<PhyBodyData> bodyA = Bodies.GetRefRW(entityA);
        RefRW<PhyBodyData> bodyB = Bodies.GetRefRW(entityB);

        float invMassA = bodyA.ValueRO.InvMass;
        float invMassB = bodyB.ValueRO.InvMass;

        float2 boxAvel = bodyA.ValueRO.Velocity;
        float2 boxBvel = bodyB.ValueRO.Velocity;
        float boxAangleVel = bodyA.ValueRO.AngularVelocity;
        float boxBangleVel = bodyB.ValueRO.AngularVelocity;
        float2 boxApos = (float2)shapeA.ValueRO.Position;
        float2 boxBpos = (float2)shapeB.ValueRO.Position;
        float2x2 rotMatrixA = PhysicsUtilities.RotationMatrix(shapeA.ValueRO.Rotation * Mathf.Deg2Rad);
        float2x2 rotMatrixB = PhysicsUtilities.RotationMatrix(shapeB.ValueRO.Rotation * Mathf.Deg2Rad);

        float2 halfExtentsA = boxA.ValueRO.dimentions * 0.5f;
        float2 halfExtentsB = boxB.ValueRO.dimentions * 0.5f;

        if (!SATIntersection(boxApos,
            rotMatrixA, halfExtentsA, 
            boxBpos, rotMatrixB, halfExtentsB, 
            out float2 mtvNormal, out float penetration))
            return;

        /// Trigger event collecting
        /// SOME UNINTENDED EVENT MIGHT BE ADDED WHEN TWO TRIGGER -> FIX
        if (shapeA.ValueRO.IsTrigger)
        {
            ///(Add() returns true if the element was not already present!)
            if (!processedTriggerPairs.Add(ColPair[index]))
            {
                triggerEvents.AddNoResize(new TriggerEvent { EmitterEntity = entityA, ReciverEntity = entityB });
            }
        }
        if (shapeB.ValueRO.IsTrigger)
        {
            var col = new CollisionPair { EntityA = ColPair[index].EntityB, EntityB = ColPair[index].EntityA };
            ///(Add() returns true if the element was not already present!)
            if (!processedTriggerPairs.Add(col))
            {
                triggerEvents.AddNoResize(new TriggerEvent { EmitterEntity = entityB, ReciverEntity = entityA });
            }
        }

        if (!shapeA.ValueRO.HasDynamics || !shapeB.ValueRO.HasDynamics)
            return;

        /// position correction 
        Vector2 correction = (penetration * mtvNormal) / SolverIteration;
        float totalInverseMass = invMassA + invMassB;
        float Afactor = 0f;
        float Bfactor = 0f;
        if (totalInverseMass > 0f)
        {
            Afactor = invMassA / totalInverseMass;
            Bfactor = (1 - Afactor);
        }
        shapeA.ValueRW.Position += -correction * Afactor;
        shapeB.ValueRW.Position += correction * Bfactor;
        boxApos = shapeA.ValueRO.Position;
        boxBpos = shapeB.ValueRO.Position;


        FindContactPoints(boxApos, rotMatrixA, halfExtentsA,
            boxBpos, rotMatrixB, halfExtentsB,
            out float2 contactPoint1, out float2 contactPoint2, out short contactCount);

        float restitution = math.min(bodyA.ValueRO.Restitution, bodyB.ValueRO.Restitution);

        float invInertiaA = bodyA.ValueRO.Inertia > 0f ? 1f / bodyA.ValueRO.Inertia : 0f;
        float invInertiaB = bodyB.ValueRO.Inertia > 0f ? 1f / bodyB.ValueRO.Inertia : 0f;

        float2 rA1 = contactPoint1 - boxApos;
        float2 rB1 = contactPoint1 - boxBpos;

        ProcessContactPoint(rA1, rB1, mtvNormal, boxApos, boxBpos, boxAvel, boxBvel, boxAangleVel, boxBangleVel,
             invInertiaA, invInertiaB, invMassA, invMassB, restitution,SolverIteration,contactCount,
             out float2 contactImpulse1);

        if (contactCount >1)
        {

            float2 rA2 = contactPoint2 - boxApos;
            float2 rB2 = contactPoint2 - boxBpos;

            ProcessContactPoint(rA2, rB2, mtvNormal, boxApos, boxBpos, boxAvel, boxBvel, boxAangleVel, boxBangleVel,
                 invInertiaA, invInertiaB, invMassA, invMassB, restitution, SolverIteration, contactCount,
                 out float2 contactImpulse2);

            contactImpulse2 /= SolverIteration;

            bodyA.ValueRW.Velocity += (Vector2)(-contactImpulse2 * invMassA);
            bodyB.ValueRW.Velocity += (Vector2)(contactImpulse2 * invMassB);

            bodyA.ValueRW.AngularVelocity += -(rA2.x * contactImpulse2.y - rA2.y * contactImpulse2.x) * invInertiaA;
            bodyB.ValueRW.AngularVelocity += (rB2.x * contactImpulse2.y - rB2.y * contactImpulse2.x) * invInertiaB;

        }

        contactImpulse1 /= SolverIteration;

        bodyA.ValueRW.Velocity += (Vector2)(-contactImpulse1 * invMassA);
        bodyB.ValueRW.Velocity += (Vector2)(contactImpulse1 * invMassB);

        bodyA.ValueRW.AngularVelocity += -(rA1.x * contactImpulse1.y - rA1.y * contactImpulse1.x) * invInertiaA;
        bodyB.ValueRW.AngularVelocity += (rB1.x * contactImpulse1.y - rB1.y * contactImpulse1.x) * invInertiaB;

    }

    private static bool SATIntersection(
        float2 posA, float2x2 rotMatrixA, float2 halfExtentsA,
        float2 posB, float2x2 rotMatrixB, float2 halfExtentsB,
        out float2 outNormal, out float outDepth)
    {
        outNormal = float2.zero;
        outDepth = float.MaxValue;

        float2 rightA = rotMatrixA.c0;
        float2 upA = rotMatrixA.c1;
        float2 rightB = rotMatrixB.c0;
        float2 upB = rotMatrixB.c1;

        float2 centerDelta = posB - posA;

        // Axis 1: rightA
        {
            float2 axis = rightA;
            float centerDist = math.dot(centerDelta, axis);
            float extentA = math.abs(math.dot(rightA * halfExtentsA.x, axis)) + math.abs(math.dot(upA * halfExtentsA.y, axis));
            float extentB = math.abs(math.dot(rightB * halfExtentsB.x, axis)) + math.abs(math.dot(upB * halfExtentsB.y, axis));
            float totalExtent = extentA + extentB;

            if (math.abs(centerDist) > totalExtent)
                return false;

            float overlap = totalExtent - math.abs(centerDist);
            if (overlap < outDepth)
            {
                outDepth = overlap;
                outNormal = (centerDist < 0) ? -axis : axis;
            }
        }

        // Axis 2: upA
        {
            float2 axis = upA;
            float centerDist = math.dot(centerDelta, axis);
            float extentA = math.abs(math.dot(rightA * halfExtentsA.x, axis)) + math.abs(math.dot(upA * halfExtentsA.y, axis));
            float extentB = math.abs(math.dot(rightB * halfExtentsB.x, axis)) + math.abs(math.dot(upB * halfExtentsB.y, axis));
            float totalExtent = extentA + extentB;

            if (math.abs(centerDist) > totalExtent)
                return false;

            float overlap = totalExtent - math.abs(centerDist);
            if (overlap < outDepth)
            {
                outDepth = overlap;
                outNormal = (centerDist < 0) ? -axis : axis;
            }
        }

        // Axis 3: rightB
        {
            float2 axis = rightB;
            float centerDist = math.dot(centerDelta, axis);
            float extentA = math.abs(math.dot(rightA * halfExtentsA.x, axis)) + math.abs(math.dot(upA * halfExtentsA.y, axis));
            float extentB = math.abs(math.dot(rightB * halfExtentsB.x, axis)) + math.abs(math.dot(upB * halfExtentsB.y, axis));
            float totalExtent = extentA + extentB;

            if (math.abs(centerDist) > totalExtent)
                return false;

            float overlap = totalExtent - math.abs(centerDist);
            if (overlap < outDepth)
            {
                outDepth = overlap;
                outNormal = (centerDist < 0) ? -axis : axis;
            }
        }

        // Axis 4: upB
        {
            float2 axis = upB;
            float centerDist = math.dot(centerDelta, axis);
            float extentA = math.abs(math.dot(rightA * halfExtentsA.x, axis)) + math.abs(math.dot(upA * halfExtentsA.y, axis));
            float extentB = math.abs(math.dot(rightB * halfExtentsB.x, axis)) + math.abs(math.dot(upB * halfExtentsB.y, axis));
            float totalExtent = extentA + extentB;

            if (math.abs(centerDist) > totalExtent)
                return false;

            float overlap = totalExtent - math.abs(centerDist);
            if (overlap < outDepth)
            {
                outDepth = overlap;
                outNormal = (centerDist < 0) ? -axis : axis;
            }
        }

        return true;
    }

    private static void FindContactPoints(
       float2 centerA, float2x2 rotMatrixA, float2 halfExtentsA,
       float2 centerB, float2x2 rotMatrixB, float2 halfExtentsB,
       out float2 contact1, out float2 contact2, out short contactCount)
    {

        float2 _contact1 = float2.zero;
        float2 _contact2 = float2.zero;
        short _contactCount = 0;

        float minDistSq = float.MaxValue;

        // Compute box corners for A
        float2 rightA = rotMatrixA.c0 * halfExtentsA.x;
        float2 upA = rotMatrixA.c1 * halfExtentsA.y;

        float2 a0 = centerA - rightA - upA;
        float2 a1 = centerA + rightA - upA;
        float2 a2 = centerA + rightA + upA;
        float2 a3 = centerA - rightA + upA;

        // Compute box corners for B
        float2 rightB = rotMatrixB.c0 * halfExtentsB.x;
        float2 upB = rotMatrixB.c1 * halfExtentsB.y;

        float2 b0 = centerB - rightB - upB;
        float2 b1 = centerB + rightB - upB;
        float2 b2 = centerB + rightB + upB;
        float2 b3 = centerB - rightB + upB;

        // A to B (checking all edges of A against all edges of B)
        Check(a0, b0, b1); Check(a0, b1, b2); Check(a0, b2, b3); Check(a0, b3, b0);
        Check(a1, b0, b1); Check(a1, b1, b2); Check(a1, b2, b3); Check(a1, b3, b0);
        Check(a2, b0, b1); Check(a2, b1, b2); Check(a2, b2, b3); Check(a2, b3, b0);
        Check(a3, b0, b1); Check(a3, b1, b2); Check(a3, b2, b3); Check(a3, b3, b0);

        // B to A (checking all edges of B against all edges of A)
        Check(b0, a0, a1); Check(b0, a1, a2); Check(b0, a2, a3); Check(b0, a3, a0);
        Check(b1, a0, a1); Check(b1, a1, a2); Check(b1, a2, a3); Check(b1, a3, a0);
        Check(b2, a0, a1); Check(b2, a1, a2); Check(b2, a2, a3); Check(b2, a3, a0);
        Check(b3, a0, a1); Check(b3, a1, a2); Check(b3, a2, a3); Check(b3, a3, a0);

        contact1 = _contact1;
        contact2 = _contact2;
        contactCount = _contactCount;

        void Check(float2 point, float2 segA, float2 segB)
        {
            PhysicsUtilities.PointSegmentDistance(point, segA, segB, out float distSq, out float2 cp);

            if (PhysicsUtilities.NearlyEqual(distSq, minDistSq))
            {
                if (!PhysicsUtilities.NearlyEqual(cp, _contact1))// && !PhysicsUtilities.NearlyEqual(cp, _contact2))
                {
                    _contact2 = cp;
                    _contactCount = 2;
                }
            }
            else if (distSq < minDistSq)
            {
                minDistSq = distSq;
                _contact1 = cp;
                _contactCount = 1;
            }
        }
    }

    private static void ProcessContactPoint(float2 rA,float2 rB, float2 mtvNormal,
        float2 boxApos, float2 boxBpos,float2 boxAvel, float2 boxBvel, float boxAanglVel, float boxBanglVel,
        float invInertiaA, float invInertiaB, float invMassA, float invMassB, float restitution,
        short SolverIteration, short contactCount,
        out float2 impulse)//, out float torqueA, out float torqueB)
    {

        float2 raPerp = new float2(-rA.y, rA.x);
        float2 rbPerp = new float2(-rB.y, rB.x);


        float2 relativeVelocity =
            (boxBvel + (rbPerp*boxBanglVel)) -
            (boxAvel + (raPerp*boxAanglVel));

        float contactVelocityN = math.dot(relativeVelocity, mtvNormal);

        //Debug.DrawRay(new float3(contactPoint.x, contactPoint.y, 0f), new float3(relativeVelocity.x*30, relativeVelocity.y*30, 0f),Color.green);

        if (contactVelocityN > 0)
        {
            impulse = Vector2.zero;
            return;
        }

        float raPerpDotN = math.dot(raPerp, mtvNormal);
        float rbPerpDotN = math.dot(rbPerp, mtvNormal);

        float denom = invMassA + invMassB +
                      (raPerpDotN * raPerpDotN) * invInertiaA +
                      (rbPerpDotN * rbPerpDotN) * invInertiaB;

        float impulseMag = (-(1 + restitution) * contactVelocityN) / denom;
        impulse = (impulseMag / contactCount) * mtvNormal;

    }


}