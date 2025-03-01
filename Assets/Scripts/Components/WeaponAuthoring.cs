using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

public struct WeaponData : IComponentData
{

    public int WeaponIdx;
    public WeaponType weaponType;


    //public float2 OffsetFromPlayer;
}

public enum WeaponType // *
{
    /// <summary>
    ///  Rifle, Shotgun, Raygun
    /// </summary>
    Raygun,
    Canon,
}

public class WeaponAuthoring : MonoBehaviour
{
    //Vector2 OffsetFromPlayer;

    class WeaponBaker : Baker<WeaponAuthoring>
    {
        public override void Bake(WeaponAuthoring authoring)
        {

            Entity entity = GetEntity(TransformUsageFlags.None);

            //authoring.transform.parent

            AddComponent(entity, new WeaponData
            {
                /// Baking weapon will be the main one
                WeaponIdx = 0,
                weaponType = WeaponType.Raygun,
                //OffsetFromPlayer = authoring.OffsetFromPlayer,

            });
        }
    }
}