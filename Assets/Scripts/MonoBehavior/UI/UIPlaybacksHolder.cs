using System.Collections;
using System.Collections.Generic;
using NUnit.Framework.Constraints;
using TMPro;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.UI;


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
    /// 
    ///  REMPLACE WITH CONTAINER GB LIST ?
    /// 
    /// </summary>
    [HideInInspector]
    public List<(FullSynthPlaybackBundle,PlaybackContainerUI)>[] synthFullBundleLists = new List<(FullSynthPlaybackBundle, PlaybackContainerUI)>[6];
    [HideInInspector]
    public List<(FullMachineDrumPlaybackBundle, PlaybackContainerUI)>[] machineDrumFullBundleLists = new List<(FullMachineDrumPlaybackBundle, PlaybackContainerUI)>[1];




    void Start()
    {
        // Initialize each list
        for (int i = 0; i < synthFullBundleLists.Length; i++)
        {
            synthFullBundleLists[i] = new List<(FullSynthPlaybackBundle, PlaybackContainerUI)>(8);
        }
        for (int i = 0; i < machineDrumFullBundleLists.Length; i++)
        {
            machineDrumFullBundleLists[i] = new List<(FullMachineDrumPlaybackBundle, PlaybackContainerUI)>(8);
        }
    }

    private void OnDestroy()
    {
        for (int i = 0; i < synthFullBundleLists.Length; i++)
        {
            for (int y = 0; y < synthFullBundleLists[i].Count; y++)
            {
                synthFullBundleLists[i][y].Item1.playbackAudioBundle.PlaybackKeys.Dispose();
                synthFullBundleLists[i][y].Item1.musicSheet._Dispose();
            }
        }
        for (int i = 0; i < machineDrumFullBundleLists.Length; i++)
        {
            for (int y = 0; y < machineDrumFullBundleLists[i].Count; y++)
            {
                machineDrumFullBundleLists[i][y].Item1.playbackAudioBundle.PlaybackPads.Dispose();
                machineDrumFullBundleLists[i][y].Item1.drumPadSheet._Dispose();
            }
        }
    }

    public void _AddSynthPlaybackContainer(ref SynthPlaybackAudioBundle newAudioBundle,ref MusicSheetData newSheetData, FullEquipmentIdx fEquipmentIdx)
    {
        if (PBholders[fEquipmentIdx.absoluteIdx].ContainerNumber == 0)
        {
            /// Activate Play button GB
            UIManager.Instance.equipmentToolBar.transform.GetChild(fEquipmentIdx.absoluteIdx).GetChild(2).GetChild(1).gameObject.SetActive(true);
        }

        UIManager.Instance.GetComponent<UIManager>().curentlyRecording = false;

        var newContainer = PBholders[fEquipmentIdx.absoluteIdx]._AddContainerUI(new int2(fEquipmentIdx.absoluteIdx, synthFullBundleLists[fEquipmentIdx.relativeIdx].ToArray().Length), fEquipmentIdx.relativeIdx); ;

        synthFullBundleLists[fEquipmentIdx.relativeIdx].Add((new FullSynthPlaybackBundle { playbackAudioBundle = newAudioBundle, musicSheet = newSheetData }, newContainer));

        if(!PBholders[fEquipmentIdx.absoluteIdx].AutoPlayOn)
            UIManager.Instance._SetEquipmentUItoSleep(fEquipmentIdx.absoluteIdx);
        /// reactivate rec button
        UIManager.Instance.equipmentToolBar.transform.GetChild(fEquipmentIdx.absoluteIdx).GetChild(2).GetChild(0).gameObject.SetActive(true);
        UIManager.Instance.equipmentToolBar.transform.GetChild(fEquipmentIdx.absoluteIdx).gameObject.GetComponentInChildren<Slider>().gameObject.transform.GetChild(1).GetChild(0).GetComponent<Image>().color = Color.green;

    }

    public void _AddDrumMachinePlaybackContainer(ref MachineDrumPlaybackAudioBundle newAudioBundle, in DrumPadSheetData newDrumPadSheetData, FullEquipmentIdx fEquipmentIdx)
    {
        UIManager.Instance.GetComponent<UIManager>().curentlyRecording = false;

        if (PBholders[fEquipmentIdx.absoluteIdx].ContainerNumber == 0)
        {
            /// Activate Play button GB
            UIManager.Instance.equipmentToolBar.transform.GetChild(fEquipmentIdx.absoluteIdx).GetChild(2).GetChild(1).gameObject.SetActive(true);
        }

        var newContainer = PBholders[fEquipmentIdx.absoluteIdx]._AddContainerUI(new int2(fEquipmentIdx.absoluteIdx, machineDrumFullBundleLists[fEquipmentIdx.relativeIdx].ToArray().Length), fEquipmentIdx.relativeIdx);
;
        machineDrumFullBundleLists[fEquipmentIdx.relativeIdx].Add((new FullMachineDrumPlaybackBundle { playbackAudioBundle = newAudioBundle, drumPadSheet = newDrumPadSheetData }, newContainer));

        if (!PBholders[fEquipmentIdx.absoluteIdx].AutoPlayOn)
            UIManager.Instance._SetEquipmentUItoSleep(fEquipmentIdx.absoluteIdx);
        /// reactivate rec button
        UIManager.Instance.equipmentToolBar.transform.GetChild(fEquipmentIdx.absoluteIdx).GetChild(2).GetChild(0).gameObject.SetActive(true);
        UIManager.Instance.equipmentToolBar.transform.GetChild(fEquipmentIdx.absoluteIdx).gameObject.GetComponentInChildren<Slider>().gameObject.transform.GetChild(1).GetChild(0).GetComponent<Image>().color = Color.green;



    }
    /// OPTI : redondant checks
    public bool _ConsumeContainerCharge(EquipmentCategory equipmentCategory, FullEquipmentIdx fEquipementIdx, ushort containerIdx)
    {
        bool emptyedContainer = false;
        if (equipmentCategory == EquipmentCategory.Weapon)
        {
            emptyedContainer = synthFullBundleLists[fEquipementIdx.relativeIdx][containerIdx].Item2._ConsumeCharge();
            if (emptyedContainer)
                _RemoveCointainer(equipmentCategory, fEquipementIdx, containerIdx);
        }
        else
        {
            emptyedContainer = machineDrumFullBundleLists[fEquipementIdx.relativeIdx][containerIdx].Item2._ConsumeCharge();
            if (emptyedContainer)
                _RemoveCointainer(equipmentCategory, fEquipementIdx, containerIdx);
        }
        return emptyedContainer;
    }
    public void _RemoveCointainer(EquipmentCategory equipmentCategory, FullEquipmentIdx fEquipementIdx, ushort containerIdx)
    {
        if (equipmentCategory == EquipmentCategory.Weapon)
        {
            synthFullBundleLists[fEquipementIdx.relativeIdx][containerIdx].Item1.playbackAudioBundle.PlaybackKeys.Dispose();
            synthFullBundleLists[fEquipementIdx.relativeIdx][containerIdx].Item1.musicSheet._Dispose();
            Destroy(synthFullBundleLists[fEquipementIdx.relativeIdx][containerIdx].Item2.gameObject);
            synthFullBundleLists[fEquipementIdx.relativeIdx].RemoveAt(containerIdx);
        }
        else
        {
            machineDrumFullBundleLists[fEquipementIdx.relativeIdx][containerIdx].Item1.playbackAudioBundle.PlaybackPads.Dispose();
            machineDrumFullBundleLists[fEquipementIdx.relativeIdx][containerIdx].Item1.drumPadSheet._Dispose();
            Destroy(machineDrumFullBundleLists[fEquipementIdx.relativeIdx][containerIdx].Item2.gameObject);
            machineDrumFullBundleLists[fEquipementIdx.relativeIdx].RemoveAt(containerIdx);
        }
        PBholders[fEquipementIdx.absoluteIdx]._RearangeContainers();
    }


    public void _ArmPlaybackForActivation(int2 PBidx)
    {

    }



}
