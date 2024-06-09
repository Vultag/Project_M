using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class SliderMono : MonoBehaviour, IBeginDragHandler, IEndDragHandler, IDragHandler
{
    [SerializeField]
    SpriteRenderer parameterA;
    [SerializeField]
    SpriteRenderer parameterB;
    [SerializeField]
    SpriteRenderer parameterC;

    private float Ascale;
    private float Bscale;
    private float Cscale;

    //Canvas canvas;
    private EntityManager entityManager;

    private RectTransform rectTrans;

    private float shapeRadius;

    //[HideInInspector]
    //public Vector3 waveShape;
    [HideInInspector]
    public AudioGenerator CurrentSynth;

    private void Start()
    {
        entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;
        Ascale = parameterA.transform.localScale.x;
        Bscale = parameterB.transform.localScale.x;
        Cscale = parameterC.transform.localScale.x;

    }

    private void Awake()
    {
        rectTrans = GetComponent<RectTransform>();
        shapeRadius = 2.5f;
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        //not used?
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        //not used?
    }

    public void OnDrag(PointerEventData eventData)
    {

        RectTransformUtility.ScreenPointToWorldPointInRectangle(this.transform as RectTransform,eventData.position,Camera.main, out var newpos);

        Vector3 distance = (newpos - transform.parent.position);
        float radDir = PhysicsUtilities.DirectionToRadians(distance.normalized);
        //float radDir = Mathf.Atan(distance.normalized.x, distance.normalized.y);
        //Debug.LogError(radDir);

       float sideLenght = 2 * Mathf.Sin(Mathf.PI/3);

        //float shapeRadius = 

        //float radDir = Mathf.Atan2(direction.x, direction.y);

        //Debug.Log(newpos.normalized);
        //Debug.Log(eventData.position.normalized);

        //Case Triangle 
        //TO DO : ADD MORE POTENTIAL CASES (SQUARE | PENTAGONE | HEXAGONE)

        //change in other cases
        float minInterval = Mathf.PI * 2 / 3;
        float posCutoffMaxFactor = Mathf.Sin(Mathf.PI/6);



        //Debug.Log(posCutoffFactor);


        //Debug.Log(Mathf.Sin(radDir)*0.5f);


        //float circleA = Mathf.Clamp(minInterval - (Mathf.Max(radDir, Mathf.PI / 3) - Mathf.Min(radDir, Mathf.PI / 3)), 0, minInterval) / minInterval;
        //float circleC = Mathf.Clamp(minInterval - (Mathf.Max(radDir, -Mathf.PI / 3) - Mathf.Min(radDir, -Mathf.PI / 3)), 0, minInterval) / minInterval;

        //Debug.Log(( Mathf.Sin(radDir)/Mathf.Sin(Mathf.PI / 3)) / 2 + 0.5f);

        //float A = Mathf.Clamp((Mathf.Cos(radDir-(Mathf.PI/3)/2))-0.5f,0,1f);
        //float A = ((Mathf.Tan(radDir)/(sideLenght))*0.5f)+0.5f;
        //float A = ((Mathf.Abs(Mathf.Cos(radDir)))-1f)*(-1f)+0.5f;

        //float A = (0.5f / Mathf.Cos(radDir));
        //float A = (((Mathf.Tan(radDir)*0.5f)/Mathf.Sin((Mathf.PI*2)/3))+1f)/2f;
        //float A = ((Mathf.Tan(radDir) *0.5f) / Mathf.Sin((Mathf.PI * 2) / 3));

        //float A = (Mathf.Clamp(Mathf.Cos(radDir) - 0.5f,0,1f) * 2;


        //float C = circleC - Mathf.Sin((circleA - 0.5f) * 2 * Mathf.PI) * (1 - Mathf.PI / 4);
        //float B = Mathf.Clamp(minInterval - ((Mathf.Max(radDir, Mathf.Sign(radDir) * Mathf.PI) - Mathf.Min(radDir, Mathf.Sign(radDir) * Mathf.PI))), 0, minInterval) / minInterval;
        //float B = 1 - (A+C);


        float maxdir = 0.5f / Mathf.Cos(((Mathf.Abs(radDir) + Mathf.PI / 3) % (Mathf.PI * 2 / 3) - (Mathf.PI / 3)));

        float lenght = Mathf.Clamp(distance.magnitude, 0f, shapeRadius* maxdir) / (shapeRadius * maxdir);

        // tentative full rotation -> trop lent?

        float A = Mathf.Clamp(maxdir - ((Mathf.Abs(radDir - Mathf.PI / 3) >= Mathf.PI / 3f) ? 1f : 0f) * ((Mathf.Abs(0.5f / (Mathf.Cos(((PhysicsUtilities.DirectionToRadians(distance.normalized, Mathf.PI / 3) + (Mathf.PI / 3)) % (Mathf.PI * 2 / 3)))))-0.5f)*2f),0f,1f);
        A = A * lenght + ((1f/3f) * (1-lenght));
        float B = Mathf.Clamp(maxdir - ((Mathf.Abs(radDir + Mathf.PI / 3) >= Mathf.PI / 3f) ? 1f : 0f) * ((Mathf.Abs(0.5f / (Mathf.Cos(((PhysicsUtilities.DirectionToRadians(distance.normalized, -Mathf.PI / 3) + (Mathf.PI / 3)) % (Mathf.PI * 2 / 3))))) - 0.5f) * 2f), 0f, 1f);
        B = B * lenght + ((1f/3f) * (1-lenght));
        float C = 1 - (A + B);

        //Debug.Log(A);
        //Debug.LogWarning(B);
        //Debug.LogError(C);

        //float temp = (0.5f / (Mathf.Cos(((PhysicsUtilities.DirectionToRadians(distance.normalized, Mathf.PI / 3) + (Mathf.PI / 3)*sign) % (Mathf.PI * 2 / 3)) - (Mathf.PI / 3) * sign)));
        float temp = Mathf.Abs(0.5f / (Mathf.Cos(((PhysicsUtilities.DirectionToRadians(distance.normalized, -Mathf.PI / 3) + (Mathf.PI / 3)) % (Mathf.PI * 2 / 3)))));
        //float temp = 0.5f/(Mathf.Cos(((radDir - (Mathf.PI / 3)) % (Mathf.PI * 2 / 3))+ (Mathf.PI / 3)));
        //float temp = 0.5f / (Mathf.Cos(((radDir + (Mathf.PI / 3)) % (Mathf.PI*2  / 3)) - (Mathf.PI / 3)));
        //Debug.Log(B);

        /* tentative distance
        float A = Vector2.Distance(new Vector2(distance.normalized.x, distance.normalized.y) * maxdir, new Vector2(Mathf.Sin((Mathf.PI * 2) / 3), -0.5f)) / ((Mathf.Sin(Mathf.PI * 2 / 3) * 2f));
        //Debug.LogError(new Vector2(Mathf.Sin((Mathf.PI * 2) / 3), -0.5f));
        Debug.Log(A);
        */

        float pole = Mathf.Max(A,B,C);

        float posCutoffFactor = maxdir;
        //remplace with max() of poles?

        //float posCutoffFactor = 0.5f / Mathf.Cos(((Mathf.Abs(radDir) + Mathf.PI / 3) % (Mathf.PI * 2 / 3) - ( Mathf.PI / 3)));



        if (distance.magnitude < shapeRadius * posCutoffFactor)
        {
            transform.position = transform.parent.position + distance;
        }
        else
        {
            transform.position = transform.parent.position + (distance.normalized * shapeRadius * posCutoffFactor);
        }

        //Debug.Log(Ascale);

        parameterA.gameObject.transform.localScale = new Vector3(Ascale * 0.7f + Ascale * (A * 0.3f), Ascale * 0.7f + Ascale * (A * 0.3f), 0);
        parameterB.gameObject.transform.localScale = new Vector3(Bscale * 0.7f + Bscale * (B * 0.3f), Bscale * 0.7f + Bscale * (B * 0.3f), 0);
        parameterC.gameObject.transform.localScale = new Vector3(Cscale * 0.7f + Cscale * (C * 0.3f), Cscale * 0.7f + Cscale * (C * 0.3f), 0);

        parameterA.color = new Color(1 - A, A, 0);
        parameterB.color = new Color(1 - B, B, 0);
        parameterC.color = new Color(1 - C, C, 0);



        SynthData newsynth = entityManager.GetComponentData<SynthData>(CurrentSynth.WeaponSynthEntity);

        newsynth.SinFactor = A;
        newsynth.SquareFactor = B;
        newsynth.SawFactor = C;

        entityManager.SetComponentData<SynthData>(CurrentSynth.WeaponSynthEntity, newsynth);

        //Debug.Log(A);
        //Debug.Log(B);
        //Debug.Log(C);
        //Debug.LogError(pole);
        //Debug.LogError(posCutoffMaxFactor);


        //float posCutoffFactor = Mathf.Lerp(posCutoffMaxFactor, 1f, ((pole-0.5f)*2));




    }



}