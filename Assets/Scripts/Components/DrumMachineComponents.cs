using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;

/// <summary>
/// [Flags]
/// </summary>
public enum MachineDrumContent
{
    //None, // 0
    SnareDrum, 
    BaseDrum, 
    HighHat,
    LowerTom,
    MidTom,
    HighTom,
    Clap,
    Cymbal,
    EggShaker,

}
public struct DrumMachineData : IComponentData
{
    public ushort equipmentIdx;
    ///public MachineDrumContent machineDrumContent;
    /// fixedArray for instrument add order ?
    public FixedList32Bytes<byte> InstrumentsInAddOrder;
}
