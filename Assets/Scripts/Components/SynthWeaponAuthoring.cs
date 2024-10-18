using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

public struct SynthWeaponData : IComponentData
{

    /// <summary>
    /// color is dynamic. has to be set per key
    /// </summary>
    public float3 color;


}

public enum SynthWeaponType // *
{
    /// <summary>
    ///  Rifle, Shotgun, Raygun
    /// </summary>
    todo,
    todoo,
}

//public class SynthWeaponAuthoring : MonoBehaviour
//{


//    class SynthWeaponBaker : Baker<SynthWeaponAuthoring>
//    {
//        public override void Bake(SynthWeaponAuthoring authoring)
//        {

//            Entity entity = GetEntity(TransformUsageFlags.None);

//            AddComponent(entity, new SynthWeaponData
//            {

//                color = authoring.,

//            });
//        }
//    }
//}