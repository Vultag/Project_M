using Unity.Collections;
using Unity.Entities;


public struct PlaybackRecordingData : IComponentData
{
    public int synthIndex;
    public float time;
    public float duration;
    //public NativeList<PlaybackKey> KeyDatasAccumulator;
}


//internal buffer capacity OPTI
public struct PlaybackRecordingKeysBuffer : IBufferElementData
{
    public PlaybackKey playbackRecordingKey;
}
