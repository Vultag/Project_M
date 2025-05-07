using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities.UniversalDelegates;
using UnityEngine;


/// <summary>
///  Curently only store data yet to be added to the audio thread
///  -> Hold all audio layout for gameloop processing ?
///  -> NO, complete audio layout stored in a playbacks Holder
///  
/// ! Conflict may emerge when more than 1 update in a frame
/// -> FIXED with queues instead of individual checks
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
    public NativeArray<SynthData> AuxillarySynthsData;
    public NativeArray<PlaybackAudioBundle> PlaybackAudioBundles;
    public static int activeSynthIdx;

    public SynthData NewSynthData;
    //public PlaybackAudioBundle NewPlaybackAudioBundle;
    public int synthPlaybackIdx;


    public MusicSheetData ActiveMusicSheet;

    public bool UpdateRequirement;

    public bool AddSynthUpdateRequirement;
    public bool SelectSynthUpdateRequirement;
    public bool ModifySynthUpdateRequirement;

    public int synthSelectionIdx;

    /// integrate to the other que ?
    public NativeQueue<int> PlaybackContextResetRequired;

    /// <summary>
    /// the shrink/grow factor for the new Playbackcontext arrays unpon filter read
    /// </summary>
    public int UpdatedActivePlaybackWeight;
    public NativeQueue<(int, PlaybackAudioBundle)> PlaybackWriteUpdateRequired;
    public NativeQueue<int> PlaybackActivationUpdateRequired;
    public NativeQueue<int> PlaybackDeactivationUpdateRequired;

    public void WriteAddSynth(SynthData newSynthData)
    {
        NewSynthData = newSynthData;
        var newSynthsData = new NativeArray<SynthData>(AuxillarySynthsData.Length + 1, Allocator.Persistent);
        var newPlaybackAudioBundles = new NativeArray<PlaybackAudioBundle>(PlaybackAudioBundles.Length + 1, Allocator.Persistent);
        for (int i = 0; i < AuxillarySynthsData.Length; i++)
        {
            newSynthsData[i] = AuxillarySynthsData[i];
            newPlaybackAudioBundles[i] = PlaybackAudioBundles[i];
            //PlaybackAudioBundles[i].PlaybackKeys.Dispose();
        }
        newSynthsData[newSynthsData.Length-1] = NewSynthData;
        //Debug.LogWarning(newSynthsData.Length - 1);
        //Debug.LogWarning(newSynthData.ADSR.Sustain);
        AuxillarySynthsData.Dispose();
        PlaybackAudioBundles.Dispose();
        AuxillarySynthsData = newSynthsData;
        PlaybackAudioBundles = newPlaybackAudioBundles;

        UpdateRequirement = true;
        AddSynthUpdateRequirement = true;
    }
    public void WriteSelectSynth(int synthIdx)
    {
        synthSelectionIdx = synthIdx;
        //Debug.Log(synthIdx);
        //Debug.Log(AuxillarySynthsData[synthIdx].ADSR.Sustain);
        UpdateRequirement = true;
        SelectSynthUpdateRequirement = true;
    }
    public void WriteModifySynth(SynthData newSynthData)
    {
        var newSynthsData = new NativeArray<SynthData>(AuxillarySynthsData.Length, Allocator.Persistent);
        AuxillarySynthsData.CopyTo(newSynthsData);
        //Debug.Log(activeSynthIdx);
        newSynthsData[activeSynthIdx] = newSynthData;
        AuxillarySynthsData.Dispose();
        AuxillarySynthsData = newSynthsData;

        NewSynthData = newSynthData;
        UpdateRequirement = true;
        ModifySynthUpdateRequirement = true;
    }

    /// REMPLACE WRITE AND ACTIVATION BY QUE 

    //public void WritePlayback(PlaybackAudioBundle newPlaybackAudioBundle, int synthIdx)
    //{
    //    NewPlaybackAudioBundle = newPlaybackAudioBundle;
    //    var newPlaybackAudioBundles = new NativeArray<PlaybackAudioBundle>(PlaybackAudioBundles.Length, Allocator.Persistent);
    //    PlaybackAudioBundles.CopyTo(newPlaybackAudioBundles);
    //    newPlaybackAudioBundles[synthIdx] = newPlaybackAudioBundle;
    //    PlaybackAudioBundles.Dispose();
    //    PlaybackAudioBundles = newPlaybackAudioBundles;

    //    synthPlaybackIdx = synthIdx;
    //    UpdateRequirement = true;
    //    PlaybackUpdateRequirement = true;
    //}
    //public void WriteActivation(int synthIdx, bool OnOff)
    //{
    //    synthActivationIdx = synthIdx;
    //    if (OnOff)
    //    {
    //        ActivationState = true;
    //    }
    //    else
    //    {
    //        ActivationState = false;
    //    }
    //    UpdateRequirement = true;
    //    ActivationUpdateRequirement = true;
    //}
    public void WriteActivation(int synthIdx)
    {
        PlaybackActivationUpdateRequired.Enqueue(synthIdx);
        UpdatedActivePlaybackWeight++;
    }
    public void WriteDeactivation(int synthIdx)
    {
        PlaybackDeactivationUpdateRequired.Enqueue(synthIdx);
        UpdatedActivePlaybackWeight--;
    }
    public void WritePlayback(PlaybackAudioBundle newPlaybackAudioBundle, int synthIdx)
    {
        PlaybackWriteUpdateRequired.Enqueue((synthIdx, newPlaybackAudioBundle));
        //    NewPlaybackAudioBundle = newPlaybackAudioBundle;
        var newPlaybackAudioBundles = new NativeArray<PlaybackAudioBundle>(PlaybackAudioBundles.Length, Allocator.Persistent);
        PlaybackAudioBundles.CopyTo(newPlaybackAudioBundles);
        newPlaybackAudioBundles[synthIdx] = newPlaybackAudioBundle;
        PlaybackAudioBundles.Dispose();
        PlaybackAudioBundles = newPlaybackAudioBundles;
    }
    public SynthData ReadAddSynth()
    {
        AddSynthUpdateRequirement = false;
        return NewSynthData;
    }
    public int ReadSelectSynth()
    {
        SelectSynthUpdateRequirement = false;
        return synthSelectionIdx;
    }
    public SynthData ReadModifySynth()
    {
        ModifySynthUpdateRequirement = false;
        return NewSynthData;
    }
    //public PlaybackAudioBundle ReadPlayback()
    //{
    //    PlaybackUpdateRequirement = false;
    //    return NewPlaybackAudioBundle;
    //}
    //public (int, bool) ReadActivation()
    //{
    //    ActivationUpdateRequirement = false;
    //    return (synthActivationIdx, ActivationState);
    //}
}