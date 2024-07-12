using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using Unity.Entities.UniversalDelegates;
using Unity.Transforms;
using Unity.VisualScripting;
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

    //private EndSimulationEntityCommandBufferSystem.Singleton esECB;

    void OnCreate(ref SystemState state)
    {
        //state.RequireAnyForUpdate(state.GetEntityQuery(typeof(TreeInsersionTag)));
        //CirclesShapesQuery = state.GetEntityQuery(typeof(PhyBodyData), typeof(CircleShapeData));
        /*Arbitratry fat on AABB to reduce de Insert/remove each frame*/
        AABBfat = 0.2f;
        AABBtree = new DynamicAABBTree<CircleShapeData>();
        /*arbitrary alocator lenght*/
        AABBtree.nodes = new NativeArray<AABBTreeNode>(500, Allocator.Persistent);
        //AABBtree.nodes = new NativeArray<AABBTreeNode>((CirclesShapesQuery.CalculateEntityCount() * 2) - 1, Allocator.Persistent);

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

        //not needed ?
        //NativeArray<CircleShapeData> CirclesShapes = CirclesShapesQuery.ToComponentDataArray<CircleShapeData>(Allocator.Temp);

        var esECB = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>();
        EntityCommandBuffer ecb = esECB.CreateCommandBuffer(state.WorldUnmanaged);

        //temp check
        if(AABBtree.rootIndex != AABBtree.nodes[AABBtree.rootIndex].parentIndex)
        {
            Debug.LogError("root is not parent of itself");
        }


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


        


            //Debug.Break();
        }

        comparequeue.Clear();
        //Debug.Log(AABBtree.nodeCount);



        #endregion

        ///put in job ?
        #region update the AABBtree

        for (int i = 0; i < AABBtree.nodeCount; i++)
        {
            if (AABBtree.nodes[i].isLeaf)
            {

                //Debug.LogError(" is leaf");

                var circleshape = SystemAPI.GetComponent<CircleShapeData>(AABBtree.nodes[i].bodyIndex);
                var tight_AABB = new AABB
                {
                    UpperBound = new Vector2(circleshape.Position.x + circleshape.radius, circleshape.Position.y + circleshape.radius),
                    LowerBound = new Vector2(circleshape.Position.x - circleshape.radius, circleshape.Position.y - circleshape.radius)
                };
                //Debug.Log("before check");

                //Debug.Break();

                /*if the node's box exited it's "fat" margin*/
                if (AABBtree.Area(AABBtree.Union(AABBtree.nodes[i].box, tight_AABB)) > AABBtree.Area(AABBtree.nodes[i].box))
                {
                    //Debug.LogError("breaks here at 7th node");
                    //Debug.Break();

                    Entity bodyIndex = AABBtree.nodes[i].bodyIndex;
                    PhysicsUtilities.CollisionLayer colLayer = AABBtree.nodes[i].LayerMask;


                    //Debug.LogError("root = " + AABBtree.rootIndex);


                    //for (int y = 0; y < AABBtree.nodeCount; y++)
                    //{
                    //    Debug.LogError("id = " + y);
                    //    Debug.LogError("parent = " + AABBtree.nodes[y].parentIndex);
                    //    //Debug.Log("boxx = " + AABBtree.nodes[y].box.LowerBound);
                    //}


                    AABBtree.RemoveLeaf(i);

                    //Debug.LogWarning("removed = " + i);

                    //Debug.Log("root = " + AABBtree.rootIndex);

                    //for (int y = 0; y < AABBtree.nodeCount; y++)
                    //{
                    //    Debug.Log("id = " + y);
                    //    Debug.Log("parent = " + AABBtree.nodes[y].parentIndex);
                    //    //Debug.Log("boxx = " + AABBtree.nodes[y].box.LowerBound);
                    //}


                    //AABBtree.RefitHierarchy(AABBtree.nodes[i].parentIndex);
                    AABBtree.InsertLeaf(bodyIndex, colLayer,
                     new AABB
                     {
                         UpperBound = tight_AABB.UpperBound + new Vector2(AABBfat, AABBfat),
                         LowerBound = tight_AABB.LowerBound - new Vector2(AABBfat, AABBfat)
                     },
                     comparequeue
                     );

                    //Debug.LogError("root = " + AABBtree.rootIndex);

                    //for (int y = 0; y < AABBtree.nodeCount; y++)
                    //{
                    //    Debug.LogError("id = " + y);
                    //    Debug.LogError("parent = " + AABBtree.nodes[y].parentIndex);
                    //    //Debug.Log("boxx = " + AABBtree.nodes[y].box.LowerBound);
                    //}


                    comparequeue.Clear();
                    //InvalidNodes.Enqueue(new InvalidatedNode { box = tight_AABB, bodyIndex = AABBtree.nodes[i].bodyIndex, treeIndex = i });
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
