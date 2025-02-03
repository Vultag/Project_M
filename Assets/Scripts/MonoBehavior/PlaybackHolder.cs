using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.Collections;
using UnityEngine;
using UnityEngine.UI;

public class PlaybackHolder : MonoBehaviour
{
    [SerializeField]
    GameObject PBcontainerPrefab; 
    public GameObject PBsynthItem;
    [HideInInspector]
    public UIManager uiManager;

    private int ContainerNumber = 0;

    void Start()
    {
        uiManager = Object.FindAnyObjectByType<UIManager>();
    }
    public void _AddContainer(PlaybackAudioBundle newAudioBundle,MusicSheetData newSheetData)
    {
        uiManager.MusicSheetGB.SetActive(false);
        _RearangeContainerForAdd();
        var PBcontainerInstance = Instantiate(PBcontainerPrefab, this.GetComponent<RectTransform>());
        PBcontainerInstance.GetComponent<RectTransform>().anchoredPosition = new Vector2(14.5f* ContainerNumber, -11.8f);
        var container = PBcontainerInstance.GetComponent<PlaybackContainer>();
        container.playbackAudioBundle = newAudioBundle;
        container.musicSheet = newSheetData;
        /// arbitrary 3 -> set number of remaining playback use
        container.GetComponentInChildren<TextMeshProUGUI>().text = "3";
        container.associatedItemColor = Random.ColorHSV();
        // do container color here
        //container.GetComponentInChildren<SynthPBitemButton>().gameObject.transform.GetComponentInChildren<Image>().color = container.associatedItemColor;
        container.associatedPlaybackItem = PBsynthItem;

        ContainerNumber++;
    }


    public void _QuickQuePlaybackItem(short synthIdx)
    {
        // to do
    }


    private void _RearangeContainerForAdd()
    {
        foreach (Transform sibling in transform)
        {
            // Skip any object with TextMeshProUGUI
            if (sibling.GetComponent<TextMeshProUGUI>() != null)
                continue;

            RectTransform rectTransform = sibling as RectTransform;
            if (rectTransform != null)
            {
                rectTransform.anchoredPosition += new Vector2(-14.5f, 0);
            }
        }
    }


}
