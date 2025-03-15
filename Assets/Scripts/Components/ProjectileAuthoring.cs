using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using UnityEngine;

public struct ProjectileInstanceData : IComponentData
{
    public float damage;
    public int penetrationCapacity;
    public float remainingLifeTime;

}