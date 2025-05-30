

using Unity.Entities;
using UnityEngine;

public struct PlayerData : IComponentData
{
    /// Strenght factor compared to forward
    public float propellerBackpedalRelativeStrenght;
    public float propellerMaxStrenght;
    public float rotate_speed;
    public Entity MainCanon;
    public Entity WeaponPrefab;
    public Entity DrumMachinePrefab;
    public Entity AudioSourcePrefab;
    public Entity Propeller;
}

public class PlayerDataAuthoring : MonoBehaviour
{

    public int HP;
    public float propellerMaxStrenght;
    public float propellerBackpedalRelativeStrenght;
    public float rotate_speed;
    public GameObject MainCanon;
    public GameObject WeaponPrefab;
    public GameObject DrumMachinePrefab;
    public GameObject AudioSourcePrefab;
    public GameObject PropellerGB;

    class DelayedDestroyDataBaker : Baker<PlayerDataAuthoring>
    {
        public override void Bake(PlayerDataAuthoring authoring)
        {

            Entity playerEntity = GetEntity(TransformUsageFlags.None);

            AddComponent(playerEntity,new PlayerData
            {

                propellerMaxStrenght = authoring.propellerMaxStrenght,
                propellerBackpedalRelativeStrenght = authoring.propellerBackpedalRelativeStrenght,
                rotate_speed = authoring.rotate_speed,
                MainCanon = GetEntity(authoring.MainCanon, TransformUsageFlags.None),
                WeaponPrefab = GetEntity(authoring.WeaponPrefab, TransformUsageFlags.None),
                DrumMachinePrefab = GetEntity(authoring.DrumMachinePrefab, TransformUsageFlags.None),
                AudioSourcePrefab = GetEntity(authoring.AudioSourcePrefab, TransformUsageFlags.None),
                Propeller = GetEntity(authoring.PropellerGB, TransformUsageFlags.None),

            });
            AddComponent(playerEntity, new HealthData{ HP = authoring.HP});
        }
    }
}