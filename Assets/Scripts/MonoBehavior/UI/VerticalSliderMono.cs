using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.EventSystems;
using static UnityEngine.Rendering.DebugUI;

public class VerticalSliderMono : MonoBehaviour, IInitializePotentialDragHandler, IBeginDragHandler, IDragHandler, IEndDragHandler, IPointerEnterHandler, IPointerExitHandler, IPointerUpHandler, IPointerDownHandler
{

    private ADSRUI ADSRui;

    /// <summary>
    ///  0 = A ; 1 = D ; 2 = S ; 3 = R ;
    /// </summary>
    [SerializeField]
    short SliderIdx;
    float BackgroundSliderSize;

    private RectTransform Rtrans;
    //float2 limit;
    float target;

    [HideInInspector]
    public string displayedValue;
    private bool mouseInSlider;


    void Start()
    {
        ADSRui = this.transform.parent.parent.GetComponent<ADSRUI>();
        Rtrans = this.GetComponent<RectTransform>();
        BackgroundSliderSize = 46f;
        target = Rtrans.localPosition.y;
    }
    public void OnDrag(PointerEventData eventData)
    {
        //Rtrans.anchoredPosition += eventData.delta;
        Vector3 pos = Rtrans.localPosition;
        target += (eventData.delta.y / ADSRui.uiManager.canvas.scaleFactor);
        float lowerLim = (ADSRui.ThisADSRLimits[SliderIdx].x-0.5f)*BackgroundSliderSize;
        float upperLim = (ADSRui.ThisADSRLimits[SliderIdx].y-0.5f)*BackgroundSliderSize;
        //Debug.Log(eventData.position- eventData.pressPosition);
        //pos.y+ (eventData.delta.y/ADSRui.uiManager.canvas.scaleFactor)
        Rtrans.localPosition = new Vector3(pos.x, Mathf.Max(lowerLim, Mathf.Min(upperLim, target)), pos.z);

        /// Gives (0 to 1)
        var noramlizedSmiderPosY = (Rtrans.localPosition.y / BackgroundSliderSize) + 0.5f;
        displayedValue = ADSRui.UIADSRchange(SliderIdx, noramlizedSmiderPosY);


        ADSRui.uiManager.UpdateDisplayToolTip(new Vector2(
            transform.position.x + transform.GetComponent<RectTransform>().sizeDelta.x * 0.025f, transform.position.y + transform.GetComponent<RectTransform>().sizeDelta.y * 0.025f), 
            displayedValue);
    }

    public void OnPointerDown(PointerEventData eventData)
    {

    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        mouseInSlider = true;

        if (ADSRui.uiManager.toolTip.gameObject.activeSelf)
            return;

        ADSRui.uiManager.toolTip.gameObject.SetActive(true);

        ADSRui.uiManager.UpdateDisplayToolTip(new Vector2(transform.position.x + transform.GetComponent<RectTransform>().sizeDelta.x * 0.025f, transform.position.y + transform.GetComponent<RectTransform>().sizeDelta.y * 0.025f), displayedValue);

    }

    public void OnPointerExit(PointerEventData eventData)
    {
        mouseInSlider = false;
        if (!eventData.pointerPress)
            ADSRui.uiManager.toolTip.gameObject.SetActive(false);
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (mouseInSlider)
            return;
        ADSRui.uiManager.toolTip.gameObject.SetActive(false);
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        target = Rtrans.localPosition.y;
    }

    public void OnEndDrag(PointerEventData eventData){}

    public void OnInitializePotentialDrag(PointerEventData eventData)
    {
        eventData.useDragThreshold = false;
    }
}
