using System.Collections;
using System.Collections.Generic;
using NUnit.Framework.Constraints;
using TMPro;
using Unity.Mathematics;
using UnityEngine;


/// <summary>
///  BE SURE TO COPY BY REFERENCE
/// </summary>
public struct FullPlaybackBundle
{
    public PlaybackAudioBundle playbackAudioBundle;
    public MusicSheetData musicSheet;
    //public Sprite associatedItemSprite;
    //public Color associatedItemColor;
}

public class UIPlaybacksHolder : MonoBehaviour
{
    /// single for now -> array ? list ? stack ?
    //public PlaybackHolder PBholder1;
    //public PlaybackHolder PBholder2;
    //public PlaybackHolder PBholder3;
    [SerializeField]
    private PlaybackHolder[] PBholders;

    /// <summary>
    /// DO NEW PREPAIR PLAY / RECORD HERE INSTEAD OF IN SYNTH UI ELEMENT ?
    /// </summary>
    [HideInInspector]
    public List<FullPlaybackBundle>[] synthFullBundleLists = new List<FullPlaybackBundle>[6];

    //[HideInInspector]
    //public List<FullPlaybackBundle> Synth1FullBundleList = new List<FullPlaybackBundle>(8);
    //[HideInInspector]
    //public List<FullPlaybackBundle> Synth2FullBundleList = new List<FullPlaybackBundle>(8);
    //[HideInInspector]
    //public List<FullPlaybackBundle> Synth3FullBundleList = new List<FullPlaybackBundle>(8);
    //[HideInInspector]
    //public List<FullPlaybackBundle> Synth4FullBundleList = new List<FullPlaybackBundle>(8);
    //[HideInInspector]
    //public List<FullPlaybackBundle> Synth5FullBundleList = new List<FullPlaybackBundle>(8);
    //[HideInInspector]
    //public List<FullPlaybackBundle> Synth6FullBundleList = new List<FullPlaybackBundle>(8);

    void Start()
    {
        // Initialize each list
        for (int i = 0; i < synthFullBundleLists.Length; i++)
        {
            synthFullBundleLists[i] = new List<FullPlaybackBundle>(8);
        }
    }

    public void _AddSynthPlaybackContainer(ref PlaybackAudioBundle newAudioBundle,ref MusicSheetData newSheetData, int synthIdx)
    {

        //synthFullBundleLists[PBidx.x][PBidx.y] = new FullPlaybackBundle { playbackAudioBundle =newAudioBundle,musicSheet=newSheetData};
        synthFullBundleLists[synthIdx].Add(new FullPlaybackBundle { playbackAudioBundle = newAudioBundle, musicSheet = newSheetData });
        //Debug.Log(synthFullBundleLists[synthIdx].ToArray().Length-1);
        
        PBholders[synthIdx]._AddContainerUI(new int2(synthIdx, synthFullBundleLists[synthIdx].ToArray().Length-1));
        PBholders[synthIdx].uiManager._SetSynthUItoSleep(synthIdx);

        //switch (PBidx.x)
        //{
        //    case 0:

        //        PBholder1._AddContainerUI(PBidx);
        //        break;
        //    //case 1:
        //    //    PBholder2._AddContainer(newAudioBundle, newSheetData);
        //    //    break;
        //    //case 2:
        //    //    PBholder3._AddContainer(newAudioBundle, newSheetData);
        //    //    break;

        //}

    }

    public void _ArmPlaybackForActivation(int2 PBidx)
    {

    }



}
