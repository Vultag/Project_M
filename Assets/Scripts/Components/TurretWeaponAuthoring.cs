using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using UnityEngine;

public class TurretWeaponAuthoring : MonoBehaviour
{

    class TurretWeaponBaker : Baker<TurretWeaponAuthoring>
    {
        public override void Bake(TurretWeaponAuthoring authoring)
        {

            Entity entity = GetEntity(TransformUsageFlags.None);

            ///AddComponent(entity, new TurretWeaponTag());
        }
    }
}