using System.Collections;
using MusicNamespace;
using TMPro;
using Unity.Entities;
using UnityEngine;

public enum EquipmentCategory
{
    Weapon,
    DrumMachine
}

public struct BuildingInfo
{
    public EquipmentCategory equipmentCategory;
    public WeaponType weaponType;
    public WeaponClass weaponClass;
    public short buildingIdx;
    public Entity WeaponAmmoPrefab;
    //...
}


public class EquipmentUIelement : MonoBehaviour
{

    private UIManager uiManager;
    public TextMeshProUGUI startCountdown;

    /// <summary>
    /// the number of beat countdown before the playback start recording
    /// </summary>
    private short BaseBeatBeforeSynthStart = 3;
    private float ContdownFontSize = 12;
    [HideInInspector]
    public ushort thisEquipmentIdx;
    [HideInInspector]
    public EquipmentCategory thisEquipmentCategory;
    [HideInInspector]
    ///public BuildingInfo thisBuildingInfo;
    private bool RecordPrepairing = false;
    public bool ActivationPrepairing = false;

    void Start()
    {
        uiManager = Object.FindAnyObjectByType<UIManager>();
        thisEquipmentIdx = (ushort)this.gameObject.transform.GetSiblingIndex();

        /// TEMP
        //thisBuildingInfo = new BuildingInfo
        //{
        //    weaponClass = WeaponClass.Ray,
        //    weaponType = WeaponType.Raygun,
        //    buildingIdx = thisEquipmentIdx,
        //    equipmentCategory = EquipmentCategory.Weapon,
        //};


    }

    public void _selectThisEquipment()
    {
        ///uiManager._SelectBuildingUI(thisEquipmentIdx, thisBuildingInfo);
        
        switch (thisEquipmentCategory)
        {
            case EquipmentCategory.Weapon:
                uiManager._SelectSynthUI(thisEquipmentIdx);
                break;
            case EquipmentCategory.DrumMachine:
                uiManager._SelectMachineDrumUI(thisEquipmentIdx);
                break;
        }
    }
    //public void _activateThisPlayback()
    //{
    //    uiManager._ActivateSynthPlayback(thisEquipmentIdx);
    //}

    public void _PrepairRecord()
    {
        if (uiManager.curentlyRecording)
            return;
        uiManager.curentlyRecording = true;
        uiManager._ResetPlayback(thisEquipmentIdx);
        /// Deactivate Rec button GB
        uiManager.equipmentToolBar.transform.GetChild(thisEquipmentIdx).GetChild(2).GetChild(0).gameObject.SetActive(false);
        ///// Deactivate Play button GB
        //uiManager.equipmentToolBar.transform.GetChild(thisSynthIdx).GetChild(2).GetChild(1).gameObject.SetActive(false);
        ///// Activate Stop button GB
        //uiManager.equipmentToolBar.transform.GetChild(thisSynthIdx).GetChild(2).GetChild(2).gameObject.SetActive(true);
        StartCoroutine(nameof(RecordCountdown), thisEquipmentIdx);
        RecordPrepairing = true;
    }
    public void _DisplayPrepairActivation()
    {
        StartCoroutine("ActivationCountdown");
    }
    public void _CancelActivation()
    {
        startCountdown.gameObject.SetActive(false);
        StopCoroutine("ActivationCountdown");
    }
    public void _CancelRecord()
    {
        startCountdown.gameObject.SetActive(false);
        StopCoroutine("RecordCountdown");
    }

    //public void _StopRecordOrPlayback() 
    //{
    //    if(RecordPrepairing)
    //    {
    //        StopCoroutine("RecordCountdown");
    //        RecordPrepairing = false;
    //        ///// Deactivate Rec button GB
    //        //uiManager.equipmentToolBar.transform.GetChild(thisSynthIdx).GetChild(2).GetChild(0).gameObject.SetActive(true);
    //        ///// Deactivate Play button GB
    //        //uiManager.equipmentToolBar.transform.GetChild(thisSynthIdx).GetChild(2).GetChild(1).gameObject.SetActive(true);
    //        ///// Activate Stop button GB
    //        //uiManager.equipmentToolBar.transform.GetChild(thisSynthIdx).GetChild(2).GetChild(2).gameObject.SetActive(false);
    //        startCountdown.gameObject.SetActive(false);
    //    }
    //    else
    //    {
    //        uiManager._StopSynthPlayback(thisSynthIdx);
    //    }
    //}

    /// <summary>
    /// cancel auto play for now
    /// </summary>
    public void _StopAutoPlay()
    {
        uiManager._StopAutoPlay(thisEquipmentIdx);
    }
    public void _StartAutoPlay()
    {
        if(!ActivationPrepairing)
        {
            uiManager._StartAutoPlay(thisEquipmentIdx);
            StartCoroutine(nameof(ActivationCountdown));
        }
        else
        {
            Debug.LogError("already ActivationPrepairing");
        }
    }

    IEnumerator RecordCountdown(ushort equipmentIdx)
    {
        startCountdown.gameObject.SetActive(true);
        startCountdown.color = Color.red;

        //int startingBeat = (int)(Mathf.Ceil((float)(MusicUtils.time))+ BaseBeatBeforeSynthStart);
        //float remainingTime = (1 - (float)(MusicUtils.time) % (60f / MusicUtils.BPM)) + BaseBeatBeforeSynthStart;

        int startingBeat = (int)(Mathf.Floor((float)(MusicUtils.time/4f))*4+ 4);
        float mesureProgress = (float)(MusicUtils.time) % ((60f / MusicUtils.BPM) * 4);
        //int BeatBeforeStart = BaseBeatBeforeSynthStart - (Mathf.FloorToInt(mesureProgress));
        float remainingTime = (4 - mesureProgress);// + BeatBeforeStart;

        while ((remainingTime - Time.deltaTime) > 0)
        {
            remainingTime -= Time.deltaTime;

            if (remainingTime > BaseBeatBeforeSynthStart)
            {
                startCountdown.text = "...";
            }
            else
            {
                float remainder = remainingTime % 1f;
                startCountdown.fontSize = ContdownFontSize-(1-remainder)*4;
                startCountdown.color = new Color(1,1- remainder, 1- remainder);
                startCountdown.text = Mathf.CeilToInt(remainingTime).ToString();
            }

            yield return new WaitForEndOfFrame();
        }
        //remainingTime -= Time.deltaTime;

        switch (thisEquipmentCategory)
        {
            case EquipmentCategory.Weapon:
                uiManager._RecordSynthPlayback(equipmentIdx, startingBeat);
                break;
            case EquipmentCategory.DrumMachine:
                uiManager._RecordDrumMachinePlayback(equipmentIdx, startingBeat);
                break;
        }

        startCountdown.gameObject.SetActive(false);
        ///RecordPrepairing = false;

        yield return null;
    }
    IEnumerator ActivationCountdown()
    {
        ActivationPrepairing = true;

        startCountdown.gameObject.SetActive(true);
        startCountdown.color = Color.green;

        //int startingBeat = (int)(Mathf.Ceil((float)(MusicUtils.time)) + BeatBeforeRecordStart);
        float mesureProgress = (float)(MusicUtils.time) % ((60f / MusicUtils.BPM)*4);
        //int BeatBeforeStart = BaseBeatBeforeSynthStart-(Mathf.FloorToInt(mesureProgress));
        float remainingTime = (4 - mesureProgress);// + BeatBeforeStart;

        while ((remainingTime - Time.deltaTime) > 0)
        {
            remainingTime -= Time.deltaTime;

            float remainder = remainingTime % 1f;
            startCountdown.fontSize = ContdownFontSize - (1 - remainder) * 4;
            startCountdown.color = new Color(1 - remainder, 1, 1 - remainder);
            startCountdown.text = Mathf.CeilToInt(remainingTime).ToString();
            

            yield return new WaitForEndOfFrame();
        }
        //remainingTime -= Time.deltaTime;

        //uiManager._ActivateSynthPlayback(PBidx);
        startCountdown.gameObject.SetActive(false);

        //Debug.Log("start");
        ActivationPrepairing = false;

        yield return null;
    }

}
