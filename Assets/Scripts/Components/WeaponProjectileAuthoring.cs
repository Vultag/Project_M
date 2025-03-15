using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using UnityEngine;

public struct WeaponProjectileData : IComponentData
{
    public float Damage;

}

/// Always add the component manually to tweak data

//public class WeaponProjectileAuthoring : MonoBehaviour
//{
//    //Vector2 OffsetFromPlayer;

//    class WeaponProjectileBaker : Baker<WeaponProjectileAuthoring>
//    {
//        public override void Bake(WeaponProjectileAuthoring authoring)
//        {

//            Entity entity = GetEntity(TransformUsageFlags.None);

//            //authoring.transform.parent

//            AddComponent(entity, new WeaponProjectileData
//            {


//            });
//        }
//    }
//}