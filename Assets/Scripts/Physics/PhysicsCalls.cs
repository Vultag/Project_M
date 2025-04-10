using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using UnityEngine;


public struct PhysicsCalls
{


    public static void DestroyPhysicsEntity(EntityCommandBuffer ecb, Entity entity)
    {

        //Debug.Log("disable : " + entity);
        TreeInsersionSystem.AABBtree.DisableEntity(entity);
        ecb.DestroyEntity(entity);

    }


    public static NativeList<Entity> CircleOverlapNode(CircleShapeData CastSphere)//, PhysicsUtilities.CollisionLayer colLayer)
    {

        DynamicAABBTree AABBtree = TreeInsersionSystem.AABBtree;
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
                if (PhysicsUtilities.Proximity(AABBtree.nodes[0].box, CastSphere) < 0 && AABBtree.nodes[0].layerMask == CastSphere.collisionLayer)
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
                float nodeA = node.LeftChild != -1 ? PhysicsUtilities.Proximity(AABBtree.nodes[node.LeftChild].box, CastSphere) : 1;
                float nodeB = node.RightChild != -1 ? PhysicsUtilities.Proximity(AABBtree.nodes[node.RightChild].box, CastSphere) : 1;

                if (nodeA < 0)
                {

                    if (AABBtree.nodes[node.LeftChild].isLeaf == true)
                    {
                        if(AABBtree.nodes[node.LeftChild].layerMask == CastSphere.collisionLayer)
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
                        if(AABBtree.nodes[node.RightChild].layerMask == CastSphere.collisionLayer)
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

    public static RayCastHit RaycastNode(Ray ray, PhysicsUtilities.CollisionLayer colLayer, ComponentLookup<CircleShapeData> CirclesShapesLookUp)
    {
        DynamicAABBTree AABBtree = TreeInsersionSystem.AABBtree;

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
                            float distance = PhysicsUtilities.Intersect(CirclesShapesLookUp[entity], ray);
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
                            float distance = PhysicsUtilities.Intersect(CirclesShapesLookUp[entity], ray);
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

//TO DO
//[BurstCompile]
//public partial struct RayCast : IJobEntity//IJob //ijobparralel ?
//{

//    [ReadOnly]
//    public DynamicAABBTree<CircleShapeData> AABBtree;


//    [WriteOnly]
//    public int HitEntityIndex;

//    public void Execute([EntityIndexInQuery] int sortKey, ref PhyBodyData body)
//    {

//        body.Velocity += CirclesNewVels[sortKey];
//        //Debug.Log(body.Velocity);
//    }
//}

