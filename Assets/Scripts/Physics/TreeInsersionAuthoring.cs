
using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using UnityEngine;


//used to insert a physics object in a tree after it has been instanciated
public struct TreeInsersionTag : IComponentData
{

}
public class TreeInsersionAuthoring : MonoBehaviour
{

    class TreeInsersionBaker : Baker<TreeInsersionAuthoring>
    {
        public override void Bake(TreeInsersionAuthoring authoring)
        {

            Entity entity = GetEntity(TransformUsageFlags.None);

            AddComponent(entity, new TreeInsersionTag
            {


            });
        }
    }
}