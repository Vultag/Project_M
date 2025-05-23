using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;

[Flags]
public enum MachineDrumContent
{
    SnareDrum,
    BaseDrum,
    HighHat

}
public struct DrumMachineData : IComponentData
{
    public ushort equipmentIdx;
    public MachineDrumContent machineDrumContent;
    /// fixedArray for instrument add order ?
    public FixedList32Bytes<byte> InstrumentAddOrder;
}