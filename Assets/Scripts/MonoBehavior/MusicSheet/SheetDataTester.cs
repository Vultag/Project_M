using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
///  DO TESTING FOR DRUM PAD RENAME TOO
/// </summary>


[ExecuteInEditMode] // This attribute makes the script execute in edit mode
public class SheetDataTester : MonoBehaviour
{

    [SerializeField]
    EquipmentCategory equipmentTested;

    private float mesureNumber;

    private float[] ElementsInMesure;
    private float[] NoteElements;
    private float[] NotesSpriteIdx;
    private float[] NotesHeight;

    private bool[] PadCheck;
    private float[] packedPadCheck;

    [SerializeField]
    private Material musicMaterial;
    [SerializeField]
    private Material padMaterial;


    private void OnValidate()
    {
        mesureNumber = 1;

        switch (equipmentTested)
        {
            case EquipmentCategory.Weapon:

                this.GetComponent<Image>().material = musicMaterial;

                //ElementsInMesure = new float[4] { 1, 10, 0, 0 };
                ///////NoteElements = new float[12] { 1, 1, 1, 1, 1, 4, 4, 4, 4, 4, 4, 4 };
                //NotesSpriteIdx = new float[12] { 20, 10, 10, 10, 10, 10, 10, 10, 10, 10, 10, 10 };
                //NotesHeight = new float[12] { 4, 6, 4, 6.25f, 4, 6, 6, 6.25f, 4, 6.75f, 6, 6.75f };

                /// CREATES THE ARRAY FOR THE MAIN PROGRAM TOO !
                ElementsInMesure = new float[4] { 1, 0, 0, 0 };
                //NoteElements = new float[48];
                NotesSpriteIdx = new float[48];
                NotesHeight = new float[48];
                NotesHeight[0] = 4f;
                NotesSpriteIdx[0] = 20;
                //NotesHeight[1] = 4.25f;
                //NotesSpriteIdx[1] = 13.25f;
                //NotesHeight[2] = 4f;
                //NotesSpriteIdx[2] = 13.5f;
                //NotesHeight[3] = 4f;
                //NotesSpriteIdx[3] = 13;
                //NotesHeight[4] = 4f;
                //NotesSpriteIdx[4] = -13;
                //NotesHeight[5] = 4f;
                //NotesSpriteIdx[5] = -13;
                //NotesHeight[6] = 4f;
                //NotesSpriteIdx[6] = -13;

                //for (int i = 0; i < NoteElements.Length; i++)
                //{
                //    NoteElements[i] = 1.0f;
                //}


                break;
            case EquipmentCategory.DrumMachine:

                PadCheck = new bool[32 * 6];
                packedPadCheck = new float[6];

                PadCheck[0] = true;
                PadCheck[1] = true;
                PadCheck[6] = true;

                PadCheck[16] = true;
                //PadCheck[18] = true;

                PadCheck[32] = true;


                this.GetComponent<Image>().material = padMaterial;


                break;
        }

    
        UpdateMaterialProperties();
    }

    private void UpdateMaterialProperties()
    {

        switch (equipmentTested)
        {
            case EquipmentCategory.Weapon:

                if (musicMaterial == null) return;

                //Debug.Log("Setting material properties");
                // Update the material properties whenever the script is enabled or validated in the editor
                musicMaterial.SetFloat("mesureNumber", mesureNumber);
                musicMaterial.SetFloatArray("ElementsInMesure", ElementsInMesure);
                //material.SetFloatArray("NoteElements", NoteElements);
                musicMaterial.SetFloatArray("NotesSpriteIdx", NotesSpriteIdx);
                musicMaterial.SetFloatArray("NotesHeight", NotesHeight);

                break;
            case EquipmentCategory.DrumMachine:

                if (padMaterial == null) return;

                /// dispose ? GB ? ToArray() CREATE GB PRESSURE
                var pads = PadCheck;

                this.GetComponent<DrumPadSheetToShader>().PackPadChecks(pads, packedPadCheck);

                padMaterial.SetFloat("mesureNumber", mesureNumber);
                padMaterial.SetFloatArray("FullPadChecks", packedPadCheck);



                break;
        }

        
    }

}