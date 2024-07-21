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
public struct AABBTreeNode
{

    public AABB box;
    public Entity bodyIndex;
    public int parentIndex;
    ///for coll filtering!:
    public PhysicsUtilities.CollisionLayer LayerMask;

    public int child1;
    public int child2;
    public bool isLeaf;

}

public unsafe struct DynamicAABBTree<T>
{

    //OPTI : Internal nodes are always even ; Leaf are odd ? no isleaf needed?

    public NativeArray<AABBTreeNode> nodes;
    public int nodeCount;
    public int rootIndex;


    public void InsertLeaf(Entity bodyIndex, PhysicsUtilities.CollisionLayer colLayer, AABB box, NativeQueue<int> comparequeue)
    {

        int newleafIndex = nodeCount;
        nodeCount++;
        AABBTreeNode newleaf = nodes[newleafIndex] = new() { box = box, bodyIndex = bodyIndex, LayerMask = colLayer, isLeaf = true };

        nodes[newleafIndex] = newleaf;
        if (nodeCount == 1)
        {
            rootIndex = newleafIndex;
        }
        else
        {
            int sibling = SearchBestForInsert(box, comparequeue);

            SplitNode(box, nodes[newleafIndex], newleafIndex, sibling);
            RefitHierarchy(nodes[newleafIndex].parentIndex);
        }

    }
    public void RemoveLeaf(int index)
    {

        if (nodeCount <= 1)
        {
            nodes[0] = default;
            nodeCount = 0;
            return;
        }

        int parentindex = nodes[index].parentIndex;
        int sibling;
        int newroot = rootIndex;

        AABBTreeNode newsibling;
        AABBTreeNode newgrandparent = nodes[nodes[parentindex].parentIndex];
        AABBTreeNode newlastleaf = nodes[nodeCount - 2];

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
                    newlastleaf.parentIndex = 0;
                    nodes[index] = newlastleaf;
                    nodes[nodeCount - 2] = default;
                    nodes[nodeCount - 1] = default;
                    nodeCount -= 2;
                    rootIndex = newroot;
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

        }
        else
        {
            newlastleaf.parentIndex = parentindex;

            newlastparent.child2 = index;

            lastsibling = newlastparent.child1;
            newlastsibling = nodes[lastsibling];
            newlastsibling.parentIndex = parentindex;

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
                        RefitHierarchy(parentindex);

                        return;
                    }
                    else
                    {
                        nodes[nodes[nodeCount - 1].parentIndex] = newlastgrandparent;

                        if (nodes[parentindex].parentIndex != nodeCount - 1 && nodes[parentindex].parentIndex != lastsibling)
                        {
                            nodes[lastsibling] = newlastsibling;
                        }

                    }
                }
                else
                {
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
            newroot = parentindex;
            newlastparent.parentIndex = parentindex;

        }


        
        if(rootIndex != parentindex)
        {

            if (nodes[parentindex].parentIndex == nodeCount - 1)
            {
                if (nodes[nodeCount-1].child1 == nodeCount - 2)
                {
                    newlastparent.child2 = sibling;
                }
                else
                {
                    newlastparent.child1 = sibling;
                }

            }
            else
            {
                if (nodes[parentindex].parentIndex == lastsibling)
                {
                    newgrandparent.parentIndex = parentindex;

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

        RefitHierarchy(parentindex);

    }
    private int GetSiblingNode(int index)
    {
        return nodes[nodes[index].parentIndex].child1==index? nodes[nodes[index].parentIndex].child2 : nodes[nodes[index].parentIndex].child1;
    }

    private int SearchBestForInsert(AABB box, NativeQueue<int> comparequeue)
    {
        float bestcost = Area(Union(nodes[rootIndex].box, box));

        comparequeue.Enqueue(rootIndex);

        int bestinsert = rootIndex;

        while (comparequeue.Count > 0)
        {
            int index = comparequeue.Dequeue();
            var node = nodes[index];

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

                if (Area(box) + GetInheritedCost(child2, box) < bestcost)
                {
                    //Queue child if it is worth evaluating
                    if (!nodes[child2].isLeaf)
                        comparequeue.Enqueue(child2);
                    else if (cost2 < cost1)
                    {
                        bestinsert = child2;
                        bestcost = cost2;
                    }
                }

            }
            if (cost1 < bestcost)
            {
                if (Area(box) + GetInheritedCost(child1, box) < bestcost)
                {
                    //Queue child if it is worth evaluating
                    if (!nodes[child1].isLeaf)
                        comparequeue.Enqueue(child1);
                    else if(cost1<cost2)
                    {
                        bestinsert = child1;
                        bestcost = cost1;
                    }

                }

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
        while (true)
        {
            int child1 = nodes[startIndex].child1;
            int child2 = nodes[startIndex].child2;
            AABBTreeNode newnode = nodes[startIndex];
            newnode.box = Union(nodes[child1].box, nodes[child2].box);
            nodes[startIndex] = newnode;
            if (startIndex == rootIndex)
                break;
            startIndex = nodes[startIndex].parentIndex;
   
        }
    }

    /*Last Node permutation -> critical to preserve the tree coherency and have RemoveLeaf() work as intended-> redirect node-1 to be node-2's parent*/
    public void LastNodePermutation()
    {
        var nodes = TreeInsersionSystem.AABBtree.nodes;

        int nodecount = TreeInsersionSystem.AABBtree.nodeCount;

        int lastNoderemplacementID = nodes[nodecount - 2].parentIndex;
        AABBTreeNode lastNodeRemplacement = nodes[lastNoderemplacementID];
        AABBTreeNode formerLastNode = nodes[nodecount - 1];
        AABBTreeNode lastNodeRemplacementParent = nodes[nodes[lastNoderemplacementID].parentIndex];
        AABBTreeNode formerLastNodeParent = nodes[nodes[nodecount - 1].parentIndex];

        AABBTreeNode lastNodeRemplacementChild1;
        AABBTreeNode lastNodeRemplacementChild2;
        AABBTreeNode formerLastNodeChild1;
        AABBTreeNode formerLastNodeChild2;

        /* node-2parent is root case */
        if (lastNoderemplacementID == rootIndex)
        {
            /* node-1 / node-2 sibling case */
            if (formerLastNode.parentIndex == lastNoderemplacementID)
            {
                //Debug.Log("1");

                if (nodes[lastNoderemplacementID].child1 == nodecount - 1)
                {
                    lastNodeRemplacement.child1 = lastNoderemplacementID;

                    lastNodeRemplacementChild2 = nodes[nodes[lastNoderemplacementID].child2];
                    lastNodeRemplacementChild2.parentIndex = nodecount - 1;
                    nodes[nodes[lastNoderemplacementID].child2] = lastNodeRemplacementChild2;
                }
                else
                {
                    lastNodeRemplacement.child2 = lastNoderemplacementID;

                    lastNodeRemplacementChild1 = nodes[nodes[lastNoderemplacementID].child1];
                    lastNodeRemplacementChild1.parentIndex = nodecount - 1;
                    nodes[nodes[lastNoderemplacementID].child1] = lastNodeRemplacementChild1;
                }
                formerLastNode.parentIndex = nodecount - 1;

                formerLastNodeChild1 = nodes[nodes[nodecount - 1].child1];
                formerLastNodeChild2 = nodes[nodes[nodecount - 1].child2];

                formerLastNodeChild1.parentIndex = lastNoderemplacementID;
                formerLastNodeChild2.parentIndex = lastNoderemplacementID;

                nodes[nodes[nodecount - 1].child1] = formerLastNodeChild1;
                nodes[nodes[nodecount - 1].child2] = formerLastNodeChild2;

                lastNodeRemplacement.parentIndex = nodecount - 1;
                nodes[nodecount - 1] = lastNodeRemplacement;
                nodes[lastNoderemplacementID] = formerLastNode;

            }
            else
            {
                //Debug.Log("2");

                if (formerLastNodeParent.child1 == nodecount - 1)
                {
                    formerLastNodeParent.child1 = lastNoderemplacementID;
                }
                else
                {
                    formerLastNodeParent.child2 = lastNoderemplacementID;
                }
                nodes[formerLastNode.parentIndex] = formerLastNodeParent;


                lastNodeRemplacementChild1 = nodes[nodes[lastNoderemplacementID].child1];
                lastNodeRemplacementChild2 = nodes[nodes[lastNoderemplacementID].child2];
                formerLastNodeChild1 = nodes[nodes[nodecount - 1].child1];
                formerLastNodeChild2 = nodes[nodes[nodecount - 1].child2];

                lastNodeRemplacementChild1.parentIndex = nodecount - 1;
                lastNodeRemplacementChild2.parentIndex = nodecount - 1;
                formerLastNodeChild1.parentIndex = lastNoderemplacementID;
                formerLastNodeChild2.parentIndex = lastNoderemplacementID;

                nodes[nodes[lastNoderemplacementID].child1] = lastNodeRemplacementChild1;
                nodes[nodes[lastNoderemplacementID].child2] = lastNodeRemplacementChild2;
                nodes[nodes[nodecount - 1].child1] = formerLastNodeChild1;
                nodes[nodes[nodecount - 1].child2] = formerLastNodeChild2;

                lastNodeRemplacement.parentIndex = nodecount - 1;
                nodes[nodecount - 1] = lastNodeRemplacement;
                nodes[lastNoderemplacementID] = formerLastNode;

            }

            rootIndex = nodecount - 1;

            return;
        }
        /* node-1 is root case */
        if (nodecount - 1 == rootIndex)
        {
            /* node-1 grandparent of node-2 case */
            if (lastNodeRemplacement.parentIndex == nodecount - 1)
            {
                //Debug.Log("3");

                if (nodes[nodecount-1].child1 == lastNoderemplacementID)
                {
                    formerLastNode.child1 = nodecount - 1;

                    formerLastNodeChild2 = nodes[nodes[nodecount-1].child2];
                    formerLastNodeChild2.parentIndex = lastNoderemplacementID;
                    nodes[nodes[nodecount - 1].child2] = formerLastNodeChild2;
                }
                else
                {
                    formerLastNode.child2 = nodecount - 1;

                    formerLastNodeChild1 = nodes[nodes[nodecount - 1].child1];
                    formerLastNodeChild1.parentIndex = lastNoderemplacementID;
                    nodes[nodes[nodecount - 1].child1] = formerLastNodeChild1;
                }
                lastNodeRemplacement.parentIndex = lastNoderemplacementID;

                lastNodeRemplacementChild1 = nodes[nodes[lastNoderemplacementID].child1];
                lastNodeRemplacementChild2 = nodes[nodes[lastNoderemplacementID].child2];

                lastNodeRemplacementChild1.parentIndex = nodecount - 1;
                lastNodeRemplacementChild2.parentIndex = nodecount - 1;

                nodes[nodes[lastNoderemplacementID].child1] = lastNodeRemplacementChild1;
                nodes[nodes[lastNoderemplacementID].child2] = lastNodeRemplacementChild2;

                formerLastNode.parentIndex = lastNoderemplacementID;
                nodes[nodecount - 1] = lastNodeRemplacement;
                nodes[lastNoderemplacementID] = formerLastNode;

            }
            else
            {
                //Debug.Log("4");

                if (lastNodeRemplacementParent.child1 == lastNoderemplacementID)
                {
                    lastNodeRemplacementParent.child1 = nodecount - 1;
                }
                else
                {
                    lastNodeRemplacementParent.child2 = nodecount - 1;
                }
                nodes[lastNodeRemplacement.parentIndex] = lastNodeRemplacementParent;


                lastNodeRemplacementChild1 = nodes[nodes[lastNoderemplacementID].child1];
                lastNodeRemplacementChild2 = nodes[nodes[lastNoderemplacementID].child2];
                formerLastNodeChild1 = nodes[nodes[nodecount - 1].child1];
                formerLastNodeChild2 = nodes[nodes[nodecount - 1].child2];

                lastNodeRemplacementChild1.parentIndex = nodecount - 1;
                lastNodeRemplacementChild2.parentIndex = nodecount - 1;
                formerLastNodeChild1.parentIndex = lastNoderemplacementID;
                formerLastNodeChild2.parentIndex = lastNoderemplacementID;

                nodes[nodes[lastNoderemplacementID].child1] = lastNodeRemplacementChild1;
                nodes[nodes[lastNoderemplacementID].child2] = lastNodeRemplacementChild2;
                nodes[nodes[nodecount - 1].child1] = formerLastNodeChild1;
                nodes[nodes[nodecount - 1].child2] = formerLastNodeChild2;

                formerLastNode.parentIndex = lastNoderemplacementID;
                nodes[nodecount - 1] = lastNodeRemplacement;
                nodes[lastNoderemplacementID] = formerLastNode;

            }

            rootIndex = lastNoderemplacementID;

            return;
        }


        /* node-1 grandparend of node-2 case */
        if (nodes[nodes[nodecount - 2].parentIndex].parentIndex == nodeCount-1)
        {
            //Debug.Log("5");

            if (nodes[formerLastNode.parentIndex].child1 == nodecount - 1)
            {
                formerLastNodeParent.child1 = lastNoderemplacementID;
            }
            else
            {
                formerLastNodeParent.child2 = lastNoderemplacementID;
            }
            nodes[formerLastNode.parentIndex] = formerLastNodeParent;

            if(nodes[nodecount - 1].child1 == lastNoderemplacementID)
            {
                formerLastNode.child1 = nodecount - 1;
            }
            else
            {
                formerLastNode.child2 = nodecount - 1;
            }
            lastNodeRemplacement.parentIndex = lastNoderemplacementID;

            lastNodeRemplacementChild1 = nodes[nodes[lastNoderemplacementID].child1];
            lastNodeRemplacementChild2 = nodes[nodes[lastNoderemplacementID].child2];
            formerLastNodeChild1 = nodes[nodes[nodecount - 1].child1];
            formerLastNodeChild2 = nodes[nodes[nodecount - 1].child2];

            lastNodeRemplacementChild1.parentIndex = nodecount - 1;
            lastNodeRemplacementChild2.parentIndex = nodecount - 1;
            if(nodes[nodecount - 1].child1 == lastNoderemplacementID)
            {
                formerLastNodeChild2.parentIndex = lastNoderemplacementID;
                nodes[nodes[nodecount - 1].child2] = formerLastNodeChild2;
            }
            else
            {
                formerLastNodeChild1.parentIndex = lastNoderemplacementID;
                nodes[nodes[nodecount - 1].child1] = formerLastNodeChild1;
            }

            nodes[nodes[lastNoderemplacementID].child1] = lastNodeRemplacementChild1;
            nodes[nodes[lastNoderemplacementID].child2] = lastNodeRemplacementChild2;

            nodes[nodecount - 1] = lastNodeRemplacement;
            nodes[lastNoderemplacementID] = formerLastNode;

            return;

        }
        /* node-1 / node-2 sibling case */
        if (nodes[nodecount - 2].parentIndex == nodes[nodecount - 1].parentIndex)
        {
            //Debug.Log("6");

            if (lastNodeRemplacementParent.child1 == lastNoderemplacementID)
            {
                lastNodeRemplacementParent.child1 = nodecount - 1;
            }
            else
            {
                lastNodeRemplacementParent.child2 = nodecount - 1;
            }
            nodes[nodes[lastNoderemplacementID].parentIndex] = lastNodeRemplacementParent;

            if (nodes[lastNoderemplacementID].child1 == nodecount - 1)
            {
                lastNodeRemplacement.child1 = lastNoderemplacementID;
            }
            else
            {
                lastNodeRemplacement.child2 = lastNoderemplacementID;
            }
            formerLastNode.parentIndex = nodecount - 1;

            lastNodeRemplacementChild1 = nodes[nodes[lastNoderemplacementID].child1];
            lastNodeRemplacementChild2 = nodes[nodes[lastNoderemplacementID].child2];
            formerLastNodeChild1 = nodes[nodes[nodecount - 1].child1];
            formerLastNodeChild2 = nodes[nodes[nodecount - 1].child2];

            formerLastNodeChild1.parentIndex = lastNoderemplacementID;
            formerLastNodeChild2.parentIndex = lastNoderemplacementID;
            if (nodes[lastNoderemplacementID].child1 == nodecount - 1)
            {
                lastNodeRemplacementChild2.parentIndex = nodecount - 1;
                nodes[nodes[lastNoderemplacementID].child2] = lastNodeRemplacementChild2;
            }
            else
            {
                lastNodeRemplacementChild1.parentIndex = nodecount - 1;
                nodes[nodes[lastNoderemplacementID].child1] = lastNodeRemplacementChild1;
            }

            nodes[nodes[nodecount - 1].child1] = formerLastNodeChild1;
            nodes[nodes[nodecount - 1].child2] = formerLastNodeChild2;

            nodes[nodecount - 1] = lastNodeRemplacement;
            nodes[lastNoderemplacementID] = formerLastNode;

            return;


        }

        /* node-1 / node-2 sibling case */
        if (lastNodeRemplacement.parentIndex == formerLastNode.parentIndex)
        {
            //Debug.Log("7");

            if (nodes[lastNodeRemplacement.parentIndex].child1 == nodecount-1)
            {
                lastNodeRemplacementParent.child1 = lastNoderemplacementID;
                lastNodeRemplacementParent.child2 = nodecount - 1;
            }
            else
            {
                lastNodeRemplacementParent.child2 = lastNoderemplacementID;
                lastNodeRemplacementParent.child1 = nodecount - 1;
            }
            nodes[lastNodeRemplacement.parentIndex] = lastNodeRemplacementParent;
        }
        else
        {
            //Debug.Log("8");

            if (nodes[lastNodeRemplacement.parentIndex].child1 == lastNoderemplacementID)
            {
                lastNodeRemplacementParent.child1 = nodecount - 1;
            }
            else
            {
                lastNodeRemplacementParent.child2 = nodecount - 1;
            }
            if (nodes[formerLastNode.parentIndex].child1 == nodecount - 1)
            {
                formerLastNodeParent.child1 = lastNoderemplacementID;
            }
            else
            {
                formerLastNodeParent.child2 = lastNoderemplacementID;
            }
            nodes[lastNodeRemplacement.parentIndex] = lastNodeRemplacementParent;
            nodes[formerLastNode.parentIndex] = formerLastNodeParent;
        }

        lastNodeRemplacementChild1 = nodes[nodes[lastNoderemplacementID].child1];
        lastNodeRemplacementChild2 = nodes[nodes[lastNoderemplacementID].child2];
        formerLastNodeChild1 = nodes[nodes[nodecount - 1].child1];
        formerLastNodeChild2 = nodes[nodes[nodecount - 1].child2];

        lastNodeRemplacementChild1.parentIndex = nodecount - 1;
        lastNodeRemplacementChild2.parentIndex = nodecount - 1;
        formerLastNodeChild1.parentIndex = lastNoderemplacementID;
        formerLastNodeChild2.parentIndex = lastNoderemplacementID;

        nodes[nodes[lastNoderemplacementID].child1] = lastNodeRemplacementChild1;
        nodes[nodes[lastNoderemplacementID].child2] = lastNodeRemplacementChild2;
        nodes[nodes[nodecount - 1].child1] = formerLastNodeChild1;
        nodes[nodes[nodecount - 1].child2] = formerLastNodeChild2;

        nodes[nodecount - 1] = lastNodeRemplacement;
        nodes[lastNoderemplacementID] = formerLastNode;


    }


    //go down the tree till leaf and test for each intersecting leaf at each step
    public void GatherIntersectingNodes(NativeList<CollisionPair> ColPair, int index)
    {
        if (nodeCount == 0)
            return;

        if (!nodes[index].isLeaf)
        {
            TryRegisterCollisionPair(ColPair,nodes[index].child1, nodes[index].child2);

            GatherIntersectingNodes(ColPair,nodes[index].child1);
            GatherIntersectingNodes(ColPair,nodes[index].child2);

        }

    }
    private bool IsOverlapping(int nodeA,int nodeB)
    {
        return (0 < PhysicsUtilities.Proximity(nodes[nodeA].box, nodes[nodeB].box));
    }
    //Decend into the tree whenever there is an intersection to see if the intersection lands and two leafs.
    //Then add the collision pair
    private void TryRegisterCollisionPair(NativeList<CollisionPair> ColPair, int nodeA, int nodeB)
    {
        if (IsOverlapping(nodeA, nodeB))
        {

            if (nodes[nodeA].isLeaf)
            {
                //both leaves, add pair
                if (nodes[nodeB].isLeaf)
                {
                    //register the pair here
                    ///add filter if need for physics resolution
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


