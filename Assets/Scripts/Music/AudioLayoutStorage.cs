using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities.UniversalDelegates;
using UnityEngine;


/// <summary>
///  Curently only store data yet to be added to the audio thread
///  -> Hold all audio layout for gameloop processing ?
///  
/// ! Conflict may emerge when more than 1 update in a frame
/// 
/// </summary>

// Create a static class to hold the shared AudioLayoutStorage struct
public static class AudioLayoutStorageHolder
{
    public static AudioLayoutStorage audioLayoutStorage;
}

public struct AudioLayoutStorage
{

    /// store synths / playbacks ?

    // PRIVATize SOME STUFF !
    ///change synthdata component to index value here?
    public NativeArray<SynthData> SynthsData;
    public NativeArray<PlaybackAudioBundle> PlaybackAudioBundles;
    public static int activeSynthIdx;

    public SynthData NewSynthData;
    public PlaybackAudioBundle NewPlaybackAudioBundle;
    public int synthPlaybackIdx;
    public int synthActivationIdx;

    public bool UpdateRequirement;

    public bool AddSynthUpdateRequirement;
    public bool SelectSynthUpdateRequirement;
    public bool ModifySynthUpdateRequirement;

    public bool PlaybackUpdateRequirement;

    public bool ActivationUpdateRequirement;
    private bool ActivationState;

    public NativeQueue<int> PlaybackContextResetRequired;

    public void WriteAddSynth(SynthData newSynthData)
    {
        NewSynthData = newSynthData;
        var newSynthsData = new NativeArray<SynthData>(SynthsData.Length + 1, Allocator.Persistent);
        var newPlaybackAudioBundles = new NativeArray<PlaybackAudioBundle>(PlaybackAudioBundles.Length + 1, Allocator.Persistent);
        for (int i = 0; i < SynthsData.Length; i++)
        {
            newSynthsData[i] = SynthsData[i];
            newPlaybackAudioBundles[i] = PlaybackAudioBundles[i];
            //PlaybackAudioBundles[i].PlaybackKeys.Dispose();
        }
        newSynthsData[newSynthsData.Length-1] = NewSynthData;
        SynthsData.Dispose();
        PlaybackAudioBundles.Dispose();
        SynthsData = newSynthsData;
        PlaybackAudioBundles = newPlaybackAudioBundles;

        UpdateRequirement = true;
        AddSynthUpdateRequirement = true;
    }
    public void WriteSelectSynth(int synthIdx)
    {
        synthActivationIdx = synthIdx;
        UpdateRequirement = true;
        SelectSynthUpdateRequirement = true;
    }
    public void WriteModifySynth(SynthData newSynthData)
    {
        var newSynthsData = new NativeArray<SynthData>(SynthsData.Length, Allocator.Persistent);
        SynthsData.CopyTo(newSynthsData);
        newSynthsData[activeSynthIdx] = newSynthData;
        SynthsData.Dispose();
        SynthsData = newSynthsData;

        NewSynthData = newSynthData;
        UpdateRequirement = true;
        ModifySynthUpdateRequirement = true;
    }
    public void WritePlayback(PlaybackAudioBundle newPlaybackAudioBundle, int synthIdx)
    {
        NewPlaybackAudioBundle = newPlaybackAudioBundle;
        var newPlaybackAudioBundles = new NativeArray<PlaybackAudioBundle>(PlaybackAudioBundles.Length, Allocator.Persistent);
        PlaybackAudioBundles.CopyTo(newPlaybackAudioBundles);
        newPlaybackAudioBundles[synthIdx] = newPlaybackAudioBundle;
        PlaybackAudioBundles.Dispose();
        PlaybackAudioBundles = newPlaybackAudioBundles;

        synthPlaybackIdx = synthIdx;
        UpdateRequirement = true;
        PlaybackUpdateRequirement = true;
    }
    public void WriteActivation(int synthIdx, bool OnOff)
    {
        synthActivationIdx = synthIdx;
        if (OnOff)
        {
            ActivationState = true;
        }
        else
        {
            ActivationState = false;
        }
        UpdateRequirement = true;
        ActivationUpdateRequirement = true;
    }
    public SynthData ReadAddSynth()
    {
        AddSynthUpdateRequirement = false;
        return NewSynthData;
    }
    public int ReadSelectSynth()
    {
        SelectSynthUpdateRequirement = false;
        return synthActivationIdx;
    }
    public SynthData ReadModifySynth()
    {
        ModifySynthUpdateRequirement = false;
        return NewSynthData;
    }
    public PlaybackAudioBundle ReadPlayback()
    {
        PlaybackUpdateRequirement = false;
        return NewPlaybackAudioBundle;
    }
    public (int, bool) ReadActivation()
    {
        ActivationUpdateRequirement = false;
        return (synthActivationIdx, ActivationState);
    }
}