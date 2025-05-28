using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

public struct ProjectileInstanceData : IComponentData
{
    public float2 direction;
    public float damage;
    public float speed;
    public int penetrationCapacity;
    public float remainingLifeTime;

}