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

        //TO DO : MOVE SOME TO DYNAMIC AABBTREE

        /*Remove the node from the tree*/
        for (int i = 0; i < TreeInsersionSystem.AABBtree.nodeCount; i++)
        {
            //Debug.Log("i" +i);
            //try to check only leaf ? OPTI
            if (TreeInsersionSystem.AABBtree.nodes[i].bodyIndex == entity)
            {

                //Debug.Log("count " + PhyResolutionSystem.AABBtree.nodeCount);
                //Debug.Log("r " + PhyResolutionSystem.AABBtree.rootIndex);
                //Debug.Log("p " + PhyResolutionSystem.AABBtree.nodes[i].parentIndex);
                //Debug.Log("pc1 " + PhyResolutionSystem.AABBtree.nodes[nodes[i].parentIndex].child1);
                //Debug.Log("pc2 " + PhyResolutionSystem.AABBtree.nodes[nodes[i].parentIndex].child2);
                //Debug.Log("lc1 " + PhyResolutionSystem.AABBtree.nodes[nodecount-1].child1);
                //Debug.Log("lc2 " + PhyResolutionSystem.AABBtree.nodes[nodecount-1].child2);

                if (TreeInsersionSystem.AABBtree.nodeCount <= 3)
                {
                    if (TreeInsersionSystem.AABBtree.nodeCount == 1)
                    {
                        //if (nodes[0].isLeaf)
                        //{
                        //    nodes[0] = nodes[1];
                        //    nodes[1] = default;
                        //}
                        TreeInsersionSystem.AABBtree.nodes[0] = default;
                        TreeInsersionSystem.AABBtree.nodeCount = 0;
                        ecb.DestroyEntity(entity);
                        return;
                    }
                    else
                    {
                        //Debug.Log("i"+i);
                        //Debug.Log(Mathf.Abs(i-1));
                        int idx = Mathf.Abs(i - 1);
                        AABBTreeNode newnode = TreeInsersionSystem.AABBtree.nodes[idx];
                        newnode.parentIndex = 0;
                        TreeInsersionSystem.AABBtree.nodes[0] = newnode;
                        TreeInsersionSystem.AABBtree.nodes[1] = default;
                        TreeInsersionSystem.AABBtree.nodes[2] = default;
                        TreeInsersionSystem.AABBtree.nodeCount -= 2;
                        TreeInsersionSystem.AABBtree.rootIndex = 0;
                        ecb.DestroyEntity(entity);
                        return;
                    }

                }

                TreeInsersionSystem.AABBtree.RemoveLeaf(i);


                var nodes = TreeInsersionSystem.AABBtree.nodes;

                int nodecount = TreeInsersionSystem.AABBtree.nodeCount;


                //Debug.Log("verify root parent here");

                //Debug.Log(" ");
                //Debug.Log(" ");
                //Debug.Log(" ");
                //Debug.Log("count " + nodecount);
                //Debug.Log("r " + PhyResolutionSystem.AABBtree.rootIndex);
                //Debug.Log("p " + nodes[i].parentIndex);
                //Debug.Log("pc1 " + nodes[nodes[i].parentIndex].child1);
                //Debug.Log("pc2 " + nodes[nodes[i].parentIndex].child2);
                //Debug.Log("lc1 " + nodes[nodecount - 1].child1);
                //Debug.Log("lc2 " + nodes[nodecount - 1].child2);

                //call node permutation in AABB

                //PhyResolutionSystem.AABBtree.RefitHierarchy(PhyResolutionSystem.AABBtree.nodes[i].parentIndex);


                //REWORK NODE REMOVAL TO NOT NEED IT ? OPTI
                /*
                 Last Node permutation -> critical to preserve the tree coherency
                 */
                if (nodes[nodecount - 2].parentIndex != nodecount - 1)
                {
                    //Debug.Log("startroot " + PhyResolutionSystem.AABBtree.rootIndex);

                    //Debug.Log("ICI " + nodes[nodecount - 2].parentIndex);

                    //Debug.Log("ICI " + (nodecount - 1));


                    //BUGS?
                    //remove entity switch?//box?


                    int newparentidx = nodes[nodecount - 2].parentIndex;
                    AABBTreeNode newparent = nodes[newparentidx];
                    int newchild1idx = nodes[newparentidx].child1;
                    AABBTreeNode newchild1 = nodes[nodes[newparentidx].child1];
                    int newchild2idx = nodes[newparentidx].child2;
                    AABBTreeNode newchild2 = nodes[nodes[newparentidx].child2];
                    AABBTreeNode newlastbranch = nodes[nodecount - 1];

                    AABBTreeNode newlastbranchchild1 = nodes[nodes[nodecount - 1].child1];
                    AABBTreeNode newlastbranchchild2 = nodes[nodes[nodecount - 1].child2];

                    int newgrandparentidx = nodes[nodes[nodecount - 2].parentIndex].parentIndex;
                    AABBTreeNode newgrandparent = nodes[newgrandparentidx];
                    int newlastbranchparentidx = nodes[nodecount - 1].parentIndex;
                    AABBTreeNode newlastbranchparent = nodes[newlastbranchparentidx];

                    
                  

                    //newparent.bodyIndex = nodes[nodecount - 1].bodyIndex;
                    //newlastbranch.bodyIndex = nodes[nodes[nodecount - 2].parentIndex].bodyIndex;
                    //newparent.box = nodes[nodecount - 1].box;
                    //newlastbranch.box = nodes[nodes[nodecount - 2].parentIndex].box;


                    //if (nodes[nodes[nodecount - 2].parentIndex].child1 == nodecount - 2)
                    //{
                    //    newchild2idx = nodes[nodes[nodecount - 2].parentIndex].child2;
                    //}
                    //else
                    //{
                    //    newchild2idx = nodes[nodes[nodecount - 2].parentIndex].child1;
                    //}

                    //newchild2 = nodes[newchild2idx];
                    newchild1.parentIndex = nodecount - 1;
                    newchild2.parentIndex = nodecount - 1;

                    if (newgrandparent.child1 == nodes[nodecount - 2].parentIndex)
                    {
                        newgrandparent.child1 = nodecount - 1;
                    }
                    else
                    {
                        newgrandparent.child2 = nodecount - 1;
                    }

                    if (newlastbranchparent.child1 == nodecount - 1)
                    {
                        newlastbranchparent.child1 = nodes[nodecount - 2].parentIndex;
                    }
                    else
                    {
                        newlastbranchparent.child2 = nodes[nodecount - 2].parentIndex;
                    }


                    newlastbranchchild1.parentIndex = nodes[nodecount - 2].parentIndex;
                    newlastbranchchild2.parentIndex = nodes[nodecount - 2].parentIndex;



                    if (TreeInsersionSystem.AABBtree.rootIndex == nodecount - 1)
                    {
                        TreeInsersionSystem.AABBtree.rootIndex = nodes[nodecount - 2].parentIndex;
                        newlastbranch.parentIndex = nodes[nodecount - 2].parentIndex;

                        /*if the nodelength-2's grand parent is nodelength-1*/
                        if (newparent.parentIndex == nodecount - 1)
                        {
                            //Debug.Log("1"); 

                            newparent.parentIndex = nodes[nodecount - 2].parentIndex;
                            //newlastbranch.parentIndex = nodes[nodecount - 2].parentIndex;

                            if (nodes[nodecount - 1].child1 == nodes[nodecount - 2].parentIndex)
                            {
                                newlastbranch.child1 = nodecount - 1;
                                nodes[nodes[nodecount - 1].child2] = newlastbranchchild2;
                            }
                            else
                            {
                                newlastbranch.child2 = nodecount - 1;
                                nodes[nodes[nodecount - 1].child1] = newlastbranchchild1;
                            }


                            nodes[newparentidx] = newlastbranch;
                            nodes[nodecount - 1] = newparent;
                            nodes[newchild1idx] = newchild1;
                            nodes[newchild2idx] = newchild2;


                        }
                        /*fully separated*/
                        else
                        {
                            //Debug.Log("3");

                            /*if 1 node in common*/
                            if (newgrandparentidx == nodes[nodecount - 1].child1 || newgrandparentidx == nodes[nodecount - 1].child2)
                            {
                                newgrandparent.parentIndex = newparentidx;
                            }

                            nodes[nodes[nodecount - 1].child1] = newlastbranchchild1;
                            nodes[nodes[nodecount - 1].child2] = newlastbranchchild2;

                            nodes[newchild1idx] = newchild1;
                            nodes[newchild2idx] = newchild2;

                            nodes[newgrandparentidx] = newgrandparent;

                            nodes[newparentidx] = newlastbranch;
                            nodes[nodecount - 1] = newparent;
                        }


                        //return;



                    }
                    else if (TreeInsersionSystem.AABBtree.rootIndex == nodes[nodecount - 2].parentIndex)
                    {
                        TreeInsersionSystem.AABBtree.rootIndex = nodecount - 1;
                        newparent.parentIndex = nodecount - 1;

                        /*count -1 and count -2 are siblings*/
                        if (nodes[nodecount - 1].parentIndex == nodes[nodecount - 2].parentIndex)
                        {
                            //Debug.Log("4");
                            //Debug.Log(" ");
                            //Debug.Log(newparentidx);
                            //Debug.Log(nodes[nodecount - 2].parentIndex);
                            //Debug.Log(nodecount - 1);
                            //Debug.Log(nodes[nodecount - 1].parentIndex);


                            newlastbranchchild1.parentIndex = nodes[nodecount - 2].parentIndex;
                            newlastbranchchild2.parentIndex = nodes[nodecount - 2].parentIndex;

                            if (newparent.child1 == nodecount - 2)
                            {
                                newparent.child2 = nodes[nodecount - 2].parentIndex;
                                nodes[nodecount - 2] = newchild1;
                            }
                            else
                            {
                                newparent.child1 = nodes[nodecount - 2].parentIndex;
                                nodes[nodecount - 2] = newchild2;
                            }

                            newlastbranch.parentIndex = nodecount - 1;
                            nodes[nodes[nodecount - 1].child1] = newlastbranchchild1;
                            nodes[nodes[nodecount - 1].child2] = newlastbranchchild2;

                            nodes[nodecount - 1] = newparent;
                            nodes[newparentidx] = newlastbranch;


                            //Debug.Log("p = " + nodes[nodecount - 2].parentIndex);

                            //return;

                        }
                        /*fully separated*/
                        else
                        {
                            //Debug.Log("6");

                            /*if 1 node in common*/
                            if (newlastbranchparentidx == nodes[newparentidx].child1 || newlastbranchparentidx == nodes[newparentidx].child2)
                            {
                                newlastbranchparent.parentIndex = nodecount - 1;
                            }

                            nodes[nodes[nodecount - 1].child1] = newlastbranchchild1;
                            nodes[nodes[nodecount - 1].child2] = newlastbranchchild2;

                            nodes[newchild1idx] = newchild1;
                            nodes[newchild2idx] = newchild2;

                            nodes[newlastbranchparentidx] = newlastbranchparent;

                            nodes[newparentidx] = newlastbranch;
                            nodes[nodecount - 1] = newparent;

                            //return;

                        }


                    }
                    else
                    {
                       
                        /*if the nodelength-2's grand parent is nodelength-1*/
                        if (newparent.parentIndex == nodecount - 1)
                        {
                            //Debug.Log("11");

                            nodes[nodes[nodecount - 1].parentIndex] = newlastbranchparent;

                            newparent.parentIndex = nodes[nodecount - 2].parentIndex;

                            if (nodes[nodecount - 1].child1 == nodes[nodecount - 2].parentIndex)
                            {
                                newlastbranch.child1 = nodecount - 1;
                                nodes[nodes[nodecount - 1].child2] = newlastbranchchild2;
                            }
                            else
                            {
                                newlastbranch.child2 = nodecount - 1;
                                nodes[nodes[nodecount - 1].child1] = newlastbranchchild1;
                            }

                            nodes[newparentidx] = newlastbranch;
                            nodes[nodecount - 1] = newparent;
                            nodes[newchild1idx] = newchild1;
                            nodes[newchild2idx] = newchild2;

                        }
                        /*count -1 and count -2 are siblings*/
                        else if (nodes[nodecount - 1].parentIndex == nodes[nodecount - 2].parentIndex)
                        {

                            nodes[nodes[nodes[nodecount - 2].parentIndex].parentIndex] = newgrandparent;

                            //Debug.Log("22");
                            //Debug.Log(" ");
                            //Debug.Log(newparentidx);
                            //Debug.Log(nodes[nodecount - 2].parentIndex);
                            //Debug.Log(nodecount - 1);
                            //Debug.Log(nodes[nodecount - 1].parentIndex);

                            newlastbranchchild1.parentIndex = nodes[nodecount - 2].parentIndex;
                            newlastbranchchild2.parentIndex = nodes[nodecount - 2].parentIndex;

                            if (newparent.child1 == nodecount - 2)
                            {
                                newparent.child2 = nodes[nodecount - 2].parentIndex;
                                nodes[nodecount - 2] = newchild1;
                            }
                            else
                            {
                                newparent.child1 = nodes[nodecount - 2].parentIndex;
                                nodes[nodecount - 2] = newchild2;
                            }

                            newlastbranch.parentIndex = nodecount - 1;
                            nodes[nodes[nodecount - 1].child1] = newlastbranchchild1;
                            nodes[nodes[nodecount - 1].child2] = newlastbranchchild2;

                            nodes[nodecount - 1] = newparent;
                            nodes[newparentidx] = newlastbranch;

                        }
                        else
                        {
                            //Debug.Log("full sep");


                            /*if 1 node in common*/
                            if (newgrandparentidx == nodes[nodecount -1].child1 || newgrandparentidx == nodes[nodecount - 1].child2)
                            {
                                newgrandparent.parentIndex = newparentidx;
                            }
                            if (newlastbranchparentidx == nodes[newparentidx].child1 || newlastbranchparentidx == nodes[newparentidx].child2)
                            {
                                newlastbranchparent.parentIndex = nodecount-1;
                            }

                            nodes[nodes[nodecount - 1].child1] = newlastbranchchild1;
                            nodes[nodes[nodecount - 1].child2] = newlastbranchchild2;

                            nodes[newchild1idx] = newchild1;
                            nodes[newchild2idx] = newchild2;

                            nodes[newgrandparentidx] = newgrandparent;

                            nodes[newlastbranchparentidx] = newlastbranchparent;

                            nodes[newparentidx] = newlastbranch;
                            nodes[nodecount - 1] = newparent;
                        }

                    }


                }
                //Debug.Log("endroot " + PhyResolutionSystem.AABBtree.rootIndex);

                //Debug.Log("AAA " + nodes[nodecount - 2].parentIndex);

                //Debug.Log("BBB " + (nodecount - 1));
            }
        }

        ecb.DestroyEntity(entity);

    }

    //RENAME -> AMBIGUOUS
    public static NativeList<Entity> GatherOverlappingNodes(CircleShapeData CastSphere, PhysicsUtilities.CollisionLayer colLayer)
    {

        DynamicAABBTree<CircleShapeData> AABBtree = TreeInsersionSystem.AABBtree;

        //redondant ? OPTI ?
        //NativeArray<CircleShapeData> CirclesShapes = TreeInsersionSystem.CirclesShapesQuery.ToComponentDataArray<CircleShapeData>(Allocator.TempJob);

        //internal capacity ? OPTI
        NativeList<(Entity,float)> OverlapList = new NativeList<(Entity,float)>(30,Allocator.Temp);
        //NativeList<float> OverlapListDistance = new NativeList<float>(30, Allocator.Temp);


        NativeQueue<int> comparequeue = new NativeQueue<int>(Allocator.Temp);

        //Debug.Log(AABBtree.rootIndex);
        //Debug.Log(AABBtree.nodeCount);

        ///rejouter "&& isFiltterType au check de proximite ?
        ///


        NativeList<Entity> result;


        comparequeue.Enqueue(AABBtree.rootIndex);
        /*if there is a only a single body to check*/
        if (AABBtree.nodeCount <= 1)
        {
            if(AABBtree.nodeCount == 1)
            {
                AABBTreeNode node = AABBtree.nodes[comparequeue.Dequeue()];
                if (PhysicsUtilities.Proximity(AABBtree.nodes[0].box, CastSphere) < 0 && AABBtree.nodes[0].LayerMask == colLayer)
                {
                    //    float prox = PhysicsUtilities.Proximity(CirclesShapes[OverlapList[i]], CastSphere);
                    OverlapList.Add((AABBtree.nodes[0].bodyIndex,0f));
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

            if (!node.isLeaf)
            {
                float nodeA = PhysicsUtilities.Proximity(AABBtree.nodes[node.child1].box, CastSphere);
                float nodeB = PhysicsUtilities.Proximity(AABBtree.nodes[node.child2].box, CastSphere);

                //proximity cirlce - box here ?
                if (nodeA < 0)
                {
                    //Debug.Log(AABBtree.nodes[node.child1].LayerMask);

                    if (AABBtree.nodes[node.child1].isLeaf && AABBtree.nodes[node.child1].LayerMask == colLayer)
                    { 
                        OverlapList.Add((AABBtree.nodes[node.child1].bodyIndex, nodeA));
                    }
                    else
                        comparequeue.Enqueue(node.child1);
                }
                if (nodeB < 0)
                {

                    //Debug.Log(AABBtree.nodes[node.child2].LayerMask);

                    if (AABBtree.nodes[node.child2].isLeaf && AABBtree.nodes[node.child2].LayerMask == colLayer)
                    { 
                        OverlapList.Add((AABBtree.nodes[node.child2].bodyIndex, nodeB));
                    }
                    else
                        comparequeue.Enqueue(node.child2);
                }
            }
        }

        //testlist.CopyFrom(OverlapList);
        //testlist.Zip(OverlapListDistance, (entity, distance) => (entity, distance)).Select(item => item.entity);

        //OverlapList.Zip(OverlapListDistance, (entity, distance) => (entity, distance)).OrderBy(item => item.distance).Select(item => item.entity);

        //OverlapList.Sort((a, b) => a.CompareTo(b.pathCost));

        //NativeList<float> compareProx = new NativeList<float>(OverlapList.Length, Allocator.Temp);
        //NativeArray<int> OrderedOverlapArray = new NativeArray<int>(OverlapList.Length, Allocator.Temp);

        /*TO DO -> SORT THE LIST*/

        /*TO DO -> Fix Resolve narrow phase*/

        /*Resolve narrow phase and sort the OverlapList by proximity*/
        //for (int i = 0; i < OverlapList.Length; i++)
        //{
        //    float prox = PhysicsUtilities.Proximity(CirclesShapes[OverlapList[i]], CastSphere);


        //    /*If the AABB intersects by the actual shape does not*/
        //    if (0 > prox)
        //    {
        //        OverlapList.RemoveAt(i);
        //    }
        //    //else
        //    //{
        //    //    compareProx.Add(prox);
        //    //}

        //}

        ///MAKE SURE DISPOSE ALL

        comparequeue.Dispose();
        NativeListUtils.QuickSort(OverlapList, 0, OverlapList.Length - 1);
        result = NativeListUtils.SelectFirst(OverlapList);
        return result;

    }

    public static RayCastHit RaycastNode(Ray ray, PhysicsUtilities.CollisionLayer colLayer, ComponentLookup<CircleShapeData> CirclesShapesLookUp)
    {
        DynamicAABBTree<CircleShapeData> AABBtree = TreeInsersionSystem.AABBtree;

        //redondant ? OPTI ?
        //NativeArray<CircleShapeData> CirclesShapes = TreeInsersionSystem.CirclesShapesQuery.ToComponentDataArray<CircleShapeData>(Allocator.TempJob);
        //var CirclesShapesLookUp = SystemAPI.GetComponentLookup<CircleShapeData>(true);

        //internal capacity ? OPTI
        NativeList<(Entity, float)> OverlapList = new NativeList<(Entity, float)>(30, Allocator.Temp);

        NativeQueue<int> comparequeue = new NativeQueue<int>(Allocator.Temp);


        //NativeList<Entity> result;


        comparequeue.Enqueue(AABBtree.rootIndex);

        /*Gather all the intersecting AABB in unordered list*/
        while (comparequeue.Count > 0)
        {

            AABBTreeNode node = AABBtree.nodes[comparequeue.Dequeue()];

            if (!node.isLeaf)
            {
                float AABBdistanceA = PhysicsUtilities.Intersect(AABBtree.nodes[node.child1].box, ray);
                float AABBdistanceB = PhysicsUtilities.Intersect(AABBtree.nodes[node.child2].box, ray);

                if (AABBdistanceA >= 0)
                {
                    if (AABBtree.nodes[node.child1].LayerMask != PhysicsUtilities.CollisionLayer.PlayerLayer && AABBtree.nodes[node.child1].LayerMask != PhysicsUtilities.CollisionLayer.MonsterLayer && AABBtree.nodes[node.child1].isLeaf)
                        Debug.Log(AABBtree.nodes[node.child1].LayerMask);

                    if (AABBtree.nodes[node.child1].isLeaf && AABBtree.nodes[node.child1].LayerMask == colLayer)
                    {
                        /// test colision with actual physics shape
                        float distance = PhysicsUtilities.Intersect(CirclesShapesLookUp[AABBtree.nodes[node.child1].bodyIndex], ray);
                        if(distance>0)
                            OverlapList.Add((AABBtree.nodes[node.child1].bodyIndex, distance));
                    }
                    else
                        comparequeue.Enqueue(node.child1);
                }
                if (AABBdistanceB >= 0)
                {
                    if (AABBtree.nodes[node.child2].LayerMask != PhysicsUtilities.CollisionLayer.PlayerLayer && AABBtree.nodes[node.child2].LayerMask != PhysicsUtilities.CollisionLayer.MonsterLayer && AABBtree.nodes[node.child2].isLeaf)
                        Debug.Log(AABBtree.nodes[node.child2].LayerMask);

                    if (AABBtree.nodes[node.child2].isLeaf && AABBtree.nodes[node.child2].LayerMask == colLayer)
                    {
                        /// test colision with actual physics shape
                        float distance = PhysicsUtilities.Intersect(CirclesShapesLookUp[AABBtree.nodes[node.child2].bodyIndex], ray);
                        if (distance > 0)
                            OverlapList.Add((AABBtree.nodes[node.child2].bodyIndex, distance));
                    }
                    else
                        comparequeue.Enqueue(node.child2);
                }
            }
        }

        comparequeue.Dispose();
        NativeListUtils.QuickSort(OverlapList, 0, OverlapList.Length - 1);
        //result = NativeListUtils.SelectFirst(OverlapList);

        return OverlapList.Length > 0 ? new RayCastHit { entity = OverlapList[0].Item1, distance = OverlapList[0].Item2 } : new RayCastHit {entity = Entity.Null };


        //if (RayIntersectsAABB(ray, node.Box, out tmin))
        //{
        //    if (node.Entity != null)
        //    {
        //        result.Add(node.Entity);
        //    }

        //    RaycastNode(node.Left, ray, result);
        //    RaycastNode(node.Right, ray, result);
        //}


    }


}
//TO DO
[BurstCompile]
public partial struct OverlapSphere : IJob//IJob //ijobparralel ?
{

    [ReadOnly]
    public CircleShapeData CastSphere;

    [ReadOnly]
    public DynamicAABBTree<CircleShapeData> AABBtree;


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

