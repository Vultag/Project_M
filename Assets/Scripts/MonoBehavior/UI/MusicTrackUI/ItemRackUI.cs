using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class ItemRackUI : MonoBehaviour, IDragHandler, IEndDragHandler, IPointerDownHandler
{
    UIManager uiManager;
    private RectTransform draggedObject;
    [HideInInspector]
    public short playbackIdx;

    [SerializeField]
    private GameObject musicTrackGB;

    private void Start()
    {
        uiManager = Object.FindAnyObjectByType<UIManager>();
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        draggedObject = eventData.pointerCurrentRaycast.gameObject.GetComponent<RectTransform>();
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
                localPoint
                //draggedObject.GetComponent<TrackPlaybackItem>().associatedPlaybackContainer
                );
        }
        draggedObject.localPosition = new Vector3(0, 0, -10);
        /// DO insertSuccess
        draggedObject = null;
    }

}
