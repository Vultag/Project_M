using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class FPScounter : MonoBehaviour
{
    private TextMeshProUGUI FPScounterText;
    private float updatePerSec = 5;
    float updateTime;

    void Start()
    {
        FPScounterText = this.gameObject.GetComponent<TextMeshProUGUI>();
    }

    private void Update()
    {
        updateTime += Time.deltaTime;
        if (updateTime > (1 / updatePerSec))
        {
            FPScounterText.text = (Mathf.RoundToInt(1f / Time.deltaTime)).ToString() + " FPS";
            updateTime = 0;
        }
    }

}
