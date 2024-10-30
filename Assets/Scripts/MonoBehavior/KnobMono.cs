using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Mathematics;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.EventSystems;

public enum KnobChangeType
{
    OCSmix,
    OCS1fine,
    OCS2fine,
    OCS2semi,
    OCS1PW,
    OCS2PW,
    FilterCutoff,
    FilterRes,
    FilterEnv,
    UnissonVoices,
    UnissonDetune,
    UnissonSpread,
}

public class KnobMono : MonoBehaviour, IInitializePotentialDragHandler, IDragHandler,IPointerEnterHandler,IPointerExitHandler,IPointerUpHandler,IPointerDownHandler
{

    //private OscillatorUI oscillatorUI;

    [SerializeField]
    private MonoBehaviour knobControllerComponent;

    private IKnobController knobControllerTarget;

    [SerializeField]
    private KnobChangeType knobChangeType;
    /// <summary>
    /// Amount of (Stops ceiled to even num)(odd when counting 0) along the knob ??
    /// 0 = FREE
    /// </summary>
    [SerializeField]
    private int knobIncrementNum;
    /// Store the accumulated move for non free knob
    private float knobAccumulator; 
    [HideInInspector]
    public string displayedValue;

    [SerializeField]
    private bool Centered = false;

    private bool mouseInKnob;

    private float turnSpeed;

    private EntityManager entityManager;

    void Start()
    {
        knobIncrementNum = knobIncrementNum == 1 ? knobIncrementNum + 1 : knobIncrementNum;
        if (Centered && knobIncrementNum != 0)
            knobIncrementNum = knobIncrementNum % 2 == 0 ? knobIncrementNum + 1 : knobIncrementNum;
        
        turnSpeed = 2f;
        entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;
        knobControllerTarget = knobControllerComponent as IKnobController;

        float iconRot = this.transform.eulerAngles.z - (180f * (Mathf.Sign(this.transform.eulerAngles.z - 180f) + 1));
        displayedValue = knobControllerTarget.UIknobChange(knobChangeType, iconRot);


    }


    public void OnDrag(PointerEventData eventData)
    {
        float iconRot = this.transform.eulerAngles.z - (180f * (Mathf.Sign(this.transform.eulerAngles.z - 180f) + 1));
        
        float newRot = 0;
        if (knobIncrementNum != 0)
        {
            iconRot = Mathf.Abs(iconRot - 145);
            knobAccumulator += (eventData.delta.y / knobControllerTarget.uiManager.canvas.scaleFactor) * turnSpeed;
            if (Mathf.Abs(knobAccumulator) <= 145 / (knobIncrementNum + 1f))
            {
                return;
            }
            while (Mathf.Abs(knobAccumulator)>145/ (knobIncrementNum + 1))
            {
                float incrementState = Mathf.Clamp(Mathf.Round(iconRot/(290/ (knobIncrementNum- 1f))) + Mathf.Sign(knobAccumulator),0, knobIncrementNum- 1f);
                newRot = -incrementState * (290 / (knobIncrementNum- 1f)) + 145;
                knobAccumulator -= (145 / (knobIncrementNum + 1f))* Mathf.Sign(knobAccumulator);

                //Debug.Log(incrementState);
            }
        }
        else
            newRot = Mathf.Max(-145f, Mathf.Min(145f, iconRot - (eventData.delta.y / knobControllerTarget.uiManager.canvas.scaleFactor)* turnSpeed));
        this.transform.rotation = Quaternion.Euler(0, 0, newRot);

        displayedValue = knobControllerTarget.UIknobChange(knobChangeType,newRot);

        knobControllerTarget.uiManager.UpdateDisplayToolTip(new Vector2(transform.position.x+transform.GetComponent<RectTransform>().sizeDelta.x * 0.025f, transform.position.y+ transform.GetComponent<RectTransform>().sizeDelta.y * 0.025f), displayedValue);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {

        mouseInKnob = true;

        if (knobControllerTarget.uiManager.toolTip.gameObject.activeSelf)
            return;

        knobControllerTarget.uiManager.toolTip.gameObject.SetActive(true);

        //Debug.Log(transform.GetComponent<RectTransform>().sizeDelta.x * 0.025f);
        knobControllerTarget.uiManager.UpdateDisplayToolTip(new Vector2(transform.position.x + transform.GetComponent<RectTransform>().sizeDelta.x * 0.025f, transform.position.y + transform.GetComponent<RectTransform>().sizeDelta.y * 0.025f), displayedValue);
    
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        mouseInKnob = false;
        if(!eventData.pointerPress)
            knobControllerTarget.uiManager.toolTip.gameObject.SetActive(false);
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (mouseInKnob)
            return;
        knobControllerTarget.uiManager.toolTip.gameObject.SetActive(false);
    }
    /// Required for OnPointerUp to work
    public void OnPointerDown(PointerEventData eventData){}
    public void OnInitializePotentialDrag(PointerEventData eventData)
    {
        eventData.useDragThreshold = false;
    }
}
