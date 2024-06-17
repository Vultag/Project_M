using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using UnityEngine;

//move to the phy body file ?
public struct CircleShapeData : IComponentData
{
    public Vector2 Position;
    public float radius;
    public PhysicsUtilities.CollisionLayer collisionLayer;

}
// A SUPPR
//public class CircleShapeAuthoring : MonoBehaviour
//{

//    public float radius;

//    class CircleShapeBaker : Baker<CircleShapeAuthoring>
//    {
//        public override void Bake(CircleShapeAuthoring authoring)
//        {

//            Entity entity = GetEntity(TransformUsageFlags.None);

//            AddComponent(entity, new CircleShapeData
//            {

//                radius = authoring.radius,

//            });
//        }
//    }
//}