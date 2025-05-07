using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class PlaybackContainerUI : MonoBehaviour, IPointerClickHandler, IPointerExitHandler,IPointerEnterHandler
{

    public PlaybackHolder playbackHolder;
    private PlaybackContainer playbackContainer;
    private bool isShowingSheet;
    [HideInInspector]
    public int2 PBidx;
    [HideInInspector]
    public Sprite associatedSprite;
    [HideInInspector]
    public Color associatedColor;
    [SerializeField]
    private Image itemImage;

    private void Start()
    {
        //playbackHolder = this.transform.parent.GetComponent<PlaybackHolder>();
        playbackContainer = this.transform.parent.GetComponent<PlaybackContainer>();
        itemImage.color = associatedColor;
        itemImage.sprite = associatedSprite;

    }
    public void OnPointerClick(PointerEventData pointerEventData)
    {
        if(pointerEventData.button == PointerEventData.InputButton.Left)
        {
            //Debug.Log(PBidx.y);
            _ArmPlaybackItem(PBidx.y);
        }
        else
        {
            /// temporary set rmb to Quick add
            playbackHolder.trackPlaybackItem.transform.parent.parent.GetComponent<ItemRackUI>()._QuickPBque(PBidx);

        }

    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (isShowingSheet) UIManager.Instance.MusicSheetGB.SetActive(false);
        isShowingSheet = false;
    }
    public void _ArmPlaybackItem(int containerIdx)
    {
        //playbackHolder.trackPlaybackItem.GetComponent<PlaybackItemUI>().playbackIdx = synthIdx;
        playbackHolder.trackPlaybackItem.GetComponent<Image>().color = associatedColor;
        //playbackHolder.trackPlaybackItem.GetComponent<TrackPlaybackItem>().associatedPlaybackContainer = playbackContainer;

        //PBidx = new int2(PBidx.x, containerIdx);
        playbackHolder.trackPlaybackItem.transform.parent.parent.GetComponent<ItemRackUI>().ArmedPlaybacks[PBidx.x] = containerIdx;

        //playbackHolder.trackPlaybackItem.SetActive(true);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        /// reactivate it if PB is recording ?
        UIManager.Instance.MusicSheetGB.SetActive(true);
        AudioLayoutStorageHolder.audioLayoutStorage.ActiveMusicSheet = this.transform.parent.parent.GetComponent<UIPlaybacksHolder>().synthFullBundleLists[PBidx.x][PBidx.y].musicSheet;
        isShowingSheet = true;
    }
}
