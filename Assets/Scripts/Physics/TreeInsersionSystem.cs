using System.Linq;
using Unity.Collections;
using Unity.Entities;
using Unity.VisualScripting;
using UnityEngine;
using static UnityEngine.EventSystems.EventTrigger;


public partial struct TreeInsersionSystem : ISystem//, ISystemStartStop
{

    public static EntityQuery CirclesShapesQuery;
    public static DynamicAABBTree AABBtree;
    /*Arbitratry fat on AABB to reduce de Insert/remove each frame*/
    public static float AABBfat;


    public void OnCreate(ref SystemState state)
    {
        /*Arbitratry fat on AABB to reduce de Insert/remove each frame*/
        AABBfat = 0.2f;
        /*arbitrary alocator lenght OPTI!*/
        AABBtree = new DynamicAABBTree(128); ;
        //AABBtree.nodes = new NativeArray<AABBTreeNode>(500, Allocator.Persistent);

    }

    public void OnDestroy(ref SystemState state)
    {
        AABBtree.DisposeAABBTree();
    }

    //void ISystemStartStop.OnStartRunning(ref SystemState state)
    //{
    //}
    //void ISystemStartStop.OnStopRunning(ref SystemState state)
    //{
    //}

    public void OnUpdate(ref SystemState state)
    {
        var esECB = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>();
        EntityCommandBuffer ecb = esECB.CreateCommandBuffer(state.WorldUnmanaged);

        #region SAFETY
        //if (AABBtree.rootIndex != AABBtree.nodes[AABBtree.rootIndex].parentIndex)
        //{
        //    Debug.LogError("root is not parent of itself");
        //}
        //if(AABBtree.nodes.Length > 1)
        //{
        //    if (AABBtree.nodes[AABBtree.nodes.Length - 2].parentIndex != AABBtree.nodes.Length - 1)
        //    {
        //        Debug.LogError("last node is not child of last parent ");
        //        Debug.LogError(AABBtree.nodes[AABBtree.nodes.Length - 2].parentIndex);
        //        Debug.Break();
        //    }
        //}
        #endregion


        #region insert at start AABB body
        foreach (var (circlesShapes, entity) in SystemAPI.Query<RefRO<CircleShapeData>>().WithEntityAccess().WithAll<TreeInsersionTag>())
        {
            //Debug.LogError("node");

            AABBtree.AddEntity(entity,
           new AABB
           {
               UpperBound = new Vector2(circlesShapes.ValueRO.Position.x + circlesShapes.ValueRO.radius + AABBfat, circlesShapes.ValueRO.Position.y + circlesShapes.ValueRO.radius + AABBfat),
               LowerBound = new Vector2(circlesShapes.ValueRO.Position.x - circlesShapes.ValueRO.radius - AABBfat, circlesShapes.ValueRO.Position.y - circlesShapes.ValueRO.radius - AABBfat)
           },
           circlesShapes.ValueRO.collisionLayer
           );

            ecb.RemoveComponent<TreeInsersionTag>(entity);

        }

        #endregion

        ///put in job ?
        #region update the AABBtree

        for (int i = 0; i < AABBtree.nodes.Length; i++)
        {
            if (AABBtree.nodes[i].isLeaf && AABBtree.nodes[i].entity == Entity.Null)
                Debug.LogError("lkpjkfopgijkhio");
        }

        foreach (int leafIndex in AABBtree.leafIndices)
        {
            var circleshape = SystemAPI.GetComponent<CircleShapeData>(AABBtree.nodes[leafIndex].entity);
            var tight_AABB = new AABB
            {
                UpperBound = new Vector2(circleshape.Position.x + circleshape.radius, circleshape.Position.y + circleshape.radius),
                LowerBound = new Vector2(circleshape.Position.x - circleshape.radius, circleshape.Position.y - circleshape.radius)
            };

            /*if the node's box exited it's "fat" margin*/
            if (AABBtree.Area(AABBtree.Union(AABBtree.nodes[leafIndex].box, tight_AABB)) > AABBtree.Area(AABBtree.nodes[leafIndex].box))
            {

                //PhysicsUtilities.CollisionLayer colLayer = AABBtree.nodes[i].layerMask;

                AABBTreeNode newNode = AABBtree.nodes[leafIndex];
                newNode.box = new AABB
                {
                    UpperBound = tight_AABB.UpperBound + new Vector2(AABBfat, AABBfat),
                    LowerBound = tight_AABB.LowerBound - new Vector2(AABBfat, AABBfat)
                };
                AABBtree.nodes[leafIndex] = newNode;

                //Debug.Log(AABBtree.nodes[leafIndex].entity);
                AABBtree.Refit(AABBtree.nodes[leafIndex].parentIndex);

            }
        }

        /// DO TREE BALANCING

        #endregion


        /*debug purpuse*/
        //for (int i = 0; i < AABBtree.nodes.Length; i++)
        //{
        //    if (AABBtree.nodes[i].isLeaf == true)
        //    {
        //        if (AABBtree.leafIndices.Contains(i))
        //        { DrawQuad(AABBtree.nodes[i].box.LowerBound, AABBtree.nodes[i].box.UpperBound, Color.yellow); }
        //        else
        //        { DrawQuad(AABBtree.nodes[i].box.LowerBound, AABBtree.nodes[i].box.UpperBound, Color.red); }
        //    }
        //    else
        //    { DrawQuad(AABBtree.nodes[i].box.LowerBound, AABBtree.nodes[i].box.UpperBound, Color.green); }
        //}




    }
    public static void DrawQuad(Vector2 lowerbounds, Vector2 upperbounds, Color color)
    {
        Debug.DrawLine(new Vector3(lowerbounds.x, lowerbounds.y, 0), new Vector3(lowerbounds.x, upperbounds.y, 0), color);
        Debug.DrawLine(new Vector3(upperbounds.x, upperbounds.y, 0), new Vector3(lowerbounds.x, upperbounds.y, 0), color);
        Debug.DrawLine(new Vector3(upperbounds.x, lowerbounds.y, 0), new Vector3(upperbounds.x, upperbounds.y, 0), color);
        Debug.DrawLine(new Vector3(lowerbounds.x, lowerbounds.y, 0), new Vector3(upperbounds.x, lowerbounds.y, 0), color);
    }

}
