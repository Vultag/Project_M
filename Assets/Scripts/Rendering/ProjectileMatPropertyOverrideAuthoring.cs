
using Unity.Entities;
using Unity.Rendering;
using UnityEngine;

[MaterialProperty("_Ocs1SinSawSquareFactor")]
public struct Ocs1SinSawSquareFactorOverride : IComponentData
{
    public Vector3 Value;
}
[MaterialProperty("_Ocs2SinSawSquareFactor")]
public struct Ocs2SinSawSquareFactorOverride : IComponentData
{
    public Vector3 Value;
}
public class ProjectileMatPropertyOverrideAuthoring : MonoBehaviour
{

    class ProjectileMatPropertyOverrideBaker : Baker<ProjectileMatPropertyOverrideAuthoring>
    {
        public override void Bake(ProjectileMatPropertyOverrideAuthoring authoring)
        {

            Entity entity = GetEntity(TransformUsageFlags.None);

            AddComponent(entity, new Ocs1SinSawSquareFactorOverride());
            AddComponent(entity, new Ocs2SinSawSquareFactorOverride());
        }
    }
}