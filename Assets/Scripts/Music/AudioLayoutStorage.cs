using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;


/// <summary>
///  Curently only store data yet to be added to the audio thread
///  -> Hold all audio layout for gameloop processing ?
///  
/// ! Conflict may emerge when more than 1 update in a frame
/// 
/// </summary>

public struct AudioLayoutStorage
{

    /// store synths / playbacks ?

    // PRIVATize SOME STUFF !

    public SynthData NewSynthsData;
    public PlaybackAudioBundle NewPlaybackAudioBundles;
    public int synthPlaybackIdx;
    public int synthActivationIdx;

    public bool UpdateRequirement;

    public bool AddSynthUpdateRequirement;
    public bool SelectSynthUpdateRequirement;
    public bool ModifySynthUpdateRequirement;

    public bool PlaybackUpdateRequirement;

    public bool ActivationUpdateRequirement;
    private bool ActivationState;

    public void WriteAddSynth(SynthData newSynthsData)
    {
        NewSynthsData = newSynthsData;
        //WritePlayback(new PlaybackAudioBundle(), PlaybackIdxtoRemplace);
        UpdateRequirement = true;
        AddSynthUpdateRequirement = true;
    }
    public void WriteSelectSynth(int synthIdx)
    {
        synthActivationIdx = synthIdx;
        UpdateRequirement = true;
        SelectSynthUpdateRequirement = true;
    }
    public void WriteModifySynth(SynthData newSynthsData)
    {
        NewSynthsData = newSynthsData;
        UpdateRequirement = true;
        ModifySynthUpdateRequirement = true;
    }
    public void WritePlayback(PlaybackAudioBundle newPlaybackAudioBundles, int synthIdx)
    {
        NewPlaybackAudioBundles = newPlaybackAudioBundles;
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
        return NewSynthsData;
    }
    public int ReadSelectSynth()
    {
        SelectSynthUpdateRequirement = false;
        return synthActivationIdx;
    }
    public SynthData ReadModifySynth()
    {
        ModifySynthUpdateRequirement = false;
        return NewSynthsData;
    }
    public PlaybackAudioBundle ReadPlayback()
    {
        PlaybackUpdateRequirement = false;
        return NewPlaybackAudioBundles;
    }
    public (int, bool) ReadActivation()
    {
        ActivationUpdateRequirement = false;
        return (synthActivationIdx, ActivationState);
    }
}