using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using Unity.Entities.UniversalDelegates;
using Unity.Transforms;
using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;
using static UnityEngine.EventSystems.EventTrigger;

[UpdateInGroup(typeof(GameSimulationSystemGroup), OrderLast = true)]
[UpdateBefore(typeof(PhyResolutionSystem))]
public partial struct TreeInsersionSystem : ISystem, ISystemStartStop
{

    public static EntityQuery CirclesShapesQuery;
    public static DynamicAABBTree<CircleShapeData> AABBtree;
    /*Arbitratry fat on AABB to reduce de Insert/remove each frame*/
    public static float AABBfat;


    void OnCreate(ref SystemState state)
    {
        /*Arbitratry fat on AABB to reduce de Insert/remove each frame*/
        AABBfat = 0.2f;
        AABBtree = new DynamicAABBTree<CircleShapeData>();
        /*arbitrary alocator lenght OPTI!*/
        AABBtree.nodes = new NativeArray<AABBTreeNode>(500, Allocator.Persistent);

    }

    void ISystemStartStop.OnStartRunning(ref SystemState state)
    {
    }
    void ISystemStartStop.OnStopRunning(ref SystemState state)
    {
    }

    void OnUpdate(ref SystemState state)
    {
        var esECB = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>();
        EntityCommandBuffer ecb = esECB.CreateCommandBuffer(state.WorldUnmanaged);

        #region SAFETY
        if (AABBtree.rootIndex != AABBtree.nodes[AABBtree.rootIndex].parentIndex)
        {
            Debug.LogError("root is not parent of itself");
        }
        if(AABBtree.nodeCount>1)
        {
            if (AABBtree.nodes[AABBtree.nodeCount - 2].parentIndex != AABBtree.nodeCount - 1)
            {
                Debug.LogError("last node is not child of last parent ");
                Debug.LogError(AABBtree.nodes[AABBtree.nodeCount - 2].parentIndex);
                Debug.Break();
            }
        }
        #endregion


        #region insert at start AABB body
        //used to compare nodes for insert
        NativeQueue<int> comparequeue = new NativeQueue<int>(Allocator.Temp);
        foreach (var (circlesShapes, entity) in SystemAPI.Query<RefRO<CircleShapeData>>().WithAll<PhyBodyData>().WithEntityAccess().WithAll<TreeInsersionTag>())
        {
            //Debug.LogError("node");

            AABBtree.InsertLeaf(entity,circlesShapes.ValueRO.collisionLayer,
           new AABB
           {
               UpperBound = new Vector2(circlesShapes.ValueRO.Position.x + circlesShapes.ValueRO.radius + AABBfat, circlesShapes.ValueRO.Position.y + circlesShapes.ValueRO.radius + AABBfat),
               LowerBound = new Vector2(circlesShapes.ValueRO.Position.x - circlesShapes.ValueRO.radius - AABBfat, circlesShapes.ValueRO.Position.y - circlesShapes.ValueRO.radius - AABBfat)
           },
           comparequeue
           );

            ecb.RemoveComponent<TreeInsersionTag>(entity);

        }

        comparequeue.Clear();
        #endregion

        ///put in job ?
        #region update the AABBtree

        for (int i = 0; i < AABBtree.nodeCount; i++)
        {
            if (AABBtree.nodes[i].isLeaf)
            {
                var circleshape = SystemAPI.GetComponent<CircleShapeData>(AABBtree.nodes[i].bodyIndex);
                var tight_AABB = new AABB
                {
                    UpperBound = new Vector2(circleshape.Position.x + circleshape.radius, circleshape.Position.y + circleshape.radius),
                    LowerBound = new Vector2(circleshape.Position.x - circleshape.radius, circleshape.Position.y - circleshape.radius)
                };

                /*if the node's box exited it's "fat" margin*/
                if (AABBtree.Area(AABBtree.Union(AABBtree.nodes[i].box, tight_AABB)) > AABBtree.Area(AABBtree.nodes[i].box))
                {

                    Entity bodyIndex = AABBtree.nodes[i].bodyIndex;
                    PhysicsUtilities.CollisionLayer colLayer = AABBtree.nodes[i].LayerMask;


                    AABBtree.RemoveLeaf(i);


                    //AABBtree.RefitHierarchy(AABBtree.nodes[i].parentIndex);
                    AABBtree.InsertLeaf(bodyIndex, colLayer,
                     new AABB
                     {
                         UpperBound = tight_AABB.UpperBound + new Vector2(AABBfat, AABBfat),
                         LowerBound = tight_AABB.LowerBound - new Vector2(AABBfat, AABBfat)
                     },
                     comparequeue
                     );

                    comparequeue.Clear();

                }

            }
            else
            {

                //Debug.Log(i + " is NOT leaf");
            }

        }
        comparequeue.Dispose();

        #endregion


        /*debug purpuse*/
        for (int i = 0; i < AABBtree.nodeCount; i++)
        {
            if (AABBtree.nodes[i].isLeaf)
                DrawQuad(AABBtree.nodes[i].box.LowerBound, AABBtree.nodes[i].box.UpperBound, Color.red);
            else
            {
                DrawQuad(AABBtree.nodes[i].box.LowerBound, AABBtree.nodes[i].box.UpperBound, Color.green);
            }
        }




    }
    public static void DrawQuad(Vector2 lowerbounds, Vector2 upperbounds, Color color)
    {
        Debug.DrawLine(new Vector3(lowerbounds.x, lowerbounds.y, 0), new Vector3(lowerbounds.x, upperbounds.y, 0), color);
        Debug.DrawLine(new Vector3(upperbounds.x, upperbounds.y, 0), new Vector3(lowerbounds.x, upperbounds.y, 0), color);
        Debug.DrawLine(new Vector3(upperbounds.x, lowerbounds.y, 0), new Vector3(upperbounds.x, upperbounds.y, 0), color);
        Debug.DrawLine(new Vector3(lowerbounds.x, lowerbounds.y, 0), new Vector3(upperbounds.x, lowerbounds.y, 0), color);
    }

}
