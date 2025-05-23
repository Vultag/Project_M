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
    public SynthPlaybackAudioBundle playbackAudioBundle;
    public MusicSheetData musicSheet;
}
public struct FullMachineDrumPlaybackBundle
{
    public MachineDrumPlaybackAudioBundle playbackAudioBundle;
    public DrumPadSheetData drumPadSheet;
}

public class UIPlaybacksHolder : MonoBehaviour
{
    /// single for now -> array ? list ? stack ?
    //public PlaybackHolder PBholder1;
    //public PlaybackHolder PBholder2;
    //public PlaybackHolder PBholder3;
    [SerializeField]
    public PlaybackHolder[] PBholders;

    /// <summary>
    /// DO NEW PREPAIR PLAY / RECORD HERE INSTEAD OF IN SYNTH UI ELEMENT ?
    /// OPTI : ALREADY IN AUDIOLAYOUTSTORAGE ? redondant ?
    /// </summary>
    [HideInInspector]
    public List<FullSynthPlaybackBundle>[] synthFullBundleLists = new List<FullSynthPlaybackBundle>[6];
    [HideInInspector]
    public List<FullMachineDrumPlaybackBundle>[] machineDrumFullBundleLists = new List<FullMachineDrumPlaybackBundle>[1];


    void Start()
    {
        // Initialize each list
        for (int i = 0; i < synthFullBundleLists.Length; i++)
        {
            synthFullBundleLists[i] = new List<FullSynthPlaybackBundle>(8);
        }
        for (int i = 0; i < machineDrumFullBundleLists.Length; i++)
        {
            machineDrumFullBundleLists[i] = new List<FullMachineDrumPlaybackBundle>(8);
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
        for (int i = 0; i < machineDrumFullBundleLists.Length; i++)
        {
            for (int y = 0; y < machineDrumFullBundleLists[i].Count; y++)
            {
                machineDrumFullBundleLists[i][y].playbackAudioBundle.PlaybackPads.Dispose();
                machineDrumFullBundleLists[i][y].drumPadSheet._Dispose();
            }
        }
    }

    public void _AddSynthPlaybackContainer(ref SynthPlaybackAudioBundle newAudioBundle,ref MusicSheetData newSheetData, FullEquipmentIdx fEquipmentIdx)
    {
        UIManager.Instance.GetComponent<UIManager>().curentlyRecording = false;

        synthFullBundleLists[fEquipmentIdx.relativeIdx].Add(new FullSynthPlaybackBundle { playbackAudioBundle = newAudioBundle, musicSheet = newSheetData });

        PBholders[fEquipmentIdx.absoluteIdx]._AddContainerUI(new int2(fEquipmentIdx.absoluteIdx, synthFullBundleLists[fEquipmentIdx.relativeIdx].ToArray().Length-1), fEquipmentIdx.relativeIdx);
        UIManager.Instance._SetEquipmentUItoSleep(fEquipmentIdx.absoluteIdx);
        /// reactivate rec button
        UIManager.Instance.equipmentToolBar.transform.GetChild(fEquipmentIdx.absoluteIdx).GetChild(2).GetChild(0).gameObject.SetActive(true);

    }

    public void _AddDrumMachinePlaybackContainer(ref MachineDrumPlaybackAudioBundle newAudioBundle, in DrumPadSheetData newDrumPadSheetData, FullEquipmentIdx fEquipmentIdx)
    {
        UIManager.Instance.GetComponent<UIManager>().curentlyRecording = false;

        machineDrumFullBundleLists[fEquipmentIdx.relativeIdx].Add(new FullMachineDrumPlaybackBundle { playbackAudioBundle = newAudioBundle, drumPadSheet = newDrumPadSheetData });

        PBholders[fEquipmentIdx.absoluteIdx]._AddContainerUI(new int2(fEquipmentIdx.absoluteIdx, machineDrumFullBundleLists[fEquipmentIdx.relativeIdx].ToArray().Length - 1), fEquipmentIdx.relativeIdx);
        UIManager.Instance._SetEquipmentUItoSleep(fEquipmentIdx.absoluteIdx);
        /// reactivate rec button
        UIManager.Instance.equipmentToolBar.transform.GetChild(fEquipmentIdx.absoluteIdx).GetChild(2).GetChild(0).gameObject.SetActive(true);


    }

    public void _ArmPlaybackForActivation(int2 PBidx)
    {

    }



}
