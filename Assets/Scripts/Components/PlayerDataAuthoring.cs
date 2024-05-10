

using Unity.Entities;
using UnityEngine;

public struct PlayerData : IComponentData
{
    public float mouv_speed;
}
public class PlayerDataAuthoring : MonoBehaviour
{

    public float mouv_speed;

    class DelayedDestroyDataBaker : Baker<PlayerDataAuthoring>
    {
        public override void Bake(PlayerDataAuthoring authoring)
        {

            Entity playerEntity = GetEntity(TransformUsageFlags.None);

            AddComponent(playerEntity,new PlayerData
            {

                mouv_speed = authoring.mouv_speed

            });
        }
    }
}