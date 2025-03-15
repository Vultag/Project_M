using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using static PhysicsUtilities;


public struct AABB
{
    public Vector2 UpperBound;
    public Vector2 LowerBound;
}
public struct AABBTreeNode
{

    public AABB box;
    //public long EntityKey; /// Packed information pointing to the Entity (int:entityIdex and int:version)
    public Entity entity;
    public int parentIndex;
    ///for coll filtering!:
    public CollisionLayer layerMask;

    public int LeftChild;
    public int RightChild;

    /// <summary>
    /// 0=disabled;1=leaf;2+n=internal;
    /// NOT WORTH IT ? REVERT TO BOOL ?
    /// </summary>
    //public ushort nodeType;
    public bool isLeaf;

}

public unsafe struct DynamicAABBTree
{

    public NativeList<AABBTreeNode> nodes;
    public NativeList<int> leafIndices; // Tracks only leaf node indices
    private NativeList<int> freeNodes; // Recycle removed nodes
    private NativeParallelHashMap<Entity, int> entityToNode; // Maps ObjectID to BVH node
    public int rootIndex;

    public DynamicAABBTree(int initialCapacity)
    {
        nodes = new NativeList<AABBTreeNode>(initialCapacity, Allocator.Persistent);
        leafIndices = new NativeList<int>(initialCapacity, Allocator.Persistent);
        freeNodes = new NativeList<int>(Allocator.Persistent);
        //entityKeyToNode = new NativeParallelHashMap<long, int>(initialCapacity, Allocator.Persistent);
        entityToNode = new NativeParallelHashMap<Entity, int>(initialCapacity, Allocator.Persistent);
        rootIndex = -1;
    }

    public int AddEntity(Entity entity, in AABB bounds, CollisionLayer collisionLayer)
    {
        int nodeID;
        if (freeNodes.Length > 0)
        {
            // Reuse a node from the pool if available
            //Debug.Log("reuse");
            nodeID = freeNodes[freeNodes.Length - 1];  // Get the last element
            freeNodes.RemoveAt(freeNodes.Length - 1);  // Remove it from the pool
        }
        else
        {
            nodeID = nodes.Length;
            nodes.Add(new AABBTreeNode());
        }
        //Debug.LogError(nodeID);
        //Debug.LogError(nodes.Length);

        nodes[nodeID] = new AABBTreeNode
        {
            box = bounds,
            entity = entity,
            parentIndex = -1,
            LeftChild = -1,
            RightChild = -1,
            layerMask = collisionLayer,
            isLeaf = true
        };

        //entityKeyToNode[entityKey] = nodeID;
        entityToNode[entity] = nodeID;
        //entityToNode.Add(entity, nodeID);

        if (rootIndex == -1)
        {
            rootIndex = nodeID;
            leafIndices.Add(nodeID);
        }
        else
        {
            InsertLeaf(nodeID, rootIndex);
        }

        return nodeID;
    }
    private void InsertLeaf(int nodeID, int currentEvaluatedNode)
    {

        if (nodes[currentEvaluatedNode].isLeaf == true)
        {
            AABBTreeNode newEvaluatedNode = nodes[currentEvaluatedNode];
            AABBTreeNode newNode = nodes[nodeID];

            int newParent = nodes.Length;
            nodes.Add(new AABBTreeNode
            {
                box = Union(newEvaluatedNode.box, newNode.box),
                LeftChild = currentEvaluatedNode,
                RightChild = nodeID,
                parentIndex = newEvaluatedNode.parentIndex,
                //nodeType = nodes[currentEvaluatedNode].parentIndex !=-1 ?(ushort)(nodes[nodes[currentEvaluatedNode].parentIndex].nodeType+1): (ushort)2
                isLeaf = false
            });
            //Debug.Log(nodes[newParent].nodeType);

            newEvaluatedNode.parentIndex = newParent;
            newNode.parentIndex = newParent;
            nodes[currentEvaluatedNode] = newEvaluatedNode;
            nodes[nodeID] = newNode;

            if (nodes[newParent].parentIndex != -1)
            {
                //Debug.Log("test");
                AABBTreeNode grandparent = nodes[nodes[newParent].parentIndex];
                if (grandparent.LeftChild == currentEvaluatedNode) grandparent.LeftChild = newParent;
                else grandparent.RightChild = newParent;
                nodes[nodes[newParent].parentIndex] = grandparent; // Update the parent node
                Refit(newParent);
            }
            else { rootIndex = newParent; }

            leafIndices.Add(nodeID);
        }
        else
        {

            // Handle Empty Child Slots First
            if (nodes[currentEvaluatedNode].LeftChild == -1)
            {
                //Debug.Log("HERE 0");
                AABBTreeNode newEvaluatedNode = nodes[currentEvaluatedNode];
                AABBTreeNode newNode = nodes[nodeID];
                newEvaluatedNode.LeftChild = nodeID;
                newNode.parentIndex = currentEvaluatedNode;
                nodes[currentEvaluatedNode] = newEvaluatedNode;
                nodes[nodeID] = newNode;
                leafIndices.Add(nodeID);
                return;
            }

            if (nodes[currentEvaluatedNode].RightChild == -1)
            {
                //Debug.Log("HERE 1");
                AABBTreeNode newEvaluatedNode = nodes[currentEvaluatedNode];
                AABBTreeNode newNode = nodes[nodeID];
                newEvaluatedNode.RightChild = nodeID;
                newNode.parentIndex = currentEvaluatedNode;
                nodes[currentEvaluatedNode] = newEvaluatedNode;
                nodes[nodeID] = newNode;
                leafIndices.Add(nodeID);
                return;
            }

            /// use nodeType's index for depth SAH here ?
            float costLeft = Area(Union(nodes[nodes[currentEvaluatedNode].LeftChild].box, nodes[nodeID].box));
            float costRight = Area(Union(nodes[nodes[currentEvaluatedNode].RightChild].box, nodes[nodeID].box));

            if (costLeft < costRight)
                InsertLeaf(nodeID, nodes[currentEvaluatedNode].LeftChild);
            else
                InsertLeaf(nodeID, nodes[currentEvaluatedNode].RightChild);

            //AABBTreeNode newEvaluatedNode = nodes[currentEvaluatedNode];
            ///// redondant ? -> Refit is called anyway at the end of the insertion
            //newEvaluatedNode.box = Union(nodes[ nodes[currentEvaluatedNode].LeftChild].box, nodes[nodes[currentEvaluatedNode].RightChild].box);
            //nodes[currentEvaluatedNode] = newEvaluatedNode;
        }
    }
    /// <summary>
    /// Mark the entity as inactive and reference it in a list of inactives nodes for recycling or cleanup
    /// </summary>
    /// <param name="entityKey"></param>
    public void DisableEntity(Entity entity)
    {
        //if (!entityKeyToNode.TryGetValue(entityKey, out int nodeID)) Debug.LogError("entity key not found");
        if (!entityToNode.TryGetValue(entity, out int nodeID)) Debug.LogError("entity not found");

        int parent = nodes[nodeID].parentIndex;
        int indexToRefit = parent;
        if (parent == -1)
        {
            Debug.Log("test");
            rootIndex = -1;
        }
        else
        {

            /// Propagate disable to parent if both childs are disabled
            int newParentIdx = UpdateAncestor(parent, nodeID);

            indexToRefit = newParentIdx;
        }
        //AABBTreeNode newNode = nodes[nodeID];
        //newNode.isLeaf = 0;
        //nodes[nodeID] = newNode;
        freeNodes.Add(nodeID);
        //entityKeyToNode.Remove(entityKey);
        entityToNode.Remove(entity);

        Refit(indexToRefit);


        int indexInIndices = leafIndices.IndexOf(nodeID);
        // Swap the node to be removed with the last element
        int lastIndex = leafIndices.Length - 1;
        if (indexInIndices != lastIndex)
        {
            leafIndices[indexInIndices] = leafIndices[lastIndex];  // Swap with last element
        }
        // Remove the last element (which is now the node we want to remove)
        leafIndices.RemoveAt(lastIndex);

    }

    private int UpdateAncestor(int currentParentIdx,int currentEvaluatedNode)
    {

        int otherChildIdx = -99;
        AABBTreeNode newParent = nodes[currentParentIdx];
        if (nodes[currentParentIdx].LeftChild == currentEvaluatedNode)
        {
            newParent.LeftChild = -1;  // mark as unused
            otherChildIdx = newParent.RightChild;
        }
        else
        {
            newParent.RightChild = -1;  // mark as unused
            otherChildIdx = newParent.LeftChild;
        }
        while (otherChildIdx == -1 && nodes[currentParentIdx].parentIndex !=-1)
        {
            //Debug.LogWarning("pop parent");
            //newParent.nodeType = 0; //disable
            //nodes[currentParentIdx] = newParent;
            currentEvaluatedNode = currentParentIdx;
            currentParentIdx = nodes[currentParentIdx].parentIndex;
            newParent = nodes[currentParentIdx];
            if (nodes[currentParentIdx].LeftChild == currentEvaluatedNode)
            {
                freeNodes.Add(newParent.LeftChild);
                newParent.LeftChild = -1;  // mark as unused
                otherChildIdx = newParent.RightChild;
            }
            else
            {
                freeNodes.Add(newParent.RightChild);
                newParent.RightChild = -1;  // mark as unused
                otherChildIdx = newParent.LeftChild;
            }
        }
        nodes[currentParentIdx] = newParent;

        ///// both childs are disabled. Disable the parent as well
        //if(otherChildState==-1)
        //{
        //    AABBTreeNode newGrandParent = nodes[nodes[parent].parentIndex];
        //    if (newGrandParent.LeftChild == parent) { newGrandParent.LeftChild = -1; }
        //    else { newGrandParent.RightChild = -1; }
        //    nodes[nodes[parent].parentIndex] = newGrandParent;

        //}
        //nodes[parent] = newParent;
        return currentParentIdx;
    }

    /// OPTI : var node
    public void Refit(int parentNodeID)
    {
        while (parentNodeID != -1)
        {
            //Debug.LogWarning(nodes.Length);
            //Debug.Log(nodes[parentNodeID].RightChild);
            //Debug.LogWarning(nodes[parentNodeID].LeftChild);
            //Debug.LogWarning(this.nodes[parentNodeID].parentIndex);

            AABBTreeNode newNode = nodes[parentNodeID];
            if (newNode.LeftChild == -1)
            { newNode.box = nodes[newNode.RightChild].box; }
            else if (newNode.RightChild == -1)
            { newNode.box = nodes[newNode.LeftChild].box; }
            else
            { newNode.box = Union(nodes[newNode.LeftChild].box, nodes[newNode.RightChild].box); }
            
            ///breaks early if the box growth has no incidence on ancestor
            if(newNode.box.Equals(nodes[parentNodeID].box))
            { break; }
            else
            {
                nodes[parentNodeID] = newNode;
                parentNodeID = nodes[parentNodeID].parentIndex;
            }
        }
    }


    public void GatherIntersectingNodes(ref NativeList<CollisionPair> ColPair, int index)
    {

        if (index == -1) return;
        /// Parent node
        if (nodes[index].isLeaf == false)
        {
            TryRegisterCollisionPair(ref ColPair, nodes[index].LeftChild, nodes[index].RightChild);
            GatherIntersectingNodes(ref ColPair, nodes[index].LeftChild);
            GatherIntersectingNodes(ref ColPair, nodes[index].RightChild);
        }

    }
    private bool IsOverlapping(int nodeA,int nodeB)
    {
        return (0 < PhysicsUtilities.Proximity(nodes[nodeA].box, nodes[nodeB].box));
    }
    //Decend into the tree whenever there is an intersection to see if the intersection lands and two leafs.
    //Then add the collision pair
    private void TryRegisterCollisionPair(ref NativeList<CollisionPair> ColPair, int nodeAidx, int nodeBidx)
    {
        if(nodeAidx == -1 || nodeBidx == -1) return;
        if (!IsOverlapping(nodeAidx, nodeBidx)) return;
        var nodeA = nodes[nodeAidx];
        var nodeB = nodes[nodeBidx];
        // Early exit for both leaves
        if (nodeA.isLeaf == true && nodeB.isLeaf == true && PhysicsUtilities.ShouldCollide(nodeA.layerMask, nodeB.layerMask))
        {
            ColPair.Add(new CollisionPair { EntityA = nodeA.entity, EntityB = nodeB.entity });
            return;
        }
        // Case where nodeA is leaf and nodeB is internal
        if (nodeA.isLeaf == true)
        {
            TryRegisterCollisionPair(ref ColPair, nodeAidx, nodeB.LeftChild);
            TryRegisterCollisionPair(ref ColPair, nodeAidx, nodeB.RightChild);
            return;
        }
        // Case where nodeB is leaf and nodeA is internal
        if (nodeB.isLeaf == true)
        {
            TryRegisterCollisionPair(ref ColPair, nodeBidx, nodeA.LeftChild);
            TryRegisterCollisionPair(ref ColPair, nodeBidx, nodeA.RightChild);
            return;
        }
        // Both are internal nodes, recurse into both children
        TryRegisterCollisionPair(ref ColPair, nodeA.LeftChild, nodeB.LeftChild);
        TryRegisterCollisionPair(ref ColPair, nodeA.LeftChild, nodeB.RightChild);
        TryRegisterCollisionPair(ref ColPair, nodeA.RightChild, nodeB.LeftChild);
        TryRegisterCollisionPair(ref ColPair, nodeA.RightChild, nodeB.RightChild);

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

    //public static Entity ReconstructEntity(long key)
    //{
    //    int index = (int)(key >> 32);
    //    int version = (int)(key & 0xFFFFFFFF);
    //    return new Entity { Index = index, Version = version };
    //}
    //public static long GetEntityKey(Entity entity)
    //{
    //    return ((long)entity.Index << 32) | (uint)entity.Version;
    //}

}


