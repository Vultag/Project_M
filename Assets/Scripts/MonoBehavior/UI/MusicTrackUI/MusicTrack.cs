using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class MusicTrack : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IPointerDownHandler,IPointerUpHandler
{
    UIManager uiManager;
    [SerializeField]
    private RectTransform draggerObjectRect;
    [HideInInspector]
    public int2 draggedItemInitialCoords;
    private Vector2 draggedItemOriginPos;
    private RectTransform Rtrans;
    //public GameObject TrackPlaybackItemPrefab;


    [SerializeField]
    private Sprite[] SlotSprites;
    //[SerializeField]
    public PlaybackHolder[] PlaybackHolderArray;

    private void Start()
    {
        uiManager = Object.FindAnyObjectByType<UIManager>();
        Rtrans = this.GetComponent<RectTransform>();

    }

    public void OnPointerDown(PointerEventData eventData)
    {
     
        RectTransformUtility.ScreenPointToLocalPointInRectangle(Rtrans.parent as RectTransform, eventData.position, eventData.pressEventCamera, out Vector2 localPoint);
        var Cbelt = this.GetComponent<MusicTrackConveyorBelt>();
        var pointDownCoords = Cbelt.GetCoordsOnTrack(localPoint);
        if (pointDownCoords.y==0) return;
        //Debug.LogError(pointDownCoords);
        if (Cbelt.isSlotFree(pointDownCoords))
        {
            draggedItemInitialCoords = pointDownCoords;
            draggerObjectRect.GetComponent<Image>().sprite = SlotSprites[pointDownCoords.x];
            draggerObjectRect.gameObject.SetActive(true);
            draggedItemOriginPos = draggerObjectRect.localPosition;
            draggerObjectRect.localPosition = new Vector3(localPoint.x, localPoint.y, Rtrans.localPosition.z);
        }
        //draggedObjectRect = eventData.pointerCurrentRaycast.gameObject.GetComponent<RectTransform>();
        //draggedObjectInitialCoords = this.GetComponent<MusicTrackConveyorBelt>().GetCoordsOnTrack(draggedObjectRect.localPosition);
    }

   

    public void OnDrag(PointerEventData eventData)
    {
        //if (draggedObjectRect != null)
        {

            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                Rtrans.parent as RectTransform, // Reference to the parent canvas
                eventData.position, // Mouse screen position
                uiManager.canvas.worldCamera, // The camera rendering the UI
                out Vector2 localPoint // The output local position
            );
            draggerObjectRect.localPosition = new Vector3(localPoint.x, localPoint.y, Rtrans.localPosition.z);
        }

    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (!draggerObjectRect.gameObject.activeSelf) return;
        bool insertSuccess = false;
        RectTransform targetRect = this.GetComponent<RectTransform>();
        if (RectTransformUtility.RectangleContainsScreenPoint(targetRect, eventData.position, eventData.pressEventCamera))
        {
            var localPoint = Vector2.zero;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(targetRect, eventData.position, eventData.pressEventCamera, out localPoint);
            insertSuccess = this.GetComponent<MusicTrackConveyorBelt>()._TryMoveTrackElement(
                localPoint,
                //draggedObjectRect.gameObject,
                draggedItemInitialCoords 
                );

        }

        //draggedObjectRect.localPosition = insertSuccess? draggedObjectRect.localPosition : draggedObjectOriginPos;
        draggerObjectRect.gameObject.SetActive(false);
    }

    public void ForceDrop()
    {
        draggerObjectRect.gameObject.SetActive(false);
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        //bool insertSuccess = false;
        //RectTransform targetRect = this.GetComponent<RectTransform>();
        //if (RectTransformUtility.RectangleContainsScreenPoint(targetRect, eventData.position, eventData.pressEventCamera))
        //{
        //    var localPoint = Vector2.zero;
        //    RectTransformUtility.ScreenPointToLocalPointInRectangle(targetRect, eventData.position, eventData.pressEventCamera, out localPoint);
        //    insertSuccess = this.GetComponent<MusicTrackConveyorBelt>()._TryMoveTrackElement(
        //        localPoint,
        //        //draggedObjectRect.gameObject,
        //        draggedObjectInitialCoords);

        //}

        ////draggedObjectRect.localPosition = insertSuccess? draggedObjectRect.localPosition : draggedObjectOriginPos;
        //draggerObjectRect.gameObject.SetActive(false);
    }
    //public void _AddPlaybackItem(Vector2 dropCoord)
    //{
    //    Debug.Log(dropCoord);
    //}

    public void OnBeginDrag(PointerEventData eventData)
    { }

   
}
