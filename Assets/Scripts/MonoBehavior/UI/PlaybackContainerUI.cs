
using TMPro;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;


/// <summary>
///  SERVES LOW PURPESE. REMOVE AND DO IT ALL IN PLAYBACKHOLDER FOR BETTER ORGANIZATION ?
///  
///  OR
///  
///  REORGANIZE AND PUT STUFF INSIDE 
/// 
/// </summary>
public class PlaybackContainerUI : MonoBehaviour, IPointerClickHandler, IPointerExitHandler,IPointerEnterHandler
{
    public PlaybackHolder playbackHolder;
    private bool isShowingSheet;
    [HideInInspector]
    public int2 PBidx;
    [HideInInspector]
    public ushort containerCharges;

    [HideInInspector]
    public ushort relativeEquipmentIdx;

    [HideInInspector]
    public Sprite associatedSprite;
    [HideInInspector]
    public Color associatedColor;
    [SerializeField]
    private Image itemImage;
    [SerializeField]
    private TextMeshProUGUI ContainerNumText;

    private void Start()
    {
        //playbackHolder = this.transform.parent.GetComponent<PlaybackHolder>();
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
    public bool _ConsumeCharge()
    {
        containerCharges--;
        ContainerNumText.text = containerCharges.ToString();
        var isEmpty = containerCharges == 0;
        if (isEmpty)
        {
            playbackHolder.ContainerNumber--;
        }
        return isEmpty;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if(playbackHolder.equipmentCategory == EquipmentCategory.Weapon)
        {
            AudioLayoutStorageHolder.audioLayoutStorage.ActiveMusicSheet = this.transform.parent.parent.GetComponent<UIPlaybacksHolder>().synthFullBundleLists[relativeEquipmentIdx][PBidx.y].Item1.musicSheet;
            UIManager.Instance.MusicSheetGB.GetComponent<MusicSheetToShader>().enabled = true;
            UIManager.Instance.MusicSheetGB.GetComponent<DrumPadSheetToShader>().enabled = false;
        }
        else
        {
            AudioLayoutStorageHolder.audioLayoutStorage.ActiveDrumPadSheetData = this.transform.parent.parent.GetComponent<UIPlaybacksHolder>().machineDrumFullBundleLists[relativeEquipmentIdx][PBidx.y].Item1.drumPadSheet;
            UIManager.Instance.MusicSheetGB.GetComponent<DrumPadSheetToShader>().enabled = true;
            UIManager.Instance.MusicSheetGB.GetComponent<MusicSheetToShader>().enabled = false;
        }
        UIManager.Instance.MusicSheetGB.SetActive(true);
        isShowingSheet = true;
    }
}
