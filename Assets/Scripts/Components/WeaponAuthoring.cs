using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using UnityEngine;

/// <summary>
///  REMOVE ENTIRELY AND USE SYNTHDATA INSTEAD ?
/// </summary>
public struct WeaponData : IComponentData
{


    public float tempShootRange;


    //Shooting on beat only for now...
    //public float tempShootSpeed;
    //public float tempShootCooldown;

}

public enum WeaponType // *
{
    /// <summary>
    ///  Rifle, Shotgun, Raygun
    /// </summary>
    todo,
    todoo,
}

public class WeaponAuthoring : MonoBehaviour
{


    public float tempShootSpeed;

    public float tempShootRange;

    class WeaponBaker : Baker<WeaponAuthoring>
    {
        public override void Bake(WeaponAuthoring authoring)
        {

            Entity entity = GetEntity(TransformUsageFlags.None);

            AddComponent(entity, new WeaponData
            {

                tempShootRange = authoring.tempShootRange,
                //tempShootSpeed = authoring.tempShootSpeed,

            });
        }
    }
}