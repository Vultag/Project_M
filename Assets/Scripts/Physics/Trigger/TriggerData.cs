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
    DamageEffect = 1 << 0,
    WeaponCollectible = 1 << 1,
    DrumMachineCollectibe = 1 << 2,
}