using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;
using Random = UnityEngine.Random;

public class PlaybackHolder : MonoBehaviour
{
    [SerializeField]
    GameObject PBcontainerPrefab;
    [SerializeField]
    EquipmentUIelement synthUIelement;
    public GameObject trackPlaybackItem;

    [SerializeField]
    Sprite associatedSprite;
    [HideInInspector]
    public EquipmentCategory equipmentCategory;

    private int ContainerNumber = 0;
    private int colorIteration = 0;

    void Start()
    {

    }
    public void _AddContainerUI(int2 PBidx,ushort relativeEquipmentIdx)
    {
        var uiManager = UIManager.Instance;
        uiManager.MusicSheetGB.SetActive(false);
        _RearangeContainerForAdd();

        var PBcontainerInstance = Instantiate(PBcontainerPrefab, this.GetComponent<RectTransform>());
        PBcontainerInstance.GetComponent<RectTransform>().anchoredPosition = new Vector2(14.5f* ContainerNumber, -11.8f);

        var containerUI = PBcontainerInstance.GetComponent<PlaybackContainerUI>();

        /// arbitrary 3 -> set number of remaining playback use
        containerUI.GetComponentInChildren<TextMeshProUGUI>().text = "3";

        float3 uniqueColor = GenerateUniqueColor();
        //float3 uniqueColor = PackIntToFloat3(colorIteration);
        //colorIteration++;
        containerUI.associatedColor = new Color(uniqueColor.x, uniqueColor.y, uniqueColor.z, 1);
        uiManager.MusicTrackGB.GetComponent<MusicTrackConveyorBelt>().indexToColorMap.Add(new int2(PBidx.x, ContainerNumber), uniqueColor);

        containerUI.associatedSprite = associatedSprite;
        containerUI.playbackHolder = GetComponent<PlaybackHolder>();
        containerUI.PBidx = PBidx;
        containerUI.relativeEquipmentIdx = relativeEquipmentIdx;
        trackPlaybackItem.gameObject.SetActive(true);
        containerUI._ArmPlaybackItem(PBidx.y);

        ContainerNumber++;
    }
    /// Arm the synth for the next mesure
    public void _QuePlaybackUI()
    {
        //Debug.LogError("not supposed to happen");
        synthUIelement._DisplayPrepairActivation();
    }
    /// Arm the synth for immediate playback
    public void _ImmediatePlaybackActivate(int2 PBidx)
    {
        if(equipmentCategory == EquipmentCategory.Weapon)
            UIManager.Instance._ActivateSynthPlayback(PBidx);
        else
            UIManager.Instance._ActivateDrumMachinePlayback(PBidx);
    }
    /// Stop the no longer active current playback
    public void _StopCurrentPlayback(int equipmentIdx)
    {
        if (equipmentCategory == EquipmentCategory.Weapon)
            UIManager.Instance._StopSynthPlayback(equipmentIdx);
        else
            UIManager.Instance._StopDrumMachinePlayback(equipmentIdx);

    }
    public void _CancelPlaybackPrepair()
    {
        synthUIelement._CancelActivation();
    }


    private void _RearangeContainerForAdd()
    {
        foreach (Transform sibling in transform)
        {
            // Skip any object with TextMeshProUGUI
            if (sibling.GetComponent<TextMeshProUGUI>() != null)
                continue;

            RectTransform rectTransform = sibling as RectTransform;
            if (rectTransform != null)
            {
                rectTransform.anchoredPosition += new Vector2(-14.5f, 0);
            }
        }
    }

    float3 GenerateUniqueColor()
    {
        System.Random rng = new System.Random(colorIteration); // Prime multipliers for better spread

        float r = (float)rng.NextDouble();
        float g = (float)rng.NextDouble();
        float b = (float)rng.NextDouble();

        colorIteration++;

        return new float3(r, g, b);
    }

}
