using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using UnityEngine;

public struct PlaybackData : IComponentData
{
    public int KeysPlayed;
    public int SynthIndex;
    public int PlaybackKeyIndex;
    public float PlaybackTime;
    //Vector2 GideReferenceDirection;
    //public float PlaybackDuration;

}