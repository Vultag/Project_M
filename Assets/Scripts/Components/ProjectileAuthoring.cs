using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using UnityEngine;

public struct ProjectileInstanceData : IComponentData
{
    public float damage;
    public float speed;
    public int penetrationCapacity;
    public float remainingLifeTime;

}