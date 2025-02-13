using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.EventSystems;

public class ItemRackUI : MonoBehaviour, IDragHandler, IEndDragHandler, IPointerDownHandler
{
    UIManager uiManager;
    private RectTransform draggedObject;
    //[HideInInspector]
    //public short playbackIdx;

    [SerializeField]
    private float rackWidth;
    [SerializeField]
    private float rackItemWidth;
    private int2 draggedPBidx;

    [HideInInspector]
    public int[] ArmedPlaybacks = new int[6];


    [SerializeField]
    private GameObject musicTrackGB;

    private void Start()
    {
        uiManager = Object.FindAnyObjectByType<UIManager>();
    }

    public void _QuickPBque(int2 PBidx)
    {
        float trackHeight = musicTrackGB.GetComponent<RectTransform>().rect.height;
        var Cbelt = musicTrackGB.GetComponent<MusicTrackConveyorBelt>();
        for (int landingYcoord = 1; landingYcoord < 10; landingYcoord++)
        {
            bool insertSucess = Cbelt._TryInsertTrackElement(
               landingYcoord*(trackHeight/10f)- trackHeight*0.5f,
               PBidx
               );
            if (insertSucess) { break; }
        }

    }

    public void OnPointerDown(PointerEventData eventData)
    {
        draggedObject = eventData.pointerCurrentRaycast.gameObject.GetComponent<RectTransform>();
        RectTransformUtility.ScreenPointToLocalPointInRectangle(draggedObject.transform.parent.parent as RectTransform, eventData.position, eventData.pressEventCamera, out Vector2 localPoint);

        int PBxIdx = Mathf.FloorToInt((localPoint.x + (rackWidth * 0.5f) + (rackItemWidth * 0.5f)) / (rackWidth + rackItemWidth) * 6);
        //Debug.LogError((rackWidth + rackItemWidth));
        draggedPBidx = new int2 (PBxIdx,ArmedPlaybacks[PBxIdx]);
    }

    public void OnDrag(PointerEventData eventData)
    {
        RectTransformUtility.ScreenPointToLocalPointInRectangle(draggedObject.transform.parent as RectTransform, eventData.position, eventData.pressEventCamera, out Vector2 localPoint);
        draggedObject.localPosition = new Vector3(localPoint.x, localPoint.y, -10);
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        RectTransform targetRect = musicTrackGB.GetComponent<RectTransform>();
        if (RectTransformUtility.RectangleContainsScreenPoint(targetRect, eventData.position, eventData.pressEventCamera))
        {
            var localPoint = Vector2.zero;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(targetRect, eventData.position, eventData.pressEventCamera, out localPoint);
            musicTrackGB.GetComponent<MusicTrackConveyorBelt>()._TryInsertTrackElement(
                localPoint.y,
                draggedPBidx
                //draggedObject.GetComponent<TrackPlaybackItem>().associatedPlaybackContainer
                );
        }
        draggedObject.localPosition = new Vector3(0, 0, -10);
        /// DO insertSuccess
        draggedObject = null;
    }

}
