using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

public enum WeaponClass
{
    Ray,
    Projectile,
}
public enum WeaponType
{
    /// <summary>
    ///  Rifle, Shotgun, Raygun
    /// </summary>
    Raygun,
    Canon,
}
public struct WeaponData : IComponentData
{
    public int WeaponIdx;
    public WeaponClass weaponClass;
    public WeaponType weaponType;

    public Entity ProjectilePrefab;
}
public struct RayData:IComponentData
{
    /// to do
}
public struct ProjectileData :IComponentData
{
    public float Damage;
    public float Speed;
    public float LifeTime;
    public int penetrationCapacity;
}


public class WeaponAuthoring : MonoBehaviour
{
    //Vector2 OffsetFromPlayer;
    public GameObject ProjectilePrefab;

    class WeaponBaker : Baker<WeaponAuthoring>
    {
        public override void Bake(WeaponAuthoring authoring)
        {

            Entity entity = GetEntity(TransformUsageFlags.None);

            //authoring.transform.parent

            /// make default weapon ?
            
            AddComponent(entity, new WeaponData
            {
                /// Baking weapon will be the main one
                /// COMPONNENT IS SET ON PICKUP ANYWAY ?
                WeaponIdx = 0,
                weaponType = WeaponType.Raygun,
                ProjectilePrefab = GetEntity(authoring.ProjectilePrefab,TransformUsageFlags.None)
                //OffsetFromPlayer = authoring.OffsetFromPlayer,

            });
            //switch()
            //{
            //    case WeaponClass.Ray:
            //        AddComponent(entity, new RayData
            //        {
            //        });
            //        break;
            //    case WeaponClass.Projectile:
            //        AddComponent(entity, new ProjectileData
            //        {
            //            Damage = 6f,
            //            Speed = 1f,
            //            ProjectilePrefab = GetEntity(authoring.ProjectilePrefab, TransformUsageFlags.None),
            //        });
            //        break;

            //}
        }
    }
}