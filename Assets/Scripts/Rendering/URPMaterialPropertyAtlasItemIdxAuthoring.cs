using Unity.Entities;
using Unity.Rendering;
using UnityEngine;

[MaterialProperty("_AtlasItemIdx")] // This must match the shader property name!
public struct URPMaterialPropertyAtlasItemIdx : IComponentData
{
    public float Value;
}
public class URPMaterialPropertyAtlasItemIdxAuthoring : MonoBehaviour
{
    //public int startingTextureIdx;

    class AtlasItemIdxBaker : Baker<URPMaterialPropertyAtlasItemIdxAuthoring>
    {
        public override void Bake(URPMaterialPropertyAtlasItemIdxAuthoring authoring)
        {

            Entity entity = GetEntity(TransformUsageFlags.None);

            AddComponent(entity, new URPMaterialPropertyAtlasItemIdx
            {
                /// not necessary ?
                //Value = authoring.startingTextureIdx
            });
        }
    }
}
