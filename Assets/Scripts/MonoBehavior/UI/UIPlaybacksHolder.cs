using System.Collections;
using System.Collections.Generic;
using NUnit.Framework.Constraints;
using TMPro;
using Unity.Mathematics;
using UnityEngine;


/// <summary>
///  BE SURE TO COPY BY REFERENCE
/// </summary>
public struct FullSynthPlaybackBundle
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
    public List<FullSynthPlaybackBundle>[] synthFullBundleLists = new List<FullSynthPlaybackBundle>[6];


    void Start()
    {
        // Initialize each list
        for (int i = 0; i < synthFullBundleLists.Length; i++)
        {
            synthFullBundleLists[i] = new List<FullSynthPlaybackBundle>(8);
        }
    }

    private void OnDestroy()
    {
        for (int i = 0; i < synthFullBundleLists.Length; i++)
        {
            for (int y = 0; y < synthFullBundleLists[i].Count; y++)
            {
                synthFullBundleLists[i][y].playbackAudioBundle.PlaybackKeys.Dispose();
                synthFullBundleLists[i][y].musicSheet._Dispose();
            }
        }
    }

    public void _AddSynthPlaybackContainer(ref PlaybackAudioBundle newAudioBundle,ref MusicSheetData newSheetData, FullEquipmentIdx fEquipmentIdx)
    {

        synthFullBundleLists[fEquipmentIdx.relativeIdx].Add(new FullSynthPlaybackBundle { playbackAudioBundle = newAudioBundle, musicSheet = newSheetData });

        PBholders[fEquipmentIdx.absoluteIdx]._AddContainerUI(new int2(fEquipmentIdx.absoluteIdx, synthFullBundleLists[fEquipmentIdx.relativeIdx].ToArray().Length-1), fEquipmentIdx.relativeIdx);
        UIManager.Instance._SetSynthUItoSleep(fEquipmentIdx.absoluteIdx);
        /// reactivate rec button
        UIManager.Instance.SynthToolBar.transform.GetChild(fEquipmentIdx.absoluteIdx).GetChild(2).GetChild(0).gameObject.SetActive(true);

    }

    /// DO _AddDrumMachinePlaybackContainer ?

    public void _ArmPlaybackForActivation(int2 PBidx)
    {

    }



}
