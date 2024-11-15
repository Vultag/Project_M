using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;


public struct PlaybackRecordingData : IComponentData
{
    public int synthIndex;
    public float time;
    public float duration;
    public float2 GideReferenceDirection;
    public float activeLegatoFz;
    //public NativeList<PlaybackKey> KeyDatasAccumulator;
}


//internal buffer capacity OPTI
public struct PlaybackRecordingKeysBuffer : IBufferElementData
{
    public PlaybackKey playbackRecordingKey;
}
