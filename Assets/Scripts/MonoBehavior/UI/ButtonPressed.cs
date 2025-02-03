using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UIElements;


/// <summary>
///  REMPLACE WITH UNIVERSAL BUTTON
/// </summary>

public class ButtonPressed : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
{
    bool b_pressed;
    [SerializeField]
    MetronomeMono metronom;
    [SerializeField]
    int valuechange;

    public void OnPointerDown(PointerEventData eventData)
    {
        b_pressed = true;   
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        b_pressed = false;
    }

    private void FixedUpdate()
    {
        if (b_pressed)
        {
            metronom.ChangeTempo(valuechange);
        }
    }

}
