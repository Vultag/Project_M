using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using UnityEngine;

public struct MCoreData : IComponentData
{
    public float Health;
    ///to do...
}

public class MCoreAuthoring : MonoBehaviour
{

    public float Health;

    class MCoreAuthoringBaker : Baker<MCoreAuthoring>
    {
        public override void Bake(MCoreAuthoring authoring)
        {

            Entity entity = GetEntity(TransformUsageFlags.None);

            AddComponent(entity, new MCoreData
            {
                Health = authoring.Health,

            });
        }
    }
}