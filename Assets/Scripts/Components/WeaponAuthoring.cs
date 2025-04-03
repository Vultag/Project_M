using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Rendering;
using Unity.Transforms;
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
    Null,
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

            //var equipmentBuffer = AddBuffer<Child>(entity);
            //equipmentBuffer.Add(new Child { Value = GetEntity(authoring.transform.GetChild(0), TransformUsageFlags.None) });
            //equipmentBuffer.Add(new Child { Value = GetEntity(authoring.transform.GetChild(1), TransformUsageFlags.None) });

            /// hide Weapon and DMachine on main slot at start
            var playerWeaponSpriteEntity = GetEntity(authoring.transform.GetChild(0), TransformUsageFlags.None);
            var playerDMachineSpriteEntity = GetEntity(authoring.transform.GetChild(1), TransformUsageFlags.None);
            //MaterialMeshInfo newWeaponMaterialMeshInfo = EntityManager.GetComponentData<MaterialMeshInfo>(playerWeaponSpriteEntity);
            //newWeaponMaterialMeshInfo.Mesh = 0;
            //ECB.SetComponent<MaterialMeshInfo>(playerWeaponSpriteEntity, newWeaponMaterialMeshInfo);
            //MaterialMeshInfo newDMachineMaterialMeshInfo = EntityManager.GetComponentData<MaterialMeshInfo>(playerDMachineSpriteEntity);
            //newDMachineMaterialMeshInfo.Mesh = 0;
            //ECB.SetComponent<MaterialMeshInfo>(playerDMachineSpriteEntity, newDMachineMaterialMeshInfo);

            //AddComponent<MaterialMeshInfo>(playerWeaponSpriteEntity, new MaterialMeshInfo
            //{
            //    Mesh = 0,
            //    Material = -1,
            //});
            //AddComponent<MaterialMeshInfo>(playerDMachineSpriteEntity, new MaterialMeshInfo
            //{
            //    Mesh = 0,
            //    Material = -2,
            //});
        }
    }
}