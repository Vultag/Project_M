using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using UnityEngine;

public struct BoxShapeData : IComponentData
{
    public Vector2 dimentions;

}
//public class BoxShapeAuthoring : MonoBehaviour
//{

//    public Vector2 dimentions;

//    class BoxShapeBaker : Baker<BoxShapeAuthoring>
//    {
//        public override void Bake(BoxShapeAuthoring authoring)
//        {

//            Entity entity = GetEntity(TransformUsageFlags.None);

//            AddComponent(entity, new BoxShapeData
//            {

//                dimentions = authoring.dimentions,

//            });
//        }
//    }
//}