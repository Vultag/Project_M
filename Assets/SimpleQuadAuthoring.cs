using Unity.Entities;
using Unity.Rendering;
using Unity.Transforms;
using UnityEngine;

public class SimpleQuadAuthoring : MonoBehaviour
{
    public Mesh quadMesh;  // Reference to a 2D quad mesh (or plane)
    public Material quadMaterial;  // Reference to the material

    // Baker class that converts the MonoBehaviour to ECS components
    class SimpleQuadBaker : Baker<SimpleQuadAuthoring>
    {
        public override void Bake(SimpleQuadAuthoring authoring)
        {
            // Create a new entity from the authoring object
            Entity entity = GetEntity(TransformUsageFlags.Dynamic);

            // Add the LocalToWorld component (required for transformation)
            AddComponent(entity, new LocalToWorld());

            // Create a RenderMeshDescription with basic render settings
            var renderMeshDescription = new RenderMeshDescription(
                shadowCastingMode: UnityEngine.Rendering.ShadowCastingMode.Off,
                receiveShadows: false,
                renderingLayerMask: 0
            );

            // Create an array for the mesh and material
            var renderMeshArray = new RenderMeshArray(
                new Material[] { authoring.quadMaterial },
                new Mesh[] { authoring.quadMesh }
            );

            // Use RenderMeshUtility to add the necessary components to the entity
            RenderMeshUtility.AddComponents(
                entity,
                World.DefaultGameObjectInjectionWorld.EntityManager,
                renderMeshDescription,
                renderMeshArray,
                MaterialMeshInfo.FromRenderMeshArrayIndices(0, 0)
            );


            // Add a LocalTransform component with position (optional)
            AddComponent(entity, new LocalTransform { Position = new Unity.Mathematics.float3(0, 0, 0) });
        }
    }
}