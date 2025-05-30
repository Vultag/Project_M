using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;


/// <summary>
/// turn into SynthPlaybackRecordingData ?
/// </summary>
public struct SynthPlaybackRecordingData : IComponentData
{
    /// <summary>
    /// Turn synthIdx ?
    /// </summary>
    public FullEquipmentIdx fEquipmentIdx;
    public float startBeat;
    public float duration;
    public float2 GideReferenceDirection;
    public float activeLegatoFz;
    //public NativeList<PlaybackKey> KeyDatasAccumulator;
}
public struct DrumMachinePlaybackRecordingData : IComponentData
{
    public FullEquipmentIdx fEquipmentIdx;
    public float startBeat;
    public float duration;
}


//internal buffer capacity OPTI
public struct PlaybackRecordingKeysBuffer : IBufferElementData
{
    public PlaybackKey playbackRecordingKey;
}
public struct PlaybackRecordingPadsBuffer : IBufferElementData
{
    public PlaybackPad playbackRecordingPad;
}
public struct MachineDrumPlaybackAudioBundle
{
    //make sure to dispose
    public NativeArray<PlaybackPad> PlaybackPads;
    public float PlaybackDuration;
}
public struct PlaybackPad
{
    public ushort instrumentIdx;
    public float time;
    /// no need for lenght cause no sustain/decay on machinedrum
    ///public float lenght;

}
