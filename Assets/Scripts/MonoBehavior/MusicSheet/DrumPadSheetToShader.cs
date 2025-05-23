using System;
using UnityEngine;
using UnityEngine.UI;

public class DrumPadSheetToShader : MonoBehaviour
{

    [SerializeField]
    private Material DrumPadSheetMaterial;

    float[] packedPadCheck;
    bool[] BoolPadCheck;



    void Start()
    {
        GetComponent<DrumPadSheetToShader>().enabled = false;

        packedPadCheck = new float[6];

    }
    private void OnEnable()
    {
        this.GetComponent<Image>().material = DrumPadSheetMaterial;
    }

    private void LateUpdate()
    {

        DrumPadSheetData activeSheet = AudioLayoutStorageHolder.audioLayoutStorage.ActiveDrumPadSheetData;

        /// dispose ? GB ? ToArray() CREATE GB PRESSURE
        BoolPadCheck = activeSheet.PadCheck.ToArray();

        PackPadChecks(BoolPadCheck, packedPadCheck);

        DrumPadSheetMaterial.SetFloat("mesureNumber", activeSheet.mesureNumber);
        DrumPadSheetMaterial.SetFloatArray("FullPadChecks", packedPadCheck);

    }

    public void PackPadChecks(bool[] pads, float[] outputArray)
    {
        // Pack each group of 32 bools into one float's bits
        for (int i = 0; i < pads.Length; i++)
        {
            int floatIndex = i / 32;
            int bitIndex = i % 32;

            // Get current bits as uint
            uint bits = BitConverter.ToUInt32(BitConverter.GetBytes(outputArray[floatIndex]), 0);

            // Clear the bit at bitIndex
            bits &= ~(1u << bitIndex);

            // Set the bit if true
            bits |= (pads[i] ? 1u : 0u) << bitIndex;

            // Convert bits back to float
            outputArray[floatIndex] = BitConverter.ToSingle(BitConverter.GetBytes(bits), 0);
        }

    }
}

