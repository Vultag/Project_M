using System.Collections;
using System.Collections.Generic;
using MusicNamespace;
using TMPro;
using Unity.Mathematics;
using UnityEngine;

public class SynthUIelement : MonoBehaviour
{

    private UIManager uiManager;
    public TextMeshProUGUI startCountdown;

    /// <summary>
    /// the number of beat countdown before the playback start recording
    /// </summary>
    private short BaseBeatBeforeSynthStart = 3;
    private float ContdownFontSize = 12;
    [HideInInspector]
    public int thisSynthIdx;
    private bool RecordPrepairing = false;

    void Start()
    {
        uiManager = Object.FindAnyObjectByType<UIManager>();
        thisSynthIdx = this.gameObject.transform.GetSiblingIndex();
        //Debug.Log(thisSynthIdx);
    }

    public void _selectThisSynth()
    {
        uiManager._SelectSynthUI(thisSynthIdx);
    }
    public void _activateThisPlayback()
    {
        uiManager._ActivatePlayback(thisSynthIdx);
    }

    public void _PrepairRecord()
    {
        uiManager._ResetPlayback(thisSynthIdx);
        /// Deactivate Rec button GB
        uiManager.SynthToolBar.transform.GetChild(thisSynthIdx).GetChild(2).GetChild(0).gameObject.SetActive(false);
        ///// Deactivate Play button GB
        //uiManager.SynthToolBar.transform.GetChild(thisSynthIdx).GetChild(2).GetChild(1).gameObject.SetActive(false);
        ///// Activate Stop button GB
        //uiManager.SynthToolBar.transform.GetChild(thisSynthIdx).GetChild(2).GetChild(2).gameObject.SetActive(true);
        StartCoroutine("RecordCountdown", thisSynthIdx);
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
    //        //uiManager.SynthToolBar.transform.GetChild(thisSynthIdx).GetChild(2).GetChild(0).gameObject.SetActive(true);
    //        ///// Deactivate Play button GB
    //        //uiManager.SynthToolBar.transform.GetChild(thisSynthIdx).GetChild(2).GetChild(1).gameObject.SetActive(true);
    //        ///// Activate Stop button GB
    //        //uiManager.SynthToolBar.transform.GetChild(thisSynthIdx).GetChild(2).GetChild(2).gameObject.SetActive(false);
    //        startCountdown.gameObject.SetActive(false);
    //    }
    //    else
    //    {
    //        uiManager._StopPlayback(thisSynthIdx);
    //    }
    //}

    IEnumerator RecordCountdown(int synthIdx)
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

        uiManager._RecordPlayback(synthIdx, startingBeat);
        startCountdown.gameObject.SetActive(false);
        RecordPrepairing = false;

        yield return null;
    }
    IEnumerator ActivationCountdown()
    {
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

        //uiManager._ActivatePlayback(PBidx);
        startCountdown.gameObject.SetActive(false);

        //Debug.Log("start");

        yield return null;
    }

}
