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

    /// <summary>
    /// TO REMOVE
    /// </summary>
    public Entity MainWeaponSpriteE;
    public Entity MainDMachineSpriteE;
}
public struct RayData:IComponentData
{
    /// to do

    //public float energyConsumptionRate;
    //public float energyRecoveryRate;
}
public struct WeaponAmmoData :IComponentData
{
    public float Damage;
    public float Speed;
    public float LifeTime;
    public int penetrationCapacity;

    //public float energyConsumptionRate;
    //public float energyRecoveryRate;
}
public struct EquipmentEnergyData : IComponentData
{
    public float energyConsumptionRate;
    public float energyRecoveryRate;
    public float energyLevel;
    public float maxEnergy;
}

public class WeaponAuthoring : MonoBehaviour
{
    //Vector2 OffsetFromPlayer;
    public GameObject ProjectilePrefab;

    public GameObject MainWeaponSpriteGB;
    public GameObject MainDMachineSpriteGB;

    class WeaponBaker : Baker<WeaponAuthoring>
    {
        public override void Bake(WeaponAuthoring authoring)
        {

            Entity entity = GetEntity(TransformUsageFlags.None);

            /// make default weapon ?
            
            AddComponent(entity, new WeaponData
            {
                /// both auxilliary weapon and main weapon -> decouple
                WeaponIdx = 0,
                weaponType = WeaponType.Raygun,
                MainWeaponSpriteE = GetEntity(authoring.MainWeaponSpriteGB,TransformUsageFlags.None),
                MainDMachineSpriteE = GetEntity(authoring.MainDMachineSpriteGB, TransformUsageFlags.None),
                //OffsetFromPlayer = authoring.OffsetFromPlayer,

            });

            ///// Authoring expected only on main weapon
            //AddComponent(entity, new MainWeaponTag());
        }
    }
}