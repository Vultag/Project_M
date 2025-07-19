using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Entities.UniversalDelegates;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.UIElements;


public struct PhysicsCalls
{

    public static void DestroyPhysicsEntity(EntityCommandBuffer ecb, Entity entity)
    {

        //Debug.Log("disable : " + entity);
        TreeInsersionSystem.DynamicBodiesAABBtree.DisableEntity(entity);
        ecb.DestroyEntity(entity);

    }


    public static NativeList<Entity> CircleOverlapNode(float2 position, float radius, PhysicsUtilities.CollisionLayer layer)//, PhysicsUtilities.CollisionLayer colLayer)
    {
        ///copying entire tree ?? OPTI
        DynamicAABBTree AABBtree = TreeInsersionSystem.DynamicBodiesAABBtree;
        int treeNodeCount = AABBtree.nodes.Length;

        //internal capacity ? OPTI
        NativeList <(Entity, float)> OverlapList = new NativeList<(Entity, float)>(30,Allocator.Temp);
     

        NativeQueue<int> comparequeue = new NativeQueue<int>(Allocator.Temp);

        NativeList<Entity> result;

        comparequeue.Enqueue(AABBtree.rootIndex);
        /*if there is a only a single body to check*/
        if (treeNodeCount <= 1)
        {
            if(treeNodeCount == 1)
            {
                AABBTreeNode node = AABBtree.nodes[comparequeue.Dequeue()];
                if (PhysicsUtilities.Proximity(AABBtree.nodes[0].box, position, radius) < 0 && (AABBtree.nodes[0].layerMask & layer) != 0)
                {
                    OverlapList.Add((AABBtree.nodes[0].entity,0f));
                }
                comparequeue.Dispose();
                result = NativeListUtils.SelectFirst(OverlapList);
                return result;

            }
            else
            {
                comparequeue.Dispose();
                result = NativeListUtils.SelectFirst(OverlapList);
                return result;
            }
    
        }
        /*Gather all the intersecting AABB in unordered list*/
        while (comparequeue.Count > 0)
        {

            AABBTreeNode node = AABBtree.nodes[comparequeue.Dequeue()];

            if (node.isLeaf == false)
            {
                float nodeA = node.LeftChild != -1 ? PhysicsUtilities.Proximity(AABBtree.nodes[node.LeftChild].box, position, radius) : 1;
                float nodeB = node.RightChild != -1 ? PhysicsUtilities.Proximity(AABBtree.nodes[node.RightChild].box, position, radius) : 1;

                if (nodeA < 0)
                {

                    if (AABBtree.nodes[node.LeftChild].isLeaf == true)
                    {
                        if((AABBtree.nodes[node.LeftChild].layerMask & layer) != 0)
                        {
                            OverlapList.Add((AABBtree.nodes[node.LeftChild].entity, nodeA));
                        }
                    }
                    else
                        comparequeue.Enqueue(node.LeftChild);
                }
                if (nodeB < 0)
                {

                    if (AABBtree.nodes[node.RightChild].isLeaf == true)
                    {
                        if((AABBtree.nodes[node.RightChild].layerMask & layer) != 0)
                        {
                            OverlapList.Add((AABBtree.nodes[node.RightChild].entity, nodeB));
                        }
                    }
                    else
                        comparequeue.Enqueue(node.RightChild);
                }
            }
        }

        ///MAKE SURE DISPOSE ALL

        comparequeue.Dispose();
        NativeListUtils.QuickSort(OverlapList, 0, OverlapList.Length - 1);
        result = NativeListUtils.SelectFirst(OverlapList);
        OverlapList.Dispose();
        return result;

    }

    public static RayCastHit RaycastNode(Ray ray, PhysicsUtilities.CollisionLayer colLayer, 
        in ComponentLookup<ShapeData> ShapeDataLookUp, in ComponentLookup<CircleShapeData> circleShapeLookUp, in ComponentLookup<BoxShapeData> boxShapeLookUp)
    {
        DynamicAABBTree AABBtree = TreeInsersionSystem.DynamicBodiesAABBtree;

        //internal capacity ? OPTI
        NativeList<(Entity, float)> OverlapList = new NativeList<(Entity, float)>(30, Allocator.Temp);

        NativeQueue<int> comparequeue = new NativeQueue<int>(Allocator.Temp);

        comparequeue.Enqueue(AABBtree.rootIndex);

        /*Gather all the intersecting AABB in unordered list*/
        while (comparequeue.Count > 0)
        {

            AABBTreeNode node = AABBtree.nodes[comparequeue.Dequeue()];

            if (node.isLeaf == false)
            {
                float AABBdistanceA = node.LeftChild != -1 ? PhysicsUtilities.Intersect(AABBtree.nodes[node.LeftChild].box, ray) : -1;
                if (AABBdistanceA >= 0)
                {
                    //if (AABBtree.nodes[node.LeftChild].layerMask != PhysicsUtilities.CollisionLayer.PlayerLayer && AABBtree.nodes[node.LeftChild].layerMask != PhysicsUtilities.CollisionLayer.MonsterLayer && AABBtree.nodes[node.LeftChild].isLeaf == true)
                    //    Debug.Log(AABBtree.nodes[node.LeftChild].layerMask);

                    if (AABBtree.nodes[node.LeftChild].isLeaf == true)
                    {
                        if(AABBtree.nodes[node.LeftChild].layerMask == colLayer)
                        {
                            Entity entity = AABBtree.nodes[node.LeftChild].entity;// DynamicAABBTree.ReconstructEntity(AABBtree.nodes[node.LeftChild].EntityKey);

                            /// test colision with actual physics shape
                            float distance = PhysicsUtilities.Intersect(ShapeDataLookUp.GetRefRO(entity).ValueRO,entity, circleShapeLookUp,boxShapeLookUp, ray);
                            if (distance > 0)
                                OverlapList.Add((entity, distance));
                        }
                    }
                    else
                        comparequeue.Enqueue(node.LeftChild);
                }

                float AABBdistanceB = node.RightChild != -1 ?PhysicsUtilities.Intersect(AABBtree.nodes[node.RightChild].box, ray):-1;
                if (AABBdistanceB >= 0)
                {
                    //if (AABBtree.nodes[node.RightChild].layerMask != PhysicsUtilities.CollisionLayer.PlayerLayer && AABBtree.nodes[node.RightChild].layerMask != PhysicsUtilities.CollisionLayer.MonsterLayer && AABBtree.nodes[node.RightChild].isLeaf == true)
                    //    Debug.Log(AABBtree.nodes[node.RightChild].layerMask);

                    if (AABBtree.nodes[node.RightChild].isLeaf == true)
                    {
                        if(AABBtree.nodes[node.RightChild].layerMask == colLayer)
                        {
                            Entity entity = AABBtree.nodes[node.RightChild].entity;//DynamicAABBTree.ReconstructEntity(AABBtree.nodes[node.RightChild].EntityKey);
                            /// test colision with actual physics shape
                            float distance = PhysicsUtilities.Intersect(ShapeDataLookUp.GetRefRO(entity).ValueRO, entity, circleShapeLookUp, boxShapeLookUp, ray);
                            if (distance > 0)
                                OverlapList.Add((entity, distance));
                        }
                    }
                    else
                        comparequeue.Enqueue(node.RightChild);
                }
            }
        }

        comparequeue.Dispose();
        NativeListUtils.QuickSort(OverlapList, 0, OverlapList.Length - 1);

        return OverlapList.Length > 0 ? new RayCastHit { entity = OverlapList[0].Item1, distance = OverlapList[0].Item2 } : new RayCastHit {entity = Entity.Null };

    }

    public static bool IsShapeOverlaping(float2 position, CircleShapeData circleData, PhysicsUtilities.CollisionLayer layer, 
        in EntityManager entityManager)
    {
        ///copying entire tree ?? OPTI
        DynamicAABBTree AABBtree = (layer & PhysicsUtilities.CollisionLayer.StaticObstacleLayer)== PhysicsUtilities.CollisionLayer.StaticObstacleLayer ? TreeInsersionSystem.StaticBodiesAABBtree : TreeInsersionSystem.DynamicBodiesAABBtree;
        int treeNodeCount = AABBtree.nodes.Length;

        float radius = circleData.radius;

        NativeQueue<int> comparequeue = new NativeQueue<int>(Allocator.Temp);

        comparequeue.Enqueue(AABBtree.rootIndex);
        /*if there is a only a single body to check*/
        if (treeNodeCount <= 1)
        {
            if (treeNodeCount == 1)
            {
                AABBTreeNode node = AABBtree.nodes[comparequeue.Dequeue()];
                if (PhysicsUtilities.Proximity(AABBtree.nodes[0].box, position, radius) < 0 && (AABBtree.nodes[0].layerMask & layer) != 0)
                {
                    var shape = entityManager.GetComponentData<ShapeData>(AABBtree.nodes[0].entity);
                    switch (shape.shapeType)
                    {
                        case ShapeType.Circle:
                            if (NarrowPhaseCheck(shape.Position,
                              entityManager.GetComponentData<CircleShapeData>(AABBtree.nodes[0].entity),
                              position, circleData))
                            {
                                comparequeue.Dispose();
                                return true;
                            }
                            break;
                        case ShapeType.Box:
                            if (NarrowPhaseCheck(position, circleData,
                                shape.Position, shape.Rotation,
                                entityManager.GetComponentData<BoxShapeData>(AABBtree.nodes[0].entity)
                                ))
                            {
                                comparequeue.Dispose();
                                return true;
                            }
                            break;
                    }
              
                    
                }
            }
            else
            {
                comparequeue.Dispose();
                return false;
            }

        }
        /*Gather all the intersecting AABB in unordered list*/
        while (comparequeue.Count > 0)
        {

            AABBTreeNode node = AABBtree.nodes[comparequeue.Dequeue()];

            if (node.isLeaf == false)
            {
                float nodeA = node.LeftChild != -1 ? PhysicsUtilities.Proximity(AABBtree.nodes[node.LeftChild].box, position, radius) : 1;
                float nodeB = node.RightChild != -1 ? PhysicsUtilities.Proximity(AABBtree.nodes[node.RightChild].box, position, radius) : 1;

                if (nodeA < 0)
                {

                    if (AABBtree.nodes[node.LeftChild].isLeaf == true)
                    {
                        if ((AABBtree.nodes[node.LeftChild].layerMask & layer) != 0)
                        {
                            var shape = entityManager.GetComponentData<ShapeData>(AABBtree.nodes[node.LeftChild].entity);
                            switch (shape.shapeType)
                            {
                                case ShapeType.Circle:
                                    if (NarrowPhaseCheck(shape.Position,
                                      entityManager.GetComponentData<CircleShapeData>(AABBtree.nodes[node.LeftChild].entity),
                                      position, circleData))
                                    {
                                        comparequeue.Dispose();
                                        return true;
                                    }
                                    break;
                                case ShapeType.Box:
                                    if (NarrowPhaseCheck(position, circleData,
                                        shape.Position, shape.Rotation,
                                        entityManager.GetComponentData<BoxShapeData>(AABBtree.nodes[node.LeftChild].entity)
                                        ))
                                    {
                                        comparequeue.Dispose();
                                        return true;
                                    }
                                    break;
                            }
                        }
                    }
                    else
                        comparequeue.Enqueue(node.LeftChild);
                }
                if (nodeB < 0)
                {

                    if (AABBtree.nodes[node.RightChild].isLeaf == true)
                    {
                        if ((AABBtree.nodes[node.RightChild].layerMask & layer) != 0)
                        {
                            var shape = entityManager.GetComponentData<ShapeData>(AABBtree.nodes[node.RightChild].entity);
                            switch (shape.shapeType)
                            {
                                case ShapeType.Circle:
                                    if (NarrowPhaseCheck(shape.Position,
                                      entityManager.GetComponentData<CircleShapeData>(AABBtree.nodes[node.RightChild].entity),
                                      position, circleData))
                                    {
                                        comparequeue.Dispose();
                                        return true;
                                    }
                                    break;
                                case ShapeType.Box:
                                    if (NarrowPhaseCheck(position, circleData,
                                        shape.Position, shape.Rotation,
                                        entityManager.GetComponentData<BoxShapeData>(AABBtree.nodes[node.RightChild].entity)
                                        ))
                                    {
                                        comparequeue.Dispose();
                                        return true;
                                    }
                                    break;
                            }
                        }
                    }
                    else
                        comparequeue.Enqueue(node.RightChild);
                }
            }
        }

        comparequeue.Dispose();
        return false;
    }
    public static bool IsShapeOverlaping(float2 position, float rotation, BoxShapeData boxData, AABB boxAABB, PhysicsUtilities.CollisionLayer layer,
    in EntityManager entityManager)
    {
        ///copying entire tree ?? OPTI
        DynamicAABBTree AABBtree = (layer & PhysicsUtilities.CollisionLayer.StaticObstacleLayer) == PhysicsUtilities.CollisionLayer.StaticObstacleLayer ? TreeInsersionSystem.StaticBodiesAABBtree : TreeInsersionSystem.DynamicBodiesAABBtree;
        int treeNodeCount = AABBtree.nodes.Length;

        NativeQueue<int> comparequeue = new NativeQueue<int>(Allocator.Temp);

        comparequeue.Enqueue(AABBtree.rootIndex);
        /*if there is a only a single body to check*/
        if (treeNodeCount <= 1)
        {
            if (treeNodeCount == 1)
            {
                AABBTreeNode node = AABBtree.nodes[comparequeue.Dequeue()];
                if (PhysicsUtilities.Proximity(AABBtree.nodes[0].box, boxAABB) < 0 && (AABBtree.nodes[0].layerMask & layer) != 0)
                {
                    var shape = entityManager.GetComponentData<ShapeData>(AABBtree.nodes[0].entity);
                    switch (shape.shapeType)
                    {
                        case ShapeType.Circle:
                            if (NarrowPhaseCheck(shape.Position,
                              entityManager.GetComponentData<CircleShapeData>(AABBtree.nodes[0].entity),
                              position, rotation, boxData))
                            {
                                comparequeue.Dispose();
                                return true;
                            }
                            break;
                        case ShapeType.Box:
                            if (NarrowPhaseCheck(position, rotation, boxData,
                                shape.Position, shape.Rotation,
                                entityManager.GetComponentData<BoxShapeData>(AABBtree.nodes[0].entity)
                                ))
                            {
                                comparequeue.Dispose();
                                return true;
                            }
                            break;
                    }


                }
            }
            else
            {
                comparequeue.Dispose();
                return false;
            }

        }
        /*Gather all the intersecting AABB in unordered list*/
        while (comparequeue.Count > 0)
        {

            AABBTreeNode node = AABBtree.nodes[comparequeue.Dequeue()];

            if (node.isLeaf == false)
            {
                //if (node.LeftChild == -1 || node.RightChild == -1)
                //    Debug.Log("mmm");

                float nodeA = node.LeftChild != -1 ? PhysicsUtilities.Proximity(AABBtree.nodes[node.LeftChild].box, boxAABB) : -1;
                float nodeB = node.RightChild != -1 ? PhysicsUtilities.Proximity(AABBtree.nodes[node.RightChild].box, boxAABB) : -1;

                //Debug.Log(nodeA);
                //Debug.Log(nodeB);
                //if (nodeA == 1 && nodeB == 1)
                //{
                //    Debug.Log(nodeA);
                //    Debug.Log(nodeB);
                //}

                if (nodeA > 0)
                {
                    if (AABBtree.nodes[node.LeftChild].isLeaf == true)
                    {
                        if ((AABBtree.nodes[node.LeftChild].layerMask & layer) != 0)
                        {
                            var shape = entityManager.GetComponentData<ShapeData>(AABBtree.nodes[node.LeftChild].entity);
                            switch (shape.shapeType)
                            {
                                case ShapeType.Circle:
                                    if (NarrowPhaseCheck(shape.Position,
                                      entityManager.GetComponentData<CircleShapeData>(AABBtree.nodes[node.LeftChild].entity),
                                      position, rotation, boxData))
                                    {
                                        comparequeue.Dispose();
                                        return true;
                                    }
                                    break;
                                case ShapeType.Box:

                                    if (NarrowPhaseCheck(position, rotation, boxData,
                                        shape.Position, shape.Rotation,
                                        entityManager.GetComponentData<BoxShapeData>(AABBtree.nodes[node.LeftChild].entity)
                                        ))
                                    {
                                        comparequeue.Dispose();
                                        return true;
                                    }
                                    break;
                            }
                        }
                    }
                    else
                        comparequeue.Enqueue(node.LeftChild);
                }
                if (nodeB > 0)
                {
                    if (AABBtree.nodes[node.RightChild].isLeaf == true)
                    {
                        if ((AABBtree.nodes[node.RightChild].layerMask & layer) != 0)
                        {
                            var shape = entityManager.GetComponentData<ShapeData>(AABBtree.nodes[node.RightChild].entity);
                            switch (shape.shapeType)
                            {
                                case ShapeType.Circle:
                                    if (NarrowPhaseCheck(shape.Position,
                                      entityManager.GetComponentData<CircleShapeData>(AABBtree.nodes[node.RightChild].entity),
                                      position, rotation, boxData))
                                    {
                                        comparequeue.Dispose();
                                        return true;
                                    }
                                    break;
                                case ShapeType.Box:
                                    if (NarrowPhaseCheck(position, rotation, boxData,
                                        shape.Position, shape.Rotation,
                                        entityManager.GetComponentData<BoxShapeData>(AABBtree.nodes[node.RightChild].entity)
                                        ))
                                    {
                                        comparequeue.Dispose();
                                        return true;
                                    }
                                    break;
                            }
                        }
                    }
                    else
                        comparequeue.Enqueue(node.RightChild);
                }
            }
        }

        comparequeue.Dispose();
        return false;
    }

    private static bool NarrowPhaseCheck(float2 posA, CircleShapeData circleShapeA, float2 posB, CircleShapeData circleShapeB)
    {
        var delta = posB - posA;
        var distSq = math.lengthsq(delta);
        var radii = circleShapeA.radius + circleShapeB.radius;
        var radiiSq = radii * radii;

        if (!(distSq < radiiSq))
            return false;
        return true;
    }
    private static bool NarrowPhaseCheck(float2 posA, CircleShapeData circleShapeA, float2 posB, float rotB, BoxShapeData boxShapeB)
    {

        // Compute rotation matrix from rotation angle
        float2x2 rotMatrix = PhysicsUtilities.RotationMatrix(rotB * Mathf.Deg2Rad);
        float2x2 invRotMatrix = math.transpose(rotMatrix);

        float2 boxHalfExtents = boxShapeB.dimentions * 0.5f;

        float2 rel = posA - posB;
        float2 localCircle = math.mul(invRotMatrix, rel);
        float2 closestLocal = math.clamp(localCircle, -boxHalfExtents, boxHalfExtents);
        float2 closestWorldOffset = math.mul(rotMatrix, closestLocal);
        float2 closestPoint = posB + closestWorldOffset;

        float2 diff = posA - closestPoint;
        float distSq = math.lengthsq(diff);
        float radius = circleShapeA.radius;

        if (distSq >= radius * radius)
            return false;

        return true;
    }
    private static bool NarrowPhaseCheck(float2 posA, float rotA, BoxShapeData boxShapeA,float2 posB, float rotB, BoxShapeData boxShapeB)
    {
        float2x2 rotA_Mat = PhysicsUtilities.RotationMatrix(rotA * Mathf.Deg2Rad);
        float2x2 rotB_Mat = PhysicsUtilities.RotationMatrix(rotB * Mathf.Deg2Rad);

        float2 halfA = boxShapeA.dimentions * 0.5f;
        float2 halfB = boxShapeB.dimentions * 0.5f;

        float2 delta = posA - posB;

        // Local axes in world space
        float2 axisA_X = rotA_Mat.c0;
        float2 axisA_Y = rotA_Mat.c1;
        float2 axisB_X = rotB_Mat.c0;
        float2 axisB_Y = rotB_Mat.c1;

        // 1. Test axis A.X
        {
            float2 axis = axisA_X;
            float projectionA = halfA.x;
            float projectionB =
                math.abs(math.dot(axis, axisB_X)) * halfB.x +
                math.abs(math.dot(axis, axisB_Y)) * halfB.y;
            float distance = math.abs(math.dot(delta, axis));
            if (distance > projectionA + projectionB)
                return false;
        }

        // 2. Test axis A.Y
        {
            float2 axis = axisA_Y;
            float projectionA = halfA.y;
            float projectionB =
                math.abs(math.dot(axis, axisB_X)) * halfB.x +
                math.abs(math.dot(axis, axisB_Y)) * halfB.y;
            float distance = math.abs(math.dot(delta, axis));
            if (distance > projectionA + projectionB)
                return false;
        }

        // 3. Test axis B.X
        {
            float2 axis = axisB_X;
            float projectionA =
                math.abs(math.dot(axis, axisA_X)) * halfA.x +
                math.abs(math.dot(axis, axisA_Y)) * halfA.y;
            float projectionB = halfB.x;
            float distance = math.abs(math.dot(delta, axis));
            if (distance > projectionA + projectionB)
                return false;
        }

        // 4. Test axis B.Y
        {
            float2 axis = axisB_Y;
            float projectionA =
                math.abs(math.dot(axis, axisA_X)) * halfA.x +
                math.abs(math.dot(axis, axisA_Y)) * halfA.y;
            float projectionB = halfB.y;
            float distance = math.abs(math.dot(delta, axis));
            if (distance > projectionA + projectionB)
                return false;
        }

        return true; // Overlaps on all axes
    }

}
//TO DO
[BurstCompile]
public partial struct OverlapSphere : IJob//IJob //ijobparralel ?
{

    [ReadOnly]
    public CircleShapeData CastSphere;

    [ReadOnly]
    public DynamicAABBTree AABBtree;


    [WriteOnly]
    public NativeArray<int> HitEntitiesIndex;

    public void Execute()
    {

        

    }
}


