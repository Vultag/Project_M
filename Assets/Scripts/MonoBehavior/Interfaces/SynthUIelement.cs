using System.Collections;
using System.Collections.Generic;
using MusicNamespace;
using TMPro;
using UnityEngine;

public class SynthUIelement : MonoBehaviour
{

    private UIManager uiManager;
    public TextMeshProUGUI startCountdown;

    /// <summary>
    /// the number of beat countdown before the playback start recording
    /// </summary>
    private short BeatBeforeRecordStart = 3;
    private float ContdownFontSize = 12;

    void Start()
    {
        uiManager = Object.FindAnyObjectByType<UIManager>();

    }

    public void _PrepairRecord()
    {
        StartCoroutine("RecordCountdown");
    }

    IEnumerator RecordCountdown()
    {
        startCountdown.gameObject.SetActive(true);
        startCountdown.color = Color.red;

        float remainingTime = (1 - (float)(MusicUtils.time) % (60f / MusicUtils.BPM)) + BeatBeforeRecordStart + Time.deltaTime;

        while ((remainingTime - Time.deltaTime) > 0)
        {
            remainingTime -= Time.deltaTime;

            if (remainingTime > BeatBeforeRecordStart)
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
        remainingTime -= Time.deltaTime;

        uiManager._RecordPlayback(-remainingTime);
        startCountdown.gameObject.SetActive(false);

        yield return null;
    }


}
