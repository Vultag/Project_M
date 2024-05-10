using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Collections;
using Unity.Entities;
using Unity.Entities.UniversalDelegates;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;


public struct AABB
{
    public Vector2 UpperBound;
    public Vector2 LowerBound;
}
//public struct InvalidatedNode
//{
//    public AABB box;
//    public int bodyIndex;
//    public int treeIndex;
//}
public struct AABBTreeNode
{

    public AABB box;
    public Entity bodyIndex;
    public int parentIndex;

    //int parent;
    //int next;

    public int child1;
    public int child2;
    public bool isLeaf;

    //for later?
    //bool moved;
}
//unsafe struct AABBTree
//{
//    //public AABBTreeNode* nodes;
//    public int nodeCount;
//    public int rootIndex;
//}


public unsafe struct DynamicAABBTree<T>
{

    //OPTI : Internal nodes are always even ; Leaf are odd ? no isleaf needed?


    //do list of nodes instead
    //private AABBTreeNode* nodes;
    public NativeArray<AABBTreeNode> nodes;
    //private List<AABBTreeNode> nodes;
    //private NativeReference<AABBTreeNode> nodes;
    //private int capacity;
    public int nodeCount;
    public int rootIndex;


    public void InsertLeaf(Entity bodyIndex, AABB box, NativeQueue<int> comparequeue)
    {
        //int newleafIndex = nodeCount;
        //nodeCount++;

        //if (nodeCount == 1)
        //{
        //    rootIndex = newleafIndex;
        //    return;
        //}

        //int bestSibling = SearchBestForInsert();

        int newleafIndex = nodeCount;
        nodeCount++;
        AABBTreeNode newleaf = nodes[newleafIndex] = new() { box = box, bodyIndex = bodyIndex, isLeaf = true };
        //int newleafIndex = AllocateLeafNode(bodyIndex, box);
        nodes[newleafIndex] = newleaf;
        if (nodeCount == 1)
        {
            rootIndex = newleafIndex;
        }
        else
        {
            //Debug.LogError(Mathf.Clamp(nodeCount - 1, 0, 100));
            int sibling = SearchBestForInsert(box, comparequeue);
            //int sibling = Mathf.Clamp(nodeCount - 2,0,1000);

            //if (sibling == rootIndex)
            //    Debug.LogWarning("rootindx");
            //if (sibling == nodeCount-1)
            //    Debug.LogWarning("node-1");
            //else if (sibling == nodeCount - 2)
            //    Debug.LogWarning("node-2");


            SplitNode(box, nodes[newleafIndex], newleafIndex, sibling);
            RefitHierarchy(nodes[newleafIndex].parentIndex);
        }

        //Debug.Log("insertion");
        //Debug.Log(nodeCount);

        //int sibling = SearchBestForInsert(box);
        //SplitNode(box, nodes[newleafIndex], newleafIndex, sibling);
        //RefitHierarchy(nodes[newleafIndex].parentIndex);
    }
    public void RemoveLeaf(int index)
    {
        //Debug.Log("count 1 = " + nodeCount);

        /*for safety*/ //OPTI
        //Debug.Log(nodeCount);
        if (nodeCount <= 1)
        {
            //Debug.LogError("1-2 to fix...");

            nodes[0] = default;
            nodeCount = 0;
            return;
        }
        //Debug.Break();

        int parentindex = nodes[index].parentIndex;
        int sibling;
        int newroot = rootIndex;

        AABBTreeNode newsibling;
        AABBTreeNode newgrandparent = nodes[nodes[parentindex].parentIndex];
        AABBTreeNode newlastleaf = nodes[nodeCount - 2];


        //Debug.LogError("root = " + rootIndex);
        //Debug.LogError("idx = " + index);
        //Debug.LogError("idxparent = " + parentindex);
        //Debug.LogError("idxlastparent = " + nodes[nodeCount - 2].parentIndex);
        //Debug.LogError("idxgrandparent = " + nodes[parentindex].parentIndex);
        //Debug.LogError("idxlastgrandparent = " + nodes[nodeCount - 1].parentIndex);

        /*if the last leaf is removed or its sibling*/
        /*early return*/
        if (parentindex == nodeCount - 1)
        {

            int grandparentidx = nodes[parentindex].parentIndex;
            //Debug.Log("1");
            if (index != nodeCount - 2)
            {
                /* if 1 root node with 2 leaf*/
                if (rootIndex == parentindex)
                {
                    //Debug.LogError("1");
                    newroot = 0;
                    //Debug.Log("ssssssssssssssssssssssssssssssssssssssssssssssssssssssssssssssssssss");
                    newlastleaf.parentIndex = 0;
                    nodes[index] = newlastleaf;
                    nodes[nodeCount - 2] = default;
                    nodes[nodeCount - 1] = default;
                    nodeCount -= 2;
                    rootIndex = newroot;
                    //RefitHierarchy(0);
                    return;
                }
                else
                {
                    //Debug.LogError("2");
                    if (newgrandparent.child1 == parentindex)
                        newgrandparent.child1 = index;
                    else
                        newgrandparent.child2 = index;

                    newlastleaf.parentIndex = nodes[parentindex].parentIndex;
                    nodes[nodes[parentindex].parentIndex] = newgrandparent;

                    nodes[index] = newlastleaf;
                    nodes[nodeCount - 2] = default;
                    nodes[nodeCount - 1] = default;
                    nodeCount -= 2;
                    rootIndex = newroot;
                    RefitHierarchy(grandparentidx);
                    //Debug.Log("count 2 = " + nodeCount);
                    return;
                }
            }
            else
            {
                //Debug.LogError("3");
                sibling = GetSiblingNode(index);

                /*parent index is root case*/
                if (rootIndex == parentindex)
                {
                    newroot = sibling;
                    newsibling = nodes[sibling];
                    newsibling.parentIndex = sibling;
                    nodes[sibling] = newsibling;

                    nodes[nodeCount - 2] = default;
                    nodes[nodeCount - 1] = default;
                    nodeCount -= 2;
                    rootIndex = newroot;
                    RefitHierarchy(sibling);
                }
                else
                {
                    newsibling = nodes[sibling];

                    if (newgrandparent.child1 == parentindex)
                        newgrandparent.child1 = sibling;
                    else
                        newgrandparent.child2 = sibling;

                    newsibling.parentIndex = nodes[parentindex].parentIndex;
                    nodes[sibling] = newsibling;
                    nodes[nodes[parentindex].parentIndex] = newgrandparent;

                    nodes[nodeCount - 2] = default;
                    nodes[nodeCount - 1] = default;
                    nodeCount -= 2;
                    rootIndex = newroot;
                    RefitHierarchy(grandparentidx);
                }
      
                //Debug.Log("count 2 = " + nodeCount);
                return;
            }

        }
        //Debug.Log("2dephase");


        /*collapse the nodes's references where the leaf has been freed*/

        AABBTreeNode newlastsibling;
        int lastsibling;

        AABBTreeNode newlastparent = nodes[nodeCount - 1];
        AABBTreeNode newlastgrandparent = nodes[nodes[nodeCount - 1].parentIndex];



        /*redirect the last nodes's references where the leaf has been freed*/



        if (newlastparent.child1 == nodeCount - 2)
        {
            newlastleaf.parentIndex = parentindex;

            newlastparent.child1 = index;

            lastsibling = newlastparent.child2;
            newlastsibling = nodes[lastsibling];
            newlastsibling.parentIndex = parentindex;

            //nodes[newlastparent.child2] = newlastsibling;

        }
        else
        {
            newlastleaf.parentIndex = parentindex;

            newlastparent.child2 = index;

            lastsibling = newlastparent.child1;
            newlastsibling = nodes[lastsibling];
            newlastsibling.parentIndex = parentindex;


            //nodes[newlastparent.child1] = newlastsibling;
        }
        if (newlastgrandparent.child1 == nodeCount - 1)
        {
            newlastgrandparent.child1 = parentindex;
        }
        else
        {
            newlastgrandparent.child2 = parentindex;
        }

        if (nodes[parentindex].child1 == index)
        {
            sibling = nodes[parentindex].child2;
        }
        else
        {
            sibling = nodes[parentindex].child1;
        }

        newsibling = nodes[sibling];

        if(newgrandparent.child1 == parentindex)
        {
            newgrandparent.child1 = sibling;
        }
        else
        {
            newgrandparent.child2 = sibling;
        }

        //Debug.LogError("sibling = " + sibling);
        //Debug.LogError("lastsibling = " + lastsibling);

        /*edge case solving...*/
        if (rootIndex != nodeCount - 1)
        {
            if (nodes[nodeCount - 1].parentIndex == parentindex)
            {
                nodes[lastsibling] = newlastsibling;
                //more ?

            }
            else
            {
                if(rootIndex != parentindex)
                {
                    /*same grandparent case*/
                    if (nodes[nodeCount - 1].parentIndex == nodes[parentindex].parentIndex)
                    {
                        //Debug.LogError("4");
                        //Debug.LogError("oooo");

                        //Debug.LogError("oooo");
                        if (nodes[nodes[parentindex].parentIndex].child1 == nodeCount - 1)
                        {
                            newgrandparent.child1 = parentindex;
                            newgrandparent.child2 = sibling;
                        }
                        else
                        {
                            newgrandparent.child2 = parentindex;
                            newgrandparent.child1 = sibling;
                        }
                        newsibling.parentIndex = nodes[parentindex].parentIndex;

                        nodes[nodes[parentindex].parentIndex] = newgrandparent;
                        nodes[sibling] = newsibling;
                        nodes[lastsibling] = newlastsibling;
                        nodes[index] = newlastleaf;
                        nodes[parentindex] = newlastparent;

                        nodes[nodeCount - 2] = default;
                        nodes[nodeCount - 1] = default;
                        nodeCount -= 2;
                        //rootIndex = newroot;
                        RefitHierarchy(parentindex);

                        //Debug.Log("count 2 = " + nodeCount);
                        return;
                    }
                    else
                    {
                        //Debug.LogError("ddddddddddddddddd");
                        //?

                        nodes[nodes[nodeCount - 1].parentIndex] = newlastgrandparent;

                        if (nodes[parentindex].parentIndex != nodeCount - 1 && nodes[parentindex].parentIndex != lastsibling)
                        {
                            //Debug.LogError("ici");
                            nodes[lastsibling] = newlastsibling;
                        }

                    }
                }
                else
                {
                    //Debug.LogError("dsdsd");
                    //?
                    if(nodes[nodes[nodeCount - 1].parentIndex].parentIndex == rootIndex)
                    {
                        newlastgrandparent.parentIndex = nodes[nodeCount - 1].parentIndex;
                    }
                    else
                    {
                        newsibling.parentIndex = sibling;
                        nodes[sibling] = newsibling;

                    }

                    nodes[nodes[nodeCount - 1].parentIndex] = newlastgrandparent;

                    nodes[lastsibling] = newlastsibling;
                    

                }
            }
        }
        else
        {
            //Debug.LogError("here 1");
            //Debug.LogError("1");
            newroot = parentindex;
            newlastparent.parentIndex = parentindex;

        }


        
        if(rootIndex != parentindex)
        {

            if (nodes[parentindex].parentIndex == nodeCount - 1)
            {
                //Debug.LogError("here " + nodes[nodeCount - 1].child1);
                //Debug.LogError("next " + nodes[nodeCount - 1].child2);
                //???? useless ?
                if (nodes[nodeCount-1].child1 == nodeCount - 2)
                {
                    newlastparent.child2 = sibling;
                }
                else
                {
                    newlastparent.child1 = sibling;
                }
                //nodes[sibling] = newsibling;

            }
            else
            {
                if (nodes[parentindex].parentIndex == lastsibling)
                {
                    //Debug.LogError("2");
                    newgrandparent.parentIndex = parentindex;
                    //not necessary ?
                    if (nodes[nodes[parentindex].parentIndex].child1 == parentindex)
                    {
                        newgrandparent.child1 = sibling;
                    }
                    else
                    {
                        newgrandparent.child2 = sibling;
                    }

                    newsibling.parentIndex = nodes[parentindex].parentIndex;

                    nodes[nodes[parentindex].parentIndex] = newgrandparent;
                    nodes[sibling] = newsibling;
                }
                else
                {
                    /*not same grandparent case*/
                    if (nodes[parentindex].parentIndex != nodes[nodeCount - 1].parentIndex)
                    {
                        //Debug.LogError("ee");
                        //Debug.LogError("ddddddddddddddddd");

                        if (sibling != nodeCount -1)
                        {
                            if(sibling != nodes[nodeCount - 1].parentIndex)
                            {
                                //Debug.LogError("eenext");
                                newsibling.parentIndex = nodes[parentindex].parentIndex;
                                nodes[nodes[parentindex].parentIndex] = newgrandparent;
                                nodes[sibling] = newsibling;
                                if(rootIndex == nodeCount-1)
                                {
                                    nodes[lastsibling] = newlastsibling;
                                }
                            }
                            else
                            {
                                newlastgrandparent.parentIndex = nodes[parentindex].parentIndex;
                                nodes[nodes[parentindex].parentIndex] = newgrandparent;
                                nodes[nodes[nodeCount - 1].parentIndex] = newlastgrandparent;
                            }
               
                        }
                        else
                        {
                            newlastparent.parentIndex = nodes[parentindex].parentIndex;
                        }

                    }

                }

            }
        }
        else
        {
            //Debug.LogError("x");
            if (sibling != nodeCount - 1)
            {
                newroot = sibling;
            }
        }

        //Debug.LogError("5");

        nodes[index] = newlastleaf;
        nodes[parentindex] = newlastparent;

        rootIndex = newroot;

        /*
         * remplace the remove node/nodeparent with the last one and free the last one/reduce count by 1
         * nodeCount - 2 = last leaf node
         * nodeCount - 1 = last leafparent node
         */
        nodes[nodeCount - 2] = default;
        nodes[nodeCount - 1]= default;
        nodeCount -= 2;
        //??
        RefitHierarchy(parentindex);

    }
    private int GetSiblingNode(int index)
    {
        return nodes[nodes[index].parentIndex].child1==index? nodes[nodes[index].parentIndex].child2 : nodes[nodes[index].parentIndex].child1;
    }

    private int SearchBestForInsert(AABB box, NativeQueue<int> comparequeue)
    {
        //branch and bound
        //Debug.Log(rootIndex);
        //int index = rootIndex;
        //VERIFY
        //float bestcost = Area(Union(nodes[rootIndex].box, box)) + IntersectionArea(nodes[rootIndex].box, box);
        float bestcost = Area(Union(nodes[rootIndex].box, box));
        //MANAGED DATA? bing problem?
        //Stack<int> comparequeue = new Stack<int>();

        // allocating at each insert ? bad ? OPTI ?
        //NativeQueue<int> comparequeue = new NativeQueue<int>(Allocator.Temp);

        comparequeue.Enqueue(rootIndex);

        int bestinsert = rootIndex;

        while (comparequeue.Count > 0)
        {
            //Debug.Log(nodes[index].isLeaf);
            int index = comparequeue.Dequeue();
            var node = nodes[index];
            //PhyResolutionSystem.DrawQuad(node.box.LowerBound, node.box.UpperBound, Color.red);
            //if (node.isLeaf)
            //    continue;

            var child1 = node.child1;
            var child2 = node.child2;
            var box1 = nodes[child1].box;
            var box2 = nodes[child2].box;
            //verify
            float inheritedcost = GetInheritedCost(index, box);


            var cost1 = Area(Union(box1, box)) + inheritedcost;
            var cost2 = Area(Union(box2, box)) + inheritedcost;
     

            if (cost2 < bestcost)
            {

                //unnessesary ? OPTI
                if (Area(box) + GetInheritedCost(child2, box) < bestcost)
                {
                    //Que child if it is worth evaluating
                    if (!nodes[child2].isLeaf)
                        comparequeue.Enqueue(child2);
                    else if (cost2 < cost1)
                    {
                        bestinsert = child2;
                        bestcost = cost2;
                    }
                }
                //SOMETHING MISSING HERE??
                //else if (cost2 < cost1)
                //{
                //    bestinsert = child2;
                //    bestcost = cost2;
                //}

            }
            if (cost1 < bestcost)
            {
                //unnessesary ? OPTI
                if (Area(box) + GetInheritedCost(child1, box) < bestcost)
                {
                    //Que child if it is worth evaluating
                    if (!nodes[child1].isLeaf)
                        comparequeue.Enqueue(child1);
                    else if(cost1<cost2)
                    {
                        bestinsert = child1;
                        bestcost = cost1;
                    }

                }
                //SOMETHING MISSING HERE??
                //else if (cost1 < cost2)
                //{
                //    bestinsert = child1;
                //    bestcost = cost1;
                //}

            }

        }

        return bestinsert;
    }
    private float GetInheritedCost(int target, AABB box)
    {
        float cost = 0;
        int index = target;

        while(true)
        {
            cost += Area(Union(nodes[index].box, box)) - Area(nodes[index].box);
            if (rootIndex == index)
                break;
            index = nodes[index].parentIndex;
        }

        return cost;
    }
    private void SplitNode(AABB box, AABBTreeNode leafnode, int newleafIndex, int siblingid)
    {

        AABBTreeNode newSibling = nodes[siblingid];
        
        int index = nodeCount;
        nodeCount++;
        AABBTreeNode newParent = nodes[index] = new() { isLeaf = false };

        newParent.box = Union(box, nodes[siblingid].box);


        // The sibling was the root
        if (siblingid == rootIndex)
        {
            newParent.child1 = siblingid;
            newParent.child2 = newleafIndex;
            /*important*/
            newParent.parentIndex = index;

            newSibling.parentIndex = index;
            leafnode.parentIndex = index;
            rootIndex = index;

            nodes[siblingid] = newSibling;
            nodes[index] = newParent;
            nodes[newleafIndex] = leafnode;

        }
        // The sibling was not the root
        else
        {
            int oldParentid = nodes[siblingid].parentIndex;
            AABBTreeNode newoldparent = nodes[oldParentid];
            newParent.parentIndex = oldParentid;

           
            if (nodes[oldParentid].child1 == siblingid)
            {
                newoldparent.child1 = index;
            }
            else
            {
                newoldparent.child2 = index;
            }
            newParent.child1 = siblingid;
            newParent.child2 = newleafIndex;
            newSibling.parentIndex = index;
            leafnode.parentIndex = index;
        
            nodes[oldParentid] = newoldparent;
            nodes[siblingid] = newSibling;
            nodes[index] = newParent;
            nodes[newleafIndex] = leafnode;
        }

    }
    public void RefitHierarchy(int startIndex)
    {
        //PhyResolutionSystem.DrawQuad(nodes[startIndex].box.LowerBound, nodes[startIndex].box.UpperBound, Color.red);
        while (true)
        {
            //Debug.Log("test");
            int child1 = nodes[startIndex].child1;
            int child2 = nodes[startIndex].child2;
            AABBTreeNode newnode = nodes[startIndex];
            newnode.box = Union(nodes[child1].box, nodes[child2].box);
            nodes[startIndex] = newnode;
           // PhyResolutionSystem.DrawQuad(newnode.box.LowerBound, newnode.box.UpperBound, Color.red);
            if (startIndex == rootIndex)
                break;
            startIndex = nodes[startIndex].parentIndex;
   
        }
    }



    //go down the tree till leaf and test for each intersecting leaf at each step
    public void GatherIntersectingNodes(NativeList<CollisionPair> ColPair, int index)
    {

        //List<CollisionPair> ColPair = new List<CollisionPair>();


        //to test
        //int numberofchecks = 0;
        if (nodeCount == 0)
            return;

        if (!nodes[index].isLeaf)
        {
            TryRegisterCollisionPair(ColPair,nodes[index].child1, nodes[index].child2);

            GatherIntersectingNodes(ColPair,nodes[index].child1);
            GatherIntersectingNodes(ColPair,nodes[index].child2);

            //numberofchecks += GatherIntersectingNodes(nodes[index].child1);
            //numberofchecks += GatherIntersectingNodes(nodes[index].child2);
        }

        //return ColPair;
    }
    private bool IsOverlapping(int nodeA,int nodeB)
    {
        return (0 < PhysicsUtilities.Proximity(nodes[nodeA].box, nodes[nodeB].box));
    }
    //Decend into the tree whenever there is an intersection to see if the intersection lands and two leafs.
    //Then add the collision pair
    private void TryRegisterCollisionPair(NativeList<CollisionPair> ColPair, int nodeA, int nodeB)
    {
        //to test num of colision
        //int temp = 0;

        if (IsOverlapping(nodeA, nodeB))
        {

            if (nodes[nodeA].isLeaf)
            {
                //both leaves, add pair
                if (nodes[nodeB].isLeaf)
                {
                    //register the pair here
                    //temp += 2;
                    ColPair.Add(new CollisionPair {BodyA = nodes[nodeA].bodyIndex,BodyB = nodes[nodeB].bodyIndex });

                }
                //1 leaf, 1 internal
                else
                {
                    TryRegisterCollisionPair(ColPair, nodeA, nodes[nodeB].child1);
                    TryRegisterCollisionPair(ColPair, nodeA, nodes[nodeB].child2);
                }
            }
            else
            {
                //1 leaf, 1 internal
                if (nodes[nodeB].isLeaf)
                {
                    TryRegisterCollisionPair(ColPair, nodeB, nodes[nodeA].child1);
                    TryRegisterCollisionPair(ColPair, nodeB, nodes[nodeA].child2);
                }
                //both internal, decend into tree
                else
                {
                    TryRegisterCollisionPair(ColPair, nodes[nodeA].child1, nodes[nodeB].child1);
                    TryRegisterCollisionPair(ColPair, nodes[nodeA].child1, nodes[nodeB].child2);
                    TryRegisterCollisionPair(ColPair, nodes[nodeA].child2, nodes[nodeB].child1);
                    TryRegisterCollisionPair(ColPair, nodes[nodeA].child2, nodes[nodeB].child2);
                }

            }
        }
        //return temp;
    }

    public AABB Union(AABB A, AABB B)
    {
        AABB C;
        C.LowerBound = new Vector2 (Mathf.Min(A.LowerBound.x, B.LowerBound.x), Mathf.Min(A.LowerBound.y, B.LowerBound.y));
        C.UpperBound = new Vector2(Mathf.Max(A.UpperBound.x, B.UpperBound.x), Mathf.Max(A.UpperBound.y, B.UpperBound.y));
        return C;
    }
 
    //surface area
    public float Area(AABB A)
    {
        Vector2 d = A.UpperBound - A.LowerBound;
        return d.x*d.y;
    }


}


