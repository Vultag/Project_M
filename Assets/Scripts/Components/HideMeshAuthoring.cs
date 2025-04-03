using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using Unity.Rendering;
using UnityEngine;

public class HideMeshAuthoring : MonoBehaviour
{

    public int materialID;

    class HideMeshBaker : Baker<HideMeshAuthoring>
    {
        public override void Bake(HideMeshAuthoring authoring)
        {

            Entity entity = GetEntity(TransformUsageFlags.Renderable);

            //SetComponent<MaterialMeshInfo>(entity, new MaterialMeshInfo
            //{
            //    Mesh = 0,
            //    Material = authoring.materialID,
            //});

            //AddComponent<MaterialMeshInfo>(entity, new MaterialMeshInfo
            //{
            //    Mesh = 0,
            //    Material = authoring.materialID,
            //});
        }
    }


}
