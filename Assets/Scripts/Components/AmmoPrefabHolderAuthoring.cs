using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using UnityEngine;
public struct AmmoPrefabHolder : IComponentData
{
    public Entity ProjectilePrefab;
}
public class AmmoPrefabHolderAuthoring : MonoBehaviour
{
    public GameObject ProjectilePrefab;
    class AmmoPrefabHolderBaker : Baker<AmmoPrefabHolderAuthoring>
    {
        public override void Bake(AmmoPrefabHolderAuthoring authoring)
        {

            var prefabEntity = GetEntity(authoring.ProjectilePrefab, TransformUsageFlags.None);
            var singletonEntity = CreateAdditionalEntity(TransformUsageFlags.None);
            AddComponent(singletonEntity, new AmmoPrefabHolder
            {
                ProjectilePrefab = prefabEntity
            });
        }
    }
}
