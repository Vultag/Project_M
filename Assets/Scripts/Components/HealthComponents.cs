using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using UnityEngine;


public struct HealthData : IComponentData
{
    public float HP;
}
public struct GlobalDamageEvent : IBufferElementData
{
    public Entity Target;  // The entity that receives damage
    public float DamageValue;
}