using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using UnityEngine;

public struct TurretData : IComponentData
{
    public Entity weaponPrefabEntity;

}

public class TurretAuthoring : MonoBehaviour
{
    public GameObject weaponPrefab;

    class TurretBaker : Baker<TurretAuthoring>
    {
        public override void Bake(TurretAuthoring authoring)
        {

            Entity entity = GetEntity(TransformUsageFlags.None);

            AddComponent(entity, new TurretData
            {
                weaponPrefabEntity = GetEntity(authoring.weaponPrefab, TransformUsageFlags.None)

            });
        }
    }
}