using System.Collections;
using System.Collections.Generic;
using MusicNamespace;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.UI;
using static UnityEngine.InputSystem.InputSettings;
using Vector2 = UnityEngine.Vector2;
using Vector3 = UnityEngine.Vector3;
using Vector4 = UnityEngine.Vector4;

/// <summary>
/// rename pb state info
/// </summary>
public struct PlabackUpdateCode
{
    //public ushort threadIdx;
    //public bool OnOff;

    public byte PBupdateBitFlags;

    public NativeQueue<ushort> PBupdateIdxQueue;
    public NativeArray<bool> ActiveThreads;
    public NativeArray<ushort> PBcontainerIdices;
}

public class MusicTrackConveyorBelt : MonoBehaviour
{
    RectTransform content; // The parent holding UI elements

    private int ThreadElementNum = 10;
    private int ThreadNum = 6;

    private float currentMesureOffset = 0f;
    private float currentTrackOffset = 0f;

    float mesureElementSpacing;
    //private bool[,] TrackSlotsGridOBS;
    int mesurePassed;

    /// <summary>
    /// Flattened 2D array to pass directly to the shader
    /// Only the first two values are used for TrackCoords
    /// and 3de value for PBcontainerIdx
    /// </summary>
    private Vector4[] TrackSlotsGrid;
    /// <summary>
    /// same here but first 3 first for color
    /// </summary>
    private Vector4[] TrackSlotsGridColor;
    private Material TrackMaterial;


    private PlabackUpdateCode PBupdateInfo;
    //private bool[] ActiveThread;


    [HideInInspector]
    public Dictionary<int2, float3> indexToColorMap = new();

    private void Start()
    {
        content = this.GetComponent<RectTransform>();
        mesureElementSpacing = (0.5f / (ThreadElementNum));
        //TrackSlotsGridOBS = new bool[2,ThreadElementNum+1];
        TrackSlotsGrid = new Vector4[10*6+6];
        TrackSlotsGridColor = new Vector4[10 * 6 + 6];

        //ActiveThread = new bool[ThreadNum];
        //PBupdateCodes = new NativeArray<PlabackUpdateCode>(ThreadNum,Allocator.Persistent);
        PBupdateInfo = new PlabackUpdateCode
        {
            PBupdateIdxQueue = new NativeQueue<ushort>(Allocator.Persistent),
            ActiveThreads = new NativeArray<bool>(7,Allocator.Persistent),
            PBcontainerIdices = new NativeArray<ushort>(7,Allocator.Persistent),
        };


        mesurePassed = 1;
        for (int i = 0; i < TrackSlotsGrid.Length; i++)
        {
            // 99->empty slot
            TrackSlotsGrid[i] = new Vector4(99, 99, 99, 0);
            TrackSlotsGridColor[i] = new Vector4(1, 1, 1, 0);
        }
        //for (int i = 0; i < ActiveThread.Length; i++)
        //{
        //    ActiveThread[i] = false;
        //}
        TrackMaterial = this.GetComponent<Image>().material;

    }

    private void OnDestroy()
    {
        PBupdateInfo.ActiveThreads.Dispose();
        PBupdateInfo.PBupdateIdxQueue.Dispose();
        PBupdateInfo.PBcontainerIdices.Dispose();
    }

    void Update()
    {
        currentMesureOffset = (MusicUtils.time % 4f) * 0.25f;
        currentTrackOffset = (MusicUtils.time / (ThreadElementNum * 4f));

        if(MusicUtils.time*0.25f+Time.deltaTime>mesurePassed)
        {
            ProcessRow();
            mesurePassed++;
        }

        /// COULD DO WITHOUT UPDATE EVERY FRAME
        TrackMaterial.SetVectorArray("TrackGridArray", TrackSlotsGrid);
        TrackMaterial.SetVectorArray("TrackGridColorArray", TrackSlotsGridColor);

    }


    public bool _TryInsertTrackElement(float localY,int2 draggedObjectPBidx)
    {
        //Vector2 normalizedPoint = new Vector2(localPoint.x/content.rect.width, (localPoint.y / content.rect.height + 0.5f));
        //Debug.Log(normalizedPoint.y);
        int Xindex = draggedObjectPBidx.x;
        int Yindex = Mathf.FloorToInt((localY / content.rect.height + 0.5f) * ThreadElementNum + currentMesureOffset);
        /// can't insert in running mesures
        if (Yindex == 0) return false;
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


            int2 spriteIdx = new int2(Xindex % 3, (Xindex / 3));
            TrackSlotsGrid[flatenedIdx] = new Vector4(spriteIdx.x, spriteIdx.y, draggedObjectPBidx.y, 0);
            indexToColorMap.TryGetValue(draggedObjectPBidx, out float3 color);
            //float3 color = ;//UnpackFloat3ToInt16Bit(draggedObjectPBidx.y);
            TrackSlotsGridColor[flatenedIdx] = new Vector4(color.x, color.y, color.z, 0);

            /// Insert in next mesure -> prepair playback
            if(Yindex==1 && TrackSlotsGrid[Xindex].x==99)
            {

                ///GetComponent<MusicTrack>().PlaybackHolderArray[Xindex]._QuePlaybackUI();
                //PBupdateInfo.PBupdateIdxQueue.Enqueue(x);
                //PBupdateInfo.PBupdateBitFlags |= (byte)(1 << x);
                //PBupdateInfo.PBcontainerIdices[x] = ContaineritemIdx;
                //PBupdateInfo.ActiveThreads[x] = true;

            }

        }
        else
        {
            //Debug.Log("occupado");
        }
        return isGridSlotFree;
    }
    public bool _TryMoveTrackElement(Vector2 localPoint, int2 InitialCoords)
    {
        Vector2 normalizedPoint = new Vector2(localPoint.x / content.rect.width, (localPoint.y / content.rect.height + 0.5f));
        int Xindex = InitialCoords.x;
        int Yindex = Mathf.FloorToInt(normalizedPoint.y * ThreadElementNum + currentMesureOffset);
        /// can't insert in running mesures
        if (Yindex == 0) return false;
        int flatenedIdx = Xindex + Yindex * 6;

        //Debug.Log(InitialCoords);

        bool isGridSlotFree = TrackSlotsGrid[flatenedIdx].x==99;
        if (isGridSlotFree)
        {
            int2 spriteIdx = new int2(Xindex % 3, (Xindex / 3));
            TrackSlotsGrid[flatenedIdx] = new Vector4(spriteIdx.x, spriteIdx.y, TrackSlotsGrid[InitialCoords.x + InitialCoords.y * 6].z, 0);
            TrackSlotsGridColor[flatenedIdx] = TrackSlotsGridColor[InitialCoords.x + InitialCoords.y * 6];
            TrackSlotsGrid[InitialCoords.x+InitialCoords.y*6] = new Vector4(99, 99, 99, 0);

            if(TrackSlotsGrid[0].x == 99)
            {
                /// Insert in next mesure -> prepair playback
                if (Yindex == 1)
                {
                    GetComponent<MusicTrack>().PlaybackHolderArray[Xindex]._QuePlaybackUI();
                }
                /// remove from the prepairing state
                else if (InitialCoords.y == 1)
                {
                    GetComponent<MusicTrack>().PlaybackHolderArray[Xindex]._CancelPlaybackPrepair();
                }
            }
       
        }
        else
        {
            //Debug.Log("occupado");
        }
        return isGridSlotFree;
    }
    public int2 GetCoordsOnTrack(Vector2 localPoint)
    {
        //Debug.LogWarning(new Vector2(localPoint.x , localPoint.y));
        Vector2 normalizedPoint = new Vector2(localPoint.x / content.rect.width+0.5f, localPoint.y / content.rect.height + 0.5f);
        int Xindex = Mathf.FloorToInt(normalizedPoint.x * ThreadNum);
        int Yindex = Mathf.FloorToInt(normalizedPoint.y * ThreadElementNum+ currentMesureOffset);
        return new int2(Xindex,Yindex);
    }
    public bool isSlotFree(int2 coords) { return TrackSlotsGrid[coords.x + coords.y * 6].x < 99; }
    void ProcessRow()
    {
        MusicTrack musicTrack = GetComponent<MusicTrack>();
        /// adjust the draggedObjectInitialCoords for if a item is being dragged while the rows get incremented
        int2 draggedCoords = musicTrack.draggedItemInitialCoords;
        GetComponent<MusicTrack>().draggedItemInitialCoords = new int2(draggedCoords.x, draggedCoords.y - 1);
        /// drop item as it is getting locked in for playback
        if (draggedCoords.y == 1) musicTrack.ForceDrop();

        for (ushort x = 0; x < ThreadNum; x++)
        {


            for (int y = 0; y < ThreadElementNum; y++)
            {
                /// Icrement slots downward to keep up with the continuous displacement
                TrackSlotsGrid[x + y * 6] = TrackSlotsGrid[x + (y+1) * 6];
                TrackSlotsGridColor[x + y * 6] = TrackSlotsGridColor[x + (y + 1) * 6];
            }

            if (TrackSlotsGrid[x].x == 99)
            {
                /// Anticipate 1 ahead for display to start prepair
                if (TrackSlotsGrid[x + 6].x < 99)
                {
                    //musicTrack.PlaybackHolderArray[x]._QuePlaybackUI();
                }
                /// Stop the thread
                if (PBupdateInfo.ActiveThreads[x] == true)
                {
                    //musicTrack.PlaybackHolderArray[x]._StopCurrentPlayback(x);
                    //PBupdateCodes.Push(new PlabackUpdateCode
                    //{
                    //    threadIdx = x,
                    //    OnOff = false 
                    //});
                    PBupdateInfo.PBupdateIdxQueue.Enqueue(x);
                    PBupdateInfo.PBupdateBitFlags |= (byte)(1 << x);
                    PBupdateInfo.ActiveThreads[x] = false;
                    Debug.Log("stop");
                }
            }
            else
            {

                /// restart thread with new playback
                ushort ContaineritemIdx = (ushort)TrackSlotsGrid[x].z;

                /// DO bool check and function change to also write playback cause new container use ?
                //musicTrack.PlaybackHolderArray[x]._ImmediatePlaybackActivate(new int2(x, ContaineritemIdx));
                //PBupdateCodes.Push(new PlabackUpdateCode { 
                //    threadIdx = x,
                //    OnOff = true,
                //    PBcontainerIdx = ContaineritemIdx
                //});
                if (PBupdateInfo.PBcontainerIdices[x] != ContaineritemIdx || PBupdateInfo.ActiveThreads[x] == false)
                {
                    PBupdateInfo.PBupdateIdxQueue.Enqueue(x);
                    PBupdateInfo.PBupdateBitFlags |= (byte)(1 << x);
                    PBupdateInfo.PBcontainerIdices[x] = ContaineritemIdx;
                    PBupdateInfo.ActiveThreads[x] = true;
                }
            }
        }

        UIManager.Instance._UpdatePlaybacks(PBupdateInfo);
        /// reset update flags after being proccessed
        PBupdateInfo.PBupdateBitFlags = 0;

        TrackSlotsGrid[60] = new Vector4(99, 99, 99, 0);
        TrackSlotsGrid[61] = new Vector4(99, 99, 99, 0);
        TrackSlotsGrid[62] = new Vector4(99, 99, 99, 0);
        TrackSlotsGrid[63] = new Vector4(99, 99, 99, 0);
        TrackSlotsGrid[64] = new Vector4(99, 99, 99, 0);
        TrackSlotsGrid[65] = new Vector4(99, 99, 99, 0);
    }

    //int UnpackFloat3ToInt16Bit(float3 color)
    //{
    //    // Scale the float values to the range of 0–65535 (for 16-bit per channel)
    //    int r = Mathf.FloorToInt(color.x * 65535f); // Scale r (0.0–1.0 -> 0–65535)
    //    int g = Mathf.FloorToInt(color.y * 65535f); // Scale g (0.0–1.0 -> 0–65535)
    //    int b = Mathf.FloorToInt(color.z * 65535f); // Scale b (0.0–1.0 -> 0–65535)

    //    // Pack the values into a single 48-bit integer (16-bit per channel)
    //    return (r << 32) | (g << 16) | b;
    //}


}