using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using UnityEngine;

public struct PlaybackData : IComponentData
{
    public int KeysPlayed;
    public int PlaybackIndex;
    public int PlaybackKeyIndex;
    public float PlaybackTime;
    //public float PlaybackDuration;

}