using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using UnityEngine;

public class DisableOnStartAuthoring : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        
    }
    class DisableOnStartBaker : Baker<DisableOnStartAuthoring>
    {
        public override void Bake(DisableOnStartAuthoring authoring)
        {

            Entity entity = GetEntity(TransformUsageFlags.None);

            AddComponent<Disabled>(entity);
        }
    }
}
