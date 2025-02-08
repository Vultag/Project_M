using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class SynthPBitemButton : MonoBehaviour, IPointerClickHandler, IPointerExitHandler
{

    private PlaybackHolder playbackHolder;
    private PlaybackContainer playbackContainer;
    [HideInInspector]
    public short SynthIdx;
    private bool isShowingSheet;

    private void Start()
    {
        playbackHolder = this.transform.parent.parent.GetComponent<PlaybackHolder>();
        playbackContainer = this.transform.parent.GetComponent<PlaybackContainer>();
        this.transform.GetChild(0).GetComponent<Image>().color = playbackContainer.associatedItemColor;
        this.transform.GetChild(0).GetComponent<Image>().sprite = playbackContainer.associatedItemSprite;

    }
    public void OnPointerClick(PointerEventData pointerEventData)
    {
        if(pointerEventData.button == PointerEventData.InputButton.Left)
        {
            _ArmPlaybackItem(SynthIdx);
        }
        else
        {
            //Debug.Log(this.transform.parent.GetComponent<PlaybackContainer>().musicSheet.ElementsInMesure[1]);
            /// temporary set rmb to display music sheet
            playbackHolder.uiManager.MusicSheetGB.SetActive(true);
            AudioLayoutStorageHolder.audioLayoutStorage.ActiveMusicSheet = this.transform.parent.GetComponent<PlaybackContainer>().musicSheet;
            isShowingSheet = true;
        }

    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (isShowingSheet) playbackHolder.uiManager.MusicSheetGB.SetActive(false);
        isShowingSheet = false;
    }
    public void _ArmPlaybackItem(short synthIdx)
    {
        //playbackHolder.trackPlaybackItem.GetComponent<PlaybackItemUI>().playbackIdx = synthIdx;
        playbackHolder.trackPlaybackItem.GetComponent<Image>().color = playbackContainer.associatedItemColor;
        playbackHolder.trackPlaybackItem.GetComponent<TrackPlaybackItem>().associatedPlaybackContainer = playbackContainer;

        playbackHolder.trackPlaybackItem.SetActive(true);
    }

}
