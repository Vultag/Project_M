using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class PlaybackItemUI : MonoBehaviour, IDragHandler, IEndDragHandler
{
    UIManager uiManager;
    private RectTransform Rtrans;
    [HideInInspector]
    public short playbackIdx;

    private void Start()
    {
        uiManager = Object.FindAnyObjectByType<UIManager>();
        Rtrans = this.GetComponent<RectTransform>();
    }

    public void OnDrag(PointerEventData eventData)
    {

        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            Rtrans.parent as RectTransform, // Reference to the parent canvas
            eventData.position, // Mouse screen position
            uiManager.canvas.worldCamera, // The camera rendering the UI
            out Vector2 localPoint // The output local position
        );
        Rtrans.localPosition = localPoint;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        throw new System.NotImplementedException();
    }


}
