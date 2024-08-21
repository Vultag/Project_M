

using Unity.Entities;
using UnityEngine;

public struct PlayerData : IComponentData
{
    public float mouv_speed;
    public Entity ActiveCanon;
    public Entity WeaponPrefab;
}
public class PlayerDataAuthoring : MonoBehaviour
{

    public float mouv_speed;
    public GameObject ActiveCanon;
    public GameObject WeaponPrefab;

    class DelayedDestroyDataBaker : Baker<PlayerDataAuthoring>
    {
        public override void Bake(PlayerDataAuthoring authoring)
        {

            Entity playerEntity = GetEntity(TransformUsageFlags.None);

            AddComponent(playerEntity,new PlayerData
            {

                mouv_speed = authoring.mouv_speed,
                ActiveCanon = GetEntity(authoring.ActiveCanon, TransformUsageFlags.None),
                WeaponPrefab = GetEntity(authoring.WeaponPrefab, TransformUsageFlags.None),

            });
        }
    }
}