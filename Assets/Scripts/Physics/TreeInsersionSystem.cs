using System.Drawing;
using System.Linq;
using Unity.Collections;
using Unity.Entities;
using Unity.Entities.UniversalDelegates;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UIElements;
using static UnityEngine.EventSystems.EventTrigger;
using Color = UnityEngine.Color;


public partial struct TreeInsersionSystem : ISystem//, ISystemStartStop
{

    public static EntityQuery CirclesShapesQuery;
    public static DynamicAABBTree DynamicBodiesAABBtree;
    public static DynamicAABBTree StaticBodiesAABBtree;
    /*Arbitratry fat on AABB to reduce de Insert/remove each frame*/
    public static float AABBfat;


    public void OnCreate(ref SystemState state)
    {
        /*Arbitratry fat on AABB to reduce de Insert/remove each frame*/
        AABBfat = 0.2f;
        /*arbitrary alocator lenght OPTI!*/
        DynamicBodiesAABBtree = new DynamicAABBTree(128);
        StaticBodiesAABBtree = new DynamicAABBTree(48);
        //AABBtree.nodes = new NativeArray<AABBTreeNode>(500, Allocator.Persistent);

    }

    public void OnDestroy(ref SystemState state)
    {
        DynamicBodiesAABBtree.DisposeAABBTree();
        StaticBodiesAABBtree.DisposeAABBTree();
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
        //if (StaticBodiesAABBtree.rootIndex != StaticBodiesAABBtree.nodes[StaticBodiesAABBtree.rootIndex].parentIndex)
        //{
        //    Debug.LogError("root is not parent of itself");
        //}
        //if (StaticBodiesAABBtree.nodes.Length > 1)
        //{
        //    if (StaticBodiesAABBtree.nodes[StaticBodiesAABBtree.nodes.Length - 2].parentIndex != StaticBodiesAABBtree.nodes.Length - 1)
        //    {
        //        Debug.LogError("last node is not child of last parent ");
        //        Debug.LogError(StaticBodiesAABBtree.nodes[StaticBodiesAABBtree.nodes.Length - 2].parentIndex);
        //        Debug.Break();
        //    }
        //}
        #endregion

        var CirclesLookUp = SystemAPI.GetComponentLookup<CircleShapeData>(true);
        var BoxLookUp = SystemAPI.GetComponentLookup<BoxShapeData>(true);

        #region insert at start AABB body
        foreach (var (shapes, insertionData, entity) in SystemAPI.Query<RefRO<ShapeData>, RefRO<TreeInsersionData>>().WithEntityAccess())
        {

            switch (shapes.ValueRO.shapeType)
            {
                case ShapeType.Circle:

                    CircleShapeData circle = CirclesLookUp.GetRefRO(entity).ValueRO;
                    /// insert in static tree
                    if (insertionData.ValueRO.IsStaticBody)
                    {
                        StaticBodiesAABBtree.AddEntity(entity,
                        new AABB
                        {
                            UpperBound = new Vector2(shapes.ValueRO.Position.x + circle.radius, shapes.ValueRO.Position.y + circle.radius),
                            LowerBound = new Vector2(shapes.ValueRO.Position.x - circle.radius, shapes.ValueRO.Position.y - circle.radius)
                        },
                        shapes.ValueRO.collisionLayer
                        );
                    }
                    /// insert in dynamic tree
                    else
                    {
                        DynamicBodiesAABBtree.AddEntity(entity,
                        new AABB
                        {
                            UpperBound = new Vector2(shapes.ValueRO.Position.x + circle.radius + AABBfat, shapes.ValueRO.Position.y + circle.radius + AABBfat),
                            LowerBound = new Vector2(shapes.ValueRO.Position.x - circle.radius - AABBfat, shapes.ValueRO.Position.y - circle.radius - AABBfat)
                        },
                        shapes.ValueRO.collisionLayer
                        );
                    }
                    break;
                case ShapeType.Box:

                    BoxShapeData box = BoxLookUp.GetRefRO(entity).ValueRO;
                    AABB boxAABB = PhysicsUtilities.AABBfromShape(shapes.ValueRO.Position, shapes.ValueRO.Rotation, box);

                    //DrawQuad(boxAABB.LowerBound, boxAABB.UpperBound, Color.red);

                    /// insert in static tree
                    if (insertionData.ValueRO.IsStaticBody)
                    {
                        StaticBodiesAABBtree.AddEntity(entity,
                        boxAABB,
                        shapes.ValueRO.collisionLayer
                        );
                    }
                    /// insert in dynamic tree
                    else
                    {
                        DynamicBodiesAABBtree.AddEntity(entity,
                        boxAABB,
                        shapes.ValueRO.collisionLayer
                        );
                    }
                    break;
            }

          

            ecb.RemoveComponent<TreeInsersionData>(entity);

        }

        #endregion

        ///put in job ?
        #region update the AABBtree

        for (int i = 0; i < DynamicBodiesAABBtree.nodes.Length; i++)
        {
            if (DynamicBodiesAABBtree.nodes[i].isLeaf && DynamicBodiesAABBtree.nodes[i].entity == Entity.Null)
                Debug.LogError("lkpjkfopgijkhio");
        }


        foreach (int leafIndex in DynamicBodiesAABBtree.leafIndices)
        {
            AABBTreeNode newNode = DynamicBodiesAABBtree.nodes[leafIndex];

            var shape = SystemAPI.GetComponent<ShapeData>(newNode.entity);
            //Debug.LogError(newNode.box.UpperBound);
            //Debug.LogError(newNode.box.LowerBound);

            AABB tight_AABB = new AABB();

            //Debug.Log(" ShapeData:rotation Quaternion->float ? et reprend box case");

            switch (shape.shapeType)
            {
                case ShapeType.Circle:
                    tight_AABB = PhysicsUtilities.AABBfromShape(shape.Position, CirclesLookUp.GetRefRO(newNode.entity).ValueRO);
                    break;
                case ShapeType.Box:
                    tight_AABB = PhysicsUtilities.AABBfromShape(shape.Position, shape.Rotation, BoxLookUp.GetRefRO(newNode.entity).ValueRO);
                    break;
            }


            //Debug.LogError(tight_AABB.UpperBound);
            //Debug.LogError(tight_AABB.LowerBound);

            /*if the node's box exited it's "fat" margin*/
            if (DynamicBodiesAABBtree.Area(DynamicBodiesAABBtree.Union(DynamicBodiesAABBtree.nodes[leafIndex].box, tight_AABB)) > DynamicBodiesAABBtree.Area(DynamicBodiesAABBtree.nodes[leafIndex].box))
            {

                //PhysicsUtilities.CollisionLayer colLayer = AABBtree.nodes[i].layerMask;

                newNode.box = new AABB
                {
                    UpperBound = tight_AABB.UpperBound + new Vector2(AABBfat, AABBfat),
                    LowerBound = tight_AABB.LowerBound - new Vector2(AABBfat, AABBfat)
                };
                DynamicBodiesAABBtree.nodes[leafIndex] = newNode;

                //Debug.Log(AABBtree.nodes[leafIndex].entity);
                DynamicBodiesAABBtree.Refit(DynamicBodiesAABBtree.nodes[leafIndex].parentIndex);

            }
        }

        /// DO TREE BALANCING

        #endregion


        /*debug purpuse*/
        //for (int i = 0; i < DynamicBodiesAABBtree.nodes.Length; i++)
        //{
        //    if (DynamicBodiesAABBtree.nodes[i].isLeaf == true)
        //    {
        //        if (DynamicBodiesAABBtree.leafIndices.Contains(i))
        //        { DrawQuad(DynamicBodiesAABBtree.nodes[i].box.LowerBound, DynamicBodiesAABBtree.nodes[i].box.UpperBound, Color.yellow); }
        //        else
        //        { DrawQuad(DynamicBodiesAABBtree.nodes[i].box.LowerBound, DynamicBodiesAABBtree.nodes[i].box.UpperBound, Color.red); }
        //    }
        //    else
        //    { DrawQuad(DynamicBodiesAABBtree.nodes[i].box.LowerBound, DynamicBodiesAABBtree.nodes[i].box.UpperBound, Color.green); }
        //}
        //for (int i = 0; i < StaticBodiesAABBtree.nodes.Length; i++)
        //{
        //    if (StaticBodiesAABBtree.nodes[i].isLeaf == true)
        //    {
        //        if (StaticBodiesAABBtree.leafIndices.Contains(i))
        //        { DrawQuad(StaticBodiesAABBtree.nodes[i].box.LowerBound, StaticBodiesAABBtree.nodes[i].box.UpperBound, Color.yellow); }
        //        else
        //        { DrawQuad(StaticBodiesAABBtree.nodes[i].box.LowerBound, StaticBodiesAABBtree.nodes[i].box.UpperBound, Color.red); }
        //    }
        //    else
        //    { DrawQuad(StaticBodiesAABBtree.nodes[i].box.LowerBound, StaticBodiesAABBtree.nodes[i].box.UpperBound, Color.green); }
        //}
        /// temp before box texture
        foreach (var (boxShapes,shape,body, entity) in SystemAPI.Query<RefRO<BoxShapeData>, RefRO<ShapeData>, RefRO<PhyBodyData>>().WithEntityAccess())
        {

            float halfWidth = boxShapes.ValueRO.dimentions.x * 0.5f;
            float halfHeight = boxShapes.ValueRO.dimentions.y * 0.5f;

            // Define local corners
            Vector2[] localCorners = new Vector2[4]
            {
            new Vector2(-halfWidth, -halfHeight),
            new Vector2(-halfWidth,  halfHeight),
            new Vector2( halfWidth,  halfHeight),
            new Vector2( halfWidth, -halfHeight)
            };

            // Build rotation matrix around Z
            Quaternion rotation = Quaternion.Euler(0f, 0f, shape.ValueRO.Rotation);

            // Transform to world space
            Vector3[] worldCorners = new Vector3[4];
            for (int i = 0; i < 4; i++)
            {
                Vector2 rotated = rotation * localCorners[i];
                worldCorners[i] = shape.ValueRO.Position + new Vector2(rotated.x, rotated.y);
            }

            // Draw lines between corners
            for (int i = 0; i < 4; i++)
            {
                Vector3 start = worldCorners[i];
                Vector3 end = worldCorners[(i + 1) % 4];
                Color color = body.ValueRO.Mass == 0? Color.blue : Color.green;
                Debug.DrawLine(start, end, color);
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
