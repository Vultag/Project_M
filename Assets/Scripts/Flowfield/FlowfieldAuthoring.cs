using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

public struct FlowfieldCellData
{
    public float2 Direction;  
    public ushort Cost;        
    public bool IsBlocked;
    public bool InLineOfSight;
    public bool IsNextToObstacle;
}
public struct FlowfieldCellGPU
{
    public float ArrowRotation;
    public float Cost;
    public float IsBlocked;       // use 0.0f or 1.0f
    public float InLineOfSight;   // same here
}

public struct FlowfieldTag : IComponentData
{

}
public class FlowfieldAuthoring : MonoBehaviour
{

    class FlowfieldBaker : Baker<FlowfieldAuthoring>
    {
        public override void Bake(FlowfieldAuthoring authoring)
        {

            Entity entity = GetEntity(TransformUsageFlags.Dynamic);
            AddTransformUsageFlags(entity, TransformUsageFlags.NonUniformScale);

            AddComponent(entity, new FlowfieldTag());
        }
    }
}