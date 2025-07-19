using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using UnityEngine;

public struct TriggerData : IComponentData
{
    public TriggerType triggerType;
}


[Flags]
public enum TriggerType : byte
{
    ProjectileDamageEffect = 1 << 0,
    CollisionDamageEffect = 1 << 1,
    WeaponCollectible = 1 << 2,
    DrumMachineCollectibe = 1 << 3,
}