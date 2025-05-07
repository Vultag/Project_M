using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using UnityEngine;

public struct MCoreData : IComponentData
{
    public float Health;
    public Entity TurretPrefab;
}

public class MCoreAuthoring : MonoBehaviour
{

    public float Health;
    public GameObject TurretPrefab;

    class MCoreAuthoringBaker : Baker<MCoreAuthoring>
    {
        public override void Bake(MCoreAuthoring authoring)
        {

            Entity entity = GetEntity(TransformUsageFlags.None);

            AddComponent(entity, new MCoreData
            {
                Health = authoring.Health,
                TurretPrefab = GetEntity(authoring.TurretPrefab, TransformUsageFlags.Dynamic),

            });
        }
    }
}