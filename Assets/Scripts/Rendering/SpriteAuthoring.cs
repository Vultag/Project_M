using System.Collections;
using System.Collections.Generic;
using NUnit.Framework.Internal;
using Unity.Entities;
using Unity.Entities.Graphics;
using Unity.Mathematics;
using Unity.Rendering;
using Unity.Transforms;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// 
///  UNUSED ?
/// 
/// </summary>



//public struct SpriteData : IComponentData
//{

//}
public class SpriteAuthoring : MonoBehaviour
{
    public Texture2D SpriteTexture;
    public Material spriteMaterial;  
    public Mesh spriteMeshAsset;

    class SpriteDataBaker : Baker<SpriteAuthoring>
    {
        public override void Bake(SpriteAuthoring authoring)
        {
            Entity entity = GetEntity(TransformUsageFlags.Renderable);
            var world = World.DefaultGameObjectInjectionWorld;
            var entityManager = world.EntityManager;

            var spriteMeshAsset = authoring.spriteMeshAsset;
            var SpriteTexture = authoring.SpriteTexture;
            var spriteMaterial = authoring.spriteMaterial;
            
            var spriteMesh = new Mesh
            {
                vertices = (Vector3[])spriteMeshAsset.vertices.Clone(),
                triangles = (int[])spriteMeshAsset.triangles.Clone(),
                normals = (Vector3[])spriteMeshAsset.normals.Clone(),
                uv = (Vector2[])spriteMeshAsset.uv.Clone()
            };
            spriteMesh.RecalculateBounds();

            float textureWidth = SpriteTexture.width;
            float textureHeight = SpriteTexture.height;

            float aspectRatio = textureWidth / textureHeight;

            AdjustUVsToTexture(ref spriteMesh, aspectRatio);

            Material materialInstance = new Material(spriteMaterial);
            materialInstance.SetTexture("_MainTex", SpriteTexture);

            var renderMeshArray = new RenderMeshArray(
                 new Material[] { materialInstance },
                 new Mesh[] { spriteMesh }
             );

            var desc = new RenderMeshDescription(
               shadowCastingMode: ShadowCastingMode.Off,
               receiveShadows: false);

            //RenderMeshUtility.AddComponents(
            //    entity,
            //    entityManager,
            //    desc,
            //    renderMeshArray,
            //    MaterialMeshInfo.FromRenderMeshArrayIndices(0, 0)
            //);


            // Add RenderMesh component
            //var renderMesh = new RenderMesh
            //{
            //    mesh = spriteMesh,
            //    material = materialInstance
            //};
            //AddSharedComponentManaged(entity, renderMesh);


            //var renderMeshArray = new RenderMeshArray(
            //     new Material[] { materialInstance },
            //     new Mesh[] { spriteMesh }
            // );
            //var desc = new RenderMeshDescription(
            //   shadowCastingMode: ShadowCastingMode.Off,
            //   receiveShadows: false);

            AddSharedComponentManaged(entity, renderMeshArray);
            AddComponent(entity, MaterialMeshInfo.FromRenderMeshArrayIndices(0, 0));

            // Add RenderBounds component (required for culling)
            var bounds = spriteMesh.bounds;
            AddComponent(entity, new RenderBounds
            {
                Value = bounds.ToAABB()
            });

            AddComponent(entity, new LocalToWorld { Value = float4x4.Translate(float3.zero) });
            var spriteTransfrom = LocalTransform.FromPosition(0, 0, 0);
            spriteTransfrom.Rotation = Quaternion.Euler(0, 0, 0);
            AddComponent(entity, spriteTransfrom);

        }
    }


    private void Start()
    {
        /*
        var world = World.DefaultGameObjectInjectionWorld;
        var entityManager = world.EntityManager;
        Entity entity = entityManager.CreateEntity();

        var spriteMesh = new Mesh
        {
            vertices = (Vector3[])spriteMeshAsset.vertices.Clone(),
            triangles = (int[])spriteMeshAsset.triangles.Clone(),
            normals = (Vector3[])spriteMeshAsset.normals.Clone(),
            uv = (Vector2[])spriteMeshAsset.uv.Clone()
        };
        spriteMesh.RecalculateBounds();

        // Get the texture's width and height
        float textureWidth = SpriteTexture.width;
        float textureHeight = SpriteTexture.height;

        // Calculate the aspect ratio of the texture
        float aspectRatio = textureWidth / textureHeight;

        AdjustUVsToTexture(ref spriteMesh, aspectRatio);

        Material materialInstance = new Material(spriteMaterial);
        materialInstance.SetTexture("_MainTex", SpriteTexture);

        // Create a RenderMeshArray for material assignment
        var renderMeshArray = new RenderMeshArray(
             new Material[] { materialInstance },
             new Mesh[] { spriteMesh }
         );

        var desc = new RenderMeshDescription(
           shadowCastingMode: ShadowCastingMode.Off,
           receiveShadows: false);

        RenderMeshUtility.AddComponents(
            entity,
            entityManager,
            desc,
            renderMeshArray,
            MaterialMeshInfo.FromRenderMeshArrayIndices(0, 0)
        );
        entityManager.AddComponentData(entity, new LocalToWorld());

        entityManager.Instantiate(entity);
        entityManager.AddComponentData(entity, new LocalToWorld { Value = float4x4.Translate(float3.zero) });
        var spriteTransfrom = LocalTransform.FromPosition(0, 0, 0);
        spriteTransfrom.Rotation = Quaternion.Euler(0,0,0);
        entityManager.AddComponentData(entity, spriteTransfrom);
        */

 


}



    static void AdjustUVsToTexture(ref Mesh mesh, float aspectRatio)
    {
        Vector2[] uvs = mesh.uv;

        for (int i = 0; i < uvs.Length; i++)
        {
            // Scale the UVs while centering them
            uvs[i].y = uvs[i].y*aspectRatio - (aspectRatio-1)*0.5f;
        }

        mesh.uv = uvs;

    }

}
