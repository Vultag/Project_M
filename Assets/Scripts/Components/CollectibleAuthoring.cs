using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using UnityEngine;

public struct WeaponCollectibleData : IComponentData
{
    public WeaponClass weaponClass;
    public WeaponType weaponType;
}

public enum CollectibleType:byte
{
    WeaponItem
}

/// <summary>
/// DO custom add logic depending on the collectible type
/// </summary>

public class CollectibleAuthoring : MonoBehaviour
{
    /// <summary>
    /// to do
    /// </summary>
    public CollectibleType collectibleType;

    public WeaponClass weaponClass;
    public WeaponType weaponType;

    class CollectibleBaker : Baker<CollectibleAuthoring>
    {
        public override void Bake(CollectibleAuthoring authoring)
        {

            Entity entity = GetEntity(TransformUsageFlags.None);

            AddComponent(entity,new WeaponCollectibleData
            {
                weaponClass = authoring.weaponClass,
                weaponType = authoring.weaponType
            });

        }
    }
}