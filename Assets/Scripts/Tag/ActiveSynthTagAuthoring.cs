
using Unity.Entities;
using UnityEngine;


/// <summary>
///  NOT USED AT THE MOMENT
/// </summary>

public struct ActiveSynthTag : IComponentData
{

}

public class ActiveSynthTagAuthoring : MonoBehaviour
{

    class SynthBaker : Baker<SynthAuthoring>
    {

        public override void Bake(SynthAuthoring authoring)
        {

            Entity entity = GetEntity(TransformUsageFlags.None);

            AddComponent(entity, new ActiveSynthTag
            {

            });

        }
    }
}