using System.Collections;
using System.Collections.Generic;
using System.Numerics;
using MusicNamespace;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.UI;
using Vector2 = UnityEngine.Vector2;
using Vector3 = UnityEngine.Vector3;
using Vector4 = UnityEngine.Vector4;

public class MusicTrackConveyorBelt : MonoBehaviour
{
    RectTransform content; // The parent holding UI elements
    //float speed = 100f;    // Speed of movement
    float beltLimit = -500f; // X position to reset elements
    float startPosition = 500f; // X position to reposition elements

    private int ThreadElementNum = 10;
    private int ThreadNum = 6;

    private float currentMesureOffset = 0f;
    private float currentTrackOffset = 0f;

    float mesureElementSpacing;
    //private bool[,] TrackSlotsGridOBS;
    int mesurePassed;

    /// <summary>
    /// Flattened 2D array to pass directly to the shader
    /// Only the first two values are used but it's till better than to
    /// have to switch to a structured buffer considering the data is small
    /// </summary>
    private Vector4[] TrackSlotsGrid;
    private Material TrackMaterial;

    private void Start()
    {
        content = this.GetComponent<RectTransform>();
        mesureElementSpacing = (0.5f / (ThreadElementNum));
        //TrackSlotsGridOBS = new bool[2,ThreadElementNum+1];
        TrackSlotsGrid = new Vector4[10*6];
        mesurePassed = 0;
        for (int i = 0; i < TrackSlotsGrid.Length; i++)
        {
            // 99->empty slot
            TrackSlotsGrid[i] = new Vector4(99, 99, 0, 0);
        }
        TrackMaterial = this.GetComponent<Image>().material;

    }

    void Update()
    {
        currentMesureOffset = (MusicUtils.time % 4f) * 0.25f;
        currentTrackOffset = (MusicUtils.time / (ThreadElementNum * 4f));
        //(0.5f/(ThreadElementNum))*index

        if(MusicUtils.time*0.25f>mesurePassed)
        {
            ProcessRow();
            mesurePassed++;
        }
        //DisplaceTrackElements(currentMesureOffset* (content.rect.height/ ThreadElementNum));

        //TrackSlotsGrid[33] = new Vector4(1, 1, 0, 0);
        //TrackSlotsGrid[11] = new Vector4(1, 0, 0, 0);

        TrackMaterial.SetVectorArray("TrackGridArray", TrackSlotsGrid);

    }

    public bool _TryInsertTrackElement(Vector2 localPoint)
    {
        Vector2 normalizedPoint = new Vector2(localPoint.x/content.rect.width, (localPoint.y / content.rect.height + 0.5f));
        //Debug.Log(normalizedPoint.y);
        int Xindex = 0;
        int Yindex = Mathf.FloorToInt(normalizedPoint.y*ThreadElementNum);
        int flatenedIdx = Xindex + Yindex * 6;

        bool isGridSlotFree = TrackSlotsGrid[flatenedIdx].x==99;
        //Debug.LogWarning(Yindex);
        if (isGridSlotFree)
        {
            //var element = Instantiate(this.GetComponent<MusicTrack>().TrackPlaybackItemPrefab, this.transform);
            var assignedPos = new Vector3(
             Xindex * content.rect.width,
             ((Yindex * 2 + 1) * mesureElementSpacing - 0.5f) * content.rect.height,
                -10);


            //element.GetComponent<RectTransform>().localPosition = assignedPos;
            //element.GetComponent<TrackPlaybackItem>().associatedPlaybackContainer = container;
            //element.GetComponent<TrackPlaybackItem>().assignedPosition = assignedPos;



            TrackSlotsGrid[flatenedIdx] = new Vector4(0,0,0,0);
        }
        else
        {
            Debug.Log("occupado");
        }
        return isGridSlotFree;
    }
    public bool _TryMoveTrackElement(Vector2 localPoint, int2 InitialCoords)
    {
        Vector2 normalizedPoint = new Vector2(localPoint.x / content.rect.width, (localPoint.y / content.rect.height + 0.5f));
        int Xindex = 0;
        int Yindex = Mathf.FloorToInt(normalizedPoint.y * ThreadElementNum);
        int flatenedIdx = Xindex + Yindex * 6;

        //Debug.Log(InitialCoords);

        bool isGridSlotFree = TrackSlotsGrid[flatenedIdx].x==99;
        if (isGridSlotFree)
        {
            TrackSlotsGrid[flatenedIdx] = new Vector4(0, 0, 0, 0);
            TrackSlotsGrid[InitialCoords.x+InitialCoords.y*6] = new Vector4(99, 99, 0, 0);
            //var assignedPos = new Vector3(
            //    Xindex * content.rect.width,
            //    ((Yindex * 2 + 1) * mesureElementSpacing - 0.5f) * content.rect.height,
            //-10);

           //item.GetComponent<RectTransform>().localPosition = assignedPos;
           //item.GetComponent<TrackPlaybackItem>().assignedPosition = assignedPos;
        }
        else
        {
            Debug.Log("occupado");
        }
        return isGridSlotFree;
    }
    public int2 GetCoordsOnTrack(Vector2 localPoint)
    {
        //Debug.LogWarning(new Vector2(localPoint.x , localPoint.y));
        Vector2 normalizedPoint = new Vector2(localPoint.x / content.rect.width+0.5f, localPoint.y / content.rect.height + 0.5f);
        Debug.LogWarning(normalizedPoint);
        int Xindex = Mathf.FloorToInt(normalizedPoint.x * ThreadNum);
        int Yindex = Mathf.FloorToInt(normalizedPoint.y * ThreadElementNum);
        return new int2(Xindex,Yindex);
    }
    public bool isSlotFree(int2 coords) { return TrackSlotsGrid[coords.x + coords.y * 6].x < 99; }
    void ProcessRow()
    {
        for (int i = 0; i < ThreadNum; i++)
        {
            // retrive item gb somehow
            // -> remplace TrackPlaybackItem with int2 index to the container's data
            /// Do playback arming here
            //Destroy();
        }
    }

    //public int2 IsPointerDownOnItem(int2 pointerDownCoords)
    //{

    //}

    void DisplaceTrackElements(float displacement)
    {
        foreach (Transform child in content)
        {
            RectTransform item = child as RectTransform;
            if (item == null) continue;

            // Move left
            item.anchoredPosition = item.GetComponent<TrackPlaybackItem>().assignedPosition+new Vector2(0,-displacement);

            // Reset position when off-screen
            if (item.anchoredPosition.y < beltLimit)
            {
                item.anchoredPosition = new Vector2(startPosition, item.anchoredPosition.y);
            }
        }
    }
}